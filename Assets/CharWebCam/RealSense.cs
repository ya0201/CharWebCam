﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Intel.RealSense;
using Intel.RealSense.Face;
using Intel.RealSense.Utility;

public class RealSense : MonoBehaviour
{
    // 例外表示
    public Text Text;
    public GameObject Body;

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
    protected int FaceSmoothWeight = 3;

    // 検出値取得
    protected bool Ready = false;

    // 検出値
    ImageInfo Resolution;
    RectI32 FaceRect;
    LandmarkPoint[] Landmark;
    Dictionary<FaceExpression, FaceExpressionResult> FaceExp;

    // 平滑化
    Smoother Smoother;
    Smoother3D SmoothBody;
    Smoother3D SmoothHead;
    Smoother2D SmoothEyes;
    Smoother1D SmoothEyesClose;
    Smoother1D SmoothBrowRai;
    Smoother1D SmoothBrowLow;
    Smoother1D SmoothSmile;
    Smoother1D SmoothKiss;
    Smoother1D SmoothMouth;
    Smoother1D SmoothTongue;

    // RealSense
    SenseManager SenseManager;
    FaceModule FaceModule;
    FaceData FaceData;
    FaceConfiguration FaceConfig;

    protected void Init()
    {
        // 初期表示位置(オフセット)の保持
        BodyPosYOffset = Body.transform.position.y; ;

        try
        {
            // RealSense初期化
            // 参考：https://software.intel.com/sites/landingpage/realsense/camera-sdk/v2016r3/documentation/html/index.html?doc_face_general_procedure.html
            SenseManager = SenseManager.CreateInstance();

            FaceModule = FaceModule.Activate(SenseManager);
            FaceModule.FrameProcessed += FaceModule_FrameProcessed;
            FaceData = FaceModule.CreateOutput();

            FaceConfig = FaceModule.CreateActiveConfiguration();
            FaceConfig.TrackingMode = TrackingModeType.FACE_MODE_COLOR;
            FaceConfig.Expressions.Properties.Enabled = true;
            FaceConfig.ApplyChanges();

            SenseManager.Init();
            SenseManager.StreamFrames(false);

            // 解像度取得
            StreamProfileSet profile;
            SenseManager.CaptureManager.Device.QueryStreamProfileSet(out profile);
            Resolution = profile.color.imageInfo;

            // 平滑化初期化
            // 参考：https://software.intel.com/sites/landingpage/realsense/camera-sdk/v2016r3/documentation/html/index.html?doc_utils_the_smoother_utility.html
            Smoother = Smoother.CreateInstance(SenseManager.Session);

            SmoothBody = Smoother.Create3DWeighted(BodyPosSmoothWeight);
            SmoothHead = Smoother.Create3DWeighted(HeadAngSmoothWeight);
            SmoothEyes = Smoother.Create2DWeighted(EyesPosSmoothWeight);
            SmoothEyesClose = Smoother.Create1DWeighted(EyesCloseSmoothWeight);
            SmoothBrowRai = Smoother.Create1DWeighted(FaceSmoothWeight);
            SmoothBrowLow = Smoother.Create1DWeighted(FaceSmoothWeight);
            SmoothSmile = Smoother.Create1DWeighted(FaceSmoothWeight);
            SmoothKiss = Smoother.Create1DWeighted(FaceSmoothWeight);
            SmoothMouth = Smoother.Create1DWeighted(FaceSmoothWeight);
            SmoothTongue = Smoother.Create1DWeighted(FaceSmoothWeight);
        }
        catch (Exception e)
        {
            Text.text = "RealSense Error\n";
            Text.text += e.Message;
        }
    }

