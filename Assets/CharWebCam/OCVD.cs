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

	[DllImport ("MySmoother")]
	protected static extern IntPtr getSmoother1D(float alpha, float gamma);
	[DllImport ("MySmoother")]
	protected static extern IntPtr getSmoother2D(float alpha, float gamma);
	[DllImport ("MySmoother")]
	protected static extern IntPtr getSmoother3D(float alpha, float gamma);
	[DllImport ("MySmoother")]
	protected static extern void deleteSmoother1D(IntPtr smoother);
	[DllImport ("MySmoother")]
	protected static extern void deleteSmoother2D(IntPtr smoother);
	[DllImport ("MySmoother")]
	protected static extern void deleteSmoother3D(IntPtr smoother);
	[DllImport ("MySmoother")]
	protected static extern float smooth1D(IntPtr smoother, float f);
	[DllImport ("MySmoother")]
	protected static extern Vector2 smooth2D(IntPtr smoother, Vector2 p);
	[DllImport ("MySmoother")]
	protected static extern Vector3 smooth3D(IntPtr smoother, Vector3 p);



	protected IntPtr cap_;
	protected IntPtr dapm_;
	protected int frame_ctr_;
	protected string MODEL_DAT_PATH = "/Users/Shared/Unity/CharWebCam/Assets/shape_predictor_68_face_landmarks.dat";
	protected const int NUM_OF_PARTS = 68;
	protected int[] face_rect_ = new int[4];
	protected int[,] detected_parts_matrix_ = new int[NUM_OF_PARTS,2];
	protected const int IMAGE_WIDTH = 720;
	protected const int IMAGE_HEIGHT = 480;

	// smoother
	protected const float ALPHA = 0.3f;
	protected const float GAMMA = 0.5f;
	IntPtr body_smoother_;	//3D
	IntPtr head_smoother_;	//3D
