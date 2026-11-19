using System.Collections.Generic;
using cfg.Game;
using FishFramework;
using FishXSortPlay;

namespace XSortGameCore
{
    /// <summary>
    /// 动态难度 / 同色组合 / 广告奖励配置的"按平台 + 关卡号取配置"统一入口。
    /// 源表已按平台拆分为 Android / iOS 两套 Luban 表；对外返回类型与拆分前一致。
    /// </summary>
    public static class LevelDynamicConfigProvider
    {
        public static GameLevelConfig GetLevelConfig(int levelId)
        {
            return GetLevelConfig(LevelPlatformResolver.GetCurrentPlatform(), levelId);
        }

        public static GameLevelConfig GetLevelConfig(LevelPlatform platform, int levelId)
        {
            return platform == LevelPlatform.IOS
                ? ConfigHelper.Tables.TbGameLeveConfigIOS.GetOrDefault(levelId)
                : ConfigHelper.Tables.TbGameLeveConfigAndroid.GetOrDefault(levelId);
        }

        public static GameLevelXSortsCfg GetXSortsConfig(int key)
        {
            return GetXSortsConfig(LevelPlatformResolver.GetCurrentPlatform(), key);
        }

        public static GameLevelXSortsCfg GetXSortsConfig(LevelPlatform platform, int key)
        {
            var dic = platform == LevelPlatform.IOS
                ? ConfigHelper.Tables.TbGameLevelXSortsCfgIOS.DataMap
                : ConfigHelper.Tables.TbGameLevelXSortsCfgAndroid.DataMap;
            return dic.ContainsKey(key) ? dic[key] : null;
        }

        public static Dictionary<int, CSVLevelAdReward> GetAdRewardMap()
        {
            return GetAdRewardMap(LevelPlatformResolver.GetCurrentPlatform());
        }

        public static Dictionary<int, CSVLevelAdReward> GetAdRewardMap(LevelPlatform platform)
        {
            return platform == LevelPlatform.IOS
                ? ConfigHelper.Tables.TbLevelAdRewardIOS.DataMap
                : ConfigHelper.Tables.TbLevelAdRewardAndroid.DataMap;
        }
    }
}
