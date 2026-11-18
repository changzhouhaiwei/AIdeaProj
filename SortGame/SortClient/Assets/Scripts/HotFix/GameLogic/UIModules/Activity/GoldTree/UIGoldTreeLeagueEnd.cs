using System;
using System.Collections;
using DG.Tweening;
using FishFramework;
using Spine;
using Spine.Unity;
using TMPro;
using TileGamePlay;
using UnityEngine;
using UnityEngine.UI;

public class UIGoldTreeLeagueEnd : UIGoldTreeLeagueEndBase
{
    const string PassUnlockAnim = "pass_unlock";
    const string PassLockShakeAnim = "pass_lock_shake";
    const float OpenPopDuration = 0.36f;
    const float OpenSettleDuration = 0.12f;
    const float TextRevealDuration = 0.5f;
    const float TextRevealDelayAfterFrontStart = 1.2f;
    const float TextRevealDelayMaintainTier = 0.2f;

    GoldTreeRankTierDogDisplay mTierDogDisplay;
    TextMeshProUGUI mContentDetailsText;
    RectTransform mContentDetailsRect;
    RectTransform mContinueTextRect;
    Transform mDogSwitchRoot;
    GameObject mFrontUpEff;
    GameObject mFrontUpEff2;
    GameObject mFrontDownEff;
    Coroutine mFrontEffectCoroutine;
    Coroutine mSettlementSequenceCoroutine;
    Coroutine mTextRevealCoroutine;
    DG.Tweening.Sequence mOpenAnimSequence;
    Tween mTextRevealTween;
    TrackEntry mActiveSpineTrackEntry;
    Spine.AnimationState.TrackEntryDelegate mActiveSpineCompleteHandler;
    bool mTextRevealTriggered;
    bool mCanClose;

    protected override void OnOpen()
    {
        base.OnOpen();
        mCanClose = false;
        mTextRevealTriggered = false;
        PrepareTextRevealNodes();
        RefreshTitleText();
        RefreshContentDetailsText();
        InitTierDogDisplay();

        var (oldTier, newTier) = GoldTreeRobotRankRewardHelper.ResolveLeagueEndTiers();
        PlaySettlementDogAnimation(oldTier, newTier);
    }

    protected override void PlayOpenAnim(Action onFinish = null)
    {
        animState = UIPanelAnimState.Opening;
        var animRoot = panelBehaviour.AnimNodeRt;
        if (animRoot == null)
        {
            animState = UIPanelAnimState.Idle;
            onFinish?.Invoke();
            return;
        }

        animRoot.localScale = new Vector3(0f, 0f, 1f);
        mOpenAnimSequence?.Kill();
        mOpenAnimSequence = DOTween.Sequence();
        mOpenAnimSequence.Append(animRoot.DOScale(new Vector3(1.05f, 1.05f, 1f), OpenPopDuration));
        mOpenAnimSequence.Append(animRoot.DOScale(Vector3.one, OpenSettleDuration));
        mOpenAnimSequence.SetUpdate(true);
        mOpenAnimSequence.SetLink(gameObject);
        mOpenAnimSequence.OnComplete(() =>
        {
            animState = UIPanelAnimState.Idle;
            onFinish?.Invoke();
            mOpenAnimSequence = null;
        });
    }

    void PrepareTextRevealNodes()
    {
        CacheTextRevealNodes();
        SetTextRevealNodeHidden(mContentDetailsRect);
        SetTextRevealNodeHidden(mContinueTextRect);
    }

    void CacheTextRevealNodes()
    {
        CacheContentDetailsTextRef();
        if (mContentDetailsRect == null && mContentDetailsText != null)
            mContentDetailsRect = mContentDetailsText.rectTransform;

        if (mContinueTextRect != null)
            return;

        var continueTran = rectTransform.Find("backBtn/continueText")
            ?? rectTransform.Find("continueText");
        if (continueTran != null)
            mContinueTextRect = continueTran as RectTransform;
    }

    static void SetTextRevealNodeHidden(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.localScale = Vector3.zero;
        rect.gameObject.SetActiveEx(false);
    }

