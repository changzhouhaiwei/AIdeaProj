using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_IOS
using IosAttributionSdk = EwingSoft.GameSDK.Ios.AttributionSdk;
using IosFirebaseSdk = EwingSoft.GameSDK.Ios.FirebaseSdk;
using IosThinkingDataSdk = EwingSoft.GameSDK.Ios.ThinkingDataSdk;
#endif

namespace EwingSoft.GameSDK
{
    /// <summary>
    /// AEO 里程碑。配置来自 Remote Config 的 GameMilestoneConfig。
    /// 不放在会被 SDK 更新覆盖的目录里；由业务封装方法在广告关闭和通关时调用。
    /// </summary>
    public static class MilestoneSdk
    {
        const string ConfigKey = "GameMilestoneConfig";
        const string ConfigEvent = "MILESTONE_CONFIG";
        const string ConfigParam = "milestone_config";
        const float FlushIntervalSeconds = 0.016f;

        const string WatchAdKey = "sf_watch_ad_milestone";
        const string WatchRewardedAdKey = "sf_watch_rewarded_ad_milestone";
        const string LevelPassKey = "sf_level_pass_milestone";

        const string WatchAdEvent = "WatchAdMilestone";
        const string WatchRewardedAdEvent = "WatchRewardedAdMilestone";
        const string LevelPassEvent = "LevelPassMilestone";

        const string AdTypeInterstitial = "Interstitial";
        const string AdTypeRewarded = "Rewarded";
        const string LogTag = "[Milestone]";

        static bool s_configLoaded;
        static GameMilestoneConfigDto s_config;
        static string s_rewardScene;
        static readonly Queue<PendingCheck> s_pending = new Queue<PendingCheck>();

        /// <summary>
        /// Remote Config 拉取成功后由业务层调用，强制重读 GameMilestoneConfig。
        /// </summary>
        public static void NotifyRemoteConfigLoaded(bool success)
        {
            Log("RemoteConfig 回调 success=" + success);
            if (!success)
            {
                Log("跳过重读配置 reason=拉取失败");
                return;
            }

            ParseConfig(true);
        }

        /// <summary>
        /// 激励展示前记下 scene，关闭时作为 reward_scene。
        /// </summary>
        public static void RememberRewardScene(string scene)
        {
            s_rewardScene = scene;
            Log("记下激励场景 reward_scene=" + FormatExtra(scene));
        }

        public static void OnFullAdClosed(int errorCode)
        {
            Log("插屏关闭 errorCode=" + errorCode);
            WatchAd(AdTypeInterstitial);
        }

        public static void OnRewardedAdClosed()
        {
            string scene = s_rewardScene;
            s_rewardScene = null;
            Log("激励关闭 reward_scene=" + FormatExtra(scene));
            WatchAd(AdTypeRewarded);
            WatchRewardedAd(scene);
        }

        public static void LevelPass(int currentLevel)
        {
            if (!ShouldCount())
            {
                Log("通关跳过 level=" + currentLevel + " reason=编辑器不计数");
                return;
            }

            if (currentLevel <= 0)
            {
                Log("通关跳过 level=" + currentLevel + " reason=关卡号无效");
                return;
            }

            var passedLevels = LoadLevelSet(LevelPassKey);
            if (passedLevels.Contains(currentLevel))
            {
                Log("通关跳过 level=" + currentLevel + " reason=该关已计过 unique=" + passedLevels.Count);
                return;
            }

            passedLevels.Add(currentLevel);
            Log("通关计数 level=" + currentLevel + " unique=" + passedLevels.Count);
            CheckMilestone(LevelPassKey, passedLevels.Count, currentLevel);
            SaveLevelSet(LevelPassKey, passedLevels);
            PlayerPrefs.Save();
        }

        static void WatchAd(string adType)
        {
            if (!ShouldCount())
            {
                Log("看广告跳过 ad_type=" + adType + " reason=编辑器不计数");
                return;
            }

            int currentCount = PlayerPrefs.GetInt(WatchAdKey, 0) + 1;
            Log("看广告计数 count=" + currentCount + " ad_type=" + adType);
            CheckMilestone(WatchAdKey, currentCount, adType);
            PlayerPrefs.SetInt(WatchAdKey, currentCount);
            PlayerPrefs.Save();
        }

