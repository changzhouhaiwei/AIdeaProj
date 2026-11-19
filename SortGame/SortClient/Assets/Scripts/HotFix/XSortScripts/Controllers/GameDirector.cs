using UnityEngine;
using XSortGameCore;
using System;
using Unity.Mathematics;
using FishFramework;
using EwingSoft.GameSDK;
using System.Linq;
using DG.Tweening;





#if UNITY_EDITOR
using UnityEditor;
using Versions = xasset.Versions;
#endif

namespace FishXSortPlay
{
    public class GameDirector : MonoBehaviour
    {
        private static GameDirector gameController;

        [DrawReference]
        [SerializeField] XSortGameModel data;

        [LineSpacer]
        [SerializeField] UIController uiController;
        // [SerializeField] MapActor mapBehavior;
        [SerializeField] MusicEmitter musicSource;
        [SerializeField] AudioClip menuMusicClip;
        [SerializeField] AudioClip levelMusicClip;

        [SerializeField] CaptureToSprite captureNode;

        public static LevelDirector levelController;

        /// <summary>结算正在展示的已通关关卡。没有 XSort 棋盘时用绳子当前关。</summary>
        public static int CompletedLevelIndex()
        {
            if (levelController != null)
                return levelController.GetLoadedLevelIndex();
            return RopeSort.RopeGameController.DisplayedLevelIndex;
        }
        private static ParticlesDirector particlesController;
        private static FloatingTextDirector floatingTextController;
        private static PUDirector powerUpController;
        public static XSortGameModel Data => gameController.data;

        private static bool isGameActive;
        public static bool IsGameActive => isGameActive;
        private static float lastOperateTime;//15s一次

        static int s_lastGeneratedLevelIndex = -1;
        static float s_lastGeneratedRealtime = -99999f;
        /// <summary>未操作就退出再进，冷却内沿用原布局，避免反复重刷。</summary>
        const float UnplayedLayoutRefreshMinInterval = 180f;

        /// <summary>成功触发任一类提示后临时前移操作时间，避免同一帧或短时间内连续弹出多种提示。</summary>
        private const float PlayHintThrottleSeconds = 100f;

        /// <summary>
        /// 本段空闲已经确认没有可提示棋子。棋盘只在 RecordLastPlayTime 时视为变化。
        /// ponytail: 不点击就改棋盘的路径（延迟揭开）要等到下次操作后的空闲才会再搜。
        /// </summary>
        static bool XSortHintMissThisIdle;
        // private static float lastOperateTime10;//10s一次

        private void Awake()
        {
            gameController = this;

            // Cache components
            CacheComponent(out particlesController);
            CacheComponent(out floatingTextController);
            CacheComponent(out levelController);
            CacheComponent(out powerUpController);

        }

        void StartInit()
        {
            CheckGameDataBufferClear();

            musicSource.Init();

            // Collect pages first, then hide baked canvases. Must run before any Init that can throw.
            uiController.Init();
            uiController.InitPages();

            try
            {
                if (particlesController != null)
                    particlesController.Init();
                if (floatingTextController != null)
                    floatingTextController.Init();
                if (powerUpController != null)
                    powerUpController.Init();
                if (levelController != null)
                    levelController.Init();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameDirector] StartInit failed after hiding page canvases: " + e);
            }

#if UNITY_EDITOR
            PlayBotController.Bind(this);
            LevelDifficultyPreRun.Bind(this);
            IQChallengePlayAudit.Bind(this);
#endif
        }

