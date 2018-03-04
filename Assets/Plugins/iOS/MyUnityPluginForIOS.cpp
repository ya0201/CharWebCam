//
//  MyUnityPluginForIOS.cpp
//  MyUnityPluginForIOS
//

#include "MyUnityPluginForIOS.h"
#include "MySmootherForIOS.h"
#include <iostream>
#include <unistd.h>
#include <sstream>
#include <vector>
#include <opencv2/opencv.hpp>
#include <opencv2/core.hpp>
#include "dlib/opencv.h"
#include "dlib/image_processing/frontal_face_detector.h"
#include "dlib/image_processing.h"


void* getVideoDevice(int device_num) {
    cv::VideoCapture* cap = new cv::VideoCapture(device_num);
    
    // check whether camera device is opened
    if(!cap->isOpened()) {
        // failed to open camera device
        std::cerr << "Error: Failed to open camera" << std::endl;
        exit(1);
    }
    // wait for adjusting brightness
    sleep(1);
    
    // set image size to 180x120
    const double w = 180, h = 120;
    cap->set(CV_CAP_PROP_FRAME_WIDTH, w);
    cap->set(CV_CAP_PROP_FRAME_HEIGHT, h);
    
    return static_cast<void*>(cap);
}

void releaseVideoDevice(void* cap) {
    auto vc = static_cast<cv::VideoCapture*>(cap);
    delete vc;
}

void* getDetectorAndPoseModel(const char* model_dat_path) {
    const std::string FACE_LANDMARK_MODEL = std::string(model_dat_path);
    
    dlib::frontal_face_detector* detector = new dlib::frontal_face_detector();
    std::istringstream sin(dlib::get_serialized_frontal_faces());
    dlib::deserialize(*detector, sin);
    
    dlib::shape_predictor* pose_model = new dlib::shape_predictor();
    dlib::deserialize(FACE_LANDMARK_MODEL) >> *pose_model;
    
    auto return_ptr = static_cast<void*>(new std::pair<dlib::frontal_face_detector*, dlib::shape_predictor*>(detector, pose_model));
    return return_ptr;
}

void detect(void* cap, void* dapm, int face_rect[4], int parts_matrix[NUM_OF_PARTS][2]) {
    auto vc = static_cast<cv::VideoCapture*>(cap);
    auto pair = static_cast<std::pair<dlib::frontal_face_detector*, dlib::shape_predictor*>*>(dapm);
    auto detector = *(pair->first);
    auto pose_model = *(pair->second);
    
    cv::Mat frame;
    vc->read(frame);
    
    // dlib
    {
        dlib::cv_image<dlib::bgr_pixel> cimg(frame);
        
        // Detect faces
        std::vector<dlib::rectangle> faces = detector(cimg);
        
        if (faces.size() != 1) {
            // no face or more than 2 faces
            return;
        }
        
        // Find the pose the face
        dlib::full_object_detection shape = pose_model(cimg, faces[0]);
        if (shape.num_parts() != NUM_OF_PARTS) {
            // the number of detected parts is not 68
            return;
        }
        
        auto rect = shape.get_rect();
        face_rect[0] = (int)rect.left();
        face_rect[1] = (int)rect.top();
        face_rect[2] = (int)rect.width();
        face_rect[3] = (int)rect.height();
        for (auto i = 0; i < NUM_OF_PARTS; i++) {
            auto part = shape.part(i);
                
            parts_matrix[i][0] = (int)part.x();
            parts_matrix[i][1] = (int)part.y();
        }
    }
}

// Double Exponential Smoothing
// http://nbviewer.jupyter.org/github/wingcloud/notebooks/blob/master/SmoothingFilter1.ipynb
class Smoother {
public:
    Smoother(float alpha, float gamma) {
        alpha_ = alpha;
        gamma_ = gamma;
    }
    
protected:
    float alpha_;
    float gamma_;
};

