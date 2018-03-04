//
//  MyUnityPluginForIOS.h
//  MyUnityPluginForIOS
//

#ifndef MyUnityPluginForIOS_h
#define MyUnityPluginForIOS_h
#define NUM_OF_PARTS 68


extern "C" {
    void* getVideoDevice(int device_num);
    void releaseVideoDevice(void* cap);
    void* getDetectorAndPoseModel(const char* model_dat_path);
    void detect(void* cap, void* dapm, int face_rect[4], int parts_matrix[NUM_OF_PARTS][2]);
}

#endif /* MyUnityPluginForIOS_h */
