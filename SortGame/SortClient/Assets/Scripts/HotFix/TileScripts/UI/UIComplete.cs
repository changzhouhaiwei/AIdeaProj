using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using Spine;
using TileGameCore;
using TileGamePlay;
using Spine.Unity;
using FishFramework;
using DG.Tweening;
using Unity.Mathematics;
using Core.Tools.MethodExtension;
using cfg.Game;

namespace FishTilePlay
{
    public class UIComplete : UIPage
    {
        [SerializeField] RectTransform safeAreaTransform;

        [Space]
        [SerializeField] UIFadeAnimation backgroundFade;

        [Header("Coins Label")]
        [SerializeField] UIScaleAnimation coinsPanelScalable;
        [SerializeField] CoinPanelView coinsPanelUI;
        [SerializeField] Button nextLevelButton;
        [SerializeField] PiggyBankBarView piggyBankBar;
        [SerializeField] Button adAwardButton;
        [SerializeField] TextMeshProUGUI nextLevelText;

        [SerializeField] TextMeshProUGUI passRateDescLb;

        // [SerializeField] private SkeletonGraphic spineSkel;
        // [SerializeField] private SkeletonGraphic spineLightSkel;

        [SerializeField] GameObject resultEff2;
        [SerializeField] Transform celebrateTopic;

        [SerializeField] UICompleteResultIntro resultIntro;

        [SerializeField] GameObject result02Panel;

        [SerializeField] private SkeletonGraphic resultTitleSpine;
        SkeletonDataAsset cachedDefaultTitleSpine;
        const string ThemeWinSlot = "themeWin";
        int mWinEffectThemeId = int.MinValue;

        [SerializeField] private GameObject touchCover;

        [SerializeField] private DifficultyShow difficultyShow;
        [SerializeField] TMP_Text difficultyText;
        [SerializeField] Image nextLevelButtonImage;

        //进度奖励
        [SerializeField] private UICompletePerNode completePerNode;

        //背景解锁进度
        [SerializeField] private UICompleteBgProgress completeBgProgress;

        //滚动奖励区域 
        [SerializeField] UICompleteAdAward completeAdAward;

        // box 奖励飞金币起点
        [SerializeField] Transform boxAwardPos;

        private int coinsHash = "Coins".GetHashCode();
        private int currentReward;
        private int mSettlementPresentationGeneration;
        private bool mNextLevelButtonClickEnabled = true;
        private Selectable.Transition mNextLevelButtonTransition;
        private bool mProgressFillStarted;
        private bool mResultTitleStarted;
        private bool mAdAwardRequestInFlight;
        private bool mAdAwardConsumed;
        private bool mTodayWinCntRecordedForSettlement;

        private const float Result01DisplayDuration = 2.0f;
        // 与 UICompleteShow.anim m_StopTime 对齐（仅停 Animator；填格由帧事件 PlayProgressFill 驱动）
        private const float Result02ShowAnimEndTime = 2.0833333f;
        // 与 vnodeNext.anim m_StopTime 对齐；播完后停 Animator，避免 Write Defaults 把 advButton 重新打开。
        private const float VNodeAnimationDuration = 0.6f;
        private string adKey = "LevelCompleteExtraAward";
        private const float ButtonAppearAnimDuration = 0.3f;
        private const string ResultTitleSpineAnimationName = "Title_In";

        // 出现动画终点 scale（Init 时从实例读取，只改 scale，不碰位置/size）
        private RectTransform mNextLevelButtonRect;
        private RectTransform mAdvButtonRect;
        private Vector3 mNextLevelPrefabScale = Vector3.one;
        private Vector3 mAdvButtonPrefabScale = Vector3.one;
        private Vector2 mNextLevelDefaultAnchoredPos;
        private Vector2 mAdvButtonAnchoredPos;
        // 与 vnodeNext1-3.anim 里 nextLevelButton 的 AnchoredPosition.y 对齐
        const float FirstThreeLevelsNextLevelPosY = 236.5f;
        const float NextLevelPosYWithPiggyBar = 13f;
        const float NextLevelPosYWithoutPiggyBar = 66f;
        private TrackEntry mResultTitleTrackEntry;
        private Action mResultTitleSpineCompleteAction;
        GameObject celebrateInst;
        GameObject celebrateSourcePrefab;
        const string ThemeCelebrateName = "themeCelebrate";

        const string PlayBtnGreenPath = "Assets/CABRes/SpriteAtlas/GPBtn/tile_btn_green.png";
        const string PlayBtnRedPath = "Assets/CABRes/SpriteAtlas/GPBtn/tile_btn_red.png";
        const string PlayBtnPurplePath = "Assets/CABRes/SpriteAtlas/GPBtn/tile_btn_purple.png";
        const string PlayBtnCoinPath = "Assets/CABRes/SpriteAtlas/GPBtn/tile_btn_coinreward.png";
        Sprite mCachedGreenBtnSprite;
        Sprite mCachedRedBtnSprite;
        Sprite mCachedPurpleBtnSprite;
        Sprite mCachedCoinBtnSprite;
        Sprite mCachedOriginalNextLevelSprite;
        RectTransform mPlayTextRect;
        float mPlayTextDefaultPosY;
        bool mCachedPlayTextPosY;

        public override void Init()
        {
            mNextLevelButtonTransition = nextLevelButton.transition;
            nextLevelButton.onClick.AddListener(NextLevelButton);
            adAwardButton.onClick.AddListener(AdAwardButtonFunc);
            CacheSettlementButtonPrefabScales();
            EnsureNextLevelDifficultyRefs();

            // UIComplete 有自己的分段金币动效，避免和通用加金币动效叠加
            coinsPanelUI.SetAutoGainAnim(false);
            coinsPanelUI.SetupPanel();
            NotchSaveArea.RegisterRectTransform(safeAreaTransform);
            HideAdvButtonClickVfx();
        }

        public void ApplyTheme()
        {
            HideAdvButtonClickVfx();
            ApplyWinEffect();
            ApplyWinCelebrateEffect();
            ApplyWinTitleEffect();
            var pack = ThemeUiApplier.Get(TileBaseThemeResolver.ResolveCurrentLevelTileBaseId());
            // 结算先套图/材质但不播放：Result02 延迟显示时若此时播，用户看不到溶解。
            ThemeUiApplier.ApplyButtonThemeStyle(
                nextLevelButton != null ? nextLevelButton.transform : null,
                pack?.button,
                pack?.buttonAppearMaterial,
                playAppear: false);
            completePerNode?.ApplyTheme();
        }

