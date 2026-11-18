using System;
using FishFramework;
using UnityEngine;

public class GoldTreeActData : BaseData
{
    /// <summary>黄金树棋子总数（一关固定 6 个）。</summary>
    public const int GoldenTreeTileCount = 6;

    /// <summary>非全 x2 关卡时，随机 x2 棋子数量的上限（含）。例如设为 3 则随机 0~3，最大不超过黄金树棋子数。</summary>
    public const int RandomX2MaxCount = 3;

    const string ActivityStartReportedPrefsPrefix = "ba_activity_start_board_";
    const string RankChangeSeenBoardPrefsPrefix = "goldtree_rank_change_seen_board_";

    long mEndTime = 0;
    int mReportedEndBoardId = -1;
    Action mSettlementChainNext;

    /// <summary>黄金树 / 段位活动是否启用（硬开关见 GameSettings.goldTreeActEnabled）。</summary>
    public bool IsActEnabled() => ActivityBuildFlags.GoldTreeActEnabled;


    public long GetActEndTime()
    {
        return mEndTime;
    }

    public override void OnReset()
    {
        mReportedEndBoardId = -1;
    }

    public override void OnRemove()
    {

    }
    public override void OnInit()
    {

    }

    //根据排名获得奖励
    public cfg.Game.GoldTreeRankAwardCfg GetAwardCfgByRank(int rank)
    {
        var list = ConfigHelper.Tables.TbGoldTreeRankAwardCfg.DataList;
        foreach (var item in list)
        {
            var rankList = item.Rank;
            if (rankList.Count == 1)
            {
                if (rank == rankList[0])
                {
                    return item;
                }
            }
            else
            {
                int lowRank = rankList[0];
                int highRank = rankList[1];
                if (rank >= lowRank && rank <= highRank)
                {
                    return item;
                }
            }
        }
        return null;
    }

    public bool NeedsBoardSettlement()
    {
        if (!IsActEnabled()) return false;
        if (GameData.Rank.HasPendingLastSeasonSettlement())
        {
            return true;
        }

        int boardId = GPUserDataManager.GPUserData.goldenTreeBoardId;
        return boardId >= 0 && boardId < GameData.Rank.GetCurrentBoardId();
    }

    public bool GetBActOpen()
    {
        if (!IsActEnabled()) return false;
        if (NeedsBoardSettlement()) return false;

        if (GameData.UnlockData.GetGoldTreeUnlock())
        {
            var userData = GPUserDataManager.Instance.UserData;
            return !GetBNoInActivityTime()
                && userData.goldenTreeActOpen
                && userData.goldenTreeBoardId == GameData.Rank.GetCurrentBoardId();
        }
        return false;
    }

    /// <summary>账号生命周期内仅弹一次 UIGoldTreeFirstTips（赛季结算清数据不重置；仅 GM ResetData / 删档重装可再弹）。</summary>
    public bool CheckShowFirstTips()
    {
        if (!IsActEnabled()) return false;
        if (!GameData.BFromStartScene) return false;
        if (GPUserDataManager.GPUserData.goldenTreeFirstTipsShown) return false;
        if (!GetBActOpen()) return false;

        return true;
    }

    public void MarkFirstTipsShown()
    {
        GPUserDataManager.GPUserData.goldenTreeFirstTipsShown = true;
        GPUserDataManager.Instance.SaveData();
    }

    public bool GetBShowActBtn()
    {
        if (!IsActEnabled()) return false;

        if (GameData.UnlockData.GetGoldTreeUnlock())
        {
            if (NeedsBoardSettlement())
            {
                return true;
            }

            var userData = GPUserDataManager.Instance.UserData;
            if (userData.goldenTreeActOpen
                && userData.goldenTreeBoardId == GameData.Rank.GetCurrentBoardId())
            {
                return true;
            }

            if (GetTodayWinCnt() >= 3 && !GetBNoInActivityTime())
            {
                return true;
            }

        }
        return false;
    }


