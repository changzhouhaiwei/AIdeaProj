using UnityEngine;
using UnityEngine.UI;
using XSortGameCore;
using TMPro;
using FishFramework;
using Spine.Unity;
using Tween = XSortGameCore.Tween;


namespace FishXSortPlay
{
    public class UIGameOver : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;

        [SerializeField] UIScaleAnimation levelFailed;
        [SerializeField] UIFadeAnimation backgroundFade;

        [SerializeField] Button replayButton;
        [SerializeField] Button reviveButton;

        [SerializeField] TextMeshProUGUI timeCountLb;
        [SerializeField] SkeletonGraphic miaobiaoSpine;
        [SerializeField] SkeletonGraphic miaobiaoIceSpine;
        [SerializeField] Transform bgEffect;
        SkeletonDataAsset cachedDefaultClockSpine;
        ThemeFailEffect failEffect;

        //显示通过率
        [SerializeField] TextMeshProUGUI passRateDescLb;

        [SerializeField] GameObject freeImage;
        [SerializeField] TextMeshProUGUI freeReviveCountLb;
        [SerializeField] GameObject adImage;

        bool stageOverOrderChecked;

        private string reviveAdKey = "Revive";
        private bool reviveLocked;
        private bool inputReady;

        // private TweenClip continuePingPongCase;
        private TweenClip countdownEndCase;
        private TweenClip[] countdownTickCases;
        private TweenClip blankAreaClickEnableCase;
        private const float BlankAreaClickDelay = 0.35f;
        const string ThemeFailName = "themeFail";
        private const string MiaobiaoAnimationName = "Begin";
        private const int CountdownStartSecond = 5;
        private const float CountdownStepInterval = 1f;
        /// <summary>高于飞入/长按棋子 Sprite（order*3 可达数千），避免盖住失败页。</summary>
        private const int CanvasSortingOrder = 20000;
        private readonly float countdownTime = (CountdownStartSecond + 1) * CountdownStepInterval;

        public void ApplyTheme()
        {
            XSortBaseThemePack pack = ThemeUiApplier.Get();
            ApplyFailEffect(pack != null ? pack.failEffect : null);
            ThemeUiApplier.PlaySound(pack?.failSound);

            if (miaobiaoSpine != null && cachedDefaultClockSpine == null)
            {
                cachedDefaultClockSpine = miaobiaoSpine.skeletonDataAsset;
            }

            SkeletonDataAsset clockAsset = pack != null ? pack.failClock : null;
            if (clockAsset == null)
            {
                clockAsset = cachedDefaultClockSpine;
            }

            ThemeUiApplier.ApplySkeleton(miaobiaoSpine, clockAsset);
        }

        void ApplyFailEffect(GameObject prefab)
        {
            Transform root = EnsureBgEffectRoot();
            if (prefab == null)
            {
                ClearSpawnedFailEffect(root);
                return;
            }

            failEffect = root != null ? root.GetComponentInChildren<ThemeFailEffect>(true) : null;
            if (failEffect != null && failEffect.SourcePrefab == prefab)
            {
                failEffect.gameObject.SetActive(true);
                return;
            }

            // 木/泡泡预制体没有 ThemeFailEffect，必须按节点清掉，不能只关组件。
            ClearSpawnedFailEffect(root);
            if (root == null)
            {
                return;
            }

            GameObject inst = Instantiate(prefab, root);
            inst.name = ThemeFailName;
            if (inst.transform is RectTransform rt)
            {
                rt.localScale = Vector3.one;
            }

            failEffect = inst.GetComponent<ThemeFailEffect>();
            if (failEffect != null)
            {
                failEffect.BindSourcePrefab(prefab);
            }

            inst.SetActive(true);
        }

        Transform EnsureBgEffectRoot()
        {
            if (bgEffect == null)
            {
                bgEffect = transform.Find("BG_Effect");
            }

            return bgEffect;
        }