        void ApplyWinTitleEffect()
        {
            if (resultTitleSpine == null)
            {
                return;
            }

            if (cachedDefaultTitleSpine == null)
            {
                cachedDefaultTitleSpine = resultTitleSpine.skeletonDataAsset;
            }

            SkeletonDataAsset asset = ThemeUiApplier.Get()?.winTitle;
            if (asset == null)
            {
                asset = cachedDefaultTitleSpine;
            }

            ThemeUiApplier.ApplySkeleton(resultTitleSpine, asset);
        }

        void HideAdvButtonClickVfx()
        {
            if (adAwardButton == null)
            {
                return;
            }

            ThemeUiApplier.HideClickVfx(
                adAwardButton.transform,
                ThemeUiApplier.Get(TileBaseThemeResolver.ResolveCurrentLevelTileBaseId())?.buttonClickVfx);
        }

        void ApplyWinEffect()
        {
            if (resultEff2 == null)
            {
                return;
            }

            GameObject winPrefab = ThemeUiApplier.Get()?.winEffect;
            if (winPrefab == null)
                winPrefab = LoadWinEffectPrefabByCfg();

            Transform caidai = resultEff2.transform.Find("themeWinDefault");
            Transform themed = resultEff2.transform.Find(ThemeWinSlot);
            int themeId = LevelDirector.LoadedLevelTheme;

            if (winPrefab != null)
            {
                if (themed != null && mWinEffectThemeId != themeId && mWinEffectThemeId != int.MinValue)
                {
                    Destroy(themed.gameObject);
                    themed = null;
                }

                if (themed == null)
                {
                    GameObject inst = Instantiate(winPrefab, resultEff2.transform);
                    inst.name = ThemeWinSlot;
                    themed = inst.transform;
                }

                mWinEffectThemeId = themeId;
                if (themed != null)
                    themed.gameObject.SetActive(true);
                if (caidai != null)
                    caidai.gameObject.SetActive(false);
                return;
            }

            if (themed != null)
                Destroy(themed.gameObject);
            mWinEffectThemeId = int.MinValue;
            if (caidai != null)
                caidai.gameObject.SetActive(true);
        }

        static GameObject LoadWinEffectPrefabByCfg()
        {
            GameTileBaseControlCfg cfg = ConfigHelper.Tables?.TbGameTileBaseControlCfg?.GetOrDefault(LevelDirector.LoadedLevelTheme);
            string prefabName = cfg != null ? cfg.WinEffect : null;
            if (string.IsNullOrEmpty(prefabName))
                return null;

            return UILoadHelper.LoadAsset<GameObject>("Assets/CABRes/Prefabs/UIMain/Canvas/" + prefabName + ".prefab");
        }

        void ApplyWinCelebrateEffect()
        {
            Transform topic = EnsureCelebrateTopic();
            GameObject prefab = ThemeUiApplier.Get()?.winCelebrateEffect;
            if (prefab == null)
            {
                prefab = ThemeUiApplier.Get(ThemeUiApplier.DefaultTileBaseId)?.winCelebrateEffect;
            }

            if (prefab == null)
            {
                ClearCelebrateTopic(topic);
                celebrateInst = null;
                celebrateSourcePrefab = null;
                return;
            }

            if (celebrateInst != null && celebrateSourcePrefab == prefab)
            {
                celebrateInst.SetActive(false);
                celebrateInst.SetActive(true);
                return;
            }

            ClearCelebrateTopic(topic);
            celebrateInst = null;
            celebrateSourcePrefab = null;
            if (topic == null)
            {
                return;
            }

            GameObject inst = Instantiate(prefab, topic);
            inst.name = ThemeCelebrateName;
            if (inst.transform is RectTransform rt)
            {
                rt.localScale = Vector3.one;
            }

            inst.SetActive(true);
            celebrateInst = inst;
            celebrateSourcePrefab = prefab;
        }

        Transform EnsureCelebrateTopic()
        {
            if (celebrateTopic == null && resultEff2 != null)
            {
                celebrateTopic = resultEff2.transform.Find("Celebrate_Topic");
            }

            return celebrateTopic;
        }

        static void ClearCelebrateTopic(Transform topic)
        {
            if (topic == null)
            {
                return;
            }

            for (int i = topic.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(topic.GetChild(i).gameObject);
            }
        }

        void CacheSettlementButtonPrefabScales()
        {
            mNextLevelButtonRect = nextLevelButton != null ? nextLevelButton.transform as RectTransform : null;
            if (mNextLevelButtonRect != null)
            {
                mNextLevelPrefabScale = mNextLevelButtonRect.localScale;
                mNextLevelDefaultAnchoredPos = mNextLevelButtonRect.anchoredPosition;
            }

            mAdvButtonRect = adAwardButton != null ? adAwardButton.transform.parent as RectTransform : null;
            if (mAdvButtonRect != null)
            {
                mAdvButtonPrefabScale = mAdvButtonRect.localScale;
                mAdvButtonAnchoredPos = mAdvButtonRect.anchoredPosition;
            }
        }

        void EnsureNextLevelDifficultyRefs()
        {
            if (difficultyText == null && nextLevelButton != null)
            {
                var node = nextLevelButton.transform.Find("difficulyNode");
                if (node != null)
                    difficultyText = node.GetComponent<TMP_Text>();
            }

            if (nextLevelButtonImage == null && nextLevelButton != null)
                nextLevelButtonImage = nextLevelButton.GetComponent<Image>();

            if (mCachedOriginalNextLevelSprite == null && nextLevelButtonImage != null)
                mCachedOriginalNextLevelSprite = nextLevelButtonImage.sprite;

            if (mPlayTextRect == null && nextLevelText != null)
                mPlayTextRect = nextLevelText.rectTransform;

            if (!mCachedPlayTextPosY && mPlayTextRect != null)
            {
                mPlayTextDefaultPosY = mPlayTextRect.anchoredPosition.y;
                mCachedPlayTextPosY = true;
            }
        }

        void ApplyPlayTextPosY(bool center)
        {
            EnsureNextLevelDifficultyRefs();
            if (mPlayTextRect == null)
                return;

            var pos = mPlayTextRect.anchoredPosition;
            pos.y = center ? 13f : mPlayTextDefaultPosY;
            mPlayTextRect.anchoredPosition = pos;
        }

