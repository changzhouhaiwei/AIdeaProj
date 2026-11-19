using System;
using UnityEngine;
using FishFramework;
using EwingSoft.GameSDK;
#if UNITY_IOS
using AdSdk = EwingSoft.GameSDK.IosBridge.AdSdk;
#endif

namespace FishXSortPlay
{
    /// <summary>
    /// 插屏播放策略：公共门禁统一走 PassCommonGates，各触发点只补自己的额外条件。
    /// 待机回前台：关内立刻播；主界面/结算记资格，进关时串行播完再 LoadLevel。
    /// </summary>
    public static class AdsPlayStrategy
    {
        const string DayFirstChapinKey = "dayFirstChapinKey";
        const int StandbySeconds = 300;
        const int InterstitialAdType = 2;
        const int IncentiveMinWinCnt = 0;
        const int DefaultMinWinCnt = 2;
        const int EGroupMinWinCnt = 0;
        const int MaxContinFailCnt = 4;

        static long s_enterBackgroundUnix;
        static bool s_pendingEnterLevelStandbyAd;
        static bool s_rewardedAdTriggeredInCurrentLevel;
        static DateTime s_cachedTodayDate;
        static string s_cachedTodayDateStr;
        static Action s_afterEnterLevelInterstitial;

        readonly struct MediaRules
        {
            public readonly int UnlockLevel;
            public readonly int MinWinCnt;
            public readonly int MaxFailCnt;

            public MediaRules(int unlockLevel, int minWinCnt, int maxFailCnt)
            {
                UnlockLevel = unlockLevel;
                MinWinCnt = minWinCnt;
                MaxFailCnt = maxFailCnt;
            }
        }

        static MediaRules GetMediaRules()
        {
            int playLevel = InterADABTestConfig.GetUnlockCompletedLevel();
            if (GPSDKMgr.Instance.GetBIsMediaSourceType1())
                return new MediaRules(playLevel, IncentiveMinWinCnt, MaxContinFailCnt);

            return new MediaRules(playLevel, DefaultMinWinCnt, MaxContinFailCnt);
        }

        static bool IsEGroup()
        {
            InterADABTestConfig.TryAssignGroup();
            return InterADABTestConfig.UsesEGroupRules();
        }

        static bool ShouldSuppressCurrentLevelInterstitial()
        {
            return s_rewardedAdTriggeredInCurrentLevel && IsEGroup();
        }

        static string GetInterAdPlace(string place)
        {
            return place ?? "";
        }

        static void ReportInterNoShow(string place, string reason)
        {
            GameBAData.ExcuteInterAdNoShow(GetInterAdPlace(place), reason);
        }

        /// <summary>
        /// 正式进游、未买去广告、本关未被激励抑制、
        /// 过解锁关、不是当日第一条（E 组豁免）、连败未达上限。
        /// </summary>
        static bool PassCommonGates(MediaRules rules, string place)
        {
            if (!GameData.BFromStartScene)
                return false;
            if (GPUserDataManager.Instance.GetBNoAd())
            {
                ReportInterNoShow(place, BAInterAdNoShowData.ReasonNoAd);
                return false;
            }
            if (ShouldSuppressCurrentLevelInterstitial())
            {
                ReportInterNoShow(place, BAInterAdNoShowData.ReasonRvSuppress);
                return false;
            }
            if (GPUserDataManager.Instance.GetLevelNum() < rules.UnlockLevel)
            {
                ReportInterNoShow(place, BAInterAdNoShowData.ReasonLevelLocked);
                return false;
            }
            if (!IsEGroup() && IsTodayFirstChapin())
            {
                ReportInterNoShow(place, BAInterAdNoShowData.ReasonTodayFirst);
                return false;
            }
            if (GPUserDataManager.Instance.UserData.continusFailCnt >= rules.MaxFailCnt)
            {
                ReportInterNoShow(place, BAInterAdNoShowData.ReasonFailStreak);
                return false;
            }
            return true;
        }

        static bool PassCommonGatesAndInterval(string place)
        {
            if (!PassCommonGates(GetMediaRules(), place))
                return false;
            if (!InterADABTestConfig.CanShowByInterval())
            {
                ReportInterNoShow(place, InterADABTestConfig.GetIntervalBlockReason());
                return false;
            }
            return true;
        }

        #region Reward

        public static bool CheckRewardAdsPlay(string placeStr)
        {
#if UNITY_EDITOR
            return true;
#else
            if (!AdSdk.IsRewardAdLoaded())
            {
                AlertDlg.ShowDlg(I2LocalizeMgr.GetString("FailToLoad"));
                GameBAData.ExcuteRewardAdNoShow(placeStr, BAInterAdNoShowData.ReasonNoFill);
                return false;
            }
            return true;
#endif
        }

        public static void BeginLevel()
        {
            s_rewardedAdTriggeredInCurrentLevel = false;
        }

        public static void MarkRewardedAdTriggeredInCurrentLevel()
        {
            s_rewardedAdTriggeredInCurrentLevel = true;
        }

        #endregion

        #region Daily first

        static string GetTodayDateOnly()
        {
            var now = DateTime.Now;
            if (s_cachedTodayDateStr == null || now.Date != s_cachedTodayDate)
            {
                s_cachedTodayDate = now.Date;
                s_cachedTodayDateStr = now.ToString("yyyy-MM-dd");
            }

            return s_cachedTodayDateStr;
        }

        static bool IsTodayFirstChapin()
        {
            string saveDateStr = PlayerPrefs.GetString(DayFirstChapinKey, "");
            return !GetTodayDateOnly().Equals(saveDateStr);
        }

