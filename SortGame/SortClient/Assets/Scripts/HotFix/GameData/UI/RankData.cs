using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FishFramework;

public class RankData : BaseData
{
    [Serializable]
    private class RobotScoreSnapshotItem
    {
        public int id;
        public long score;
        public string tier;
    }

    [Serializable]
    private class RobotScoreSnapshotData
    {
        public List<RobotScoreSnapshotItem> items = new();
    }

    [Serializable]
    public class OneUserData
    {
        public string playerName;
        public string playerId;
        public string pictureUrl = "1";
        public long score;
        public long rank;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static OneUserData FromJson(string json)
        {
            return JsonUtility.FromJson<OneUserData>(json);
        }

        public OneUserData(OneUserData cloneData)
        {
            if (cloneData == null)
                return;
            playerName = cloneData.playerName;
            playerId = cloneData.playerId;
            pictureUrl = cloneData.pictureUrl;
            score = cloneData.score;
            rank = cloneData.rank;
        }

        public OneUserData()
        {
        }
    }

    /// <summary>排行榜总人数上限（49 机器人 + 1 玩家）。</summary>
    const int RankListMax = 50;
    const int RobotCountMax = RankListMax - 1;

    [Serializable]
    public class RankBoardData
    {
        [SerializeField]
        public List<OneUserData> boardlist;

        public static RankBoardData FromJson(string json)
        {
            return JsonUtility.FromJson<RankBoardData>(json);
        }
    }

    private List<OneUserData> rankShowList = new();
    private List<OneUserData> settlementShowList = new();
    private List<OneUserData> robotRankData = new();
    private Dictionary<int, string> robotTierById = new();
    private string lastRobotSettlementKey = string.Empty;
    private bool useSettlementBoard = false;

    private int CompareRankOrder(OneUserData a, OneUserData b)
    {
        int scoreCmp = -a.score.CompareTo(b.score);
        if (scoreCmp != 0)
        {
            return scoreCmp;
        }

        string myUid = GPUserDataManager.GPUserData.uid;
        bool aIsMe = !string.IsNullOrEmpty(myUid) && a.playerId.Equals(myUid);
        bool bIsMe = !string.IsNullOrEmpty(myUid) && b.playerId.Equals(myUid);
        if (aIsMe != bIsMe)
        {
            return aIsMe ? -1 : 1;
        }

        return string.CompareOrdinal(a.playerId, b.playerId);
    }

    public override void OnReset()
    {
    }

    public override void OnRemove()
    {
    }

    public override void OnInit()
    {
        rankShowList.Clear();
        settlementShowList.Clear();
    }

    private bool HasPendingLastSeasonBoard()
    {
        var userData = GPUserDataManager.GPUserData;
        return userData.goldenTreeLastSeasonPendingReward &&
               !string.IsNullOrEmpty(userData.goldenTreeLastSeasonBoardJson);
    }

    private bool TryLoadPendingLastSeasonBoard()
    {
        if (!HasPendingLastSeasonBoard())
        {
            return false;
        }

        var userData = GPUserDataManager.GPUserData;
        RankBoardData data = null;
        try
        {
            data = RankBoardData.FromJson(userData.goldenTreeLastSeasonBoardJson);
        }
        catch
        {
            data = null;
        }

        if (data?.boardlist == null || data.boardlist.Count == 0)
        {
            return false;
        }

        settlementShowList = data.boardlist
            .Select(v => new OneUserData(v))
            .ToList();
        settlementShowList.Sort(CompareRankOrder);
        if (settlementShowList.Count > RankListMax)
        {
            settlementShowList.RemoveRange(RankListMax, settlementShowList.Count - RankListMax);
        }

        for (int i = 0; i < settlementShowList.Count; i++)
        {
            settlementShowList[i].rank = i + 1;
        }
        return true;
    }

    private bool ShouldTakeSeasonSnapshot()
    {
        var userData = GPUserDataManager.GPUserData;
        if (userData.goldenTreeBoardId < 0)
        {
            return false;
        }

        return userData.goldenTreeBoardId < GetCurrentBoardId();
    }