        void RefreshNextLevelDifficulty()
        {
            EnsureNextLevelDifficultyRefs();

            if (difficultyShow != null)
                difficultyShow.gameObject.SetActive(false);

            // 过关后 levelNum 已指向下一关；用存档进度算按钮文案，避免 LoadedLevelIndex+2 与提前加关不一致
            int nextLevelId = GameData.BFromStartScene && GPUserDataManager.Instance != null
                ? GPUserDataManager.Instance.GetShowLevelNum() + 1
                : (GameDirector.levelController != null
                    ? GameDirector.levelController.GetLoadedLevelIndex() + 2
                    : 0);

            bool isInfinite = GPUserDataManager.Instance != null
                && GPUserDataManager.Instance.GetBInfinitMode();
            if (isInfinite)
            {
                ApplyNextLevelDifficultyVisual(showDifficultyText: false, restoreOriginalSprite: true);
                RefreshNextLevelButtonLabel(isBonus: false);
                return;
            }

            int difficulty = 0;
            bool isCoinLevel = false;
            if (GameData.BFromStartScene && nextLevelId > 0)
            {
                var config = LevelDynamicConfigProvider.GetLevelConfig(nextLevelId);
                if (config != null)
                {
                    difficulty = config.Difficulty;
                    isCoinLevel = ActivityData.IsCoinRewardLevelEnabled() && config.Coinlevel == 1;
                }
            }

            bool showDifficultyText = difficulty == 1 || difficulty == 2;
            bool isBonus = GameData.BFromStartScene
                && nextLevelId > 0
                && AwardColorEggCoverConfig.IsEnabled(nextLevelId);
            ApplyNextLevelDifficultyVisual(showDifficultyText, restoreOriginalSprite: false, difficulty, isCoinLevel);
            RefreshNextLevelButtonLabel(isBonus);
        }

        void RefreshNextLevelButtonLabel(bool isBonus)
        {
            if (nextLevelText == null)
                return;

            // 默认 I2 词条 NextLevel（下一关）；奖励关才覆盖。两侧都走 SetText，避免只写奖励关后委托残留。
            if (isBonus)
            {
                LocalizedTMPRefresher.SetText(nextLevelText, () => AwardColorEggCoverText.BonusButton());
                return;
            }

            LocalizedTMPRefresher.SetText(nextLevelText, () => I2LocalizeMgr.GetString("NextLevel"));
        }

