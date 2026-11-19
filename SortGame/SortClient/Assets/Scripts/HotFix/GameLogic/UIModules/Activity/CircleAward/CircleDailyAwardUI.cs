using System;
using System.Collections.Generic;
using Core.Tools.MethodExtension;
using DG.Tweening;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;
using FishXSortPlay;


public class CircleDailyAwardUI : CricleDailyAwardUIBase
{
    const string WheelIdleAnim = "DJ_Idle";
    const string WheelIntenseAnim = "DJ_Intense";

    int ExtraLaps => mSpinSettings != null ? mSpinSettings.ExtraLaps : CircleDailyAwardSpinSettings.DefaultExtraLaps;
    float SpineIntenseDelay => mWheelSpineSettings != null
        ? mWheelSpineSettings.SpineIntenseDelay
        : CircleDailyAwardWheelSpineSettings.DefaultSpineIntenseDelay;
    float TrailPrevAlpha => mSpinSettings != null ? mSpinSettings.TrailPrevAlpha : CircleDailyAwardSpinSettings.DefaultTrailPrevAlpha;
    float TrailPrevPrevAlpha => mSpinSettings != null ? mSpinSettings.TrailPrevPrevAlpha : CircleDailyAwardSpinSettings.DefaultTrailPrevPrevAlpha;
    float WinHoldTime => mSpinSettings != null ? mSpinSettings.WinHoldTime : CircleDailyAwardSpinSettings.DefaultWinHoldTime;

    class AwardCellView
    {
        public Transform Root;
        public Image Icon;
        public TextMeshProUGUI CountText;
    }

    class SpinItemView
    {
        public int Id;
        public Transform Root;
        public GameObject Selected;
        public Image SelectedImage;
        public Color SelectedBaseColor = Color.white;
        public GameObject UpSet;
        public Tween UpSetTween;
        public readonly List<AwardCellView> Cells = new();
    }

    private cfg.Game.ActivityCircleAwardCfg mCurAwardData;
    private CircleDailyAwardSpinSettings mSpinSettings;
    private CircleDailyAwardWheelSpineSettings mWheelSpineSettings;
    private SkeletonGraphic mWheelSpine;
    private CoinPanelView currencyComp;
    private TextMeshProUGUI adBtnText;
    private TextMeshProUGUI normalBtnText;
    private TextMeshProUGUI countTimeText;
    private readonly List<SpinItemView> mSpinItems = new();
    private Sequence mSpinSeq;
    private int mHighlightIndex = -1;
    private int mTrailAge;
    private bool mBusy;
    private bool mDuckedMainCityBgm;

    string dailyAdKey = "DailyCircleReward";

    int mMaxCnt = ActivityData.CircleMaxDailySpin;


    protected override void OnOpen()
    {
        base.OnOpen();
        BindRuntimeRefs();
        SetTopButtonEnabled(closeBtn, true);
        SetTopButtonEnabled(ruleBtn, true);
        currencyComp?.Activate();
        currencyComp?.SetupPanel();

        InitSpinItems();
        RefreshAwards();
        PlayWheelIdle();
        XSortGameCore.AudioController.VolumeChanged += OnAudioVolumeChanged;
        PlaySpinBg();
        DuckMainCityBgm();
        Interval(1f, UpdateTime);
        UpdateTime(0f);

        GameEvent.AddEventListener((int)EventNameDef.ADCircleDailyGetAward, CbAdSuc);
    }

    protected override void OnDestroying()
    {
        base.OnDestroying();
        KillSpinSeq();
        KillAllUpSetTweens();
        XSortGameCore.AudioController.VolumeChanged -= OnAudioVolumeChanged;
        StopSpinBg();
        RestoreMainCityBgm();
        GameEvent.RemoveEventListener((int)EventNameDef.ADCircleDailyGetAward, CbAdSuc);
    }