    private void SaveLastSeasonSnapshotIfNeeded()
    {
        if (!ShouldTakeSeasonSnapshot())
        {
            return;
        }

        var userData = GPUserDataManager.GPUserData;
        if (HasPendingLastSeasonBoard() && userData.goldenTreeLastSeasonBoardId == userData.goldenTreeBoardId)
        {
            return;
        }

        var snapshotList = GetRankList();
        if (snapshotList == null || snapshotList.Count == 0)
        {
            return;
        }

        RankBoardData snapshot = new()
        {
            boardlist = snapshotList.Select(v => new OneUserData(v)).ToList()
        };

        userData.goldenTreeLastSeasonBoardId = userData.goldenTreeBoardId;
        userData.goldenTreeLastSeasonBoardJson = JsonUtility.ToJson(snapshot);
        userData.goldenTreeLastSeasonSnapshotLeagueTier =
            GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(userData.goldenTreeLeagueTier);
        userData.goldenTreeLastSeasonPendingReward = true;

        GPUserDataManager.Instance.SaveData();
    }

    public bool PrepareLastSeasonSettlementBoard()
    {
        SaveLastSeasonSnapshotIfNeeded();
        useSettlementBoard = TryLoadPendingLastSeasonBoard();
        if (useSettlementBoard)
            CacheSettlementDisplayTiersFromLoadedBoard();
        return useSettlementBoard;
    }

    void CacheSettlementDisplayTiersFromLoadedBoard()
    {
        if (!TryGetMySettlementRankData(out var myData) || myData.rank < 1)
            return;

        int boardId = GPUserDataManager.GPUserData.goldenTreeLastSeasonBoardId;
        if (GoldTreeLeagueRankFlagHelper.TryCacheSettlementDisplayTiers((int)myData.rank, boardId))
            GPUserDataManager.Instance.SaveData();
    }

    /// <summary>
    /// 领取上赛季结算奖励时，根据 CSVGoldTreeLeagueRank 配置结算升/保/降段并写入玩家段位。
    /// </summary>
    public void ApplyTierSettlementOnClaim(int playerRank, int settlementBoardId)
    {
        if (playerRank < 1 || playerRank > RankListMax)
            return;

        if (settlementBoardId < 0)
            settlementBoardId = GPUserDataManager.GPUserData.goldenTreeLastSeasonBoardId;
        if (settlementBoardId < 0)
            return;

        GoldTreeLeagueRankFlagHelper.ApplyDailySettlement(playerRank, settlementBoardId);
        GPUserDataManager.Instance.SaveData();
    }

    public bool HasPendingLastSeasonSettlement()
    {
        return HasPendingLastSeasonBoard();
    }

    public void ExitSettlementBoard()
    {
        useSettlementBoard = false;
    }

    public void OnSettlementClaimedAndClosed()
    {
        var userData = GPUserDataManager.GPUserData;
        userData.goldenTreeLastSeasonPendingReward = false;
        userData.goldenTreeLastSeasonBoardId = -1;
        userData.goldenTreeLastSeasonBoardJson = "";
        userData.goldenTreeLastSeasonSnapshotLeagueTier = 0;
        settlementShowList.Clear();
        useSettlementBoard = false;
        GPUserDataManager.Instance.SaveData();
    }