    void RefreshTitleText()
    {
        if (textTMP == null)
            return;

        textTMP.text = GoldTreeRobotRankRewardHelper.GetPlayerLastSeasonLeagueRankEndTitle();
    }

    void CacheContentDetailsTextRef()
    {
        if (mContentDetailsText != null)
            return;

        var textTran = rectTransform.Find("nineBg/ContentDetailsText")
            ?? rectTransform.Find("ContentDetailsText");
        if (textTran != null)
            mContentDetailsText = textTran.GetComponent<TextMeshProUGUI>();
    }

    void RefreshContentDetailsText()
    {
        CacheContentDetailsTextRef();
        LocalizedTMPRefresher.SetText(
            mContentDetailsText,
            GoldTreeRobotRankRewardHelper.GetPlayerLeagueRankEndDetailsText);
    }

    void InitTierDogDisplay()
    {
        mTierDogDisplay ??= new GoldTreeRankTierDogDisplay();

        mDogSwitchRoot = rectTransform.Find("nineBg/Image_Root/dogSwitch")
            ?? rectTransform.Find("dogSwitch")
            ?? rectTransform;
        mTierDogDisplay.Bind(
            mDogSwitchRoot,
            shiftDuration: ResolveDogShiftDuration(),
            useLeagueEndPedestal: true);
    }

    float ResolveDogShiftDuration()
    {
        var settings = viewBehaviour.GetComponent<GoldTreeRankTierDogDisplaySettings>();
        return settings != null
            ? settings.ShiftDuration
            : GoldTreeRankTierDogDisplay.DefaultShiftDuration;
    }

    void PlaySettlementDogAnimation(int oldTier, int newTier)
    {
        CacheTierEffectNodes();
        ResetTierEffects();
        StopSettlementSequenceCoroutine();

        mTierDogDisplay?.PlaySettlementTransition(oldTier, newTier, true, () =>
        {
            if (viewBehaviour == null)
                return;

            StopSettlementSequenceCoroutine();
            mSettlementSequenceCoroutine = viewBehaviour.StartCoroutine(
                SettlementSequenceCoroutine(oldTier, newTier));
        });
    }

    IEnumerator SettlementSequenceCoroutine(int oldTier, int newTier)
    {
        HideLockAndSpineBelowCurrentTier(newTier);

        int delta = newTier - oldTier;
        if (delta > 0)
        {
            PlaySettlementSound(TileGameCore.AudioController.AudioClips?.rank_promotion);
            var rightSlot = GetDogSlot("right");
            PrepareSpinePlayback(rightSlot);
            yield return PlaySpineOnce(GetPassSpine(rightSlot), PassUnlockAnim);
            PlayRankUpEffects();
        }
        else if (delta < 0)
        {
            PlaySettlementSound(TileGameCore.AudioController.AudioClips?.rank_demotion);
            PlayRankDownEffects();

            var rightSlot = GetDogSlot("right");
            PrepareSpinePlayback(rightSlot);
            yield return PlaySpineOnce(GetPassSpine(rightSlot), PassUnlockAnim);

            var middleSlot = GetDogSlot("middle");
            PrepareSpinePlayback(middleSlot, hideLock: false);
            yield return PlaySpineOnce(GetPassSpine(middleSlot), PassLockShakeAnim);
        }
        else
        {
            PlaySettlementSound(TileGameCore.AudioController.AudioClips?.rank_maintain);
            HideMaintainTierLocks();
            ScheduleTextRevealAfterFrontStart(TextRevealDelayMaintainTier);
        }

        mSettlementSequenceCoroutine = null;
        TryEnableClose();
    }

    static void PlaySettlementSound(AudioClip clip)
    {
        if (clip == null)
            return;
        TileGameCore.AudioController.PlaySound(clip);
    }

    static int GetSlotTierId(string slotName, int currentTier)
    {
        return slotName switch
        {
            "left" => currentTier - 1,
            "middle" => currentTier,
            "right" => currentTier + 1,
            _ => -1,
        };
    }