        void CheckGameDataBufferClear()
        {
            if (GameData.BFromStartScene)
            {
                var gameSettings = GameModule.Setting.GetGameSettings();
                if (!gameSettings.GetVersion().Equals(GPUserDataManager.Instance.GetLocalGameVersion()))
                {
                    RecoverLevelData.ClearCurrentLevelData();
                }

                GPUserDataManager.Instance.SaveLocalGameVersion();
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            // Before StartInit/LoadLevel: match StartScene boot for configs + xasset (XSortsNew sprites).
            if (AutoRunLevelInEditor || LevelDifficultyPreRun.HasPending || IQChallengePlayAudit.HasPending)
            {
                EnsureEditorTestLevelBoot();
            }
#endif

            StartInit();

#if UNITY_EDITOR
            if (LevelDifficultyPreRun.HasPending)
            {
                LevelDifficultyPreRun.TryStartPending();
            }
            else if (IQChallengePlayAudit.HasPending)
            {
                IQChallengePlayAudit.TryStartPending();
            }
            // Level Editor "Test level" sets AutoRunLevelInEditor — load selected level, skip Main Menu.
            else if (AutoRunLevelInEditor)
            {
                CheckIfNeedToAutoRunLevel();
            }
            else
#endif
            {
                int currentLevel = 0;
                if (GPUserDataManager.HasInit())
                    currentLevel = GPUserDataManager.Instance.GetFinalLevelNum();
                // 没有 XSort 棋盘时，直接打开绳子玩法并铺当前关。
                if (levelController == null)
                {
                    LoadLevel(currentLevel);
                }
                else if (currentLevel == 0 && !GPSDKMgr.Instance.IsUnderReviewIOS)
                {
                    LoadLevel(currentLevel);
                }
                else
                {
                    PlayMenuMusic();
                    UIController.ShowPage<UIMainMenu>();
                }
            }

            if (!GameData.BFromStartScene)
            {
                return;
            }

            // GPSDKMgr.Instance.BindShushuAndAppflyer();
            GPSDKMgr.Instance.QueryCurrencyInfo();
            GameLoading.MarkAsReadyToHide();
        }

        private void OnDestroy()
        {

        }

        public static void LoadLevel(int index, SimpleCallback onLevelLoaded = null, string dynamicStr = "", int baLoadState = 0, bool forceOpen = false)
        {
            if (levelController == null)
            {
                UIBottomNav.Hide();
                UIController.HidePage<UIMainMenu>();
                PlayLevelMusic();
                isGameActive = true;
                RopeSort.RopeGameController.Enter(index);
                onLevelLoaded?.Invoke();
                return;
            }

            if (LevelDifficultySim.SkipPresentation)
            {
                RecordLastPlayTime();
                GameBAData.ClearBeforeNewGame();
                RecoverLevelData.ClearCurrentLevelData();
                PlayLevelMusic();
                LevelChunkLoader.EnsureChunkSync(index);
                levelController.LoadLevel(index, onLevelLoaded, dynamicStr, baLoadState: baLoadState);
                isGameActive = true;
                return;
            }

            if (XSortAppUpdate.TryShow("LoadLevel"))
            {
                return;
            }

            NetDetectController.NotifyInLevel();
            RecordLastPlayTime();
            AdsPlayStrategy.BeginLevel();
            InterADABTestConfig.TryRecordLoadLevel();
            if (GPUserDataManager.HasInit())
                GPUserDataManager.Instance.TryCaptureSdkCountryCode("进关");
            // 归因无回调：每次进关前轮询 AttrSdk，再按分组/关卡决定是否 LoadFullAd
            GPSDKMgr.Instance?.TryLoadInterstitialIfNeeded();

            void DoLoadLevel()
            {
                LevelChunkLoader.EnsureChunk(index, () => DoLoadLevelAfterChunkReady());
            }

            void DoLoadLevelAfterChunkReady()
            {
                //埋点数据提前清理
                GameBAData.ClearBeforeNewGame();
                PlayLevelMusic();
                // gameController.mapBehavior.Hide();
                UIBottomNav.Hide();
                UIController.HidePage<UIMainMenu>();//(() =>
                                                    // {
                bool crossDayFirstEnter = !forceOpen && NextDayFirstEnterEase.TryConsumeForThisLoad();

                                                    //判定是否有数据
                var recoverData = RecoverLevelData.GetCurrentLevelData();
                if (!GameData.BFromStartScene)
                {
                    recoverData = null;
                }
                else if (recoverData != null && recoverData.LevelIndex == index && !RecoverLevelData.HasLayoutProgress())
                {
                    // 跨日首关不复用未玩布局，避免带入上次 win 档
                    if (!crossDayFirstEnter && ShouldReuseUnplayedLayout(index) && recoverData.IsValid() && !forceOpen)
                    {
                        RecoverLevelData.BeginRecoverSession(recoverData);
                        levelController.LoadLevelFromSave(index, onLevelLoaded, dynamicStr, baLoadState: baLoadState, recoverLevelData: recoverData);
                        UIController.ShowPage<UIGame>();
                        isGameActive = true;
                        return;
                    }

                    RecoverLevelData.ClearCurrentLevelData();
                    recoverData = null;
                }

                if (recoverData != null && recoverData.LevelIndex == index && recoverData.IsValid()
                    && RecoverLevelData.HasLayoutProgress() && !forceOpen)
                {
                    RecoverLevelData.BeginRecoverSession(recoverData);
                    levelController.LoadLevelFromSave(index, onLevelLoaded, dynamicStr, baLoadState: baLoadState, recoverLevelData: recoverData);
                    UIController.ShowPage<UIGame>();
                    isGameActive = true;
                }
                else
                {
                    CheckShowStory(index, () =>
                    {
                        Utility.DGDelayedCall(0.1f, () =>
                        {
                            levelController.LoadLevel(index, onLevelLoaded, dynamicStr, baLoadState: baLoadState);
                            UIController.ShowPage<UIGame>();
                            isGameActive = true;
                        });
                    });
                }
                // });
            }

            // 待机进关插屏：先 AdBreak/插屏，关播后再 LoadLevel，避免与加载叠内存
            if (AdsPlayStrategy.TryConsumeEnterLevelStandbyAd())
            {
                AdsPlayStrategy.PlayEnterLevelStandbyAdThen(DoLoadLevel);
                return;
            }

            DoLoadLevel();
        }

        public static void MarkLevelGenerated(int levelIndex)
        {
            s_lastGeneratedLevelIndex = levelIndex;
            s_lastGeneratedRealtime = Time.realtimeSinceStartup;
        }

        static bool ShouldReuseUnplayedLayout(int levelIndex)
        {
            // 颜色关没操作过就重进，要重新随机花色，不复用未玩布局。
            if (LevelDirector.IsColorChallengeLevel(levelIndex))
            {
                return false;
            }

            return levelIndex == s_lastGeneratedLevelIndex
                && Time.realtimeSinceStartup - s_lastGeneratedRealtime < UnplayedLayoutRefreshMinInterval;
        }

        private static void CheckShowStory(int levelIndex, Action act = null)
        {
            if (GameData.BFromStartScene)
            {
                if (HasBgChange(levelIndex) && !GPUserDataManager.Instance.GetBInfinitMode())
                {
                    var list = ConfigHelper.Tables.TbGameBgShow.DataList;
                    var data = list.FirstOrDefault(x => x.StartLevel == (levelIndex + 1));
                    int bgIndex = data != null ? list.IndexOf(data) : -1;

                    Action continueAct = () => PlaySceneBgThenLoad(levelIndex, act);

                    // 前三段背景切换跳过 UIStoryBgPassDlg（含 8→9），仍播背景过渡
                    if (bgIndex >= 0 && bgIndex < 4)
                    {
                        continueAct();
                        return;
                    }

                    var node = GameModule.UI.OpenPanel<UIStoryBgPassDlg>();
                    node.RefreshUI(data);
                    node.EndAction = continueAct;
                    return;
                }

                if (LevelDirector.NeedsBackgroundTransition(levelIndex))
                {
                    PlaySceneBgThenLoad(levelIndex, act);
                    return;
                }
            }
            act?.Invoke();//调用
        }

        static void PlaySceneBgThenLoad(int levelIndex, Action act)
        {
            LevelDirector.PlayBackgroundTransition(levelIndex);
            float loadDelay = LevelDirector.GetLevelXSortsLoadDelay();
            if (loadDelay > 0f)
            {
                Utility.DGDelayedCall(loadDelay, () => act?.Invoke());
            }
            else
            {
                act?.Invoke();
            }
        }


        //是否有背景变化.
        private static bool HasBgChange(int levelIndex)
        {
            int saveIndex = PlayerPrefs.GetInt("BgShowIndex", -1);
            if (saveIndex == levelIndex || levelIndex == 0)
            {
                return false;
            }

            var list = ConfigHelper.Tables.TbGameBgShow.DataList;
            foreach (var item in list)
            {
                if (item.StartLevel == levelIndex + 1)
                {
                    PlayerPrefs.SetInt("BgShowIndex", levelIndex);
                    return true;
                }
            }
            return false;
        }

        public static void OnLevelCompleted()
        {
            if (!isGameActive)
                return;

            if (LevelDifficultySim.IsRunning)
            {
                isGameActive = false;
                RaycastController.Disable();
                LevelDifficultySim.NotifyFinished(true);
                return;
            }

            GameEvent.Send((int)EventNameDef.EncouragePlayEffectsHide);
            DockActor.HideSubmitImpactEffectsForSettlement();

            // 立即禁用射线检测，限制操作
            RaycastController.Disable();
            GPSDKMgr.Instance.HideADBanner();

            bool saveCheck = false;
            if (GPUserDataManager.HasInit())
                GPUserDataManager.Instance.TryCaptureSdkCountryCode("过关");
            // F3/GM 过关与正式通关共用 OnMatchCompleted(true)。挑战进行中一律计胜（RecordLevelWin 内部再判 IsRunning）。商店跳关不走这条。
            PuppyAdventureFlow.RecordLevelWinAtCompletion();
            if (GameData.BFromStartScene)
                PiggyBankState.RecordWin();

            DelayCall(1.0f, () =>
            {
                if (!saveCheck)
                {
                    isGameActive = false;
                    SaveController.Save();
                    saveCheck = true;

                    //连胜记录
                    if (GameData.BFromStartScene)
                    {
                        GameBAData.ExcuteFinishUniqueLevelFirebase();
                        GameBAData.ExcuteFinishTotalWinsAppFlyer();
                        if (levelController != null)
                        {
                            MilestoneSdk.LevelPass(levelController.GetLoadedLevelIndex() + 1);
                        }

                        if (AwardColorEggCoverPending.ShouldSkipMainStage)
                            Debug.Log($"[彩蛋胜利] 结算跳过 main_stage status=1 level={LevelDirector.LoadedLevelIndex}");
                        else
                            GameBAData.ExcuteMainStage(1);//连胜连败次数位置
                        GPUserDataManager.Instance.IncrementWinCount();
                        AwardColorEggCoverPending.MarkSessionWinRecorded();
                        GameData.Activity.RecordDailyVictoryWin();
                    }

                    RecoverLevelData.ClearCurrentLevelData();
                    CheckShowLevelComplete();
                }
            }
            );
        }

        static void DelayCall(float time, Action action)
        {
            var seq = DG.Tweening.DOTween.Sequence();
            seq.AppendInterval(1.0f);
            seq.AppendCallback(() =>
            {
                action?.Invoke();
            });
            seq.OnComplete(() => seq.Kill());
        }

        private static void CheckShowLevelComplete()
        {
            TryShowCoinCollectThen(ContinueAfterCoinCollect);
        }

        private static void ContinueAfterCoinCollect()
        {
            LevelDirector.ShowOrHideLevelNode(false);

            if (GameData.BFromStartScene
                && PuppyAdventureFlow.TryShowLevelWin(ContinueAfterPuppyWin))
            {
                return;
            }

            ContinueAfterPuppyWin();
        }

        private static void ContinueAfterPuppyWin()
        {
            if (GameData.BFromStartScene && GameData.Activity.ConsumePendingSettlementDailyWinDlg())
            {
                UIGameFunc.OpenDailyWinRewardOnFirstVictory(ProceedNormalSettlement);
                return;
            }

            ProceedNormalSettlement();
        }

        private static void ProceedNormalSettlement()
        {
            ShowCompleteUI();
        }

        private static void TryShowCoinCollectThen(Action next)
        {
            if (LevelDirector.BInCoinRewardLevel && LevelDirector.CoinRewardXSortTotalCount > 0)
            {
                var gameUI = UIController.GetPage<UIGame>();
                if (gameUI != null)
                {
                    gameUI.PlayCoinCollectThen(next);
                    return;
                }
            }

            next?.Invoke();
        }

        private static void ShowCompleteUI()
        {
            UIController.HidePage<UIGame>(() =>
            {
                bool retb = AdsPlayStrategy.TryPlayOnWin();
                float tm = 1 / 33f;
                if (retb)
                {
                    tm = 0.5f;
                }

                Utility.DGDelayedCall(1 / 33f, () =>
                {
                    UIController.ShowSettlementPage<UIComplete>();
                });
            });
        }

        public static void OnLevelFailed()
        {
            if (!isGameActive)
                return;

            if (LevelDifficultySim.IsRunning)
            {
                isGameActive = false;
                RaycastController.Disable();
                LevelDifficultySim.NotifyFinished(false);
                return;
            }

            if (GPUserDataManager.HasInit())
                GPUserDataManager.Instance.TryCaptureSdkCountryCode("失败出关");

            isGameActive = false;

            GameEvent.Send((int)EventNameDef.EncouragePlayEffectsHide);
            DockActor.HideSubmitImpactEffectsForSettlement();
            DockActor.NormalizeOccupiedXSortSortingForSettlement();
            LevelDirector.Dock?.StopAllAnimations();
            UIQuickVictory.HideAll();

            RaycastController.ResetLongPress();
            RaycastController.Disable();

            SaveController.Save();

            RecoverLevelData.ClearCurrentLevelData();

            if (GameData.BFromStartScene)
                PiggyBankState.RecordFail();

            XSortBaseStyleResolver.MarkCurrentLevelFailed();

            UIController.HidePage<UIGame>(() =>
            {
                UIController.ShowPage<UIGameOver>();
            });
        }

        public static void LoadNextLevel(SimpleCallback onLevelLoaded = null)
        {
            if (levelController == null)
            {
                int index = RopeSort.RopeGameController.DisplayedLevelIndex;
                if (GameData.BFromStartScene && GPUserDataManager.HasInit())
                    index = Mathf.Min(GPUserDataManager.Instance.GetLevelNum(), RopeSort.RopeLevelCatalog.Last);
                LoadLevel(index, onLevelLoaded);
                return;
            }

            if (GameData.BFromStartScene)
            {
                int levelNum = GPUserDataManager.Instance.GetFinalLevelNum();
                LoadLevel(levelNum, onLevelLoaded);
            }
            else
            {
                LoadLevel(LevelDirector.DisplayedLevelIndex, onLevelLoaded);
            }
        }

        public static void ReplayLevel(int baLoadState = 0)
        {
            if (levelController == null)
            {
                isGameActive = false;
                LoadLevel(RopeSort.RopeGameController.DisplayedLevelIndex, baLoadState: baLoadState);
                return;
            }

            isGameActive = false;
            if (GPUserDataManager.HasInit())
                GPUserDataManager.Instance.TryCaptureSdkCountryCode("重开出关");
            GameEvent.Send((int)EventNameDef.EncouragePlayEffectsHide);
            DockActor.HideSubmitImpactEffectsForSettlement();
            SaveController.Save();
            UIController.ShowPage<UIMainMenu>();
            LoadLevel(LevelDirector.DisplayedLevelIndex, baLoadState: baLoadState);
        }

        public static void ReturnToMenu()
        {
            if (levelController == null)
            {
                isGameActive = false;
                PlayMenuMusic();
                UIController.HidePage<RopeSort.RopeGamePage>(() => UIController.ShowPage<UIMainMenu>());
                return;
            }

            if (GPUserDataManager.HasInit())
                GPUserDataManager.Instance.TryCaptureSdkCountryCode("回主页");

            if (isGameActive)
            {
                RecoverLevelData.SaveCurrentLevelData();
            }

            isGameActive = false;
            GameEvent.Send((int)EventNameDef.EncouragePlayEffectsHide);
            DockActor.HideSubmitImpactEffectsForSettlement();

            LevelDirector.UnloadLevel();
            LevelDirector.ShowOrHideLevelNode(false);
            PlayMenuMusic();
            UIController.ShowPage<UIMainMenu>();
            RecoverLevelData.Flush();
            SaveController.Save();
        }

        static void PlayMenuMusic()
        {
            if (gameController == null || gameController.musicSource == null)
                return;
            gameController.musicSource.PlayClip(gameController.menuMusicClip, 1.3f * 1.3f);
        }

        static void PlayLevelMusic()
        {
            if (gameController == null || gameController.musicSource == null)
                return;
            gameController.musicSource.PlayClip(gameController.levelMusicClip, 1.3f);
        }

        public static void Revive()
        {
            // 广告 PauseGame 后 timeScale 可能仍为 0；飞回用 unscaled tween，这里再解一次冻。
            if (GameModule.Setting != null)
                GameModule.Setting.ResumeGame();
            XSortBaseStyleResolver.CancelCurrentLevelReroll();
            LevelDirector.Revive(() => { isGameActive = true; });
        }

        //记录上次的时间
        public static void RecordLastPlayTime(bool bforce = false)
        {
            lastOperateTime = Time.timeSinceLevelLoad;
            XSortHintMissThisIdle = false;

            if (isGameActive || bforce)
            {
                PUDirector.PowerUpsUIController.HideAllOperEffect();
                UIGame gameUI = UIController.GetPage<UIGame>();
                gameUI.GetXSortHintNode().HideEff(); //(true);
                //隐藏所有的提示。
            }
        }

        public static float GetIdleOperateSeconds()
        {
            return Mathf.Max(0f, Time.timeSinceLevelLoad - lastOperateTime);
        }

        public static float GetLastOperateTimeSinceLevelLoad()
        {
            return lastOperateTime;
        }

        public static void CheckShowPlayHint()
        {
            if (!isGameActive)
                return;

            float idle = Time.timeSinceLevelLoad - lastOperateTime;
            GetPlayHintIntervals(out float XSortInterval, out float itemInterval);

            int levelIndex = GPUserDataManager.Instance.GetLevelNum();
            bool hintsUnlocked = levelIndex > 2;

            // 1) 瓦片可走提示（早触发、成功则本帧不再做后续提示）
            // 失败只搜一次：局面没变时 GetHintXSorts 结果相同，再搜只会每帧分配。
            if (hintsUnlocked && idle > XSortInterval && !XSortHintMissThisIdle)
            {
                var gameUI = UIController.GetPage<UIGame>();
                if (gameUI.GetXSortHintNode().ShowEff())
                {
                    BumpOperateTimeToThrottleHints();
                    return;
                }

                XSortHintMissThisIdle = true;
            }

            // 2) 道具引导 + 关卡后期暂存席紧张时的广告位动效（共用「更长」无操作时间）
            if (!hintsUnlocked || idle <= itemInterval)
                return;

            if (PUDirector.PowerUpsUIController.CheckShowEffect())
            {
                BumpOperateTimeToThrottleHints();
                return;
            }

            // 第8关起：暂存格余量≤3 且长时间无操作时展示侧边广告相关动效（一关内不限制次数）
            if (levelIndex > 7 && LevelDirector.DockAb.GetRestEmptyCnt() <= 3)
            {
                BumpOperateTimeToThrottleHints();
                UIController.GetPage<UIGame>().ShowOrHideAdEffect(true);
                return;
            }
        }

        static void GetPlayHintIntervals(out float XSortSeconds, out float itemSeconds)
        {
            XSortSeconds = 10f;
            itemSeconds = 15f;

            if (!GameData.BFromStartScene)
            {
                XSortSeconds *= 0.5f;
                itemSeconds *= 0.5f;
                return;
            }

            if (GPUserDataManager.Instance.UserData.continusWinCnt >= 2)
                XSortSeconds *= 2f;
        }

        static void BumpOperateTimeToThrottleHints()
        {
            lastOperateTime = Time.timeSinceLevelLoad + PlayHintThrottleSeconds;
        }

        #region Extensions
        public bool CacheComponent<T>(out T component) where T : Component
        {
            Component unboxedComponent = gameObject.GetComponent(typeof(T));

            if (unboxedComponent != null)
            {
                component = (T)unboxedComponent;

                return true;
            }

            Debug.LogWarning(string.Format("Scripts Holder doesn't have {0} script added to it", typeof(T)));

            component = null;

            return false;
        }
        #endregion

        #region Dev

#if UNITY_EDITOR

        private static readonly string AUTO_RUN_LEVEL_SAVE_NAME = "auto run level editor";

        public static bool AutoRunLevelInEditor
        {
            get { return EditorPrefs.GetBool(AUTO_RUN_LEVEL_SAVE_NAME, false); }
            set { EditorPrefs.SetBool(AUTO_RUN_LEVEL_SAVE_NAME, value); }
        }

        /// <summary>
        /// Minimal StartScene-equivalent boot so Test Level gets configs + XSortsNew assets like normal play.
        /// Does not set BFromStartScene (avoids SDK/UIModule main-city paths).
        /// </summary>
        private static void EnsureEditorTestLevelBoot()
        {
            try
            {
                if (!Versions.Initialized)
                {
                    Versions.InitializeAsync();
                }

                ConfigHelper.EnsureInitialized();

                if (GameData.Activity == null)
                {
                    GameData.RegisterData();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TestLevel] boot failed: {e.Message}");
            }
        }

        private void CheckIfNeedToAutoRunLevel()
        {
            if (AutoRunLevelInEditor)
                LoadLevel(LevelDirector.DisplayedLevelIndex);

            AutoRunLevelInEditor = false;
        }
#endif


        #endregion


        void Update()
        {
            GameDirector.CheckShowPlayHint();
            // 绳子关没有棋盘射线，XSort2 的 F1 挂在 RaycastController 上，这里进不到。
            if (levelController == null)
                RopeSort.RopeGmPanel.Tick();
        }

        public CaptureToSprite GetCaptureToSprite()
        {
            return captureNode;
        }

        private static string guideKey = "Guide5Hint";

        public static bool GetHasShowGuide()//超过10关算作已经出现过
        {
            var num = GPUserDataManager.Instance.GetLevelNum();
            return PlayerPrefs.GetInt(guideKey, 0) == 1 || num >= 10;
        }

        public static void SetHasShowGuide()
        {
            PlayerPrefs.SetInt(guideKey, 1);
            // PlayerPrefs.Save();
        }

    }
}