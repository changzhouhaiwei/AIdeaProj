using System;
using System.Globalization;
using FishFramework;
using XSortGamePlay;
using UnityEngine;

[Serializable]
public sealed class PiggyBankSaveData
{
    public string dayId = "";
    public int stageIndex;
    public int progress;
    public bool pendingClaim;
    /// <summary>累计「当天第一次连胜」次数，规则同小狗 startCount。</summary>
    public int activityStartCount;
    /// <summary>最近一次打过 status=0 的日期。换日后再赢才 +1。</summary>
    public string activityRoundDayId = "";
}

/// <summary>
/// Pinata 连胜存钱罐。通关第 100 关后开启；本地 0 点重置；阶段顺序 3→5→7。
/// </summary>
public static class PiggyBankState
{
    public const int UnlockCompletedLevels = 100;
    const string SaveKeyPrefix = "PiggyBank_v1_";

    static PiggyBankSaveData data;
    static string loadedSaveKey;

    /// <summary>结算条用：本关胜利时的阶段连胜目标（3/5/7）与进度，Claim 推进后仍保留。</summary>
    public static int SettlementStageLevel { get; private set; }
    public static int SettlementProgress { get; private set; }
    public static bool SettlementHadWin { get; private set; }
    /// <summary>本关失败清掉了连胜进度，失败页显示金猪图标。</summary>
    public static bool ShowFailTips { get; private set; }
    /// <summary>被清掉的连胜数，失败页用来比进度、画 process。</summary>
    public static int FailLostProgress { get; private set; }
    public static int FailLostStageLevel { get; private set; }

    public static event Action Changed;

    public static PiggyBankSaveData Data
    {
        get
        {
            string key = SaveKey();
            if (data == null || loadedSaveKey != key)
            {
                loadedSaveKey = key;
                data = LoadData(key);
            }
            EnsureDay();
            return data;
        }
    }

    public static bool IsUnlocked
    {
        get
        {
            if (!GameData.BFromStartScene || !GPUserDataManager.HasInit())
                return false;
            return GPUserDataManager.Instance.GetLevelNum() >= UnlockCompletedLevels;
        }
    }

    /// <summary>第 100 关通关当胜不计，从下一关开始记连胜。</summary>
    public static bool CanRecordWin
    {
        get
        {
            if (!IsUnlocked)
                return false;
            return GPUserDataManager.Instance.GetLevelNum() > UnlockCompletedLevels;
        }
    }

    public static bool IsMainEntryVisible
    {
        get
        {
            EnsureDay();
            if (!IsUnlocked)
                return false;
            // 结算本关快照还在时仍显示（含刚打完 7 并领奖）；回主城清快照后隐藏
            if (SettlementHadWin)
                return true;
            return !AllStagesClaimedToday;
        }
    }

    public static bool PendingClaim => Data.pendingClaim;
    public static bool AllStagesClaimedToday => Data.stageIndex >= StageCount;

    public static int StageCount
    {
        get
        {
            var list = ConfigHelper.Tables?.TbPiggyRewardCfg?.DataList;
            return list == null ? 0 : list.Count;
        }
    }

    public static cfg.Game.PiggyRewardCfg CurrentCfg
    {
        get
        {
            var list = ConfigHelper.Tables?.TbPiggyRewardCfg?.DataList;
            if (list == null || list.Count == 0)
                return null;
            int idx = Mathf.Clamp(Data.stageIndex, 0, list.Count - 1);
            if (Data.stageIndex >= list.Count)
                return list[list.Count - 1];
            return list[idx];
        }
    }

    public static int CurrentStageLevel => CurrentCfg != null ? CurrentCfg.Level : 3;
    public static int CurrentProgress => Data.progress;
    public static int CurrentTarget => CurrentCfg != null ? CurrentCfg.Level : 3;

    public static int DisplayStageLevel
    {
        get
        {
            if (SettlementHadWin && SettlementStageLevel > 0)
                return SettlementStageLevel;
            return CurrentStageLevel;
        }
    }

    public static int DisplayProgress
    {
        get
        {
            if (SettlementHadWin)
                return SettlementProgress;
            if (AllStagesClaimedToday)
                return CurrentTarget;
            return CurrentProgress;
        }
    }