        void ClearSpawnedFailEffect(Transform root)
        {
            if (failEffect != null)
            {
                failEffect.Stop();
                failEffect = null;
            }

            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        public override void Init()
        {
            // 按钮点击由 Prefab 上 NoThanksButton / ContinueButton 绑定；此处不再 AddListener，避免与 Prefab 重复触发
            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
        }

        /// <summary>Prefab：全屏透明 Replay 区域</summary>
        public void NoThanksButton() => ReplayButton();

        /// <summary>Prefab：复活按钮</summary>
        public void ContinueButton() => ReviveButton();

        #region Show/Hide

        public override void PlayShowAnimation()
        {
            RefreshStageOverTips();

            DockActor.HideSubmitImpactEffectsForSettlement();

            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = CanvasSortingOrder;
            }

            StopCountdownAnimation();
            CancelBlankAreaClickEnableDelay();
            ApplyTheme();
            PrepareMiaobiaoAnimation();
            failEffect?.ResetProgress();
            reviveLocked = false;
            inputReady = false;
            if (reviveButton != null)
                reviveButton.interactable = true;
            if (replayButton != null)
                replayButton.interactable = false;

            if (GameData.BFromStartScene)
            {
                LockFreeReviveCapIfNeeded();
                if (GPUserDataManager.Instance.GetFreeReviveRemainCount() <= 0)
                    GameBAData.ExcuteAdImpress(reviveAdKey);// 广告显示的埋点
                GameBAData.ExcuteStageEvent(9);
            }


            GPSDKMgr.Instance.HideADBanner();

            levelFailed.Hide(immediately: true);

            float fadeDuration = 0.3f;
            backgroundFade.Show(fadeDuration);

            timeCountLb.text = "5";

            ShowPassRateDesc();
            RefreshFreeReviveUI();

            blankAreaClickEnableCase = Tween.DelayedCall(BlankAreaClickDelay, EnableFailPageInput, unscaledTime: true);

            Tween.DelayedCall(fadeDuration * 0.8f, delegate
            {
                levelFailed.Show();

                // 开始倒计时：数字步进 + miaobiao Spine 表盘动画
                StartCountdownAnimation();
                UIController.OnPageOpened(this);
            });

        }

