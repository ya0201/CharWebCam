using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class OCVD : MonoBehaviour {

	#if UNITY_IOS
	[DllImport ("__Internal")]
	private static extern IntPtr getVideoDevice(int camera_num); 
	[DllImport ("__Internal")]
	private static extern void releaseVideoDevice(IntPtr cap); 
	[DllImport ("__Internal")]
	private static extern IntPtr getDetectorAndPoseModel(string model_dat_path);
	[DllImport ("__Internal")]
	protected static extern void detect(IntPtr cap, IntPtr dapm, int[] face_rect, int[,] matrix);
	[DllImport ("__Internal")]
	protected static extern IntPtr getSmoother1D(float alpha, float gamma);
	[DllImport ("__Internal")]
	protected static extern IntPtr getSmoother2D(float alpha, float gamma);
	[DllImport ("__Internal")]
	protected static extern IntPtr getSmoother3D(float alpha, float gamma);
	[DllImport ("__Internal")]
	protected static extern void deleteSmoother1D(IntPtr smoother);
	[DllImport ("__Internal")]
	protected static extern void deleteSmoother2D(IntPtr smoother);
	[DllImport ("__Internal")]
	protected static extern void deleteSmoother3D(IntPtr smoother);
	[DllImport ("__Internal")]
	protected static extern float smooth1D(IntPtr smoother, float f);
	[DllImport ("__Internal")]
	protected static extern Vector2 smooth2D(IntPtr smoother, Vector2 p);
	[DllImport ("__Internal")]
	protected static extern Vector3 smooth3D(IntPtr smoother, Vector3 p);

	#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX // OSX

	[DllImport ("MyUnityPlugin")]
	protected static extern IntPtr getVideoDevice(int camera_num); 
	[DllImport ("MyUnityPlugin")]
	protected static extern void releaseVideoDevice(IntPtr cap); 
	[DllImport ("MyUnityPlugin")]
	protected static extern IntPtr getDetectorAndPoseModel(string model_dat_path);
	[DllImport ("MyUnityPlugin")]
	protected static extern void detect(IntPtr cap, IntPtr dapm, int[] face_rect, int[,] matrix);
	[DllImport ("MyUnityPlugin")]
	protected static extern void setImgSize(IntPtr cap, int img_size_w, int img_size_h);

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
	#endif



	protected IntPtr cap_;
	protected IntPtr dapm_;
//	protected int frame_ctr_;
	protected const int NUM_OF_PARTS = 68;
	protected int[] face_rect_ = new int[4];
	protected int[,] detected_parts_matrix_ = new int[NUM_OF_PARTS,2];
	protected const int IMAGE_WIDTH = 720;
	protected const int IMAGE_HEIGHT = 480;
	protected const float BPX_INIT = 0;
	protected const float BPY_INIT = -1.3f;
	protected const float BPZ_INIT = 0.5f;
	protected const float HAX_INIT = -7.0f;
	protected const float HAY_INIT = -1.0f;
	protected const float HAZ_INIT = 1.5f;
	protected float MOUTH_OPEN_RATIO = 50;

	// smoother
//	protected const float ALPHA = 0.3f;
	protected const float ALPHA = 0.05f;
	protected const float GAMMA = 0.79f;
	IntPtr body_smoother_;	//3D
	IntPtr head_smoother_;	//3D
	IntPtr eye_close_smoother_;	//1D
	IntPtr mouth_open_smoother_;	//1D
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
	protected Vector3 LastBodyPos; // <- by me
	protected Vector3 HeadAng;
	protected Vector3 LastHeadAng; // <- by me
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
	protected float HeadAngZ = 200;
	protected int HeadAngSmoothWeight = 20;
	protected float EyesPosX = 0.8f;
	protected float EyesPosY = 0.2f;
	protected int EyesPosSmoothWeight = 5;
	protected int EyesCloseSmoothWeight = 3;
	protected int FaceSmoothWeight = 10;


	protected void Init () {
		string model_dat_path = "/usr/local/share/charwebcam/shape_predictor_68_face_landmarks.dat";
		int camera_num = 0;


//		if (Application.platform == RuntimePlatform.IPhonePlayer) {
//			Debug.Log ("ios");
//			model_dat_path = Application.dataPath + "/shape_predictor_68_face_landmarks.dat";
//			camera_num = 1;
//		} else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer) {
//			Debug.Log ("osx");
//			model_dat_path = Application.dataPath + "/../../shape_predictor_68_face_landmarks.dat";
//			if (Application.platform == RuntimePlatform.OSXEditor) {
//				model_dat_path = Application.dataPath + "/shape_predictor_68_face_landmarks.dat";
//			}
//			camera_num = 0;
//		} else {
//			Debug.Log ("else");
//			model_dat_path = Application.dataPath + "/shape_predictor_68_face_landmarks.dat";
//			camera_num = 0;
//		}

//		Debug.Log (camera_num);
		cap_ = getVideoDevice (camera_num);
		setImgSize (cap_, IMAGE_WIDTH/4, IMAGE_HEIGHT/4);

		dapm_ = getDetectorAndPoseModel (model_dat_path);
		for (int i=0; i<NUM_OF_PARTS; i++) {
			detected_parts_matrix_[i,0] = detected_parts_matrix_[i,1] = 0;
		}

		body_smoother_ = getSmoother3D(ALPHA, GAMMA);
		LastBodyPos = smooth3D (body_smoother_, new Vector3 (BPX_INIT, BPY_INIT, BPZ_INIT));

//		head_smoother_ = getSmoother3D(ALPHA-0.2f, GAMMA);
		head_smoother_ = getSmoother3D(ALPHA, GAMMA);
		LastHeadAng = smooth3D (head_smoother_, new Vector3 (HAX_INIT, HAY_INIT, HAZ_INIT));

		eye_close_smoother_ = getSmoother1D (ALPHA, GAMMA);
		mouth_open_smoother_ = getSmoother1D (ALPHA, GAMMA);
	}

	protected void UpdateParam(int[] face_rect, int[,] detected_parts_matrix) {
//		if (frame_ctr_++ % 4 != 0) {
//			return;
//		}
//		detect (cap_, dapm_, face_rect_, detected_parts_matrix_);

		// 体位置
		BodyPos = ClipBodyPos(smooth3D(body_smoother_, GetBodyPos(face_rect)));
//		BodyPos = smooth3D(body_smoother_, GetBodyPos(face_rect));
//		Debug.Log ("BodyPos: " + BodyPos);

		// 頭角度
		HeadAng = smooth3D (head_smoother_, GetHeadAng (detected_parts_matrix));
//		Debug.Log ("HeadAng: " + HeadAng);

		// 視線
		EyesPos = GetEyesPos(detected_parts_matrix);

		// 目パチ
//		float eyeL = FaceExp[FaceExpression.EXPRESSION_EYES_CLOSED_LEFT].intensity;
//		float eyeR = FaceExp[FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT].intensity;
//		EyesClose = SmoothEyesClose.SmoothValue(Mathf.Max(eyeL, eyeR));
//		Debug.Log("GetEyeOpenRatio: " + GetEyeOpenRatio(detected_parts_matrix));
		float eye_close_ratio = 100-GetEyeOpenRatio(detected_parts_matrix);
//		EyesClose = eye_close_ratio < 50 ? 0 : (eye_close_ratio - 50) * 2;
		EyesClose = eye_close_ratio < 30 ? eye_close_ratio : 50;
		EyesClose = smooth1D (eye_close_smoother_, EyesClose);

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

		MOUTH_OPEN_RATIO = smooth1D (mouth_open_smoother_, GetMouthOpenRatio (detected_parts_matrix));

		// 笑顔
//		Smile = 100;
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
	}

	virtual protected void OnDestroy() {
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
		float xMax = 2.0f*IMAGE_WIDTH;
		float yMax = 2.0f*IMAGE_HEIGHT;
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
		//		return new Vector3(-xPos, -1.3f, zPos);
		Vector3 calced_body_pos = new Vector3(-xPos, yPos, zPos);
		if (float.IsNaN (calced_body_pos.x) || float.IsNaN (calced_body_pos.y) || float.IsNaN (calced_body_pos.z)) {
			Debug.Log ("NaN detected");
			return LastBodyPos;
		} else {
			LastBodyPos = calced_body_pos;
			return calced_body_pos;
		}
	}

	/// <summary>
	/// 体位置をある範囲にクリッピング
	/// </summary>
	/// <param name="BodyPos">体位置</param>
	/// <returns>体位置</returns>
	Vector3 ClipBodyPos(Vector3 BodyPos)
	{
//		float clipped_y = BodyPos.y > -1.2f ? -1.2f : BodyPos.y < -1.4f ? -1.4f : BodyPos.y;
//		float clipped_z = BodyPos.z > 0.6f ? 0.6f : BodyPos.z < 0.4f ? 0.4f : BodyPos.z;
//		return new Vector3(BodyPos.x, clipped_y, clipped_z);
		return new Vector3(BodyPos.x-0.3f, BPY_INIT, BPZ_INIT);
	}

	/// <summary>
	/// 頭角度を取得
	/// </summary>
	/// <param name="detected_parts_matrix">顔器官検出結果</param>
	/// <returns>頭角度</returns>
	Vector3 GetHeadAng(int[,] detected_parts_matrix)
	{
		if (detected_parts_matrix.GetLength(0) != NUM_OF_PARTS) {
			Debug.Log ("detect value in head ang error");
		}
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

		Vector3 calced_head_ang = new Vector3(xAng, -yAng, zAng - 10);
		if (float.IsNaN (calced_head_ang.x) || float.IsNaN (calced_head_ang.y) || float.IsNaN (calced_head_ang.z)) {
			Debug.Log ("NaN detected");
			return LastHeadAng;
		} else {
			LastHeadAng = calced_head_ang;
			return calced_head_ang;
		}
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
		return new Vector2(0, 0);
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
//		Debug.Log("lTop: " + lTop);
		Vector2 lBottom = new Vector2((detected_parts_matrix[46,0] + detected_parts_matrix[47,0]) / 2, 
			Math.Max(detected_parts_matrix[46,1], detected_parts_matrix[47,1])); //24
//		Debug.Log("lBottom: " + lBottom);
		Vector2 lEye = new Vector2((lLeft.x + lRight.x) / 2, (lTop.y + lBottom.y) / 2); //77
//		Debug.Log("lEye: " + lEye);
		Vector2 rLeft = new Vector2(detected_parts_matrix[39,0], detected_parts_matrix[39,1]); //10
		Vector2 rRight = new Vector2(detected_parts_matrix[36,0], detected_parts_matrix[36,1]); //14
		Vector2 rTop = new Vector2((detected_parts_matrix[37,0] + detected_parts_matrix[38,0]) / 2, 
			Math.Min(detected_parts_matrix[37,1], detected_parts_matrix[38,1])); //12
		Vector2 rBottom = new Vector2((detected_parts_matrix[40,0] + detected_parts_matrix[41,0]) / 2, 
			Math.Max(detected_parts_matrix[40,1], detected_parts_matrix[41,1])); //16
		Vector2 rEye = new Vector2((rLeft.x + rRight.x) / 2, (rTop.y + rBottom.y) / 2); //76

		// 末尾で調整
		float tmp1, tmp2;
//		Debug.Log ("gcr: " + GetCenterRatio (lRight, lEye, lLeft));
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
		return Math.Abs((Vector2.Distance(v1, center) - Vector2.Distance(v2, center))) / Vector2.Distance(v1, v2);
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
		
	float GetEyeOpenRatio(int[,] detected_parts_matrix) {
		Vector2 lLeft = new Vector2(detected_parts_matrix[45,0], detected_parts_matrix[45,1]); //22
		Vector2 lRight = new Vector2(detected_parts_matrix[42,0], detected_parts_matrix[42,1]); //18
		Vector2 lTop = new Vector2((detected_parts_matrix[43,0] + detected_parts_matrix[44,0]) / 2, 
			Math.Min(detected_parts_matrix[43,1], detected_parts_matrix[43,1])); //20
		Vector2 lBottom = new Vector2((detected_parts_matrix[46,0] + detected_parts_matrix[47,0]) / 2, 
			Math.Max(detected_parts_matrix[46,1], detected_parts_matrix[47,1])); //24
		Vector2 rLeft = new Vector2(detected_parts_matrix[39,0], detected_parts_matrix[39,1]); //10
		Vector2 rRight = new Vector2(detected_parts_matrix[36,0], detected_parts_matrix[36,1]); //14
		Vector2 rTop = new Vector2((detected_parts_matrix[37,0] + detected_parts_matrix[38,0]) / 2, 
			Math.Min(detected_parts_matrix[37,1], detected_parts_matrix[38,1])); //12
		Vector2 rBottom = new Vector2((detected_parts_matrix[40,0] + detected_parts_matrix[41,0]) / 2, 
			Math.Max(detected_parts_matrix[40,1], detected_parts_matrix[41,1])); //16

		float lTate = Vector2.Distance (lTop, lBottom);
		float lYoko = Vector2.Distance (lRight, lLeft);
		float rTate = Vector2.Distance (rTop, rBottom);
		float rYoko = Vector2.Distance (rRight, rLeft);
		float lOpenRatio = 3 * lTate / lYoko;
		float rOpenRatio = 3 * rTate / rYoko;

		return Math.Max (lOpenRatio, rOpenRatio)*100;
	}

	float GetMouthOpenRatio(int[,] detected_parts_matrix) {
		Vector2 left = new Vector2(detected_parts_matrix[45,0], detected_parts_matrix[45,1]);
		Vector2 right = new Vector2(detected_parts_matrix[54,0], detected_parts_matrix[54,1]);
		Vector2 top = new Vector2(detected_parts_matrix[62,0], detected_parts_matrix[62,1]);
		Vector2 bottom = new Vector2 (detected_parts_matrix [66, 0], detected_parts_matrix [66, 1]);

		float OpenRatio = Vector2.Distance(top, bottom) / Vector2.Distance(left, right);
		return Math.Max(OpenRatio*100*2.5f-10, 0);
	}
}