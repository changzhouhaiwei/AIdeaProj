using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public enum GuideMaskType
{
    None,
    Circle,
    Rect,
}

[RequireComponent(typeof(CircleGuide))]
[RequireComponent(typeof(RectGuide))]
public class GuideController : MonoBehaviour, ICanvasRaycastFilter
{
    private Canvas moduleCanvas;

    private CircleGuide mCircleGuide;
    private RectGuide mRectGuide;

    // private GuideConfig guideCfg;



    private Image mMaskImg;
    private RectTransform mTarget;
    private bool mIsShowMask;

    private void Awake()
    {
        mMaskImg = transform.GetComponent<Image>();
        if (mMaskImg == null)
        {
            throw new System.Exception("mask初始化失败");
        }

        moduleCanvas = GameModule.UI.UICanvas;
        mCircleGuide = transform.GetComponent<CircleGuide>();
        mRectGuide = transform.GetComponent<RectGuide>();
    }

    public void SetModuleCanvas(Canvas canvas)
    {
        moduleCanvas = canvas;
    }

    // public void GetModuleCanvas(Canvas canvas)
    // {
    //     return moduleCanvas ?? GameModule.UI.UICanvas;
    // }

    public void ResetMaskImage()
    {
        mMaskImg.color = new Color(0, 0, 0, 0);
        mMaskImg.material = null;
    }

    // public void Guide(RectTransform target, GuideMaskType maskType,GuideConfig _cfg,bool isShowMask = false, float offsetX = 0f, float offsetY = 0f)
    // {
    //     guideCfg = _cfg;
    //     mTarget = target;
    //     mIsShowMask = isShowMask;
    //     if (mIsShowMask)
    //     {
    //         mMaskImg.color = new Color(0, 0, 0, 0.5f);
    //     }
    //     else
    //     {
    //         mMaskImg.color = new Color(0, 0, 0, 0);
    //     }

    //     // var canvas = GameUtility.FindInParents<Canvas>(mTarget.gameObject);
    //     switch (maskType)
    //     {
    //         case GuideMaskType.Circle:
    //             mMaskImg.material = mCircleGuide.MaskMaterial;
    //             float val = 0f;
    //             if(guideCfg.Offset.Count>0)
    //             {   
    //                 val =  guideCfg.Offset[1];
    //             }
    //             mCircleGuide.Guide(moduleCanvas, mTarget, guideCfg.Offset[1]);
    //             break;
    //         case GuideMaskType.Rect:
    //             mMaskImg.material = mRectGuide.MaskMaterial;

    //             float valuex = guideCfg.Offset.Count > 0 ? guideCfg.Offset[0] : 0;
    //             float valuey = guideCfg.Offset.Count > 1 ? guideCfg.Offset[1] : 0;
    //             mRectGuide.Guide(moduleCanvas, mTarget,0,0);

    //             break;
    //         // case GuideMaskType.
    //     }
    // }

    public void PlayGuideAnimation(GuideMaskType maskType)
    {
        mMaskImg.color = new Color(0, 0, 0, 0.5f);
        switch (maskType)
        {
            case GuideMaskType.Circle:
                mCircleGuide.PlayGuideAnimation();
                break;
            case GuideMaskType.Rect:
                mRectGuide.PlayGuideAnimation();
                break;
        }
    }

    // 在方法IsRaycastLocationValid中判断当前点击的位置是否符合响应事件的条件 return true事件不能渗透 false能渗透
    // RectTransformUtility.RectangleContainsScreenPoint(target, sp) 矩形区域包不包含鼠标点击的点
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        // if (mTarget == null)
        // {
        //     return true;
        // }

        // var guideCam = GuideManager.Instance.GetGuideCamera();
        // var b =  !RectTransformUtility.RectangleContainsScreenPoint(mTarget, sp, guideCam);

        // return b;
        return false;
    }



}