        void ShowPassRateDesc()
        {
            if(!GameData.BFromStartScene)
            {
                return;
            }
            
            if (GameDirector.levelController == null)
                return;

            var levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
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
                string coloredRate = $"<color=#FF72C8>{rateDisplayStr}</color>";
                
                // 替换formatStr中的{0}为带颜色的通过率
                passRateDescLb.text = string.Format(formatStr, coloredRate);
            }
            else
            {
                // 如果没有通过率配置，显示默认文本或隐藏
                passRateDescLb.text = "";
            }
        }

        public override void PlayHideAnimation()
        {
            ThemeUiApplier.StopSound(ThemeUiApplier.Get()?.failSound);
            backgroundFade.Hide(immediately: true);

            // 停止倒计时动画
            StopCountdownAnimation();
            ClearSpawnedFailEffect(EnsureBgEffectRoot());
            CancelBlankAreaClickEnableDelay();
            if (replayButton != null)
                replayButton.interactable = true;
            UIController.OnPageClosed(this);
        }

        #endregion

        #region Buttons 

        private void ReviveButton()
        {
            if (!inputReady || reviveLocked)
                return;

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            if (GPUserDataManager.Instance.TryConsumeFreeRevive())
            {
                LockRevive();
                StopCountdownAnimation();
                ReviveCallback(true, 3); // 免费复活
                return;
            }

#if UNITY_EDITOR
            LockRevive();
            StopCountdownAnimation(); // 停止倒计时
            ReviveCallback(true, 4); // 编辑器按广告复活
#else
            if (GameData.BFromStartScene && PlatformMgr.GetUseSDK())
            {
                GameBAData.ExcuteAdClick(reviveAdKey);
                if (!AdsPlayStrategy.CheckRewardAdsPlay(reviveAdKey))
                {
                    return;
                }

                LockRevive();
                StopCountdownAnimation();
                UIController.ShowPage<UIAdsLoading>();
                GPSDKMgr.Instance.PlayRewardedAd(reviveAdKey, 1);
            }
            else
            {
                LockRevive();
                StopCountdownAnimation(); // 停止倒计时
                ReviveCallback(true, 4);
            }
#endif
        }

        void LockRevive()
        {
            reviveLocked = true;
        }

        public void UnlockReviveAfterAdFail()
        {
            if (!isPageDisplayed)
                return;
            reviveLocked = false;
        }

        private void ReviveCallback(bool watchedRV, int stageStatus)
        {
            if (!watchedRV) return;

            GameBAData.ExcuteStageEvent(stageStatus);
            GameBAData.ExcuteMainStage(6);
            GameDirector.Revive();
            UIController.HidePage<UIGameOver>();
            UIController.ShowPage<UIGame>();
            DockActor.ResetSubmitImpactPlayback();
        }

        /// <summary>主线首败按展示关锁定终身免费复活上限；无尽用有限关进度（已超过 14）。</summary>
        void LockFreeReviveCapIfNeeded()
        {
            if (!GPUserDataManager.HasInit())
                return;
            if (GameDirector.levelController == null)
                return;

            int displayLevel = GPUserDataManager.Instance.GetBInfinitMode()
                ? GPUserDataManager.Instance.GetLevelNum() + 1
                : GameDirector.levelController.GetLoadedLevelIndex() + 1;
            if (displayLevel <= 0)
                return;
            GPUserDataManager.Instance.TryLockFreeReviveCapOnFirstFail(displayLevel);
        }

        void RefreshFreeReviveUI()
        {
            int remain = GPUserDataManager.Instance.GetFreeReviveRemainCount();
            bool showFree = remain > 0;
            freeImage.SetActiveEx(showFree);
            adImage.SetActiveEx(!showFree);
            if (showFree && freeReviveCountLb != null)
            {
                freeReviveCountLb.text = string.Format(I2LocalizeMgr.GetString("UI_revivefree"), remain);
            }
        }

        /// <summary>失败页：一个活动断进度用 StageOverTips_1，两个用 StageOverTips_2。两个都有时进度高的排前面。</summary>
        public void RefreshStageOverTips()
        {
            if (!stageOverOrderChecked)
            {
                stageOverOrderChecked = true;
                Debug.Assert(PiggyBeforePuppy(4, 2));
                Debug.Assert(!PiggyBeforePuppy(2, 4));
                Debug.Assert(!PiggyBeforePuppy(3, 3));
                Debug.Assert(ShowPiggyNode(3, "3") && ShowPiggyNode(5, "5") && ShowPiggyNode(7, "7")
                    && !ShowPiggyNode(3, "5") && !ShowPiggyNode(4, "5"));
            }

            Transform one = transform.Find("StageOverTips_1");
            Transform two = transform.Find("StageOverTips_2");
            bool showPuppy = PuppyAdventureDemoState.IsRunning;
            bool showPiggy = PiggyBankState.ShowFailTips;
            int count = (showPuppy ? 1 : 0) + (showPiggy ? 1 : 0);
            if (one != null)
                one.gameObject.SetActive(count == 1);
            if (two != null)
                two.gameObject.SetActive(count == 2);
            if (count == 0)
                return;

            Transform root = count == 2 ? two : one;
            if (root == null)
                return;

            Transform layout = root.Find("layout");
            Transform puppy = layout != null ? layout.Find("puppy") : null;
            Transform piggy = layout != null ? layout.Find("piggy") : null;
            if (puppy != null)
                puppy.gameObject.SetActive(showPuppy);
            if (piggy != null)
                piggy.gameObject.SetActive(showPiggy);
            if (showPiggy)
                ApplyPiggyStage(piggy, PiggyBankState.FailLostStageLevel);

            int puppyProgress = showPuppy ? PuppyAdventureDemoState.CurWin : 0;
            int piggyProgress = showPiggy ? PiggyBankState.FailLostProgress : 0;
            if (showPuppy && showPiggy && puppy != null && piggy != null)
            {
                if (PiggyBeforePuppy(piggyProgress, puppyProgress))
                    piggy.SetAsFirstSibling();
                else
                    puppy.SetAsFirstSibling();
            }

            SetProcess(puppy, showPuppy
                ? $"{PuppyAdventureDemoState.CurWin}/{PuppyAdventureDemoState.WinTarget}"
                : null);
            SetProcess(piggy, showPiggy
                ? $"{PiggyBankState.FailLostProgress}/{Mathf.Max(1, PiggyBankState.FailLostStageLevel)}"
                : null);

            Transform tipsTf = root.Find("Tips");
            var tips = tipsTf != null ? tipsTf.GetComponent<TextMeshProUGUI>() : null;
            if (tips != null)
            {
                var localize = tips.GetComponent<I2.Loc.Localize>();
                if (localize != null)
                    localize.enabled = false;
                XSortGamePlay.LocalizedTMPRefresher.SetText(tips, BuildLoseText);
            }

            if (layout is RectTransform layoutRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRt);
        }

        static bool PiggyBeforePuppy(int piggyProgress, int puppyProgress) => piggyProgress > puppyProgress;

        /// <summary>失败页金猪：阶段 3/5/7 只显示同名节点（tub1/tub2/tub3）。</summary>
        static void ApplyPiggyStage(Transform piggy, int stageLevel)
        {
            if (piggy == null)
                return;
            for (int i = 0; i < piggy.childCount; i++)
            {
                Transform child = piggy.GetChild(i);
                if (child.name != "3" && child.name != "5" && child.name != "7")
                    continue;
                child.gameObject.SetActive(ShowPiggyNode(stageLevel, child.name));
            }
        }

        static bool ShowPiggyNode(int stageLevel, string node) =>
            (stageLevel == 3 || stageLevel == 5 || stageLevel == 7) && node == stageLevel.ToString();

        static void SetProcess(Transform icon, string text)
        {
            if (icon == null || text == null)
                return;
            Transform process = icon.Find("process");
            var label = process != null ? process.GetComponent<TextMeshProUGUI>() : null;
            if (label != null)
                label.text = text;
        }

        static string BuildLoseText()
        {
            bool showPuppy = PuppyAdventureDemoState.IsRunning;
            bool showPiggy = PiggyBankState.ShowFailTips;
            string puppyName = I2LocalizeMgr.GetString("JungleQuest_Title");
            string piggyName = I2LocalizeMgr.GetString("UI_EggSmash");
            if (showPuppy && showPiggy)
            {
                bool piggyFirst = PiggyBeforePuppy(PiggyBankState.FailLostProgress, PuppyAdventureDemoState.CurWin);
                string first = piggyFirst ? piggyName : puppyName;
                string second = piggyFirst ? puppyName : piggyName;
                return string.Format(I2LocalizeMgr.GetString("UI_Revive1Lose"), first, second);
            }

            string only = showPuppy ? puppyName : piggyName;
            return string.Format(I2LocalizeMgr.GetString("UI_Revive2Lose"), only);
        }

        private void ReplayButton()
        {
            if (XSortAppUpdate.TryShow("Fail.Replay"))
                return;
            ReplayButtonFunc(false);
            AdsPlayStrategy.SetBTodayFirstChapin();//今日首次打关取消
        }

        private void ReplayButtonFunc(bool bAuto)
        {
            if (XSortAppUpdate.TryShow("Fail.Replay"))
                return;
            if (!bAuto && !inputReady)
                return;
            if (GPSDKMgr.Instance != null)
            {
                if (GPSDKMgr.Instance.IsAdPlaying || GPSDKMgr.Instance.ShouldIgnorePostAdClick())
                {
                    return;
                }

                if (GPSDKMgr.Instance.HasPendingRewardGrant)
                {
                    // 用户明确放弃等待迟到 Viewed，空白重开才清掉待发奖
                    if (bAuto)
                    {
                        return;
                    }

                    GPSDKMgr.Instance.ClearPendingRewardGrant();
                }
            }

            // 不复活（点放弃/倒计时自动重开）：先弹冒险落水，点继续后再 ExecuteReplay
            if (PuppyAdventureFlow.TryShowGiveUp(() => ExecuteReplay(bAuto)))
            {
                AudioController.PlaySound(AudioController.AudioClips.buttonSound);
                StopCountdownAnimation();
                UIController.HidePage<UIGameOver>();
                return;
            }

            ExecuteReplay(bAuto);
        }

        void ExecuteReplay(bool bAuto)
        {
            if (GameData.BFromStartScene)
            {
                AdsPlayStrategy.TryPlayOnFailRestart();

                GameBAData.ExcuteMainStage(2); //失败统一记录为2
                GPUserDataManager.Instance.IncrementFailCount();
            }

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            StopCountdownAnimation(); // 停止倒计时

            UIController.HidePage<UIGameOver>();
            if (bAuto)
            {
                GameDirector.ReplayLevel(8);
            }
            else
            {
                GameDirector.ReplayLevel(9);
            }
        }

        #endregion

        #region Countdown Animation

        /// <summary>
        /// 开始倒计时：保留秒数显示与结束回调，表盘视觉改由 miaobiao Spine 播放。
        /// </summary>
        private void StartCountdownAnimation()
        {
            StopCountdownAnimation();

            SetCountdownDisplay(CountdownStartSecond);
            PlayMiaobiaoAnimation();
            failEffect?.Play(countdownTime);

            countdownTickCases = new TweenClip[CountdownStartSecond];
            for (int i = 1; i <= CountdownStartSecond; i++)
            {
                int displaySecond = CountdownStartSecond - i;
                countdownTickCases[i - 1] = Tween.DelayedCall(i * CountdownStepInterval, () =>
                {
                    SetCountdownDisplay(displaySecond);
                }, unscaledTime: true);
            }

            countdownEndCase = Tween.DelayedCall(countdownTime, () =>
            {
                ReplayButtonFunc(true);
            }, unscaledTime: true);
        }

        /// <summary>
        /// 停止倒计时动画
        /// </summary>
        private void StopCountdownAnimation()
        {
            if (countdownTickCases != null)
            {
                for (int i = 0; i < countdownTickCases.Length; i++)
                {
                    if (countdownTickCases[i] != null && countdownTickCases[i].IsActive)
                    {
                        countdownTickCases[i].Kill();
                    }
                }

                countdownTickCases = null;
            }

            if (countdownEndCase != null && countdownEndCase.IsActive)
            {
                countdownEndCase.Kill();
                countdownEndCase = null;
            }

            failEffect?.Stop();
            FreezeMiaobiaoAnimation();
        }

        void EnableFailPageInput()
        {
            blankAreaClickEnableCase = null;
            if (!isPageDisplayed)
                return;

            inputReady = true;
            if (replayButton != null)
                replayButton.interactable = true;
        }

        void CancelBlankAreaClickEnableDelay()
        {
            if (blankAreaClickEnableCase != null && blankAreaClickEnableCase.IsActive)
            {
                blankAreaClickEnableCase.Kill();
                blankAreaClickEnableCase = null;
            }
        }

        private void SetCountdownDisplay(int second)
        {
            if (timeCountLb != null)
            {
                timeCountLb.text = second.ToString();
            }
        }

        private void PlayMiaobiaoAnimation()
        {
            PlayMiaobiaoOn(miaobiaoSpine);
            PlayMiaobiaoOn(miaobiaoIceSpine);
        }

        private void PlayMiaobiaoOn(SkeletonGraphic spine)
        {
            if (spine == null)
            {
                return;
            }

            spine.UnscaledTime = true;
            spine.freeze = false;

            if (!spine.IsValid)
            {
                spine.Initialize(true);
            }

            if (spine.AnimationState == null || spine.Skeleton == null)
            {
                return;
            }

            spine.AnimationState.ClearTracks();
            spine.Skeleton.SetToSetupPose();
            var entry = spine.AnimationState.SetAnimation(0, MiaobiaoAnimationName, false);
            if (entry != null)
            {
                entry.MixDuration = 0f;
                entry.TrackTime = 0f;
                // 播完后定格末帧，避免 Clear/空轨回到 setup 造成表盘红色“倒退”
                entry.Complete += trackEntry => HoldMiaobiaoLastFrame(spine, trackEntry);
            }

            spine.Update(0f);
            spine.LateUpdate();
        }

        private void PrepareMiaobiaoAnimation()
        {
            PrepareMiaobiaoOn(miaobiaoSpine);
            PrepareMiaobiaoOn(miaobiaoIceSpine);
        }

        private void PrepareMiaobiaoOn(SkeletonGraphic spine)
        {
            if (spine == null)
            {
                return;
            }

            PlayMiaobiaoOn(spine);
            spine.freeze = true;
        }

        private void FreezeMiaobiaoAnimation()
        {
            FreezeMiaobiaoOn(miaobiaoSpine);
            FreezeMiaobiaoOn(miaobiaoIceSpine);
        }

        private void FreezeMiaobiaoOn(SkeletonGraphic spine)
        {
            if (spine == null)
            {
                return;
            }

            var entry = spine.AnimationState != null
                ? spine.AnimationState.GetCurrent(0)
                : null;
            if (entry != null)
            {
                entry.TimeScale = 0f;
                if (spine.Skeleton != null)
                {
                    spine.AnimationState.Apply(spine.Skeleton);
                }

                spine.Update(0f);
                spine.LateUpdate();
            }

            spine.freeze = true;
        }

        private void HoldMiaobiaoLastFrame(SkeletonGraphic spine, Spine.TrackEntry trackEntry)
        {
            if (spine == null || spine.AnimationState == null)
            {
                return;
            }

            var entry = trackEntry ?? spine.AnimationState.GetCurrent(0);
            if (entry == null || entry.Animation == null)
            {
                return;
            }

            entry.TimeScale = 0f;
            entry.TrackTime = entry.AnimationEnd;
            if (spine.Skeleton != null)
            {
                spine.AnimationState.Apply(spine.Skeleton);
            }

            spine.Update(0f);
            spine.LateUpdate();
            spine.freeze = true;
        }

        #endregion
    }
}
