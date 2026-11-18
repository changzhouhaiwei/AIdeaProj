using System.Collections.Generic;
using DG.Tweening;
using FishFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAwardTips : GPViewBase
{
    [SerializeField] RectTransform bgRect;
    [SerializeField] RectTransform hNodeRect;
    [SerializeField] List<RectTransform> arrowRectList;
    [SerializeField] List<GameObject> awardCellList;

    const float ArrowGap = 30f;
    const float ScreenPadding = 20f;
    const float OpenAnimDuration = 0.25f;

    static readonly Vector3[] sCorners = new Vector3[4];

    Vector3 mDefaultScale = Vector3.one;
    Vector2 mDefaultPivot = new Vector2(0.5f, 0.5f);
    Tween mOpenTween;
    IList<IRankCell> mAwardCells;
    IRankCell mShownCell;
    Vector2 mLastPointer;
    bool mEmptyPointerBound;

    protected override void Awake()
    {
        base.Awake();
        if (hNodeRect != null)
        {
            mDefaultScale = hNodeRect.localScale;
            mDefaultPivot = hNodeRect.pivot;
        }
        BindEmptyPointer();
        // Prefab starts inactive; do not Hide() here — Awake/Start run on first Show
        // and would immediately close tips (and leave scale at 0 after a killed open anim).
    }

    void BindEmptyPointer()
    {
        if (mEmptyPointerBound)
            return;

        var empty = transform.Find("EmptyButton");
        if (empty == null)
            return;

        mEmptyPointerBound = true;
        var trigger = empty.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = empty.gameObject.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entry.callback.AddListener(data =>
        {
            if (data is PointerEventData pointerData)
                mLastPointer = pointerData.position;
        });
        trigger.triggers.Add(entry);
    }

    public void Hide()
    {
        mOpenTween?.Kill();
        mOpenTween = null;
        mShownCell = null;
        if (hNodeRect != null)
        {
            hNodeRect.localScale = mDefaultScale;
            hNodeRect.pivot = mDefaultPivot;
        }
        gameObject.SetActive(false);
    }

    public void OnEmptyButtonClick()
    {
        var next = FindAwardCellAt(mLastPointer);
        if (next != null && next != mShownCell)
        {
            Show(next, mAwardCells);
            return;
        }

        Hide();
    }

    public void Show(IRankCell cell, IList<IRankCell> awardCells = null)
    {
        mShownCell = cell;
        if (awardCells != null)
            mAwardCells = awardCells;

        BindEmptyPointer();
        // Cold-load sprites while still inactive so the hitch is not tied to a wrong-layout frame.
        WarmAwardSprites(cell);
        gameObject.SetActive(true);
        int awardCount = ShowAwardsData(cell);
        ShowTipsTran(cell, awardCount);
    }

    IRankCell FindAwardCellAt(Vector2 screenPos)
    {
        if (mAwardCells == null || mAwardCells.Count == 0)
            return null;

        var tipsCanvas = GetComponentInParent<Canvas>();
        var uiCam = tipsCanvas != null && tipsCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? tipsCanvas.worldCamera
            : null;

        IRankCell iconHit = null;
        for (int i = 0; i < mAwardCells.Count; i++)
        {
            var cell = mAwardCells[i];
            if (cell == null)
                continue;

            var icon = cell.GetAwardIconObj();
            if (icon == null || !icon.activeInHierarchy)
                continue;

            var iconRt = icon.transform as RectTransform;
            if (iconRt != null && RectTransformUtility.RectangleContainsScreenPoint(iconRt, screenPos, uiCam))
                iconHit = cell;
        }

        return iconHit;
    }

    private void WarmAwardSprites(IRankCell cell)
    {
        var dataList = cell?.GetRankAwardData();
        if (dataList?.Award == null)
        {
            return;
        }

        for (int i = 0; i < dataList.Award.Count && i < 4; i++)
        {
            var tempList = dataList.Award[i].Split(',');
            if (tempList.Length >= 2)
            {
                LoadSprite("LevelAward", "item_" + tempList[0]);
            }
        }
    }

    private void ShowIconsAndNum(GameObject cell, string id, string num)
    {
        TextMeshProUGUI text = cell.GetComponentInChildren<TextMeshProUGUI>();
        Image icon = cell.GetComponentInChildren<Image>();

        text.text = "x" + num;
        icon.sprite = LoadSprite("LevelAward", "item_" + id);
    }

    private void RebuildHNodeLayout()
    {
        if (hNodeRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(hNodeRect);

        Rect rect = hNodeRect.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(hNodeRect);
        }
    }

    private int ShowAwardsData(IRankCell cell)
    {
        TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.buttonSoundSpread);
        var dataList = cell.GetRankAwardData();
        if (dataList == null || awardCellList == null)
        {
            return 0;
        }

        for (int i = 0; i < awardCellList.Count && i < 4; i++)
        {
            var cellObj = awardCellList[i];
            bool active = i < dataList.Award.Count;
            cellObj.SetActive(active);
            if (active)
            {
                var tempList = dataList.Award[i].Split(',');
                if (tempList.Length >= 2)
                {
                    ShowIconsAndNum(cellObj, tempList[0], tempList[1]);
                }
            }
        }

        RebuildHNodeLayout();

        return Mathf.Min(dataList.Award.Count, 4);
    }

    private void ShowTipsTran(IRankCell cell, int awardCount)
    {
        var sp = cell.GetAwardIconObj();
        if (sp == null || hNodeRect == null || arrowRectList == null || arrowRectList.Count == 0)
        {
            return;
        }

        // Layout/position while at full scale — never measure corners at scale 0.
        hNodeRect.localScale = mDefaultScale;
        hNodeRect.pivot = mDefaultPivot;
        RebuildHNodeLayout();

        var parentRect = hNodeRect.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        var root = parentRect.parent as RectTransform;
        if (root == null)
        {
            root = parentRect;
        }

        var tipsCanvas = hNodeRect.GetComponentInParent<Canvas>();
        var uiCam = tipsCanvas != null && tipsCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? tipsCanvas.worldCamera
            : null;

        Vector3 iconWorld = sp.transform.position;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, iconWorld);
        Vector3 iconLocal = parentRect.InverseTransformPoint(iconWorld);
        bool isTop = screenPos.y > Screen.height * 0.5f;

        // Default to the center arrow; only switch to a side arrow if the centered hNode overflows.
        int idx = isTop ? 0 : 1;
        PlaceWithArrow(idx, parentRect, iconLocal, isTop);

        // With only 1-2 awards always keep the center arrow.
        if (awardCount >= 3)
        {
            Rect safe = root.rect;
            GetHNodeBoundsInRoot(root, out float minX, out float maxX, out _, out _);
            bool overflowLeft = minX < safe.xMin + ScreenPadding;
            bool overflowRight = maxX > safe.xMax - ScreenPadding;

            if (overflowLeft && !overflowRight)
            {
                // Icon near left edge: anchor arrow on the left so hNode extends right.
                idx = isTop ? 2 : 4;
            }
            else if (overflowRight && !overflowLeft)
            {
                // Icon near right edge: anchor arrow on the right so hNode extends left.
                idx = isTop ? 3 : 5;
            }
        }

        // Pivot exactly at the active arrow so the open animation scales out from the arrow,
        // then re-align (pivot change shifts the rect) and clamp.
        if (idx >= 0 && idx < arrowRectList.Count && arrowRectList[idx] != null)
        {
            ApplyScalePivot(arrowRectList[idx]);
        }
        PlaceWithArrow(idx, parentRect, iconLocal, isTop);
        ClampHNodeInScreen(root);

        PlayOpenAnim();
    }

    private void ApplyScalePivot(RectTransform activeArrow)
    {
        Rect rect = hNodeRect.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        // Reset to default pivot first so arrow→pivot math never chains a dirty pivot.
        hNodeRect.pivot = mDefaultPivot;

        // Arrow center in hNode local space (handles the arrow's own offset/flip and hNode scale).
        Vector3 arrowLocal = hNodeRect.InverseTransformPoint(activeArrow.TransformPoint(activeArrow.rect.center));

        // Convert that point to a normalized pivot (may fall outside [0,1] since arrows can sit
        // beyond the hNode rect, e.g. the bottom arrows around y = -86 with a 100px tall rect).
        float pivotX = arrowLocal.x / rect.width + mDefaultPivot.x;
        float pivotY = arrowLocal.y / rect.height + mDefaultPivot.y;
        var target = new Vector2(pivotX, pivotY);

        if (!Mathf.Approximately(mDefaultPivot.x, target.x) || !Mathf.Approximately(mDefaultPivot.y, target.y))
        {
            hNodeRect.pivot = target;
        }
    }

    private void PlayOpenAnim()
    {
        mOpenTween?.Kill();
        hNodeRect.localScale = Vector3.zero;
        mOpenTween = hNodeRect
            .DOScale(mDefaultScale, OpenAnimDuration)
            .SetEase(Ease.OutQuad)
            .SetLink(hNodeRect.gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void PlaceWithArrow(int idx, RectTransform parentRect, Vector3 iconLocal, bool isTop)
    {
        if (idx < 0 || idx >= arrowRectList.Count || arrowRectList[idx] == null)
        {
            return;
        }

        for (int i = 0; i < arrowRectList.Count; i++)
        {
            if (arrowRectList[i] != null)
            {
                arrowRectList[i].gameObject.SetActive(i == idx);
            }
        }

        AlignHNodeToIcon(parentRect, arrowRectList[idx], iconLocal, isTop);
    }

    private void AlignHNodeToIcon(RectTransform parentRect, RectTransform activeArrow, Vector3 iconLocal, bool isTop)
    {
        Vector3 arrowCenterWorld = activeArrow.TransformPoint(activeArrow.rect.center);
        Vector3 arrowLocal = parentRect.InverseTransformPoint(arrowCenterWorld);

        float deltaX = iconLocal.x - arrowLocal.x;
        float desiredArrowY = iconLocal.y + (isTop ? -ArrowGap : ArrowGap);
        float deltaY = desiredArrowY - arrowLocal.y;

        hNodeRect.anchoredPosition += new Vector2(deltaX, deltaY);
    }

    private void GetHNodeBoundsInRoot(RectTransform root, out float minX, out float maxX, out float minY, out float maxY)
    {
        hNodeRect.GetWorldCorners(sCorners);
        minX = float.MaxValue;
        maxX = float.MinValue;
        minY = float.MaxValue;
        maxY = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector3 local = root.InverseTransformPoint(sCorners[i]);
            minX = Mathf.Min(minX, local.x);
            maxX = Mathf.Max(maxX, local.x);
            minY = Mathf.Min(minY, local.y);
            maxY = Mathf.Max(maxY, local.y);
        }
    }

    private void ClampHNodeInScreen(RectTransform root)
    {
        GetHNodeBoundsInRoot(root, out float minX, out float maxX, out float minY, out float maxY);

        Rect safe = root.rect;
        float shiftX = 0f;
        float shiftY = 0f;

        if (minX < safe.xMin + ScreenPadding)
        {
            shiftX = (safe.xMin + ScreenPadding) - minX;
        }
        else if (maxX > safe.xMax - ScreenPadding)
        {
            shiftX = (safe.xMax - ScreenPadding) - maxX;
        }

        if (minY < safe.yMin + ScreenPadding)
        {
            shiftY = (safe.yMin + ScreenPadding) - minY;
        }
        else if (maxY > safe.yMax - ScreenPadding)
        {
            shiftY = (safe.yMax - ScreenPadding) - maxY;
        }

        if (shiftX != 0f || shiftY != 0f)
        {
            hNodeRect.anchoredPosition += new Vector2(shiftX, shiftY);
        }
    }
}
