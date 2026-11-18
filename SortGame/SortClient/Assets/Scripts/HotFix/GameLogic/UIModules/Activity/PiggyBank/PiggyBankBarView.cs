using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 主菜单 / 结算 nextLevelButton 下的 PiggyBankBar + PiggyStageTips。
/// 节点全部在 Inspector 绑定，不要 Find。
/// </summary>
public sealed class PiggyBankBarView : MonoBehaviour
{
    public const float DoneFontSize = 50f;
    public const float IdleFontSize = 34f;
    public const float CurrentFontSize = 44f;
    public const float PunchFontSize = 65f;
    public const float RevealPunchDuration = 0.18f;
    public const float RevealSettleDuration = 0.22f;
    /// <summary>与 DailyboxTips / UIAwardTips.OpenAnimDuration 一致。</summary>
    public const float TipsOpenDuration = 0.25f;
    public const float TipsCloseDuration = 0.15f;
    static readonly Color IdleColor = Color.white;
    static readonly Color CurrentColor = new Color(1f, 0.84f, 0.15f, 1f);
    static readonly Color LockedStageColor = new Color(0.5568628f, 0.5568628f, 0.5568628f, 1f);

    [Header("Stages")]
    [SerializeField] GameObject stage3Root;
    [SerializeField] GameObject stage5Root;
    [SerializeField] GameObject stage7Root;
    [SerializeField] TextMeshProUGUI[] stage3ItemTexts;
    [SerializeField] TextMeshProUGUI[] stage5ItemTexts;
    [SerializeField] TextMeshProUGUI[] stage7ItemTexts;

    [Header("Rule / Tips")]
    [SerializeField] Button stage3Button;
    [SerializeField] Button stage5Button;
    [SerializeField] Button stage7Button;
    [SerializeField] RectTransform alertMark;
    [SerializeField] GameObject tipsRoot;
    [SerializeField] GameObject arrow3;
    [SerializeField] GameObject arrow5;
    [SerializeField] GameObject arrow7;
    [SerializeField] TextMeshProUGUI timerTex;

    [Header("Tips Stage 3")]
    [SerializeField] GameObject tip3Finish;
    [SerializeField] GameObject tip3NoFinish;
    [SerializeField] GameObject tip3Lock;

    [Header("Tips Stage 5")]
    [SerializeField] GameObject tip5Finish;
    [SerializeField] GameObject tip5NoFinish;
    [SerializeField] GameObject tip5Lock;

    [Header("Tips Stage 7")]
    [SerializeField] GameObject tip7Finish;
    [SerializeField] GameObject tip7NoFinish;
    [SerializeField] GameObject tip7Lock;

    bool listening;
    int displayStageLevel;
    bool deferredReveal;
    int deferredDoneCount;
    Tween revealTween;
    Tween tipsTween;
    GameObject outsideCatcher;
    readonly List<RaycastResult> outsideHits = new List<RaycastResult>();
    Vector2 tipsDefaultPivot = new Vector2(0.5f, 0.5f);
    Vector2 tipsDefaultAnchoredPos;
    bool tipsLayoutCached;

    void Awake()
    {
        BindStageButtons();
        CacheTipsLayout();
        if (tipsRoot != null)
            tipsRoot.SetActive(false);
    }

    void OnEnable()
    {
        BindStageButtons();
        if (!listening)
        {
            PiggyBankState.Changed += OnStateChanged;
            listening = true;
        }
        RefreshForContext();
        if (isActiveAndEnabled && tipsRoot != null && tipsRoot.activeSelf)
            ShowOutsideCatcher();
    }

    void OnDisable()
    {
        KillRevealTween();
        bool tipsAnimating = tipsTween != null && tipsTween.IsActive();
        KillTipsTween();
        KillAlertTween();
        if (tipsAnimating)
            FinishCloseTips();
        else if (tipsRoot == null || !tipsRoot.activeSelf)
            RestoreAlertShown();
        HideOutsideCatcher();
        if (listening)
        {
            PiggyBankState.Changed -= OnStateChanged;
            listening = false;
        }
    }

