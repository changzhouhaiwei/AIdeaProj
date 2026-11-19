using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FishFramework;
using XSortGamePlay;

public class RewardShowDlg : GPViewBase
{
    private const float HorizontalPadding = 20f;
    private const float ArrowEdgePadding = 16f;
    private const float AwardBgVerticalOffset = 80f;
    private const float ShowScaleAnimDuration = 0.25f;
    private static readonly Vector2 BgBottomPivot = new(0.5f, 0f);
    private static readonly Vector3[] sCorners = new Vector3[4];

    private RectTransform bgRect;
    private RectTransform mLayoutContainerRect;
    private RectTransform mArrowLeftRect;
    private RectTransform mArrowMiddleRect;
    private RectTransform mArrowRightRect;
    private readonly List<GameObject> awardCellList = new();
    private GameObject mAwardCellTemplate;
    private Transform mRewardWrapperParent;
    private Button mEmptyButton;
    private RectTransform mRootRect;
    private Vector3 mDefaultRootLocalPosition;
    private Vector3 mDefaultBgLocalPosition;
    private Vector3 mDefaultBgLocalScale = Vector3.one;
    private bool mBgBottomPivotApplied;
    private Tween mShowScaleTween;
    private Transform mOriginalParent;
    private int mOriginalSiblingIndex;
    private Vector2 mOriginalAnchorMin;
    private Vector2 mOriginalAnchorMax;
    private Vector2 mOriginalAnchoredPosition;
    private Vector2 mOriginalSizeDelta;
    private Vector3 mOriginalLocalScale;
    private bool mOverlayLayoutApplied;
    private int mSuppressDismissFrame = -1;
    private Func<Vector2, bool> mOverlayClickHandler;
    private bool mEmptyButtonEventBound;
    private Vector2 mLastPointerScreenPosition;
    private Vector2 mDefaultArrowLeftPos;
    private Vector2 mDefaultArrowMiddlePos;
    private Vector2 mDefaultArrowRightPos;
    private bool mDefaultArrowPosCached;

    public bool IsVisible => gameObject.activeSelf;

    public void SetOverlayClickHandler(Func<Vector2, bool> handler)
    {
        mOverlayClickHandler = handler;
    }

    public bool IsAwardContent(GameObject go)
    {
        if (go == null || bgRect == null)
        {
            return false;
        }

        Transform target = go.transform;
        return target == bgRect || target.IsChildOf(bgRect);
    }

    private void Awake()
    {
        ResolveReferences();
        BindButton();
    }

    private void OnDestroy()
    {
        CancelShowScaleAnimation();
        UnbindButton();
    }

    public void Show(RectTransform anchorRect, List<ActivityData.DailyWinRewardTipData> rewardDataList)
    {
        ResolveReferences();
        BindButton();
        if (anchorRect == null || rewardDataList == null || rewardDataList.Count == 0)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);
        ApplyOverlayLayout();
        mSuppressDismissFrame = Time.frameCount;
        ShowAwardsData(rewardDataList);
        Canvas.ForceUpdateCanvases();
        if (mLayoutContainerRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(mLayoutContainerRect);
        }

        if (bgRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(bgRect);
        }