    void BindRuntimeRefs()
    {
        mSpinSettings = viewBehaviour != null ? viewBehaviour.GetComponent<CircleDailyAwardSpinSettings>() : null;
        currencyComp = gameObject != null ? gameObject.GetComponentInChildren<CoinPanelView>(true) : null;
        var animator = gameObject != null ? gameObject.GetComponent<Animator>() : null;
        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        BindWheelSpine();
        if (watchBtn == null)
        {
            return;
        }

        adBtnText = watchBtn.transform.Find("adBtnText")?.GetComponent<TextMeshProUGUI>();
        normalBtnText = watchBtn.transform.Find("normalBtnText")?.GetComponent<TextMeshProUGUI>();
        countTimeText = watchBtn.transform.Find("countTimeText")?.GetComponent<TextMeshProUGUI>();
    }

    void BindWheelSpine()
    {
        if (gameObject == null)
        {
            mWheelSpine = null;
            mWheelSpineSettings = null;
            return;
        }

        var wheelTf = FindDeepChild(gameObject.transform, "Wheel (wheel_new)")
                      ?? FindDeepChild(gameObject.transform, "Wheel");
        mWheelSpine = wheelTf != null
            ? wheelTf.GetComponent<SkeletonGraphic>()
            : gameObject.GetComponentInChildren<SkeletonGraphic>(true);
        mWheelSpineSettings = wheelTf != null
            ? wheelTf.GetComponent<CircleDailyAwardWheelSpineSettings>()
            : null;
        if (mWheelSpineSettings == null && mWheelSpine != null)
        {
            mWheelSpineSettings = mWheelSpine.GetComponent<CircleDailyAwardWheelSpineSettings>();
        }

        if (mWheelSpineSettings == null)
        {
            mWheelSpineSettings = gameObject.GetComponentInChildren<CircleDailyAwardWheelSpineSettings>(true);
        }
    }

    void PlayWheelIdle()
    {
        if (mWheelSpine == null || mWheelSpine.AnimationState == null)
        {
            return;
        }

        mWheelSpine.AnimationState.ClearTrack(0);
        mWheelSpine.AnimationState.SetAnimation(0, WheelIdleAnim, true);
        mWheelSpine.Update(0f);
    }

    static void PlaySpinBg()
    {
        var clip = XSortGameCore.AudioController.AudioClips?.spin_bg;
        if (clip == null)
            return;
        XSortGameCore.AudioController.PlayLoopSound(clip, 1f, XSortGameCore.AudioType.Music);
    }

    static void StopSpinBg()
    {
        var clip = XSortGameCore.AudioController.AudioClips?.spin_bg;
        if (clip == null)
            return;
        XSortGameCore.AudioController.StopSound(clip);
    }

    void OnAudioVolumeChanged(XSortGameCore.AudioType audioType, float volume)
    {
        if (audioType != XSortGameCore.AudioType.Music)
            return;
        if (volume > 0f)
            PlaySpinBg();
        else
            StopSpinBg();
    }

    const float MainCityBgmDuckScale = 0.1f;

    void DuckMainCityBgm()
    {
        if (mDuckedMainCityBgm)
            return;

        var music = XSortGameCore.MusicEmitter.ActiveMusicSource;
        if (music == null || music.AudioSource == null)
            return;

        music.SetVolume(MainCityBgmDuckScale);
        mDuckedMainCityBgm = true;
    }

    void RestoreMainCityBgm()
    {
        if (!mDuckedMainCityBgm)
            return;

        mDuckedMainCityBgm = false;
        var music = XSortGameCore.MusicEmitter.ActiveMusicSource;
        if (music != null && music.AudioSource != null)
            music.SetVolume(1f);
    }

    void PlayWheelIntenseThenIdle()
    {
        if (mWheelSpine == null || mWheelSpine.AnimationState == null)
        {
            return;
        }

        mWheelSpine.AnimationState.ClearTrack(0);
        mWheelSpine.AnimationState.SetAnimation(0, WheelIntenseAnim, false);
        mWheelSpine.AnimationState.AddAnimation(0, WheelIdleAnim, true, 0f);
        mWheelSpine.Update(0f);
    }

