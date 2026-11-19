using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishXSortPlay.Map;
using XSortGameCore;
using XSortGamePlay;
using FishFramework;


namespace FishXSortPlay
{
    public class UIMainMenu : UIPage
    {
        public readonly float STORE_AD_RIGHT_OFFSET_X = 300F;

        [SerializeField] RectTransform safeAreaRectTransform;

        [Space]
        [SerializeField] RectTransform tapToPlayRect;
        [SerializeField] Button playButton;
        [SerializeField] TMP_Text normalLevelText;
        [SerializeField] TMP_Text infinitLevelText;
        [SerializeField] TextMeshProUGUI toroxPlayCntText;
        [SerializeField] Image toroxPlayCntImg;
        [SerializeField] GameObject normalLevelObj;
        [SerializeField] GameObject infinitLevelObj;
        [SerializeField] GameObject infinitHintObj;
        [SerializeField] TMP_Text difficultyText;
        [SerializeField] Image normalPlayButtonImage;

        [Space]
        [SerializeField] UIScaleAnimation coinsLabelScalable;
        [SerializeField] CoinPanelView coinsPanel;
        [SerializeField] RectTransform currencyPanelRect;

        [SerializeField] UIScaleAnimation settingsScalable;

        [SerializeField] RectTransform avatarBtn;
        [SerializeField] UIMainMenuButton noAdsButton;
        [SerializeField] UIMainMenuButton dailyGiftButton;
        [SerializeField] UIMainMenuButton dailyWinRewardTipButton;

        [SerializeField] private UIMainMenuSpineCtrl spineCtrl;
        [SerializeField] private DifficultyShow difficultyShow;
        [SerializeField] PiggyBankBarView piggyBankBar;
        // [SerializeField] private Image userHeadIcon;

        private TweenClip mDailyWinRewardTipButtonScaleTween;
        private TweenClip showHideStoreAdButtonDelayTweenCase;
        private int enterMenuTime = 1;

        private int m_currentLevel;
        private Image mDailyWinRewardFireIcon;
        private Vector3? mDailyWinRewardTipButtonDefaultScale;
        private PuppyAdventureIcon mPuppyAdventureIcon;
        private Vector2? mPuppyAdventureHomePos;
        private static bool sPuppyPopupHandledThisSession;
        private bool mCachedTopLeftInitialPos;
        private float mCurrencyPosXWithAvatar;
        private float mDailyRewardPosXWithAvatar;
        private float mAvatarPosX;

        const string PlayBtnGreenPath = "Assets/CABRes/SpriteAtlas/GPBtn/XSort_btn_green.png";
        const string PlayBtnRedPath = "Assets/CABRes/SpriteAtlas/GPBtn/XSort_btn_red.png";
        const string PlayBtnPurplePath = "Assets/CABRes/SpriteAtlas/GPBtn/XSort_btn_purple.png";
        const string PlayBtnCoinPath = "Assets/CABRes/SpriteAtlas/GPBtn/XSort_btn_coinreward.png";
        Sprite mCachedGreenBtnSprite;
        Sprite mCachedRedBtnSprite;
        Sprite mCachedPurpleBtnSprite;
        Sprite mCachedCoinBtnSprite;
        Sprite mCachedThemeBackgroundSprite;
        RectTransform mPlayTextRect;
        float mPlayTextDefaultPosY;
        bool mCachedPlayTextPosY;

        private void Update()
        {
            if (PiggyBankState.EnsureDay())
                RefreshPiggyBank();
        }

        private void OnEnable()
        {
            GameEvent.AddEventListener((int)EventNameDef.DailyVictoryWinRecorded, OnDailyVictoryWinRecorded);
            RefreshPuppyAdventure();
            RefreshPiggyBank();
            if (GameData.BFromStartScene)
            {
                CheckShowDailyWinRewardBtn();
            }
        }

        private void OnDisable()
        {
            GameEvent.RemoveEventListener((int)EventNameDef.DailyVictoryWinRecorded, OnDailyVictoryWinRecorded);
        }

        void OnDailyVictoryWinRecorded()
        {
            CheckShowDailyWinRewardBtn();
        }

