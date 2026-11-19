using System;
using FishFramework;
using UnityEngine;

public enum PuppyAdventureStartMode
{
    Ready,
    Cooldown,
    DailyClosed,
    Running
}

public enum PuppyAdventurePresentation
{
    None,
    Current,
    Advance,
    Complete,
    Fail
}

public enum PuppyAdventurePopupReason
{
    Manual,
    LifetimeUnlock,
    StagePromotion,
    CooldownFinished,
    Nag
}

[Serializable]
public sealed class PuppyAdventureStageConfig
{
    public int stage;
    public int winTarget;
    public int prizePool;
    public int nextStage;
    public int isMax;
    public int failCooldownMin;
    public int failDropCount;
    public int dropToStage;
    public string startDesKey;
}

[Serializable]
public sealed class PuppyAdventureEliminateConfig
{
    public int stage;
    public int progress;
    public int minRemain;
    public int maxRemain;
}

[Serializable]
public sealed class PuppyAdventureConfig
{
    public bool enabled = true;
    public int unlockLevel = 50;
    public int startStage = 5;
    public int playerCount = 100;
    public int dailyWinCap = 2;
    public int dayResetHourUtc = 4;
    public int challengeTimeMin = 120;
    public int nagIntervalLevels = 5;
    public int nagMaxPerDay = 2;
    public int consecutiveXMute = 2;
    public int startPopupDailyCap;
    public string periodType = "Permanent";
    public PuppyAdventureStageConfig[] stages;
    public PuppyAdventureEliminateConfig[] eliminate;
}

[Serializable]
public sealed class PuppyAdventureSaveData
{
    public int currentStage = 5;
    public int curWin;
    public int remainPlayers = 100;
    public int[] robotIcons;
    public long cooldownEndTime;
    public long challengeExpireTime;
    public int dailyWinCount;
    public string dailyWinDayId = "";
    public int consecutiveFailThisStage;
    public int startCount;
    public int finalLowPopulationMisses;
    public bool hasShownLifetimeStartPopup;
    public string popupDate = "";
    public int nagCountToday;
    public int winsSinceLastPopupX;
    public bool pendingNag;
    public int consecutiveX;
    public bool autoPopupMuted;
    public bool pendingStageStartPopup;
    public bool pendingCooldownPopup;
    public bool challengeStarted;
    public int lastRewardCoins;
    public int lastRewardPlayers;
    public bool lastRewardPromoted;
    public int lastChallengeStage;
    public int lastChallengeTarget;
    public int lastChallengePrizePool;
    public int lastFailureWin;
    /// <summary>最高阶（isMax）通关后为 true，主城入口隐藏；活动日切后清零再显示。</summary>
    public bool allStagesCompleted;
}

/// <summary>
/// 狗狗大冒险正式状态。保留旧类名，避免已生成 Prefab 和热更引用失效。
/// 规则来自 Luban 表 TbPuppyAdventure*。存档按账号写入 PlayerPrefs。
/// </summary>
public static class PuppyAdventureDemoState
{
    const string SaveKeyPrefix = "PuppyAdventure_v1_";

    static PuppyAdventureConfig config;
    static PuppyAdventureSaveData data;
    static string loadedSaveKey;
    static PuppyAdventurePresentation presentation;
    static PuppyAdventurePopupReason popupReason;
    static bool timeoutHandledThisFrame;
    static int animFromWin;
    static int animToWin;
    static int animFromPlayers;
    static int animToPlayers;

    public static event Action Changed;

    public static PuppyAdventureConfig Config => config ??= LoadConfig();
    public static PuppyAdventureSaveData Data
    {
        get
        {
            string key = SaveKey();
            if (data == null || loadedSaveKey != key)
            {
                loadedSaveKey = key;
                data = LoadData(key);
            }
            return data;
        }
    }
    public static int Stage => Data.currentStage;
    public static int CurWin => Data.curWin;
    public static int RemainPlayers => Data.remainPlayers;
    public static int WinTarget => StageCfg.winTarget;
    public static int PrizePool => StageCfg.prizePool;
    public static int DisplayWinTarget => (presentation == PuppyAdventurePresentation.Complete
        || presentation == PuppyAdventurePresentation.Fail)
        && Data.lastChallengeTarget > 0 ? Data.lastChallengeTarget : WinTarget;
    /// <summary>局内细条：正在打的下一关，{curWin+1}/{winTarget}。</summary>
    public static int HudWin
    {
        get
        {
            int target = WinTarget;
            if (target <= 0)
                return 0;
            return Mathf.Min(CurWin + 1, target);
        }
    }
    public static int DisplayPrizePool => (presentation == PuppyAdventurePresentation.Complete
        || presentation == PuppyAdventurePresentation.Fail)
        && Data.lastChallengePrizePool > 0 ? Data.lastChallengePrizePool : PrizePool;
    public static bool IsRunning => IsEnabled && Data.challengeStarted;
    public static bool IsEnabled => Config.enabled && ActivityBuildFlags.PuppyAdventureEnabled;
    public static bool IsUnlocked
    {
        get
        {
#if UNITY_EDITOR
            if (BypassUnlockForEditor)
                return IsEnabled;
#endif
            return IsEnabled
                && GameData.BFromStartScene
                && GPUserDataManager.Instance != null
                && GPUserDataManager.Instance.GetLevelNum() + 1 >= Config.unlockLevel + 1;
        }
    }

