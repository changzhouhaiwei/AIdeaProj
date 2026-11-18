using UnityEngine;

namespace FishTilePlay
{
    public static class AutoCompleteSettings
    {
        const string PrefsKey = "AutoCompleteEnabled";
        const string UserConfiguredKey = "AutoCompleteUserConfigured";

        /// <summary>第六关（1-based）对应的 0-based 关卡索引。</summary>
        public const int MinUnlockedLevelIndex = 5;

        /// <summary>剩余棋子占初始总数比例上限（含备战栏）。</summary>
        public const float MaxRemainingRatio = 0.15f;

        /// <summary>剩余棋子数必须大于此值才触发（即最后 3 颗不触发）。</summary>
        public const int MinRemainingTiles = 3;

        /// <summary>初始组数下限（1 组 = 3 颗棋子）。</summary>
        public const int MinInitialGroupCount = 15;

        /// <summary>
        /// 动态配置第二项 easyHard（形如 [x,3]/[x,4]/[x,5]）为分层拆组时不触发自动完成。
        /// Android / iOS 均按本局实际读到的配置判断。
        /// </summary>
        static readonly int[] BlockedEasyHardValues = { 3, 4, 5 };

        public static bool IsFeatureUnlocked()
        {
            return GetCurrentLevelIndex() >= MinUnlockedLevelIndex;
        }

        /// <summary>
        /// 金币关、自带快速过关的特殊关，或本局动态配置 easyHard 为 3/4/5 时，自动完成不应触发。
        /// </summary>
        public static bool IsBlockedByCurrentLevelConfig()
        {
            if (LevelDirector.BInCoinRewardLevel)
                return true;

            // 这些关先完成自己的目标，再走专用快速过关。进度条触发不能提前清盘。
            if (BoxCoverManager.HasActiveLevel
                || IQChallengeManager.HasActiveLevel
                || AwardColorEggCoverManager.HasActiveLevel)
                return true;

            var levelGen = LevelDirector.LevelGenService;
            if (levelGen == null)
                return false;

            int easyHardValue = levelGen.GetDynamicData()?.easyHardValue ?? 0;
            for (int i = 0; i < BlockedEasyHardValues.Length; i++)
            {
                if (easyHardValue == BlockedEasyHardValues[i])
                    return true;
            }

            return false;
        }

        public static int GetCurrentLevelIndex()
        {
            if (GameDirector.IsGameActive && GameDirector.levelController != null)
                return GameDirector.levelController.GetLoadedLevelIndex();

            return GPUserDataManager.Instance.GetShowLevelNum();
        }

        public static bool IsEnabled
        {
            get
            {
                if (!IsFeatureUnlocked())
                    return false;

                // 第六关后、玩家尚未在设置里保存过选择时，默认开启
                if (!PlayerPrefs.HasKey(UserConfiguredKey))
                    return true;

                return PlayerPrefs.GetInt(PrefsKey, 1) == 1;
            }
            set
            {
                if (!IsFeatureUnlocked())
                    return;

                PlayerPrefs.SetInt(PrefsKey, value ? 1 : 0);
                PlayerPrefs.SetInt(UserConfiguredKey, 1);
                PlayerPrefs.Save();
            }
        }
    }
}