    void HideLockAndSpineBelowCurrentTier(int currentTier)
    {
        HideSlotLockAndSpineIfBelowTier("left", currentTier);
        HideSlotLockAndSpineIfBelowTier("middle", currentTier);
        HideSlotLockAndSpineIfBelowTier("right", currentTier);
    }

    void HideSlotLockAndSpineIfBelowTier(string slotName, int currentTier)
    {
        int tierId = GetSlotTierId(slotName, currentTier);
        if (!GoldTreeRobotRankRewardHelper.IsValidTierId(tierId) || tierId >= currentTier)
            return;

        HideSlotLockAndSpine(GetDogSlot(slotName));
    }

    Transform GetDogSlot(string slotName)
    {
        if (mDogSwitchRoot == null || string.IsNullOrEmpty(slotName))
            return null;

        return mDogSwitchRoot.Find(slotName);
    }

    static SkeletonGraphic GetPassSpine(Transform slot)
    {
        if (slot == null)
            return null;

        var spineTran = slot.Find("SkeletonGraphic (Pass_unlock)");
        return spineTran != null ? spineTran.GetComponent<SkeletonGraphic>() : null;
    }

    static GameObject GetLockNode(Transform slot)
    {
        if (slot == null)
            return null;

        var lockTran = slot.Find("lock");
        return lockTran != null ? lockTran.gameObject : null;
    }

    static void HideSlotLockAndSpine(Transform slot)
    {
        if (slot == null || !slot.gameObject.activeSelf)
            return;

        SetLockVisible(slot, false);
        var spine = GetPassSpine(slot);
        spine?.gameObject.SetActiveEx(false);
    }

    static void PrepareSpinePlayback(Transform slot, bool hideLock = true)
    {
        if (slot == null)
            return;

        if (hideLock)
            SetLockVisible(slot, false);

        var spine = GetPassSpine(slot);
        spine?.gameObject.SetActiveEx(true);
    }

    static void SetLockVisible(Transform slot, bool visible)
    {
        var lockNode = GetLockNode(slot);
        lockNode?.SetActiveEx(visible);
    }

    void HideMaintainTierLocks()
    {
        HideSlotLockAndSpine(GetDogSlot("left"));
        HideSlotLockAndSpine(GetDogSlot("middle"));
    }

    IEnumerator PlaySpineOnce(SkeletonGraphic spine, string animName)
    {
        ClearActiveSpineCallback();
        if (spine == null || string.IsNullOrEmpty(animName))
            yield break;

        spine.gameObject.SetActiveEx(true);
        var animationState = spine.AnimationState;
        if (animationState == null)
            yield break;

        animationState.ClearTrack(0);
        var trackEntry = animationState.SetAnimation(0, animName, false);
        if (trackEntry == null)
        {
            Debug.LogWarning($"[UIGoldTreeLeagueEnd] Spine animation not found: {animName}");
            yield break;
        }

        bool completed = false;
        mActiveSpineTrackEntry = trackEntry;
        mActiveSpineCompleteHandler = _ => { completed = true; };
        trackEntry.Complete += mActiveSpineCompleteHandler;
        spine.Update(0f);

        float timeout = ResolveSpineAnimTimeout(spine, animName, trackEntry);
        float elapsed = 0f;
        while (!completed && spine != null)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
                break;

            yield return null;
        }