    void UpdateTime(float dt)
    {
        if (countTimeText == null)
        {
            return;
        }

        var nextTime = GPUserDataManager.Instance.UserData.GetSecondsUntilNext5AM();
        countTimeText.text = Utility.GetHourMinSec(nextTime);
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
    }

    void SetBusy(bool busy)
    {
        mBusy = busy;
        SetButtonInputEnabled(watchBtn, !busy);
        SetTopButtonEnabled(closeBtn, !busy);
        SetTopButtonEnabled(ruleBtn, !busy);
    }

    static void SetTopButtonEnabled(TButton btn, bool enabled)
    {
        if (btn == null)
        {
            return;
        }

        btn.disableToGray = false;
        btn.interactable = enabled;

        var graphic = btn.targetGraphic as Image ?? btn.image;
        if (graphic != null)
        {
            graphic.material = null;
        }
    }

    static void SetButtonInputEnabled(TButton btn, bool enabled)
    {
        if (btn == null)
        {
            return;
        }

        btn.interactable = true;
        if (btn.image != null)
        {
            btn.image.material = null;
        }

        var canvasGroup = btn.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = btn.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = enabled;
        canvasGroup.interactable = true;
    }

    bool GetTouchEnable()
    {
        return !mBusy;
    }

    void InitSpinItems()
    {
        mSpinItems.Clear();
        if (gameObject == null)
        {
            return;
        }

        var cfgs = GameData.Activity.GetActivityCircleAwardCfgs();
        for (int id = 1; id <= ActivityData.CircleSlotCount; id++)
        {
            var root = FindDeepChild(gameObject.transform, "SpinItem (" + id + ")");
            if (root == null)
            {
                continue;
            }

            var selectedTf = FindDeepChild(root, "Selected");
            var selectedImage = selectedTf != null ? selectedTf.GetComponent<Image>() : null;
            var upSetTf = FindChildByTrimmedName(root, "UI_UpSet");
            var view = new SpinItemView
            {
                Id = id,
                Root = root,
                Selected = selectedTf != null ? selectedTf.gameObject : null,
                SelectedImage = selectedImage,
                SelectedBaseColor = selectedImage != null ? selectedImage.color : Color.white,
                UpSet = upSetTf != null ? upSetTf.gameObject : null,
            };
            CollectAwardCells(root, view.Cells);

            SetSelectedVisible(view, false, 0f);
            SetUpSetVisible(view, false);
            BindAwardCells(view, FindCfg(cfgs, id));

            mSpinItems.Add(view);
        }

        ClearHighlight();
    }

    static cfg.Game.ActivityCircleAwardCfg FindCfg(List<cfg.Game.ActivityCircleAwardCfg> cfgs, int id)
    {
        if (cfgs == null)
        {
            return null;
        }

        for (int i = 0; i < cfgs.Count; i++)
        {
            if (cfgs[i].Id == id)
            {
                return cfgs[i];
            }
        }

        return null;
    }

    void RefreshAwards()
    {
        watchBtn.SetActiveEx(true);

        int totalCnt = GPUserDataManager.Instance.UserData.GetTotalCircleAwardCnt();

        normalBtnText.SetActiveEx(totalCnt == 0);
        adBtnText.SetActiveEx(totalCnt > 0 && mMaxCnt - totalCnt > 0);
        countTimeText.SetActiveEx(totalCnt >= mMaxCnt);

        int restCnt = mMaxCnt - totalCnt;
        if (adBtnText != null)
        {
            adBtnText.text = I2LocalizeMgr.GetString("UI_CarouselActivity_MainPanelButton" + (6 - restCnt));
        }

        if (totalCnt > 0 && mMaxCnt - totalCnt > 0)
        {
            GameBAData.ExcuteAdImpress(dailyAdKey);
        }

        var cfgs = GameData.Activity.GetActivityCircleAwardCfgs();
        for (int i = 0; i < mSpinItems.Count; i++)
        {
            var view = mSpinItems[i];
            BindAwardCells(view, FindCfg(cfgs, view.Id));
        }
    }