    /// <summary>本地 0 点（GM 改时后的游戏本地时间）换日则清档。返回 true 表示这次真的换日了。</summary>
    public static bool EnsureDay()
    {
        SelfCheckOnce();
        string key = SaveKey();
        if (data == null || loadedSaveKey != key)
        {
            loadedSaveKey = key;
            data = LoadData(key);
        }

        string today = TodayId();
        if (data.dayId == today)
            return false;

        data.dayId = today;
        data.stageIndex = 0;
        data.progress = 0;
        data.pendingClaim = false;
        ShowFailTips = false;
        FailLostProgress = 0;
        FailLostStageLevel = 0;
        ClearSettlementSnapshot();
        Save();
        return true;
    }

    public static void ClearSettlementSnapshot()
    {
        SettlementHadWin = false;
        SettlementStageLevel = 0;
        SettlementProgress = 0;
    }

    public static void RecordWin()
    {
        if (!CanRecordWin)
            return;

        EnsureDay();
        if (data.pendingClaim || AllStagesClaimedToday)
        {
            // 7/7 领完后的下一关结算不再沿用上一条快照。
            if (AllStagesClaimedToday)
                ClearSettlementSnapshot();
            return;
        }

        var cfg = CurrentCfg;
        if (cfg == null || cfg.Level <= 0)
            return;

        int target = cfg.Level;
        bool firstWinToday = data.activityRoundDayId != data.dayId;
        if (firstWinToday)
        {
            data.activityStartCount++;
            data.activityRoundDayId = data.dayId;
        }

        string uniqueId = ActivityUniqueId();

        data.progress = Mathf.Min(data.progress + 1, target);
        SettlementHadWin = true;
        SettlementStageLevel = target;
        SettlementProgress = data.progress;

        if (data.progress >= target)
            data.pendingClaim = true;

        Save();
        // 当天第一次：先 0 再 3。攒满再打 1。
        if (firstWinToday)
            GameBAData.ExcutePiggyActivity(0, target, 0, uniqueId);
        GameBAData.ExcutePiggyActivity(3, target, data.progress, uniqueId);
        if (data.pendingClaim)
            GameBAData.ExcutePiggyActivity(1, target, data.progress, uniqueId);
    }

    public static void RecordFail()
    {
        ShowFailTips = false;
        FailLostProgress = 0;
        FailLostStageLevel = 0;
        if (!IsUnlocked)
            return;

        EnsureDay();
        if (data.pendingClaim || AllStagesClaimedToday)
            return;

        if (data.progress == 0)
            return;

        int lost = data.progress;
        int target = CurrentStageLevel;
        string uniqueId = ActivityUniqueId();
        FailLostProgress = lost;
        FailLostStageLevel = target;
        data.progress = 0;
        ShowFailTips = true;
        ClearSettlementSnapshot();
        Save();
        GameBAData.ExcutePiggyActivity(1, target, lost, uniqueId);
    }

    /// <summary>发奖并推进阶段。返回本次金币数（item id 4）。</summary>
    public static int Claim()
    {
        EnsureDay();
        if (!data.pendingClaim)
            return 0;

        var cfg = CurrentCfg;
        int target = cfg != null ? cfg.Level : 0;
        int progress = data.progress;
        int coins = ParseCoinReward(cfg != null ? cfg.Rewards : null);
        string uniqueId = ActivityUniqueId();
        data.pendingClaim = false;
        data.progress = 0;
        data.stageIndex = Mathf.Min(data.stageIndex + 1, StageCount);
        Save();
        GameBAData.ExcutePiggyActivity(2, target, progress, uniqueId, coins);
        return coins;
    }

    static string ActivityUniqueId()
    {
        int n = data != null ? data.activityStartCount : 0;
        return n > 0 ? n.ToString() : "";
    }

    public static bool TryParseReward(string rewards, out int itemId, out int count)
    {
        itemId = 0;
        count = 0;
        if (string.IsNullOrWhiteSpace(rewards))
            return false;

        string trimmed = rewards.Trim();
        char sep = trimmed.Contains("_") ? '_' : ',';
        string[] parts = trimmed.Split(sep);
        if (parts.Length != 2)
            return false;
        return int.TryParse(parts[0], out itemId) && int.TryParse(parts[1], out count);
    }

    public static int ParseCoinReward(string rewards)
    {
        if (!TryParseReward(rewards, out int itemId, out int count))
            return 0;
        return itemId == 4 ? count : count;
    }

