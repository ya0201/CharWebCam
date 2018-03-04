using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;

public class MouthMove : MonoBehaviour
{
	[DllImport ("MySmoother")]
	protected static extern IntPtr getSmoother1D(float alpha, float gamma);
	[DllImport ("MySmoother")]
	protected static extern void deleteSmoother1D(IntPtr smoother);
	[DllImport ("MySmoother")]
	protected static extern float smooth1D(IntPtr smoother, float f);

    // 音声入力デバイス選択表示
    public Text Text;

	protected const float ALPHA = 0.5f;
	protected const float GAMMA = 0.79f;
	protected IntPtr mouth_move_smoother_;	//1D

    /// <summary>
    /// 音声入力デバイス選択待機
    /// </summary>
    protected void Init()
    {
        StartCoroutine("SelectMicrophone");
		mouth_move_smoother_ = getSmoother1D (ALPHA, GAMMA);
    }

    /// <summary>
    /// 音声入力デバイスの一覧表示と選択
    /// </summary>
    /// <remarks>
    /// https://docs.unity3d.com/jp/540/Manual/Coroutines.html
    /// https://docs.unity3d.com/ja/540/ScriptReference/Microphone-devices.html
    /// https://docs.unity3d.com/ja/540/ScriptReference/Microphone.Start.html
    /// </remarks>
    IEnumerator SelectMicrophone()
    {
        // 一覧表示
        Text.text = "Device to move mouth.\n\n";
        for (int i = 0; i < Microphone.devices.Length; i++)
        {
            Text.text += "[" + i + "]" + Microphone.devices[i] + "\n";
        }
        Text.text += "\nPlease select with number key.";

        // 選択待機
        while (true)
        {
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                if (Input.GetKey(KeyCode.Alpha0 + i))
                {
                    // 録音開始
                    AudioSource audio = GetComponent<AudioSource>();
                    audio.clip = Microphone.Start(Microphone.devices[i], true, 10, 44100);
                    Text.text = "";
                }
            }
            yield return null;
        }
    }

    /// <summary>
    /// 音量取得
    /// </summary>
    /// <returns>音量</returns>
    /// <remarks>
    /// https://docs.unity3d.com/jp/540/ScriptReference/AudioClip.GetData.html
    /// </remarks>
    protected float GetVolume()
    {
        // 録音が開始されていなければ中断
        AudioSource audio = GetComponent<AudioSource>();
        if (audio.clip == null)
        {
            return 0;
        }

        // 入力音量取得
        float[] samples = new float[audio.clip.samples * audio.clip.channels];
        audio.clip.GetData(samples, 0);
        float vol = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            vol += Mathf.Abs(samples[i]);
            samples[i] = samples[i] * 0.5F;
        }
        audio.clip.SetData(samples, 0);

        return vol;
    }

	virtual protected void OnDestroy() {
		deleteSmoother1D (mouth_move_smoother_);
	}
}