    /// <summary>主城入口：解锁且当日未通关最高阶（日切后可再显）。</summary>
    public static bool IsMainEntryVisible
    {
        get
        {
            RefreshTimeState();
            return IsUnlocked && !Data.allStagesCompleted;
        }
    }

    public static PuppyAdventurePresentation Presentation => presentation;
    public static PuppyAdventurePopupReason PopupReason => popupReason;
    public static int AnimFromWin => animFromWin;
    public static int AnimToWin => animToWin;
    public static int AnimFromPlayers => animFromPlayers;
    public static int AnimToPlayers => animToPlayers;

    public static PuppyAdventureStageConfig StageCfg
    {
        get
        {
            PuppyAdventureStageConfig[] stages = Config.stages;
            if (stages != null)
            {
                for (int i = 0; i < stages.Length; i++)
                    if (stages[i].stage == Data.currentStage)
                        return stages[i];
            }

            if (stages != null && stages.Length > 0)
                return stages[0];

            return new PuppyAdventureStageConfig
            {
                stage = Data.currentStage,
                startDesKey = ""
            };
        }
    }

    public static PuppyAdventureStartMode StartMode
    {
        get
        {
            RefreshTimeState();
            if (Data.challengeStarted) return PuppyAdventureStartMode.Running;
            if (Data.dailyWinCount >= Config.dailyWinCap) return PuppyAdventureStartMode.DailyClosed;
            if (Data.cooldownEndTime > Now) return PuppyAdventureStartMode.Cooldown;
            return PuppyAdventureStartMode.Ready;
        }
    }

    public static long Now => GameData.GetServerTime();
    public static long ChallengeSecondsLeft => Math.Max(0, Data.challengeExpireTime - Now);
    public static long CooldownSecondsLeft => Math.Max(0, Data.cooldownEndTime - Now);
    public static long DailyClosedSecondsLeft => Math.Max(0, NextResetTime() - Now);

    /// <summary>unique_id：累计点 Start 次数；upcoming=true 时为下一次将开始的序号。</summary>
    public static string GetActivityUniqueId(bool upcoming = false)
    {
        int n = Data.startCount;
        if (upcoming)
            n = n <= 0 ? 1 : n + 1;
        return n > 0 ? n.ToString() : "";
    }

    public static bool StartChallenge()
    {
        RefreshTimeState();
        if (!IsUnlocked || StartMode != PuppyAdventureStartMode.Ready)
            return false;

        Data.challengeStarted = true;
        Data.curWin = 0;
        Data.remainPlayers = Config.playerCount;
        Data.robotIcons = BuildVisualRobotIcons();
        Data.challengeExpireTime = Now + Config.challengeTimeMin * 60L;
        Data.startCount++;
        Data.hasShownLifetimeStartPopup = true;
        Data.pendingStageStartPopup = false;
        Data.pendingCooldownPopup = false;
        Data.pendingNag = false;
        Data.winsSinceLastPopupX = 0;
        Data.consecutiveX = 0;
        Data.autoPopupMuted = false;
        popupReason = PuppyAdventurePopupReason.Manual;
        Save();
        // activity status=0：点 Start 开启
        GameBAData.ExcutePuppyActivity(0, Stage, Data.curWin, GetActivityUniqueId());
        return true;
    }

    public static bool RecordLevelWin()
    {
        RefreshTimeState();
        if (IsUnlocked && Data.pendingNag)
        {
            Data.winsSinceLastPopupX++;
            Save();
        }
        if (!IsRunning)
            return false;

        int fromWin = Data.curWin;
        int fromPlayers = Data.remainPlayers;
        Data.curWin = Mathf.Min(Data.curWin + 1, WinTarget);
        Data.remainPlayers = RollRemainPlayers(Data.curWin);
        animFromWin = fromWin;
        animToWin = Data.curWin;
        animFromPlayers = fromPlayers;
        animToPlayers = Data.remainPlayers;
        presentation = Data.curWin >= WinTarget
            ? PuppyAdventurePresentation.Complete
            : PuppyAdventurePresentation.Advance;

        // activity status=3：每次前进
        GameBAData.ExcutePuppyActivity(3, Stage, Data.curWin, GetActivityUniqueId());

        if (Data.curWin >= WinTarget)
            CompleteChallenge();
        else
            Save();
        return true;
    }

    public static bool RecordGiveUp()
    {
        RefreshTimeState();
        if (!IsRunning)
            return false;

        animFromWin = Data.curWin;
        animToWin = Data.curWin;
        animFromPlayers = Data.remainPlayers;
        animToPlayers = Data.remainPlayers;
        presentation = PuppyAdventurePresentation.Fail;
        FailChallenge();
        return true;
    }

