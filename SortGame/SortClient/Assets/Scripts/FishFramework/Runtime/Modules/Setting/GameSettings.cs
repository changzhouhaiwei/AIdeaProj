using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FishFramework
{
    public enum Languages
    {
        ChineseSimplified,
        ChineseTraditional,
        English,
        Spanish,
        Portuguese,
        Russian,
        German,
        French,
        Turkish,
        Indonesian,
        Italian,
        Japanese,
        Korean,
        Ukrainian,
    }

    public enum ChannelPlatform
    {
        None,
        FishSDK,
    }

    /// <summary>
    /// 关卡资源平台：区分 Android / iOS 各自独立的关卡数据库、单关资源、动态难度与最大关卡数。
    /// 注意：这与登录/渠道用的 <see cref="ChannelPlatform"/> 是两个概念，不要混用。
    /// </summary>
    public enum LevelPlatform
    {
        Android = 0,
        IOS = 1,
    }

    public enum ServerURLType
    {
        INTRA_URL = 1, // 内网
        OUTER_URL = 2, // 外网
    }


    [CreateAssetMenu(menuName = "GameSettings", fileName = "GameSettings", order = 0)]
    public class GameSettings : ScriptableObject
    {
        [LabelText("游戏的日志模式")] public bool logMode;

        [LabelText("是否开启GM")] public bool gmMode;

        [LabelText("游戏的运行帧频，默认60帧")] public int game60;

        [LabelText("打包语言选择")] public Languages language = Languages.ChineseSimplified;

        [LabelText("打包服务器地址选择")] public ServerURLType urlType = ServerURLType.INTRA_URL;

        [LabelText("打包平台选择")] public ChannelPlatform platform = ChannelPlatform.None;

        [LabelText("关卡资源平台(Android/iOS)")] public LevelPlatform levelPlatform = LevelPlatform.Android;

        [LabelText("Android最大关卡数")] public int androidMaxLevelNum = 1000;

        [LabelText("iOS最大关卡数")] public int iosMaxLevelNum = 100;

        [LabelText("游戏大版本")] public int appVersion = 1;

        [TitleGroup("资源版本号", boldTitle: true, horizontalLine: false, Indent = true)]
        public int major;

        [TitleGroup("资源版本号", boldTitle: true, Indent = true)]
        public int minor;

        [TitleGroup("资源版本号", boldTitle: true, Indent = true)]
        public int build;

        [TitleGroup("活动开关", boldTitle: true, horizontalLine: true)]
        [LabelText("黄金树 / 段位活动")]
        [Tooltip("关闭后：不弹窗、不显示按钮、关卡内不生效、x2 不生效")]
        public bool goldTreeActEnabled;

        [TitleGroup("活动开关")]
        [LabelText("全局七日连胜")]
        [Tooltip("关闭后：不显示按钮、不弹窗、不记录连胜")]
        public bool dailyVictoryActEnabled;

        [TitleGroup("活动开关")]
        [LabelText("每日转盘")]
        [Tooltip("关闭后隐藏主界面转盘入口")]
        public bool circleAwardEnabled = true;

        [TitleGroup("活动开关")]
        [LabelText("狗狗大冒险")]
        [Tooltip("关闭后：不显示入口、不弹窗、不记录进度、不显示复活提醒")]
        public bool puppyAdventureEnabled = true;

        [TitleGroup("活动开关")]
        [LabelText("关卡主题")]
        [Tooltip("关闭后不读关卡表 Theme，界面固定原皮。不关经典随机底板。")]
        public bool randomTileBaseThemeEnabled;

        [TitleGroup("活动开关")]
        [LabelText("经典随机底板")]
        [Tooltip("棋子底板。有限关 1–20 用 default，21 关起随机不放回。关卡主题关掉时，不看关卡表 Theme。")]
        public bool classicRandomTileBaseEnabled;

        [TitleGroup("活动开关")]
        [LabelText("金币关卡")]
        [Tooltip("关闭后：coinlevel=1 的关卡也不生成金币棋子、不弹 Coin Collect")]
        public bool coinRewardLevelEnabled;

        [TitleGroup("活动开关")]
        [LabelText("颜色挑战")]
        [Tooltip("关闭后：colorchallenge 有值的关卡也按普通关生成花色，不显示颜色关提示")]
        public bool colorChallengeEnabled = true;

#if UNITY_EDITOR
        public void AddVersion()
        {
            build += 1;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif

        public string GetVersion()
        {
            var ver = new Version(major, minor, build);
            return ver.ToString();
        }

        public string GetUploadHotupURL()
        {
            return "";
        }
    }
}