    //以下时间基于 GameData.GetServerTime()，日界均为 UTC。
    //每日 UTC 04:00 开启新一日并结束当日玩法倒计时；UTC 00:00–04:00 不可参与（可领奖）。

    public long GetEndTime()
    {
        long nowServerSeconds = GameData.GetServerTime();
        var nowUtc = DateTimeOffset.FromUnixTimeSeconds(nowServerSeconds).UtcDateTime;

        var todayUtcOpen = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 4, 0, 0, DateTimeKind.Utc);
        DateTime endUtc = nowUtc < todayUtcOpen ? todayUtcOpen : todayUtcOpen.AddDays(1);

        mEndTime = new DateTimeOffset(endUtc).ToUnixTimeSeconds();
        return mEndTime;
    }

    //下次开启时间：若当前时间在今日 UTC 04:00 前则返回今日 04:00，否则返回次日 04:00
    public long GetNextOpenTime()
    {
        long nowServerSeconds = GameData.GetServerTime();
        var nowUtc = DateTimeOffset.FromUnixTimeSeconds(nowServerSeconds).UtcDateTime;

        var todayUtcOpen = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 4, 0, 0, DateTimeKind.Utc);
        DateTime nextOpenUtc = nowUtc < todayUtcOpen ? todayUtcOpen : todayUtcOpen.AddDays(1);
        return new DateTimeOffset(nextOpenUtc).ToUnixTimeSeconds();
    }

    // UTC 00:00–04:00 不在活动时间内，但是可以领奖
    public bool GetBNoInActivityTime()
    {
        if (!IsActEnabled()) return true;

        long nowServerSeconds = GameData.GetServerTime();
        var nowUtc = DateTimeOffset.FromUnixTimeSeconds(nowServerSeconds).UtcDateTime;

        var todayUtcMidnight = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var todayUtcOpen = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 4, 0, 0, DateTimeKind.Utc);

        return nowUtc >= todayUtcMidnight && nowUtc < todayUtcOpen;
    }


    //增加今日胜利次数
    public void IncreaseTodayWinCnt()
    {
        if (!IsActEnabled()) return;
        if (!GameData.GoldTreeActData.GetBActOpen())
        {
            GPUserDataManager.GPUserData.goldenTreeTodayWinCnt++;
        }
    }

    //清除今日胜利次数
    public void ClearTodayWinCnt()
    {
        GPUserDataManager.GPUserData.goldenTreeTodayWinCnt = 0;
        GPUserDataManager.GPUserData.goldenTreeTodayWinBoardId = -1;
    }

    public int GetTodayWinCnt()
    {
        var userData = GPUserDataManager.GPUserData;
        if (userData.goldenTreeTodayWinBoardId != GameData.Rank.GetCurrentBoardId())
        {
            return 0;
        }

        return userData.goldenTreeTodayWinCnt;
    }

    public void AddTodayWinCnt()
    {
        if (!IsActEnabled()) return;

        int currentBoardId = GameData.Rank.GetCurrentBoardId();
        var userData = GPUserDataManager.GPUserData;
        if (userData.goldenTreeTodayWinBoardId != currentBoardId)
        {
            userData.goldenTreeTodayWinCnt = 0;
            userData.goldenTreeTodayWinBoardId = currentBoardId;
        }

        userData.goldenTreeTodayWinCnt++;
    }

    public int GetActWinCnt()
    {
        return GPUserDataManager.GPUserData.goldenTreeActWinCnt;
    }

    public void IncreaseActWinCnt()
    {
        if (!IsActEnabled()) return;
        if (!GetBActOpen()) return;
        GPUserDataManager.GPUserData.goldenTreeActWinCnt++;
    }

    public int RollGoldenTreeX2Count(int levelIndex)
    {
        if (GetBGoldenTreeAllX2Level(levelIndex))
        {
            return GoldenTreeTileCount;
        }

        int maxCount = Math.Min(RandomX2MaxCount, GoldenTreeTileCount);
        return UnityEngine.Random.Range(0, maxCount + 1);
    }

    /// <summary>活动前三胜窗口内、该关本周期未失败时，进关 6 棋子全 x2。</summary>
    public bool GetBGoldenTreeAllX2Level(int levelIndex)
    {
        if (!GameData.BFromStartScene) return false;
        if (!GetBActOpen()) return false;
        if (GetActWinCnt() >= 3) return false;
        return !GPUserDataManager.Instance.IsGoldenTreeLevelFailed(levelIndex);
    }

    public void MarkGoldenTreeLevelFailed(int levelIndex)
    {
        if (!IsActEnabled()) return;
        if (!GetBActOpen()) return;
        GPUserDataManager.Instance.MarkGoldenTreeLevelFailed(levelIndex);
    }

    /// <summary>主城 Play 旁 goldtreeIcon：与进关全 x2 判定一致。</summary>
    public bool GetBShowMainMenuGoldTreeIcon(int levelIndex)
    {
        return GetBGoldenTreeAllX2Level(levelIndex);
    }

    //是否可领取奖励（含主动触发与主城自动弹出）
    public bool CheckCanShowAwardDlg()
    {
        return TryShowSettlementAwardDlg();
    }

    /// <summary>待赛季结算领奖（不打开界面）。胜利页自动弹开启窗时用来跳过排位开启。</summary>
    public bool HasPendingSettlementAward()
    {
        if (!IsActEnabled()) return false;
        if (!GameData.BFromStartScene) return false;
        if (IsSettlementDlgShowing()) return true;
        if (GameData.Rank != null && GameData.Rank.HasPendingLastSeasonSettlement()) return true;
        return NeedsBoardSettlement();
    }

    public bool TryShowSettlementAwardDlg(Action onFlowClosed = null)
    {
        if (!IsActEnabled()) return false;
        if (!GameData.BFromStartScene) return false;

        if (IsSettlementDlgShowing())
        {
            RememberSettlementChain(onFlowClosed);
            return true;
        }

        if (GameData.Rank.HasPendingLastSeasonSettlement() && GameData.Rank.PrepareLastSeasonSettlementBoard())
        {
            ShowActivityResult();
            RememberSettlementChain(onFlowClosed);
            return true;
        }

        if (GPUserDataManager.GPUserData.goldenTreeBoardId == -1
            && !GameData.Rank.HasPendingLastSeasonSettlement())
        {
            return false;
        }

        if (!GameData.UnlockData.GetGoldTreeUnlock())
        {
            return false;
        }

        if (NeedsBoardSettlement())
        {
            GameData.Rank.PrepareLastSeasonSettlementBoard();
            ShowActivityResult();
            RememberSettlementChain(onFlowClosed);
            return true;
        }

        return false;
    }

    void RememberSettlementChain(Action onFlowClosed)
    {
        if (onFlowClosed != null)
            mSettlementChainNext = onFlowClosed;
    }

    /// <summary>结算领奖弹窗和排行页都关掉后，继续主城后续弹窗。</summary>
    public void NotifySettlementFlowClosed()
    {
        if (mSettlementChainNext == null)
            return;
        if (GameModule.UI != null &&
            (GameModule.UI.ExistPanel<UIGoldTreeResult>() || GameModule.UI.ExistPanel<UIGoldTreeLeagueEnd>()))
            return;

        var rank = UIRankView.Instance;
        if (rank != null && rank.IsSettlementFlowActive)
            return;

        var next = mSettlementChainNext;
        mSettlementChainNext = null;
        next.Invoke();
    }

    bool IsSettlementDlgShowing()
    {
        if (GameModule.UI.ExistPanel<UIGoldTreeResult>()
            || GameModule.UI.ExistPanel<UIGoldTreeLeagueEnd>())
        {
            return true;
        }

        var rankView = UIRankView.Instance;
        return rankView != null && rankView.IsSettlementFlowActive;
    }

    public void ClearAllActivityData()//清除所有活动数据
    {
        GPUserDataManager.GPUserData.ClearAllGoldTreeData();
        ClearTodayWinCnt();
    }

    /// <summary>同一 board 只允许报一次 status=0。成功标记则应立刻上报。</summary>
    public bool TryMarkActivityStartReported()
    {
        var userData = GPUserDataManager.GPUserData;
        if (userData == null)
            return false;

        int boardId = userData.goldenTreeBoardId;
        if (boardId < 0)
            return false;

        string uid = GPUserDataManager.Instance != null ? GPUserDataManager.Instance.GetUID() : "";
        string prefsKey = ActivityStartReportedPrefsPrefix + uid;
        if (PlayerPrefs.GetInt(prefsKey, -1) == boardId)
            return false;

        PlayerPrefs.SetInt(prefsKey, boardId);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>
    /// 本轮日榜第一次打开排名变化不可跳过，同一 board 之后可以。
    /// 新的一天活动（新 board）视为新一轮，重新从第一次算。
    /// </summary>
    public bool AllowRankChangeAnimSkip()
    {
        var userData = GPUserDataManager.GPUserData;
        int boardId = userData != null && userData.goldenTreeBoardId >= 0
            ? userData.goldenTreeBoardId
            : (GameData.Rank != null ? GameData.Rank.GetCurrentBoardId() : -1);
        if (boardId < 0)
            return false;

        string uid = GPUserDataManager.Instance != null ? GPUserDataManager.Instance.GetUID() : "";
        string prefsKey = RankChangeSeenBoardPrefsPrefix + uid;
        bool seen = PlayerPrefs.GetInt(prefsKey, -1) == boardId;
        if (!seen)
        {
            PlayerPrefs.SetInt(prefsKey, boardId);
            PlayerPrefs.Save();
        }

        return seen;
    }

    public long GetCountdownRemaining()
    {
        var userData = GPUserDataManager.GPUserData;
        if (userData == null || userData.goldenTreeBoardId < 0 || GameData.Rank == null)
            return -1;

        if (GameData.Rank.GetCurrentBoardId() > userData.goldenTreeBoardId)
            return 0;

        long remaining = GetEndTime() - GameData.GetServerTime();
        return remaining < 0 ? 0 : remaining;
    }

    public string GetActivityUniqueId(bool upcoming = false)
    {
        var userData = GPUserDataManager.GPUserData;
        if (userData == null)
            return "";

        int openCnt = userData.goldenTreeOpenCnt;
        if (openCnt <= 0 && userData.goldenTreeBoardId >= 0)
            openCnt = 1;
        if (upcoming)
            openCnt = openCnt <= 0 ? 1 : openCnt + 1;
        return openCnt > 0 ? openCnt.ToString() : "";
    }

    public bool TryConsumeActivityEndReport(int boardId)
    {
        if (boardId < 0 || mReportedEndBoardId == boardId)
            return false;

        mReportedEndBoardId = boardId;
        return true;
    }

    public bool TryReportActivityCountdownEnded(long prevRemaining, long remaining)
    {
        if (remaining < 0 || prevRemaining <= 0 || remaining > 0)
            return false;

        var userData = GPUserDataManager.GPUserData;
        if (userData == null || !TryConsumeActivityEndReport(userData.goldenTreeBoardId))
            return false;

        GameBAData.ExcuteActivity(1);
        return true;
    }

    void ShowActivityResult()
    {
        GameData.Rank.PrepareLastSeasonSettlementBoard();

        UIRankView rankview = UIRankView.Instance;
        if (rankview == null || !GameModule.UI.ExistPanel<UIRankView>())
        {
            UIRankView.Instance = null;
            rankview = GameModule.UI.OpenPanel<UIRankView>();
        }

        rankview.ShowGetResultAward();
    }
}