        static void WatchRewardedAd(string rewardScene)
        {
            if (!ShouldCount())
            {
                Log("看激励跳过 reward_scene=" + FormatExtra(rewardScene) + " reason=编辑器不计数");
                return;
            }

            int currentCount = PlayerPrefs.GetInt(WatchRewardedAdKey, 0) + 1;
            Log("看激励计数 count=" + currentCount + " reward_scene=" + FormatExtra(rewardScene));
            CheckMilestone(WatchRewardedAdKey, currentCount, rewardScene);
            PlayerPrefs.SetInt(WatchRewardedAdKey, currentCount);
            PlayerPrefs.Save();
        }

        static bool ShouldCount()
        {
            return !Application.isEditor;
        }

        static void ParseConfig(bool forceUpdate)
        {
            if (!forceUpdate && s_configLoaded)
            {
                return;
            }

            try
            {
                string config = ReadRemoteConfig();
                if (!string.IsNullOrEmpty(config))
                {
                    s_config = JsonUtility.FromJson<GameMilestoneConfigDto>(config);
                    var param = new Dictionary<string, object> { { ConfigParam, config } };
                    TrackThinkingData(ConfigEvent, param);
                }
                else
                {
                    s_config = null;
                }

                s_configLoaded = true;
                Log("配置解析完成 " + DescribeConfig(config));
            }
            catch (Exception e)
            {
                s_configLoaded = true;
                s_config = null;
                Debug.LogWarning(LogTag + " 配置解析失败 " + e.Message);
            }

            FlushPending();
        }

        static void CheckMilestone(string eventKey, int milestone, object extra)
        {
            if (!s_configLoaded)
            {
                s_pending.Enqueue(new PendingCheck(eventKey, milestone, extra));
                Log("配置未就绪，先入队 " + EventName(eventKey) + " value=" + milestone + " pending=" + s_pending.Count);
                ParseConfig(false);
                return;
            }

            EvaluateMilestone(eventKey, milestone, extra);
        }