    void MakeRobotCfg()
    {
        robotRankData.Clear();
        robotTierById.Clear();
        int goldenTreeBoardId = GPUserDataManager.GPUserData.goldenTreeBoardId;
        if (goldenTreeBoardId >= 1)
        {
            var nameList = ConfigHelper.Tables.TbCSVRobotNameCfg.DataList;
            var nameIdList = FishFramework.Utility.GetRandomSequence(nameList.Count, goldenTreeBoardId);
            var list = ConfigHelper.Tables.TbGoldTreeRobotCfg.DataList;
            var tierAssignments = GoldTreeRobotGrowthHelper.BuildRobotTierAssignments(
                GPUserDataManager.GPUserData.goldenTreeLeagueTier,
                goldenTreeBoardId);

            int index = 0;
            foreach (var item in list)
            {
                if (index >= RobotCountMax)
                {
                    break;
                }

                var nameIdx = nameIdList[index] - 1;

                var oneUser = new OneUserData();
                oneUser.playerId = item.Id.ToString();
                oneUser.rank = item.Rank;
                oneUser.score = GoldTreeRobotGrowthHelper.CalcInitialRobotScore(
                    item.Score, item.StartJitter, goldenTreeBoardId * 10000 + item.Id);
                if (nameList.Count > nameIdx)
                {
                    oneUser.playerName = nameList[nameIdx].PlayerName;
                }
                else
                {
                    oneUser.playerName = "error";
                }

                oneUser.pictureUrl = item.PictureUrl;
                robotRankData.Add(oneUser);

                if (index < tierAssignments.Count)
                {
                    robotTierById[item.Id] = tierAssignments[index];
                }

                index++;
            }

            ApplyRobotScoreSnapshotOrInit(goldenTreeBoardId);
        }
    }

    public OneUserData GetMyRankData(int playerScoreOverride = -1)
    {
        if (useSettlementBoard && TryGetMySettlementRankData(out var settlementMyData))
            return settlementMyData;

        MakeRankShowList(playerScoreOverride);
        return rankShowList.Find((v) => v.playerId.Equals(GPUserDataManager.Instance.GetUID()));
    }

    bool TryGetMySettlementRankData(out OneUserData myData)
    {
        myData = null;
        if (settlementShowList == null || settlementShowList.Count == 0)
            return false;

        myData = settlementShowList.Find((v) => v.playerId.Equals(GPUserDataManager.Instance.GetUID()));
        return myData != null;
    }

    public bool TryGetPendingLastSeasonMyRank(out OneUserData myData)
    {
        myData = null;
        var userData = GPUserDataManager.GPUserData;
        if (!userData.goldenTreeLastSeasonPendingReward
            || string.IsNullOrEmpty(userData.goldenTreeLastSeasonBoardJson))
        {
            return false;
        }

        if (TryGetMySettlementRankData(out myData))
            return true;

        RankBoardData data = null;
        try
        {
            data = RankBoardData.FromJson(userData.goldenTreeLastSeasonBoardJson);
        }
        catch
        {
            data = null;
        }

        if (data?.boardlist == null || data.boardlist.Count == 0)
            return false;

        var list = data.boardlist
            .Select(v => new OneUserData(v))
            .ToList();
        list.Sort(CompareRankOrder);
        if (list.Count > RankListMax)
            list.RemoveRange(RankListMax, list.Count - RankListMax);

        for (int i = 0; i < list.Count; i++)
            list[i].rank = i + 1;

        myData = list.Find(v => v.playerId.Equals(GPUserDataManager.Instance.GetUID()));
        return myData != null;
    }

    private OneUserData GetMyMakeRankData(int playerScore)
    {
        GPUserData userData = GPUserDataManager.Instance.UserData;
        return new OneUserData
        {
            playerId = userData.uid,
            pictureUrl = userData.userhead.ToString(),
            playerName = userData.username,
            rank = 13,
            score = playerScore
        };
    }

    static int ResolvePlayerScore(int playerScoreOverride)
    {
        return playerScoreOverride >= 0
            ? playerScoreOverride
            : GPUserDataManager.GPUserData.goldenTreeActPoint;
    }

    /// <summary>活动开启后首次上榜：机器人用配置表初始分/名次，不走涨分公式。</summary>
    static bool IsFirstActivityRanking()
    {
        return GPUserDataManager.GPUserData.goldenTreeActWinCnt <= 1;
    }

    /// <summary>仅推进结算时钟，不套用涨分公式；后续自然时间从该时刻起算。</summary>
    void AnchorRobotSettlementClock()
    {
        GPUserDataManager.GPUserData.goldenTreeRobotLastSettleTime = GameData.GetServerTime();
        GPUserDataManager.Instance.SaveData();
    }

