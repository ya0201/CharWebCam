using UnityEngine;

public class MM_UnityChan : MouthMove
{
    public SkinnedMeshRenderer Mouth;
	private int frame_ctr_;
	private bool mouth_close_flag_;

    void Start()
    {
        Init();
		frame_ctr_ = 0;
		mouth_close_flag_ = false;
    }

    void Update()
    {
		if (frame_ctr_ % 10 == 0) {
			mouth_close_flag_ = !mouth_close_flag_;
		}

		if (mouth_close_flag_) {
			Mouth.SetBlendShapeWeight (6, 0);
		} else {
        	float vol = GetVolume();
//			Mouth.SetBlendShapeWeight(6, smooth1D(mouth_move_smoother_, (vol < 1 ? 0 : vol * 5)));
//			Mouth.SetBlendShapeWeight(6, (vol < 1 ? 0 : vol * 5));
			Mouth.SetBlendShapeWeight(6, (vol < 40 ? 0 : vol < 80 ? 50 : 100));
//			Mouth.SetBlendShapeWeight(6, (vol*5));
		}
    }
}