    /// <summary>
    /// 検出値を取得
    /// </summary>
    void FaceModule_FrameProcessed(object sender, FrameProcessedEventArgs args)
    {
        FaceData.Update();
        var face = FaceData.QueryFaceByIndex(0);
        if (face != null)
        {
            // 検出値
            FaceRect = face.Detection.BoundingRect;
            Landmark = face.Landmarks.Points;
            FaceExp = face.Expressions.ExpressionResults;

            // 体位置
            BodyPos = SmoothBody.SmoothValue(GetBodyPos(FaceRect));

            // 頭角度
            HeadAng = SmoothHead.SmoothValue(GetHeadAng(Landmark));

            // 視線
            EyesPos = SmoothEyes.SmoothValue(GetEyesPos(Landmark));

            // 目パチ
            float eyeL = FaceExp[FaceExpression.EXPRESSION_EYES_CLOSED_LEFT].intensity;
            float eyeR = FaceExp[FaceExpression.EXPRESSION_EYES_CLOSED_RIGHT].intensity;
            EyesClose = SmoothEyesClose.SmoothValue(Mathf.Max(eyeL, eyeR));

            // 眉上
            float browLowL = FaceExp[FaceExpression.EXPRESSION_BROW_LOWERER_LEFT].intensity;
            float browLowR = FaceExp[FaceExpression.EXPRESSION_BROW_LOWERER_RIGHT].intensity;
            BrowLow = SmoothBrowLow.SmoothValue(Mathf.Max(browLowL, browLowR));

            // 眉下
            float browRaiL = FaceExp[FaceExpression.EXPRESSION_BROW_RAISER_LEFT].intensity;
            float browRaiR = FaceExp[FaceExpression.EXPRESSION_BROW_RAISER_RIGHT].intensity;
            BrowRai = SmoothBrowRai.SmoothValue(Mathf.Max(browRaiL, browRaiR));

            // 笑顔
            Smile = SmoothSmile.SmoothValue(FaceExp[FaceExpression.EXPRESSION_SMILE].intensity);

            // キス(口開と若干競合)
            Kiss = SmoothKiss.SmoothValue(FaceExp[FaceExpression.EXPRESSION_KISS].intensity);

            // 口開(キスと若干競合)
            Mouth = SmoothMouth.SmoothValue(FaceExp[FaceExpression.EXPRESSION_MOUTH_OPEN].intensity);

            // べー(口開と競合)
            Tongue = SmoothTongue.SmoothValue(FaceExp[FaceExpression.EXPRESSION_TONGUE_OUT].intensity);

            Ready = true;
        }
    }

    /// <summary>
    /// 体位置を取得
    /// </summary>
    /// <param name="rect">顔の矩形</param>
    /// <returns>体位置</returns>
    /// <remarks>
    /// https://software.intel.com/sites/landingpage/realsense/camera-sdk/v2016r3/documentation/html/index.html?doc_face_face_location_data.html
    /// </remarks>
    Vector3 GetBodyPos(RectI32 rect)
    {
        // 体位置に利用するため頭位置を取得
        float xMax = Resolution.width;
        float yMax = Resolution.height;
        float xPos = rect.x + (rect.w / 2);
        float yPos = rect.h + (rect.h / 2);
        float zPos = (yMax - rect.h);

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
    /// <param name="points">顔の検出点</param>
    /// <returns>頭角度</returns>
    /// <remarks>
    /// https://software.intel.com/sites/landingpage/realsense/camera-sdk/v2016r3/documentation/html/index.html?doc_face_face_landmark_data.html
    /// </remarks>
    Vector3 GetHeadAng(LandmarkPoint[] points)
    {
        // 頭向きに利用するため顔の中心と左右端、唇下、顎下を取得
        Vector2 center = points[29].image;
        Vector2 left = points[68].image;
        Vector2 right = points[54].image;
        Vector2 mouth = points[42].image;
        Vector2 chin = points[61].image;

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
    /// <param name="points">顔の検出点</param>
    /// <returns>瞳位置</returns>
    /// <remarks>
    /// https://software.intel.com/sites/landingpage/realsense/camera-sdk/v2016r3/documentation/html/index.html?doc_face_face_landmark_data.html
    /// </remarks>
    Vector2 GetEyesPos(LandmarkPoint[] points)
    {
        // 左右の目の瞳と上下左右端を取得
        Vector2 lEye = points[77].image;
        Vector2 lLeft = points[22].image;
        Vector2 lRight = points[18].image;
        Vector2 lTop = points[20].image;
        Vector2 lBottom = points[24].image;
        Vector2 rEye = points[76].image;
        Vector2 rLeft = points[10].image;
        Vector2 rRight = points[14].image;
        Vector2 rTop = points[12].image;
        Vector2 rBottom = points[16].image;

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
    /// 2点感の角度を求める
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

    void OnDestroy()
    {
        // 平滑化開放
        SmoothTongue.Dispose();
        SmoothMouth.Dispose();
        SmoothKiss.Dispose();
        SmoothSmile.Dispose();
        SmoothBrowLow.Dispose();
        SmoothBrowRai.Dispose();
        SmoothEyesClose.Dispose();
        SmoothEyes.Dispose();
        SmoothHead.Dispose();
        SmoothBody.Dispose();
        Smoother.Dispose();

        // RealSense開放
        FaceModule.FrameProcessed -= FaceModule_FrameProcessed;
        FaceConfig.Dispose();
        FaceData.Dispose();
        FaceModule.Dispose();
        SenseManager.Dispose();
    }
}