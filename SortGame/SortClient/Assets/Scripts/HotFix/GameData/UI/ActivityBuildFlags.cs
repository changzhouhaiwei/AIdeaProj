using FishFramework;
using UnityEngine;

/// <summary>
/// 活动/玩法相关硬开关。开关配置在 <see cref="GameSettings"/>，发版或热更时勾选对应项；
/// 业务代码禁止直接读取，请走各功能的 IsXxxEnabled()。
/// </summary>
public static class ActivityBuildFlags
{
    static GameSettings LoadSettings()
    {
        GameSettings settings = null;
        if (GameModule.Setting != null)
        {
            settings = GameModule.Setting.GetGameSettings();
        }

        if (settings == null)
        {
            settings = Resources.Load<GameSettings>(nameof(GameSettings));
        }

        return settings;
    }

    /// <summary>黄金树 / 段位活动。改为 false 可关闭（不弹窗、不显示按钮、关卡内不生效、x2 不生效）。</summary>
    public static bool GoldTreeActEnabled => LoadSettings()?.goldTreeActEnabled ?? false;

    /// <summary>全局七日连胜（WinStreak）。改为 false 可关闭（不显示按钮、不弹窗、不记录连胜）。</summary>
    public static bool DailyVictoryActEnabled => LoadSettings()?.dailyVictoryActEnabled ?? false;

    /// <summary>每日转盘。改为 false 可隐藏主界面的转盘入口。</summary>
    public static bool CircleAwardEnabled => LoadSettings()?.circleAwardEnabled ?? true;

    /// <summary>狗狗大冒险。关闭后入口、弹窗、进度、结算接入和复活提醒全部关闭。</summary>
    public static bool PuppyAdventureEnabled => LoadSettings()?.puppyAdventureEnabled ?? true;

    /// <summary>关卡主题（背景、按钮、结算）。false 时 tilebaseId=0；true 时只跟关卡表里写死的单个主题，不随机。</summary>
    public static bool RandomTileBaseThemeEnabled => LoadSettings()?.randomTileBaseThemeEnabled ?? false;

    /// <summary>经典随机底板，读 TileBaseStyle。false 时棋子底板仍跟关卡主题。</summary>
    public static bool ClassicRandomTileBaseEnabled => LoadSettings()?.classicRandomTileBaseEnabled ?? false;

    /// <summary>金币关卡。改为 false 可关闭（配置 coinlevel=1 的关卡也不生成金币棋子、不弹 Coin Collect）。</summary>
    public static bool CoinRewardLevelEnabled => LoadSettings()?.coinRewardLevelEnabled ?? false;

    /// <summary>颜色挑战。改为 false 可关闭（配置了 colorchallenge 的关卡也按普通关生成花色，不显示颜色关提示）。</summary>
    public static bool ColorChallengeEnabled => LoadSettings()?.colorChallengeEnabled ?? true;
}