    OneUserData BuildMyRankData(int playerScore)
    {
        var myData = GetMyMakeRankData(playerScore);
        myData.score = playerScore;
        myData.playerName = GPUserDataManager.GPUserData.username;
        myData.pictureUrl = GPUserDataManager.GPUserData.userhead.ToString();
        return myData;
    }

    void FinalizeRankList()
    {
        rankShowList.Sort(CompareRankOrder);

        if (rankShowList.Count > RankListMax)
        {
            rankShowList.RemoveRange(RankListMax, rankShowList.Count - RankListMax);
        }

        for (int i = 0; i < rankShowList.Count; i++)
        {
            rankShowList[i].rank = i + 1;
        }
    }

    void MakeFirstRankShowList(int playerScore)
    {
        MakeRobotCfg();

        if (playerScore <= 0)
        {
            BuildFixedConfigRankList(BuildMyRankData(playerScore));
            return;
        }

        rankShowList.Clear();
        rankShowList.AddRange(robotRankData);
        rankShowList.Add(BuildMyRankData(playerScore));
        FinalizeRankList();
    }

    void BuildFixedConfigRankList(OneUserData myData)
    {
        int playerSlotRank = GPUserDataManager.GPUserData.goldenTreeLastRank;
        if (playerSlotRank <= 0)
        {
            playerSlotRank = RankListMax - 3;
        }

        var robotByCfgRank = new Dictionary<int, OneUserData>();
        foreach (var robot in robotRankData)
        {
            if (!int.TryParse(robot.playerId, out int robotId))
            {
                continue;
            }

            var cfg = ConfigHelper.Tables.TbGoldTreeRobotCfg.GetOrDefault(robotId);
            if (cfg != null)
            {
                robotByCfgRank[cfg.Rank] = robot;
            }
        }

        rankShowList.Clear();
        for (int rank = 1; rank <= RankListMax; rank++)
        {
            if (rank == playerSlotRank)
            {
                myData.rank = rank;
                rankShowList.Add(myData);
            }
            else if (robotByCfgRank.TryGetValue(rank, out var robot))
            {
                robot.rank = rank;
                rankShowList.Add(robot);
            }
        }
    }

    void MakeRankShowList(int playerScoreOverride = -1)
    {
        int playerScore = ResolvePlayerScore(playerScoreOverride);
        if (IsFirstActivityRanking())
        {
            MakeFirstRankShowList(playerScore);
            return;
        }

        TryApplyRobotIdleGrowth();
        MakeRobotCfg();
        rankShowList.Clear();
        rankShowList.AddRange(robotRankData);
        rankShowList.Add(BuildMyRankData(playerScore));
        FinalizeRankList();
    }

    public List<OneUserData> GetRankList(int playerScoreOverride = -1)
    {
        if (useSettlementBoard && settlementShowList != null && settlementShowList.Count > 0)
        {
            return settlementShowList.Take(RankListMax).Select(v => new OneUserData(v)).ToList();
        }

        MakeRankShowList(playerScoreOverride);
        return rankShowList.Take(RankListMax).ToList();
    }

    public void ClampLocalActPoint()
    {
        var userData = GPUserDataManager.GPUserData;
        if (userData.goldenTreeActPoint > 1500)
        {
            userData.goldenTreeActPoint = 150 + UnityEngine.Random.Range(0, 10);
        }
    }

    private long sinceTimeStamp = 1759708800; //2025/10/06
    private int mLeaderBoardId = -1;

    public void SetBoardId()
    {
        var sinceUtc = System.DateTimeOffset.FromUnixTimeSeconds(sinceTimeStamp).UtcDateTime;
        var nowUtc = System.DateTimeOffset.FromUnixTimeSeconds(GameData.GetServerTime()).UtcDateTime;

        var dayBoundaryOffset = System.TimeSpan.FromHours(4);
        var effectiveSinceDate = (sinceUtc - dayBoundaryOffset).Date;
        var effectiveNowDate = (nowUtc - dayBoundaryOffset).Date;

        int daysSince = (int)(effectiveNowDate - effectiveSinceDate).TotalDays;
        if (daysSince < 0)
        {
            daysSince = 0;
        }

        int startBoardCursor = 0;
#if UNITY_ANDROID
        startBoardCursor = 120000;
#else
        startBoardCursor = 220000;
#endif

        mLeaderBoardId = daysSince + startBoardCursor;
    }

