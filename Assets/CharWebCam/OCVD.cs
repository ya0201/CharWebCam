using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class OCVD : MonoBehaviour {
	
	[DllImport ("MyUnityPlugin")]
	protected static extern IntPtr getVideoDevice(); 
	[DllImport ("MyUnityPlugin")]
	protected static extern void releaseVideoDevice(IntPtr cap); 
	[DllImport ("MyUnityPlugin")]
	protected static extern IntPtr getDetectorAndPoseModel(string model_dat_path);
	[DllImport ("MyUnityPlugin")]
	protected static extern void detect(IntPtr cap, IntPtr dapm, int[] face_rect, int[,] matrix);


	protected IntPtr cap_;
	protected IntPtr dapm_;
	protected int frame_ctr_;
	protected string MODEL_DAT_PATH = "/Users/Shared/Unity/CharWebCam/Assets/shape_predictor_68_face_landmarks.dat";
	protected const int NUM_OF_PARTS = 68;
	protected int[] face_rect_ = new int[4];
	protected int[,] detected_parts_matrix_ = new int[NUM_OF_PARTS,2];
	protected const int IMAGE_WIDTH = 720;
	protected const int IMAGE_HEIGHT = 480;

	public Text ErrorLog;
	public Text DetectedValue;
	public Material Material;

	// キャラクター制御パラメーター
	protected Vector3 BodyPos;
	protected Vector3 HeadAng;
	protected Vector2 EyesPos;
	protected float EyesClose;
	protected float BrowRai;
	protected float BrowLow;
	protected float Smile;
	protected float Kiss;
	protected float Mouth;
	protected float Tongue;

	// キャラクター制御パラメーターの調整値
	protected float BodyPosX = 3;
	protected float BodyPosY = 3;
	protected float BodyPosZ = 500;
	protected float BodyPosYOffset;
	protected int BodyPosSmoothWeight = 20;
	protected float HeadAngX = 70;
	protected float HeadAngY = 90;
	protected float HeadAngZ = 300;
	protected int HeadAngSmoothWeight = 20;
	protected float EyesPosX = 0.8f;
	protected float EyesPosY = 0.2f;
	protected int EyesPosSmoothWeight = 5;
	protected int EyesCloseSmoothWeight = 3;
	protected int FaceSmoothWeight = 10;

	// 検出値取得
	protected bool Ready = false;


	void Initialize () {
		cap_ = getVideoDevice ();
		dapm_ = getDetectorAndPoseModel (MODEL_DAT_PATH);
		frame_ctr_ = 0;
		for (int i=0; i<NUM_OF_PARTS; i++) {
			detected_parts_matrix_[i,0] = detected_parts_matrix_[i,1] = 0;
		}
	}

	void UpdateValue() {
		if (frame_ctr_++ % 4 != 0) {
			return;
		}
		detect (cap_, dapm_, face_rect_, detected_parts_matrix_);
		string str = "part0 = (" + detected_parts_matrix_ [0,0] + ", " + detected_parts_matrix_ [0,1] + ")";
		Debug.Log (str);
	}

	void Finalize() {
		releaseVideoDevice(cap_);
	}

	/// <summary>
	/// 体位置を取得
	/// </summary>
	/// <param name="face_rect">顔の矩形</param>
	/// <returns>体位置</returns>
	Vector3 GetBodyPos(int[] face_rect)
	{
		// 体位置に利用するため頭位置を取得
		float xMax = IMAGE_WIDTH;
		float yMax = IMAGE_HEIGHT;
		float xPos = face_rect[0] + (face_rect[2] / 2);	// x + (w / 2)
		float yPos = face_rect[1] + (face_rect[3] / 2);	// y + (h / 2)
		float zPos = (yMax - face_rect[3]);

		// 末尾の除算で調整
		xPos = (xPos - (xMax / 2)) / (xMax / 2) / BodyPosX;
		yPos = (yPos - (yMax / 2)) / (yMax / 2) / BodyPosY;
		zPos = zPos / BodyPosZ;

		// 初期位置のオフセットを適用
		yPos += BodyPosYOffset;

		// 顔の大きさと中心から初期位置分ずらして体位置に利用
		return new Vector3(-xPos, yPos, zPos);
	}
}