        static void EvaluateMilestone(string eventKey, int milestone, object extra)
        {
            string milestoneEvent = EventName(eventKey);
            if (string.IsNullOrEmpty(milestoneEvent))
            {
                Log("跳过未知事件 key=" + eventKey + " value=" + milestone);
                return;
            }

            MilestoneSectionDto section = Section(eventKey);
            if (section == null || !section.enabled || section.milestones == null)
            {
                Log(milestoneEvent + " 跳过 value=" + milestone + " reason=未开启 " + DescribeSection(section));
                return;
            }

            if (Array.IndexOf(section.milestones, milestone) < 0)
            {
                Log(milestoneEvent + " 未命中 value=" + milestone + " " + DescribeSection(section));
                return;
            }

            string reportedKey = eventKey + milestone + "_reported";
            if (PlayerPrefs.GetInt(reportedKey, 0) != 0)
            {
                Log(milestoneEvent + " 跳过 value=" + milestone + " reason=已上报过 key=" + reportedKey);
                return;
            }

            string eventName = milestoneEvent + milestone;
            var param = new Dictionary<string, object>
            {
                { "milestone", milestone },
                { "total_count", milestone },
            };

            try
            {
                if (extra != null)
                {
                    if (eventKey == WatchAdKey)
                    {
                        param["ad_type"] = extra;
                    }
                    else if (eventKey == WatchRewardedAdKey)
                    {
                        param["reward_scene"] = extra;
                    }
                    else if (eventKey == LevelPassKey)
                    {
                        param["current_level"] = extra;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogTag + " 附加参数失败 event=" + eventName + " " + e.Message);
            }

            LogHit(eventName + " milestone=" + milestone
                + " total_count=" + milestone
                + DescribeExtra(eventKey, extra)
                + " -> AppsFlyer + ThinkingData");
            TrackAppsFlyer(eventName, param);
            TrackThinkingData(eventName, param);
            PlayerPrefs.SetInt(reportedKey, 1);
            PlayerPrefs.Save();
        }

        static void Log(string message)
        {
            Debug.Log(LogTag + " " + message);
        }

        static void LogHit(string message)
        {
            Debug.LogWarning(LogTag + " ★命中上报★ " + message);
        }

        static string FormatExtra(object extra)
        {
            return extra == null ? "null" : extra.ToString();
        }

        static string DescribeExtra(string eventKey, object extra)
        {
            if (extra == null)
            {
                return string.Empty;
            }

            if (eventKey == WatchAdKey)
            {
                return " ad_type=" + extra;
            }

            if (eventKey == WatchRewardedAdKey)
            {
                return " reward_scene=" + extra;
            }

            if (eventKey == LevelPassKey)
            {
                return " current_level=" + extra;
            }

            return string.Empty;
        }

        static string DescribeSection(MilestoneSectionDto section)
        {
            if (section == null)
            {
                return "section=null";
            }

            string list = section.milestones == null ? "null" : string.Join(",", section.milestones);
            return "enabled=" + section.enabled + " milestones=[" + list + "]";
        }

        static string DescribeConfig(string raw)
        {
            if (string.IsNullOrEmpty(raw) || s_config == null)
            {
                return "raw=empty 三档均不可用";
            }

            return "rawLen=" + raw.Length
                + " 看广告{" + DescribeSection(s_config.watch_ad_milestone) + "}"
                + " 看激励{" + DescribeSection(s_config.watch_rewarded_ad_milestone) + "}"
                + " 通关{" + DescribeSection(s_config.level_pass_milestone) + "}";
        }

        static void FlushPending()
        {
            if (s_pending.Count == 0)
            {
                return;
            }

            PendingCheck task = s_pending.Dequeue();
            FlushLater(task);
        }

        static async void FlushLater(PendingCheck task)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(FlushIntervalSeconds));
                EvaluateMilestone(task.EventKey, task.Milestone, task.Extra);
            }
            finally
            {
                FlushPending();
            }
        }

        static string EventName(string eventKey)
        {
            if (eventKey == WatchAdKey)
            {
                return WatchAdEvent;
            }

            if (eventKey == WatchRewardedAdKey)
            {
                return WatchRewardedAdEvent;
            }

            if (eventKey == LevelPassKey)
            {
                return LevelPassEvent;
            }

            return null;
        }

        static MilestoneSectionDto Section(string eventKey)
        {
            if (s_config == null)
            {
                return null;
            }

            if (eventKey == WatchAdKey)
            {
                return s_config.watch_ad_milestone;
            }

            if (eventKey == WatchRewardedAdKey)
            {
                return s_config.watch_rewarded_ad_milestone;
            }

            if (eventKey == LevelPassKey)
            {
                return s_config.level_pass_milestone;
            }

            return null;
        }

        static HashSet<int> LoadLevelSet(string key)
        {
            var values = new HashSet<int>();
            string data = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(data))
            {
                return values;
            }

            string[] split = data.Split('|');
            for (int i = 0; i < split.Length; i++)
            {
                string value = split[i].Trim();
                if (int.TryParse(value, out int level))
                {
                    values.Add(level);
                }
                else if (value.Length > 0)
                {
                    Debug.LogWarning("Parse number [" + value + "] failed.");
                }
            }

            return values;
        }

        static void SaveLevelSet(string key, HashSet<int> values)
        {
            PlayerPrefs.SetString(key, string.Join("|", values));
        }

        static string ReadRemoteConfig()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return FirebaseSdk.GetStringRemoteConfig(ConfigKey);
#elif UNITY_IOS && !UNITY_EDITOR
            return IosFirebaseSdk.GetStringRemoteConfig(ConfigKey);
#else
            return string.Empty;
#endif
        }

        static void TrackAppsFlyer(string eventName, Dictionary<string, object> parameters)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AttrSdk.TrackEvent(eventName, parameters);
#elif UNITY_IOS && !UNITY_EDITOR
            IosAttributionSdk.TrackEvent(eventName, parameters);
#endif
        }

        static void TrackThinkingData(string eventName, Dictionary<string, object> parameters)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ThinkingDataSdk.LogEvent(eventName, parameters);
#elif UNITY_IOS && !UNITY_EDITOR
            IosThinkingDataSdk.LogEvent(eventName, parameters);
#endif
        }

        readonly struct PendingCheck
        {
            public readonly string EventKey;
            public readonly int Milestone;
            public readonly object Extra;

            public PendingCheck(string eventKey, int milestone, object extra)
            {
                EventKey = eventKey;
                Milestone = milestone;
                Extra = extra;
            }
        }

        [Serializable]
        class GameMilestoneConfigDto
        {
            public MilestoneSectionDto watch_ad_milestone;
            public MilestoneSectionDto watch_rewarded_ad_milestone;
            public MilestoneSectionDto level_pass_milestone;
        }

        [Serializable]
        class MilestoneSectionDto
        {
            public bool enabled;
            public int[] milestones;
        }
    }
}