    public int GetCurrentBoardId()
    {
        SetBoardId();
        return mLeaderBoardId;
    }

    public void SaveCurrentBoard()
    {
        SaveLastSeasonSnapshotIfNeeded();
        var userData = GPUserDataManager.GPUserData;
        if (userData.goldenTreeLeagueTier < GoldTreeRobotRankRewardHelper.MinTierId)
            userData.goldenTreeLeagueTier = GoldTreeRobotRankRewardHelper.MinTierId;

        if (userData.goldenTreeOpenCnt <= 0)
            userData.goldenTreeOpenCnt = userData.goldenTreeBoardId >= 0 ? 1 : 0;
        userData.goldenTreeOpenCnt++;

        userData.goldenTreeBoardId = GetCurrentBoardId();
        userData.goldenTreeActPoint = 0;
        userData.goldenTreeActWinCnt = 0;
        userData.goldenTreeActOpen = true;
        userData.ClearGoldenTreeFailedLevels();
        userData.goldenTreeLastRank = RankListMax - 3;
        userData.goldenTreeRobotScoreBoardId = -1;
        userData.goldenTreeRobotScoreJson = "";
        userData.goldenTreeRobotLastSettleTime = GoldTreeRobotGrowthHelper.GetActivityDayStartUnix();
        lastRobotSettlementKey = string.Empty;

        Debug.Log("gold tree board id = " + GPUserDataManager.GPUserData.goldenTreeBoardId);
    }

    public void ApplyRobotSettlementGrowthIfNeeded(bool isWin, int levelIndex, int playerGain = 0)
    {
        if (!GameData.BFromStartScene)
        {
            return;
        }

        var userData = GPUserDataManager.GPUserData;
        int boardId = userData.goldenTreeBoardId;
        if (boardId < 0)
        {
            return;
        }

        string settleType = isWin ? "win" : "fail";
        string settlementKey = $"{boardId}_{levelIndex}_{settleType}";
        if (settlementKey.Equals(lastRobotSettlementKey))
        {
            return;
        }

        if (IsFirstActivityRanking())
        {
            AnchorRobotSettlementClock();
            lastRobotSettlementKey = settlementKey;
            return;
        }

        MakeRobotCfg();
        if (robotRankData.Count == 0)
        {
            return;
        }

        if (!isWin)
        {
            playerGain = 0;
        }

        if (ApplyRobotSettlementGrowth(playerGain))
        {
            SaveRobotScoreSnapshot(boardId);
            GPUserDataManager.Instance.SaveData();
            lastRobotSettlementKey = settlementKey;
        }
    }

    public void TryApplyRobotIdleGrowth()
    {
        if (IsFirstActivityRanking())
        {
            return;
        }

        if (!CanApplyRobotGrowth())
        {
            return;
        }

        MakeRobotCfg();
        if (robotRankData.Count == 0)
        {
            return;
        }

        if (ApplyRobotSettlementGrowth(0))
        {
            SaveRobotScoreSnapshot(GPUserDataManager.GPUserData.goldenTreeBoardId);
            GPUserDataManager.Instance.SaveData();
        }
    }

    bool CanApplyRobotGrowth()
    {
        if (!GameData.BFromStartScene)
        {
            return false;
        }

        if (useSettlementBoard)
        {
            return false;
        }

        var userData = GPUserDataManager.GPUserData;
        int boardId = userData.goldenTreeBoardId;
        if (boardId < 0 || boardId != GetCurrentBoardId())
        {
            return false;
        }

        if (GameData.GoldTreeActData != null && GameData.GoldTreeActData.NeedsBoardSettlement())
        {
            return false;
        }

        return true;
    }