    /// <summary>
    /// 主页进度：Current 用已连胜 curWin；Advance/Complete 从上一格播到已入账的新进度。
    /// 动画结束必须落到 toWin，不能停在 fromWin。
    /// </summary>
    public static void ResolveHomeProgress(out int fromWin, out int toWin)
    {
        if (presentation == PuppyAdventurePresentation.Fail)
        {
            fromWin = toWin = Data.lastFailureWin;
            return;
        }

        if (presentation == PuppyAdventurePresentation.Advance
            || presentation == PuppyAdventurePresentation.Complete)
        {
            toWin = Mathf.Max(animToWin, CurWin);
            fromWin = animFromWin;
            if (animToWin == 0 && toWin > 1)
                fromWin = toWin - 1;
            if (fromWin > toWin)
                fromWin = Mathf.Max(0, toWin - 1);
            return;
        }

        fromWin = toWin = CurWin;
    }

    public static void ResolveHomePlayers(out int fromPlayers, out int toPlayers)
    {
        toPlayers = animToPlayers > 0 ? animToPlayers : RemainPlayers;
        fromPlayers = animFromPlayers > 0 ? animFromPlayers : toPlayers;
    }

    /// <summary>
    /// 路点映射：progress 直接对应激活子节点下标。0=start，N=第 N 格。
    /// nodeCount 应为当前激活节点数（五步 6，七步 8）。
    /// </summary>
    public static int TrailNodeIndex(int progress, int winTarget, int nodeCount)
    {
        if (nodeCount <= 0)
            return -1;
        return Mathf.Clamp(progress, 0, nodeCount - 1);
    }

    /// <summary>
    /// 换了父节点就必须播跳跃；只有已在目标台且偏移≈0 才跳过。
    /// </summary>
    public static bool JumpNeedsTween(Vector2 fromAnchored, bool parentChanged)
    {
        return parentChanged || fromAnchored.sqrMagnitude >= 1f;
    }

    public const string PlayersNestedPath = "PlayersPos/Players";

    /// <summary>
    /// 视觉 robot 槽（不含自己）。满员约 20 人，随逻辑人数递减，第四格附近约 10 人。
    /// </summary>
    public const int VisualRobotSlots = 19;

    static readonly int[] VisualRobotKnots = { 0, 7, 14, 23, 40, 65, 99 };
    static readonly int[] VisualCountKnots = { 0, 7, 8, 9, 12, 16, 19 };