    void OnDestroy()
    {
        KillRevealTween();
        KillTipsTween();
        KillAlertTween();
        UnbindStageButton(stage3Button);
        UnbindStageButton(stage5Button);
        UnbindStageButton(stage7Button);
        if (outsideCatcher != null)
            Destroy(outsideCatcher);
    }

    void KillRevealTween()
    {
        if (revealTween != null && revealTween.IsActive())
            revealTween.Kill();
        revealTween = null;
    }

    void KillTipsTween()
    {
        if (tipsTween != null && tipsTween.IsActive())
            tipsTween.Kill();
        tipsTween = null;
    }

    void CacheTipsLayout()
    {
        if (tipsLayoutCached || tipsRoot == null)
            return;
        var rt = tipsRoot.transform as RectTransform;
        if (rt == null)
            return;
        tipsDefaultPivot = rt.pivot;
        tipsDefaultAnchoredPos = rt.anchoredPosition;
        tipsLayoutCached = true;
    }

    void Update()
    {
        PiggyBankState.EnsureDay();
        if (tipsRoot == null || !tipsRoot.activeSelf || timerTex == null)
            return;
        RefreshTimer();
    }

    void BindStageButtons()
    {
        BindStageButton(stage3Button);
        BindStageButton(stage5Button);
        BindStageButton(stage7Button);
    }

    void BindStageButton(Button button)
    {
        if (button == null)
            return;
        button.onClick.RemoveListener(OnStageClicked);
        button.onClick.AddListener(OnStageClicked);
    }

    void UnbindStageButton(Button button)
    {
        if (button == null)
            return;
        button.onClick.RemoveListener(OnStageClicked);
    }

    void OnStateChanged()
    {
        if (!PiggyBankState.SettlementHadWin)
            deferredReveal = false;
        if (!deferredReveal)
            RefreshForContext();
    }

    /// <summary>GM 改档后立刻刷进度和已打开的气泡箭头。</summary>
    public void RefreshAfterExternalChange()
    {
        if (!PiggyBankState.SettlementHadWin)
            deferredReveal = false;
        if (!isActiveAndEnabled)
            return;
        RefreshForContext();
    }

    public void RefreshForContext()
    {
        bool show = PiggyBankState.IsMainEntryVisible;
        gameObject.SetActive(show);
        if (!show)
            return;

        int stageLevel = PiggyBankState.DisplayStageLevel;
        int progress = PiggyBankState.DisplayProgress;
        int hl = progress;
        if (deferredReveal && PiggyBankState.SettlementHadWin)
            hl = Mathf.Max(0, progress - 1);

        Apply(stageLevel, hl);
    }

        /// <summary>结算开场：先显示本关前进度（新格仍 34），等 nextLevelButton 出现后再 Reveal。</summary>
    public void ApplySettlementDeferred()
    {
        bool show = PiggyBankState.IsMainEntryVisible;
        gameObject.SetActive(show);
        if (!show)
            return;

        deferredReveal = PiggyBankState.SettlementHadWin;
        int stageLevel = PiggyBankState.DisplayStageLevel;
        int progress = PiggyBankState.DisplayProgress;
        deferredDoneCount = deferredReveal ? Mathf.Max(0, progress - 1) : progress;
        Apply(stageLevel, deferredDoneCount);
    }

    public void RevealSettlementProgress()
    {
        if (!gameObject.activeInHierarchy && !gameObject.activeSelf)
            return;

        int stageLevel = PiggyBankState.DisplayStageLevel;
        int progress = PiggyBankState.DisplayProgress;
        deferredReveal = false;

        // 无新进度：直接刷满
        if (!PiggyBankState.SettlementHadWin || progress <= deferredDoneCount)
        {
            Apply(stageLevel, progress);
            return;
        }

        displayStageLevel = stageLevel;
        SetActive(stage3Root, stageLevel == 3);
        SetActive(stage5Root, stageLevel == 5);
        SetActive(stage7Root, stageLevel == 7);

        TextMeshProUGUI[] texts = OrderLeftToRight(GetStageTexts(stageLevel));
        ApplyItemStyles(texts, deferredDoneCount);
        PlayRevealNewItem(texts, progress - 1);
        RefreshTipsFlags();
    }