    bool ApplyRobotSettlementGrowth(int playerGain)
    {
        var userData = GPUserDataManager.GPUserData;
        long nowUnix = GameData.GetServerTime();
        long lastSettleUnix = userData.goldenTreeRobotLastSettleTime;
        if (lastSettleUnix <= 0)
        {
            lastSettleUnix = GoldTreeRobotGrowthHelper.GetActivityDayStartUnix();
        }

        // 自然时间：自上次结算至本次结算的分钟数，再代入 idleRate 等公式项。
        int timePassedMinutes = GoldTreeRobotGrowthHelper.GetTimePassedMinutes(lastSettleUnix, nowUnix);
        if (timePassedMinutes <= 0 && playerGain <= 0)
        {
            return false;
        }

        var sortedRobots = robotRankData
            .OrderByDescending(v => v.score)
            .ThenBy(v => v.playerId)
            .ToList();

        for (int i = 0; i < sortedRobots.Count; i++)
        {
            if (!int.TryParse(sortedRobots[i].playerId, out int robotId))
            {
                continue;
            }

            string robotTier = robotTierById.TryGetValue(robotId, out string tier) ? tier : "R1";
            int robotRank = i + 1;
            int gain = GoldTreeRobotGrowthHelper.CalcRobotGain(playerGain, robotRank, robotTier, timePassedMinutes);
            if (gain > 0)
            {
                sortedRobots[i].score += gain;
            }
        }

        userData.goldenTreeRobotLastSettleTime = nowUnix;
        return true;
    }

    private void ApplyRobotScoreSnapshotOrInit(int boardId)
    {
        var userData = GPUserDataManager.GPUserData;
        if (userData.goldenTreeRobotScoreBoardId != boardId || string.IsNullOrEmpty(userData.goldenTreeRobotScoreJson))
        {
            SaveRobotScoreSnapshot(boardId);
            return;
        }

        RobotScoreSnapshotData snapshot;
        try
        {
            snapshot = JsonUtility.FromJson<RobotScoreSnapshotData>(userData.goldenTreeRobotScoreJson);
        }
        catch
        {
            SaveRobotScoreSnapshot(boardId);
            return;
        }

        if (snapshot?.items == null || snapshot.items.Count == 0)
        {
            SaveRobotScoreSnapshot(boardId);
            return;
        }

        Dictionary<int, long> scoreMap = new();
        Dictionary<int, string> tierMap = new();
        foreach (var item in snapshot.items)
        {
            scoreMap[item.id] = item.score;
            if (!string.IsNullOrEmpty(item.tier))
            {
                tierMap[item.id] = item.tier;
            }
        }

        bool missingTier = false;
        foreach (var robot in robotRankData)
        {
            if (!int.TryParse(robot.playerId, out int robotId))
            {
                continue;
            }

            if (scoreMap.TryGetValue(robotId, out long score))
            {
                robot.score = score;
            }

            if (tierMap.TryGetValue(robotId, out string tier))
            {
                robotTierById[robotId] = tier;
            }
            else
            {
                missingTier = true;
            }
        }

        if (missingTier)
        {
            SaveRobotScoreSnapshot(boardId);
        }
    }

    private void SaveRobotScoreSnapshot(int boardId)
    {
        var userData = GPUserDataManager.GPUserData;
        userData.goldenTreeRobotScoreBoardId = boardId;

        RobotScoreSnapshotData snapshot = new();
        foreach (var robot in robotRankData)
        {
            if (!int.TryParse(robot.playerId, out int robotId))
            {
                continue;
            }

            snapshot.items.Add(new RobotScoreSnapshotItem
            {
                id = robotId,
                score = robot.score,
                tier = robotTierById.TryGetValue(robotId, out string tier) ? tier : string.Empty
            });
        }

        userData.goldenTreeRobotScoreJson = JsonUtility.ToJson(snapshot);
    }

    public void InvalidateRobotRankCacheForLeagueTierChange()
    {
        robotRankData.Clear();
        robotTierById.Clear();
        rankShowList.Clear();
        lastRobotSettlementKey = string.Empty;
    }
}