    public static int VisualRobotCount(int remainPlayers)
    {
        int robots = Mathf.Max(0, remainPlayers - 1);
        if (robots <= VisualCountKnots[1])
            return robots;

        for (int i = 1; i < VisualRobotKnots.Length; i++)
        {
            if (robots > VisualRobotKnots[i])
                continue;
            int r0 = VisualRobotKnots[i - 1];
            int r1 = VisualRobotKnots[i];
            int v0 = VisualCountKnots[i - 1];
            int v1 = VisualCountKnots[i];
            if (r1 <= r0)
                return Mathf.Clamp(v1, 0, VisualRobotSlots);
            float t = (robots - r0) / (float)(r1 - r0);
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(v0, v1, t)), 0, VisualRobotSlots);
        }

        return VisualRobotSlots;
    }

    static int[] BuildVisualRobotIcons()
    {
        var icons = new int[VisualRobotSlots];
        for (int i = 0; i < VisualRobotSlots; i++)
            icons[i] = UnityEngine.Random.Range(1, 9);
        return icons;
    }

    public static bool CheckTimeout()
    {
        timeoutHandledThisFrame = false;
        RefreshTimeState();
        return timeoutHandledThisFrame;
    }

    public static void SetPresentation(PuppyAdventurePresentation value)
    {
        presentation = value;
    }

    public static void OpenPopup(PuppyAdventurePopupReason reason)
    {
        popupReason = reason;
        if (reason == PuppyAdventurePopupReason.CooldownFinished)
            Data.pendingCooldownPopup = false;
        Save();
    }

    public static void ClosePopupByX()
    {
        if (popupReason == PuppyAdventurePopupReason.LifetimeUnlock)
        {
            Data.hasShownLifetimeStartPopup = true;
            Data.winsSinceLastPopupX = 0;
            CountAutomaticX();
        }
        else if (popupReason == PuppyAdventurePopupReason.CooldownFinished)
        {
            Data.pendingNag = true;
            Data.winsSinceLastPopupX = 0;
            CountAutomaticX();
        }
        else if (popupReason == PuppyAdventurePopupReason.Nag)
        {
            Data.pendingNag = true;
            Data.winsSinceLastPopupX = 0;
            Data.nagCountToday++;
            CountAutomaticX();
        }
        else if (popupReason == PuppyAdventurePopupReason.StagePromotion)
        {
            Data.pendingStageStartPopup = false;
            Data.pendingNag = true;
            Data.winsSinceLastPopupX = 0;
        }
        Save();
    }

    public static PuppyAdventurePopupReason GetPendingAutoPopup()
    {
        RefreshTimeState();
        if (!IsUnlocked || Data.allStagesCompleted || Data.challengeStarted || Data.autoPopupMuted)
            return PuppyAdventurePopupReason.Manual;
        if (!Data.hasShownLifetimeStartPopup)
            return PuppyAdventurePopupReason.LifetimeUnlock;
        // 升阶弹窗不受 DailyClosed 挡住，否则当日第 2 次通关升阶后永远不弹 7 阶 Start
        if (Data.pendingStageStartPopup)
            return PuppyAdventurePopupReason.StagePromotion;
        if (StartMode != PuppyAdventureStartMode.Ready)
            return PuppyAdventurePopupReason.Manual;
        if (Data.pendingCooldownPopup)
            return PuppyAdventurePopupReason.CooldownFinished;
        if (Data.pendingNag && Data.winsSinceLastPopupX >= Config.nagIntervalLevels
            && Data.nagCountToday < Config.nagMaxPerDay)
            return PuppyAdventurePopupReason.Nag;
        return PuppyAdventurePopupReason.Manual;
    }

    public static bool HasPendingAutoPopup()
    {
        return GetPendingAutoPopup() != PuppyAdventurePopupReason.Manual;
    }

    public static void MarkPopupShown(PuppyAdventurePopupReason reason)
    {
        popupReason = reason;
        if (reason == PuppyAdventurePopupReason.CooldownFinished)
            Data.pendingCooldownPopup = false;
        if (reason == PuppyAdventurePopupReason.Nag)
            Data.winsSinceLastPopupX = 0;
        Save();
    }

    public static void DebugPreviewWin()
    {
        int target = Mathf.Max(1, DisplayWinTarget);
        int fromWin = presentation == PuppyAdventurePresentation.Advance
            || presentation == PuppyAdventurePresentation.Complete
            ? Mathf.Max(animToWin, Data.curWin)
            : Data.curWin;
        if (fromWin >= target)
            fromWin = Mathf.Max(0, target - 1);

        int fromPlayers = Data.remainPlayers > 0 ? Data.remainPlayers : Config.playerCount;
        int toWin = Mathf.Min(fromWin + 1, target);
        int toPlayers = RollRemainPlayers(toWin);

        animFromWin = fromWin;
        animToWin = toWin;
        animFromPlayers = fromPlayers;
        animToPlayers = toPlayers;
        Data.curWin = toWin;
        Data.remainPlayers = toPlayers;
        presentation = toWin >= target
            ? PuppyAdventurePresentation.Complete
            : PuppyAdventurePresentation.Advance;
        Save();
    }

    public static void DebugPreviewFail()
    {
        int progress = presentation == PuppyAdventurePresentation.Fail
            ? Data.lastFailureWin
            : Data.curWin;
        int players = Data.remainPlayers > 0 ? Data.remainPlayers : Config.playerCount;
        Data.lastFailureWin = progress;
        animFromWin = progress;
        animToWin = progress;
        animFromPlayers = players;
        animToPlayers = players;
        presentation = PuppyAdventurePresentation.Fail;
    }

    public static void DebugResetToStart()
    {
        Data.curWin = 0;
        Data.remainPlayers = Config.playerCount;
        Data.challengeStarted = true;
        Data.challengeExpireTime = Now + Mathf.Max(1, Config.challengeTimeMin) * 60L;
        Data.cooldownEndTime = 0;
        presentation = PuppyAdventurePresentation.None;
        animFromWin = 0;
        animToWin = 0;
        animFromPlayers = 0;
        animToPlayers = 0;
        Save();
    }

    public static void ResetRun()
    {
        Data.curWin = 0;
        Data.remainPlayers = Config.playerCount;
        Save();
    }

    public static void PromoteIfNeeded()
    {
        // 领奖入账时已经升阶段；保留兼容调用。
    }

    /// <summary>GM ResetData / 删档：清进度、挑战与冷却倒计时、弹窗标记。</summary>
    public static void ResetAll()
    {
        DebugReset(new PuppyAdventureSaveData());
    }

    /// <summary>GM：按 wins/stage 写入进度并进入挑战中（如 2/5、3/7）。第二段优先按 stage id 匹配，可从高阶降到低阶。</summary>
    public static bool GmSetProgress(int wins, int stageOrTarget)
    {
        PuppyAdventureStageConfig match = FindStageForGm(stageOrTarget);
        if (match == null)
        {
            Debug.LogWarning($"[PuppyAdventure] GmSetProgress: no stage for {stageOrTarget}");
            return false;
        }

        Data.currentStage = match.stage;
        Data.curWin = Mathf.Clamp(wins, 0, match.winTarget);
        Data.challengeStarted = true;
        Data.challengeExpireTime = Now + Mathf.Max(1, Config.challengeTimeMin) * 60L;
        Data.cooldownEndTime = 0;
        Data.dailyWinCount = 0;
        Data.autoPopupMuted = false;
        Data.allStagesCompleted = false;
        Data.hasShownLifetimeStartPopup = true;
        Data.pendingStageStartPopup = false;
        Data.pendingCooldownPopup = false;
        Data.lastChallengeStage = match.stage;
        Data.lastChallengeTarget = match.winTarget;
        Data.lastChallengePrizePool = match.prizePool;
        Data.remainPlayers = Data.curWin <= 0 ? Config.playerCount : RollRemainPlayers(Data.curWin);
        if (Data.robotIcons == null || Data.robotIcons.Length == 0)
            Data.robotIcons = BuildVisualRobotIcons();
        presentation = PuppyAdventurePresentation.None;
        popupReason = PuppyAdventurePopupReason.Manual;
        Save();
        return true;
    }

    static PuppyAdventureStageConfig FindStageForGm(int stageOrTarget)
    {
        PuppyAdventureStageConfig[] stages = Config.stages;
        if (stages == null)
            return null;

        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i] != null && stages[i].stage == stageOrTarget)
                return stages[i];
        }

        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i] != null && stages[i].winTarget == stageOrTarget)
                return stages[i];
        }

        return null;
    }

    public static void DebugReset(PuppyAdventureSaveData value)
    {
        loadedSaveKey = SaveKey();
        data = value ?? new PuppyAdventureSaveData();
        presentation = PuppyAdventurePresentation.None;
        popupReason = PuppyAdventurePopupReason.Manual;
        timeoutHandledThisFrame = false;
        animFromWin = 0;
        animToWin = 0;
        animFromPlayers = 0;
        animToPlayers = 0;
        Save();
    }