    static void CollectAwardCells(Transform root, List<AwardCellView> cells)
    {
        var cellRoots = new List<Transform>();
        CollectAwardCellRoots(root, cellRoots);
        cellRoots.Sort((a, b) =>
        {
            var ra = a as RectTransform;
            var rb = b as RectTransform;
            float ax = ra != null ? ra.anchoredPosition.x : a.localPosition.x;
            float bx = rb != null ? rb.anchoredPosition.x : b.localPosition.x;
            return ax.CompareTo(bx);
        });

        for (int i = 0; i < cellRoots.Count; i++)
        {
            var cellRoot = cellRoots[i];
            var iconTf = FindDeepChild(cellRoot, "icon");
            var textTf = FindDeepChild(cellRoot, "Text (TMP)");
            cells.Add(new AwardCellView
            {
                Root = cellRoot,
                Icon = iconTf != null ? iconTf.GetComponent<Image>() : null,
                CountText = textTf != null ? textTf.GetComponent<TextMeshProUGUI>() : null,
            });
        }
    }

    static void CollectAwardCellRoots(Transform root, List<Transform> cellRoots)
    {
        if (root == null)
        {
            return;
        }

        if (root.name.StartsWith("CricleDailyAwardCell"))
        {
            cellRoots.Add(root);
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            CollectAwardCellRoots(root.GetChild(i), cellRoots);
        }
    }

    void BindAwardCells(SpinItemView view, cfg.Game.ActivityCircleAwardCfg awardCfg)
    {
        if (view == null)
        {
            return;
        }

        int awardCount = awardCfg != null && awardCfg.Award != null ? awardCfg.Award.Count : 0;
        for (int i = 0; i < view.Cells.Count; i++)
        {
            var cell = view.Cells[i];
            int itemId = 0;
            int num = 0;
            bool show = i < awardCount
                && ActivityData.TryParseCircleAward(awardCfg.Award[i], out itemId, out num);
            if (cell.Root != null)
            {
                cell.Root.SetActiveEx(show);
            }

            if (!show)
            {
                continue;
            }

            if (cell.Icon != null)
            {
                cell.Icon.sprite = LoadSprite("LevelAward", "item_" + itemId);
            }

            if (cell.CountText != null)
            {
                int cnt = GPUserDataManager.Instance.UserData.GetCircleAwardCount(view.Id - 1);
                int chen = cnt > 0 ? 2 : 1;
                cell.CountText.text = "x" + num * chen;
            }
        }
    }