        ClearActiveSpineCallback();
    }

    static float ResolveSpineAnimTimeout(SkeletonGraphic spine, string animName, TrackEntry trackEntry)
    {
        float duration = trackEntry.Animation?.Duration ?? 0f;
        if (duration <= 0f)
        {
            var anim = spine.SkeletonDataAsset?.GetSkeletonData(false)?.FindAnimation(animName);
            duration = anim?.Duration ?? 0f;
        }

        return duration > 0f ? duration + 0.15f : 2f;
    }

    void ClearActiveSpineCallback()
    {
        if (mActiveSpineTrackEntry != null && mActiveSpineCompleteHandler != null)
            mActiveSpineTrackEntry.Complete -= mActiveSpineCompleteHandler;

        mActiveSpineTrackEntry = null;
        mActiveSpineCompleteHandler = null;
    }

    void CacheTierEffectNodes()
    {
        if (mFrontUpEff != null)
            return;

        var root = rectTransform.Find("nineBg") ?? rectTransform;
        mFrontUpEff = root.Find("Front_Up_Eff")?.gameObject;
        mFrontUpEff2 = root.Find("Front_Up_Eff2")?.gameObject;
        mFrontDownEff = root.Find("Front_Down_Eff")?.gameObject;
    }

    void ResetTierEffects()
    {
        StopFrontEffectCoroutine();
        SetEffectActive(mFrontUpEff, false);
        SetEffectActive(mFrontUpEff2, false);
        SetEffectActive(mFrontDownEff, false);
    }

    void PlayRankUpEffects()
    {
        if (mFrontUpEff != null || mFrontUpEff2 != null)
        {
            SetEffectActive(mFrontUpEff, true);
            SetEffectActive(mFrontUpEff2, true);
            PlayParticles(mFrontUpEff);
            PlayParticles(mFrontUpEff2);
            mFrontEffectCoroutine = viewBehaviour.StartCoroutine(HideFrontUpEffectsWhenFinished());
        }

        ScheduleTextRevealAfterFrontStart();
    }

    IEnumerator HideFrontUpEffectsWhenFinished()
    {
        // 以 Front_Up_Eff 粒子时长为准，同步关闭 Front_Up_Eff2
        var primary = mFrontUpEff != null ? mFrontUpEff : mFrontUpEff2;
        yield return HideEffectWhenParticlesFinished(primary, onlyNonLooping: true);
        SetEffectActive(mFrontUpEff, false);
        SetEffectActive(mFrontUpEff2, false);
    }

    void PlayRankDownEffects()
    {
        if (mFrontDownEff == null)
        {
            ScheduleTextRevealAfterFrontStart();
            return;
        }

        StopFrontEffectCoroutine();
        SetEffectActive(mFrontDownEff, true);
        mFrontDownEff.transform.SetAsLastSibling();
        ScheduleTextRevealAfterFrontStart();
        mFrontEffectCoroutine = viewBehaviour.StartCoroutine(PlayRankDownEffectsCoroutine());
    }

    IEnumerator PlayRankDownEffectsCoroutine()
    {
        yield return null;
        PlayParticles(mFrontDownEff);
        yield return HideEffectWhenParticlesFinished(mFrontDownEff, onlyNonLooping: true);
    }

    void ScheduleTextRevealAfterFrontStart(float delay = TextRevealDelayAfterFrontStart)
    {
        if (mTextRevealTriggered || viewBehaviour == null)
            return;

        StopTextRevealCoroutine();
        mTextRevealCoroutine = viewBehaviour.StartCoroutine(RevealTextsAfterFrontStartDelay(delay));
    }

    IEnumerator RevealTextsAfterFrontStartDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (mTextRevealTriggered || gameObject == null || !gameObject.activeInHierarchy)
            yield break;

        mTextRevealTriggered = true;
        yield return RevealTextNode(mContentDetailsRect);
        yield return RevealTextNode(mContinueTextRect);
        mTextRevealCoroutine = null;
        TryEnableClose();
    }

    IEnumerator RevealTextNode(RectTransform rect)
    {
        if (rect == null)
            yield break;

        rect.gameObject.SetActiveEx(true);
        rect.localScale = Vector3.zero;

        bool completed = false;
        mTextRevealTween?.Kill();
        mTextRevealTween = rect.DOScale(1f, TextRevealDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetLink(gameObject)
            .OnComplete(() => completed = true);

        while (!completed && rect != null)
            yield return null;
    }

    static void SetEffectActive(GameObject effectRoot, bool active)
    {
        effectRoot?.SetActiveEx(active);
    }

    static void PlayParticles(GameObject effectRoot)
    {
        if (effectRoot == null)
            return;

        for (int i = 0; i < effectRoot.transform.childCount; i++)
            effectRoot.transform.GetChild(i).gameObject.SetActiveEx(true);

        var particleSystems = effectRoot.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            ps.gameObject.SetActiveEx(true);
            ps.Clear(true);
            ps.Play(true);
        }
    }

    IEnumerator HideEffectWhenParticlesFinished(GameObject effectRoot, bool onlyNonLooping)
    {
        if (effectRoot == null)
            yield break;

        yield return null;
        yield return null;

        float maxDuration = GetMaxParticleDuration(effectRoot, onlyNonLooping);
        float elapsed = 0f;
        bool hasBeenAlive = false;

        while (effectRoot.activeSelf)
        {
            bool alive = AreParticlesAlive(effectRoot, onlyNonLooping);
            if (alive)
                hasBeenAlive = true;

            if (hasBeenAlive && !alive)
                break;

            elapsed += Time.deltaTime;
            if (!hasBeenAlive && elapsed >= 0.5f)
                break;

            if (elapsed >= maxDuration + 1f)
                break;

            yield return null;
        }

        if (effectRoot != null)
            effectRoot.SetActiveEx(false);

        mFrontEffectCoroutine = null;
    }

    static float GetMaxParticleDuration(GameObject effectRoot, bool onlyNonLooping)
    {
        float maxDuration = 0f;
        var particleSystems = effectRoot.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            if (onlyNonLooping && ps.main.loop)
                continue;

            float duration = ps.main.duration + ps.main.startLifetime.constantMax + ps.main.startDelay.constantMax;
            if (duration > maxDuration)
                maxDuration = duration;
        }

        return maxDuration > 0f ? maxDuration : 5f;
    }

    static bool AreParticlesAlive(GameObject effectRoot, bool onlyNonLooping)
    {
        var particleSystems = effectRoot.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            if (!ps.gameObject.activeInHierarchy)
                continue;

            if (onlyNonLooping && ps.main.loop)
                continue;

            if (ps.IsAlive(true))
                return true;
        }

        return false;
    }

    void StopFrontEffectCoroutine()
    {
        if (mFrontEffectCoroutine == null || viewBehaviour == null)
            return;

        viewBehaviour.StopCoroutine(mFrontEffectCoroutine);
        mFrontEffectCoroutine = null;
    }

    void StopTextRevealCoroutine()
    {
        if (mTextRevealCoroutine == null || viewBehaviour == null)
            return;

        viewBehaviour.StopCoroutine(mTextRevealCoroutine);
        mTextRevealCoroutine = null;
    }

    void StopSettlementSequenceCoroutine()
    {
        if (mSettlementSequenceCoroutine == null || viewBehaviour == null)
            return;

        viewBehaviour.StopCoroutine(mSettlementSequenceCoroutine);
        mSettlementSequenceCoroutine = null;
        ClearActiveSpineCallback();
    }

    void TryEnableClose()
    {
        if (mCanClose)
            return;

        if (mSettlementSequenceCoroutine != null)
            return;

        if (!mTextRevealTriggered || mTextRevealCoroutine != null)
            return;

        mCanClose = true;
    }

    protected override void OnClicked(Button button)
    {
        if (button == backBtn)
        {
            CloseAndRefreshRankView();
        }
    }

    protected override bool OnEscButtonPressed()
    {
        if (!mCanClose)
            return true;

        CloseAndRefreshRankView();
        return true;
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        if (!mCanClose)
            return;

        CloseAndRefreshRankView();
    }

    void CloseAndRefreshRankView()
    {
        if (!mCanClose)
            return;

        CloseSelf(() =>
        {
            UIRankView.Instance?.RefreshAfterLeagueTierChanged();
            GameData.GoldTreeActData?.NotifySettlementFlowClosed();
        });
    }

    protected override void OnDestroyed()
    {
        mOpenAnimSequence?.Kill();
        mOpenAnimSequence = null;
        mTextRevealTween?.Kill();
        mTextRevealTween = null;
        StopSettlementSequenceCoroutine();
        StopFrontEffectCoroutine();
        StopTextRevealCoroutine();
        ResetTierEffects();
        mTierDogDisplay?.Dispose();
        mTierDogDisplay = null;
        mContentDetailsText = null;
        mContentDetailsRect = null;
        mContinueTextRect = null;
        mDogSwitchRoot = null;
        mFrontUpEff = null;
        mFrontUpEff2 = null;
        mFrontDownEff = null;
        base.OnDestroyed();
    }
}