#if UNITY_EDITOR
    public static bool BypassUnlockForEditor;

    public static void SetDebugStartMode(PuppyAdventureStartMode mode)
    {
        Data.challengeStarted = mode == PuppyAdventureStartMode.Running;
        Data.challengeExpireTime = mode == PuppyAdventureStartMode.Running ? Now + 7200 : 0;
        Data.cooldownEndTime = mode == PuppyAdventureStartMode.Cooldown ? Now + 1800 : 0;
        Data.dailyWinCount = mode == PuppyAdventureStartMode.DailyClosed ? Config.dailyWinCap : 0;
        Save();
    }

    public static string RunSelfTest()
    {
        var sb = new System.Text.StringBuilder();
        int fail = 0;
        void Check(string name, bool ok)
        {
            sb.Append(ok ? "OK  " : "FAIL ");
            sb.AppendLine(name);
            if (!ok) fail++;
        }

        bool oldBypass = BypassUnlockForEditor;
        BypassUnlockForEditor = true;
        try
        {
            DebugReset(new PuppyAdventureSaveData());
            Check("config stages", Config.stages != null && Config.stages.Length >= 2);
            Check("unlock default stage 5", Stage == 5 && WinTarget == 5 && PrizePool == 700);
            Check("ready before start", StartMode == PuppyAdventureStartMode.Ready);
            Check("popup ① first unlock", GetPendingAutoPopup() == PuppyAdventurePopupReason.LifetimeUnlock);
            Check("start", StartChallenge());
            Check("running after start", IsRunning && StartMode == PuppyAdventureStartMode.Running && CurWin == 0);
            Check("start clears mute", !Data.autoPopupMuted && Data.consecutiveX == 0 && !Data.pendingNag);
            Check("trail 0 is start", TrailNodeIndex(0, 5, 6) == 0);
            Check("trail 0!=1", TrailNodeIndex(0, 5, 6) != TrailNodeIndex(1, 5, 6));
            Check("trail 1 second node", TrailNodeIndex(1, 5, 6) == 1);
            Check("trail 5 last of 5", TrailNodeIndex(5, 5, 6) == 5);
            Check("trail 7 last of 7", TrailNodeIndex(7, 7, 8) == 7);
            Check("jump when parent changed", JumpNeedsTween(Vector2.zero, true));
            Check("no jump when already on dest", !JumpNeedsTween(Vector2.zero, false));
            Check("jump when offset", JumpNeedsTween(new Vector2(40f, 0f), false));
            Check("players nested under PlayersPos", PlayersNestedPath == "PlayersPos/Players");
            Check("visual robots 100", VisualRobotCount(100) == VisualRobotSlots);
            Check("visual robots drop each step",
                VisualRobotCount(100) > VisualRobotCount(70)
                && VisualRobotCount(70) > VisualRobotCount(40)
                && VisualRobotCount(40) > VisualRobotCount(24));
            Check("visual robots pad4", VisualRobotCount(24) >= 8 && VisualRobotCount(24) <= 10);
            Check("start robot icons slots", Data.robotIcons != null && Data.robotIcons.Length == VisualRobotSlots);

            RecordLevelWin();
            Check("first win advance anim", Presentation == PuppyAdventurePresentation.Advance
                && AnimFromWin == 0 && AnimToWin == 1
                && AnimFromPlayers == 100 && AnimToPlayers == RemainPlayers);

            for (int i = 0; i < 3; i++)
                RecordLevelWin();
            Check("4 wins not complete", CurWin == 4 && IsRunning && Stage == 5);
            bool remainDown = RemainPlayers < 100;
            Check("eliminate clamp down", remainDown && RemainPlayers >= 2);

            RecordLevelWin();
            Check("complete last jump fields", Presentation == PuppyAdventurePresentation.Complete
                && AnimFromWin == 4 && AnimToWin == 5);
            Check("stage5 complete promote", !IsRunning && Stage == 7 && Data.dailyWinCount == 1);
            Check("award ceil", Data.lastRewardCoins == Mathf.CeilToInt(700f / Mathf.Max(1, Data.lastRewardPlayers)));
            Check("pending ② after promote", Data.pendingStageStartPopup);
            Data.hasShownLifetimeStartPopup = false;
            Check("popup ①>②", GetPendingAutoPopup() == PuppyAdventurePopupReason.LifetimeUnlock);
            Data.hasShownLifetimeStartPopup = true;
            Check("popup ② next", GetPendingAutoPopup() == PuppyAdventurePopupReason.StagePromotion);

            Check("start stage7", StartChallenge());
            Check("start clears ②", !Data.pendingStageStartPopup);
            for (int i = 0; i < 7; i++)
                RecordLevelWin();
            Check("2nd daily win closes", StartMode == PuppyAdventureStartMode.DailyClosed && Data.dailyWinCount >= 2);
            Check("no auto popup when daily closed", !HasPendingAutoPopup());

            DebugReset(new PuppyAdventureSaveData { currentStage = 7, hasShownLifetimeStartPopup = true });
            Check("start stage7 fail chain", StartChallenge());
            Check("give up fail", RecordGiveUp());
            Check("fail cooldown 30m", Data.cooldownEndTime > Now && CooldownSecondsLeft >= 29 * 60);
            Check("stage7 still after 1 fail", Stage == 7 && Data.consecutiveFailThisStage == 1);
            Check("no popup in cooldown", !HasPendingAutoPopup());

            Data.cooldownEndTime = 0;
            Data.challengeStarted = false;
            Check("start after first fail", StartChallenge());
            RecordGiveUp();
            Check("stage7 two fails drop to 5", Stage == 5 && Data.consecutiveFailThisStage == 0);

            DebugReset(new PuppyAdventureSaveData { hasShownLifetimeStartPopup = true });
            Check("start timeout", StartChallenge());
            Data.challengeExpireTime = Now - 1;
            bool timedOut = CheckTimeout();
            Check("timeout fails without running", timedOut && !IsRunning && Data.cooldownEndTime > Now);

            DebugReset(new PuppyAdventureSaveData { hasShownLifetimeStartPopup = true, pendingCooldownPopup = true, pendingNag = true, winsSinceLastPopupX = 5 });
            Check("priority ③>④", GetPendingAutoPopup() == PuppyAdventurePopupReason.CooldownFinished);
            MarkPopupShown(PuppyAdventurePopupReason.CooldownFinished);
            Data.pendingNag = true;
            Data.winsSinceLastPopupX = 5;
            Check("④ after ③ consumed", GetPendingAutoPopup() == PuppyAdventurePopupReason.Nag);

            popupReason = PuppyAdventurePopupReason.CooldownFinished;
            int xBefore = Data.consecutiveX;
            ClosePopupByX();
            Check("⑤ counts auto ①③④ X", Data.consecutiveX == xBefore + 1);

            popupReason = PuppyAdventurePopupReason.Manual;
            xBefore = Data.consecutiveX;
            ClosePopupByX();
            Check("⑤ ignores manual X", Data.consecutiveX == xBefore);

            popupReason = PuppyAdventurePopupReason.StagePromotion;
            xBefore = Data.consecutiveX;
            ClosePopupByX();
            Check("⑤ ignores ② X", Data.consecutiveX == xBefore);

            Data.consecutiveX = 1;
            CountAutomaticX();
            Check("mute after 2 auto X", Data.autoPopupMuted && Data.consecutiveX >= 2);

            DebugReset(new PuppyAdventureSaveData { currentStage = 7, hasShownLifetimeStartPopup = true });
            Check("stage7 start for hud", StartChallenge());
            Data.curWin = 3;
            Data.remainPlayers = 43;
            Check("hud curWin3 is 4/7", HudWin == 4 && WinTarget == 7);
            int homeFrom;
            int homeTo;
            ResolveHomeProgress(out homeFrom, out homeTo);
            Check("home idle still 3/7", homeFrom == 3 && homeTo == 3 && CurWin == 3);
            Check("record 4th win", RecordLevelWin());
            ResolveHomeProgress(out homeFrom, out homeTo);
            Check("after win anim 3→4", AnimFromWin == 3 && AnimToWin == 4 && CurWin == 4);
            Check("home final 4/7", homeFrom == 3 && homeTo == 4 && WinTarget - CurWin == 3);
            Check("trail 3rd vs 4th", TrailNodeIndex(3, 7, 8) == 3 && TrailNodeIndex(4, 7, 8) == 4);

            DebugReset(new PuppyAdventureSaveData { hasShownLifetimeStartPopup = true });
            Check("stage5 start for hud", StartChallenge());
            Check("hud curWin0 is 1/5", HudWin == 1 && WinTarget == 5);
            Check("stage5 0!=1 XSorts", TrailNodeIndex(0, 5, 6) != TrailNodeIndex(1, 5, 6));
            Check("record first stage5 win", RecordLevelWin());
            ResolveHomeProgress(out homeFrom, out homeTo);
            Check("stage5 first 0→1", homeFrom == 0 && homeTo == 1 && CurWin == 1);

            // GM ResetAll：清挑战/冷却/每日关闭倒计时
            Data.challengeStarted = true;
            Data.challengeExpireTime = Now + 7200;
            Data.cooldownEndTime = Now + 1800;
            Data.dailyWinCount = Config.dailyWinCap;
            ResetAll();
            Check("ResetAll ready", StartMode == PuppyAdventureStartMode.Ready
                && Data.challengeExpireTime == 0
                && Data.cooldownEndTime == 0
                && Data.dailyWinCount == 0
                && !Data.challengeStarted);
        }
        finally
        {
            BypassUnlockForEditor = oldBypass;
        }

        sb.Append("result fail=").Append(fail);
        return sb.ToString();
    }