        void ApplyNextLevelDifficultyVisual(bool showDifficultyText, bool restoreOriginalSprite, int difficulty = 0, bool isCoinLevel = false)
        {
            ApplyPlayTextPosY(!showDifficultyText);

            PlayBtnDifficultyFont.Apply(nextLevelText, difficulty, restoreOriginalSprite, isCoinLevel);
            PlayBtnDifficultyFont.Apply(difficultyText, difficulty, restoreOriginalSprite, isCoinLevel);

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

            if (nextLevelButtonImage != null)
            {
                Sprite sprite;
                if (restoreOriginalSprite)
                    sprite = mCachedOriginalNextLevelSprite;
                else
                    sprite = LoadPlayButtonSprite(difficulty, isCoinLevel);

                if (sprite != null)
                    nextLevelButtonImage.sprite = sprite;
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

        void RefreshNextLevelButtonPosition()
        {
            if (mNextLevelButtonRect == null)
                return;

            // 第 8 关结算才出广告。在那之前和前三关一样停在上移位置，不给广告留空。
            if (!GameData.UnlockData.GetCompleteAdExtraAwardUnlock())
            {
                var firstPos = mNextLevelDefaultAnchoredPos;
                firstPos.y = FirstThreeLevelsNextLevelPosY;
                mNextLevelButtonRect.anchoredPosition = firstPos;
                return;
            }

            var pos = mNextLevelDefaultAnchoredPos;
            pos.y = piggyBankBar != null && piggyBankBar.gameObject.activeSelf
                ? NextLevelPosYWithPiggyBar
                : NextLevelPosYWithoutPiggyBar;
            mNextLevelButtonRect.anchoredPosition = pos;
        }

        void PlayButtonAppearAnim(RectTransform rect, Vector3 prefabScale, Action onComplete = null)
        {
            if (rect == null)
            {
                onComplete?.Invoke();
                return;
            }

            DOTween.Kill(rect, false);
            rect.localScale = Vector3.zero;
            rect.DOScale(prefabScale, ButtonAppearAnimDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => onComplete?.Invoke());
        }

        void EnsureResult02Panel()
        {
            if (result02Panel != null || safeAreaTransform == null)
            {
                return;
            }

            Transform panel = safeAreaTransform.Find("Result02Panel");
            if (panel != null)
            {
                result02Panel = panel.gameObject;
            }
        }

        private void SetSettlementContentVisible(bool visible)
        {
            EnsureResult02Panel();

            // 新结构：主结算收在 Result02Panel，与 Result01_Eff / Result02_Eff 分离
            if (result02Panel != null)
            {
                result02Panel.SetActive(visible);
                if (!visible)
                {
                    ResetResultTitleSpine();
                }

                return;
            }

            if (safeAreaTransform == null)
            {
                return;
            }

            Transform resultEff2Transform = resultEff2 != null ? resultEff2.transform : null;
            Transform resultIntroTransform = resultIntro != null ? resultIntro.transform : null;

            for (int i = 0; i < safeAreaTransform.childCount; i++)
            {
                Transform child = safeAreaTransform.GetChild(i);
                if (child == resultEff2Transform || child == resultIntroTransform)
                {
                    continue;
                }

                child.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                ResetResultTitleSpine();
            }
        }

        private void ClearSpineAnim()
        {
            resultIntro?.Clear();

            if (resultEff2 != null)
            {
                resultEff2.SetActive(false);
            }

            ResetResultTitleSpine();
        }

        private void PlayResultTitleSpine(Action onComplete = null)
        {
            if (resultTitleSpine == null)
            {
                onComplete?.Invoke();
                return;
            }

            ClearResultTitleTrackEntry();
            mResultTitleSpineCompleteAction = onComplete;
            resultTitleSpine.gameObject.SetActive(true);
            if (!resultTitleSpine.IsValid)
            {
                resultTitleSpine.Initialize(false);
            }

            if (!resultTitleSpine.IsValid)
            {
                InvokeResultTitleSpineComplete();
                return;
            }

            resultTitleSpine.AnimationState.ClearTrack(0);
            resultTitleSpine.Skeleton.SetToSetupPose();

            mResultTitleTrackEntry = resultTitleSpine.AnimationState.SetAnimation(
                0, ResultTitleSpineAnimationName, false);
            if (mResultTitleTrackEntry == null)
            {
                InvokeResultTitleSpineComplete();
                return;
            }

            mResultTitleTrackEntry.MixDuration = 0f;
            mResultTitleTrackEntry.TrackTime = 0f;
            mResultTitleTrackEntry.Complete += OnResultTitleSpineComplete;
            resultTitleSpine.Update(0f);
        }

        private void OnResultTitleSpineComplete(TrackEntry trackEntry)
        {
            if (trackEntry == null || trackEntry != mResultTitleTrackEntry)
            {
                return;
            }

            trackEntry.TrackTime = trackEntry.Animation != null
                ? trackEntry.Animation.Duration
                : trackEntry.TrackTime;
            trackEntry.TimeScale = 0f;
            resultTitleSpine?.Update(0f);
            InvokeResultTitleSpineComplete();
        }

        private void ResetResultTitleSpine()
        {
            mResultTitleSpineCompleteAction = null;
            ClearResultTitleTrackEntry();
            if (resultTitleSpine == null)
            {
                return;
            }

            if (resultTitleSpine.IsValid)
            {
                resultTitleSpine.AnimationState.ClearTrack(0);
                resultTitleSpine.Skeleton.SetToSetupPose();
                resultTitleSpine.Update(0f);
            }

            resultTitleSpine.gameObject.SetActive(false);
        }

        private void ClearResultTitleTrackEntry()
        {
            if (mResultTitleTrackEntry == null)
            {
                return;
            }

            mResultTitleTrackEntry.Complete -= OnResultTitleSpineComplete;
            mResultTitleTrackEntry = null;
        }

        private void InvokeResultTitleSpineComplete()
        {
            var onComplete = mResultTitleSpineCompleteAction;
            mResultTitleSpineCompleteAction = null;
            onComplete?.Invoke();
        }

        private void ShowSpineAnim()
        {
            ClearSpineAnim();

            int levelIndex = GPUserDataManager.Instance.GetLevelNum();
            int playCount = GPUserDataManager.Instance.GetLevelPlayCount(levelIndex - 1);
            LevelDirector.WinState = 1;
            if (playCount == 1 && LevelDirector.WinState == 1)
            {
                LevelDirector.WinState = 2;
            }

            if (resultIntro != null)
            {
                resultIntro.Play(() =>
                {
                    if (resultEff2 != null)
                    {
                        resultEff2.SetActive(true);
                    }

                    ShowPassRateDesc();
                });
                return;
            }

            DelayedCall(2.0f, () =>
            {
                if (resultEff2 != null)
                {
                    resultEff2.SetActive(true);
                }

                ShowPassRateDesc();
            });
        }

        private bool IsAdvButtonVisible()
        {
            if (mAdvButtonRect != null)
            {
                return mAdvButtonRect.gameObject.activeSelf;
            }

            if (adAwardButton == null)
            {
                return false;
            }

            Transform advButton = adAwardButton.transform.parent;
            return advButton != null && advButton.gameObject.activeSelf;
        }

        private void SetAdvButtonVisible(bool visible, Action onComplete = null)
        {
            if (adAwardButton == null && mAdvButtonRect == null)
            {
                RefreshNextLevelButtonPosition();
                onComplete?.Invoke();
                return;
            }

            if (visible)
            {
                if (mAdvButtonRect != null)
                {
                    DOTween.Kill(mAdvButtonRect, false);
                    mAdvButtonRect.gameObject.SetActive(true);
                    mAdvButtonRect.localScale = mAdvButtonPrefabScale;
                }
                else
                {
                    adAwardButton.SetActiveEx(true);
                    adAwardButton.SetParentActiveEx(true);
                }

                if (adAwardButton != null)
                {
                    adAwardButton.interactable = true;
                }

                RefreshNextLevelButtonPosition();
                onComplete?.Invoke();
                return;
            }

            // vnodeNext 的 Write Defaults 会覆盖脚本改的显隐，先停 Animator 再关节点
            completePerNode?.StopBtnNodeAnimator();

            // 直接关 advButton 根节点，避免 SetParentActiveEx 误伤层级
            if (mAdvButtonRect != null)
            {
                DOTween.Kill(mAdvButtonRect, false);
                mAdvButtonRect.gameObject.SetActive(false);
            }
            else if (adAwardButton != null)
            {
                adAwardButton.SetParentActiveEx(false);
            }

            // Result02Panel Animator 会驱动 nextLevel/adv 显隐；领奖隐藏 adv 时强制保住下一关按钮
            if (adAwardButton != null)
            {
                adAwardButton.interactable = false;
            }

            EnsureNextLevelButtonVisible();
            RefreshNextLevelButtonPosition();
            onComplete?.Invoke();
        }

        void EnsureNextLevelButtonVisible()
        {
            if (nextLevelButton == null)
            {
                return;
            }

            nextLevelButton.SetActiveEx(true);
            if (mNextLevelButtonRect != null)
            {
                DOTween.Kill(mNextLevelButtonRect, false);
                mNextLevelButtonRect.localScale = mNextLevelPrefabScale;
            }
        }

        /// <summary>
        /// 入场动画播完后关掉 Animator，避免之后代码改显隐被曲线盖掉。
        /// </summary>
        void StopResult02ShowAnimatorOverride()
        {
            EnsureResult02Panel();
            if (result02Panel == null)
            {
                return;
            }

            var anim = result02Panel.GetComponent<Animator>();
            if (anim != null)
            {
                anim.enabled = false;
            }
        }

        void EnableResult02ShowAnimator()
        {
            EnsureResult02Panel();
            if (result02Panel == null)
            {
                return;
            }

            var anim = result02Panel.GetComponent<Animator>();
            if (anim == null)
            {
                return;
            }

            anim.enabled = true;
            anim.Rebind();
            anim.Update(0f);
        }

        /// <summary>
        /// 供 Result02Panel 动画帧事件调用：从头播放 Title_In。
        /// </summary>
        public void PlayResultTitleIn()
        {
            if (mResultTitleStarted)
            {
                return;
            }

            mResultTitleStarted = true;
            PlayResultTitleSpine();
        }

        private void CheckShowAdAwardButton(Action onComplete = null)
        {
            if (ShouldShowAdAwardButton())
            {
                SetAdvButtonVisible(true, onComplete);
                GameBAData.ExcuteAdImpress(adKey);
                return;
            }

            SetAdvButtonVisible(false);
            onComplete?.Invoke();
        }

        bool ShouldShowAdAwardButton()
        {
            return GameData.BFromStartScene
                && GameData.UnlockData.GetCompleteAdExtraAwardUnlock()
                && !mAdAwardConsumed;
        }

        public override void PlayShowAnimation()
        {
            DockActor.HideSubmitImpactEffectsForSettlement();

            UIQuickVictory.HideAll();

            GPSDKMgr.Instance.HideADBanner();

            // 结算页面会被连续复用，先清除上一关 vNode 动画遗留的按钮状态。
            mAdAwardRequestInFlight = false;
            mAdAwardConsumed = false;
            mTodayWinCntRecordedForSettlement = false;
            SetAdvButtonVisible(false);

            gameObject.SetActive(false);
            gameObject.SetActive(true);
            ApplyTheme();

            // 先只展示 Result01_Eff；前四关结算立刻开 Result02Panel，第五关及以后等 2s。
            // 脑力关已经播过结算面板，这里不再等 2 秒开场。
            SetSettlementContentVisible(false);
            if (IsIqChallengeSettlement())
                resultIntro?.Clear();
            else
                ShowSpineAnim();
            AudioController.PlaySound(AudioController.AudioClips.winning_1);

            int presentationGeneration = ++mSettlementPresentationGeneration;
            float result02Delay = IsIqChallengeSettlement() || !ShouldDelayResult02Panel()
                ? 0f
                : Result01DisplayDuration;
            if (result02Delay <= 0f)
            {
                PresentSettlementMainDefault(presentationGeneration);
            }
            else
            {
                Utility.DGDelayedCall(result02Delay, () =>
                {
                    PresentSettlementMainDefault(presentationGeneration);
                }, gameObject);
            }
        }

        static bool IsIqChallengeSettlement()
        {
            return LevelDirector.IsIQChallengeLevelIndex(LevelDirector.LoadedLevelIndex);
        }

        /// <summary>
        /// 第五关及以后结算才延迟打开 Result02Panel（与过关后已 InceaseLevelNum 的 levelNum 对齐）。
        /// </summary>
        static bool ShouldDelayResult02Panel()
        {
            return UICompleteResultIntro.ShouldShow(GPUserDataManager.Instance.GetLevelNum());
        }

        /// <summary>
        /// Result01 展示结束后打开 Result02Panel，并走主结算初始化。
        /// </summary>
        void PresentSettlementMainDefault(int presentationGeneration)
        {
            if (presentationGeneration != mSettlementPresentationGeneration)
            {
                return;
            }

            ApplyPiggyBankSettlementDeferred();

            SetSettlementContentVisible(true);
            EnableResult02ShowAnimator();
            mProgressFillStarted = false;
            mResultTitleStarted = false;

            RefreshNextLevelDifficulty();

            coinsPanelScalable.Show(immediately: true);
            currentReward = LevelDirector.CurrentReward;
            completeBgProgress?.ResetForDelayedPlay();
            // processGroup 亮起前先定格到填充前进度（如 1 格），避免闪成上一局满格
            completePerNode?.PrepareProgressBarBeforeFill();

            SetNextLevelButtonInteractable(false);
            // vNode 在进度填完（满箱则开箱后再）由代码显示
            ScheduleResult02AfterAnimOnly(presentationGeneration);
        }

        /// <summary>
        /// 入场播完后停 Animator；填格只由 UICompleteShow 帧事件 PlayProgressFill 触发。
        /// </summary>
        void ScheduleResult02AfterAnimOnly(int presentationGeneration)
        {
            Utility.DGDelayedCall(Result02ShowAnimEndTime, () =>
            {
                if (presentationGeneration != mSettlementPresentationGeneration)
                {
                    return;
                }

                StopResult02ShowAnimatorOverride();

                if (!ShouldShowAdAwardButton())
                {
                    SetAdvButtonVisible(false);
                }
            }, gameObject);
        }

        void ShowVNodeAndUnlock(int presentationGeneration)
        {
            bool shouldShowAdButton = ShouldShowAdAwardButton();
            // 先统一恢复节点与交互状态，再由 vNode 动画控制二者的入场缩放。
            SetAdvButtonVisible(shouldShowAdButton);
            completePerNode?.ShowBtnNode(shouldShowAdButton);
            // 满箱会先播开箱再显按钮；溶解必须等 btnNode 真正亮起再播，否则 Progress 在隐藏阶段跑完。
            ThemeUiApplier.PlayButtonThemeAppear(
                nextLevelButton != null ? nextLevelButton.transform : null);

            if (shouldShowAdButton)
            {
                GameBAData.ExcuteAdImpress(adKey);
                CheckAdAwardShow();
            }

            // 入场结束后停 Animator，再播 Piggy 字号弹大，再评价弹窗 / 活动窗。
            Utility.DGDelayedCall(VNodeAnimationDuration + 1f / 60f, () =>
            {
                if (presentationGeneration != mSettlementPresentationGeneration)
                {
                    return;
                }

                completePerNode?.StopBtnNodeAnimator();
                if (!ShouldShowAdAwardButton())
                {
                    SetAdvButtonVisible(false);
                }

                EnsureNextLevelButtonVisible();
                // 等下一关按钮缩放结束后，再播新进度变色 + 0.8s 放大
                RevealPiggyBankSettlementProgress();
                RefreshNextLevelButtonPosition();

                UIGameFunc.CheckShowRateDlg(() =>
                {
                    if (presentationGeneration != mSettlementPresentationGeneration)
                    {
                        return;
                    }

                    TryAutoShowActivityStartPopups();
                });
            }, gameObject);
        }

        /// <summary>
        /// 供 Result02Panel 动画帧事件调用：processGroup 显示后播进度块填充。
        /// 必须延后出帧事件回调：填充会 DestroyImmediate 替换 Image，事件回调内禁止。
        /// </summary>
        public void PlayProgressFill()
        {
            if (mProgressFillStarted)
            {
                return;
            }

            mProgressFillStarted = true;
            Utility.DGDelayedCall(1f / 60f, () =>
            {
                completePerNode?.PlayPreparedProgressFill(OnProgressFillFinished);
            }, gameObject);
        }

        private void SetNextLevelButtonInteractable(bool interactable)
        {
            mNextLevelButtonClickEnabled = interactable;
            nextLevelButton.transition = interactable ? mNextLevelButtonTransition : Selectable.Transition.None;
            SetTouchEnable(interactable);
        }

        /// <summary>活动开启弹窗期间：下一关不可点但不置灰，且不盖 touchCover。</summary>
        void LockNextLevelForActivityPopup()
        {
            mNextLevelButtonClickEnabled = false;
            if (nextLevelButton != null)
                nextLevelButton.transition = mNextLevelButtonTransition;
            SetTouchEnable(true);
        }

        void UnlockNextLevelAfterActivity()
        {
            SetNextLevelButtonInteractable(true);
        }

        void OnProgressFillFinished()
        {
            completeBgProgress?.ResetForDelayedPlay();

            int presentationGeneration = mSettlementPresentationGeneration;

            // 满箱：立刻开箱 Spine，结束后再等 1s 显 vNode；未满：直接显 vNode
            if (completePerNode != null && completePerNode.GetBGetBoxAward())
            {
                completePerNode.PlayOpenBoxThen(() =>
                {
                    if (presentationGeneration != mSettlementPresentationGeneration)
                    {
                        return;
                    }

                    TryShowPiggyClaimThenUnlock(presentationGeneration);
                });
                return;
            }

            TryShowPiggyClaimThenUnlock(presentationGeneration);
        }

        void TryShowPiggyClaimThenUnlock(int presentationGeneration)
        {
            PiggyBankFlow.TryShowClaimBeforeButtons(() =>
            {
                if (presentationGeneration != mSettlementPresentationGeneration)
                    return;
                ShowVNodeAndUnlock(presentationGeneration);
            });
        }

        void ApplyPiggyBankSettlementDeferred()
        {
            piggyBankBar?.ApplySettlementDeferred();
        }

        void RevealPiggyBankSettlementProgress()
        {
            piggyBankBar?.RevealSettlementProgress();
        }

        void PlayBgProgressThenShowNext()
        {
            OnProgressFillFinished();
        }

        void ShowVNodeAndEnableNext()
        {
            OnProgressFillFinished();
        }

        bool TryClaimPendingBoxAwardThen(Action onDone)
        {
            if (completePerNode == null || !completePerNode.GetBGetBoxAward())
            {
                return false;
            }

            int awardId = completePerNode.AwardID;
            int awardNum = completePerNode.AwardNum;
            SetNextLevelButtonInteractable(false);

            if (awardId == 4)
            {
                completePerNode.HideClaimButton();
                completePerNode.ClearPendingBoxAward();
                ExcuteCommonAddCoin(onDone, awardNum, hideAdvButton: true);
            }
            else
            {
                completePerNode.GrantBoxAward();
                completePerNode.HideClaimButton();
                completePerNode.ClearPendingBoxAward();
                onDone?.Invoke();
            }

            return true;
        }

        private void ExcuteCommonAddCoin(Action afterFunc, int totalNumber = 20, bool hideAdvButton = true)
        {
            // Result02 内容已藏，仅保留金币栏；不再额外藏下一关（已在面板内被关掉）
            if (hideAdvButton)
            {
                SetAdvButtonVisible(false);
            }

            SetTouchEnable(false);

            DelayedCall(0.2f, () =>
            {
                TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.coinAdd);
            });

            int v1 = (int)(totalNumber * 0.25f);
            int v2 = (int)(totalNumber * 0.25f);
            int v3 = (int)(totalNumber * 0.25f);
            int v4 = totalNumber - v1 - v2 - v3;

            PlayRewardShake(0.35f, v1);
            PlayRewardShake(0.55f, v2);
            PlayRewardShake(0.75f, v3);
            PlayRewardShake(0.95f, v4);

            coinsPanelUI.PlayCoinFly(() =>
            {
                DelayedCall(0.15f, () =>
                {
                    afterFunc?.Invoke();
                });
            }, GetBoxAwardFlyStart());
        }

        Transform GetBoxAwardFlyStart()
        {
            if (boxAwardPos != null)
                return boxAwardPos;

            if (completePerNode != null)
                boxAwardPos = completePerNode.transform.Find("boxAwardPos");

            return boxAwardPos;
        }

        void PlayRewardShake(float delayTime, int award)
        {
            DelayedCall(delayTime, () =>
            {
                // 视觉抖动由到达特效 OnArrivePlay 统一触发；这里只负责加币
                if (GameData.BFromStartScene)
                {
                    GPUserDataManager.Instance.AddCoin(award);
                    CurrencyController.Add(CurrencyType.Coins, currentReward);
                }
                else
                {
                    CurrencyController.Add(CurrencyType.Coins, currentReward);
                }
            });
        }

        public override void PlayHideAnimation()
        {
            if (!isPageDisplayed)
                return;

            coinsPanelUI?.StopCoinFly();
            if (mNextLevelButtonRect != null)
                mNextLevelButtonRect.anchoredPosition = mNextLevelDefaultAnchoredPos;

            canvas.enabled = false;
            isPageDisplayed = false;
            UIController.OnPageClosed(this);
            ClearSpineAnim();
        }

        public void NextLevelButton()
        {
            if (!mNextLevelButtonClickEnabled)
            {
                return;
            }

            if (TileAppUpdate.TryShow("Settle.Next"))
            {
                return;
            }

            GameObject clickVfx = ThemeUiApplier.PairedClickVfx(
                TileBaseThemeResolver.ResolveCurrentLevelTileBaseId());
            ThemeUiApplier.PlayClickVfx(
                nextLevelButton != null ? nextLevelButton.transform : null,
                clickVfx);
            ThemeUiApplier.ApplyButtonThemeStyle(
                nextLevelButton != null ? nextLevelButton.transform : null,
                null);

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            AdsPlayStrategy.SetBTodayFirstChapin();//今日首次打关取消

            SetNextLevelButtonInteractable(false);

            if (clickVfx == null)
            {
                ContinueAfterNextLevelClick();
                return;
            }

            DelayedCall(ThemeUiApplier.ClickVfxHoldSeconds, ContinueAfterNextLevelClick);
        }

        void ContinueAfterNextLevelClick()
        {
            // 有宝箱奖励：藏 Result02（保留金币栏）→ 飞币/发奖 → 下一关；否则直接进下一关
            if (completePerNode != null && completePerNode.GetBGetBoxAward())
            {
                StopResult02ShowAnimatorOverride();
                HideResult02ContentKeepCoins();
                PlayBoxAwardThenEnterNextLevel();
                return;
            }

            ProceedToNextLevelAfterSettlement();
        }

        /// <summary>
        /// 隐藏 Result02Panel 下除金币栏外的内容，保证飞币 UI 仍可用。
        /// </summary>
        void HideResult02ContentKeepCoins()
        {
            EnsureResult02Panel();
            if (result02Panel == null)
            {
                return;
            }

            Transform coinsRoot = null;
            if (coinsPanelUI != null)
            {
                coinsRoot = coinsPanelUI.transform;
            }
            else if (coinsPanelScalable != null && coinsPanelScalable.Transform != null)
            {
                coinsRoot = coinsPanelScalable.Transform;
            }

            Transform keepChild = coinsRoot;
            while (keepChild != null && keepChild.parent != result02Panel.transform)
            {
                keepChild = keepChild.parent;
            }

            for (int i = 0; i < result02Panel.transform.childCount; i++)
            {
                Transform child = result02Panel.transform.GetChild(i);
                if (keepChild != null && child == keepChild)
                {
                    child.gameObject.SetActive(true);
                    continue;
                }

                child.gameObject.SetActive(false);
            }

            if (coinsPanelScalable != null)
            {
                coinsPanelScalable.Show(immediately: true);
            }
        }

        void PlayBoxAwardThenEnterNextLevel()
        {
            int awardId = completePerNode.AwardID;
            int awardNum = completePerNode.AwardNum;

            if (awardId == 4)
            {
                int coinAmount = awardNum > 0 ? awardNum : currentReward;
                completePerNode.HideClaimButton();
                completePerNode.ClearPendingBoxAward();
                ExcuteCommonAddCoin(ProceedToNextLevelAfterSettlement, coinAmount, hideAdvButton: false);
                return;
            }

            completePerNode.GrantBoxAward();
            completePerNode.HideClaimButton();
            completePerNode.ClearPendingBoxAward();
            ProceedToNextLevelAfterSettlement();
        }

        void ProceedToNextLevelAfterSettlement()
        {
            // 赛季结算领奖仍在点下一关时触发；开启窗已在按钮出现后自动弹出
            if (GameData.BFromStartScene && GameData.GoldTreeActData != null
                && GameData.GoldTreeActData.CheckCanShowAwardDlg())
            {
                SetNextLevelButtonInteractable(true);
                return;
            }

            HideCompleteAndLoadNextLevel();
        }

        void HideCompleteAndLoadNextLevel()
        {
            UIController.HidePage<UIComplete>(() => GameDirector.LoadNextLevel());
        }

        void TryAutoShowActivityStartPopups()
        {
            if (!GameData.BFromStartScene)
            {
                UnlockNextLevelAfterActivity();
                return;
            }

            RecordTodayWinCntOnce();

            // 排位优先：本关弹出排位开启窗则火山顺延到下一关（不 MarkPopupShown）
            if (TryOpenGoldTreeStartOnComplete())
            {
                LockNextLevelForActivityPopup();
                return;
            }

            if (PuppyAdventureFlow.TryShowPendingPopup(UnlockNextLevelAfterActivity))
            {
                LockNextLevelForActivityPopup();
                return;
            }

            UnlockNextLevelAfterActivity();
        }

        void RecordTodayWinCntOnce()
        {
            if (mTodayWinCntRecordedForSettlement)
                return;
            mTodayWinCntRecordedForSettlement = true;
            if (GameData.GoldTreeActData != null)
                GameData.GoldTreeActData.AddTodayWinCnt();
        }

        bool TryOpenGoldTreeStartOnComplete()
        {
            if (GameData.GoldTreeActData == null)
                return false;

            // 待赛季结算领奖时不弹开启窗（无副作用判断）
            if (GameData.GoldTreeActData.HasPendingSettlementAward())
                return false;

            if (GameData.GoldTreeActData.GetBNoInActivityTime())
                return false;

            if (GameData.GoldTreeActData.GetBActOpen())
                return false;

            // LoadedLevelIndex 从 0 计：12 = 刚通关第 13 关；AddTodayWinCnt 已执行
            const int goldTreeFirstOpenLoadedIndex = 12;
            int loadedLevelIndex = LevelDirector.LoadedLevelIndex;
            bool isFirstContinueToday = GameData.GoldTreeActData.GetTodayWinCnt() == 1;

            if (loadedLevelIndex > goldTreeFirstOpenLoadedIndex
                && !GameData.Activity.HadWinYesterdayBeforeTodayFirstWin()
                && isFirstContinueToday)
            {
                OpenGoldTreeStartOnComplete();
                return true;
            }

            if (loadedLevelIndex == goldTreeFirstOpenLoadedIndex
                || (loadedLevelIndex > goldTreeFirstOpenLoadedIndex
                    && GameData.GoldTreeActData.GetTodayWinCnt() >= 3))
            {
                OpenGoldTreeStartOnComplete();
                return true;
            }

            return false;
        }

        void OpenGoldTreeStartOnComplete()
        {
            var node = GameModule.UI.OpenPanel<UIGoldTreeStart>();
            node.CloseStartsActivityOnly = true;
            string startUniqueId = GameData.GoldTreeActData != null
                ? GameData.GoldTreeActData.GetActivityUniqueId(true)
                : "";
            GameBAData.ExcuteGameTriggerActivity("1", "rank", 0, startUniqueId);
            node.ClickAction = UnlockNextLevelAfterActivity;
        }


        int m_adAwardIdx = 0;
        int m_adAwardType = 4;
        int m_adAwardValue = 0;
        string m_adAwardId = "";

        public void CheckAdAwardShow(Action onCompleted = null)
        {
            if (!IsAdvButtonVisible())
            {
                onCompleted?.Invoke();
                return;
            }

            m_adAwardId = "";

            if (completeAdAward == null || !completeAdAward.EnsureAwardDataReady())
            {
                Debug.LogWarning("[UIComplete] Ad award data not ready, skip scroll reward.");
                GameBAData.ExcuteMainStageReward(0, "");
                onCompleted?.Invoke();
                return;
            }

            int rewardCount = completeAdAward.RewardCount;
            if (rewardCount <= 0)
            {
                GameBAData.ExcuteMainStageReward(0, "");
                onCompleted?.Invoke();
                return;
            }

            // 滚动期间禁止点广告，避免与滚动状态冲突；下一关按钮不受此阻塞
            adAwardButton.interactable = false;

            int idx = UnityEngine.Random.Range(0, rewardCount);
            var awardStr = completeAdAward.GetRewardString(idx);
            if (string.IsNullOrEmpty(awardStr))
            {
                adAwardButton.interactable = true;
                GameBAData.ExcuteMainStageReward(0, "");
                onCompleted?.Invoke();
                return;
            }

            var parts = awardStr.Split(',');
            m_adAwardType = parts[0].ToInt();
            m_adAwardValue = parts.Length > 1 ? parts[1].ToInt() : 0;
            m_adAwardIdx = idx;
            m_adAwardId = awardStr;
            GameBAData.ExcuteMainStageReward(0, awardStr);

            completeAdAward.PlayScrollAnim(idx, () =>
            {
                adAwardButton.interactable = true;
                onCompleted?.Invoke();
            });
        }


        public void AdAwardButtonFunc()
        {
            // 随机滚动动画还没播放结束时忽略点击，避免奖励流程和滚动动画冲突导致卡死
            if (mAdAwardConsumed || mAdAwardRequestInFlight || !IsAdvButtonVisible()
                || completeAdAward.IsAnimating)
            {
                return;
            }

            GameBAData.ExcuteMainStageReward(1, m_adAwardId);
            mAdAwardRequestInFlight = true;
            adAwardButton.interactable = false;

            if (!AdsPlayStrategy.CheckRewardAdsPlay(adKey))
            {
                mAdAwardRequestInFlight = false;
                adAwardButton.interactable = true;
                return;
            }

#if !UNITY_EDITOR
            UIController.ShowPage<UIAdsLoading>();
            GPSDKMgr.Instance.PlayRewardedAd(adKey, 3);
#else
            ExcuteAddAdAward();
#endif
        }

        public void ExcuteAddAdAward()
        {
            if (mAdAwardConsumed)
            {
                return;
            }

            mAdAwardConsumed = true;
            mAdAwardRequestInFlight = false;
            if (m_adAwardType == 4)
            {
                GameBAData.ExcuteMainStageReward(2, m_adAwardId);
                AddAdCoinAward(m_adAwardValue);
            }
            else
            {
                AddOtherAward();
            }
        }

        public void AddOtherAward()
        {
            SetAdvButtonVisible(false);
            SetNextLevelButtonInteractable(false);

            completeAdAward.PlayGetAwardAnim(m_adAwardIdx, () =>
            {
                EnsureNextLevelButtonVisible();
                SetNextLevelButtonInteractable(true);
            });
        }

        public void AddAdCoinAward(int coins = 50)
        {
            var startFrom = adAwardButton != null ? adAwardButton.transform : null;
            SetAdvButtonVisible(false);
            SetNextLevelButtonInteractable(false);

            DelayedCall(0.2f, () =>
            {
                TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.coinAdd);
            });

            int coin1 = (int)(coins * 0.2f);
            int coin2 = (int)(coins * 0.2f);
            int coin3 = (int)(coins * 0.2f);
            int coin4 = coins - coin1 - coin2 - coin3;

            PlayRewardShake(0.35f, coin1);
            PlayRewardShake(0.55f, coin2);
            PlayRewardShake(0.75f, coin3);
            PlayRewardShake(0.95f, coin4);

            coinsPanelUI.PlayCoinFly(() =>
            {
                EnsureNextLevelButtonVisible();
                SetNextLevelButtonInteractable(true);
            }, startFrom);
        }


