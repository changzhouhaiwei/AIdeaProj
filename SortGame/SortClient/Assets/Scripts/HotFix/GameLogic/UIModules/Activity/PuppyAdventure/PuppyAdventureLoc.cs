using FishFramework;

public static class PuppyAdventureLoc
{
    public static string Get(string key, params object[] args)
    {
        string raw = I2LocalizeMgr.GetString(key);
        if (string.IsNullOrEmpty(raw) || raw == key)
            raw = Fallback(key);

        if (args != null && args.Length > 0)
            return string.Format(raw, args);
        return raw;
    }

    static string Fallback(string key)
    {
        return key switch
        {
            "JungleQuest_Title" => "狗狗大冒险",
            "JungleQuest_StartDes_5" => "打 5 关即可完成挑战！",
            "JungleQuest_StartDes_7" => "打 7 关即可完成挑战！",
            "JungleQuest_MatchingDes" => "正在寻找同水平玩家",
            "JungleQuest_StageName_5" => "挑战5",
            "JungleQuest_StageName_7" => "挑战7",
            "JungleQuest_MainRemainDes" => "再打 {0} 关即可完成挑战！",
            "JungleQuest_PlayersLeft" => "还剩 {0} 名玩家！",
            "JungleQuest_ShareTip" => "大奖由胜者平分！",
            "JungleQuest_Help1" => "100 名玩家同时开打！",
            "JungleQuest_Help2" => "连续通关关卡！",
            "JungleQuest_Help3" => "与剩余赢家平分金币奖池！",
            "JungleQuest_Help4" => "放弃复活即本轮失败！打满关数即可领奖。",
            "JungleQuest_FailDes" => "本轮挑战失败。",
            "JungleQuest_FailCooldownDes" => "本轮挑战失败。下一轮开始倒计时：",
            "JungleQuest_FailKeepStage" => "冷却结束后仍为{0}",
            "JungleQuest_RewardTitle" => "你赢了：",
            "JungleQuest_ShareReward" => "你正与另外 {0} 位赢家平分奖励！",
            "JungleQuest_NextStageDes" => "下一挑战：打 {0} 关",
            "JungleQuest_MaxStageDes" => "已达最高挑战",
            "JungleQuest_DailyClosedDes" => "今日已胜利2次，次日04:00再来",
            "JungleQuest_ChallengeTimeDes" => "剩余时间：",
            "JungleQuest_GrandPrize" => "终极大奖",
            "JungleQuest_WinAdvance" => "恭喜！已前进到下一步！",
            "UI_Start" => "开始",
            "UI_TaptoContinue" => "点击继续",
            "UI_Congratulations" => "恭喜！",
            _ => key
        };
    }
}