#endif

    public static string FormatTime(long seconds)
    {
        seconds = Math.Max(0, seconds);
        return $"{seconds / 3600:00}:{seconds % 3600 / 60:00}:{seconds % 60:00}";
    }

    static void CompleteChallenge()
    {
        int oldStage = Data.currentStage;
        int wins = Data.curWin;
        int players = Mathf.Max(1, Data.remainPlayers);
        int award = Mathf.CeilToInt(PrizePool / (float)players);
        string uniqueId = GetActivityUniqueId();

        // 金币在 Reward 点 tapToContinue 飞币时入账，此处只记 lastRewardCoins。
        Data.lastChallengeStage = oldStage;
        Data.lastChallengeTarget = WinTarget;
        Data.lastChallengePrizePool = PrizePool;
        Data.lastRewardCoins = award;
        Data.lastRewardPlayers = players;
        bool finishingMaxStage = StageCfg.isMax != 0;
        Data.lastRewardPromoted = StageCfg.nextStage != oldStage;
        Data.currentStage = StageCfg.nextStage;
        Data.dailyWinCount++;
        Data.consecutiveFailThisStage = 0;
        Data.challengeStarted = false;
        Data.challengeExpireTime = 0;
        Data.cooldownEndTime = 0;
        if (finishingMaxStage)
            Data.allStagesCompleted = true;
        // 升阶必挂起 Start；全阶段完成后不再弹
        Data.pendingStageStartPopup = Data.lastRewardPromoted && !Data.allStagesCompleted;
        Save();
        // activity status=1：整局胜利结算
        GameBAData.ExcutePuppyActivity(1, oldStage, wins, uniqueId);
    }

    /// <summary>领取待发奖励金币：取出并清零 lastRewardCoins，避免重复发奖。</summary>
    public static int TakePendingRewardCoins()
    {
        int coins = Mathf.Max(0, Data.lastRewardCoins);
        if (coins <= 0)
            return 0;
        Data.lastRewardCoins = 0;
        Save();
        return coins;
    }

    static void FailChallenge()
    {
        PuppyAdventureStageConfig stage = StageCfg;
        int failStage = Data.currentStage;
        int failWins = Data.curWin;
        string uniqueId = GetActivityUniqueId();
        Data.lastFailureWin = Data.curWin;
        Data.lastChallengeStage = Data.currentStage;
        Data.lastChallengeTarget = stage.winTarget;
        Data.lastChallengePrizePool = stage.prizePool;
        Data.challengeStarted = false;
        Data.challengeExpireTime = 0;
        Data.curWin = 0;
        Data.remainPlayers = Config.playerCount;
        Data.cooldownEndTime = Now + stage.failCooldownMin * 60L;
        Data.pendingCooldownPopup = false;

        if (stage.failDropCount > 0)
        {
            Data.consecutiveFailThisStage++;
            if (Data.consecutiveFailThisStage >= stage.failDropCount)
            {
                Data.currentStage = stage.dropToStage;
                Data.consecutiveFailThisStage = 0;
            }
        }
        Save();
        // activity status=1：失败/放弃/超时结算
        GameBAData.ExcutePuppyActivity(1, failStage, failWins, uniqueId);
    }

    static void RefreshTimeState()
    {
        PuppyAdventureSaveData save = Data;
        string dayId = DayId(Now);
        bool changed = false;
        if (save.dailyWinDayId != dayId)
        {
            save.dailyWinDayId = dayId;
            save.dailyWinCount = 0;
            save.allStagesCompleted = false;
            changed = true;
        }

        string localDate = DateTime.Now.ToString("yyyy-MM-dd");
        if (save.popupDate != localDate)
        {
            save.popupDate = localDate;
            save.nagCountToday = 0;
            changed = true;
        }

        if (save.cooldownEndTime > 0 && save.cooldownEndTime <= Now)
        {
            save.cooldownEndTime = 0;
            save.pendingCooldownPopup = true;
            changed = true;
        }

        if (save.challengeStarted && save.challengeExpireTime > 0 && save.challengeExpireTime <= Now)
        {
            presentation = PuppyAdventurePresentation.None;
            FailChallenge();
            timeoutHandledThisFrame = true;
            return;
        }

        if (changed)
            Save();
    }

    static int RollRemainPlayers(int progress)
    {
        int currentRobots = Mathf.Max(1, Data.remainPlayers - 1);
        if (progress >= WinTarget)
            return RollFinalPlayers(currentRobots) + 1;

        PuppyAdventureEliminateConfig[] rows = Config.eliminate;
        if (rows != null)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                PuppyAdventureEliminateConfig row = rows[i];
                if (row.stage != Stage || row.progress != progress)
                    continue;
                int rolled = UnityEngine.Random.Range(row.minRemain, row.maxRemain + 1);
                return Mathf.Clamp(rolled, 1, Mathf.Max(1, currentRobots - 1)) + 1;
            }
        }
        return Mathf.Max(2, Data.remainPlayers - 1);
    }

    static int RollFinalPlayers(int currentRobots)
    {
        int min;
        int max;
        if (Data.startCount <= 2)
        {
            min = 2; max = 6;
        }
        else
        {
            int roll = UnityEngine.Random.Range(0, 100);
            if (roll < 10 || Data.finalLowPopulationMisses >= 4) { min = 2; max = 5; }
            else if (roll < 45) { min = 6; max = 9; }
            else { min = 11; max = 12; }
        }
        int result = Mathf.Clamp(UnityEngine.Random.Range(min, max + 1), 1, Mathf.Max(1, currentRobots - 1));
        if (result >= 2 && result <= 5)
            Data.finalLowPopulationMisses = 0;
        else
            Data.finalLowPopulationMisses++;
        return result;
    }

    static void CountAutomaticX()
    {
        Data.pendingNag = true;
        Data.consecutiveX++;
        if (Data.consecutiveX >= Config.consecutiveXMute)
            Data.autoPopupMuted = true;
    }

    static long NextResetTime()
    {
        DateTime now = DateTimeOffset.FromUnixTimeSeconds(Now).UtcDateTime;
        DateTime reset = new(now.Year, now.Month, now.Day, Config.dayResetHourUtc, 0, 0, DateTimeKind.Utc);
        if (now >= reset) reset = reset.AddDays(1);
        return new DateTimeOffset(reset).ToUnixTimeSeconds();
    }

    static string DayId(long unix)
    {
        DateTime utc = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime.AddHours(-Config.dayResetHourUtc);
        return utc.ToString("yyyy-MM-dd");
    }

    static PuppyAdventureConfig LoadConfig()
    {
        PuppyAdventureConfig fromTables = TryLoadFromTables();
        if (fromTables != null)
            return fromTables;

        Debug.LogError("[PuppyAdventure] Luban tables missing (TbPuppyAdventureTimeCfg / TbPuppyAdventureStageCfg). Using safe defaults.");
        return new PuppyAdventureConfig();
    }

    static PuppyAdventureConfig TryLoadFromTables()
    {
        ConfigHelper.EnsureInitialized();
        var tables = ConfigHelper.Tables;
        var timeList = tables?.TbPuppyAdventureTimeCfg?.DataList;
        var stageList = tables?.TbPuppyAdventureStageCfg?.DataList;
        if (timeList == null || timeList.Count == 0 || stageList == null || stageList.Count == 0)
            return null;

        var time = timeList[0];
        var cfg = new PuppyAdventureConfig
        {
            enabled = true,
            unlockLevel = time.UnlockLevel,
            startStage = time.StartStage,
            playerCount = time.PlayerCount,
            dailyWinCap = time.DailyWinCap,
            dayResetHourUtc = time.DayResetHourUtc,
            challengeTimeMin = time.ChallengeTimeMin,
            nagIntervalLevels = time.NagIntervalLevels,
            nagMaxPerDay = time.NagMaxPerDay,
            consecutiveXMute = time.ConsecutiveXMute,
            startPopupDailyCap = time.StartPopupDailyCap,
            periodType = time.PeriodType,
            stages = new PuppyAdventureStageConfig[stageList.Count]
        };

        for (int i = 0; i < stageList.Count; i++)
        {
            var row = stageList[i];
            cfg.stages[i] = new PuppyAdventureStageConfig
            {
                stage = row.Stage,
                winTarget = row.WinTarget,
                prizePool = row.PrizePool,
                nextStage = row.NextStage,
                isMax = row.IsMax,
                failCooldownMin = row.FailCooldownMin,
                failDropCount = row.FailDropCount,
                dropToStage = row.DropToStage,
                startDesKey = row.StartDesKey
            };
        }

        var elimList = tables.TbPuppyAdventureEliminateCfg?.DataList;
        if (elimList != null && elimList.Count > 0)
        {
            cfg.eliminate = new PuppyAdventureEliminateConfig[elimList.Count];
            for (int i = 0; i < elimList.Count; i++)
            {
                var row = elimList[i];
                cfg.eliminate[i] = new PuppyAdventureEliminateConfig
                {
                    stage = row.Stage,
                    progress = row.Progress,
                    minRemain = row.MinRemain,
                    maxRemain = row.MaxRemain
                };
            }
        }

        return cfg;
    }

    static PuppyAdventureSaveData LoadData(string key)
    {
        string json = PlayerPrefs.GetString(key, "");
        PuppyAdventureSaveData loaded = string.IsNullOrEmpty(json)
            ? new PuppyAdventureSaveData()
            : JsonUtility.FromJson<PuppyAdventureSaveData>(json);
        if (loaded == null) loaded = new PuppyAdventureSaveData();
        if (loaded.currentStage != 5 && loaded.currentStage != 7)
            loaded.currentStage = Config.startStage;
        if (loaded.remainPlayers <= 0)
            loaded.remainPlayers = Config.playerCount;
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
        if (data == null) return;
        string key = loadedSaveKey ?? SaveKey();
        PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        GPUserDataManager.Instance?.SaveData();
        Changed?.Invoke();
    }
}