    TextMeshProUGUI[] GetStageTexts(int stageLevel)
    {
        if (stageLevel == 3) return stage3ItemTexts;
        if (stageLevel == 5) return stage5ItemTexts;
        return stage7ItemTexts;
    }

    void PlayRevealNewItem(TextMeshProUGUI[] texts, int index)
    {
        KillRevealTween();
        if (texts == null || index < 0 || index >= texts.Length)
            return;

        TextMeshProUGUI tmp = texts[index];
        if (tmp == null)
            return;

        RectTransform number = tmp.rectTransform;
        RectTransform check = FindCheck(tmp);
        number.DOKill();
        if (check != null)
            check.DOKill();

        tmp.gameObject.SetActive(true);
        tmp.text = (index + 1).ToString();
        StyleNumber(tmp, true);
        number.localScale = Vector3.one;
        if (check != null)
        {
            check.gameObject.SetActive(true);
            check.localScale = Vector3.zero;
        }

        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        seq.Append(number.DOScale(1.18f, RevealPunchDuration).SetEase(Ease.OutQuad));
        seq.Append(number.DOScale(0f, RevealSettleDuration).SetEase(Ease.InQuad));
        if (check != null)
            seq.Join(check.DOScale(1f, RevealSettleDuration).SetEase(Ease.OutBack));
        seq.OnComplete(() =>
        {
            SetItemDone(tmp, check);
            if (texts != null && index + 1 < texts.Length)
                StyleNumber(texts[index + 1], true);
        });
        revealTween = seq;
    }

    static void SetItemDone(TextMeshProUGUI tmp, RectTransform check)
    {
        if (tmp != null)
        {
            tmp.rectTransform.DOKill();
            tmp.rectTransform.localScale = Vector3.one;
            tmp.gameObject.SetActive(false);
        }

        if (check == null)
            return;
        check.DOKill();
        check.gameObject.SetActive(true);
        check.localScale = Vector3.one;
    }

    public void ApplyMainMenu()
    {
        deferredReveal = false;
        PiggyBankState.ClearSettlementSnapshot();
        bool show = PiggyBankState.IsMainEntryVisible;
        gameObject.SetActive(show);
        if (!show)
            return;

        int stageLevel = PiggyBankState.CurrentStageLevel;
        int progress = PiggyBankState.AllStagesClaimedToday
            ? PiggyBankState.CurrentTarget
            : PiggyBankState.CurrentProgress;
        Apply(stageLevel, progress);
    }

    void Apply(int stageLevel, int doneCount)
    {
        displayStageLevel = stageLevel;

        SetActive(stage3Root, stageLevel == 3);
        SetActive(stage5Root, stageLevel == 5);
        SetActive(stage7Root, stageLevel == 7);

        KillRevealTween();
        ApplyItemStyles(OrderLeftToRight(GetStageTexts(stageLevel)), doneCount);
        RefreshTipsFlags();
    }

    static void SetActive(GameObject go, bool on)
    {
        if (go != null)
            go.SetActive(on);
    }