    protected override void OnClicked(Button button)
    {
        if (button == ruleBtn)
        {
            if (!GetTouchEnable())
            {
                return;
            }

            GameModule.UI.OpenPanel<CircleDailyAwardRuleUI>();
        }
        else if (button == closeBtn)
        {
            if (GetTouchEnable())
            {
                CloseSelf();
            }
        }
        else if (button == watchBtn)
        {
            if (!GetTouchEnable())
            {
                return;
            }

            XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.buttonSoundSpread);

            ReqCircleAward();
        }
    }

    protected override bool OnEscButtonPressed()
    {
        if (GetTouchEnable())
        {
            CloseSelf();
        }
        return true;
    }

    void ReqCircleAward()
    {
        if (mBusy)
        {
            return;
        }

        int awardCnt = GPUserDataManager.Instance.UserData.GetTotalCircleAwardCnt();
        if (awardCnt >= mMaxCnt)
        {
            return;
        }

        mCurAwardData = GameData.Activity.GetRandomAwardByCnt(awardCnt);
        if (mCurAwardData == null)
        {
            return;
        }

        if (awardCnt == 0)
        {
            PlaySpinAnim();
        }
        else
        {
            PlayRewardedAd();
        }
    }

    void PlayRewardedAd()
    {
        if (!AdsPlayStrategy.CheckRewardAdsPlay(dailyAdKey))
        {
            return;
        }

#if !UNITY_EDITOR
        XSortGameCore.UIController.ShowPage<UIAdsLoading>();
        GPSDKMgr.Instance.PlayRewardedAd(dailyAdKey, 6);
#else
        PlaySpinAnim();
#endif
    }

    void CbAdSuc()
    {
        PlaySpinAnim();
    }

    void PlaySpinAnim()
    {
        if (mCurAwardData == null)
        {
            SetBusy(false);
            return;
        }

        SetBusy(true);
        KillSpinSeq();
        ClearHighlight();

        int slotCount = Mathf.Max(1, mSpinItems.Count);
        int targetIndex = GetSpinIndexById(mCurAwardData.Id);
        if (targetIndex < 0)
        {
            GrantAward();
            return;
        }

        int startIndex = GetSpinIndexById(2);
        if (startIndex < 0)
        {
            startIndex = 0;
        }

        int forward = (targetIndex - startIndex + slotCount) % slotCount;
        int steps = ExtraLaps * slotCount + forward;
        if (steps <= 0)
        {
            steps = slotCount;
        }

        var delays = BuildStepDelays(steps);
        // 同帧立刻亮第一格，避免 ClearHighlight 后等 DOTween 首帧回调造成「愣神」。
        int cursor = (startIndex + 1) % slotCount;
        SetHighlight(cursor);

        mSpinSeq = DOTween.Sequence();
        mSpinSeq.SetUpdate(true);
        mSpinSeq.SetLink(gameObject);

        // 必须先 Append 跳格时间轴，再 Insert Spine。
        // 若先 InsertCallback(0.6)，空 Sequence 会被拉长到 0.6s，Append 全部挤到后面，表现为起步愣神。
        for (int i = 1; i < steps; i++)
        {
            mSpinSeq.AppendInterval(delays[i - 1]);
            cursor = (cursor + 1) % slotCount;
            int nextIndex = cursor;
            mSpinSeq.AppendCallback(() => SetHighlight(nextIndex));
        }

        mSpinSeq.AppendInterval(delays[steps - 1]);

        float trailBeat = delays.Count > 0 ? delays[delays.Count - 1] : CircleDailyAwardSpinSettings.DefaultFallbackInterval;
        int winIndex = targetIndex;
        mSpinSeq.AppendCallback(() => RefreshHighlightTrail(winIndex, 1));
        mSpinSeq.AppendInterval(trailBeat);
        mSpinSeq.AppendCallback(() => RefreshHighlightTrail(winIndex, 0));
        mSpinSeq.AppendInterval(WinHoldTime);
        mSpinSeq.OnComplete(GrantAward);

        float intenseDelay = SpineIntenseDelay;
        if (intenseDelay <= 0f)
        {
            PlayWheelIntenseThenIdle();
        }
        else
        {
            mSpinSeq.InsertCallback(intenseDelay, PlayWheelIntenseThenIdle);
        }
    }

    List<float> BuildStepDelays(int steps)
    {
        var delays = new List<float>(steps);
        for (int i = 0; i < steps; i++)
        {
            delays.Add(GetStepInterval(i, steps));
        }

        return delays;
    }

    float GetStepInterval(int stepIndex, int steps)
    {
        if (mSpinSettings != null)
        {
            return mSpinSettings.GetStepInterval(stepIndex, steps);
        }

        return CircleDailyAwardSpinSettings.EvaluateDefaultStepInterval(stepIndex, steps);
    }

    int GetSpinIndexById(int id)
    {
        for (int i = 0; i < mSpinItems.Count; i++)
        {
            if (mSpinItems[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    void ClearHighlight()
    {
        mHighlightIndex = -1;
        mTrailAge = 0;
        for (int i = 0; i < mSpinItems.Count; i++)
        {
            SetSelectedVisible(mSpinItems[i], false, 0f);
        }
    }

    void SetHighlight(int index)
    {
        if (index < 0 || index >= mSpinItems.Count)
        {
            return;
        }

        mHighlightIndex = index;
        mTrailAge++;
        RefreshHighlightTrail(index);
        XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.circleRotation);
    }

    void RefreshHighlightTrail(int index, int trailCount = -1)
    {
        int slotCount = mSpinItems.Count;
        if (slotCount <= 0 || index < 0 || index >= slotCount)
        {
            return;
        }

        if (trailCount < 0)
        {
            trailCount = mTrailAge >= 2 ? 2 : (mTrailAge >= 1 ? 1 : 0);
        }

        int prevIndex = (index - 1 + slotCount) % slotCount;
        int prevPrevIndex = (index - 2 + slotCount) % slotCount;
        for (int i = 0; i < slotCount; i++)
        {
            if (i == index)
            {
                SetSelectedVisible(mSpinItems[i], true, 1f);
            }
            else if (i == prevIndex && trailCount >= 1)
            {
                SetSelectedVisible(mSpinItems[i], true, TrailPrevAlpha);
            }
            else if (i == prevPrevIndex && trailCount >= 2)
            {
                SetSelectedVisible(mSpinItems[i], true, TrailPrevPrevAlpha);
            }
            else
            {
                SetSelectedVisible(mSpinItems[i], false, 0f);
            }
        }
    }

    static void SetSelectedVisible(SpinItemView view, bool visible, float alpha)
    {
        if (view?.Selected == null)
        {
            return;
        }

        view.Selected.SetActiveEx(visible);
        if (view.SelectedImage == null)
        {
            return;
        }

        var color = view.SelectedBaseColor;
        color.a = view.SelectedBaseColor.a * alpha;
        view.SelectedImage.color = color;
    }

    SpinItemView GetSpinItemById(int id)
    {
        for (int i = 0; i < mSpinItems.Count; i++)
        {
            if (mSpinItems[i].Id == id)
            {
                return mSpinItems[i];
            }
        }

        return null;
    }

    void PlayUpSetOnce(SpinItemView view)
    {
        if (view == null || view.UpSet == null)
        {
            return;
        }

        KillUpSetTween(view);
        var doubleClip = XSortGameCore.AudioController.AudioClips?.spin_double;
        if (doubleClip != null)
            XSortGameCore.AudioController.PlaySound(doubleClip);
        view.UpSet.SetActiveEx(true);

        // 先确保子节点激活再 Play
        var root = view.UpSet.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            root.GetChild(i).gameObject.SetActiveEx(true);
        }

        RestartParticleSystems(root);

        float duration = Mathf.Max(0.5f, GetParticleEffectDuration(root));
        view.UpSetTween = DOVirtual.DelayedCall(duration, () =>
        {
            StopParticleSystems(root);
            SetUpSetVisible(view, false);
            view.UpSetTween = null;
        }).SetUpdate(true).SetLink(gameObject);
    }

    static void SetUpSetVisible(SpinItemView view, bool visible)
    {
        if (view == null || view.UpSet == null)
        {
            return;
        }

        if (!visible)
        {
            StopParticleSystems(view.UpSet.transform);
        }

        view.UpSet.SetActiveEx(visible);
    }

    void KillUpSetTween(SpinItemView view)
    {
        if (view == null || view.UpSetTween == null)
        {
            return;
        }

        if (view.UpSetTween.IsActive())
        {
            view.UpSetTween.Kill();
        }

        view.UpSetTween = null;
    }

    void KillAllUpSetTweens()
    {
        for (int i = 0; i < mSpinItems.Count; i++)
        {
            KillUpSetTween(mSpinItems[i]);
            SetUpSetVisible(mSpinItems[i], false);
        }
    }

    static void RestartParticleSystems(Transform root)
    {
        if (root == null)
        {
            return;
        }

        foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    static void StopParticleSystems(Transform root)
    {
        if (root == null)
        {
            return;
        }

        foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);
        }
    }

    static float GetParticleEffectDuration(Transform root)
    {
        float maxDuration = 0f;
        if (root == null)
        {
            return maxDuration;
        }

        foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particleSystem.main;
            if (main.loop)
            {
                continue;
            }

            float duration = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
            maxDuration = Mathf.Max(maxDuration, duration);
        }

        return maxDuration;
    }

    static Transform FindChildByTrimmedName(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (string.Equals(root.name.Trim(), name, StringComparison.Ordinal))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindChildByTrimmedName(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    void GrantAward()
    {
        if (mCurAwardData == null || mCurAwardData.Award == null || mCurAwardData.Award.Count == 0)
        {
            SetBusy(false);
            return;
        }

        SetBusy(true);

        int idx = mCurAwardData.Id - 1;
        int cnt = GPUserDataManager.Instance.UserData.GetCircleAwardCount(idx);
        int chen = cnt > 0 ? 2 : 1;
        var awards = BuildGrantedAwards(mCurAwardData.Award, chen);

        // UI_Add_Alpha 是 HDR 加法混合：UIShopGetDlg 的半透黑底盖不住，
        // 自定义材质还可能脱离 Canvas 排序画到弹窗之上。发奖弹窗打开前先关掉高亮。
        ClearHighlight();

        int awardId = mCurAwardData.Id;
        var dlg = GameModule.UI.OpenPanel<UIShopGetDlg>(PanelLayer.Tips, PanelOpenType.Single);
        if (dlg == null)
        {
            ClaimCurrentAward(awards, awardId, chen);
            return;
        }

        dlg.Open();
        dlg.ShowAwards(awards, () => ClaimCurrentAward(awards, awardId, chen));
    }

    static List<string> BuildGrantedAwards(IList<string> rawAwards, int chen)
    {
        var awards = new List<string>();
        if (rawAwards == null)
        {
            return awards;
        }

        for (int i = 0; i < rawAwards.Count; i++)
        {
            if (!ActivityData.TryParseCircleAward(rawAwards[i], out int id, out int num))
            {
                continue;
            }

            awards.Add(id + "," + num * chen);
        }

        return awards;
    }

    void ClaimCurrentAward(List<string> awards, int awardId, int chen)
    {
        if (awardId <= 0)
        {
            SetBusy(false);
            return;
        }

        int idx = awardId - 1;
        // 首次抽中该格后，界面数量会切到 x2：关闭领奖弹窗时播 UI_UpSet
        bool playUpSet = chen <= 1;

        int coinNum = 0;
        if (awards != null)
        {
            for (int i = 0; i < awards.Count; i++)
            {
                if (!ActivityData.TryParseCircleAward(awards[i], out int id, out int num))
                {
                    continue;
                }

                if (id == 4)
                {
                    coinNum += num;
                }
                else
                {
                    GPUserDataManager.Instance.AddCommonItemData(id, num);
                }
            }
        }

        GPUserDataManager.Instance.UserData.AddCircleAwardCount(idx);
        GameData.Activity.ApplyCircleAwardDrawResult(awardId);
        mCurAwardData = null;
        ClearHighlight();
        RefreshAwards();

        if (playUpSet)
        {
            // 等领奖弹窗销毁后再播，避免被盖住或同帧激活失败
            DOVirtual.DelayedCall(0.05f, () => PlayUpSetOnce(GetSpinItemById(awardId)))
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        if (coinNum <= 0)
        {
            SetBusy(false);
            return;
        }

        PlayCoinFlyToBar(coinNum);
    }

    void PlayCoinFlyToBar(int coinNum)
    {
        if (currencyComp == null)
        {
            GPUserDataManager.Instance.AddCoin(coinNum);
            SetBusy(false);
            return;
        }

        DOVirtual.DelayedCall(0.1f, () =>
        {
            currencyComp.PlayCoinFly(() => SetBusy(false));
            currencyComp.PlayCoinGainBatched(coinNum, batch =>
            {
                GPUserDataManager.Instance.AddCoin(batch);
            });
        }).SetUpdate(true).SetLink(gameObject);
        DOVirtual.DelayedCall(1.0f, () =>
        {
            XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.coinAdd);
        }).SetUpdate(true).SetLink(gameObject);
    }

    void KillSpinSeq()
    {
        if (mSpinSeq != null && mSpinSeq.IsActive())
        {
            mSpinSeq.Kill();
        }

        mSpinSeq = null;
    }

    static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