        ShowTipsTran(anchorRect);
        PlayShowScaleAnimation();
    }

    public void Hide()
    {
        CancelShowScaleAnimation();
        ResetBgScale();
        RestoreOverlayLayout();
        gameObject.SetActive(false);
    }

    /// <summary>Prefab 兼容入口，不使用 UnityEngine.Input。</summary>
    public void OnEmptyButtonClick()
    {
        HandleOverlayDismiss(mLastPointerScreenPosition);
    }

    private void OnEmptyButtonPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        mLastPointerScreenPosition = eventData.position;
        HandleOverlayDismiss(eventData.position);
    }

    private void HandleOverlayDismiss(Vector2 screenPosition)
    {
        if (Time.frameCount <= mSuppressDismissFrame)
        {
            return;
        }

        if (mOverlayClickHandler != null && mOverlayClickHandler.Invoke(screenPosition))
        {
            return;
        }

        Hide();
    }

    private void ResolveReferences()
    {
        if (mRootRect == null)
        {
            mRootRect = transform as RectTransform;
            if (mRootRect != null)
            {
                mDefaultRootLocalPosition = mRootRect.localPosition;
            }
        }

        if (bgRect == null)
        {
            bgRect = transform.Find("awardBg") as RectTransform;
            if (bgRect != null)
            {
                mDefaultBgLocalScale = bgRect.localScale;
                EnsureBgBottomPivot();
                mDefaultBgLocalPosition = bgRect.localPosition;
            }
        }

        if (mEmptyButton == null)
        {
            mEmptyButton = transform.Find("EmptyButton")?.GetComponent<Button>();
        }

        if (bgRect != null)
        {
            if (mLayoutContainerRect == null)
            {
                mLayoutContainerRect = bgRect.Find("WW") as RectTransform;
                mLayoutContainerRect ??= bgRect.Find("hNode") as RectTransform;
            }
        }

        if (mLayoutContainerRect != null)
        {
            mRewardWrapperParent ??= mLayoutContainerRect;
            mArrowLeftRect ??= mLayoutContainerRect.Find("arrowL") as RectTransform;
            mArrowMiddleRect ??= mLayoutContainerRect.Find("arrowM") as RectTransform;
            mArrowRightRect ??= mLayoutContainerRect.Find("arrowR") as RectTransform;
            mArrowRightRect ??= mLayoutContainerRect.Find("arrowdR") as RectTransform;
            CacheDefaultArrowPositions();
        }

        if (awardCellList.Count == 0 && mRewardWrapperParent != null)
        {
            var cellNodes = new List<Transform>();
            for (int i = 0; i < mRewardWrapperParent.childCount; i++)
            {
                Transform child = mRewardWrapperParent.GetChild(i);
                if (!IsRewardWrapperNode(child))
                {
                    continue;
                }

                cellNodes.Add(child);
            }

            cellNodes.Sort((a, b) => GetRewardCellOrder(a.name).CompareTo(GetRewardCellOrder(b.name)));
            foreach (Transform child in cellNodes)
            {
                awardCellList.Add(child.gameObject);
            }

            if (awardCellList.Count > 0)
            {
                mAwardCellTemplate = awardCellList[0];
            }
        }
    }

    private void BindButton()
    {
        if (mEmptyButton == null || mEmptyButtonEventBound)
        {
            return;
        }

        mEmptyButtonEventBound = true;
        mEmptyButton.onClick.RemoveAllListeners();

        EventTrigger trigger = mEmptyButton.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = mEmptyButton.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.RemoveAll(entry =>
            entry.eventID == EventTriggerType.PointerDown || entry.eventID == EventTriggerType.PointerClick);

        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerDown,
        };
        pointerDownEntry.callback.AddListener(data =>
        {
            if (data is PointerEventData pointerData)
            {
                mLastPointerScreenPosition = pointerData.position;
            }
        });
        trigger.triggers.Add(pointerDownEntry);

        EventTrigger.Entry pointerClickEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick,
        };
        pointerClickEntry.callback.AddListener(data => OnEmptyButtonPointerClick((PointerEventData)data));
        trigger.triggers.Add(pointerClickEntry);

        // 禁用 Button，避免 prefab 上残留的 onClick 走旧逻辑；点击由 EventTrigger 处理。
        mEmptyButton.enabled = false;
    }

    private void UnbindButton()
    {
        mEmptyButtonEventBound = false;
        if (mEmptyButton == null)
        {
            return;
        }

        mEmptyButton.onClick.RemoveAllListeners();
        mEmptyButton.enabled = true;
    }

    private void ShowAwardsData(List<ActivityData.DailyWinRewardTipData> rewardDataList)
    {
        // cell1：头像框专用；道具一律从 cell2 起排。无头像框时隐藏 cell1。
        bool hasAvatarFrame = rewardDataList.Count > 0 && rewardDataList[0].IsAvatarFrame;
        int itemStartIndex = hasAvatarFrame ? 1 : 0;
        int itemCount = Mathf.Max(0, rewardDataList.Count - itemStartIndex);
        // cell1 + 道具格；扩容用 cell2 作模板，避免复制头像框 cell1
        int requiredCellCount = 1 + itemCount;
        EnsureAwardCellCount(requiredCellCount);

        for (int i = 0; i < awardCellList.Count; i++)
        {
            awardCellList[i].SetActive(false);
        }

        if (hasAvatarFrame && awardCellList.Count > 0)
        {
            GameObject frameCell = awardCellList[0];
            frameCell.SetActive(true);
            ShowRewardData(frameCell, rewardDataList[0]);
        }

        for (int i = 0; i < itemCount; i++)
        {
            int cellIndex = i + 1; // cell2 起
            if (cellIndex >= awardCellList.Count)
            {
                break;
            }

            GameObject cellObj = awardCellList[cellIndex];
            cellObj.SetActive(true);
            ShowRewardData(cellObj, rewardDataList[itemStartIndex + i]);
        }
    }

    private void EnsureAwardCellCount(int requiredCount)
    {
        if (requiredCount <= awardCellList.Count || mRewardWrapperParent == null)
        {
            return;
        }

        // 优先用 cell2 作为普通道具模板；没有则回退 cell1
        GameObject template = mAwardCellTemplate;
        if (awardCellList.Count > 1)
        {
            template = awardCellList[1];
        }

        if (template == null)
        {
            return;
        }

        while (awardCellList.Count < requiredCount)
        {
            GameObject newCell = Instantiate(template, mRewardWrapperParent);
            newCell.name = $"cell{awardCellList.Count + 1}";
            awardCellList.Add(newCell);
        }
    }

    private static TextMeshProUGUI GetRewardCountText(
        GameObject cell,
        ActivityData.DailyWinRewardTipData rewardData)
    {
        if (cell == null)
        {
            return null;
        }

        if (rewardData != null && rewardData.IsAvatarFrame)
        {
            var streakText = cell.transform.Find("node/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (streakText != null)
            {
                return streakText;
            }
        }

        return cell.GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// RankAwardCellAvatarFrame 预制体（含 UserFrameNodeFixed / TotalWinCountfixed）全部用预制体固定展示，运行时不改。
    /// </summary>
    private static bool IsFixedRankAwardAvatarFrameCell(GameObject cell)
    {
        if (cell == null)
        {
            return false;
        }

        if (cell.name.Contains("RankAwardCellAvatarFrame"))
        {
            return true;
        }

        foreach (var child in cell.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "RankAwardCellAvatarFrame"
                || child.name == "UserFrameNodeFixed"
                || child.name == "TotalWinCountfixed"
                || child.name == "TotalWinCountFixed")
            {
                return true;
            }
        }

        return false;
    }

    private void ShowRewardData(GameObject cell, ActivityData.DailyWinRewardTipData rewardData)
    {
        if (cell == null || rewardData == null)
        {
            return;
        }

        // 固定头像框 cell：frame / TotalWinCountfixed 等均保持预制体原样
        if (rewardData.IsAvatarFrame && IsFixedRankAwardAvatarFrameCell(cell))
        {
            return;
        }

        TextMeshProUGUI text = GetRewardCountText(cell, rewardData);
        Image icon = cell.GetComponentInChildren<Image>();
        if (text != null)
        {
            text.text = rewardData.CountText;
        }

        if (icon == null)
        {
            return;
        }

        Sprite rewardSprite = rewardData.UseAtlasSprite
            ? LoadAtlasSprite(rewardData.SpriteDirectory, rewardData.SpriteName)
            : LoadSprite(rewardData.SpriteDirectory, rewardData.SpriteName);

        if (rewardSprite == null)
        {
            rewardSprite = rewardData.UseAtlasSprite
                ? LoadSprite(rewardData.SpriteDirectory, rewardData.SpriteName)
                : LoadAtlasSprite(rewardData.SpriteDirectory, rewardData.SpriteName);
        }

        if (rewardSprite == null && !string.IsNullOrEmpty(rewardData.FallbackSpriteDirectory) && !string.IsNullOrEmpty(rewardData.FallbackSpriteName))
        {
            rewardSprite = LoadSprite(rewardData.FallbackSpriteDirectory, rewardData.FallbackSpriteName);
        }

        if (rewardSprite != null)
        {
            icon.sprite = rewardSprite;
        }
    }

    private void ApplyOverlayLayout()
    {
        if (mRootRect == null || mOverlayLayoutApplied)
        {
            return;
        }

        mOriginalParent = mRootRect.parent;
        mOriginalSiblingIndex = mRootRect.GetSiblingIndex();
        mOriginalAnchorMin = mRootRect.anchorMin;
        mOriginalAnchorMax = mRootRect.anchorMax;
        mOriginalAnchoredPosition = mRootRect.anchoredPosition;
        mOriginalSizeDelta = mRootRect.sizeDelta;
        mOriginalLocalScale = mRootRect.localScale;

        Transform overlayParent = GetOverlayParent();
        if (overlayParent != null)
        {
            mRootRect.SetParent(overlayParent, false);
            mRootRect.SetAsLastSibling();
        }

        mRootRect.anchorMin = Vector2.zero;
        mRootRect.anchorMax = Vector2.one;
        mRootRect.offsetMin = Vector2.zero;
        mRootRect.offsetMax = Vector2.zero;
        mRootRect.localScale = Vector3.one;
        mOverlayLayoutApplied = true;
    }

    private void RestoreOverlayLayout()
    {
        if (!mOverlayLayoutApplied || mRootRect == null)
        {
            return;
        }

        mRootRect.SetParent(mOriginalParent, false);
        mRootRect.SetSiblingIndex(mOriginalSiblingIndex);
        mRootRect.anchorMin = mOriginalAnchorMin;
        mRootRect.anchorMax = mOriginalAnchorMax;
        mRootRect.anchoredPosition = mOriginalAnchoredPosition;
        mRootRect.sizeDelta = mOriginalSizeDelta;
        mRootRect.localScale = mOriginalLocalScale;
        mOverlayLayoutApplied = false;
    }

    private Transform GetOverlayParent()
    {
        Transform current = mRootRect != null ? mRootRect.parent : null;
        while (current != null)
        {
            if (current.GetComponent<UIPanelBehaviour>() != null)
            {
                return current;
            }

            current = current.parent;
        }

        return mOriginalParent;
    }

    private void ShowTipsTran(RectTransform targetRect)
    {
        if (targetRect == null || bgRect == null || mRootRect == null)
        {
            return;
        }

        Canvas tipsCanvas = mRootRect.GetComponentInParent<Canvas>();
        RectTransform parentRect = mRootRect.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        Camera uiCam = tipsCanvas != null && tipsCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? tipsCanvas.worldCamera
            : null;

        ResetPopupLayout();
        RebuildVisualLayout();

        Vector2 targetCenterLocalPoint = ScreenToLocalPoint(parentRect, uiCam, targetRect.TransformPoint(targetRect.rect.center));
        Vector2 targetTopCenterLocalPoint = ScreenToLocalPoint(parentRect, uiCam, GetWorldTopCenter(targetRect));
        float rootX = targetCenterLocalPoint.x;
        float rootY = GetRootLocalY(targetTopCenterLocalPoint.y);

        if (!mOverlayLayoutApplied)
        {
            mRootRect.localPosition = new Vector3(rootX, rootY, mDefaultRootLocalPosition.z);
        }

        Rect parentBounds = parentRect.rect;
        float minVisibleX = parentBounds.xMin + HorizontalPadding;
        float maxVisibleX = parentBounds.xMax - HorizontalPadding;

        // Align to the target first, then clamp by WW's real width.
        // Prefab arrowL/R sit at a fixed ±150 and over-shift when WW is narrower (day 1).
        ApplyArrowLayout(mArrowMiddleRect, GetBgLocalXForArrow(mArrowMiddleRect), rootX, rootY);
        ClampBgInParent(parentRect, minVisibleX, maxVisibleX);
        AlignArrowToTarget(mArrowMiddleRect, parentRect, targetCenterLocalPoint);
    }

    private void ResetPopupLayout()
    {
        if (!mOverlayLayoutApplied && mRootRect != null)
        {
            mRootRect.localPosition = mDefaultRootLocalPosition;
        }

        if (bgRect != null)
        {
            CancelShowScaleAnimation();
            ResetBgScale();
            bgRect.localPosition = mDefaultBgLocalPosition;
        }

        RestoreArrowPositions();
        SetActiveArrow(null);
    }

    private void EnsureBgBottomPivot()
    {
        if (bgRect == null || mBgBottomPivotApplied)
        {
            return;
        }

        ApplyPivotKeepPosition(bgRect, BgBottomPivot);
        mBgBottomPivotApplied = true;
    }

    private static void ApplyPivotKeepPosition(RectTransform rectTransform, Vector2 pivot)
    {
        if (rectTransform == null || rectTransform.pivot == pivot)
        {
            return;
        }

        Vector2 size = rectTransform.rect.size;
        Vector2 deltaPivot = rectTransform.pivot - pivot;
        Vector2 deltaPosition = new(deltaPivot.x * size.x, deltaPivot.y * size.y);
        rectTransform.pivot = pivot;
        rectTransform.localPosition -= new Vector3(deltaPosition.x, deltaPosition.y, 0f);
    }

    private void PlayShowScaleAnimation()
    {
        if (bgRect == null)
        {
            return;
        }

        CancelShowScaleAnimation();
        EnsureBgBottomPivot();
        bgRect.localScale = Vector3.zero;
        mShowScaleTween = bgRect
            .DOScale(mDefaultBgLocalScale, ShowScaleAnimDuration)
            .SetEase(Ease.OutBack)
            .SetLink(bgRect.gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void CancelShowScaleAnimation()
    {
        if (mShowScaleTween == null)
        {
            return;
        }

        mShowScaleTween.Kill();
        mShowScaleTween = null;
    }

    private void ResetBgScale()
    {
        if (bgRect == null)
        {
            return;
        }

        bgRect.localScale = mDefaultBgLocalScale;
    }

    private void ApplyArrowLayout(RectTransform activeArrow, float bgLocalX, float rootX, float rootY)
    {
        if (mOverlayLayoutApplied)
        {
            bgRect.localPosition = new Vector3(
                rootX + bgLocalX,
                rootY + mDefaultBgLocalPosition.y + AwardBgVerticalOffset,
                mDefaultBgLocalPosition.z
            );
        }
        else
        {
            bgRect.localPosition = new Vector3(
                bgLocalX,
                mDefaultBgLocalPosition.y + AwardBgVerticalOffset,
                mDefaultBgLocalPosition.z);
        }

        SetActiveArrow(activeArrow);
    }

    private void SetActiveArrow(RectTransform activeArrow)
    {
        SetArrowVisible(mArrowLeftRect, activeArrow == mArrowLeftRect);
        SetArrowVisible(mArrowMiddleRect, activeArrow == mArrowMiddleRect);
        SetArrowVisible(mArrowRightRect, activeArrow == mArrowRightRect);
    }

    private static void SetArrowVisible(RectTransform arrowRect, bool visible)
    {
        if (arrowRect != null)
        {
            arrowRect.gameObject.SetActive(visible);
        }
    }

    private static bool IsArrowNode(Transform node)
    {
        return node != null && (node.name == "arrowL" || node.name == "arrowM" || node.name == "arrowR" || node.name == "arrowdR");
    }

    private static bool IsRewardWrapperNode(Transform node)
    {
        return node != null && !IsArrowNode(node) && node.name.StartsWith("cell");
    }

    private float GetBgLocalXForArrow(RectTransform arrowRect)
    {
        if (arrowRect == null || bgRect == null)
        {
            return mDefaultBgLocalPosition.x;
        }

        float arrowLocalX = GetArrowLocalXInBg(arrowRect);
        return -arrowLocalX;
    }

    private float GetArrowLocalXInBg(RectTransform arrowRect)
    {
        Vector3 arrowCenterWorld = arrowRect.TransformPoint(arrowRect.rect.center);
        Vector3 arrowCenterInBg = bgRect.InverseTransformPoint(arrowCenterWorld);
        return arrowCenterInBg.x;
    }

    private void CacheDefaultArrowPositions()
    {
        if (mDefaultArrowPosCached)
        {
            return;
        }

        if (mArrowLeftRect != null)
        {
            mDefaultArrowLeftPos = mArrowLeftRect.anchoredPosition;
        }

        if (mArrowMiddleRect != null)
        {
            mDefaultArrowMiddlePos = mArrowMiddleRect.anchoredPosition;
        }

        if (mArrowRightRect != null)
        {
            mDefaultArrowRightPos = mArrowRightRect.anchoredPosition;
        }

        mDefaultArrowPosCached = true;
    }

    private void RestoreArrowPositions()
    {
        if (!mDefaultArrowPosCached)
        {
            return;
        }

        if (mArrowLeftRect != null)
        {
            mArrowLeftRect.anchoredPosition = mDefaultArrowLeftPos;
        }

        if (mArrowMiddleRect != null)
        {
            mArrowMiddleRect.anchoredPosition = mDefaultArrowMiddlePos;
        }

        if (mArrowRightRect != null)
        {
            mArrowRightRect.anchoredPosition = mDefaultArrowRightPos;
        }
    }

    private void RebuildVisualLayout()
    {
        if (mLayoutContainerRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(mLayoutContainerRect);
        if (mLayoutContainerRect.rect.width <= 0f)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(mLayoutContainerRect);
        }
    }

    private RectTransform GetVisualRect()
    {
        return mLayoutContainerRect != null ? mLayoutContainerRect : bgRect;
    }

    private void GetVisualBoundsInParent(RectTransform parentRect, out float minX, out float maxX)
    {
        RectTransform visualRect = GetVisualRect();
        visualRect.GetWorldCorners(sCorners);
        minX = float.MaxValue;
        maxX = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector3 local = parentRect.InverseTransformPoint(sCorners[i]);
            minX = Mathf.Min(minX, local.x);
            maxX = Mathf.Max(maxX, local.x);
        }
    }

    private void ClampBgInParent(RectTransform parentRect, float minVisibleX, float maxVisibleX)
    {
        if (bgRect == null || parentRect == null)
        {
            return;
        }

        GetVisualBoundsInParent(parentRect, out float minX, out float maxX);
        float shiftX = 0f;
        float visibleWidth = maxVisibleX - minVisibleX;
        if (maxX - minX > visibleWidth)
        {
            shiftX = minVisibleX - minX;
        }
        else if (minX < minVisibleX)
        {
            shiftX = minVisibleX - minX;
        }
        else if (maxX > maxVisibleX)
        {
            shiftX = maxVisibleX - maxX;
        }

        if (Mathf.Approximately(shiftX, 0f))
        {
            return;
        }

        Vector3 worldDelta = parentRect.TransformVector(new Vector3(shiftX, 0f, 0f));
        Transform bgParent = bgRect.parent;
        Vector3 localDelta = bgParent != null ? bgParent.InverseTransformVector(worldDelta) : worldDelta;
        bgRect.localPosition += new Vector3(localDelta.x, 0f, 0f);
    }

    private void AlignArrowToTarget(RectTransform activeArrow, RectTransform parentRect, Vector2 targetLocalInParent)
    {
        if (activeArrow == null || mLayoutContainerRect == null || parentRect == null)
        {
            return;
        }

        Vector3 targetWorld = parentRect.TransformPoint(new Vector3(targetLocalInParent.x, targetLocalInParent.y, 0f));
        Vector3 targetInLayout = mLayoutContainerRect.InverseTransformPoint(targetWorld);

        float halfWidth = mLayoutContainerRect.rect.width * 0.5f;
        float arrowHalfWidth = activeArrow.rect.width * 0.5f;
        float minX = -halfWidth + arrowHalfWidth + ArrowEdgePadding;
        float maxX = halfWidth - arrowHalfWidth - ArrowEdgePadding;
        if (minX > maxX)
        {
            minX = maxX = 0f;
        }

        Vector2 pos = activeArrow.anchoredPosition;
        pos.x = Mathf.Clamp(targetInLayout.x, minX, maxX);
        activeArrow.anchoredPosition = pos;
    }

    private static int GetRewardCellOrder(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName) || !nodeName.StartsWith("cell"))
        {
            return int.MaxValue;
        }

        return int.TryParse(nodeName.Substring(4), out int order) ? order : int.MaxValue;
    }

    private float GetRootLocalY(float targetTopLocalY)
    {
        RectTransform referenceArrow = mArrowMiddleRect != null ? mArrowMiddleRect : mArrowLeftRect != null ? mArrowLeftRect : mArrowRightRect;
        if (referenceArrow == null)
        {
            return targetTopLocalY + mRootRect.rect.height * 0.5f;
        }

        float arrowCenterLocalY = GetArrowCenterLocalYInBg(referenceArrow);
        return targetTopLocalY - (mDefaultBgLocalPosition.y + arrowCenterLocalY);
    }

    private float GetArrowCenterLocalYInBg(RectTransform arrowRect)
    {
        Vector3 arrowCenterWorld = arrowRect.TransformPoint(arrowRect.rect.center);
        Vector3 arrowCenterInBg = bgRect.InverseTransformPoint(arrowCenterWorld);
        return arrowCenterInBg.y;
    }

    private static Vector2 ScreenToLocalPoint(RectTransform parentRect, Camera uiCam, Vector3 worldPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            RectTransformUtility.WorldToScreenPoint(uiCam, worldPoint),
            uiCam,
            out Vector2 localPoint
        );
        return localPoint;
    }

    private static Vector3 GetWorldTopCenter(RectTransform targetRect)
    {
        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        return (corners[1] + corners[2]) * 0.5f;
    }
}