        void EnsurePuppyAdventureEntry()
        {
            if (mPuppyAdventureIcon == null)
                mPuppyAdventureIcon = GetComponentInChildren<PuppyAdventureIcon>(true);
            if (mPuppyAdventureIcon == null)
                return;
            mPuppyAdventureIcon.EnsureClickable();
        }

        void RefreshPuppyAdventure()
        {
            EnsurePuppyAdventureEntry();
            if (mPuppyAdventureIcon == null)
                return;

            bool show = PuppyAdventureDemoState.IsMainEntryVisible;
            mPuppyAdventureIcon.gameObject.SetActive(show);
            if (show)
                mPuppyAdventureIcon.Refresh();
            LayoutActivityEnterIcons();
        }

        void RefreshPiggyBank()
        {
            piggyBankBar?.ApplyMainMenu();
        }

        /// <summary>
        /// 主界面活动入口布局。冒险图标保持自己的位置。
        /// </summary>
        void LayoutActivityEnterIcons()
        {
            EnsurePuppyAdventureEntry();
            if (mPuppyAdventureIcon == null)
                return;

            var iconRt = mPuppyAdventureIcon.transform as RectTransform;
            if (iconRt == null)
                return;

            if (!mPuppyAdventureHomePos.HasValue)
                mPuppyAdventureHomePos = iconRt.anchoredPosition;

            iconRt.anchoredPosition = mPuppyAdventureHomePos.Value;
        }

        protected override bool OnEscButtonPressed()
        {
            GameModule.UI.OpenPanel<UIQuitGameDlg>();
            return true;
        }

        protected void AddEvent()
        {
            GameEvent.AddEventListener((int)EventNameDef.BuyNoAdPackSuccess, CheckShowAdsBtn);
        }

        protected void RemoveEvent()
        {
            GameEvent.RemoveEventListener((int)EventNameDef.BuyNoAdPackSuccess, CheckShowAdsBtn);
        }

        public void EnterUserInfoDlg()
        {
            GameModule.UI.OpenPanel<UIUserInfoDlg>();
        }


        void Start()
        {
            AddEvent();
        }

        void OnDestroy()
        {
            RemoveEvent();
        }

        public override void Init()
        {
            if (spineCtrl == null)
                spineCtrl = GetComponent<UIMainMenuSpineCtrl>();

            coinsPanel.SetAutoGainAnim(true);
            coinsPanel.SetupPanel();

            noAdsButton.Button.onClick.AddListener(NoAdButton);
            playButton.onClick.AddListener(PlayButton);
            dailyGiftButton.Button.onClick.AddListener(DailyGiftButton);

            EnsureDailyWinRewardRefs();
            CacheTopLeftInitialPositions();
            EnsurePuppyAdventureEntry();

            if (dailyWinRewardTipButton.IsConfigured)
            {
                dailyWinRewardTipButton.Button.onClick.AddListener(DailyWinRewardTipButton);
                CacheDailyWinRewardTipButtonDefaultScale();
            }
            else
            {
                Debug.LogWarning("UIMainMenu: DailyRewardButton is missing or unwired.");
            }

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
        }

        private bool mBShowDaily = false;

        #region Show/Hide

        //返回主界面需要统一拉取一些信息
        public void CommonReq()
        {
            if (!GameData.BFromStartScene)
            {
                return;
            }
            ShowToroxPlayCnt();
            GameBAData.ExcuteCommonEvent();
            GPSDKMgr.Instance.QueryCurrencyInfo();
            GPSDKMgr.Instance.QueryCompleteOrders();

            if (GPUserDataManager.Instance.GetLevelNum() > 9 && GPSDKMgr.Instance.GetBShowNotificationRequested())
            {
                GameModule.UI.OpenPanel<UIApplyNotificationDlg>();
            }
        }

        void ShowToroxPlayCnt()
        {
            if (!GameData.BFromStartScene)
            {
                return;
            }

            bool bShow = GPUserDataManager.Instance.GetBInfinitMode();
            toroxPlayCntImg.SetActiveEx(bShow);
            if (bShow)
            {
                LocalizedTMPRefresher.SetText(toroxPlayCntText, () =>
                    string.Format(I2LocalizeMgr.GetString("UI_CumulativeWins"), GPUserDataManager.Instance.GetPassLevelCnt().ToString()));
            }
        }


