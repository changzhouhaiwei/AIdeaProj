using System;
using System.Collections.Generic;
using System.Collections;
using Core.Tools.MethodExtension;
using DG.Tweening;
using FishFramework;
using FishXSortPlay;
using Spine;
using Spine.Unity;
using XSortGameCore;
using XSortGamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDailyWinRewardDlg : UIDailyWinRewardDlgBase, IUIOpen<DailyWinRewardDlgOpenParam>
{
    private const string WinBgInDayAnim = "BG_in_day";
    private const string WinBgDayLoopAnim = "BG_day_loop";
    private const string WinBgInNightAnim = "BG_in_night";
    private const string WinBgNightLoopAnim = "BG_night_loop";
    private const string StarLoopAnim = "StarLoop";
    private const string UiDogLoopAnim = "UI_DogLoop";
    private const string DailyWinAtlasDir = "DailyWin";
    private const string BoardDaySprite = "DailyWin_d";
    private const string BoardNightSprite = "DailyWin_d2";
    private const string RewardDaySprite = "DailyWin_reward_d";
    private const string RewardNightSprite = "DailyWin_reward_d_hei";
    private const string RewardD2DaySprite = "DailyWin_reward_d2";
    private const string RewardD2NightSprite = "DailyWin_reward_d2_hei";
    private const string StarBgDaySprite = "DailyWin_star_bg";
    private const string StarBgNightSprite = "DailyWin_star_bg2";
    private const string PanelAnimatorIsStartParam = "isStart";
    private const string PanelAnimatorInStateName = "UIDailyWinRewarDlg_In";
    private const string PanelAnimatorManualInStateName = "UIDailyWinRewarDlg_UI_In";
    private const float PanelAnimatorInFallbackDuration = 2.67f;
    private const float PanelAnimatorStartDelay = 1.2f; //animator在打开界面的delay
    private const float BoxTipsDisplaySeconds = 3f;         //boxtips显示时间   
    private const float SettlementBoxTipsPostAnimatorDelay = 2f;  //boxtips 在animator后的delay
    private const float RankAwardCellMoveUpDistance = 70f;   //rank award cell移动距离  
    private const float RankAwardCellMoveDuration = 0.8f;    //rank award cell移动时间  
    private const string ItemInAnimStateName = "Item_in";
    private const float ItemInAnimDuration = 0.5f;
    private const float ItemInAnimPostCountChangeDelay = 0.5f;
    private const float TotalWinCountChargeBeatDuration = 0.045f;
    private const float TotalWinCountShrinkDuration = 0.07f;
    private const float TotalWinCountShrinkScale = 0.88f;
    private const float TotalWinCountPopDuration = 0.08f;
    private const float TotalWinCountPopScale = 1.2f;
    private const float TotalWinCountSettleDuration = 0.16f;
    private const int SignEventStatusSettlement = 0;
    private const int SignEventStatusMainCity = 4;

    private sealed class DailyWinRewardItemView
    {
        public RectTransform Root;
        public Toggle Toggle;
        public Image ToggleImage;
        public bool UsesRewardD2;
        public TextMeshProUGUI Label;
        public RectTransform RankAwardCell;
        public Vector2 RankAwardCellInitialPos;
        public readonly List<RectTransform> RankAwardNodes = new();
        public RectTransform RankAwardNodeTemplate;
        public Animator ItemAnimator;
        public ActivityData.DailyWinRewardViewData ViewData;
    }

    private static readonly string[] WeekdayTermKeys =
    {
        "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat",
    };

    private readonly List<DailyWinRewardItemView> mItemViewList = new();
    private RewardShowDlg mRewardShowDlg;
    private Timer mBoxTipsHideTimer;
    private Timer mBoxTipsShowDelayTimer;
    private Timer mAnimatorWaitTimer;
    private Timer mItemPostAnimTimer;
    private Timer mTodayItemCheckFinalizeTimer;
    private Timer mPanelAnimatorStartTimer;
    private Timer mStartButtonSnapTimer;
    private Coroutine mTextEffCoroutine;
    private bool mPanelAnimatorStartTriggered;
    private bool mForcedSettlementInPlay;
    private DailyWinRewardDlgOpenParam mOpenParam;
    private SkeletonGraphic mDogNewSpine;
    private SkeletonGraphic mDogSpeedSetSpine;
    private SkeletonGraphic mWinBgSpine;
    private SkeletonGraphic mStarLoopSpine;
    private SkeletonGraphic mUiDogSpine;
    private Image mBoardImage;
    private Image mStarBgImage;
    private Animator mPanelAnimator;
    private RectTransform mSpeekSet;
    private GameObject mTextEff;
    private Vector3 mTotalWinCountDefaultScale;
    private bool mHasTotalWinCountDefaultScale;
    private TrackEntry mDogNewTrackEntry;
    private TrackEntry mDogSpeedSetTrackEntry;
    private bool mSettlementContinueEnabled = true;

    protected override void OnOpen()
    {
        if (!GameData.Activity.IsDailyVictoryActEnabled())
        {
            CloseSelf();
            return;
        }

        mOpenParam = null;
        base.OnOpen();
        ReportSignOpenEvent();
        ApplyOpenModeUI();
        RefreshHeaderTexts();
        BuildRewardItems();
        PlayOpenSequence();
    }

    public void OnOpen(DailyWinRewardDlgOpenParam openParam)
    {
        if (!GameData.Activity.IsDailyVictoryActEnabled())
        {
            var onContinue = openParam?.OnContinue;
            CloseSelf();
            onContinue?.Invoke();
            return;
        }

        mOpenParam = openParam;
        base.OnOpen();
        ReportSignOpenEvent();
        ApplyOpenModeUI();
        RefreshHeaderTexts();
        BuildRewardItems();
        PlayOpenSequence();
    }

    private void ApplyOpenModeUI()
    {
        bool fromSettlement = IsSettlementOpen();
        if (closeButton != null)
        {
            closeButton.gameObject.SetActiveEx(!fromSettlement);
        }

        // 关卡结束结算必须播完整段演出，不能点空白/继续提前跳过
        SetSettlementContinueEnabled(!fromSettlement);
    }

    private void ReportSignOpenEvent()
    {
        int status = mOpenParam != null && mOpenParam.FromSettlement
            ? SignEventStatusSettlement
            : SignEventStatusMainCity;
        int day = GameData.Activity.GetDailyVictoryTotalWinDays();
        GameBAData.ExcuteSignEvent(status, day);
        if (mOpenParam != null && mOpenParam.FromSettlement)
            GameBAData.ExcuteGameTriggerActivity("3", "sign", 1);
    }

    protected override void OnDestroying()
    {
        CancelOpenSequence();
        HideBoxTips();
        base.OnDestroying();
    }

    protected override void OnClicked(Button button)
    {
        if (button == closeButton)
        {
            if (IsSettlementOpen())
            {
                return;
            }

            HideBoxTips();
            CloseSelf();
            return;
        }

        if (button == startButton)
        {
            OnStartButtonClicked();
            return;
        }

        base.OnClicked(button);
    }

    private void OnStartButtonClicked()
    {
        if (IsSettlementOpen() && !mSettlementContinueEnabled)
        {
            return;
        }

        HideBoxTips();

        if (IsSettlementOpen())
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            var onContinue = mOpenParam.OnContinue;
            CloseSelf();
            onContinue?.Invoke();
            return;
        }

        AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        int levelIndex = GPUserDataManager.Instance.GetFinalLevelNum();
        CloseSelf();
        GameDirector.LoadLevel(levelIndex);
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        if (IsSettlementOpen())
        {
            if (IsBoxTipsVisible())
            {
                HideBoxTips();
            }

            return;
        }

        if (IsBoxTipsVisible())
        {
            HideBoxTips();
            return;
        }

        base.OnBackgroundClicked(bgClickEventType);
    }

    protected override bool OnEscButtonPressed()
    {
        if (IsBoxTipsVisible())
        {
            HideBoxTips();
            return true;
        }

        if (IsSettlementOpen())
        {
            return true;
        }

        HideBoxTips();
        CloseSelf();
        return true;
    }

    private void PlayOpenSequence()
    {
        CancelOpenSequence();
        HideBoxTips();

        bool fromSettlement = mOpenParam != null && mOpenParam.FromSettlement;
        if (fromSettlement)
        {
            PlaySettlementOpenSequence();
        }
        else
        {
            PlayManualOpenSequence();
        }
    }

    private void PlayManualOpenSequence()
    {
        PlayPanelAnimatorInForManualOpen();
        PlayWinBgLoop();
        ShowDay7BoxTipsTimed(BoxTipsDisplaySeconds);
    }

    private void PlayPanelAnimatorInForManualOpen()
    {
        var animator = EnsurePanelAnimator();
        if (animator != null)
        {
            animator.SetBool(PanelAnimatorIsStartParam, false);
            animator.enabled = true;
            animator.Play(PanelAnimatorManualInStateName, 0, 0f);
            animator.Update(0f);
        }

        // Manual UI_In does not animate Start Button scale. Keep Animator on that
        // state so WriteDefaults hold bind-pose scale 1. Disabling it restores Idle's 0.
        ReleaseInteractivePressScales(disablePanelAnimator: false);
    }

    private void PlaySettlementOpenSequence()
    {
        PreparePanelAnimatorForSettlement();

        mPanelAnimatorStartTriggered = false;

        PlaySettlementLoopingSpines();

        CancelSettlementDogCompleteCallbacks();
        CancelPanelAnimatorStartTimer();
        bool hasPlayedDogAnimation = false;
        hasPlayedDogAnimation |= PlaySettlementDogOnce(EnsureDogNewSpine(), ref mDogNewTrackEntry, OnDogNewCompleteForSettlement);
        hasPlayedDogAnimation |= PlaySettlementDogOnce(EnsureDogSpeedSetSpine(), ref mDogSpeedSetTrackEntry, OnDogSpeedSetCompleteForSettlement);
        if (hasPlayedDogAnimation)
        {
            PlayDailyStreakFlySound();
        }

        SchedulePanelAnimatorStartForSettlement();
    }

    private void SchedulePanelAnimatorStartForSettlement()
    {
        mPanelAnimatorStartTimer = Delay(PanelAnimatorStartDelay, OnPanelAnimatorStartDelayTimer, useRealTime: true);
    }

    private void OnPanelAnimatorStartDelayTimer()
    {
        mPanelAnimatorStartTimer = null;
        if (gameObject == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        TriggerPanelAnimatorStartForSettlement();
    }

    private void OnDogNewCompleteForSettlement(TrackEntry trackEntry)
    {
        CancelDogNewCompleteCallback();
        if (gameObject == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        HideSpine(EnsureDogNewSpine());
    }

    private void OnDogSpeedSetCompleteForSettlement(TrackEntry trackEntry)
    {
        CancelDogSpeedSetCompleteCallback();
        if (gameObject == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        HideSpine(EnsureDogSpeedSetSpine());
    }

    private void TriggerPanelAnimatorStartForSettlement()
    {
        if (mPanelAnimatorStartTriggered)
        {
            return;
        }

        mPanelAnimatorStartTriggered = true;
        SetPanelAnimatorStarted();
        StartAnimatorWaitTimer();
    }

    private void StartAnimatorWaitTimer()
    {
        CancelAnimatorWaitTimer();
        mAnimatorWaitTimer = Delay(GetPanelAnimatorInDuration(), OnSettlementAnimatorComplete, useRealTime: true);
    }

    private float GetPanelAnimatorInDuration()
    {
        return Mathf.Max(
            GetPanelAnimatorClipDuration(PanelAnimatorInStateName, PanelAnimatorInFallbackDuration),
            PanelAnimatorInFallbackDuration);
    }

    private float GetPanelAnimatorClipDuration(string clipName, float fallbackDuration)
    {
        var animator = EnsurePanelAnimator();
        var controller = animator != null ? animator.runtimeAnimatorController : null;
        if (controller == null)
        {
            return fallbackDuration;
        }

        foreach (var clip in controller.animationClips)
        {
            if (clip != null && clip.name == clipName)
            {
                return clip.length;
            }
        }

        return fallbackDuration;
    }

    /// <summary>
    /// Idle zeros Start Button. Manual open keeps the panel Animator on UI_In so
    /// WriteDefaults hold scale 1. Settlement must disable after In so TotalWinCount
    /// DOTween can run; then force scale and CanvasGroup alpha to 1.
    /// </summary>
    private void ReleaseInteractivePressScales(bool disablePanelAnimator)
    {
        if (disablePanelAnimator)
        {
            var animator = EnsurePanelAnimator();
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        PinIdleHiddenAppearTargets();
        ConfigureButtonPressScale(startButton);
        ConfigureButtonPressScale(closeButton);
        ConfigureRewardItemPressScales();
        if (disablePanelAnimator)
        {
            ScheduleStartButtonVisiblePin();
        }
    }

    private void PinIdleHiddenAppearTargets()
    {
        PinStartButtonVisible();
        var speekSet = EnsureSpeekSet();
        if (speekSet != null)
        {
            speekSet.localScale = Vector3.one;
        }
    }

    private void PinStartButtonVisible()
    {
        if (startButton == null)
        {
            return;
        }

        startButton.gameObject.SetActive(true);
        startButton.transform.localScale = Vector3.one;
        var canvasGroup = startButton.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void ScheduleStartButtonVisiblePin()
    {
        CancelStartButtonSnapTimer();
        mStartButtonSnapTimer = Delay(0.05f, () =>
        {
            mStartButtonSnapTimer = null;
            if (gameObject == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            PinIdleHiddenAppearTargets();
            ConfigureButtonPressScale(startButton);
        }, useRealTime: true);
    }

    private void CancelStartButtonSnapTimer()
    {
        if (mStartButtonSnapTimer == null)
        {
            return;
        }

        Unschedule(mStartButtonSnapTimer);
        mStartButtonSnapTimer = null;
    }

    private static void ConfigureButtonPressScale(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.transform.localScale = Vector3.one;

        var tButton = button as TButton;
        if (tButton == null)
        {
            tButton = button.GetComponent<TButton>();
        }

        if (tButton != null)
        {
            tButton.clickDoTween = true;
            tButton.transition = Selectable.Transition.None;
            tButton.SetRestingScale(Vector3.one);
        }
    }

    private void ConfigureRewardItemPressScales()
    {
        foreach (var itemView in mItemViewList)
        {
            ConfigureItemTogglePressScale(itemView);
        }
    }

    private static void ConfigureItemTogglePressScale(DailyWinRewardItemView itemView)
    {
        if (itemView?.Toggle == null)
        {
            return;
        }

        itemView.Toggle.transition = Selectable.Transition.None;
        if (itemView.Toggle is TToggle tToggle)
        {
            tToggle.clickDoTween = true;
            tToggle.SetRestingScale(Vector3.one);
            return;
        }

        itemView.Toggle.transform.localScale = Vector3.one;
    }

    private void OnSettlementAnimatorComplete()
    {
        mAnimatorWaitTimer = null;
        if (gameObject == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        var animator = EnsurePanelAnimator();
        if (animator != null && animator.enabled)
        {
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(PanelAnimatorInStateName))
            {
                if (state.normalizedTime < 0.99f)
                {
                    float remain = Mathf.Max(0.05f, (1f - state.normalizedTime) * state.length);
                    mAnimatorWaitTimer = Delay(remain, OnSettlementAnimatorComplete, useRealTime: true);
                    return;
                }
            }
            else if (!mForcedSettlementInPlay)
            {
                // Timer fired while still on Idle; freezing now would hide Start Button / SpeekSet at scale 0.
                mForcedSettlementInPlay = true;
                animator.SetBool(PanelAnimatorIsStartParam, true);
                animator.Play(PanelAnimatorInStateName, 0, 0f);
                animator.Update(0f);
                mAnimatorWaitTimer = Delay(GetPanelAnimatorInDuration(), OnSettlementAnimatorComplete, useRealTime: true);
                return;
            }
        }

        ReleaseInteractivePressScales(disablePanelAnimator: true);

        int streakDay = GameData.Activity.GetDailyVictoryStreakDay();
        ScheduleSettlementBoxTipsTimed(BoxTipsDisplaySeconds);

        if (IsSettlementOpen())
        {
            PlayTotalWinCountTransition(() => ScheduleTodayItemInThenReward(streakDay));
        }
        else
        {
            PlayTodayItemInThenReward(streakDay);
        }
    }

    private void ScheduleTodayItemInThenReward(int streakDay)
    {
        CancelItemPostAnimTimer();
        mItemPostAnimTimer = Delay(ItemInAnimPostCountChangeDelay, () =>
        {
            mItemPostAnimTimer = null;
            PlayTodayItemInThenReward(streakDay);
        }, useRealTime: true);
    }

    private void PlayTodayItemInThenReward(int streakDay)
    {
        var itemView = GetItemViewByDay(streakDay);
        if (itemView == null)
        {
            OnTodayItemPostAnimDelayComplete(streakDay);
            return;
        }

        PlayTodayItemAnimator(itemView, () => OnTodayItemPostAnimDelayComplete(streakDay));
    }

    private void OnTodayItemPostAnimDelayComplete(int streakDay)
    {
        if (gameObject == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (streakDay == 7)
        {
            OpenDay7RewardGetIfClaimable();
            EnableSettlementContinue();
            return;
        }

        PlayDay1To6RankAwardCellFlyAnim(streakDay);
    }

    private void OpenDay7RewardGetIfClaimable()
    {
        // 含 UserFrameNode 的领奖弹窗：仅第 7 天头像框可领时打开
        if (!GameData.Activity.CanClaimDailyVictoryAvatarFrame())
        {
            return;
        }

        if (GameModule.UI.ExistPanel<UIDailyRewardGet>())
        {
            GameModule.UI.DestroyPanel<UIDailyRewardGet>();
        }

        GameModule.UI.OpenPanel<UIDailyRewardGet>(PanelLayer.Tips, PanelOpenType.Single).Open();
    }

    private void PlayDay1To6RankAwardCellFlyAnim(int streakDay)
    {
        var rewardCfg = GameData.Activity.GetRewardCfgByStreakDay(streakDay);
        if (!GameData.Activity.HasRewardContent(rewardCfg))
        {
            EnableSettlementContinue();
            return;
        }

        if (GameData.Activity.HasClaimableStreakReward())
        {
            GameData.Activity.ClaimCurrentStreakReward();
        }

        var itemView = GetItemViewByDay(streakDay);
        if (itemView?.RankAwardCell == null)
        {
            EnableSettlementContinue();
            return;
        }

        var rewardList = GameData.Activity.BuildDailyWinRewardTipList(rewardCfg);
        if (rewardList.Count == 0)
        {
            EnableSettlementContinue();
            return;
        }

        EnsureAndPopulateRankAwardNodes(itemView, rewardList);
        PlayRankAwardCellFlyAnim(itemView, EnableSettlementContinue);
    }

    private void PlayTodayItemAnimator(DailyWinRewardItemView itemView, Action onComplete)
    {
        var animator = itemView?.ItemAnimator;
        if (animator == null)
        {
            onComplete?.Invoke();
            return;
        }

        PrepareTodayItemForInAnim(itemView);

        animator.enabled = true;
        animator.Play(ItemInAnimStateName, 0, 0f);
        animator.Update(0f);
        PlayDailyStreakRewardSound();

        ScheduleTodayItemCheckFinalize(itemView, onComplete);
    }

    private void PrepareTodayItemForInAnim(DailyWinRewardItemView itemView)
    {
        var toggle = itemView?.Toggle;
        if (toggle == null)
        {
            return;
        }

        ApplyItemToggleCheckedState(toggle, false);

        var checkmarkRect = toggle.graphic != null ? toggle.graphic.rectTransform : null;
        if (checkmarkRect == null)
        {
            return;
        }

        // Toggle 需为 on 状态 Animator 才能驱动 Checkmark 缩放，初始 scale=0 视觉上仍为未勾选
        toggle.SetIsOnWithoutNotify(true);
        checkmarkRect.localScale = Vector3.zero;
    }

    private void ScheduleTodayItemCheckFinalize(DailyWinRewardItemView itemView, Action onComplete)
    {
        CancelTodayItemCheckFinalizeTimer();
        mTodayItemCheckFinalizeTimer = Delay(ItemInAnimDuration, () =>
        {
            mTodayItemCheckFinalizeTimer = null;
            FinalizeTodayItemCheckmark(itemView);
            onComplete?.Invoke();
        }, useRealTime: true);
    }

    private static void FinalizeTodayItemCheckmark(DailyWinRewardItemView itemView)
    {
        if (itemView?.Toggle == null)
        {
            return;
        }

        if (itemView.ViewData != null)
        {
            itemView.ViewData.IsChecked = true;
        }

        ApplyItemToggleCheckedState(itemView.Toggle, true);
        // Item_in Animator owns Checkmark scale while playing; release so toggle press-scale can run.
        ResetItemAnimator(itemView);
        ConfigureItemTogglePressScale(itemView);
    }

    private static void ApplyItemToggleCheckedState(Toggle toggle, bool isChecked)
    {
        toggle.SetIsOnWithoutNotify(isChecked);
        if (toggle is TToggle tToggle)
        {
            tToggle.SetLabelColor(tToggle.NoSelectColor);
        }

        var toggleImage = toggle.GetComponent<Image>();
        if (toggleImage != null)
        {
            toggleImage.enabled = !isChecked;
        }
    }

    private bool ShouldDeferTodayItemCheckmark(int day)
    {
        if (mOpenParam == null || !mOpenParam.FromSettlement)
        {
            return false;
        }

        return day == GameData.Activity.GetDailyVictoryStreakDay();
    }

    private static void ResetItemAnimator(DailyWinRewardItemView itemView)
    {
        if (itemView?.ItemAnimator == null)
        {
            return;
        }

        itemView.ItemAnimator.enabled = false;
    }

    private void ResetAllItemAnimators()
    {
        foreach (var itemView in mItemViewList)
        {
            ResetItemAnimator(itemView);
        }
    }

    private void CancelItemPostAnimTimer()
    {
        if (mItemPostAnimTimer == null)
        {
            return;
        }

        Unschedule(mItemPostAnimTimer);
        mItemPostAnimTimer = null;
    }

    private void CancelTodayItemCheckFinalizeTimer()
    {
        if (mTodayItemCheckFinalizeTimer == null)
        {
            return;
        }

        Unschedule(mTodayItemCheckFinalizeTimer);
        mTodayItemCheckFinalizeTimer = null;
    }

    private static void PlayDailyStreakFlySound()
    {
        var clip = AudioController.AudioClips?.dailyStreakFly;
        if (clip != null)
        {
            AudioController.PlaySound(clip);
        }
    }

    private static void PlayDailyStreakRewardSound()
    {
        var clip = AudioController.AudioClips?.dailyStreakReward;
        if (clip != null)
        {
            AudioController.PlaySound(clip);
        }
    }

    private static void PlayDailyStreakNumSound()
    {
        var clip = AudioController.AudioClips?.dailyStreakNum;
        if (clip != null)
        {
            AudioController.PlaySound(clip);
        }
    }

    private void PlaySettlementLoopingSpines()
    {
        PlayWinBgInThenLoop();
        PlaySpineLoop(EnsureStarLoopSpine(), StarLoopAnim);
        PlaySpineLoop(EnsureUiDogSpine(), UiDogLoopAnim);
    }

    private void PlayWinBgInThenLoop()
    {
        var spine = EnsureWinBgSpine();
        if (spine != null)
        {
            bool isNight = IsDailyWinRewardNightTime();
            string inAnimation = isNight ? WinBgInNightAnim : WinBgInDayAnim;
            string loopAnimation = GetWinBgLoopAnimation(isNight);

            spine.gameObject.SetActiveEx(true);
            spine.AnimationState.ClearTrack(0);
            spine.AnimationState.SetAnimation(0, inAnimation, false);
            spine.AnimationState.AddAnimation(0, loopAnimation, true, 0f);
            spine.Update(0f);
        }

        ApplyDayNightUiSprites();
    }

    private void PlayWinBgLoop()
    {
        PlaySpineLoop(EnsureWinBgSpine(), GetWinBgLoopAnimation(IsDailyWinRewardNightTime()));
        ApplyDayNightUiSprites();
    }

    private void ApplyDayNightUiSprites()
    {
        bool isNight = IsDailyWinRewardNightTime();

        ApplyAtlasSprite(EnsureBoardImage(), isNight ? BoardNightSprite : BoardDaySprite);
        ApplyAtlasSprite(EnsureStarBgImage(), isNight ? StarBgNightSprite : StarBgDaySprite);

        foreach (var itemView in mItemViewList)
        {
            if (itemView?.ToggleImage == null)
            {
                continue;
            }

            string spriteName = itemView.UsesRewardD2
                ? (isNight ? RewardD2NightSprite : RewardD2DaySprite)
                : (isNight ? RewardNightSprite : RewardDaySprite);
            ApplyAtlasSprite(itemView.ToggleImage, spriteName);
        }
    }

    private void ApplyAtlasSprite(Image image, string spriteName)
    {
        if (image == null || string.IsNullOrEmpty(spriteName))
        {
            return;
        }

        var sprite = LoadAtlasSprite(DailyWinAtlasDir, spriteName);
        if (sprite != null)
        {
            image.sprite = sprite;
        }
    }

    private static bool IsRewardD2Sprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        string name = sprite.name;
        return name == RewardD2DaySprite || name == RewardD2NightSprite;
    }

    private static string GetWinBgLoopAnimation(bool isNight)
    {
        return isNight ? WinBgNightLoopAnim : WinBgDayLoopAnim;
    }

    /// <summary>
    /// Uses the player's device-local time. Night runs from 18:00 inclusive to 06:00 exclusive.
    /// </summary>
    private static bool IsDailyWinRewardNightTime()
    {
        int hour = DateTime.Now.Hour;
        return hour >= 18 || hour < 6;
    }

    private static void PlaySpineLoop(SkeletonGraphic spine, string animationName)
    {
        if (spine == null || string.IsNullOrEmpty(animationName))
        {
            return;
        }

        spine.gameObject.SetActiveEx(true);
        spine.AnimationState.ClearTrack(0);
        spine.AnimationState.SetAnimation(0, animationName, true);
        spine.Update(0f);
    }

    private static bool PlaySettlementDogOnce(SkeletonGraphic spine, ref TrackEntry trackEntry, Spine.AnimationState.TrackEntryDelegate onComplete)
    {
        if (spine == null)
        {
            return false;
        }

        string animationName = GetFirstSpineAnimationName(spine);
        if (string.IsNullOrEmpty(animationName))
        {
            spine.gameObject.SetActiveEx(false);
            return false;
        }

        spine.gameObject.SetActiveEx(true);
        spine.AnimationState.ClearTrack(0);
        trackEntry = spine.AnimationState.SetAnimation(0, animationName, false);
        trackEntry.Complete += onComplete;
        spine.Update(0f);
        return true;
    }

    private static string GetFirstSpineAnimationName(SkeletonGraphic spine)
    {
        var skeletonDataAsset = spine != null ? spine.SkeletonDataAsset : null;
        var skeletonData = skeletonDataAsset != null ? skeletonDataAsset.GetSkeletonData(true) : null;
        if (skeletonData == null || skeletonData.Animations == null || skeletonData.Animations.Count == 0)
        {
            return null;
        }

        return skeletonData.Animations.Items[0]?.Name;
    }

    private static void HideSpine(SkeletonGraphic spine)
    {
        if (spine == null)
        {
            return;
        }

        if (spine.AnimationState != null)
        {
            spine.AnimationState.ClearTrack(0);
        }

        spine.gameObject.SetActiveEx(false);
    }

    private void PreparePanelAnimatorForSettlement()
    {
        var animator = EnsurePanelAnimator();
        if (animator == null)
        {
            return;
        }

        animator.SetBool(PanelAnimatorIsStartParam, false);
        animator.enabled = true;
    }

    private void SetPanelAnimatorStarted()
    {
        var animator = EnsurePanelAnimator();
        if (animator == null)
        {
            return;
        }

        animator.SetBool(PanelAnimatorIsStartParam, true);
    }

    private void ResetPanelAnimator()
    {
        var animator = EnsurePanelAnimator();
        if (animator == null)
        {
            return;
        }

        animator.SetBool(PanelAnimatorIsStartParam, false);
        animator.enabled = false;
    }

    private void PlayRankAwardCellFlyAnim(DailyWinRewardItemView itemView, Action onComplete)
    {
        var rankAwardCell = itemView.RankAwardCell;
        if (rankAwardCell == null)
        {
            onComplete?.Invoke();
            return;
        }

        var canvasGroup = rankAwardCell.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = rankAwardCell.gameObject.AddComponent<CanvasGroup>();
        }

        rankAwardCell.anchoredPosition = itemView.RankAwardCellInitialPos;
        canvasGroup.alpha = 1f;
        rankAwardCell.gameObject.SetActiveEx(true);

        var startPos = itemView.RankAwardCellInitialPos;
        var endPos = startPos + new Vector2(0f, RankAwardCellMoveUpDistance);

        var sequence = DOTween.Sequence();
        sequence.SetId(this);
        sequence.SetLink(rankAwardCell.gameObject, LinkBehaviour.KillOnDestroy);
        sequence.Append(rankAwardCell
            .DOAnchorPos(endPos, RankAwardCellMoveDuration)
            .SetEase(Ease.Linear));
        sequence.OnComplete(() =>
        {
            if (rankAwardCell == null)
            {
                onComplete?.Invoke();
                return;
            }

            rankAwardCell.gameObject.SetActiveEx(false);
            rankAwardCell.anchoredPosition = itemView.RankAwardCellInitialPos;
            canvasGroup.alpha = 1f;
            onComplete?.Invoke();
        });
        sequence.Play();
    }

    private void EnsureAndPopulateRankAwardNodes(
        DailyWinRewardItemView itemView,
        List<ActivityData.DailyWinRewardTipData> tipList)
    {
        if (itemView?.RankAwardCell == null)
        {
            return;
        }

        CollectRankAwardNodes(itemView);

        int rewardCount = tipList?.Count ?? 0;
        EnsureRankAwardNodeCount(itemView, rewardCount);

        for (int i = 0; i < itemView.RankAwardNodes.Count; i++)
        {
            var node = itemView.RankAwardNodes[i];
            if (node == null)
            {
                continue;
            }

            bool show = i < rewardCount;
            node.gameObject.SetActiveEx(show);
            if (show)
            {
                PopulateRankAwardNode(node.gameObject, tipList[i]);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(itemView.RankAwardCell);
    }

    private static void CollectRankAwardNodes(DailyWinRewardItemView itemView)
    {
        if (itemView.RankAwardCell == null)
        {
            return;
        }

        itemView.RankAwardNodes.Clear();
        var nodes = new List<RectTransform>();
        for (int i = 0; i < itemView.RankAwardCell.childCount; i++)
        {
            var child = itemView.RankAwardCell.GetChild(i) as RectTransform;
            if (child == null || !IsRankAwardNode(child))
            {
                continue;
            }

            nodes.Add(child);
        }

        nodes.Sort((a, b) => GetRankAwardNodeOrder(a.name).CompareTo(GetRankAwardNodeOrder(b.name)));
        itemView.RankAwardNodes.AddRange(nodes);
        itemView.RankAwardNodeTemplate = itemView.RankAwardNodes.Count > 0 ? itemView.RankAwardNodes[0] : null;
    }

    private static void EnsureRankAwardNodeCount(DailyWinRewardItemView itemView, int requiredCount)
    {
        if (requiredCount <= itemView.RankAwardNodes.Count || itemView.RankAwardNodeTemplate == null)
        {
            return;
        }

        while (itemView.RankAwardNodes.Count < requiredCount)
        {
            var newNode = UnityEngine.Object.Instantiate(itemView.RankAwardNodeTemplate, itemView.RankAwardCell);
            int index = itemView.RankAwardNodes.Count;
            newNode.name = index == 0 ? "node" : $"node ({index})";
            newNode.SetAsLastSibling();
            itemView.RankAwardNodes.Add(newNode);
        }
    }

    private static bool IsRankAwardNode(Transform node)
    {
        return node != null && node.name.StartsWith("node");
    }

    private static int GetRankAwardNodeOrder(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName) || !nodeName.StartsWith("node"))
        {
            return int.MaxValue;
        }

        if (nodeName == "node")
        {
            return 0;
        }

        // "node (1)" / "node (2)"
        int openParen = nodeName.IndexOf('(');
        int closeParen = nodeName.IndexOf(')');
        if (openParen >= 0 && closeParen > openParen
            && int.TryParse(nodeName.Substring(openParen + 1, closeParen - openParen - 1), out int order))
        {
            return order;
        }

        return int.MaxValue;
    }

    private void PopulateRankAwardNode(GameObject node, ActivityData.DailyWinRewardTipData rewardData)
    {
        if (node == null || rewardData == null)
        {
            return;
        }

        // RankAwardCellAvatarFrame：frame / TotalWinCountfixed 等全部用预制体固定展示
        if (IsFixedRankAwardAvatarFrameCell(node))
        {
            return;
        }

        var text = node.GetComponentInChildren<TextMeshProUGUI>(true);
        var icon = node.GetComponentInChildren<Image>(true);
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

        if (rewardSprite == null
            && !string.IsNullOrEmpty(rewardData.FallbackSpriteDirectory)
            && !string.IsNullOrEmpty(rewardData.FallbackSpriteName))
        {
            rewardSprite = LoadSprite(rewardData.FallbackSpriteDirectory, rewardData.FallbackSpriteName);
        }

        if (rewardSprite != null)
        {
            icon.sprite = rewardSprite;
        }
    }

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

    private void ScheduleSettlementBoxTipsTimed(float displaySeconds)
    {
        CancelBoxTipsShowDelayTimer();
        mBoxTipsShowDelayTimer = Delay(SettlementBoxTipsPostAnimatorDelay, () =>
        {
            mBoxTipsShowDelayTimer = null;
            ShowDay7BoxTipsTimed(displaySeconds);
        }, useRealTime: true);
    }

    private void ShowDay7BoxTipsTimed(float seconds)
    {
        CancelBoxTipsShowDelayTimer();
        CancelBoxTipsHideTimer();

        var lastItemView = GetLastDayItemView();
        if (lastItemView == null)
        {
            return;
        }

        if (mRewardShowDlg == null && dailyWinReward != null)
        {
            mRewardShowDlg = dailyWinReward.GetComponentInChildren<RewardShowDlg>(true);
            mRewardShowDlg?.SetOverlayClickHandler(HandleOverlayClick);
        }

        Canvas.ForceUpdateCanvases();
        if (dailyWinReward != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(dailyWinReward);
        }

        OnItemClicked(lastItemView);
        mBoxTipsHideTimer = Delay(seconds, () =>
        {
            mBoxTipsHideTimer = null;
            HideBoxTips();
        }, useRealTime: true);
    }

    private void CancelOpenSequence()
    {
        CancelSettlementDogCompleteCallbacks();
        CancelPanelAnimatorStartTimer();
        mPanelAnimatorStartTriggered = false;
        mForcedSettlementInPlay = false;
        CancelBoxTipsShowDelayTimer();
        CancelBoxTipsHideTimer();
        CancelAnimatorWaitTimer();
        CancelItemPostAnimTimer();
        CancelTodayItemCheckFinalizeTimer();
        CancelStartButtonSnapTimer();
        StopTextEff();
        ResetTotalWinCountScale();
        ResetAllItemAnimators();
        ResetPanelAnimator();
        HideSpine(EnsureDogNewSpine());
        HideSpine(EnsureDogSpeedSetSpine());
        DOTween.Kill(this);
    }

    private bool PlayTextEff(Action onComplete)
    {
        var textEff = EnsureTextEff();
        if (textEff == null || viewBehaviour == null)
        {
            return false;
        }

        StopTextEff();
        textEff.SetActiveEx(false);
        textEff.SetActiveEx(true);
        PlayDailyStreakNumSound();
        RestartParticleSystems(textEff.transform);
        mTextEffCoroutine = viewBehaviour.StartCoroutine(WaitForTextEffComplete(textEff, onComplete));
        return true;
    }

    private IEnumerator WaitForTextEffComplete(GameObject textEff, Action onComplete)
    {
        // Let the newly-enabled particle systems emit once before checking their state.
        yield return null;

        float maxDuration = GetParticleEffectDuration(textEff.transform);
        float elapsed = 0f;
        bool hasBeenAlive = false;
        while (textEff != null && textEff.activeInHierarchy)
        {
            bool isAlive = AreParticlesAlive(textEff.transform);
            hasBeenAlive |= isAlive;
            if (hasBeenAlive && !isAlive)
            {
                break;
            }

            elapsed += Time.deltaTime;
            if ((!hasBeenAlive && elapsed >= 0.5f) || elapsed >= maxDuration + 1f)
            {
                break;
            }

            yield return null;
        }

        if (textEff != null)
        {
            StopParticleSystems(textEff.transform);
            textEff.SetActiveEx(false);
        }

        mTextEffCoroutine = null;
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            onComplete?.Invoke();
        }
    }

    private void StopTextEff()
    {
        if (mTextEffCoroutine != null && viewBehaviour != null)
        {
            viewBehaviour.StopCoroutine(mTextEffCoroutine);
        }

        mTextEffCoroutine = null;
        if (mTextEff != null)
        {
            StopParticleSystems(mTextEff.transform);
            mTextEff.SetActiveEx(false);
        }
    }

    private GameObject EnsureTextEff()
    {
        if (mTextEff == null)
        {
            var textEffTransform = FindDescendant<Transform>(gameObject.transform, "TextEff");
            mTextEff = textEffTransform != null ? textEffTransform.gameObject : null;
        }

        return mTextEff;
    }

    private static void RestartParticleSystems(Transform root)
    {
        foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (!particleSystem.gameObject.activeInHierarchy)
            {
                continue;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);
            particleSystem.Play(true);
        }
    }

    private static void StopParticleSystems(Transform root)
    {
        foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(true);
        }
    }

    private static float GetParticleEffectDuration(Transform root)
    {
        float maxDuration = 0f;
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

    private static bool AreParticlesAlive(Transform root)
    {
        foreach (var particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (particleSystem.gameObject.activeInHierarchy && particleSystem.IsAlive(true))
            {
                return true;
            }
        }

        return false;
    }

    private void CancelSettlementDogCompleteCallbacks()
    {
        CancelDogNewCompleteCallback();
        CancelDogSpeedSetCompleteCallback();
    }

    private void CancelDogNewCompleteCallback()
    {
        if (mDogNewTrackEntry == null)
        {
            return;
        }

        mDogNewTrackEntry.Complete -= OnDogNewCompleteForSettlement;
        mDogNewTrackEntry = null;
    }

    private void CancelDogSpeedSetCompleteCallback()
    {
        if (mDogSpeedSetTrackEntry == null)
        {
            return;
        }

        mDogSpeedSetTrackEntry.Complete -= OnDogSpeedSetCompleteForSettlement;
        mDogSpeedSetTrackEntry = null;
    }

    private void CancelBoxTipsHideTimer()
    {
        if (mBoxTipsHideTimer == null)
        {
            return;
        }

        Unschedule(mBoxTipsHideTimer);
        mBoxTipsHideTimer = null;
    }

    private void CancelBoxTipsShowDelayTimer()
    {
        if (mBoxTipsShowDelayTimer == null)
        {
            return;
        }

        Unschedule(mBoxTipsShowDelayTimer);
        mBoxTipsShowDelayTimer = null;
    }

    private void CancelAnimatorWaitTimer()
    {
        if (mAnimatorWaitTimer == null)
        {
            return;
        }

        Unschedule(mAnimatorWaitTimer);
        mAnimatorWaitTimer = null;
    }

    private void CancelPanelAnimatorStartTimer()
    {
        if (mPanelAnimatorStartTimer == null)
        {
            return;
        }

        Unschedule(mPanelAnimatorStartTimer);
        mPanelAnimatorStartTimer = null;
    }

    private SkeletonGraphic EnsureDogNewSpine()
    {
        if (mDogNewSpine == null)
        {
            mDogNewSpine = FindDescendant<SkeletonGraphic>(gameObject.transform, "SkeletonGraphic (Dog_New)");
        }

        return mDogNewSpine;
    }

    private SkeletonGraphic EnsureDogSpeedSetSpine()
    {
        if (mDogSpeedSetSpine == null)
        {
            mDogSpeedSetSpine = FindDescendant<SkeletonGraphic>(gameObject.transform, "SkeletonGraphic (Dog_SpeedSet)");
        }

        return mDogSpeedSetSpine;
    }

    private SkeletonGraphic EnsureWinBgSpine()
    {
        if (mWinBgSpine == null)
        {
            mWinBgSpine = FindDescendant<SkeletonGraphic>(gameObject.transform, "SkeletonGraphic (Win_BG)");
        }

        return mWinBgSpine;
    }

    private Image EnsureBoardImage()
    {
        if (mBoardImage == null)
        {
            mBoardImage = FindDescendant<Image>(gameObject.transform, "DailyWinReward");
        }

        return mBoardImage;
    }

    private Image EnsureStarBgImage()
    {
        if (mStarBgImage == null)
        {
            mStarBgImage = FindDescendant<Image>(gameObject.transform, "star_d");
        }

        return mStarBgImage;
    }

    private SkeletonGraphic EnsureStarLoopSpine()
    {
        if (mStarLoopSpine == null)
        {
            mStarLoopSpine = FindDescendant<SkeletonGraphic>(gameObject.transform, "SkeletonGraphic (StarLoop_spine)");
        }

        return mStarLoopSpine;
    }

    private SkeletonGraphic EnsureUiDogSpine()
    {
        if (mUiDogSpine == null)
        {
            mUiDogSpine = FindDescendant<SkeletonGraphic>(gameObject.transform, "SkeletonGraphic (UI_Dog)");
        }

        return mUiDogSpine;
    }

    private RectTransform EnsureSpeekSet()
    {
        if (mSpeekSet == null)
        {
            mSpeekSet = FindDescendant<RectTransform>(gameObject.transform, "SpeekSet");
        }

        return mSpeekSet;
    }

    private Animator EnsurePanelAnimator()
    {
        if (mPanelAnimator == null)
        {
            mPanelAnimator = gameObject.GetComponent<Animator>();
        }

        return mPanelAnimator;
    }

    private DailyWinRewardItemView GetItemViewByDay(int day)
    {
        foreach (var itemView in mItemViewList)
        {
            if (itemView.ViewData?.RewardCfg != null && itemView.ViewData.RewardCfg.Day == day)
            {
                return itemView;
            }
        }

        return null;
    }

    private void RefreshHeaderTexts()
    {
        if (totalWinCount != null)
        {
            RefreshTotalWinCountDisplay();
        }
    }

    private void RefreshTotalWinCountDisplay()
    {
        if (totalWinCount == null)
        {
            return;
        }

        int targetCount = GameData.Activity.GetDailyVictoryTotalWinDays();
        int displayCount = targetCount;
        if (IsSettlementOpen())
        {
            displayCount = Mathf.Max(0, targetCount - 1);
        }

        totalWinCount.text = displayCount.ToString();
    }

    private bool IsSettlementOpen()
    {
        return mOpenParam != null && mOpenParam.FromSettlement;
    }

    private void EnableSettlementContinue()
    {
        SetSettlementContinueEnabled(true);
    }

    private void SetSettlementContinueEnabled(bool enabled)
    {
        mSettlementContinueEnabled = enabled;
        if (startButton == null)
        {
            return;
        }

        var canvasGroup = startButton.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = enabled;
        }
    }

    private void PlayTotalWinCountTransition(Action onCountChanged)
    {
        if (totalWinCount == null)
        {
            onCountChanged?.Invoke();
            return;
        }

        int targetCount = GameData.Activity.GetDailyVictoryTotalWinDays();
        int startCount = Mathf.Max(0, targetCount - 1);
        if (targetCount <= startCount)
        {
            totalWinCount.text = targetCount.ToString();
            onCountChanged?.Invoke();
            return;
        }

        var rectTransform = totalWinCount.rectTransform;
        Vector3 defaultScale = GetTotalWinCountDefaultScale();
        totalWinCount.text = startCount.ToString();
        rectTransform.localScale = defaultScale;

        DOTween.Kill(totalWinCount);
        var sequence = DOTween.Sequence()
            .SetId(this)
            .SetTarget(totalWinCount)
            .SetLink(totalWinCount.gameObject, LinkBehaviour.KillOnDestroy);
        // Three increasingly strong beats make the charge-up read as intentional rather than random shaking.
        sequence.Append(rectTransform.DOScale(defaultScale * 1.035f, TotalWinCountChargeBeatDuration).SetEase(Ease.OutQuad));
        sequence.Append(rectTransform.DOScale(defaultScale * 0.975f, TotalWinCountChargeBeatDuration).SetEase(Ease.InQuad));
        sequence.Append(rectTransform.DOScale(defaultScale * 1.055f, TotalWinCountChargeBeatDuration).SetEase(Ease.OutQuad));
        sequence.Append(rectTransform.DOScale(defaultScale * 0.965f, TotalWinCountChargeBeatDuration).SetEase(Ease.InQuad));
        sequence.Append(rectTransform.DOScale(defaultScale * 1.08f, TotalWinCountChargeBeatDuration).SetEase(Ease.OutQuad));
        sequence.Append(rectTransform.DOScale(defaultScale, TotalWinCountChargeBeatDuration).SetEase(Ease.InQuad));
        sequence.Append(rectTransform
            .DOScale(defaultScale * TotalWinCountShrinkScale, TotalWinCountShrinkDuration)
            .SetEase(Ease.InQuad));
        sequence.AppendCallback(() =>
            {
                if (totalWinCount != null)
                {
                    totalWinCount.text = targetCount.ToString();
                }

                PlayTextEff(null);
                onCountChanged?.Invoke();
            });
        sequence.Append(rectTransform
            .DOScale(defaultScale * TotalWinCountPopScale, TotalWinCountPopDuration)
            .SetEase(Ease.OutBack));
        sequence.Append(rectTransform
            .DOScale(defaultScale, TotalWinCountSettleDuration)
            .SetEase(Ease.OutQuad));
        sequence.Play();
    }

    private Vector3 GetTotalWinCountDefaultScale()
    {
        if (!mHasTotalWinCountDefaultScale && totalWinCount != null)
        {
            mTotalWinCountDefaultScale = totalWinCount.rectTransform.localScale;
            mHasTotalWinCountDefaultScale = true;
        }

        return mHasTotalWinCountDefaultScale ? mTotalWinCountDefaultScale : Vector3.one;
    }

    private void ResetTotalWinCountScale()
    {
        if (totalWinCount != null && mHasTotalWinCountDefaultScale)
        {
            totalWinCount.rectTransform.localScale = mTotalWinCountDefaultScale;
        }
    }

    private void BuildRewardItems()
    {
        var viewDataList = GameData.Activity.GetDailyWinRewardViewDatas();
        if (dailyWinReward == null || viewDataList.Count == 0)
        {
            return;
        }

        EnsureItemViews();
        if (mItemViewList.Count == 0)
        {
            return;
        }

        if (mRewardShowDlg == null)
        {
            mRewardShowDlg = dailyWinReward.GetComponentInChildren<RewardShowDlg>(true);
        }

        mRewardShowDlg?.SetOverlayClickHandler(HandleOverlayClick);
        HideBoxTips();

        for (int i = 0; i < mItemViewList.Count; i++)
        {
            var itemView = mItemViewList[i];
            bool shouldShow = i < viewDataList.Count;
            itemView.Root.SetActiveEx(shouldShow);
            if (shouldShow)
            {
                var viewData = viewDataList[i];
                if (ShouldDeferTodayItemCheckmark(viewData.RewardCfg.Day))
                {
                    viewData.IsChecked = false;
                }

                BindItemView(itemView, viewData);
            }

            if (itemView.RankAwardCell != null)
            {
                itemView.RankAwardCell.gameObject.SetActiveEx(false);
            }

            ResetItemAnimator(itemView);
        }
    }

    private DailyWinRewardItemView GetLastDayItemView()
    {
        DailyWinRewardItemView lastItemView = null;
        int maxDay = int.MinValue;

        foreach (var itemView in mItemViewList)
        {
            if (itemView.Root == null || !itemView.Root.gameObject.activeSelf)
            {
                continue;
            }

            if (itemView.ViewData?.RewardCfg == null || itemView.Toggle == null)
            {
                continue;
            }

            if (itemView.ViewData.TipRewardList == null || itemView.ViewData.TipRewardList.Count == 0)
            {
                continue;
            }

            int day = itemView.ViewData.RewardCfg.Day;
            if (day < maxDay)
            {
                continue;
            }

            maxDay = day;
            lastItemView = itemView;
        }

        return lastItemView;
    }

    private void EnsureItemViews()
    {
        if (mItemViewList.Count > 0 || dailyWinReward == null)
        {
            return;
        }

        var itemRoots = new List<RectTransform>();
        foreach (Transform child in dailyWinReward)
        {
            if (!IsRewardItemNode(child.name))
            {
                continue;
            }

            if (child is RectTransform itemRoot)
            {
                itemRoots.Add(itemRoot);
            }
        }

        itemRoots.Sort(CompareRewardItemRoot);
        foreach (var itemRoot in itemRoots)
        {
            mItemViewList.Add(CreateItemView(itemRoot));
        }
    }

    private static int CompareRewardItemRoot(RectTransform a, RectTransform b)
    {
        int orderCompare = GetRewardItemOrder(a.name).CompareTo(GetRewardItemOrder(b.name));
        return orderCompare != 0 ? orderCompare : a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
    }

    private static bool IsRewardItemNode(string nodeName)
    {
        return TryGetRewardItemOrder(nodeName, out _);
    }

    private static int GetRewardItemOrder(string nodeName)
    {
        return TryGetRewardItemOrder(nodeName, out int order) ? order : int.MaxValue;
    }

    private static bool TryGetRewardItemOrder(string nodeName, out int order)
    {
        order = int.MaxValue;
        if (nodeName == "item")
        {
            order = 0;
            return true;
        }

        if (!nodeName.StartsWith("item"))
        {
            return false;
        }

        int openParen = nodeName.IndexOf('(');
        if (openParen < 0)
        {
            return false;
        }

        int closeParen = nodeName.IndexOf(')', openParen + 1);
        if (closeParen <= openParen)
        {
            return false;
        }

        if (!int.TryParse(nodeName.Substring(openParen + 1, closeParen - openParen - 1), out int index))
        {
            return false;
        }

        order = index;
        return true;
    }

    private DailyWinRewardItemView CreateItemView(RectTransform itemRoot)
    {
        var rankAwardCell = FindDescendant<RectTransform>(itemRoot, "RankAwardCell");
        var toggle = FindDescendant<Toggle>(itemRoot, "Toggle");
        var toggleImage = toggle != null ? toggle.image : null;
        if (toggleImage == null && toggle != null)
        {
            toggleImage = toggle.GetComponent<Image>();
        }

        var itemView = new DailyWinRewardItemView
        {
            Root = itemRoot,
            Toggle = toggle,
            ToggleImage = toggleImage,
            UsesRewardD2 = IsRewardD2Sprite(toggleImage != null ? toggleImage.sprite : null),
            RankAwardCell = rankAwardCell,
            RankAwardCellInitialPos = rankAwardCell != null ? rankAwardCell.anchoredPosition : Vector2.zero,
            ItemAnimator = itemRoot.GetComponent<Animator>(),
        };

        CollectRankAwardNodes(itemView);

        if (itemView.Toggle != null)
        {
            itemView.Label = FindDescendant<TextMeshProUGUI>(itemView.Toggle.transform, "Label");
            itemView.Toggle.onValueChanged.AddListener(_ => OnItemToggleClicked(itemView));
            ConfigureItemTogglePressScale(itemView);
        }

        return itemView;
    }

    private void BindItemView(DailyWinRewardItemView itemView, ActivityData.DailyWinRewardViewData viewData)
    {
        itemView.ViewData = viewData;
        if (itemView.Label != null)
        {
            int cycleDay = GameData.Activity.GetDailyVictoryStreakDay();
            int anchorDay = cycleDay <= 0 ? 1 : cycleDay;
            int dayOffset = viewData.RewardCfg.Day - anchorDay;
            LocalizedTMPRefresher.SetText(itemView.Label, () => GetWeekdayLabel(dayOffset));
        }

        EnsureAndPopulateRankAwardNodes(itemView, viewData?.TipRewardList);

        if (itemView.Toggle == null)
        {
            return;
        }

        ApplyItemToggleCheckedState(itemView.Toggle, viewData.IsChecked);
        ConfigureItemTogglePressScale(itemView);
    }

    private void OnItemToggleClicked(DailyWinRewardItemView itemView)
    {
        if (itemView.Toggle == null)
        {
            return;
        }

        CancelBoxTipsHideTimer();

        AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        bool shouldCheck = itemView.ViewData != null && itemView.ViewData.IsChecked;
        ApplyItemToggleCheckedState(itemView.Toggle, shouldCheck);
        OnItemClicked(itemView);
    }

    private void OnItemClicked(DailyWinRewardItemView itemView)
    {
        if (mRewardShowDlg == null || itemView.Toggle == null || itemView.ViewData == null)
        {
            return;
        }

        mRewardShowDlg.SetOverlayClickHandler(HandleOverlayClick);
        RefreshToggleCheckedStates();
        mRewardShowDlg.Show(itemView.Toggle.transform as RectTransform, itemView.ViewData.TipRewardList);
    }

    private void RefreshToggleCheckedStates()
    {
        foreach (var itemView in mItemViewList)
        {
            if (itemView.Toggle == null || itemView.ViewData == null)
            {
                continue;
            }

            ApplyItemToggleCheckedState(itemView.Toggle, itemView.ViewData.IsChecked);
        }
    }

    private bool IsBoxTipsVisible()
    {
        return mRewardShowDlg != null && mRewardShowDlg.IsVisible;
    }

    private void HideBoxTips()
    {
        CancelBoxTipsShowDelayTimer();
        CancelBoxTipsHideTimer();
        mRewardShowDlg?.Hide();
    }

    private bool HandleOverlayClick(Vector2 screenPosition)
    {
        if (TryGetPanelButtonAtScreenPosition(screenPosition, out var button))
        {
            if (button == startButton && IsSettlementOpen() && !mSettlementContinueEnabled)
            {
                return true;
            }

            OnClicked(button);
            return true;
        }

        if (TryGetItemViewAtScreenPosition(screenPosition, out var itemView))
        {
            OnItemToggleClicked(itemView);
            return true;
        }

        return false;
    }

    private bool TryGetPanelButtonAtScreenPosition(Vector2 screenPosition, out Button button)
    {
        button = null;
        if (!TryRaycastAtScreenPosition(screenPosition, out var results))
        {
            return false;
        }

        foreach (var result in results)
        {
            if (IsBoxTipsOverlayRaycastTarget(result.gameObject))
            {
                continue;
            }

            var panelButton = result.gameObject.GetComponentInParent<Button>();
            if (panelButton != closeButton && panelButton != startButton)
            {
                continue;
            }

            button = panelButton;
            return true;
        }

        return false;
    }

    private bool TryGetItemViewAtScreenPosition(Vector2 screenPosition, out DailyWinRewardItemView itemView)
    {
        itemView = null;
        if (!TryRaycastAtScreenPosition(screenPosition, out var results))
        {
            return false;
        }

        foreach (var result in results)
        {
            if (IsBoxTipsOverlayRaycastTarget(result.gameObject))
            {
                continue;
            }

            var toggle = result.gameObject.GetComponentInParent<Toggle>();
            if (toggle == null)
            {
                continue;
            }

            foreach (var view in mItemViewList)
            {
                if (view.Toggle != toggle)
                {
                    continue;
                }

                itemView = view;
                return true;
            }
        }

        return false;
    }

    private bool TryRaycastAtScreenPosition(Vector2 screenPosition, out List<RaycastResult> results)
    {
        results = null;
        if (EventSystem.current == null)
        {
            return false;
        }

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition,
        };
        results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    private bool IsBoxTipsOverlayRaycastTarget(GameObject go)
    {
        if (mRewardShowDlg == null || go == null)
        {
            return false;
        }

        if (!go.transform.IsChildOf(mRewardShowDlg.transform))
        {
            return false;
        }

        return !mRewardShowDlg.IsAwardContent(go);
    }

    private static string GetWeekdayLabel(int dayOffsetFromToday)
    {
        var targetDate = System.DateTime.Now.Date.AddDays(dayOffsetFromToday);
        int dayOfWeekIndex = (int)targetDate.DayOfWeek;
        if (dayOfWeekIndex < 0 || dayOfWeekIndex >= WeekdayTermKeys.Length)
        {
            return string.Empty;
        }

        return I2LocalizeMgr.GetString(WeekdayTermKeys[dayOfWeekIndex]);
    }

    private static T FindDescendant<T>(Transform root, string nodeName) where T : Component
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == nodeName)
            {
                return child.GetComponent<T>();
            }
        }

        return null;
    }
}