    static void ApplyItemStyles(TextMeshProUGUI[] texts, int doneCount)
    {
        if (texts == null)
            return;
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI tmp = texts[i];
            if (tmp == null)
                continue;

            tmp.DOKill();
            tmp.rectTransform.DOKill();
            tmp.text = (i + 1).ToString();
            StyleNumber(tmp, i == doneCount);
            tmp.rectTransform.localScale = Vector3.one;

            RectTransform check = FindCheck(tmp);
            if (check != null)
                check.DOKill();

            bool done = i < doneCount;
            tmp.gameObject.SetActive(!done);
            if (check == null)
                continue;
            check.gameObject.SetActive(done);
            check.localScale = Vector3.one;
        }
    }

    static TextMeshProUGUI[] OrderLeftToRight(TextMeshProUGUI[] texts)
    {
        if (texts == null || texts.Length <= 1)
            return texts;

        var ordered = new TextMeshProUGUI[texts.Length];
        Array.Copy(texts, ordered, texts.Length);
        Array.Sort(ordered, (a, b) => ItemX(a).CompareTo(ItemX(b)));
        return ordered;
    }

    static float ItemX(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return 0f;
        RectTransform item = tmp.transform.parent as RectTransform;
        return item != null ? item.anchoredPosition.x : tmp.rectTransform.anchoredPosition.x;
    }

    static void StyleNumber(TextMeshProUGUI tmp, bool current)
    {
        if (tmp == null)
            return;
        tmp.fontSize = current ? CurrentFontSize : IdleFontSize;
        tmp.color = current ? CurrentColor : IdleColor;
    }

    static readonly Vector2 AlertTailPivot = new Vector2(8.5f / 45f, 0f);
    bool alertPivotReady;
    Tween alertTween;

    void KillAlertTween()
    {
        if (alertTween != null && alertTween.IsActive())
            alertTween.Kill();
        alertTween = null;
    }

    /// <summary>
    /// 缩放锚点放在气泡尾巴尖（红圈）。只改一次轴心，并补偿位置，避免图标跳一下。
    /// </summary>
    void PrepareAlertTailPivot(RectTransform mark)
    {
        if (mark == null || alertPivotReady)
            return;

        Vector2 size = mark.rect.size;
        if (size.x < 1f || size.y < 1f)
            size = mark.sizeDelta;
        Vector2 delta = AlertTailPivot - mark.pivot;
        mark.pivot = AlertTailPivot;
        mark.anchoredPosition += new Vector2(delta.x * size.x, delta.y * size.y);
        alertPivotReady = true;
    }

    void PlayAlertScale(float scale, float duration, Ease ease)
    {
        RectTransform mark = ResolveAlert();
        if (mark == null)
            return;
        PrepareAlertTailPivot(mark);
        KillAlertTween();
        alertTween = mark
            .DOScale(scale, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetLink(mark.gameObject, LinkBehaviour.KillOnDestroy);
    }

    void RestoreAlertShown()
    {
        KillAlertTween();
        RectTransform mark = ResolveAlert();
        if (mark == null)
            return;
        PrepareAlertTailPivot(mark);
        mark.localScale = Vector3.one;
    }

    RectTransform ResolveAlert()
    {
        return alertMark;
    }

    static RectTransform FindCheck(TextMeshProUGUI tmp)
    {
        if (tmp == null || tmp.transform.parent == null)
            return null;
        Transform parent = tmp.transform.parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == "DH")
                return child as RectTransform;
        }

        return null;
    }

    void OnStageClicked()
    {
        if (tipsRoot == null)
            return;
        var clips = TileGameCore.AudioController.AudioClips;
        if (clips != null && clips.buttonSound != null)
            TileGameCore.AudioController.PlaySound(clips.buttonSound);
        if (tipsRoot.activeSelf)
            CloseTips(TipsCloseDuration);
        else
            OpenTips();
    }

    void OpenTips()
    {
        CacheTipsLayout();
        KillTipsTween();

        tipsRoot.transform.localScale = Vector3.one;
        tipsRoot.SetActive(true);
        RefreshTipsFlags();
        RefreshTimer();
        ApplyScalePivot(arrow5 != null ? arrow5.transform as RectTransform : null);

        tipsRoot.transform.localScale = Vector3.zero;
        tipsTween = tipsRoot.transform
            .DOScale(Vector3.one, TipsOpenDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(tipsRoot, LinkBehaviour.KillOnDestroy);
        PlayAlertScale(0f, TipsOpenDuration, Ease.OutQuad);
        ShowOutsideCatcher();
    }

    /// <summary>
    /// 全屏透明点击层盖在当前 Canvas 最上层。点在气泡（含箭头）上不关。
    /// 点进度条自己只关弹窗。点到活动、广告等其他按钮时，立刻关掉弹窗并把这次点击交过去。
    /// </summary>
    void EnsureOutsideCatcher()
    {
        if (outsideCatcher != null || !Application.isPlaying)
            return;

        var canvas = GetComponentInParent<Canvas>();
        var host = canvas != null ? canvas.transform as RectTransform : null;
        if (host == null)
            return;

        var go = new GameObject(
            "PiggyStageTipsOutside",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(EventTrigger));
        go.layer = host.gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(host, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        var image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        var trigger = go.GetComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(OnOutsideClick);
        trigger.triggers.Add(entry);

        go.SetActive(false);
        outsideCatcher = go;
    }

    void ShowOutsideCatcher()
    {
        EnsureOutsideCatcher();
        if (outsideCatcher == null)
            return;
        outsideCatcher.SetActive(true);
        outsideCatcher.transform.SetAsLastSibling();
    }

    void HideOutsideCatcher()
    {
        if (outsideCatcher != null)
            outsideCatcher.SetActive(false);
    }

    void OnOutsideClick(BaseEventData data)
    {
        var pointer = data as PointerEventData;
        if (pointer != null && IsPointerOnTips(pointer.position))
            return;

        GameObject hit = pointer != null ? RaycastBelowCatcher(pointer) : null;
        if (hit != null && !hit.transform.IsChildOf(transform))
        {
            CloseTips(TipsCloseDuration);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
            return;
        }

        CloseTips(TipsCloseDuration);
    }

    GameObject RaycastBelowCatcher(PointerEventData pointer)
    {
        if (EventSystem.current == null)
            return null;

        outsideHits.Clear();
        EventSystem.current.RaycastAll(pointer, outsideHits);
        for (int i = 0; i < outsideHits.Count; i++)
        {
            GameObject go = outsideHits[i].gameObject;
            if (go == null || IsCatcherOrTips(go))
                continue;
            return go;
        }

        return null;
    }

    bool IsCatcherOrTips(GameObject go)
    {
        if (outsideCatcher != null && (go == outsideCatcher || go.transform.IsChildOf(outsideCatcher.transform)))
            return true;
        if (tipsRoot != null && (go == tipsRoot || go.transform.IsChildOf(tipsRoot.transform)))
            return true;
        Transform arrow = GetActiveArrow();
        return arrow != null && (go.transform == arrow || go.transform.IsChildOf(arrow));
    }

    void CloseTipsImmediate()
    {
        KillTipsTween();
        if (tipsRoot != null)
        {
            tipsRoot.SetActive(false);
            tipsRoot.transform.localScale = Vector3.one;
            RestoreTipsLayout();
        }

        RestoreAlertShown();
        HideOutsideCatcher();
    }

    bool IsPointerOnTips(Vector2 screenPos)
    {
        var tipsRt = tipsRoot != null ? tipsRoot.transform as RectTransform : null;
        var cam = TipsEventCamera(tipsRt);
        if (ContainsScreen(tipsRt, screenPos, cam))
            return true;
        return ContainsScreen(GetActiveArrow() as RectTransform, screenPos, cam);
    }

    static Camera TipsEventCamera(RectTransform rt)
    {
        if (rt == null)
            return null;
        var canvas = rt.GetComponentInParent<Canvas>();
        if (canvas == null)
            return null;
        canvas = canvas.rootCanvas;
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    static bool ContainsScreen(RectTransform rt, Vector2 screenPos, Camera cam)
    {
        return rt != null
            && rt.gameObject.activeInHierarchy
            && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
    }

    void CloseTips(float duration)
    {
        KillTipsTween();
        if (tipsRoot == null || !tipsRoot.activeSelf)
            return;

        // 打开是 OutQuad 放大；InQuad 缩回是同一段曲线倒放。箭头保持预制体位置。
        tipsTween = tipsRoot.transform
            .DOScale(Vector3.zero, duration)
            .SetEase(Ease.InQuad)
            .SetUpdate(true)
            .SetLink(tipsRoot, LinkBehaviour.KillOnDestroy)
            .OnComplete(FinishCloseTips);
        PlayAlertScale(1f, duration, Ease.InQuad);
    }

    void FinishCloseTips()
    {
        tipsTween = null;
        if (tipsRoot == null)
            return;
        tipsRoot.SetActive(false);
        tipsRoot.transform.localScale = Vector3.one;
        RestoreTipsLayout();
        RestoreAlertShown();
        HideOutsideCatcher();
    }

    void RestoreTipsLayout()
    {
        if (!tipsLayoutCached || tipsRoot == null)
            return;
        var tipsRt = tipsRoot.transform as RectTransform;
        if (tipsRt == null)
            return;
        tipsRt.pivot = tipsDefaultPivot;
        tipsRt.anchoredPosition = tipsDefaultAnchoredPos;
    }

    /// <summary>缩放锚点固定在 5jiantou，不改箭头自身坐标。</summary>
    void ApplyScalePivot(RectTransform activeArrow)
    {
        var tipsRt = tipsRoot != null ? tipsRoot.transform as RectTransform : null;
        if (tipsRt == null || activeArrow == null)
            return;

        CacheTipsLayout();
        tipsRt.pivot = tipsDefaultPivot;
        tipsRt.anchoredPosition = tipsDefaultAnchoredPos;

        Rect rect = tipsRt.rect;
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        Vector3 arrowLocal = tipsRt.InverseTransformPoint(activeArrow.TransformPoint(activeArrow.rect.center));
        float pivotX = arrowLocal.x / rect.width + tipsDefaultPivot.x;
        float pivotY = arrowLocal.y / rect.height + tipsDefaultPivot.y;
        Vector2 target = new Vector2(pivotX, pivotY);
        if (Mathf.Approximately(tipsDefaultPivot.x, target.x) && Mathf.Approximately(tipsDefaultPivot.y, target.y))
            return;

        Vector2 size = tipsRt.rect.size;
        Vector2 delta = target - tipsRt.pivot;
        tipsRt.pivot = target;
        tipsRt.anchoredPosition += new Vector2(delta.x * size.x, delta.y * size.y);
    }

    Transform GetActiveArrow()
    {
        int focus = displayStageLevel > 0 ? displayStageLevel : PiggyBankState.DisplayStageLevel;
        if (focus == 3 && arrow3 != null) return arrow3.transform;
        if (focus == 5 && arrow5 != null) return arrow5.transform;
        if (focus == 7 && arrow7 != null) return arrow7.transform;
        if (arrow3 != null && arrow3.activeSelf) return arrow3.transform;
        if (arrow5 != null && arrow5.activeSelf) return arrow5.transform;
        if (arrow7 != null && arrow7.activeSelf) return arrow7.transform;
        return null;
    }

    void RefreshTipsFlags()
    {
        int focus = deferredReveal && displayStageLevel > 0
            ? displayStageLevel
            : PiggyBankState.DisplayStageLevel;
        if (!deferredReveal)
            displayStageLevel = focus;
        SetActive(arrow3, focus == 3);
        SetActive(arrow5, focus == 5);
        SetActive(arrow7, focus == 7);

        ApplyStageTip(3, tip3Finish, tip3NoFinish, tip3Lock);
        ApplyStageTip(5, tip5Finish, tip5NoFinish, tip5Lock);
        ApplyStageTip(7, tip7Finish, tip7NoFinish, tip7Lock);
    }

    static void ApplyStageTip(int stageLevel, GameObject finish, GameObject noFinish, GameObject lockGo)
    {
        bool completed = PiggyBankState.AllStagesClaimedToday
            || PiggyBankState.IsStageCompleted(stageLevel);
        bool isCurrent = !completed && (
            PiggyBankState.IsCurrentStage(stageLevel)
            || (PiggyBankState.SettlementHadWin
                && PiggyBankState.SettlementStageLevel == stageLevel));

        if (completed)
        {
            SetActive(finish, true);
            SetActive(noFinish, false);
            SetActive(lockGo, false);
        }
        else if (isCurrent)
        {
            SetActive(finish, false);
            SetActive(noFinish, true);
            SetActive(lockGo, false);
        }
        else
        {
            SetActive(finish, false);
            SetActive(noFinish, true);
            SetActive(lockGo, true);
        }

        SetStageCardGray(lockGo, !completed && !isCurrent);
    }

    static void SetStageCardGray(GameObject lockGo, bool locked)
    {
        if (lockGo == null)
            return;
        var card = lockGo.transform.parent != null
            ? lockGo.transform.parent.GetComponent<Image>()
            : null;
        if (card != null)
            card.color = locked ? LockedStageColor : Color.white;
    }

    void RefreshTimer()
    {
        if (timerTex == null)
            return;
        TimeSpan left = PiggyBankState.TimeUntilNextDay();
        timerTex.text = string.Format("{0:00}:{1:00}:{2:00}",
            (int)left.TotalHours, left.Minutes, left.Seconds);
    }
}
