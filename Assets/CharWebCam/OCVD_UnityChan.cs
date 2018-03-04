using UnityEngine;
using System.Threading;
using System.Collections.Concurrent;

public class OCVD_UnityChan : OCVD
{
    public GameObject Body;
    public GameObject Head;
    public MeshRenderer EyeL;
    public MeshRenderer EyeR;
    public SkinnedMeshRenderer BLW_DEF;
    public SkinnedMeshRenderer EYE_DEF;
    public SkinnedMeshRenderer EL_DEF;
//    public SkinnedMeshRenderer MTH_DEF;

	private Thread detect_thread_;
	private BlockingCollection<int[]> detected_face_collection_;
	private BlockingCollection<int[,]> detected_parts_collection_;
	private int frame_ctr_;

    void Start()
    {
        BodyPosYOffset = Body.transform.position.y;
        Init();

		detected_face_collection_ = new BlockingCollection<int[]>(new ConcurrentStack<int[]>());
		detected_parts_collection_ = new BlockingCollection<int[,]>(new ConcurrentStack<int[,]>());

		detect_thread_ = new Thread(doDetect);
		detect_thread_.Start();

		frame_ctr_ = 0;
    }

    void Update()
    {
		UpdateParam(detected_face_collection_.Take(), detected_parts_collection_.Take());
		Debug.Log (detected_parts_collection_.ToString());

        // 各パラメータ表示
        UpdateParamText();

		// yが-1.4~-1.2にあると都合が良さそう
		// zが0.4~0.6にあると都合が良さそう
        // 体移動
		#if UNITY_IOS
			Body.transform.position = new Vector3(BodyPos.x - 0.1f, BodyPos.y, BodyPos.z);
		#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX // OSX
			Body.transform.position = BodyPos;
		#endif
        

        // 頭向き
        Head.transform.localEulerAngles = new Vector3(HeadAng.x, HeadAng.y, HeadAng.z + 10);

        // 視線
        EyeL.material.SetTextureOffset("_MainTex", EyesPos * 0.5f);
        EyeR.material.SetTextureOffset("_MainTex", EyesPos * 0.5f);

        // 目パチ
        EYE_DEF.SetBlendShapeWeight(6, EyesClose);
        EL_DEF.SetBlendShapeWeight(6, EyesClose);
		EYE_DEF.SetBlendShapeWeight(0, EyesClose);
//		EYE_DEF.SetBlendShapeWeight(0, 40);
//		EYE_DEF.SetBlendShapeWeight(6, 60);

        // 眉上
        BLW_DEF.SetBlendShapeWeight(2, BrowRai);
        EYE_DEF.SetBlendShapeWeight(2, BrowRai);
        EL_DEF.SetBlendShapeWeight(2, BrowRai);

        // 眉下
        BLW_DEF.SetBlendShapeWeight(3, BrowLow);
        EYE_DEF.SetBlendShapeWeight(3, BrowLow);
        EL_DEF.SetBlendShapeWeight(3, BrowLow);
//        MTH_DEF.SetBlendShapeWeight(3, BrowLow);

//		MTH_DEF.SetBlendShapeWeight(0, MOUTH_OPEN_RATIO);
//		MTH_DEF.SetBlendShapeWeight(5, MOUTH_OPEN_RATIO);

//        // 笑顔
//        BLW_DEF.SetBlendShapeWeight(0, Smile);
//        EYE_DEF.SetBlendShapeWeight(0, Smile);
//        EL_DEF.SetBlendShapeWeight(0, Smile);
//        MTH_DEF.SetBlendShapeWeight(6, Smile);
//
//        // キス
//        BLW_DEF.SetBlendShapeWeight(4, Kiss);
//        EYE_DEF.SetBlendShapeWeight(4, Kiss);
//        EL_DEF.SetBlendShapeWeight(4, Kiss);
//        MTH_DEF.SetBlendShapeWeight(4, Kiss);
//
//        // 表情競合対策
//        if (Smile > 10)
//        {
//            BLW_DEF.SetBlendShapeWeight(3, 0);
//            EYE_DEF.SetBlendShapeWeight(6, 0);
//            EL_DEF.SetBlendShapeWeight(6, 0);
//        }
//        if (Kiss > 10)
//        {
//            BLW_DEF.SetBlendShapeWeight(3, 0);
//            EYE_DEF.SetBlendShapeWeight(3, 0);
//            EL_DEF.SetBlendShapeWeight(3, 0);
//            MTH_DEF.SetBlendShapeWeight(3, 0);
//        }
    }

	private void doDetect()
	{
		while (!detected_face_collection_.IsAddingCompleted && !detected_parts_collection_.IsAddingCompleted) {
			int[] detected_face = new int[4];
			int[,] detected_parts = new int[NUM_OF_PARTS, 2];
			for (int i=0; i<NUM_OF_PARTS; i++) {
				detected_parts[i,0] = detected_parts[i,1] = 0;
			}
			detect (cap_, dapm_, detected_face, detected_parts);
			detected_face_collection_.Add (detected_face);
			detected_parts_collection_.Add (detected_parts);
		}
	}

	protected override void OnDestroy() {
		if (detect_thread_ != null) {
			detected_face_collection_.CompleteAdding();
			detected_parts_collection_.CompleteAdding();
			detect_thread_.Join();

			detected_face_collection_.Dispose ();
			detected_parts_collection_.Dispose ();
			detect_thread_ = null;
		}
		base.OnDestroy ();
	}
}