    public static int PendingRewardCoins()
    {
        if (!PendingClaim)
            return 0;
        return DisplayRewardCoins();
    }

    public static int DisplayRewardCoins()
    {
        var cfg = CurrentCfg;
        return ParseCoinReward(cfg != null ? cfg.Rewards : null);
    }

    public static bool IsStageCompleted(int stageLevel)
    {
        EnsureDay();
        var list = ConfigHelper.Tables?.TbPiggyRewardCfg?.DataList;
        if (list == null)
            return false;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Level != stageLevel)
                continue;
            return data.stageIndex > i;
        }
        return false;
    }

    public static bool IsCurrentStage(int stageLevel)
    {
        if (AllStagesClaimedToday)
            return false;
        return CurrentStageLevel == stageLevel;
    }

    public static DateTime NowLocal => GameData.GetGameLocalDateTime();

    public static TimeSpan TimeUntilNextDay()
    {
        DateTime now = NowLocal;
        TimeSpan left = now.Date.AddDays(1) - now;
        return left.TotalSeconds < 0 ? TimeSpan.Zero : left;
    }

    static string TodayId() => NowLocal.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    static bool selfChecked;

    static void SelfCheckOnce()
    {
        if (selfChecked)
            return;
        selfChecked = true;
        var almost = new DateTime(2026, 10, 23, 23, 59, 59);
        DateTime next = almost.Date.AddDays(1);
        Debug.Assert(almost.ToString("yyyyMMdd", CultureInfo.InvariantCulture) == "20261023");
        Debug.Assert(next.ToString("yyyyMMdd", CultureInfo.InvariantCulture) == "20261024");
        Debug.Assert((next - almost).TotalSeconds < 2);
    }

    static PiggyBankSaveData LoadData(string key)
    {
        string json = PlayerPrefs.GetString(key, "");
        var loaded = string.IsNullOrEmpty(json)
            ? new PiggyBankSaveData()
            : JsonUtility.FromJson<PiggyBankSaveData>(json);
        if (loaded == null)
            loaded = new PiggyBankSaveData();
        return loaded;
    }

    static string SaveKey()
    {
        string uid = GPUserDataManager.HasInit() && GPUserDataManager.Instance.UserData != null
            ? GPUserDataManager.Instance.GetUID()
            : "guest";
        return SaveKeyPrefix + uid;
    }

    static void Save()
    {
        if (data == null)
            return;
        string key = loadedSaveKey ?? SaveKey();
        PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        GPUserDataManager.Instance?.SaveData();
        Changed?.Invoke();
    }

    /// <summary>GM：进度/目标。目标可以是连胜 3、5、7，也可以是阶段 1、2、3。例如 5/5、2/2、7/3。</summary>
    public static bool GmSetStage(int progress, int stageOrLevel)
    {
        var list = ConfigHelper.Tables?.TbPiggyRewardCfg?.DataList;
        if (list == null)
            return false;

        int idx = -1;
        int target = 0;
        bool matchedLevel = false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Level != stageOrLevel)
                continue;
            idx = i;
            target = list[i].Level;
            matchedLevel = true;
            break;
        }

        if (idx < 0)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Stage != stageOrLevel)
                    continue;
                idx = i;
                target = list[i].Level;
                break;
            }
        }

        if (idx < 0)
            return false;

        // 2/2 这种是在选第 2 档，不是进度 2。直接攒满，方便进领奖界面看动画。
        if (!matchedLevel && progress <= list[idx].Stage)
            progress = target;

        if (progress < 0 || progress > target)
            return false;

        var save = Data;
        save.stageIndex = idx;
        save.progress = progress;
        save.pendingClaim = progress >= target;
        ClearSettlementSnapshot();
        Save();
        return true;
    }

    public static string GmProgressText()
    {
        var save = Data;
        if (save.stageIndex >= StageCount && StageCount > 0)
        {
            int last = CurrentTarget;
            return last + "/" + last;
        }

        return CurrentProgress + "/" + CurrentStageLevel;
    }

    public static void ResetAll()
    {
        ClearSettlementSnapshot();
        string key = loadedSaveKey ?? SaveKey();
        loadedSaveKey = key;
        data = new PiggyBankSaveData { dayId = TodayId() };
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