        void Start()
        {
            GameEvent.AddEventListener((int)EventNameDef.UICompleteExtraAward, ExcuteAddAdAward);
        }

        void OnDestroy()
        {
            GameEvent.RemoveEventListener((int)EventNameDef.UICompleteExtraAward, ExcuteAddAdAward);
        }

        public void SetTouchEnable(bool b)
        {
            touchCover.SetActive(!b);
        }

        void DelayedCall(float delayTime, Action action)
        {
            var oneSeq = DOTween.Sequence();
            oneSeq.AppendInterval(delayTime)
            .AppendCallback(() =>
            {
                action?.Invoke();
            })
            .OnComplete(() => oneSeq.Kill()); // 自动释放

        }

        void ShowPassRateDesc()
        {
            if (!GameData.BFromStartScene || passRateDescLb == null)
            {
                return;
            }

            var levelIndex = GameDirector.CompletedLevelIndex();
            var cfg = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);

            var rateStr = cfg != null ? cfg.PassRate : "";

            if (rateStr != null && rateStr.Length > 0)
            {
                // rateStr是类似12%这样的配置. 转换成0.12这样.
                float passRate = 0f;
                if (rateStr.EndsWith("%"))
                {
                    string numStr = rateStr.Substring(0, rateStr.Length - 1);
                    if (float.TryParse(numStr, out float numValue))
                    {
                        passRate = numValue / 100f; // 12% -> 0.12
                    }
                }
                else if (float.TryParse(rateStr, out float numValue))
                {
                    // 如果没有%符号，假设已经是百分比数值
                    passRate = numValue / 100f;
                }

                //显示失败后的描述文本
                string passKey = "LevelFailDes1"; //des1是0.12
                float displayRate = passRate; // 使用转换后的值，如0.12

                if (UnityEngine.Random.Range(0, 2) == 0)
                {
                    passKey = "LevelFailDes2";//des2是1-0.12 = 0.88 这样的逻辑
                    displayRate = 1f - passRate; // 1 - 0.12 = 0.88
                }

                var formatStr = I2LocalizeMgr.GetString(passKey);

                // 将显示值转换为百分比字符串（如0.12 -> 12%，0.88 -> 88%）
                string rateDisplayStr = $"{(displayRate * 100f):F0}%";

                // 将通过率加上红色富文本颜色标识
                string coloredRate = $"<color=#FFE139>{rateDisplayStr}</color>";

                // 替换formatStr中的{0}为带颜色的通过率
                passRateDescLb.text = string.Format(formatStr, coloredRate);
            }
            else
            {
                // 如果没有通过率配置，显示默认文本或隐藏
                passRateDescLb.text = I2LocalizeMgr.GetString("LevelWinPanel_FirstWinDes0");
            }
        }

    }
}