class Smoother1D : public Smoother {
public:
    Smoother1D(float alpha, float gamma) : Smoother(alpha, gamma) {
        empty_ = true;
    }
    
    float smooth(float input) {
        if (empty_) {
            empty_ = false;
            last_smoothed_ = last_b_ = input;
            return input;
        }
        
        float smoothed = alpha_*input + (1-alpha_)*(last_smoothed_+last_b_);
        float b = gamma_*(smoothed-last_smoothed_) + (1-gamma_)*last_b_;
        last_smoothed_ = smoothed;
        last_b_ = b;
        
        return smoothed;
    }
    
private:
    float last_smoothed_;
    float last_b_;
    bool empty_;
};

class Smoother2D : public Smoother {
public:
    Smoother2D(float alpha, float gamma) : Smoother(alpha, gamma) {
        x_smoother_ = new Smoother1D(alpha, gamma);
        y_smoother_ = new Smoother1D(alpha, gamma);
    }
    
    ~Smoother2D() {
        if (!x_smoother_) {
            delete x_smoother_;
        }
        if (!y_smoother_) {
            delete y_smoother_;
        }
    }
    
    Vector2 smooth(Vector2 input) {
        Vector2 result;
        result.x = x_smoother_->smooth(input.x);
        result.y = y_smoother_->smooth(input.y);
        
        return result;
    }
    
private:
    Smoother1D* x_smoother_;
    Smoother1D* y_smoother_;
};

class Smoother3D : public Smoother {
public:
    Smoother3D(float alpha, float gamma) : Smoother(alpha, gamma) {
        x_smoother_ = new Smoother1D(alpha, gamma);
        y_smoother_ = new Smoother1D(alpha, gamma);
        z_smoother_ = new Smoother1D(alpha, gamma);
    }
    
    ~Smoother3D() {
        if (!x_smoother_) {
            delete x_smoother_;
        }
        if (!y_smoother_) {
            delete y_smoother_;
        }
        if (!z_smoother_) {
            delete z_smoother_;
        }
    }
    
    Vector3 smooth(Vector3 input) {
        Vector3 result;
        result.x = x_smoother_->smooth(input.x);
        result.y = y_smoother_->smooth(input.y);
        result.z = z_smoother_->smooth(input.z);
        
        return result;
    }
    
private:
    Smoother1D* x_smoother_;
    Smoother1D* y_smoother_;
    Smoother1D* z_smoother_;
};

void* getSmoother1D(float alpha, float gamma) {
    auto smoother1d = new Smoother1D(alpha, gamma);
    return static_cast<void*>(smoother1d);
}
void* getSmoother2D(float alpha, float gamma) {
    auto smoother2d = new Smoother2D(alpha, gamma);
    return static_cast<void*>(smoother2d);
}
void* getSmoother3D(float alpha, float gamma) {
    auto smoother3d = new Smoother3D(alpha, gamma);
    return static_cast<void*>(smoother3d);
}
void deleteSmoother1D(void* smoother) {
    auto smoother1d = static_cast<Smoother1D*>(smoother);
    delete smoother1d;
}
void deleteSmoother2D(void* smoother) {
    auto smoother2d = static_cast<Smoother2D*>(smoother);
    delete smoother2d;
}
void deleteSmoother3D(void* smoother) {
    auto smoother3d = static_cast<Smoother3D*>(smoother);
    delete smoother3d;
}

float smooth1D(void* smoother, float f) {
    auto smoother1d = static_cast<Smoother1D*>(smoother);
    return smoother1d->smooth(f);
}
Vector2 smooth2D(void* smoother, Vector2 p) {
    auto smoother2d = static_cast<Smoother2D*>(smoother);
    return smoother2d->smooth(p);
}
Vector3 smooth3D(void* smoother, Vector3 p) {
    auto smoother3d = static_cast<Smoother3D*>(smoother);
    return smoother3d->smooth(p);
}