        void CheckShowAdsBtn()
        {
            //是否显示无广告
            int levelIndex = GPUserDataManager.Instance.GetLevelNum();
            noAdsButton.Button.SetActiveEx(levelIndex > 7 && (!GPUserDataManager.Instance.GetBNoAd()));
        }

        void EnsureDailyWinRewardRefs()
        {
            dailyWinRewardTipButton.TryBindFromHierarchy(transform, "DailyRewardButton");
        }

        void EnsureAvatarAndCurrencyRefs()
        {
            if (avatarBtn == null)
            {
                var found = FindDeepChild(transform, "Avatarbtn");
                if (found != null)
                {
                    avatarBtn = found as RectTransform;
                }
            }

            if (currencyPanelRect == null)
            {
                if (coinsPanel != null)
                {
                    currencyPanelRect = coinsPanel.transform as RectTransform;
                }
                else if (coinsLabelScalable != null && coinsLabelScalable.Transform != null)
                {
                    currencyPanelRect = coinsLabelScalable.Transform as RectTransform;
                }
            }
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

        void CacheTopLeftInitialPositions()
        {
            if (mCachedTopLeftInitialPos)
            {
                return;
            }

            EnsureDailyWinRewardRefs();
            EnsureAvatarAndCurrencyRefs();

            if (avatarBtn != null)
            {
                mAvatarPosX = avatarBtn.anchoredPosition.x;
            }

            if (currencyPanelRect != null)
            {
                mCurrencyPosXWithAvatar = currencyPanelRect.anchoredPosition.x;
            }

            if (dailyWinRewardTipButton.IsConfigured)
            {
                var dailyRect = dailyWinRewardTipButton.Button.transform as RectTransform;
                if (dailyRect != null)
                {
                    mDailyRewardPosXWithAvatar = dailyRect.anchoredPosition.x;
                }
            }

            mCachedTopLeftInitialPos = true;
        }

        void RefreshAvatarAndTopLeftOffset(bool showAvatar)
        {
            CacheTopLeftInitialPositions();

            if (avatarBtn != null)
            {
                avatarBtn.SetActiveEx(showAvatar);
            }

            // 有头像：还原预制体默认 X；无头像：Currency / DailyReward 与头像 X 对齐
            float currencyX = showAvatar ? mCurrencyPosXWithAvatar : mAvatarPosX;
            float dailyX = showAvatar ? mDailyRewardPosXWithAvatar : mAvatarPosX;

            if (currencyPanelRect != null)
            {
                var pos = currencyPanelRect.anchoredPosition;
                pos.x = currencyX;
                currencyPanelRect.anchoredPosition = pos;
            }

            if (dailyWinRewardTipButton.IsConfigured)
            {
                var dailyRect = dailyWinRewardTipButton.Button.transform as RectTransform;
                if (dailyRect != null)
                {
                    var pos = dailyRect.anchoredPosition;
                    pos.x = dailyX;
                    dailyRect.anchoredPosition = pos;
                }
            }
        }

        void CheckShowDailyWinRewardBtn()
        {
            EnsureDailyWinRewardRefs();

            if (GameData.Activity == null)
            {
                Tween.DelayedCall(0.1f, CheckShowDailyWinRewardBtn);
                return;
            }

            bool showButton = GameData.Activity.IsDailyVictoryActEnabled();
            Debug.Log($"[AdDiag] avatar show={showButton} uid={GPUserDataManager.Instance.GetUID()} head={GPUserDataManager.Instance.UserData.userhead}");
            if (dailyWinRewardTipButton.IsConfigured)
            {
                dailyWinRewardTipButton.Button.SetActiveEx(showButton);
                if (showButton)
                {
                    RefreshDailyWinRewardButton();
                    if (UIController.IsDisplayed<UIMainMenu>())
                    {
                        ShowDailyWinRewardTipButton(true);
                    }
                }
            }

            RefreshAvatarAndTopLeftOffset(showButton);
        }

        void CacheDailyWinRewardTipButtonDefaultScale()
        {
            if (!dailyWinRewardTipButton.IsConfigured)
            {
                return;
            }

            var rect = dailyWinRewardTipButton.Button.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            // Don't cache while root is hide-scaled to ~0 (would poison later shows to 1).
            var scale = rect.localScale;
            if (scale.x >= 0.5f && scale.y >= 0.5f && scale.z >= 0.5f)
                mDailyWinRewardTipButtonDefaultScale = scale;
            else if (!mDailyWinRewardTipButtonDefaultScale.HasValue)
                mDailyWinRewardTipButtonDefaultScale = new Vector3(0.65f, 0.65f, 0.65f);
        }

        Vector3 GetDailyWinRewardTipButtonDefaultScale()
        {
            if (!mDailyWinRewardTipButtonDefaultScale.HasValue)
            {
                CacheDailyWinRewardTipButtonDefaultScale();
            }

            return mDailyWinRewardTipButtonDefaultScale ?? Vector3.one;
        }

        public void RefreshDailyWinRewardButton()
        {
            RefreshDailyWinRewardButtonText();
            RefreshDailyWinRewardFireIcon();
            RefreshMainMenuHead();
        }

        void RefreshMainMenuHead()
        {
            var mainMenuHead = GetComponentInChildren<MainMenuHead>(true);
            mainMenuHead?.RefreshDisplay();
        }

        void RefreshDailyWinRewardButtonText()
        {
            if (!dailyWinRewardTipButton.IsConfigured)
            {
                return;
            }

            var text = FindDeepChild(dailyWinRewardTipButton.Button.transform, "text")?.GetComponent<TMP_Text>();
            if (text == null)
            {
                return;
            }

            LocalizedTMPRefresher.SetText(text, () => GameData.Activity.GetDailyVictoryStreakButtonLabel());
        }

        void RefreshDailyWinRewardFireIcon()
        {
            if (!dailyWinRewardTipButton.IsConfigured)
            {
                return;
            }

            if (mDailyWinRewardFireIcon == null)
            {
                mDailyWinRewardFireIcon = FindDeepChild(dailyWinRewardTipButton.Button.transform, "fireicon")
                    ?.GetComponent<Image>();
            }

            if (mDailyWinRewardFireIcon == null)
            {
                return;
            }

            string spriteName = GameData.Activity.GetDailyVictoryMainMenuFireIconSpriteName();
            var sprite = UILoadHelper.LoadAsset<Sprite>(
                $"Assets/CABRes/SpriteAtlas/DailyWin/{spriteName}.png");
            if (sprite != null)
            {
                mDailyWinRewardFireIcon.sprite = sprite;
            }
        }

        void SetLevelShow()
        {
            bool isInfinite = GPUserDataManager.Instance.GetBInfinitMode();
            infinitLevelObj.SetActive(isInfinite);
            infinitHintObj.SetActive(false);
            normalLevelObj.SetActive(!isInfinite);

            LocalizedTMPRefresher.SetText(infinitLevelText, () =>
                I2LocalizeMgr.GetString("LevelName").TrimEnd() + " " + (GPUserDataManager.Instance.GetShowLevelNum() + 1).ToString());
            int showLevel = GPUserDataManager.Instance.GetShowLevelNum() + 1;
            LocalizedTMPRefresher.SetText(normalLevelText, () =>
                AwardColorEggCoverConfig.IsEnabled(showLevel)
                    ? AwardColorEggCoverText.BonusButton()
                    : I2LocalizeMgr.GetString("LevelName").TrimEnd() + " " + showLevel);

            if (difficultyShow != null)
                difficultyShow.gameObject.SetActive(false);

            if (isInfinite)
            {
                ApplyPlayTextPosY(center: true);
                ApplyTheme();
                return;
            }

            RefreshMainLevelDifficulty();
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            int XSortbaseId = XSortBaseThemeResolver.ResolveCurrentLevelXSortBaseId();
            var pack = ThemeUiApplier.Get(XSortbaseId);
            ThemeUiApplier.ApplyButtonThemeStyle(
                normalLevelObj != null ? normalLevelObj.transform : null,
                pack?.button,
                pack?.buttonAppearMaterial);
            ApplyThemeBackground(XSortbaseId);
        }

        void ApplyThemeBackground(int XSortbaseId)
        {
            Transform node = transform.Find("themebackground");
            Image image = node != null ? node.GetComponent<Image>() : null;
            if (image == null)
            {
                return;
            }

            if (mCachedThemeBackgroundSprite == null)
            {
                mCachedThemeBackgroundSprite = image.sprite;
            }

            Sprite sprite = ThemeUiApplier.Get(XSortbaseId)?.mainBg;
            if (sprite == null)
            {
                sprite = mCachedThemeBackgroundSprite;
            }
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }

        void EnsureMainLevelDifficultyRefs()
        {
            if (difficultyText == null && normalLevelObj != null)
            {
                var node = normalLevelObj.transform.Find("diffcultyNode");
                if (node != null)
                    difficultyText = node.GetComponent<TMP_Text>();
            }

            if (normalPlayButtonImage == null && normalLevelObj != null)
                normalPlayButtonImage = normalLevelObj.GetComponent<Image>();

            if (mPlayTextRect == null && normalLevelText != null)
                mPlayTextRect = normalLevelText.rectTransform;

            if (!mCachedPlayTextPosY && mPlayTextRect != null)
            {
                mPlayTextDefaultPosY = mPlayTextRect.anchoredPosition.y;
                mCachedPlayTextPosY = true;
            }
        }

        void ApplyPlayTextPosY(bool center)
        {
            EnsureMainLevelDifficultyRefs();
            if (mPlayTextRect == null)
                return;

            var pos = mPlayTextRect.anchoredPosition;
            pos.y = center ? 7f : mPlayTextDefaultPosY;
            mPlayTextRect.anchoredPosition = pos;
        }

        void RefreshMainLevelDifficulty()
        {
            EnsureMainLevelDifficultyRefs();

            int difficulty = 0;
            bool isCoinLevel = false;
            if (GameData.BFromStartScene)
            {
                var config = LevelDynamicConfigProvider.GetLevelConfig(
                    GPUserDataManager.Instance.GetFinalLevelNum() + 1);
                if (config != null)
                {
                    difficulty = config.Difficulty;
                    isCoinLevel = ActivityData.IsCoinRewardLevelEnabled() && config.Coinlevel == 1;
                }
            }

            bool showDifficultyText = difficulty == 1 || difficulty == 2;
            ApplyPlayTextPosY(!showDifficultyText);

            PlayBtnDifficultyFont.Apply(normalLevelText, difficulty, isCoinLevel: isCoinLevel);
            PlayBtnDifficultyFont.Apply(difficultyText, difficulty, isCoinLevel: isCoinLevel);

            if (difficultyText != null)
            {
                difficultyText.gameObject.SetActive(showDifficultyText);
                if (showDifficultyText)
                {
                    int difficultyKey = difficulty;
                    LocalizedTMPRefresher.SetText(difficultyText, () =>
                        I2LocalizeMgr.GetString("DifficultyName" + difficultyKey));
                }
            }

            if (normalPlayButtonImage != null)
            {
                var sprite = LoadPlayButtonSprite(difficulty, isCoinLevel);
                if (sprite != null)
                    normalPlayButtonImage.sprite = sprite;
            }
        }

        Sprite LoadPlayButtonSprite(int difficulty, bool isCoinLevel)
        {
            if (isCoinLevel)
                return LoadCachedPlayButtonSprite(ref mCachedCoinBtnSprite, PlayBtnCoinPath);
            if (difficulty == 1)
                return LoadCachedPlayButtonSprite(ref mCachedRedBtnSprite, PlayBtnRedPath);
            if (difficulty == 2)
                return LoadCachedPlayButtonSprite(ref mCachedPurpleBtnSprite, PlayBtnPurplePath);
            return LoadCachedPlayButtonSprite(ref mCachedGreenBtnSprite, PlayBtnGreenPath);
        }

        static Sprite LoadCachedPlayButtonSprite(ref Sprite cached, string path)
        {
            if (cached != null)
                return cached;

            cached = UILoadHelper.LoadAsset<Sprite>(path);
            return cached;
        }

        public override void PlayShowAnimation()
        {
            DockActor.HideSubmitImpactEffectsForSettlement();

            GPUserDataManager.Instance?.UserData?.TryResetCircleAwardCountsByDaily5AM();
            NextDayFirstEnterEase.TryMarkPendingOnLogin();
            // Activity may be null when GameScene opens without GameData.RegisterData (e.g. editor Test Level).
            GameData.Activity?.TryResetDailyVictoryStreakOnLogin();
            CommonReq();
            NetDetectController.NotifyMainCity();

            if (GameData.BFromStartScene)
            {
                bool showCircleAward = GameData.Activity != null
                    && GameData.Activity.IsCircleAwardEnabled()
                    && GameData.UnlockData != null
                    && GameData.UnlockData.GetCircleDailyAwardUnlock();
                if (dailyGiftButton.IsConfigured)
                {
                    dailyGiftButton.Button.SetActiveEx(showCircleAward);
                }
                CheckShowDailyWinRewardBtn();
                RefreshMainMenuHead();
            }

            LevelDirector.ShowOrHideLevelNode(false);
            mPlayClickBusy = false;
            showHideStoreAdButtonDelayTweenCase?.Kill();

            HideAdButton(true);
            ShowTapToPlay();
            if (dailyWinRewardTipButton.IsConfigured
                && dailyWinRewardTipButton.Button.gameObject.activeSelf)
            {
                ShowDailyWinRewardTipButton();
            }

            spineCtrl?.OnShowPrepare();
            Tween.DelayedCall(0.05f, () => spineCtrl?.OnShowDelayedRestoreAndPlay());

            //是否显示无广告
            CheckShowAdsBtn();

            coinsLabelScalable.Show();
            coinsPanel.Activate();
            ShowSettingsButton();
            // UILevelNumberText.Show();


            if (GameData.BFromStartScene)
            {
                m_currentLevel = GPUserDataManager.Instance.GetFinalLevelNum();
            }
            else
            {
                m_currentLevel = LevelDirector.MaxReachedLevelIndex;
            }
            SetLevelShow();
            HideGoldActivityEntry();
            RefreshPuppyAdventure();
            RefreshPiggyBank();
            showHideStoreAdButtonDelayTweenCase = Tween.DelayedCall(0.12f, delegate
            {
                ShowAdButton();
            });

            MapLevelAbstractActor.OnLevelClicked += OnLevelOnMapSelected;
            UIController.OnPageOpened(this);

            // Bottom nav needs GameModule.UI; skip when GameScene opens without bootstrap (e.g. editor Test Level).
            if (GameData.BFromStartScene)
            {
                UIBottomNav.Show(BottomNavTab.Home);
            }

            Tween.DelayedCall(0.3f, TryAutoShowMainCityEntryDlgs);

            if (GameData.BFromStartScene)
            {
                Tween.DelayedCall(1.05f, () =>
                {
                    if (this != null && UIController.IsDisplayed<UIMainMenu>())
                    {
                        XSortAppUpdate.TryShow("Home.Delay");
                    }
                });
            }
        }

        void TryAutoShowMainCityEntryDlgs()
        {
            if (!UIController.IsDisplayed<UIMainMenu>())
            {
                return;
            }

            // Pinata 未领补弹优先级最高，关完再走其它补领弹窗
            if (PiggyBankFlow.TryShowPendingClaimOnHome(ContinueMainCityDlgsAfterPiggy))
                return;

            ContinueMainCityDlgsAfterPiggy();
        }

        void ContinueMainCityDlgsAfterPiggy()
        {
            if (!UIController.IsDisplayed<UIMainMenu>())
                return;
            if (AwardColorEggCoverPending.TryShowPendingOnHome(ContinueMainCityDlgsAfterColorEgg))
                return;
            ContinueMainCityDlgsAfterColorEgg();
        }

        void ContinueMainCityDlgsAfterColorEgg()
        {
            if (!UIController.IsDisplayed<UIMainMenu>())
                return;
            UIGameFunc.TryShowPendingDailyVictoryRewardGetDlg(ShowFeedbackThenRest);
        }

        void ShowFeedbackThenRest()
        {
            if (!UIController.IsDisplayed<UIMainMenu>())
            {
                return;
            }

            if (GPSDKMgr.Instance.GetBShowFeedback())
            {
                GPSDKMgr.Instance.ResetBShowFeedback();
                UIGameFunc.OpenFeedbackDlg(ShowCircleAwardThenPuppy);
                return;
            }

            ShowCircleAwardThenPuppy();
        }

        void ShowCircleAwardThenPuppy()
        {
            if (!UIController.IsDisplayed<UIMainMenu>())
            {
                return;
            }

            if (GameData.BFromStartScene
                && GPUserDataManager.Instance.UserData.GetTotalCircleAwardCnt() == 0
                && !mBShowDaily)
            {
                mBShowDaily = true;
                Tween.DelayedCall(0.5f, () =>
                {
                    if (!UIController.IsDisplayed<UIMainMenu>())
                    {
                        mBShowDaily = false;
                        return;
                    }

                    var panel = UIGameFunc.OpenCircleAwardActivity();
                    GameBAData.ExcuteGameTriggerActivity("2", "spin", 1);
                    if (panel != null)
                    {
                        panel.closeExtraAct = ShowPuppyEntryPopup;
                        return;
                    }

                    ShowPuppyEntryPopup();
                }, unscaledTime: true);
                return;
            }

            ShowPuppyEntryPopup();
        }

        void ShowPuppyEntryPopup()
        {
            if (!UIController.IsDisplayed<UIMainMenu>() || sPuppyPopupHandledThisSession)
            {
                return;
            }

            if (!PuppyAdventureDemoState.IsMainEntryVisible)
            {
                return;
            }

            sPuppyPopupHandledThisSession = true;
            PuppyAdventureFlow.TryShowPendingPopup();
        }


        public override void PlayHideAnimation()
        {
            spineCtrl?.StopAll();
            spineCtrl?.SetSpineGraphicsEnabled(false);

            showHideStoreAdButtonDelayTweenCase?.Kill();


            coinsLabelScalable.Hide(immediately: true);
            HideSettingsButton(immediately: true);
            HideTapToPlayButton(immediately: true);
            HideDailyWinRewardTipButton(immediately: true);
            HideAdButton(immediately: true);

            UIBottomNav.Hide();
            UIGameFunc.CloseCircleAwardActivity();

            MapLevelAbstractActor.OnLevelClicked -= OnLevelOnMapSelected;
            UIController.OnPageClosed(this);
        }

        #endregion

        #region Tap To Play Label

        static void CancelTButtonPress(Component target)
        {
            if (target == null)
                return;
            var tButton = target.GetComponent<TButton>();
            tButton?.CancelPressTween();
        }

        static void SyncTButtonRestingScale(Component target, Vector3 scale)
        {
            if (target == null)
                return;
            var tButton = target.GetComponent<TButton>();
            if (tButton != null)
            {
                // Press scale on an inner visual: root owns show/hide scale
                // (e.g. DailyReward default 0.65); child resting stays at 1.
                if (tButton.UsesExternalScaleTarget)
                {
                    target.transform.localScale = scale;
                    tButton.SetRestingScale(Vector3.one);
                }
                else
                    tButton.SetRestingScale(scale);
            }
            else
                target.transform.localScale = scale;
        }

        void ShowSettingsButton()
        {
            CancelTButtonPress(settingsScalable.Transform);
            settingsScalable.Show(onCompleted: () =>
            {
                SyncTButtonRestingScale(settingsScalable.Transform, Vector3.one);
            });
        }

        void HideSettingsButton(bool immediately = false)
        {
            CancelTButtonPress(settingsScalable.Transform);
            settingsScalable.Hide(immediately: immediately);
        }

        public void ShowTapToPlay(bool immediately = false)
        {
            CancelTButtonPress(playButton);

            if (immediately)
            {
                SyncTButtonRestingScale(playButton, Vector3.one);
                return;
            }

            // RESET
            tapToPlayRect.localScale = Vector3.zero;

            Tween.DOPushScale(tapToPlayRect, Vector3.one * 1.2f, Vector3.one, 0.35f, 0.2f, DG.Tweening.Ease.OutCubic, DG.Tweening.Ease.InCubic, 0).OnComplete(delegate
            {
                SyncTButtonRestingScale(playButton, Vector3.one);
            });

        }

        public void HideTapToPlayButton(bool immediately = false)
        {
            CancelTButtonPress(playButton);

            if (immediately)
            {
                tapToPlayRect.localScale = Vector3.zero;

                return;
            }

            tapToPlayRect.DOTScale(Vector3.zero, 0.3f).SetEasing(DG.Tweening.Ease.InCubic);
        }

        void ShowDailyWinRewardTipButton(bool immediately = false)
        {
            if (!dailyWinRewardTipButton.IsConfigured
                || !dailyWinRewardTipButton.Button.gameObject.activeInHierarchy)
            {
                return;
            }

            var rect = dailyWinRewardTipButton.Button.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            if (mDailyWinRewardTipButtonScaleTween != null && mDailyWinRewardTipButtonScaleTween.IsActive)
            {
                mDailyWinRewardTipButtonScaleTween.Kill();
            }

            CancelTButtonPress(dailyWinRewardTipButton.Button);
            DG.Tweening.DOTween.Kill(rect);

            var targetScale = GetDailyWinRewardTipButtonDefaultScale();

            if (immediately)
            {
                SyncTButtonRestingScale(dailyWinRewardTipButton.Button, targetScale);
                return;
            }

            rect.localScale = Vector3.zero;
            mDailyWinRewardTipButtonScaleTween = Tween.DOPushScale(
                rect,
                targetScale * 1.2f,
                targetScale,
                0.35f,
                0.2f,
                DG.Tweening.Ease.OutCubic,
                DG.Tweening.Ease.InCubic,
                0).OnComplete(() =>
            {
                SyncTButtonRestingScale(dailyWinRewardTipButton.Button, targetScale);
            });
        }

        void HideDailyWinRewardTipButton(bool immediately = false)
        {
            if (mDailyWinRewardTipButtonScaleTween != null && mDailyWinRewardTipButtonScaleTween.IsActive)
            {
                mDailyWinRewardTipButtonScaleTween.Kill();
            }

            if (!dailyWinRewardTipButton.IsConfigured
                || !dailyWinRewardTipButton.Button.gameObject.activeInHierarchy)
            {
                return;
            }

            var rect = dailyWinRewardTipButton.Button.transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            CancelTButtonPress(dailyWinRewardTipButton.Button);

            if (immediately)
            {
                rect.localScale = Vector3.zero;
                return;
            }

            rect.DOTScale(Vector3.zero, 0.3f).SetEasing(DG.Tweening.Ease.InCubic);
        }

        #endregion

        #region Ad Button Label

        private void ShowAdButton(bool immediately = false)
        {
        }

        private void HideAdButton(bool immediately = false)
        {
        }

        private void OnAdPurchased()
        {
        }

        #endregion

        #region Buttons

        bool mPlayClickBusy;

        private void PlayButton()
        {
            if (XSortAppUpdate.TryShow("Home.Play"))
            {
                return;
            }

            if (mPlayClickBusy)
            {
                return;
            }

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            int XSortbaseId = XSortBaseThemeResolver.ResolveCurrentLevelXSortBaseId();
            GameObject clickVfx = ThemeUiApplier.PairedClickVfx(XSortbaseId);
            ThemeUiApplier.PlayClickVfx(
                playButton != null ? playButton.transform : null,
                clickVfx);
            ThemeUiApplier.ApplyButtonThemeStyle(
                normalLevelObj != null ? normalLevelObj.transform : null,
                null);

            if (clickVfx == null)
            {
                OnPlayTriggered(m_currentLevel);
                return;
            }

            mPlayClickBusy = true;
            Tween.DelayedCall(ThemeUiApplier.ClickVfxHoldSeconds, () =>
            {
                mPlayClickBusy = false;
                OnPlayTriggered(m_currentLevel);
            }, unscaledTime: true);
        }

        private void DailyGiftButton()
        {
            UIGameFunc.OpenCircleAwardActivity();
        }

        private void DailyWinRewardTipButton()
        {
            if (!GameData.Activity.IsDailyVictoryActEnabled())
            {
                return;
            }

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            GameModule.UI.OpenPanel<UIDailyWinRewardDlg>();
        }

        void HideGoldActivityEntry()
        {
            var goldBtn = transform.Find("goldActivityBtn");
            if (goldBtn != null)
                goldBtn.gameObject.SetActive(false);
        }

        //deprecated
        private void OnLevelOnMapSelected(int levelId)
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            OnPlayTriggered(levelId);
        }

        private void OnPlayTriggered(int levelId)
        {
            GameDirector.LoadLevel(levelId);
        }

        private void OnIapStoreClosed(UIPage page, System.Type pageType)
        {
            
        }

        private void NoAdButton()
        {
            GameModule.UI.OpenPanel<UINoAdPanel>();

            //这里进入免广告购买逻辑
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }


        #endregion
    }


}