//	Smoother2D SmoothEyes = null;
//	Smoother1D SmoothEyesClose = null;
//	Smoother1D SmoothBrowRai = null;
//	Smoother1D SmoothBrowLow = null;
//	Smoother1D SmoothSmile = null;
//	Smoother1D SmoothKiss = null;
//	Smoother1D SmoothMouth = null;
//	Smoother1D SmoothTongue = null;

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


	protected void Init () {
		cap_ = getVideoDevice ();
		dapm_ = getDetectorAndPoseModel (MODEL_DAT_PATH);
		frame_ctr_ = 0;
		for (int i=0; i<NUM_OF_PARTS; i++) {
			detected_parts_matrix_[i,0] = detected_parts_matrix_[i,1] = 0;
		}

		BodyPos = new Vector3 (BodyPosX, BodyPosY, BodyPosZ);

		body_smoother_ = getSmoother3D(ALPHA, GAMMA);
		head_smoother_ = getSmoother3D(ALPHA, GAMMA);
	}

	protected void UpdateParam() {
		if (frame_ctr_++ % 4 != 0) {
			return;
		}
		detect (cap_, dapm_, face_rect_, detected_parts_matrix_);
//		string str = "part0 = (" + detected_parts_matrix_ [0,0] + ", " + detected_parts_matrix_ [0,1] + ")";
//		Debug.Log (str);

		// 体位置
		BodyPos = smooth3D(body_smoother_, GetBodyPos(face_rect_));

		// 頭角度
		HeadAng = smooth3D(head_smoother_, GetHeadAng(detected_parts_matrix_));

		// 視線
		EyesPos = GetEyesPos(detected_parts_matrix_);

		// 目パチ
//		float eyeL = FaceExp[FaceExpression.EXPRESSION_EYES_CLOSED_LEFT].intensity;
//		float eyeR = FaceExp[FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT].intensity;
//		EyesClose = SmoothEyesClose.SmoothValue(Mathf.Max(eyeL, eyeR));
//		EyesClose = EyesClose < 50 ? 0 : (EyesClose - 50) * 2;
		EyesClose = 50;

		// 眉上
//		float browRaiL = FaceExp[FaceExpression.EXPRESSION_BROW_RAISER_LEFT].intensity;
//		float browRaiR = FaceExp[FaceExpression.EXPRESSION_BROW_RAISER_RIGHT].intensity;
//		BrowRai = SmoothBrowRai.SmoothValue(Mathf.Max(browRaiL, browRaiR));
		BrowRai = 50;

		// 眉下
//		float browLowL = FaceExp[FaceExpression.EXPRESSION_BROW_LOWERER_LEFT].intensity;
//		float browLowR = FaceExp[FaceExpression.EXPRESSION_BROW_LOWERER_RIGHT].intensity;
//		BrowLow = SmoothBrowLow.SmoothValue(Mathf.Max(browLowL, browLowR));
		BrowLow = 50;

		// 笑顔
//		Smile = SmoothSmile.SmoothValue(FaceExp[FaceExpression.EXPRESSION_SMILE].intensity);
//
//		// キス(口開と若干競合)
//		Kiss = SmoothKiss.SmoothValue(FaceExp[FaceExpression.EXPRESSION_KISS].intensity);
//
//		// 口開(キスと若干競合)
//		Mouth = SmoothMouth.SmoothValue(FaceExp[FaceExpression.EXPRESSION_MOUTH_OPEN].intensity);
//
//		// べー(口開と競合)
//		Tongue = SmoothTongue.SmoothValue(FaceExp[FaceExpression.EXPRESSION_TONGUE_OUT].intensity);

		Ready = true;

	}

	void OnDestroy() {
		releaseVideoDevice(cap_);
		deleteSmoother3D (body_smoother_);
		deleteSmoother3D (head_smoother_);
	}

	/// <summary>
	/// 検出値表示用文字列の取得
	/// </summary>
	protected void UpdateParamText()
	{
		string text = "";
		text += "BodyPos\n" + BodyPos + "\n";
		text += "HeadAng\n" + HeadAng + "\n";
		text += "EyesPos\n" + EyesPos + "\n";
		text += "EyesClose : " + EyesClose + "\n";
		text += "BrowRai : " + BrowRai + "\n";
		text += "BrowLow : " + BrowLow + "\n";
		text += "Smile : " + Smile + "\n";
		text += "Kiss : " + Kiss + "\n";
		text += "Mouth : " + Mouth + "\n";
		text += "Tongue : " + Tongue + "\n";
		DetectedValue.text = text;
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

	/// <summary>
	/// 頭角度を取得
	/// </summary>
	/// <param name="detected_parts_matrix">顔器官検出結果</param>
	/// <returns>頭角度</returns>
	Vector3 GetHeadAng(int[,] detected_parts_matrix)
	{
		// 頭向きに利用するため顔の中心と左右端、唇下、顎下を取得
//		Vector2 center = points[29].image;
//		Vector2 left = points[68].image;
//		Vector2 right = points[54].image;
//		Vector2 mouth = points[42].image;
//		Vector2 chin = points[61].image;
		Vector2 center = new Vector2(detected_parts_matrix[30,0], detected_parts_matrix[30,1]);
		Vector2 left = new Vector2(detected_parts_matrix[15,0], detected_parts_matrix[15,1]);;
		Vector2 right = new Vector2(detected_parts_matrix[1,0], detected_parts_matrix[1,1]);
		Vector2 mouth = new Vector2(detected_parts_matrix[57,0], detected_parts_matrix[57,1]);
		Vector2 chin = new Vector2(detected_parts_matrix[8,0], detected_parts_matrix[8,1]);

		// 末尾で調整(0.2は顔幅に対する唇下から顎までの比 / 300はその値に対する倍率 / 10.416はUnityちゃん初期値)
		float xAng = (Vector2.Distance(left, center) - Vector2.Distance(right, center)) / Vector2.Distance(left, right) * HeadAngX;
		float yAng = GetAngle(mouth, chin) - HeadAngY;
		float zAng = (Vector2.Distance(mouth, chin) / Vector2.Distance(left, right) - 0.2f) * HeadAngZ;

		// 唇下と顎下の点から角度計算して頭向きに利用
		return new Vector3(xAng, -yAng, zAng);
	}

	/// <summary>
	/// 瞳位置を取得
	/// </summary>
	/// <param name="detected_parts_matrix">顔器官検出結果</param>
	/// <returns>瞳位置</returns>
	/// <remarks>
	/// https://software.intel.com/sites/landingpage/realsense/camera-sdk/v2016r3/documentation/html/index.html?doc_face_face_landmark_data.html
	/// </remarks>
	Vector2 GetEyesPos(int[,] detected_parts_matrix)
	{
		// 左右の目の瞳と上下左右端を取得
//		Vector2 lEye = points[77].image;
//		Vector2 lLeft = points[22].image;
//		Vector2 lRight = points[18].image;
//		Vector2 lTop = points[20].image;
//		Vector2 lBottom = points[24].image;
//		Vector2 rEye = points[76].image;
//		Vector2 rLeft = points[10].image;
//		Vector2 rRight = points[14].image;
//		Vector2 rTop = points[12].image;
//		Vector2 rBottom = points[16].image;
		Vector2 lLeft = new Vector2(detected_parts_matrix[45,0], detected_parts_matrix[45,1]); //22
		Vector2 lRight = new Vector2(detected_parts_matrix[42,0], detected_parts_matrix[42,1]); //18
		Vector2 lTop = new Vector2((detected_parts_matrix[43,0] + detected_parts_matrix[44,0]) / 2, 
			Math.Min(detected_parts_matrix[43,1], detected_parts_matrix[43,1])); //20
		Vector2 lBottom = new Vector2((detected_parts_matrix[46,0] + detected_parts_matrix[47,0]) / 2, 
			Math.Max(detected_parts_matrix[46,1], detected_parts_matrix[47,1])); //24
		Vector2 lEye = new Vector2((lLeft.x + lRight.x) / 2, (lTop.y + lBottom.y) / 2); //77
		Vector2 rLeft = new Vector2(detected_parts_matrix[39,0], detected_parts_matrix[39,1]); //10
		Vector2 rRight = new Vector2(detected_parts_matrix[36,0], detected_parts_matrix[36,1]); //14
		Vector2 rTop = new Vector2((detected_parts_matrix[37,0] + detected_parts_matrix[38,0]) / 2, 
			Math.Min(detected_parts_matrix[37,1], detected_parts_matrix[38,1])); //12
		Vector2 rBottom = new Vector2((detected_parts_matrix[40,0] + detected_parts_matrix[41,0]) / 2, 
			Math.Max(detected_parts_matrix[40,1], detected_parts_matrix[41,1])); //16
		Vector2 rEye = new Vector2((rLeft.x + rRight.x) / 2, (rTop.y + rBottom.y) / 2); //76

		// 末尾で調整
		float tmp1, tmp2;
		tmp1 = GetCenterRatio(lRight, lEye, lLeft) * EyesPosX;
		tmp2 = GetCenterRatio(rRight, rEye, rLeft) * EyesPosX;
		float xPos = (tmp1 + tmp2) / 2;
		tmp1 = GetCenterRatio(lTop, lEye, lBottom) * EyesPosY;
		tmp2 = GetCenterRatio(rTop, rEye, rBottom) * EyesPosY;
		float yPos = (tmp1 + tmp2) / 2;

		// 唇下と顎下の点から角度計算して頭向きに利用
		return new Vector2(xPos, yPos);
	}

	/// <summary>
	/// 3点の中間比を求める
	/// </summary>
	/// <param name="v1">端1</param>
	/// <param name="center">中点</param>
	/// <param name="v2">端2</param>
	/// <returns>中点比</returns>
	protected float GetCenterRatio(Vector2 v1, Vector2 center, Vector2 v2)
	{
		return (Vector2.Distance(v1, center) - Vector2.Distance(v2, center)) / Vector2.Distance(v1, v2);
	}

	/// <summary>
	/// 2点間の角度を求める
	/// http://qiita.com/2dgames_jp/items/60274efb7b90fa6f986a
	/// https://gist.github.com/mizutanikirin/e9a71ef994ebb5f0d912
	/// </summary>
	/// <param name="p1">点1</param>
	/// <param name="p2">点2</param>
	/// <returns>角度</returns>
	protected float GetAngle(Vector2 p1, Vector2 p2)
	{
		float dx = p2.x - p1.x;
		float dy = p2.y - p1.y;
		float rad = Mathf.Atan2(dy, dx);
		return rad * Mathf.Rad2Deg;
	}
}