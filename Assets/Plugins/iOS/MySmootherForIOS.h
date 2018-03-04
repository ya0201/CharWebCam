//
//  MySmootherForIOS.h
//  MySmootherForIOS
//

#ifndef MySmootherForIOS_h
#define MySmootherForIOS_h

#include <stdio.h>
//#include <string.h>

typedef struct Vector2 {
    float x;
    float y;
} Vector2;

typedef struct Vector3 {
    float x;
    float y;
    float z;
} Vector3;

extern "C" {
    void* getSmoother1D(float alpha, float gamma);
    void* getSmoother2D(float alpha, float gamma);
    void* getSmoother3D(float alpha, float gamma);
    void deleteSmoother1D(void* smoother);
    void deleteSmoother2D(void* smoother);
    void deleteSmoother3D(void* smoother);
    float smooth1D(void* smoother, float f);
    Vector2 smooth2D(void* smoother, Vector2 p);
    Vector3 smooth3D(void* smoother, Vector3 p);
    //    void toString(Vector2 p, char* dest);
}

#endif /* MySmootherForIOS_h */
