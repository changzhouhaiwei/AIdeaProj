using System.Collections.Generic;
using cfg.Game;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;
using DG.Tweening;

public class UIMapMain : UIMapMainBase
{
    private const string SceneBgMapDir = "SceneBgMap";
    private const string SceneBgDir = "SceneBg";
    private const string RuntimeGroupPrefix = "runtime_";
    private const float GroupSpacing = 24f;
    private const float ContentPaddingTop = 16f;
    private const float ContentPaddingBottom = 1f;
    private const float FrameBottomPad = 26f;
    private const float ScrollToBottomDuration = 0.45f;
    private const float ScrollBottomEpsilon = 0.001f;

    private static readonly Color UnlockedPhotoColor = Color.white;
    private static readonly Color LockedPhotoColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    private static readonly Color LockedFrameColor = new Color32(0x57, 0x57, 0x57, 0xFF);

    private readonly Dictionary<string, Transform> groupTemplates = new Dictionary<string, Transform>();
    private readonly List<GameObject> runtimeGroups = new List<GameObject>();

    private bool templatesCached;
    private ScrollRect mapScrollRect;
    private RectTransform focusSlotRt;
    private int cachedCurLevel;
    private Tween scrollToBottomTween;

    protected override void OnOpen()
    {
        base.OnOpen();
        UIBottomNav.Show(BottomNavTab.Map);
        RebuildMapScroll();
    }

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == closeBtn)
        {
            CloseFromBottomNav();
        }
        else if (button == downBtn)
        {
            ScrollToBottom();
        }
    }

    protected override void OnValueChanged(ScrollRect scrollRect, Vector2 value)
    {
        base.OnValueChanged(scrollRect, value);
        if (scrollRect == mapScrollRect || scrollRect == scrollView)
        {
            RefreshDownButtonVisibility();
        }
    }

    protected override void OnUnbindCompsAndEvents()
    {
        KillScrollToBottomTween();
        mapScrollRect = null;
        base.OnUnbindCompsAndEvents();
    }

    protected override bool OnEscButtonPressed()
    {
        CloseFromBottomNav();
        return true;
    }

    public void CloseFromBottomNav()
    {
        CloseSelf();
        UIBottomNav.SetTab(BottomNavTab.Home);
    }

    private void RebuildMapScroll()
    {
        var content = FindMapContent();
        if (content == null)
        {
            Debug.LogError("[UIMapMain] Map ScrollView Content not found.");
            return;
        }

        if (ConfigHelper.Tables == null)
        {
            Debug.LogError("[UIMapMain] ConfigHelper.Tables is null.");
            return;
        }

        CacheGroupTemplates(content);
        SetupScrollLayout(content);

        var styleGroups = CollectStyleGroups();
        if (styleGroups.Count == 0)
        {
            Debug.LogError("[UIMapMain] TbGameBgShowStyleCfg has no valid group sequence.");
            return;
        }

        var bgList = CollectBgList();
        ClearRuntimeGroups(content);
        focusSlotRt = null;
        cachedCurLevel = GetCurrentLevelId();

        int pathIndex = 0;
        int styleIndex = 0;
        while (pathIndex < bgList.Count)
        {
            string groupName = styleGroups[styleIndex % styleGroups.Count];
            styleIndex++;

            if (!groupTemplates.TryGetValue(groupName, out var template) || template == null)
            {
                Debug.LogWarning($"[UIMapMain] Missing group template: {groupName}");
                continue;
            }

            var instance = Object.Instantiate(template.gameObject, content);
            instance.name = RuntimeGroupPrefix + groupName;
            instance.SetActive(true);
            instance.transform.SetAsLastSibling();
            PrepareGroupForLayout(instance.transform as RectTransform);
            runtimeGroups.Add(instance);

            var slots = CollectPatternSlots(instance.transform);
            if (slots.Count == 0)
            {
                Debug.LogWarning($"[UIMapMain] Group template has no parttern slots: {groupName}");
                Object.Destroy(instance);
                runtimeGroups.RemoveAt(runtimeGroups.Count - 1);
                continue;
            }

            int remaining = bgList.Count - pathIndex;
            bool isLastGroup = remaining <= slots.Count;
            bool incomplete = remaining < slots.Count;

            for (int s = 0; s < slots.Count; s++)
            {
                var slot = slots[s];

                if (pathIndex < bgList.Count)
                {
                    slot.gameObject.SetActive(true);
                    BindFilledPattern(slot, bgList[pathIndex]);
                    pathIndex++;
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }

            if (incomplete)
            {
                CropGroupToVisibleFrames(instance.transform as RectTransform);
            }

            if (isLastGroup || pathIndex >= bgList.Count)
            {
                break;
            }
        }

        RefreshScrollDisplay(content);
        ScrollToFocusSlot();
        RefreshDownButtonVisibility();
    }

    private static List<string> CollectStyleGroups()
    {
        var styleGroups = new List<string>();
        var styleList = ConfigHelper.Tables.TbGameBgShowStyleCfg.DataList;
        for (int i = 0; i < styleList.Count; i++)
        {
            var style = styleList[i];
            if (style == null || string.IsNullOrEmpty(style.Group))
            {
                continue;
            }

            styleGroups.Add(style.Group);
        }

        return styleGroups;
    }

    private static List<GameBgCfg> CollectBgList()
    {
        var bgList = new List<GameBgCfg>();
        var rawBgList = ConfigHelper.Tables.TbGameBgShow.DataList;
        for (int i = 0; i < rawBgList.Count; i++)
        {
            var cfg = rawBgList[i];
            if (cfg != null && !string.IsNullOrEmpty(cfg.Path))
            {
                bgList.Add(cfg);
            }
        }

        return bgList;
    }

    private void SetupScrollLayout(Transform content)
    {
        var contentRt = content as RectTransform;
        if (contentRt != null)
        {
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
        }

        var layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = GroupSpacing;
            layout.padding = new RectOffset(0, 0, (int)ContentPaddingTop, (int)ContentPaddingBottom);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;
        }

        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        if (mapScrollRect != null)
        {
            mapScrollRect.horizontal = false;
            mapScrollRect.vertical = true;
            mapScrollRect.inertia = true;
        }
    }

    private static void PrepareGroupForLayout(RectTransform groupRt)
    {
        if (groupRt == null)
        {
            return;
        }

        float width = groupRt.sizeDelta.x > 0f ? groupRt.sizeDelta.x : 720f;
        float height = groupRt.sizeDelta.y > 0f ? groupRt.sizeDelta.y : 600f;

        groupRt.anchorMin = new Vector2(0.5f, 1f);
        groupRt.anchorMax = new Vector2(0.5f, 1f);
        groupRt.pivot = new Vector2(0.5f, 1f);
        groupRt.sizeDelta = new Vector2(width, height);
        groupRt.anchoredPosition = Vector2.zero;
        groupRt.localScale = Vector3.one;
        groupRt.localRotation = Quaternion.identity;

        var layoutElement = groupRt.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = groupRt.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = width;
        layoutElement.preferredHeight = height;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
        layoutElement.minHeight = height;
    }

    private static void CropGroupToVisibleFrames(RectTransform groupRt)
    {
        if (groupRt == null)
        {
            return;
        }

        bool anyVisible = false;
        float minY = 0f;
        var corners = new Vector3[4];
        var slots = CollectPatternSlots(groupRt);
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || !slot.gameObject.activeSelf)
            {
                continue;
            }

            var frame = slot.Find("kuang") as RectTransform;
            if (frame == null)
            {
                frame = slot.Find("mask") as RectTransform;
            }

            if (frame == null)
            {
                continue;
            }

            frame.GetWorldCorners(corners);
            for (int c = 0; c < 4; c++)
            {
                float ly = groupRt.InverseTransformPoint(corners[c]).y;
                if (!anyVisible || ly < minY)
                {
                    minY = ly;
                    anyVisible = true;
                }
            }
        }

        if (!anyVisible)
        {
            return;
        }

        float oldH = groupRt.sizeDelta.y;
        float newH = Mathf.Max(-minY + FrameBottomPad, 1f);
        if (newH >= oldH)
        {
            return;
        }

        groupRt.sizeDelta = new Vector2(groupRt.sizeDelta.x, newH);

        var layoutElement = groupRt.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredHeight = newH;
            layoutElement.minHeight = newH;
        }

        float dy = (oldH - newH) * 0.5f;
        for (int i = 0; i < groupRt.childCount; i++)
        {
            var child = groupRt.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            child.anchoredPosition -= new Vector2(0f, dy);
        }
    }

    private void RefreshScrollDisplay(Transform content)
    {
        var contentRt = content as RectTransform;
        if (contentRt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }

        Canvas.ForceUpdateCanvases();

        if (mapScrollRect != null)
        {
            mapScrollRect.StopMovement();
            mapScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void ScrollToFocusSlot()
    {
        if (mapScrollRect == null || focusSlotRt == null)
        {
            return;
        }

        var content = mapScrollRect.content;
        var viewport = mapScrollRect.viewport != null ? mapScrollRect.viewport : mapScrollRect.transform as RectTransform;
        if (content == null || viewport == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        float scrollable = contentHeight - viewportHeight;
        if (scrollable <= 1f)
        {
            mapScrollRect.verticalNormalizedPosition = 1f;
            return;
        }

        // Content is top-pivoted; convert slot center into content-local Y (0 at top, negative downward).
        Vector3 slotWorld = focusSlotRt.TransformPoint(focusSlotRt.rect.center);
        float slotLocalY = content.InverseTransformPoint(slotWorld).y;
        float offsetFromTop = -slotLocalY - viewportHeight * 0.5f;
        float normalized = 1f - Mathf.Clamp01(offsetFromTop / scrollable);
        mapScrollRect.verticalNormalizedPosition = normalized;
    }

    private void ScrollToBottom()
    {
        if (mapScrollRect == null || !CanScroll())
        {
            RefreshDownButtonVisibility();
            return;
        }

        KillScrollToBottomTween();
        mapScrollRect.StopMovement();

        if (mapScrollRect.verticalNormalizedPosition <= ScrollBottomEpsilon)
        {
            mapScrollRect.verticalNormalizedPosition = 0f;
            RefreshDownButtonVisibility();
            return;
        }

        scrollToBottomTween = DOTween.To(
                () => mapScrollRect.verticalNormalizedPosition,
                value => mapScrollRect.verticalNormalizedPosition = value,
                0f,
                ScrollToBottomDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                scrollToBottomTween = null;
                if (mapScrollRect != null)
                {
                    mapScrollRect.verticalNormalizedPosition = 0f;
                }

                RefreshDownButtonVisibility();
            });
    }

    private void RefreshDownButtonVisibility()
    {
        if (downBtn == null)
        {
            return;
        }

        bool shouldShow = mapScrollRect != null
            && CanScroll()
            && mapScrollRect.verticalNormalizedPosition > ScrollBottomEpsilon;
        if (downBtn.gameObject.activeSelf != shouldShow)
        {
            downBtn.gameObject.SetActive(shouldShow);
        }
    }

    private bool CanScroll()
    {
        if (mapScrollRect == null || mapScrollRect.content == null)
        {
            return false;
        }

        var viewport = mapScrollRect.viewport != null
            ? mapScrollRect.viewport
            : mapScrollRect.transform as RectTransform;
        return viewport != null && mapScrollRect.content.rect.height > viewport.rect.height + 1f;
    }

    private void KillScrollToBottomTween()
    {
        if (scrollToBottomTween != null && scrollToBottomTween.IsActive())
        {
            scrollToBottomTween.Kill();
        }

        scrollToBottomTween = null;
    }

    private void CacheGroupTemplates(Transform content)
    {
        if (templatesCached)
        {
            return;
        }

        groupTemplates.Clear();
        for (int i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i);
            if (!child.name.StartsWith("group"))
            {
                continue;
            }

            if (child.name.StartsWith(RuntimeGroupPrefix))
            {
                continue;
            }

            groupTemplates[child.name] = child;
            child.gameObject.SetActive(false);
        }

        templatesCached = true;
    }

    private void ClearRuntimeGroups(Transform content)
    {
        runtimeGroups.Clear();

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i);
            if (child.name.StartsWith(RuntimeGroupPrefix))
            {
                // Immediate: rebuild runs in the same frame and must not keep stale groups in layout.
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private Transform FindMapContent()
    {
        if (scrollView != null && scrollView.content != null)
        {
            mapScrollRect = scrollView;
            return scrollView.content;
        }

        var scrollViews = gameObject.GetComponentsInChildren<ScrollRect>(true);
        for (int i = 0; i < scrollViews.Length; i++)
        {
            var content = scrollViews[i].content;
            if (content == null)
            {
                continue;
            }

            for (int c = 0; c < content.childCount; c++)
            {
                var child = content.GetChild(c);
                if (child.name.StartsWith("group") || child.name.StartsWith(RuntimeGroupPrefix))
                {
                    mapScrollRect = scrollViews[i];
                    return content;
                }
            }
        }

        return null;
    }

    private static List<Transform> CollectPatternSlots(Transform groupTf)
    {
        var slots = new List<Transform>();
        for (int i = 0; i < groupTf.childCount; i++)
        {
            var child = groupTf.GetChild(i);
            if (child.name.StartsWith("parttern"))
            {
                slots.Add(child);
            }
        }

        return slots;
    }

    private void BindFilledPattern(Transform slot, GameBgCfg cfg)
    {
        var maskImg = slot.Find("mask")?.GetComponent<Image>();
        var photoImg = slot.Find("mask/Image")?.GetComponent<Image>();
        var kuangImg = slot.Find("kuang")?.GetComponent<Image>();
        var staytuned = slot.Find("mask/StaytunedText");
        var lockNode = slot.Find("mask/Lock");
        bool isLocked = IsBgLocked(cfg, cachedCurLevel);

        if (staytuned != null)
        {
            staytuned.gameObject.SetActive(false);
        }

        if (lockNode != null)
        {
            lockNode.gameObject.SetActive(isLocked);
        }

        if (maskImg != null && !string.IsNullOrEmpty(cfg.Phototype))
        {
            var maskSprite = LoadSprite(SceneBgMapDir, cfg.Phototype);
            if (maskSprite != null)
            {
                maskImg.sprite = maskSprite;
            }
        }

        if (kuangImg != null)
        {
            kuangImg.color = isLocked ? LockedFrameColor : Color.white;

            if (!string.IsNullOrEmpty(cfg.Phototype))
            {
                var kuangSprite = LoadKuangSprite(cfg.Phototype);
                if (kuangSprite != null)
                {
                    kuangImg.sprite = kuangSprite;
                }
            }
        }

        if (photoImg != null && !string.IsNullOrEmpty(cfg.Path))
        {
            photoImg.gameObject.SetActive(true);
            // Fill the shaped mask instead of letterboxing inside the frame.
            photoImg.preserveAspect = false;
            photoImg.color = isLocked ? LockedPhotoColor : UnlockedPhotoColor;

            var bgSprite = LoadSpriteBg(SceneBgDir, "bg_" + cfg.Path);
            if (bgSprite != null)
            {
                photoImg.sprite = bgSprite;
            }
        }

        BindPatternClick(slot, cfg, isLocked);
        UpdateFocusSlot(slot as RectTransform, cfg, isLocked);
    }

    private void UpdateFocusSlot(RectTransform slotRt, GameBgCfg cfg, bool isLocked)
    {
        if (slotRt == null || cfg == null)
        {
            return;
        }

        // Prefer the background that covers the player's current level.
        if (cachedCurLevel >= cfg.StartLevel && cachedCurLevel <= cfg.EndLevel)
        {
            focusSlotRt = slotRt;
            return;
        }

        // Fallback: furthest unlocked photo (list is ordered by progression).
        if (!isLocked)
        {
            focusSlotRt = slotRt;
        }
    }

    private void BindPatternClick(Transform slot, GameBgCfg cfg, bool isLocked)
    {
        var button = slot.GetComponent<Button>();
        if (button == null)
        {
            button = slot.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            var graphic = slot.Find("mask")?.GetComponent<Graphic>()
                          ?? slot.Find("kuang")?.GetComponent<Graphic>();
            if (graphic != null)
            {
                button.targetGraphic = graphic;
            }
        }

        button.enabled = true;
        button.onClick.RemoveAllListeners();
        var captured = cfg;
        var capturedLocked = isLocked;
        button.onClick.AddListener(() => OnPatternClicked(captured, capturedLocked));
    }

    private static int GetCurrentLevelId()
    {
        if (GPUserDataManager.Instance == null)
        {
            return 1;
        }

        return GPUserDataManager.Instance.GetLevelNum() + 1;
    }

    private static bool IsBgLocked(GameBgCfg cfg, int curLevel)
    {
        if (cfg == null)
        {
            return true;
        }

        return curLevel < cfg.StartLevel;
    }

    private static void OnPatternClicked(GameBgCfg cfg, bool isLocked)
    {
        if (cfg == null)
        {
            return;
        }

        if (isLocked)
        {
            AlertDlg.ShowDlg(I2LocalizeMgr.GetString("UIPhotoAlbumPhotoLockTips"));
            return;
        }

        // Tips 层在 Panel 之上，确保盖住 UIMapMain
        var dlg = GameModule.UI.OpenPanel<UIStoryBgPassDlg>(PanelLayer.Tips, PanelOpenType.Single);
        if (dlg == null)
        {
            return;
        }

        dlg.Open();
        GameModule.UI.BringPanelToFront(dlg);
        dlg.RefreshUI(cfg, true);
    }

    private static Sprite LoadKuangSprite(string phototype)
    {
        var sprite = LoadSprite(SceneBgMapDir, phototype + "_kuang");
        if (sprite == null)
        {
            // Asset file typo: yuanjiaojuxing_kaung.png
            sprite = LoadSprite(SceneBgMapDir, phototype + "_kaung");
        }

        return sprite;
    }
}