        public static void SetBTodayFirstChapin()
        {
            PlayerPrefs.SetString(DayFirstChapinKey, GetTodayDateOnly());
        }

        #endregion

        #region Background standby

        public static bool HasSavedBackgroundTime => s_enterBackgroundUnix != 0;

        public static void SaveBackgroundTime()
        {
            // 广告 Activity 抢焦点不算真后台，避免关播后误判「待机回前台」
            if (GPSDKMgr.Instance != null && GPSDKMgr.Instance.IsAdPlaying)
                return;

            s_enterBackgroundUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public static void ClearBackgroundTime()
        {
            s_enterBackgroundUnix = 0;
        }

        public static void EnterForeground()
        {
            if (GPSDKMgr.Instance != null && GPSDKMgr.Instance.IsAdPlaying)
                return;
            if (s_enterBackgroundUnix == 0)
                return;

            long deltaTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - s_enterBackgroundUnix;
            if (deltaTime >= StandbySeconds)
            {
                s_pendingEnterLevelStandbyAd = false;
                if (GameDirector.IsGameActive)
                    TryPlayOnForegroundInLevel();
                else
                    s_pendingEnterLevelStandbyAd = true;
            }

            s_enterBackgroundUnix = 0;
        }

        /// <summary>
        /// 进关待机插屏：只判断并消费资格，不立刻弹 AdBreak。
        /// LoadLevel 在播完（或未加载可跳过）后再加载关卡，避免与插屏叠内存。
        /// </summary>
        public static bool TryConsumeEnterLevelStandbyAd()
        {
            if (!s_pendingEnterLevelStandbyAd)
                return false;
            if (!PassCommonGatesAndInterval("OnforegroudEnterLevel"))
                return false;

            s_pendingEnterLevelStandbyAd = false;
            return true;
        }

        /// <summary>
        /// AdBreak → 插屏；关播/失败/未加载时再执行 onFinished（通常为 LoadLevel 正文）。
        /// </summary>
        public static void PlayEnterLevelStandbyAdThen(Action onFinished)
        {
            ShowTestDlg("AD === 4.2");
            ShowAdBreakDlg(() =>
            {
                s_afterEnterLevelInterstitial = onFinished;
                GPSDKMgr.Instance.PlayInterstitialAd("OnforegroudEnterLevel", InterstitialAdType);
            });
        }

        /// <summary>
        /// 进关插屏关播或超时失败时推进「先广告后进关」后续逻辑；无挂起回调时为空操作。
        /// 仅应由插屏结束路径调用，不要绑到激励 AdsPlayFailed。
        /// </summary>
        public static void NotifyEnterLevelInterstitialFinished()
        {
            var act = s_afterEnterLevelInterstitial;
            s_afterEnterLevelInterstitial = null;
            act?.Invoke();
        }

        #endregion

        #region Play moments

        /// <summary>关内待机回前台：AdBreak 后播 OnforegroudInLevel。</summary>
        public static bool TryPlayOnForegroundInLevel()
        {
            if (!PassCommonGatesAndInterval("OnforegroudInLevel"))
                return false;

            ShowTestDlg("AD=== 4.1");
            ShowAdBreakDlg(() => PlayInterstitialAd("OnforegroudInLevel"));
            return true;
        }

        /// <summary>胜利结算前。非激励通关当前档开启关后首条必出、无连胜门槛；激励渠道为 0，其余为 2。</summary>
        public static bool TryPlayOnWin()
        {
            var rules = GetMediaRules();
            if (!PassCommonGates(rules, "Win"))
                return false;

            int minWinCnt = IsEGroup() ? EGroupMinWinCnt : rules.MinWinCnt;
            if (GPUserDataManager.Instance.UserData.continusWinCnt < minWinCnt)
            {
                ReportInterNoShow("Win", BAInterAdNoShowData.ReasonWinStreak);
                return false;
            }
            if (!InterADABTestConfig.CanShowByInterval())
            {
                ReportInterNoShow("Win", InterADABTestConfig.GetIntervalBlockReason());
                return false;
            }

            ShowTestDlg("AD=== 1.2");
            PlayInterstitialAd("Win");
            return true;
        }

        /// <summary>失败页离开 / 重新开始。通关当前档开启关后过 CD。</summary>
        public static bool TryPlayOnFailRestart()
        {
            if (!PassCommonGatesAndInterval("FailAfter"))
                return false;

            ShowTestDlg("AD=== 3.2");
            PlayInterstitialAd("FailAfter");
            return true;
        }

        public static void ShowAdBreakDlg(Action func)
        {
            var panel = GameModule.UI.OpenPanel<UIAdBreakDlg>();
            panel.afterAct = func;
        }

        #endregion

        static void PlayInterstitialAd(string placeStr, Action onShown = null)
        {
            GPSDKMgr.Instance.PlayInterstitialAd(placeStr, InterstitialAdType, errorCode =>
            {
                if (errorCode == 0)
                    onShown?.Invoke();
            });
        }

        static void ShowTestDlg(string str)
        {
            if (!GameData.BFromStartScene || !GameModule.Setting.GmMode)
                return;

            string group = InterADABTestConfig.GetTestGroupForAnalytics();
            int cd = InterADABTestConfig.GetIntervalSeconds();
            int eff = InterADABTestConfig.GetEffectiveIntervalSeconds();
            AlertDlg.ShowDlg($"{str}\ngroup={group} cd={cd}->{eff}s eligible={InterADABTestConfig.IsEligible()}");
        }
    }
}
