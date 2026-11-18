using System;
using DG.Tweening;
using FishFramework;
using FishTilePlay;
using UnityEngine;
using UnityEngine.UI;

public class UIGoldTreeFirstTips : UIGoldTreeFirstTipsBase
{
    const float OpenPopDuration = 0.6f;
    const float OpenSettleDuration = 0.08f;
    const float CloseShrinkDuration = 0.25f;

    /// <summary>点击关闭后执行（替代本关 TipsDlg 的 afterDoFunc）。</summary>
    public Action ClickAction;

    RectTransform mAnimRoot;
    Sequence mAnimSequence;
    bool mRaycastDisabledBySelf;

    protected override void OnInternalCreated()
    {
        base.OnInternalCreated();
        mAnimRoot = rectTransform.Find("content") as RectTransform;
    }

    protected override void PlayOpenAnim(Action onFinish = null)
    {
        animState = UIPanelAnimState.Opening;
        if (mAnimRoot == null)
        {
            animState = UIPanelAnimState.Idle;
            onFinish?.Invoke();
            return;
        }

        mAnimRoot.localScale = Vector3.zero;
        RaycastController.Disable();
        mRaycastDisabledBySelf = true;

        mAnimSequence?.Kill();
        mAnimSequence = DOTween.Sequence();
        mAnimSequence.Append(mAnimRoot.DOScale(1.05f, OpenPopDuration).SetEase(Ease.OutBack));
        mAnimSequence.Append(mAnimRoot.DOScale(1f, OpenSettleDuration));
        mAnimSequence.SetUpdate(true);
        mAnimSequence.SetLink(gameObject);
        mAnimSequence.OnComplete(() =>
        {
            animState = UIPanelAnimState.Idle;
            onFinish?.Invoke();
        });
    }

    protected override void PlayCloseAnim(Action onFinish = null)
    {
        animState = UIPanelAnimState.Closing;
        if (mAnimRoot == null)
        {
            FinishClose(onFinish);
            return;
        }

        mAnimSequence?.Kill();
        mAnimSequence = DOTween.Sequence();
        mAnimSequence.Append(mAnimRoot.DOScale(0.6f, CloseShrinkDuration).SetEase(Ease.InBack));
        mAnimSequence.SetUpdate(true);
        mAnimSequence.SetLink(gameObject);
        mAnimSequence.OnComplete(() => FinishClose(onFinish));
    }

    void FinishClose(Action onFinish)
    {
        animState = UIPanelAnimState.Closed;
        RestoreRaycastIfNeeded();
        onFinish?.Invoke();
    }

    void RestoreRaycastIfNeeded()
    {
        if (!mRaycastDisabledBySelf)
        {
            return;
        }

        RaycastController.Enable();
        mRaycastDisabledBySelf = false;
    }

    protected override void OnClicked(Button button)
    {
        if (button != backBtn)
        {
            return;
        }

        if (showState != UIPanelShowState.Idle || animState != UIPanelAnimState.Idle)
        {
            return;
        }

        var clickAction = ClickAction;
        ClickAction = null;
        CloseSelf(() => clickAction?.Invoke(), willExcuteExtraAct: false);
    }

    protected override void OnDestroyed()
    {
        mAnimSequence?.Kill();
        RestoreRaycastIfNeeded();
        ClickAction = null;
        base.OnDestroyed();
    }
}
