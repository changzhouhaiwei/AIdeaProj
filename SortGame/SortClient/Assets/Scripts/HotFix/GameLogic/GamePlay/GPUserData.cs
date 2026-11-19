using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using Unity.Mathematics;

[Serializable]
public class AvatarFrameRecord
{
    [SerializeField] public int frameId;
    [SerializeField] public long expireUtcTicks;
}

[Serializable]
public class LevelPlayRecord
{
    [SerializeField] public int levelId;
    [SerializeField] public int playCount;

    public LevelPlayRecord() { }

    public LevelPlayRecord(int levelId, int playCount)
    {
        this.levelId = levelId;
        this.playCount = playCount;
    }
}

[Serializable]
public class TileBaseStyleBag
{
    public List<string> knownNames = new List<string>();
    public List<string> remaining = new List<string>();
    public string lastDrawn = "";
}

[Serializable]
public class LevelStyleRecord
{
    [SerializeField] public int levelNum;
    /// <summary>-1 用关卡主题底板，&gt;=0 为 TileBaseStyle 下标。</summary>
    [SerializeField] public int styleId = -1;
    [SerializeField] public bool pendingReroll;

    public LevelStyleRecord() { }

    public LevelStyleRecord(int levelNum, int styleId, bool pendingReroll)
    {
        this.levelNum = levelNum;
        this.styleId = styleId;
        this.pendingReroll = pendingReroll;
    }
}

[Serializable]
public class LevelTileBaseRecord
{
    [SerializeField] public int levelNum;
    [SerializeField] public int tilebaseId;

    public LevelTileBaseRecord() { }

    public LevelTileBaseRecord(int levelNum, int tilebaseId)
    {
        this.levelNum = levelNum;
        this.tilebaseId = tilebaseId;
    }
}

[Serializable]
public class GPUserData
{
    [SerializeField] private int _levelNum;
    [SerializeField] private int _infinitLevelNum;
    [SerializeField] private int _coin;
    [SerializeField] private int _time;
    [SerializeField] private int _version;

    //局内道具数量
    [SerializeField] private int _inGameItemHintCnt;
    [SerializeField] private int _inGameItemShuffleCnt;
    [SerializeField] private int _inGameItemUndoCnt;
    [SerializeField] private int _inGameItemHighLightCnt;
    [SerializeField] private int _ropeUndoCnt;
    [SerializeField] private int _ropeHintCnt;
    [SerializeField] private int _ropeExtraCnt;
    /// <summary>1 表示绳子道具已经按新档补过。老存档缺字段时是 0，读档后补成各 3。</summary>
    [SerializeField] private int _ropeItemsInited;

    [SerializeField] private int _continusWinCnt;
    [SerializeField] private int _continusFailCnt;
    /// <summary>历史字段，仅读档兼容；难度连胜不再按日清零。</summary>
    [SerializeField] private int _continusWinLocalDate;
    /// <summary>跨日首关降难度上次生效的本地日历日（yyyyMMdd）。</summary>
    [SerializeField] private int _nextDayEaseAppliedLocalDate;
    /// <summary>历史最高本局连击峰值总和。</summary>
    [SerializeField] private int _maxComboCnt;

    [SerializeField] private int _levelPassActivityStage;
    [SerializeField] private float _levelPassActivityPoint;
    [SerializeField] private string _uid;

    //用户信息
    [SerializeField] private string _username;
    [SerializeField] private string _countryCode;
    [SerializeField] private int _userhead;
    [SerializeField] private int _userframe;
    [SerializeField] private List<AvatarFrameRecord> _ownedAvatarFrames;

    /// <summary>
    /// 支付产生的数据
    /// </summary>
    [SerializeField] private bool _bNoAd; //免广告
    [SerializeField] private int _freeReviveUsedCnt; //账号累计已用免费复活次数
    /// <summary>账号免费复活上限。-1=未锁定（首败时按关卡写入）；老存档缺字段时 LoadFromJson 补 5。</summary>
    [SerializeField] private int _maxFreeReviveCount;

    [SerializeField] private int _adSourceType; //0:默认 1:ajoe渠道加大广告播放力度
    /// <summary>插屏 AB V2：历史字段，仅读档兼容；逻辑已切 V3，勿再写入。</summary>
    [SerializeField] private bool _interADAbEligible;
    /// <summary>插屏 AB V2 组名；逻辑已切 V3，勿再写入。</summary>
    [SerializeField] private string _interADTestGroup;
    /// <summary>插屏 AB V2 CD 锚点；逻辑已切 V3，勿再写入。</summary>
    [SerializeField] private long _interADCooldownUnix;
    [SerializeField] private int _interADLoadLevelRecordDate;
    /// <summary>插屏 AB V3：历史字段，仅读档兼容；逻辑已切 V4，勿再写入。</summary>
    [SerializeField] private bool _interADAbEligibleV3;
    /// <summary>插屏 AB V3 组名；逻辑已切 V4，勿再写入。</summary>
    [SerializeField] private string _interADTestGroupV3;
    /// <summary>V3 CD 锚点；逻辑已切 V4，勿再写入。</summary>
    [SerializeField] private long _interADCooldownUnixV3;
    /// <summary>插屏 AB V4 组名 E，空串表示尚未打标（归因未回或激励渠道）。V5 不再用它当正式分组。</summary>
    [SerializeField] private string _interADTestGroupV4;
    /// <summary>插屏 V5 正式分组。空串未分组。incentive，或 facebook_t1 / other_t3。写入后不再改。</summary>
    [SerializeField] private string _interADGroupV5;
    /// <summary>V4 CD 锚点 Unix 秒。记录点 A 与 B 共用。</summary>
    [SerializeField] private long _interADCooldownUnixV4;

    //关卡次数记录

    [SerializeField] private List<LevelPlayRecord> _levelPlayRecord;

    //关卡棋子底板记录（一旦确定不可更改，无视重开）
    [SerializeField] private List<LevelTileBaseRecord> _levelTileBaseRecord;

    //上次会话已知的棋子底板 id 集合（用于检测新增背景）
    [SerializeField] private List<int> _knownTileBaseIds;

    //待优先插入的新增背景 id 队列（逐关消费）
    [SerializeField] private List<int> _pendingPriorityTileBaseIds;

    // 无限关棋子底板记录（关号 = 无限关展示序号 1..N，与有限关独立）
    [SerializeField] private List<LevelTileBaseRecord> _infinitLevelTileBaseRecord;

    // 无限关：上次会话已知的底板 id 集合 / 待优先插入的新增背景队列
    [SerializeField] private List<int> _knownInfinitTileBaseIds;
    [SerializeField] private List<int> _pendingPriorityInfinitTileBaseIds;

    // 经典底板 TileBaseStyle。有限关 / 无限关分开记。
    [SerializeField] private List<LevelStyleRecord> _levelStyleRecord;
    [SerializeField] private List<LevelStyleRecord> _infinitLevelStyleRecord;
    [SerializeField] private TileBaseStyleBag _tileBaseStyleBag;
    [SerializeField] private TileBaseStyleBag _infinitTileBaseStyleBag;

    // 模式切换时一次性排除：有限最后一关→下一无限关 / 无限→扩关后首关有限关
    [SerializeField] private int _tileBaseExcludeIdForNextInfinite = -1;
    [SerializeField] private int _tileBaseExcludeIdForNextFinite = -1;
    [SerializeField] private int _tileBaseExcludeFiniteLevel = -1;

    //轮盘数据存储 隔天五点刷新时间
    [SerializeField] private List<int> _circleAwardCntList;
    [SerializeField] private long _circleAwardLastResetTicks; // 上次重置的UTC Ticks
    [SerializeField] private List<int> _circleDailyRemovedIds;
    [SerializeField] private int _circleGrandMissCnt;
    [SerializeField] private int _circleSecondMissCnt;

    //七日连胜奖励
    [SerializeField] private int _dailyVictoryStreakDay;
    [SerializeField] private int _dailyVictoryTotalWinDays;
    [SerializeField] private long _dailyVictoryLastWinWindowTicks;
    [SerializeField] private int _dailyVictoryLastClaimedStreakDay;
    [SerializeField] private int _dailyVictoryLastClaimedTotalWinDays;
    [SerializeField] private int _dailyVictoryClaimRepairVersion;
    /// <summary>活动重开后保留连胜的一次性补丁版本；升版本可再次对存量用户补偿。</summary>
    [SerializeField] private int _dailyVictoryReopenPatchVersion;
    [SerializeField] private bool _gmForceDailyVictoryActEnabled;
    [SerializeField] private int _dailyVictoryLastLoginLocalDate;
    [SerializeField] private int _dailyVictorySnapshotTotalWinDays;
    [SerializeField] private long _dailyVictorySnapshotLastWinTicks;
    [SerializeField] private bool _dailyVictoryPendingSettlementDlg;
    [SerializeField] private bool _dailyVictoryHadWinYesterdayBeforeTodayFirstWin;

    //黄金树活动
    [SerializeField] private int _goldenTreeActPoint;
    [SerializeField] private int _goldenTreeTodayWinCnt;
    [SerializeField] private int _goldenTreeTodayWinBoardId = -1;
    [SerializeField] private int _goldenTreeActWinCnt;
    [SerializeField] private bool _goldenTreeActOpen;
    [SerializeField] private int _goldenTreeBoardId = -1;
    [SerializeField] private int _goldenTreeLastRank = -1;
    [SerializeField] private string _goldenTreeGroupId = "";
    [SerializeField] private int _goldenTreeOpenCnt;
    [SerializeField] private int _goldenTreeRobotScoreBoardId = -1;
    [SerializeField] private string _goldenTreeRobotScoreJson = "";
    [SerializeField] private long _goldenTreeRobotLastSettleTime;
    [SerializeField] private int _goldenTreeLastSeasonBoardId = -1;
    [SerializeField] private string _goldenTreeLastSeasonBoardJson = "";
    [SerializeField] private int _goldenTreeLastSeasonSnapshotLeagueTier;
    [SerializeField] private bool _goldenTreeLastSeasonPendingReward;
    [SerializeField] private int _goldenTreeSettlementRewardGrantedBoardId = -1;
    [SerializeField] private int _goldenTreeLeagueTier;
    [SerializeField] private int _goldenTreeLastSeasonLeagueTier;
    [SerializeField] private int _goldenTreeSettlementLeagueTier;
    [SerializeField] private int _goldenTreeSettlementTierAppliedBoardId = -1;
    [SerializeField] private bool _goldenTreeFirstTipsShown;
    /// <summary>当天 TipsDlg 打开记录。格式 yyyyMMdd|levelKey:count，跨日整段丢弃。</summary>
    [SerializeField] private string _tipsDlgDailyOpen = "";
    [SerializeField] private List<int> _goldenTreeFailedLevelIds = new();

    public void ClearAllGoldTreeData()
    {
        _goldenTreeActPoint = 0;
        _goldenTreeActWinCnt = 0;
        _goldenTreeActOpen = false;
        _goldenTreeBoardId = -1;
        _goldenTreeGroupId = "";
        _goldenTreeLastRank = -1;
        _goldenTreeRobotScoreBoardId = -1;
        _goldenTreeRobotScoreJson = "";
        _goldenTreeRobotLastSettleTime = 0;
        _goldenTreeLastSeasonBoardId = -1;
        _goldenTreeLastSeasonBoardJson = "";
        _goldenTreeLastSeasonSnapshotLeagueTier = 0;
        _goldenTreeLastSeasonPendingReward = false;
        ClearGoldenTreeFailedLevels();
        // 不重置 _goldenTreeSettlementRewardGrantedBoardId：防止结算弹窗重复打开时二次发奖
        // 不重置 _goldenTreeLeagueTier / _goldenTreeSettlementTierAppliedBoardId：段位跨日榜保留
        // 不重置 _goldenTreeLastSeasonLeagueTier / _goldenTreeSettlementLeagueTier：供联赛结束展示
        // 不重置 _goldenTreeFirstTipsShown：账号生命周期内仅弹一次，仅 GM ResetData / 删档重装可重置
        // 不重置 _goldenTreeOpenCnt：账号内第几次开启活动，跨日结算不清
    }

    public bool IsGoldenTreeLevelFailed(int levelId)
    {
        if (_goldenTreeFailedLevelIds == null || _goldenTreeFailedLevelIds.Count == 0)
            return false;

        for (int i = 0; i < _goldenTreeFailedLevelIds.Count; i++)
        {
            if (_goldenTreeFailedLevelIds[i] == levelId)
                return true;
        }

        return false;
    }

    public void MarkGoldenTreeLevelFailed(int levelId)
    {
        if (_goldenTreeFailedLevelIds == null)
            _goldenTreeFailedLevelIds = new List<int>();

        if (IsGoldenTreeLevelFailed(levelId))
            return;

        _goldenTreeFailedLevelIds.Add(levelId);
        _changedFields.Add(nameof(goldenTreeFailedLevelIds));
    }

    public void ClearGoldenTreeFailedLevels()
    {
        if (_goldenTreeFailedLevelIds == null || _goldenTreeFailedLevelIds.Count == 0)
            return;

        _goldenTreeFailedLevelIds.Clear();
        _changedFields.Add(nameof(goldenTreeFailedLevelIds));
    }

    public void ClearAllDailyVictoryData()
    {
        _dailyVictoryStreakDay = 0;
        _dailyVictoryTotalWinDays = 0;
        _dailyVictoryLastWinWindowTicks = 0;
        _dailyVictoryLastClaimedStreakDay = 0;
        _dailyVictoryLastClaimedTotalWinDays = 0;
        _gmForceDailyVictoryActEnabled = false;
        _dailyVictoryLastLoginLocalDate = 0;
        _dailyVictorySnapshotTotalWinDays = 0;
        _dailyVictorySnapshotLastWinTicks = 0;
        _dailyVictoryPendingSettlementDlg = false;
        _dailyVictoryHadWinYesterdayBeforeTodayFirstWin = false;

        bool shouldClearEquippedFrame = false;
        if (_userframe > 0 && _ownedAvatarFrames != null)
        {
            for (int i = 0; i < _ownedAvatarFrames.Count; i++)
            {
                if (_ownedAvatarFrames[i].frameId == _userframe)
                {
                    shouldClearEquippedFrame = true;
                    break;
                }
            }
        }

        if (_ownedAvatarFrames != null)
        {
            _ownedAvatarFrames.Clear();
        }

        if (shouldClearEquippedFrame)
        {
            _userframe = DefaultAvatarFrameId;
        }
    }

    /// <summary>
    /// 需要一个标志用户首日
    /// 增加一个递增+1方法，递增的时候判定是否隔日了。
    /// 只有在首日递增方法才生效.
    /// 增加获得次数方法
    /// </summary>
    /// 

    [SerializeField] private int _videoPlayCntFirstDay;

    private bool shouldCheckNewUser = false; //1.0.13开始添加

    // 首日窗口起点（本地5点对齐）的UTC Ticks，用于判断是否仍处于“首日”
    [SerializeField] private long _firstDayWindowStartUtcTicks;

    // 首日（本地日历日），格式：yyyyMMdd，例如 20251022 表示 2025-10-22
    [SerializeField] private int _firstDayLocalDate;

    // 变更追踪
    private HashSet<string> _changedFields = new();

    // 属性访问器，自动标记变更
    public int levelNum
    {
        get => _levelNum;
        set
        {
            if (_levelNum != value)
            {
                _levelNum = value;
                _changedFields.Add(nameof(levelNum));
            }
        }
    }

    // 首日视频次数：只读访问器（如果已过首日窗口则返回0）
    public int videoPlayCntFirstDay
    {
        get
        {
            return IsWithinFirstDayWindow() ? _videoPlayCntFirstDay : 0;
        }
    }

    // 首日窗口起点（UTC ticks）公开属性（便于变更追踪和序列化）
    public long firstDayWindowStartUtcTicks
    {
        get => _firstDayWindowStartUtcTicks;
        set
        {
            if (_firstDayWindowStartUtcTicks != value)
            {
                _firstDayWindowStartUtcTicks = value;
                _changedFields.Add(nameof(firstDayWindowStartUtcTicks));
            }
        }
    }

    // 首日（本地日历日）
    public int firstDayLocalDate
    {
        get => _firstDayLocalDate;
        set
        {
            if (_firstDayLocalDate != value)
            {
                _firstDayLocalDate = value;
                _changedFields.Add(nameof(firstDayLocalDate));
            }
        }
    }

    // 无限关卡数属性访问器，自动标记变更
    public int infinitLevelNum
    {
        get => _infinitLevelNum;
        set
        {
            if (_infinitLevelNum != value)
            {
                _infinitLevelNum = value;
                _changedFields.Add(nameof(infinitLevelNum));
            }
        }
    }

    // 免广告标记属性访问器
    public bool noAd
    {
        get => _bNoAd;
        set
        {
            if (_bNoAd != value)
            {
                _bNoAd = value;
                _changedFields.Add(nameof(noAd));
            }
        }
    }

    public int freeReviveUsedCnt
    {
        get => _freeReviveUsedCnt;
        set
        {
            if (_freeReviveUsedCnt != value)
            {
                _freeReviveUsedCnt = value;
                _changedFields.Add(nameof(freeReviveUsedCnt));
            }
        }
    }

    public int maxFreeReviveCount
    {
        get => _maxFreeReviveCount;
        set
        {
            if (_maxFreeReviveCount != value)
            {
                _maxFreeReviveCount = value;
                _changedFields.Add(nameof(maxFreeReviveCount));
            }
        }
    }


    // 属性访问器，自动标记变更
    public string uid
    {
        get => _uid;
        set
        {
            if (_uid != value && value.Length > 0)
            {
                _uid = value;
                _changedFields.Add(nameof(uid));
            }
        }
    }

    // 用户名：未设置时默认返回 "player" + _uid
    public string username
    {
        get => !string.IsNullOrEmpty(_username)
            ? _username
            : "player" + ((_uid ?? string.Empty).Length > (12 - "player".Length)
                ? (_uid ?? string.Empty).Substring((_uid ?? string.Empty).Length - (12 - "player".Length))
                : (_uid ?? string.Empty));
        set
        {
            if (_username != value)
            {
                _username = value;
                _changedFields.Add(nameof(username));
            }
        }
    }

    public string countryCode
    {
        get => _countryCode;
        set
        {
            if (_countryCode != value)
            {
                _countryCode = value;
                _changedFields.Add(nameof(countryCode));
            }
        }
    }

    // 头像：未设置或为0时默认返回1
    public int userhead
    {
        get => _userhead == 0 ? 1 : _userhead;
        set
        {
            if (_userhead != value)
            {
                _userhead = value;
                _changedFields.Add(nameof(userhead));
            }
        }
    }

    /// <summary>默认头像框 frame_1（TbAvatarFrameCfg）</summary>
    public const int DefaultAvatarFrameId = 2;

    // 头像框：未设置或为0时默认返回 frame_1
    public int userframe
    {
        get => _userframe == 0 ? DefaultAvatarFrameId : _userframe;
        set
        {
            if (_userframe != value)
            {
                _userframe = value;
                _changedFields.Add(nameof(userframe));
            }
        }
    }

    public List<AvatarFrameRecord> ownedAvatarFrames
    {
        get
        {
            _ownedAvatarFrames ??= new List<AvatarFrameRecord>();
            return _ownedAvatarFrames;
        }
    }

    public int adsourceType
    {
        get => _adSourceType;
        set
        {
            if (_adSourceType != value)
            {
                _adSourceType = value;
                _changedFields.Add(nameof(adsourceType));
            }
        }
    }

    public bool interADAbEligible
    {
        get => _interADAbEligible;
        set
        {
            if (_interADAbEligible != value)
            {
                _interADAbEligible = value;
                _changedFields.Add(nameof(interADAbEligible));
            }
        }
    }

    /// <summary>插屏 AB V2 组名；逻辑已切 V3，勿再写入。</summary>
    public string interADTestGroup
    {
        get => _interADTestGroup ?? "";
        set
        {
            string next = value ?? "";
            if (_interADTestGroup != next)
            {
                _interADTestGroup = next;
                _changedFields.Add(nameof(interADTestGroup));
            }
        }
    }

    /// <summary>V2 CD 锚点；逻辑已切 V3，勿再写入。</summary>
    public long interADCooldownUnix
    {
        get => _interADCooldownUnix;
        set
        {
            if (_interADCooldownUnix != value)
            {
                _interADCooldownUnix = value;
                _changedFields.Add(nameof(interADCooldownUnix));
            }
        }
    }

    /// <summary>插屏 AB V3 eligible；逻辑已切 V4，勿再写入。</summary>
    public bool interADAbEligibleV3
    {
        get => _interADAbEligibleV3;
        set
        {
            if (_interADAbEligibleV3 != value)
            {
                _interADAbEligibleV3 = value;
                _changedFields.Add(nameof(interADAbEligibleV3));
            }
        }
    }

    /// <summary>插屏 AB V3 组名；逻辑已切 V4，勿再写入。</summary>
    public string interADTestGroupV3
    {
        get => _interADTestGroupV3 ?? "";
        set
        {
            string next = value ?? "";
            if (_interADTestGroupV3 != next)
            {
                _interADTestGroupV3 = next;
                _changedFields.Add(nameof(interADTestGroupV3));
            }
        }
    }

    /// <summary>V3 CD 锚点；逻辑已切 V4，勿再写入。</summary>
    public long interADCooldownUnixV3
    {
        get => _interADCooldownUnixV3;
        set
        {
            if (_interADCooldownUnixV3 != value)
            {
                _interADCooldownUnixV3 = value;
                _changedFields.Add(nameof(interADCooldownUnixV3));
            }
        }
    }

    /// <summary>插屏 AB V4 组名 E，空串表示尚未打标。</summary>
    public string interADTestGroupV4
    {
        get => _interADTestGroupV4 ?? "";
        set
        {
            string next = value ?? "";
            if (_interADTestGroupV4 != next)
            {
                _interADTestGroupV4 = next;
                _changedFields.Add(nameof(interADTestGroupV4));
            }
        }
    }

    /// <summary>插屏 V5 正式分组。空串表示还没分组，广告走 other T3。写入后不再改。</summary>
    public string interADGroupV5
    {
        get => _interADGroupV5 ?? "";
        set
        {
            string next = value ?? "";
            if (_interADGroupV5 != next)
            {
                _interADGroupV5 = next;
                _changedFields.Add(nameof(interADGroupV5));
            }
        }
    }

    /// <summary>V4 CD 锚点 Unix 秒。记录点 A 与 B 共用，后写覆盖。</summary>
    public long interADCooldownUnixV4
    {
        get => _interADCooldownUnixV4;
        set
        {
            if (_interADCooldownUnixV4 != value)
            {
                _interADCooldownUnixV4 = value;
                _changedFields.Add(nameof(interADCooldownUnixV4));
            }
        }
    }

    /// <summary>记录点 A 已写入的本地日历日 yyyyMMdd；0 表示尚未记过。</summary>
    public int interADLoadLevelRecordDate
    {
        get => _interADLoadLevelRecordDate;
        set
        {
            if (_interADLoadLevelRecordDate != value)
            {
                _interADLoadLevelRecordDate = value;
                _changedFields.Add(nameof(interADLoadLevelRecordDate));
            }
        }
    }

    public int coin
    {
        get => _coin;
        set
        {
            if (_coin != value)
            {
                _coin = value;
                _changedFields.Add(nameof(coin));
            }
        }
    }

    public int time
    {
        get => _time;
        set
        {
            if (_time != value)
            {
                _time = value;
                _changedFields.Add(nameof(time));
            }
        }
    }

    public int version
    {
        get => _version;
        set
        {
            if (_version != value)
            {
                _version = value;
                _changedFields.Add(nameof(version));
            }
        }
    }

    // 连胜次数属性访问器
    public int continusWinCnt
    {
        get => _continusWinCnt;
        set
        {
            if (_continusWinCnt != value)
            {
                _continusWinCnt = value;
                _changedFields.Add(nameof(continusWinCnt));
            }
        }
    }

    // 连败次数属性访问器
    public int continusFailCnt
    {
        get => _continusFailCnt;
        set
        {
            if (_continusFailCnt != value)
            {
                _continusFailCnt = value;
                _changedFields.Add(nameof(continusFailCnt));
            }
        }
    }

    public int continusWinLocalDate
    {
        get => _continusWinLocalDate;
        set
        {
            if (_continusWinLocalDate != value)
            {
                _continusWinLocalDate = value;
                _changedFields.Add(nameof(continusWinLocalDate));
            }
        }
    }

    public int nextDayEaseAppliedLocalDate
    {
        get => _nextDayEaseAppliedLocalDate;
        set
        {
            if (_nextDayEaseAppliedLocalDate != value)
            {
                _nextDayEaseAppliedLocalDate = value;
                _changedFields.Add(nameof(nextDayEaseAppliedLocalDate));
            }
        }
    }

    public int maxComboCnt
    {
        get => _maxComboCnt;
        set
        {
            if (_maxComboCnt != value)
            {
                _maxComboCnt = value;
                _changedFields.Add(nameof(maxComboCnt));
            }
        }
    }

    // 累计打关活动阶段
    public int levelPassActivityStage
    {
        get => _levelPassActivityStage;
        set
        {
            if (_levelPassActivityStage != value)
            {
                _levelPassActivityStage = value;
                _changedFields.Add(nameof(levelPassActivityStage));
            }
        }
    }

    // 累计打关活动积分
    public float levelPassActivityPoint
    {
        get => _levelPassActivityPoint;
        set
        {
            if (Math.Abs(_levelPassActivityPoint - value) > float.Epsilon)
            {
                _levelPassActivityPoint = value;
                _changedFields.Add(nameof(levelPassActivityPoint));
            }
        }
    }

    // 提示道具数量属性访问器
    public int inGameItemHintCnt
    {
        get => _inGameItemHintCnt;
        set
        {
            if (_inGameItemHintCnt != value)
            {
                _inGameItemHintCnt = value;
                _changedFields.Add(nameof(inGameItemHintCnt));
            }
        }
    }

    // 打乱道具数量属性访问器
    public int inGameItemShuffleCnt
    {
        get => _inGameItemShuffleCnt;
        set
        {
            if (_inGameItemShuffleCnt != value)
            {
                _inGameItemShuffleCnt = value;
                _changedFields.Add(nameof(inGameItemShuffleCnt));
            }
        }
    }

    // 撤销道具数量属性访问器
    public int inGameItemUndoCnt
    {
        get => _inGameItemUndoCnt;
        set
        {
            if (_inGameItemUndoCnt != value)
            {
                _inGameItemUndoCnt = value;
                _changedFields.Add(nameof(inGameItemUndoCnt));
            }
        }
    }

    // HighLight 道具数量属性访问器
    public int inGameItemHighLightCnt
    {
        get => _inGameItemHighLightCnt;
        set
        {
            if (_inGameItemHighLightCnt != value)
            {
                _inGameItemHighLightCnt = value;
                _changedFields.Add(nameof(inGameItemHighLightCnt));
            }
        }
    }

    public int ropeUndoCnt
    {
        get => _ropeUndoCnt;
        set
        {
            if (_ropeUndoCnt != value)
            {
                _ropeUndoCnt = value;
                _changedFields.Add(nameof(ropeUndoCnt));
            }
        }
    }

    public int ropeHintCnt
    {
        get => _ropeHintCnt;
        set
        {
            if (_ropeHintCnt != value)
            {
                _ropeHintCnt = value;
                _changedFields.Add(nameof(ropeHintCnt));
            }
        }
    }

    public int ropeExtraCnt
    {
        get => _ropeExtraCnt;
        set
        {
            if (_ropeExtraCnt != value)
            {
                _ropeExtraCnt = value;
                _changedFields.Add(nameof(ropeExtraCnt));
            }
        }
    }

    public void EnsureRopeItems()
    {
        if (_ropeItemsInited != 0)
            return;
        _ropeUndoCnt = 3;
        _ropeHintCnt = 3;
        _ropeExtraCnt = 3;
        _ropeItemsInited = 1;
        _changedFields.Add(nameof(ropeUndoCnt));
        _changedFields.Add(nameof(ropeHintCnt));
        _changedFields.Add(nameof(ropeExtraCnt));
    }


    // 关卡次数记录属性访问器
    public List<LevelPlayRecord> levelPlayRecord
    {
        get => _levelPlayRecord;
        set
        {
            _levelPlayRecord = value;
            _changedFields.Add(nameof(levelPlayRecord));
        }
    }

    // 关卡棋子底板记录属性访问器
    public List<LevelTileBaseRecord> levelTileBaseRecord
    {
        get
        {
            _levelTileBaseRecord ??= new List<LevelTileBaseRecord>();
            return _levelTileBaseRecord;
        }
        set
        {
            _levelTileBaseRecord = value;
            _changedFields.Add(nameof(levelTileBaseRecord));
        }
    }

    /// <summary>
    /// 获取指定关卡已确定的棋子底板 id；未确定返回 -1。
    /// </summary>
    public int GetLevelTileBaseId(int levelNum)
    {
        if (_levelTileBaseRecord == null)
        {
            return -1;
        }

        for (int i = 0; i < _levelTileBaseRecord.Count; i++)
        {
            if (_levelTileBaseRecord[i].levelNum == levelNum)
            {
                return _levelTileBaseRecord[i].tilebaseId;
            }
        }

        return -1;
    }

    /// <summary>
    /// 记录指定关卡的棋子底板 id（一旦确定不可更改，无视重开）。
    /// </summary>
    public void SetLevelTileBaseId(int levelNum, int tilebaseId)
    {
        _levelTileBaseRecord ??= new List<LevelTileBaseRecord>();

        for (int i = 0; i < _levelTileBaseRecord.Count; i++)
        {
            if (_levelTileBaseRecord[i].levelNum == levelNum)
            {
                // 已确定则不覆盖
                return;
            }
        }

        _levelTileBaseRecord.Add(new LevelTileBaseRecord(levelNum, tilebaseId));
        _changedFields.Add(nameof(levelTileBaseRecord));
    }

    public bool TryGetTileBaseStyle(int levelNum, bool infinite, out int styleId, out bool pendingReroll)
    {
        styleId = -1;
        pendingReroll = false;
        List<LevelStyleRecord> list = infinite ? _infinitLevelStyleRecord : _levelStyleRecord;
        if (list == null)
        {
            return false;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].levelNum != levelNum)
            {
                continue;
            }

            styleId = list[i].styleId;
            pendingReroll = list[i].pendingReroll;
            return true;
        }

        return false;
    }

    public void SetTileBaseStyle(int levelNum, bool infinite, int styleId, bool pendingReroll)
    {
        List<LevelStyleRecord> list = StyleList(infinite);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].levelNum != levelNum)
            {
                continue;
            }

            list[i].styleId = styleId;
            list[i].pendingReroll = pendingReroll;
            MarkStyleList(infinite);
            return;
        }

        list.Add(new LevelStyleRecord(levelNum, styleId, pendingReroll));
        MarkStyleList(infinite);
    }

    public void MarkTileBaseStyleReroll(int levelNum, bool infinite)
    {
        if (!TryGetTileBaseStyle(levelNum, infinite, out int styleId, out _))
        {
            styleId = -1;
        }

        SetTileBaseStyle(levelNum, infinite, styleId, true);
    }

    List<LevelStyleRecord> StyleList(bool infinite)
    {
        if (infinite)
        {
            _infinitLevelStyleRecord ??= new List<LevelStyleRecord>();
            return _infinitLevelStyleRecord;
        }

        _levelStyleRecord ??= new List<LevelStyleRecord>();
        return _levelStyleRecord;
    }

    void MarkStyleList(bool infinite)
    {
        _changedFields.Add(infinite ? nameof(infinitLevelStyleRecord) : nameof(levelStyleRecord));
    }

    public List<LevelStyleRecord> levelStyleRecord
    {
        get => StyleList(false);
        set
        {
            _levelStyleRecord = value ?? new List<LevelStyleRecord>();
            _changedFields.Add(nameof(levelStyleRecord));
        }
    }

    public List<LevelStyleRecord> infinitLevelStyleRecord
    {
        get => StyleList(true);
        set
        {
            _infinitLevelStyleRecord = value ?? new List<LevelStyleRecord>();
            _changedFields.Add(nameof(infinitLevelStyleRecord));
        }
    }

    public TileBaseStyleBag TileBaseBag(bool infinite)
    {
        if (infinite)
        {
            _infinitTileBaseStyleBag ??= new TileBaseStyleBag();
            _infinitTileBaseStyleBag.knownNames ??= new List<string>();
            _infinitTileBaseStyleBag.remaining ??= new List<string>();
            return _infinitTileBaseStyleBag;
        }

        _tileBaseStyleBag ??= new TileBaseStyleBag();
        _tileBaseStyleBag.knownNames ??= new List<string>();
        _tileBaseStyleBag.remaining ??= new List<string>();
        return _tileBaseStyleBag;
    }

    public void TouchTileBaseStyleBag(bool infinite)
    {
        _changedFields.Add(infinite ? nameof(infinitTileBaseStyleBag) : nameof(tileBaseStyleBag));
    }

    public TileBaseStyleBag tileBaseStyleBag
    {
        get => TileBaseBag(false);
        set
        {
            _tileBaseStyleBag = value ?? new TileBaseStyleBag();
            _changedFields.Add(nameof(tileBaseStyleBag));
        }
    }

    public TileBaseStyleBag infinitTileBaseStyleBag
    {
        get => TileBaseBag(true);
        set
        {
            _infinitTileBaseStyleBag = value ?? new TileBaseStyleBag();
            _changedFields.Add(nameof(infinitTileBaseStyleBag));
        }
    }

    /// <summary>
    /// 删除关号大于 level 的棋子底板记录（用于旧存档迁移到逐关模型）。
    /// </summary>
    public void RemoveLevelTileBaseRecordsAbove(int level)
    {
        if (_levelTileBaseRecord == null || _levelTileBaseRecord.Count == 0)
        {
            return;
        }

        int removed = _levelTileBaseRecord.RemoveAll(record => record.levelNum > level);
        if (removed > 0)
        {
            _changedFields.Add(nameof(levelTileBaseRecord));
        }
    }

    /// <summary>
    /// 删除关号小于 level 的棋子底板记录（裁剪历史，仅保留当前轮附近，封顶存档体积）。
    /// </summary>
    public void RemoveLevelTileBaseRecordsBelow(int level)
    {
        if (_levelTileBaseRecord == null || _levelTileBaseRecord.Count == 0)
        {
            return;
        }

        int removed = _levelTileBaseRecord.RemoveAll(record => record.levelNum < level);
        if (removed > 0)
        {
            _changedFields.Add(nameof(levelTileBaseRecord));
        }
    }

    // 上次会话已知的棋子底板 id 集合
    public List<int> knownTileBaseIds
    {
        get
        {
            _knownTileBaseIds ??= new List<int>();
            return _knownTileBaseIds;
        }
        set
        {
            _knownTileBaseIds = value ?? new List<int>();
            _changedFields.Add(nameof(knownTileBaseIds));
        }
    }

    // 待优先插入的新增背景 id 队列
    public List<int> pendingPriorityTileBaseIds
    {
        get
        {
            _pendingPriorityTileBaseIds ??= new List<int>();
            return _pendingPriorityTileBaseIds;
        }
        set
        {
            _pendingPriorityTileBaseIds = value ?? new List<int>();
            _changedFields.Add(nameof(pendingPriorityTileBaseIds));
        }
    }

    /// <summary>标记棋子底板会话状态（known/pending）已变更，便于落盘。</summary>
    public void MarkTileBaseSessionStateChanged()
    {
        _changedFields.Add(nameof(knownTileBaseIds));
        _changedFields.Add(nameof(pendingPriorityTileBaseIds));
        _changedFields.Add(nameof(knownInfinitTileBaseIds));
        _changedFields.Add(nameof(pendingPriorityInfinitTileBaseIds));
    }

    public List<LevelTileBaseRecord> infinitLevelTileBaseRecord
    {
        get
        {
            _infinitLevelTileBaseRecord ??= new List<LevelTileBaseRecord>();
            return _infinitLevelTileBaseRecord;
        }
        set
        {
            _infinitLevelTileBaseRecord = value;
            _changedFields.Add(nameof(infinitLevelTileBaseRecord));
        }
    }

    public int GetInfinitLevelTileBaseId(int infiniteLevelNum)
    {
        if (_infinitLevelTileBaseRecord == null)
        {
            return -1;
        }

        for (int i = 0; i < _infinitLevelTileBaseRecord.Count; i++)
        {
            if (_infinitLevelTileBaseRecord[i].levelNum == infiniteLevelNum)
            {
                return _infinitLevelTileBaseRecord[i].tilebaseId;
            }
        }

        return -1;
    }

    public void SetInfinitLevelTileBaseId(int infiniteLevelNum, int tilebaseId)
    {
        _infinitLevelTileBaseRecord ??= new List<LevelTileBaseRecord>();

        for (int i = 0; i < _infinitLevelTileBaseRecord.Count; i++)
        {
            if (_infinitLevelTileBaseRecord[i].levelNum == infiniteLevelNum)
            {
                return;
            }
        }

        _infinitLevelTileBaseRecord.Add(new LevelTileBaseRecord(infiniteLevelNum, tilebaseId));
        _changedFields.Add(nameof(infinitLevelTileBaseRecord));
    }

    public void RemoveInfinitLevelTileBaseRecordsAbove(int level)
    {
        if (_infinitLevelTileBaseRecord == null || _infinitLevelTileBaseRecord.Count == 0)
        {
            return;
        }

        int removed = _infinitLevelTileBaseRecord.RemoveAll(record => record.levelNum > level);
        if (removed > 0)
        {
            _changedFields.Add(nameof(infinitLevelTileBaseRecord));
        }
    }

    public void RemoveInfinitLevelTileBaseRecordsBelow(int level)
    {
        if (_infinitLevelTileBaseRecord == null || _infinitLevelTileBaseRecord.Count == 0)
        {
            return;
        }

        int removed = _infinitLevelTileBaseRecord.RemoveAll(record => record.levelNum < level);
        if (removed > 0)
        {
            _changedFields.Add(nameof(infinitLevelTileBaseRecord));
        }
    }

    public List<int> knownInfinitTileBaseIds
    {
        get
        {
            _knownInfinitTileBaseIds ??= new List<int>();
            return _knownInfinitTileBaseIds;
        }
        set
        {
            _knownInfinitTileBaseIds = value ?? new List<int>();
            _changedFields.Add(nameof(knownInfinitTileBaseIds));
        }
    }

    public List<int> pendingPriorityInfinitTileBaseIds
    {
        get
        {
            _pendingPriorityInfinitTileBaseIds ??= new List<int>();
            return _pendingPriorityInfinitTileBaseIds;
        }
        set
        {
            _pendingPriorityInfinitTileBaseIds = value ?? new List<int>();
            _changedFields.Add(nameof(pendingPriorityInfinitTileBaseIds));
        }
    }

    public void SetTileBaseExcludeForNextInfinite(int tilebaseId)
    {
        _tileBaseExcludeIdForNextInfinite = tilebaseId;
        _changedFields.Add(nameof(tileBaseExcludeIdForNextInfinite));
    }

    public int ConsumeTileBaseExcludeForNextInfinite()
    {
        int id = _tileBaseExcludeIdForNextInfinite;
        if (id >= 0)
        {
            _tileBaseExcludeIdForNextInfinite = -1;
            _changedFields.Add(nameof(tileBaseExcludeIdForNextInfinite));
        }

        return id;
    }

    public void SetTileBaseExcludeForNextFinite(int tilebaseId, int finiteLevel)
    {
        _tileBaseExcludeIdForNextFinite = tilebaseId;
        _tileBaseExcludeFiniteLevel = finiteLevel;
        _changedFields.Add(nameof(tileBaseExcludeIdForNextFinite));
        _changedFields.Add(nameof(tileBaseExcludeFiniteLevel));
    }

    public int ConsumeTileBaseExcludeForFiniteLevel(int finiteLevel)
    {
        if (_tileBaseExcludeIdForNextFinite < 0 || _tileBaseExcludeFiniteLevel != finiteLevel)
        {
            return -1;
        }

        int id = _tileBaseExcludeIdForNextFinite;
        _tileBaseExcludeIdForNextFinite = -1;
        _tileBaseExcludeFiniteLevel = -1;
        _changedFields.Add(nameof(tileBaseExcludeIdForNextFinite));
        _changedFields.Add(nameof(tileBaseExcludeFiniteLevel));
        return id;
    }

    public int tileBaseExcludeIdForNextInfinite => _tileBaseExcludeIdForNextInfinite;
    public int tileBaseExcludeIdForNextFinite => _tileBaseExcludeIdForNextFinite;
    public int tileBaseExcludeFiniteLevel => _tileBaseExcludeFiniteLevel;

    // 轮盘计数列表属性访问器
    public List<int> circleAwardCntList
    {
        get
        {
            EnsureCircleAwardListInitialized();
            return _circleAwardCntList;
        }
        set
        {
            _circleAwardCntList = value;
            _changedFields.Add(nameof(circleAwardCntList));
        }
    }

    // 轮盘计数上次重置时间（UTC Ticks）
    public long circleAwardLastResetTicks
    {
        get => _circleAwardLastResetTicks;
        set
        {
            if (_circleAwardLastResetTicks != value)
            {
                _circleAwardLastResetTicks = value;
                _changedFields.Add(nameof(circleAwardLastResetTicks));
            }
        }
    }

    public int circleGrandMissCnt
    {
        get => _circleGrandMissCnt;
        set
        {
            if (_circleGrandMissCnt != value)
            {
                _circleGrandMissCnt = value;
                _changedFields.Add(nameof(circleGrandMissCnt));
            }
        }
    }

    public int circleSecondMissCnt
    {
        get => _circleSecondMissCnt;
        set
        {
            if (_circleSecondMissCnt != value)
            {
                _circleSecondMissCnt = value;
                _changedFields.Add(nameof(circleSecondMissCnt));
            }
        }
    }

    public int dailyVictoryStreakDay
    {
        get => _dailyVictoryStreakDay;
        set
        {
            if (_dailyVictoryStreakDay != value)
            {
                _dailyVictoryStreakDay = value;
                _changedFields.Add(nameof(dailyVictoryStreakDay));
            }
        }
    }

    public long dailyVictoryLastWinWindowTicks
    {
        get => _dailyVictoryLastWinWindowTicks;
        set
        {
            if (_dailyVictoryLastWinWindowTicks != value)
            {
                _dailyVictoryLastWinWindowTicks = value;
                _changedFields.Add(nameof(dailyVictoryLastWinWindowTicks));
            }
        }
    }

    public int dailyVictoryLastClaimedStreakDay
    {
        get => _dailyVictoryLastClaimedStreakDay;
        set
        {
            if (_dailyVictoryLastClaimedStreakDay != value)
            {
                _dailyVictoryLastClaimedStreakDay = value;
                _changedFields.Add(nameof(dailyVictoryLastClaimedStreakDay));
            }
        }
    }

    public int dailyVictoryLastClaimedTotalWinDays
    {
        get
        {
            MigrateDailyVictoryClaimProgressIfNeeded();
            return _dailyVictoryLastClaimedTotalWinDays;
        }
        set
        {
            MigrateDailyVictoryClaimProgressIfNeeded();
            if (_dailyVictoryLastClaimedTotalWinDays != value)
            {
                _dailyVictoryLastClaimedTotalWinDays = value;
                _changedFields.Add(nameof(dailyVictoryLastClaimedTotalWinDays));
            }
        }
    }

    public int dailyVictoryClaimRepairVersion
    {
        get => _dailyVictoryClaimRepairVersion;
        set
        {
            if (_dailyVictoryClaimRepairVersion != value)
            {
                _dailyVictoryClaimRepairVersion = value;
                _changedFields.Add(nameof(dailyVictoryClaimRepairVersion));
            }
        }
    }

    public int dailyVictoryReopenPatchVersion
    {
        get => _dailyVictoryReopenPatchVersion;
        set
        {
            if (_dailyVictoryReopenPatchVersion != value)
            {
                _dailyVictoryReopenPatchVersion = value;
                _changedFields.Add(nameof(dailyVictoryReopenPatchVersion));
            }
        }
    }

    public bool gmForceDailyVictoryActEnabled
    {
        get => _gmForceDailyVictoryActEnabled;
        set
        {
            if (_gmForceDailyVictoryActEnabled != value)
            {
                _gmForceDailyVictoryActEnabled = value;
                _changedFields.Add(nameof(gmForceDailyVictoryActEnabled));
            }
        }
    }

    public int dailyVictoryTotalWinDays
    {
        get => _dailyVictoryTotalWinDays;
        set
        {
            if (_dailyVictoryTotalWinDays != value)
            {
                _dailyVictoryTotalWinDays = value;
                _changedFields.Add(nameof(dailyVictoryTotalWinDays));
            }
        }
    }

    public int dailyVictoryLastLoginLocalDate
    {
        get => _dailyVictoryLastLoginLocalDate;
        set
        {
            if (_dailyVictoryLastLoginLocalDate != value)
            {
                _dailyVictoryLastLoginLocalDate = value;
                _changedFields.Add(nameof(dailyVictoryLastLoginLocalDate));
            }
        }
    }

    public int dailyVictorySnapshotTotalWinDays
    {
        get => _dailyVictorySnapshotTotalWinDays;
        set
        {
            if (_dailyVictorySnapshotTotalWinDays != value)
            {
                _dailyVictorySnapshotTotalWinDays = value;
                _changedFields.Add(nameof(dailyVictorySnapshotTotalWinDays));
            }
        }
    }

    public long dailyVictorySnapshotLastWinTicks
    {
        get => _dailyVictorySnapshotLastWinTicks;
        set
        {
            if (_dailyVictorySnapshotLastWinTicks != value)
            {
                _dailyVictorySnapshotLastWinTicks = value;
                _changedFields.Add(nameof(dailyVictorySnapshotLastWinTicks));
            }
        }
    }

    public bool dailyVictoryPendingSettlementDlg
    {
        get => _dailyVictoryPendingSettlementDlg;
        set
        {
            if (_dailyVictoryPendingSettlementDlg != value)
            {
                _dailyVictoryPendingSettlementDlg = value;
                _changedFields.Add(nameof(dailyVictoryPendingSettlementDlg));
            }
        }
    }

    public bool dailyVictoryHadWinYesterdayBeforeTodayFirstWin
    {
        get => _dailyVictoryHadWinYesterdayBeforeTodayFirstWin;
        set
        {
            if (_dailyVictoryHadWinYesterdayBeforeTodayFirstWin != value)
            {
                _dailyVictoryHadWinYesterdayBeforeTodayFirstWin = value;
                _changedFields.Add(nameof(dailyVictoryHadWinYesterdayBeforeTodayFirstWin));
            }
        }
    }

    public int goldenTreeActPoint
    {
        get => _goldenTreeActPoint;
        set => _goldenTreeActPoint = value;
    }

    public void InCreateGoldenTreePoint()
    {
        _goldenTreeActPoint++;
    }

    public int goldenTreeTodayWinCnt
    {
        get => _goldenTreeTodayWinCnt;
        set => _goldenTreeTodayWinCnt = value;
    }

    public int goldenTreeTodayWinBoardId
    {
        get => _goldenTreeTodayWinBoardId;
        set => _goldenTreeTodayWinBoardId = value;
    }

    public int goldenTreeActWinCnt
    {
        get => _goldenTreeActWinCnt;
        set => _goldenTreeActWinCnt = value;
    }

    public bool goldenTreeActOpen
    {
        get => _goldenTreeActOpen;
        set => _goldenTreeActOpen = value;
    }

    public bool goldenTreeFirstTipsShown
    {
        get => _goldenTreeFirstTipsShown;
        set => _goldenTreeFirstTipsShown = value;
    }

    public string tipsDlgDailyOpen
    {
        get => _tipsDlgDailyOpen ?? "";
        set => _tipsDlgDailyOpen = value ?? "";
    }

    public List<int> goldenTreeFailedLevelIds
    {
        get => _goldenTreeFailedLevelIds;
        set
        {
            _goldenTreeFailedLevelIds = value ?? new List<int>();
            _changedFields.Add(nameof(goldenTreeFailedLevelIds));
        }
    }

    public int goldenTreeLastRank
    {
        get => math.min(_goldenTreeLastRank, 50);
        set => _goldenTreeLastRank = math.min(value, 50);
    }

    public int goldenTreeBoardId
    {
        get => _goldenTreeBoardId;
        set => _goldenTreeBoardId = value;
    }

    public string goldenTreeGroupId
    {
        get => _goldenTreeGroupId ?? "";
        set => _goldenTreeGroupId = value ?? "";
    }

    public int goldenTreeOpenCnt
    {
        get => _goldenTreeOpenCnt;
        set => _goldenTreeOpenCnt = value;
    }

    public int goldenTreeRobotScoreBoardId
    {
        get => _goldenTreeRobotScoreBoardId;
        set => _goldenTreeRobotScoreBoardId = value;
    }

    public string goldenTreeRobotScoreJson
    {
        get => _goldenTreeRobotScoreJson ?? "";
        set => _goldenTreeRobotScoreJson = value ?? "";
    }

    /// <summary>机器人涨分上次结算时间（unix 秒）；首次以当日 UTC 04:00 为基准。</summary>
    public long goldenTreeRobotLastSettleTime
    {
        get => _goldenTreeRobotLastSettleTime;
        set => _goldenTreeRobotLastSettleTime = value;
    }

    public int goldenTreeLastSeasonBoardId
    {
        get => _goldenTreeLastSeasonBoardId;
        set => _goldenTreeLastSeasonBoardId = value;
    }

    public string goldenTreeLastSeasonBoardJson
    {
        get => _goldenTreeLastSeasonBoardJson ?? "";
        set => _goldenTreeLastSeasonBoardJson = value ?? "";
    }

    /// <summary>上赛季日榜快照时的联赛段位，结算升/保/降段以此为准。</summary>
    public int goldenTreeLastSeasonSnapshotLeagueTier
    {
        get => _goldenTreeLastSeasonSnapshotLeagueTier;
        set => _goldenTreeLastSeasonSnapshotLeagueTier = value;
    }

    public bool goldenTreeLastSeasonPendingReward
    {
        get => _goldenTreeLastSeasonPendingReward;
        set => _goldenTreeLastSeasonPendingReward = value;
    }

    /// <summary>已发放结算奖励的赛季 boardId；-1 表示无。不被 ClearAllGoldTreeData 清除。</summary>
    public int goldenTreeSettlementRewardGrantedBoardId
    {
        get => _goldenTreeSettlementRewardGrantedBoardId;
        set => _goldenTreeSettlementRewardGrantedBoardId = value;
    }

    /// <summary>联赛段位 id（1 青铜 ~ 8 荣耀）；0 表示未初始化。不被 ClearAllGoldTreeData 清除。</summary>
    public int goldenTreeLeagueTier
    {
        get => _goldenTreeLeagueTier;
        set => _goldenTreeLeagueTier = value;
    }

    /// <summary>上一日榜结算前的联赛段位，供段位变化展示。</summary>
    public int goldenTreeLastSeasonLeagueTier
    {
        get => _goldenTreeLastSeasonLeagueTier;
        set => _goldenTreeLastSeasonLeagueTier = value;
    }

    /// <summary>上一日榜结算后的联赛段位，供联赛结束展示。</summary>
    public int goldenTreeSettlementLeagueTier
    {
        get => _goldenTreeSettlementLeagueTier;
        set => _goldenTreeSettlementLeagueTier = value;
    }

    /// <summary>已应用段位结算的日榜 boardId；-1 表示无。不被 ClearAllGoldTreeData 清除。</summary>
    public int goldenTreeSettlementTierAppliedBoardId
    {
        get => _goldenTreeSettlementTierAppliedBoardId;
        set => _goldenTreeSettlementTierAppliedBoardId = value;
    }

    // 增加关卡进入次数的方法
    public void IncrementLevelPlayCount(int levelId)
    {
        if (_levelPlayRecord == null)
        {
            _levelPlayRecord = new List<LevelPlayRecord>();
        }

        // 查找现有记录

        LevelPlayRecord existingRecord = null;
        for (int i = 0; i < _levelPlayRecord.Count; i++)
        {
            if (_levelPlayRecord[i].levelId == levelId)
            {
                existingRecord = _levelPlayRecord[i];
                break;
            }
        }


        if (existingRecord != null)
        {
            existingRecord.playCount++;
        }
        else
        {
            _levelPlayRecord.Add(new LevelPlayRecord(levelId, 1));
        }
        _changedFields.Add(nameof(levelPlayRecord));
    }

    // 获取关卡进入次数的方法
    public int GetLevelPlayCount(int levelId)
    {
        if (_levelPlayRecord == null)
        {
            return 0;
        }

        // 查找记录

        for (int i = 0; i < _levelPlayRecord.Count; i++)
        {
            if (_levelPlayRecord[i].levelId == levelId)
            {
                return _levelPlayRecord[i].playCount;
            }
        }
        return 0;
    }

    //
    // 轮盘奖励计数：每天凌晨五点刷新格子次数与当日剔除；保底未中计数跨天累计
    //

    // 增加指定索引的计数（默认+1）
    public void AddCircleAwardCount(int idx, int delta = 1)
    {
        TryResetCircleAwardCountsByDaily5AM();
        EnsureCircleAwardListInitialized();
        if (idx < 0 || idx >= ActivityData.CircleSlotCount)
        {
            return;
        }
        _circleAwardCntList[idx] += delta;
        _changedFields.Add(nameof(circleAwardCntList));
    }

    // 获取指定索引的计数
    public int GetCircleAwardCount(int idx)
    {
        TryResetCircleAwardCountsByDaily5AM();
        EnsureCircleAwardListInitialized();
        if (idx < 0 || idx >= ActivityData.CircleSlotCount)
        {
            return 0;
        }
        return _circleAwardCntList[idx];
    }

    public int GetTotalCircleAwardCnt()
    {
        TryResetCircleAwardCountsByDaily5AM();
        EnsureCircleAwardListInitialized();
        return _circleAwardCntList.Sum();
    }

    public bool IsCircleAwardRemovedToday(int awardId)
    {
        TryResetCircleAwardCountsByDaily5AM();
        EnsureCircleDailyRemovedInitialized();
        return _circleDailyRemovedIds.Contains(awardId);
    }

    public void AddCircleDailyRemovedId(int awardId)
    {
        TryResetCircleAwardCountsByDaily5AM();
        EnsureCircleDailyRemovedInitialized();
        if (_circleDailyRemovedIds.Contains(awardId))
        {
            return;
        }

        _circleDailyRemovedIds.Add(awardId);
        _changedFields.Add(nameof(circleDailyRemovedIds));
    }

    public List<int> circleDailyRemovedIds
    {
        get
        {
            TryResetCircleAwardCountsByDaily5AM();
            EnsureCircleDailyRemovedInitialized();
            return _circleDailyRemovedIds;
        }
    }

    public void AddCircleGrandMissCnt(int delta = 1)
    {
        circleGrandMissCnt = _circleGrandMissCnt + delta;
    }

    public void ResetCircleGrandMissCnt()
    {
        circleGrandMissCnt = 0;
    }

    public void AddCircleSecondMissCnt(int delta = 1)
    {
        circleSecondMissCnt = _circleSecondMissCnt + delta;
    }

    public void ResetCircleSecondMissCnt()
    {
        circleSecondMissCnt = 0;
    }

    DateTime GetCircleAwardNowLocal()
    {
        return GameData.IsGmServerTimeOverride
            ? GameData.GetGameLocalDateTime()
            : DateTime.Now;
    }

    // 手动重置当日次数，并刷新重置时间
    public void ResetCircleAwardCounts()
    {
        ResetCircleAwardCountsInternal(GetCircleAwardNowLocal());
    }

    // 如跨过每日5点则自动重置
    public void TryResetCircleAwardCountsByDaily5AM()
    {
        EnsureCircleAwardListInitialized();
        DateTime nowLocal = GetCircleAwardNowLocal();
        DateTime windowStartLocal = (nowLocal.Hour >= 5)
            ? new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, 5, 0, 0)
            : new DateTime(nowLocal.AddDays(-1).Year, nowLocal.AddDays(-1).Month, nowLocal.AddDays(-1).Day, 5, 0, 0);

        DateTime lastResetLocal = _circleAwardLastResetTicks == 0
            ? DateTime.MinValue
            : new DateTime(_circleAwardLastResetTicks, DateTimeKind.Utc).ToLocalTime();

        if (lastResetLocal < windowStartLocal)
        {
            ResetCircleAwardCountsInternal(nowLocal);
        }
    }

    // 获取下一次本地时间凌晨5点（今日未到5点则为今天5点，否则为明天5点）
    public DateTime GetNextDaily5AMLocal()
    {
        DateTime nowLocal = GetCircleAwardNowLocal();
        DateTime today5 = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, 5, 0, 0);
        if (nowLocal < today5)
        {
            return today5;
        }
        DateTime tomorrow = nowLocal.AddDays(1);
        return new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 5, 0, 0);
    }

    // 获取下一次本地时间凌晨5点对应的 Unix 时间戳（秒）
    public long GetNextDaily5AMUnixSeconds()
    {
        DateTime nextLocal = GetNextDaily5AMLocal();
        return new DateTimeOffset(nextLocal).ToUnixTimeSeconds();
    }

    public long GetSecondsUntilNext5AM()
    {
        DateTime nowLocal = GetCircleAwardNowLocal();
        DateTime next5AM = GetNextDaily5AMLocal();
        TimeSpan span = next5AM - nowLocal;
        return (long)span.TotalSeconds;
    }

    /// <summary>
    /// 每日连胜/首胜的日切窗口起点（本地 00:00）。
    /// </summary>
    public DateTime GetCurrentDailyMidnightWindowStartLocal()
    {
        if (GameData.IsGmServerTimeOverride)
        {
            var gameLocal = GameData.GetGameLocalDateTime();
            return new DateTime(gameLocal.Year, gameLocal.Month, gameLocal.Day);
        }

        return DateTime.Today;
    }

    private DateTime GetDailyVictoryLastWinWindowLocal()
    {
        return _dailyVictoryLastWinWindowTicks == 0
            ? DateTime.MinValue
            : new DateTime(_dailyVictoryLastWinWindowTicks, DateTimeKind.Utc).ToLocalTime();
    }

    private DateTime ParseLocalDateFromInt(int localDateInt)
    {
        int y = localDateInt / 10000;
        int m = (localDateInt % 10000) / 100;
        int d = localDateInt % 100;
        return new DateTime(y, m, d);
    }

    private void SaveDailyVictoryLoginSnapshot()
    {
        _dailyVictoryLastLoginLocalDate = GetTodayLocalDateInt();
        _dailyVictorySnapshotTotalWinDays = _dailyVictoryTotalWinDays;
        _dailyVictorySnapshotLastWinTicks = _dailyVictoryLastWinWindowTicks;
        _changedFields.Add(nameof(dailyVictoryLastLoginLocalDate));
        _changedFields.Add(nameof(dailyVictorySnapshotTotalWinDays));
        _changedFields.Add(nameof(dailyVictorySnapshotLastWinTicks));
    }

    private void RestoreDailyVictoryLoginSnapshot()
    {
        _dailyVictoryTotalWinDays = _dailyVictorySnapshotTotalWinDays;
        _dailyVictoryStreakDay = CalcDailyVictoryCycleDay(_dailyVictoryTotalWinDays);
        if (_dailyVictoryTotalWinDays > 0)
        {
            DateTime currentWindow = GetCurrentDailyMidnightWindowStartLocal();
            _dailyVictoryLastWinWindowTicks = currentWindow.ToUniversalTime().Ticks;
        }
        else
        {
            _dailyVictoryLastWinWindowTicks = _dailyVictorySnapshotLastWinTicks;
        }

        _changedFields.Add(nameof(dailyVictoryStreakDay));
        _changedFields.Add(nameof(dailyVictoryTotalWinDays));
        _changedFields.Add(nameof(dailyVictoryLastWinWindowTicks));
    }

    /// <summary>
    /// 活动关闭期间连胜冻结；重开后首次登录若上次胜利早于昨天，
    /// 将上次胜利日窗补到昨天，避免登录日切/今日首胜把存量连胜清掉。只执行一次。
    /// </summary>
    public void TryApplyDailyVictoryReopenStreakPatch()
    {
        const int CurrentReopenPatchVersion = 1;
        if (_dailyVictoryReopenPatchVersion >= CurrentReopenPatchVersion)
        {
            return;
        }

        if (_dailyVictoryTotalWinDays > 0)
        {
            DateTime currentWindow = GetCurrentDailyMidnightWindowStartLocal();
            DateTime yesterdayWindow = currentWindow.AddDays(-1);
            bool needsPatch = _dailyVictoryLastWinWindowTicks == 0;
            if (!needsPatch)
            {
                DateTime lastWinWindow = GetDailyVictoryLastWinWindowLocal();
                needsPatch = lastWinWindow < yesterdayWindow;
            }

            if (needsPatch)
            {
                _dailyVictoryLastWinWindowTicks = yesterdayWindow.ToUniversalTime().Ticks;
                _dailyVictoryStreakDay = CalcDailyVictoryCycleDay(_dailyVictoryTotalWinDays);
                _changedFields.Add(nameof(dailyVictoryLastWinWindowTicks));
                _changedFields.Add(nameof(dailyVictoryStreakDay));
                // 同步登录快照，避免 today > lastLogin+1 分支直接清零
                SaveDailyVictoryLoginSnapshot();
            }
        }

        _dailyVictoryReopenPatchVersion = CurrentReopenPatchVersion;
        _changedFields.Add(nameof(dailyVictoryReopenPatchVersion));
    }

    public void TryResetDailyVictoryStreakOnLogin()
    {
        int today = GetTodayLocalDateInt();
        int lastLogin = _dailyVictoryLastLoginLocalDate;

        if (lastLogin == 0)
        {
            if (_dailyVictoryLastWinWindowTicks != 0)
            {
                _dailyVictoryLastLoginLocalDate = ToLocalDateInt(GetDailyVictoryLastWinWindowLocal());
                _dailyVictorySnapshotTotalWinDays = _dailyVictoryTotalWinDays;
                _dailyVictorySnapshotLastWinTicks = _dailyVictoryLastWinWindowTicks;
                lastLogin = _dailyVictoryLastLoginLocalDate;
            }
            else
            {
                SaveDailyVictoryLoginSnapshot();
                return;
            }
        }

        DateTime todayDt = ParseLocalDateFromInt(today);
        DateTime lastLoginDt = ParseLocalDateFromInt(lastLogin);

        if (todayDt < lastLoginDt)
        {
            RestoreDailyVictoryLoginSnapshot();
        }
        else if (todayDt > lastLoginDt.AddDays(1))
        {
            _dailyVictoryStreakDay = 0;
            _dailyVictoryTotalWinDays = 0;
            ResetDailyVictoryClaimProgress();
            _changedFields.Add(nameof(dailyVictoryStreakDay));
            _changedFields.Add(nameof(dailyVictoryTotalWinDays));
        }
        else if (_dailyVictoryLastWinWindowTicks != 0)
        {
            DateTime currentWindow = GetCurrentDailyMidnightWindowStartLocal();
            DateTime yesterdayWindow = currentWindow.AddDays(-1);
            DateTime lastWinWindow = GetDailyVictoryLastWinWindowLocal();

            if (lastWinWindow < yesterdayWindow)
            {
                _dailyVictoryStreakDay = 0;
                _dailyVictoryTotalWinDays = 0;
                ResetDailyVictoryClaimProgress();
                _changedFields.Add(nameof(dailyVictoryStreakDay));
                _changedFields.Add(nameof(dailyVictoryTotalWinDays));
            }
        }

        SaveDailyVictoryLoginSnapshot();
    }

    public static int CalcDailyVictoryCycleDay(int totalWinDays)
    {
        return totalWinDays <= 0 ? 0 : ((totalWinDays - 1) % 7) + 1;
    }

    static bool ShouldResetDailyVictoryClaimProgress(int previousTotalWinDays, int newTotalWinDays)
    {
        if (newTotalWinDays <= 0)
        {
            return true;
        }

        if (newTotalWinDays == 1 && previousTotalWinDays > 1)
        {
            return true;
        }

        return false;
    }

    void ResetDailyVictoryClaimProgress()
    {
        _dailyVictoryLastClaimedStreakDay = 0;
        _dailyVictoryLastClaimedTotalWinDays = 0;
        _changedFields.Add(nameof(dailyVictoryLastClaimedStreakDay));
        _changedFields.Add(nameof(dailyVictoryLastClaimedTotalWinDays));
    }

    void MigrateDailyVictoryClaimProgressIfNeeded()
    {
        if (_dailyVictoryClaimRepairVersion < 1)
        {
            _dailyVictoryClaimRepairVersion = 1;
            if (_dailyVictoryTotalWinDays == 7
                && _dailyVictoryLastClaimedStreakDay == 7
                && CalcDailyVictoryCycleDay(_dailyVictoryTotalWinDays) == 7)
            {
                _dailyVictoryLastClaimedTotalWinDays = 0;
                _changedFields.Add(nameof(dailyVictoryLastClaimedTotalWinDays));
                _changedFields.Add(nameof(dailyVictoryClaimRepairVersion));
                return;
            }

            _changedFields.Add(nameof(dailyVictoryClaimRepairVersion));
        }

        if (_dailyVictoryLastClaimedTotalWinDays > 0 || _dailyVictoryLastClaimedStreakDay <= 0)
        {
            return;
        }

        int cycleDay = CalcDailyVictoryCycleDay(_dailyVictoryTotalWinDays);
        if (_dailyVictoryTotalWinDays < _dailyVictoryLastClaimedStreakDay
            || _dailyVictoryLastClaimedStreakDay > cycleDay)
        {
            _dailyVictoryLastClaimedTotalWinDays = 0;
        }
        else
        {
            _dailyVictoryLastClaimedTotalWinDays = _dailyVictoryLastClaimedStreakDay;
        }

        _changedFields.Add(nameof(dailyVictoryLastClaimedTotalWinDays));
    }

    public void GmSetDailyVictoryStreak(int totalWinDays)
    {
        gmForceDailyVictoryActEnabled = true;
        totalWinDays = Mathf.Max(0, totalWinDays);
        _dailyVictoryTotalWinDays = totalWinDays;
        _dailyVictoryStreakDay = CalcDailyVictoryCycleDay(totalWinDays);
        _changedFields.Add(nameof(dailyVictoryStreakDay));
        _changedFields.Add(nameof(dailyVictoryTotalWinDays));

        ResetDailyVictoryClaimProgress();

        if (totalWinDays > 0)
        {
            DateTime yesterdayWindow = GetCurrentDailyMidnightWindowStartLocal().AddDays(-1);
            _dailyVictoryLastWinWindowTicks = yesterdayWindow.ToUniversalTime().Ticks;
        }
        else
        {
            _dailyVictoryLastWinWindowTicks = 0;
            _goldenTreeActOpen = false;
            _goldenTreeTodayWinCnt = 0;
            _changedFields.Add(nameof(goldenTreeActOpen));
            _changedFields.Add(nameof(goldenTreeTodayWinCnt));
        }

        _changedFields.Add(nameof(dailyVictoryLastWinWindowTicks));
        SaveDailyVictoryLoginSnapshot();

        // GM 设置连胜未满 7 天时收回连胜头像框
        if (totalWinDays < 7 && GPUserDataManager.Instance != null)
        {
            GPUserDataManager.Instance.SyncWinStreakAvatarFrameWithStreak();
        }
    }

    public void GmSetGoldTreeActPoint(int point)
    {
        point = Mathf.Max(0, point);
        _goldenTreeActPoint = point;
        _changedFields.Add(nameof(goldenTreeActPoint));
    }

    public void GmSetGoldTreeLeagueTier(int tier)
    {
        tier = GoldTreeLeagueRankFlagHelper.NormalizeLeagueTier(tier);
        _goldenTreeLeagueTier = tier;
        _goldenTreeLastSeasonLeagueTier = 0;
        _goldenTreeSettlementLeagueTier = 0;
        _goldenTreeLastSeasonSnapshotLeagueTier = 0;
        _goldenTreeSettlementTierAppliedBoardId = -1;
        _goldenTreeRobotScoreBoardId = -1;
        _goldenTreeRobotScoreJson = "";
        _goldenTreeRobotLastSettleTime = GoldTreeRobotGrowthHelper.GetActivityDayStartUnix();

        _changedFields.Add(nameof(goldenTreeLeagueTier));
        _changedFields.Add(nameof(goldenTreeLastSeasonLeagueTier));
        _changedFields.Add(nameof(goldenTreeSettlementLeagueTier));
        _changedFields.Add(nameof(goldenTreeLastSeasonSnapshotLeagueTier));
        _changedFields.Add(nameof(goldenTreeSettlementTierAppliedBoardId));
    }

    public bool HasDailyVictoryWinToday()
    {
        if (_dailyVictoryTotalWinDays <= 0 || _dailyVictoryLastWinWindowTicks == 0)
        {
            return false;
        }

        DateTime currentWindow = GetCurrentDailyMidnightWindowStartLocal();
        DateTime lastWinWindow = GetDailyVictoryLastWinWindowLocal();
        return lastWinWindow == currentWindow;
    }

    public bool RecordDailyVictoryWin(out bool hadWinYesterday)
    {
        hadWinYesterday = false;
        DateTime currentWindow = GetCurrentDailyMidnightWindowStartLocal();
        DateTime currentDate = currentWindow.Date;
        int previousTotalWinDays = _dailyVictoryTotalWinDays;

        if (_dailyVictoryLastWinWindowTicks == 0)
        {
            _dailyVictoryTotalWinDays = 1;
        }
        else
        {
            DateTime lastDate = GetDailyVictoryLastWinWindowLocal().Date;

            if (currentDate == lastDate)
            {
                return false;
            }

            if (currentDate < lastDate)
            {
                return false;
            }

            DateTime yesterdayDate = currentDate.AddDays(-1);
            hadWinYesterday = lastDate == yesterdayDate;
            if (lastDate == yesterdayDate)
            {
                _dailyVictoryTotalWinDays++;
            }
            else
            {
                _dailyVictoryTotalWinDays = 1;
            }
        }

        if (ShouldResetDailyVictoryClaimProgress(previousTotalWinDays, _dailyVictoryTotalWinDays))
        {
            ResetDailyVictoryClaimProgress();
        }

        _dailyVictoryStreakDay = CalcDailyVictoryCycleDay(_dailyVictoryTotalWinDays);
        _dailyVictoryLastWinWindowTicks = currentWindow.ToUniversalTime().Ticks;
        _changedFields.Add(nameof(dailyVictoryStreakDay));
        _changedFields.Add(nameof(dailyVictoryTotalWinDays));
        _changedFields.Add(nameof(dailyVictoryLastWinWindowTicks));
        SaveDailyVictoryLoginSnapshot();
        return true;
    }

    // 私有：确保列表正确初始化为11个0
    private void EnsureCircleAwardListInitialized()
    {
        if (_circleAwardCntList == null)
        {
            _circleAwardCntList = new List<int>(ActivityData.CircleSlotCount);
        }
        if (_circleAwardCntList.Count != ActivityData.CircleSlotCount)
        {
            _circleAwardCntList.Clear();
            for (int i = 0; i < ActivityData.CircleSlotCount; i++)
            {
                _circleAwardCntList.Add(0);
            }
        }
    }

    private void EnsureCircleDailyRemovedInitialized()
    {
        if (_circleDailyRemovedIds == null)
        {
            _circleDailyRemovedIds = new List<int>();
        }
    }

    // 私有：执行重置并记录时间（不清保底未中计数）
    private void ResetCircleAwardCountsInternal(DateTime nowLocal)
    {
        EnsureCircleAwardListInitialized();
        EnsureCircleDailyRemovedInitialized();
        for (int i = 0; i < ActivityData.CircleSlotCount; i++)
        {
            _circleAwardCntList[i] = 0;
        }
        _circleDailyRemovedIds.Clear();
        _circleAwardLastResetTicks = nowLocal.ToUniversalTime().Ticks;
        _changedFields.Add(nameof(circleAwardCntList));
        _changedFields.Add(nameof(circleAwardLastResetTicks));
        _changedFields.Add(nameof(circleDailyRemovedIds));
    }

    /// <summary>
    /// 增加连胜次数
    /// 当增加连胜时，连败次数会被重置为0
    /// </summary>
    public void IncrementWinCount()
    {
        _continusWinCnt++;
        _continusFailCnt = 0; // 重置连败次数
        _changedFields.Add(nameof(continusWinCnt));
        _changedFields.Add(nameof(continusFailCnt));
    }

    /// <summary>
    /// 增加连败次数
    /// 当增加连败时，连胜次数会被重置为0
    /// </summary>
    public void IncrementFailCount()
    {
        _continusFailCnt++;
        _continusWinCnt = 0; // 重置连胜次数
        _changedFields.Add(nameof(continusFailCnt));
        _changedFields.Add(nameof(continusWinCnt));
    }

    /// <summary>
    /// 重置连胜和连败次数
    /// </summary>
    public void ResetWinFailCount()
    {
        _continusWinCnt = 0;
        _continusFailCnt = 0;
        _continusWinLocalDate = 0;
        _nextDayEaseAppliedLocalDate = 0;
        _changedFields.Add(nameof(continusWinCnt));
        _changedFields.Add(nameof(continusFailCnt));
        _changedFields.Add(nameof(continusWinLocalDate));
        _changedFields.Add(nameof(nextDayEaseAppliedLocalDate));
    }

    public int GetTodayLocalDate()
    {
        return GetTodayLocalDateInt();
    }

    /// <summary>
    /// 增加提示道具数量
    /// </summary>
    /// <param name="count">增加的数量</param>
    public void AddHintItem(int count = 1)
    {
        _inGameItemHintCnt += count;
        _changedFields.Add(nameof(inGameItemHintCnt));
    }

    /// <summary>
    /// 减少提示道具数量
    /// </summary>
    /// <param name="count">减少的数量</param>
    /// <returns>是否成功减少（数量不足时返回false）</returns>
    public bool UseHintItem(int count = 1)
    {
        if (_inGameItemHintCnt >= count)
        {
            _inGameItemHintCnt -= count;
            _changedFields.Add(nameof(inGameItemHintCnt));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 增加打乱道具数量
    /// </summary>
    /// <param name="count">增加的数量</param>
    public void AddShuffleItem(int count = 1)
    {
        _inGameItemShuffleCnt += count;
        _changedFields.Add(nameof(inGameItemShuffleCnt));
    }

    /// <summary>
    /// 减少打乱道具数量
    /// </summary>
    /// <param name="count">减少的数量</param>
    /// <returns>是否成功减少（数量不足时返回false）</returns>
    public bool UseShuffleItem(int count = 1)
    {
        if (_inGameItemShuffleCnt >= count)
        {
            _inGameItemShuffleCnt -= count;
            _changedFields.Add(nameof(inGameItemShuffleCnt));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 增加撤销道具数量
    /// </summary>
    /// <param name="count">增加的数量</param>
    public void AddUndoItem(int count = 1)
    {
        _inGameItemUndoCnt += count;
        _changedFields.Add(nameof(inGameItemUndoCnt));
    }

    /// <summary>
    /// 减少撤销道具数量
    /// </summary>
    /// <param name="count">减少的数量</param>
    /// <returns>是否成功减少（数量不足时返回false）</returns>
    public bool UseUndoItem(int count = 1)
    {
        if (_inGameItemUndoCnt >= count)
        {
            _inGameItemUndoCnt -= count;
            _changedFields.Add(nameof(inGameItemUndoCnt));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 增加 HighLight 道具数量
    /// </summary>
    public void AddHighLightItem(int count = 1)
    {
        _inGameItemHighLightCnt += count;
        _changedFields.Add(nameof(inGameItemHighLightCnt));
    }

    /// <summary>
    /// 减少 HighLight 道具数量
    /// </summary>
    public bool UseHighLightItem(int count = 1)
    {
        if (_inGameItemHighLightCnt >= count)
        {
            _inGameItemHighLightCnt -= count;
            _changedFields.Add(nameof(inGameItemHighLightCnt));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 重置所有道具数量
    /// </summary>
    public void ResetAllItems()
    {
        _inGameItemHintCnt = 0;
        _inGameItemShuffleCnt = 0;
        _inGameItemUndoCnt = 0;
        _inGameItemHighLightCnt = 0;

        _changedFields.Add(nameof(inGameItemHintCnt));
        _changedFields.Add(nameof(inGameItemShuffleCnt));
        _changedFields.Add(nameof(inGameItemUndoCnt));
        _changedFields.Add(nameof(inGameItemHighLightCnt));
    }

    // 构造函数 - 使用默认值初始化
    public GPUserData()
    {
        // 确保所有字段都初始化为默认值
        _levelNum = 0;
        _infinitLevelNum = -1;
        _coin = 300;
        _time = 0;
        _uid = "";
        _countryCode = "";
        _version = 0;
        _continusWinCnt = 0;
        _continusFailCnt = 0;
        _continusWinLocalDate = 0;
        _nextDayEaseAppliedLocalDate = 0;
        _maxComboCnt = 0;
        _levelPassActivityStage = 1;
        _levelPassActivityPoint = 0f;
        _inGameItemHintCnt = 3;
        _inGameItemShuffleCnt = 3;
        _inGameItemUndoCnt = 3;
        _inGameItemHighLightCnt = 3;
        _ropeUndoCnt = 3;
        _ropeHintCnt = 3;
        _ropeExtraCnt = 3;
        _ropeItemsInited = 1;

        _bNoAd = false;
        _freeReviveUsedCnt = 0;
        _maxFreeReviveCount = GPUserDataManager.UnlockedMaxFreeReviveCount;
        _interADAbEligible = true;
        _interADTestGroup = "";
        _interADCooldownUnix = 0;
        _interADLoadLevelRecordDate = 0;
        _interADAbEligibleV3 = true;
        _interADTestGroupV3 = "";
        _interADCooldownUnixV3 = 0;
        _interADTestGroupV4 = "";
        _interADGroupV5 = "";
        _interADCooldownUnixV4 = 0;
        _levelPlayRecord = new List<LevelPlayRecord>();
        _levelTileBaseRecord = new List<LevelTileBaseRecord>();
        _knownTileBaseIds = new List<int>();
        _pendingPriorityTileBaseIds = new List<int>();
        _infinitLevelTileBaseRecord = new List<LevelTileBaseRecord>();
        _knownInfinitTileBaseIds = new List<int>();
        _pendingPriorityInfinitTileBaseIds = new List<int>();
        _levelStyleRecord = new List<LevelStyleRecord>();
        _infinitLevelStyleRecord = new List<LevelStyleRecord>();
        _tileBaseStyleBag = new TileBaseStyleBag();
        _infinitTileBaseStyleBag = new TileBaseStyleBag();
        _tileBaseExcludeIdForNextInfinite = -1;
        _tileBaseExcludeIdForNextFinite = -1;
        _tileBaseExcludeFiniteLevel = -1;
        _circleAwardCntList = new List<int>(6) { 0, 0, 0, 0, 0, 0 };
        _circleAwardLastResetTicks = DateTime.UtcNow.Ticks;
        ClearAllDailyVictoryData();
        _goldenTreeActPoint = 0;
        _goldenTreeTodayWinCnt = 0;
        _goldenTreeActWinCnt = 0;
        _goldenTreeActOpen = false;
        _goldenTreeBoardId = -1;
        _goldenTreeLastRank = -1;
        _goldenTreeGroupId = "";
        _goldenTreeOpenCnt = 0;
        _goldenTreeRobotScoreBoardId = -1;
        _goldenTreeRobotScoreJson = "";
        _goldenTreeRobotLastSettleTime = 0;
        _goldenTreeLastSeasonBoardId = -1;
        _goldenTreeLastSeasonBoardJson = "";
        _goldenTreeLastSeasonPendingReward = false;
        _goldenTreeSettlementRewardGrantedBoardId = -1;
        _goldenTreeLeagueTier = 0;
        _goldenTreeLastSeasonLeagueTier = 0;
        _goldenTreeSettlementLeagueTier = 0;
        _goldenTreeSettlementTierAppliedBoardId = -1;

        shouldCheckNewUser = true;
        _videoPlayCntFirstDay = 0;
        _firstDayWindowStartUtcTicks = DateTime.Now.Date.ToUniversalTime().Ticks;
        _firstDayLocalDate = GetTodayLocalDateInt();
        _changedFields.Clear(); // 确保变更追踪为空
    }


    // 检查是否有变更
    public bool HasChanges => _changedFields.Count > 0;

    // 获取完整数据的JSON
    public string GetFullDataJson()
    {
        return JsonUtility.ToJson(this);
    }

    // 从JSON恢复数据
    public void LoadFromJson(string json)
    {
        // JsonUtility 序列化的是私有字段名，不是属性名。
        // 构造函数默认 eligible=true；老存档缺该字段时需降为 false。
        bool jsonHasEligible = !string.IsNullOrEmpty(json)
            && (json.IndexOf("\"_interADAbEligible\"", StringComparison.Ordinal) >= 0
                || json.IndexOf("\"interADAbEligible\"", StringComparison.Ordinal) >= 0);
        bool jsonHasEligibleV3 = !string.IsNullOrEmpty(json)
            && (json.IndexOf("\"_interADAbEligibleV3\"", StringComparison.Ordinal) >= 0
                || json.IndexOf("\"interADAbEligibleV3\"", StringComparison.Ordinal) >= 0);
        bool jsonHasGroupV4 = !string.IsNullOrEmpty(json)
            && (json.IndexOf("\"_interADTestGroupV4\"", StringComparison.Ordinal) >= 0
                || json.IndexOf("\"interADTestGroupV4\"", StringComparison.Ordinal) >= 0);
        bool jsonHasGroupV5 = !string.IsNullOrEmpty(json)
            && (json.IndexOf("\"_interADGroupV5\"", StringComparison.Ordinal) >= 0
                || json.IndexOf("\"interADGroupV5\"", StringComparison.Ordinal) >= 0);
        bool jsonHasMaxFreeRevive = !string.IsNullOrEmpty(json)
            && (json.IndexOf("\"_maxFreeReviveCount\"", StringComparison.Ordinal) >= 0
                || json.IndexOf("\"maxFreeReviveCount\"", StringComparison.Ordinal) >= 0);

        JsonUtility.FromJsonOverwrite(json, this);
        _changedFields.Clear(); // 加载后清除变更标记
        EnsureRopeItems();

        // 构造函数默认未锁定=-1；老存档缺字段时 FromJsonOverwrite 会留下 -1，必须改回 5。
        if (!jsonHasMaxFreeRevive)
        {
            _maxFreeReviveCount = GPUserDataManager.LegacyMaxFreeReviveCount;
        }

        // V3 首次迁移只继承“存档中明确存在且为 true”的 V2 eligible。
        // 必须在下方 V2 兼容修正之前取值：历史存档缺 _interADAbEligible 时，
        // 即使残留了错误的 V2 组，也必须留在 V3 rest。
        bool migrateEligibleToV3 = jsonHasEligible && _interADAbEligible;

        if (!string.IsNullOrWhiteSpace(_interADTestGroup))
        {
            // V2 已入组账号保持 eligible（修复曾因字段名误判被写成 false 的存档）
            _interADAbEligible = true;
        }
        else if (!jsonHasEligible)
        {
            _interADAbEligible = false;
        }

        if (!string.IsNullOrWhiteSpace(_interADTestGroupV3))
        {
            _interADAbEligibleV3 = true;
        }
        else if (!jsonHasEligibleV3)
        {
            // 不继承 V2 组，只继承有效实验资格；V3 组保持空，随后按 V3 RC 重新分组。
            _interADAbEligibleV3 = migrateEligibleToV3;
        }

        // 老存档没有 V5 分组。不从 V4 的 E 继承，升级后按渠道和国家码重新正式分组。
        if (!jsonHasGroupV5)
            _interADGroupV5 = "";

        // V4 首次迁移：原 E 组沿用冷却锚点；legacy / 空组 / 未入组从 0 开始，过开启关后首条必出。
        if (!jsonHasGroupV4)
        {
            if (string.Equals(_interADTestGroupV3, "E", StringComparison.OrdinalIgnoreCase))
            {
                interADTestGroupV4 = "E";
                interADCooldownUnixV4 = _interADCooldownUnixV3;
            }
            else
            {
                interADTestGroupV4 = "";
                interADCooldownUnixV4 = 0;
            }
        }

        // 兼容旧存档：若未初始化过首日字段，补初始化（以本地日历日为准）
        if (_firstDayLocalDate == 0)
        {
            if (_firstDayWindowStartUtcTicks != 0)
            {
                DateTime localFromOldTicks = new DateTime(_firstDayWindowStartUtcTicks, DateTimeKind.Utc).ToLocalTime();
                _firstDayLocalDate = ToLocalDateInt(localFromOldTicks);
            }
            else
            {
                _firstDayLocalDate = GetTodayLocalDateInt();
            }
        }

        if (_firstDayWindowStartUtcTicks == 0)
        {
            _firstDayWindowStartUtcTicks = DateTime.Now.Date.ToUniversalTime().Ticks;
        }
    }

    // 标记所有字段为已变更（用于强制同步）
    public void MarkAllAsChanged()
    {
        _changedFields.Add(nameof(levelNum));
        _changedFields.Add(nameof(infinitLevelNum));
        _changedFields.Add(nameof(coin));
        _changedFields.Add(nameof(time));
        _changedFields.Add(nameof(uid));
        _changedFields.Add(nameof(countryCode));
        _changedFields.Add(nameof(version));
        _changedFields.Add(nameof(continusWinCnt));
        _changedFields.Add(nameof(continusFailCnt));
        _changedFields.Add(nameof(continusWinLocalDate));
        _changedFields.Add(nameof(nextDayEaseAppliedLocalDate));
        _changedFields.Add(nameof(maxComboCnt));
        _changedFields.Add(nameof(levelPassActivityStage));
        _changedFields.Add(nameof(levelPassActivityPoint));
        _changedFields.Add(nameof(inGameItemHintCnt));
        _changedFields.Add(nameof(inGameItemShuffleCnt));
        _changedFields.Add(nameof(inGameItemUndoCnt));
        _changedFields.Add(nameof(inGameItemHighLightCnt));
        _changedFields.Add(nameof(ropeUndoCnt));
        _changedFields.Add(nameof(ropeHintCnt));
        _changedFields.Add(nameof(ropeExtraCnt));
        _changedFields.Add(nameof(levelPlayRecord));
        _changedFields.Add(nameof(levelTileBaseRecord));
        _changedFields.Add(nameof(knownTileBaseIds));
        _changedFields.Add(nameof(pendingPriorityTileBaseIds));
        _changedFields.Add(nameof(infinitLevelTileBaseRecord));
        _changedFields.Add(nameof(knownInfinitTileBaseIds));
        _changedFields.Add(nameof(pendingPriorityInfinitTileBaseIds));
        _changedFields.Add(nameof(tileBaseExcludeIdForNextInfinite));
        _changedFields.Add(nameof(tileBaseExcludeIdForNextFinite));
        _changedFields.Add(nameof(tileBaseExcludeFiniteLevel));
        _changedFields.Add(nameof(levelStyleRecord));
        _changedFields.Add(nameof(infinitLevelStyleRecord));
        _changedFields.Add(nameof(tileBaseStyleBag));
        _changedFields.Add(nameof(infinitTileBaseStyleBag));
        _changedFields.Add(nameof(noAd));
        _changedFields.Add(nameof(freeReviveUsedCnt));
        _changedFields.Add(nameof(maxFreeReviveCount));

        _changedFields.Add(nameof(videoPlayCntFirstDay));
        _changedFields.Add(nameof(firstDayWindowStartUtcTicks));
        _changedFields.Add(nameof(interADAbEligibleV3));
        _changedFields.Add(nameof(interADTestGroupV3));
        _changedFields.Add(nameof(interADCooldownUnixV3));
        _changedFields.Add(nameof(interADTestGroupV4));
        _changedFields.Add(nameof(interADGroupV5));
        _changedFields.Add(nameof(interADCooldownUnixV4));

    }

    // 清除变更标记。SaveData 写入 PlayerPrefs 后调用。
    public void ClearChanges()
    {
        _changedFields.Clear();
    }

    // 重置数据
    public void Reset()
    {
        _levelNum = 0;
        _infinitLevelNum = -1;
        _coin = 300;
        _time = 0;
        _uid = "";
        _countryCode = "";
        _version = 0;
        _continusWinCnt = 0;
        _continusFailCnt = 0;
        _continusWinLocalDate = 0;
        _nextDayEaseAppliedLocalDate = 0;
        _maxComboCnt = 0;
        _levelPassActivityStage = 1;
        _levelPassActivityPoint = 0f;
        _inGameItemHintCnt = 3;
        _inGameItemShuffleCnt = 3;
        _inGameItemUndoCnt = 3;
        _inGameItemHighLightCnt = 3;
        _ropeUndoCnt = 3;
        _ropeHintCnt = 3;
        _ropeExtraCnt = 3;
        _ropeItemsInited = 1;

        _bNoAd = false;
        _freeReviveUsedCnt = 0;
        _maxFreeReviveCount = GPUserDataManager.UnlockedMaxFreeReviveCount;
        _interADAbEligible = true;
        _interADTestGroup = "";
        _interADCooldownUnix = 0;
        _interADLoadLevelRecordDate = 0;
        _interADAbEligibleV3 = true;
        _interADTestGroupV3 = "";
        _interADCooldownUnixV3 = 0;
        _interADTestGroupV4 = "";
        _interADGroupV5 = "";
        _interADCooldownUnixV4 = 0;
        _levelPlayRecord.Clear();
        if (_levelTileBaseRecord == null)
            _levelTileBaseRecord = new List<LevelTileBaseRecord>();
        else
            _levelTileBaseRecord.Clear();
        if (_knownTileBaseIds == null)
            _knownTileBaseIds = new List<int>();
        else
            _knownTileBaseIds.Clear();
        if (_pendingPriorityTileBaseIds == null)
            _pendingPriorityTileBaseIds = new List<int>();
        else
            _pendingPriorityTileBaseIds.Clear();
        if (_infinitLevelTileBaseRecord == null)
            _infinitLevelTileBaseRecord = new List<LevelTileBaseRecord>();
        else
            _infinitLevelTileBaseRecord.Clear();
        if (_knownInfinitTileBaseIds == null)
            _knownInfinitTileBaseIds = new List<int>();
        else
            _knownInfinitTileBaseIds.Clear();
        if (_pendingPriorityInfinitTileBaseIds == null)
            _pendingPriorityInfinitTileBaseIds = new List<int>();
        else
            _pendingPriorityInfinitTileBaseIds.Clear();
        _tileBaseExcludeIdForNextInfinite = -1;
        _tileBaseExcludeIdForNextFinite = -1;
        _tileBaseExcludeFiniteLevel = -1;
        if (_levelStyleRecord == null)
            _levelStyleRecord = new List<LevelStyleRecord>();
        else
            _levelStyleRecord.Clear();
        if (_infinitLevelStyleRecord == null)
            _infinitLevelStyleRecord = new List<LevelStyleRecord>();
        else
            _infinitLevelStyleRecord.Clear();
        _tileBaseStyleBag = new TileBaseStyleBag();
        _infinitTileBaseStyleBag = new TileBaseStyleBag();
        _circleAwardCntList = new List<int>(6) { 0, 0, 0, 0, 0, 0 };
        _circleAwardLastResetTicks = DateTime.UtcNow.Ticks;
        ClearAllDailyVictoryData();
        _goldenTreeActPoint = 0;
        _goldenTreeTodayWinCnt = 0;
        _goldenTreeActWinCnt = 0;
        _goldenTreeActOpen = false;
        _goldenTreeBoardId = -1;
        _goldenTreeLastRank = -1;
        _goldenTreeGroupId = "";
        _goldenTreeOpenCnt = 0;
        _goldenTreeRobotScoreBoardId = -1;
        _goldenTreeRobotScoreJson = "";
        _goldenTreeRobotLastSettleTime = 0;
        _goldenTreeSettlementRewardGrantedBoardId = -1;
        _goldenTreeLeagueTier = 0;
        _goldenTreeLastSeasonLeagueTier = 0;
        _goldenTreeSettlementLeagueTier = 0;
        _goldenTreeSettlementTierAppliedBoardId = -1;
        _goldenTreeFirstTipsShown = false;
        _tipsDlgDailyOpen = "";
        if (_goldenTreeFailedLevelIds == null)
            _goldenTreeFailedLevelIds = new List<int>();
        else
            _goldenTreeFailedLevelIds.Clear();
        _videoPlayCntFirstDay = 0;
        _firstDayWindowStartUtcTicks = DateTime.Now.Date.ToUniversalTime().Ticks;
        _firstDayLocalDate = GetTodayLocalDateInt();
        _changedFields.Clear();
    }

    // ========================= 首日窗口与次数逻辑 =========================
    private int GetTodayLocalDateInt()
    {
        DateTime localNow = GameData.IsGmServerTimeOverride
            ? GameData.GetGameLocalDateTime()
            : DateTime.Now;
        return ToLocalDateInt(localNow);
    }

    private int ToLocalDateInt(DateTime local)
    {
        return local.Year * 10000 + local.Month * 100 + local.Day;
    }

    // 获取首日窗口起止（本地时间）
    public DateTime GetFirstDayWindowStartLocal()
    {
        // 返回首日当天的本地 00:00:00
        if (_firstDayLocalDate == 0)
        {
            _firstDayLocalDate = GetTodayLocalDateInt();
        }
        int y = _firstDayLocalDate / 10000;
        int m = (_firstDayLocalDate % 10000) / 100;
        int d = _firstDayLocalDate % 100;
        return new DateTime(y, m, d, 0, 0, 0);
    }


    public DateTime GetFirstDayWindowEndLocal()
    {
        return GetFirstDayWindowStartLocal().AddDays(1);
    }

    // 当前是否仍在首日窗口内
    public bool IsWithinFirstDayWindow()
    {
        if (!shouldCheckNewUser) //是否确认新用户
        {
            return false;
        }

        if (_firstDayLocalDate == 0)
        {
            _firstDayLocalDate = GetTodayLocalDateInt();
        }
        return GetTodayLocalDateInt() == _firstDayLocalDate;
    }

    // 首日视频次数+1（仅在首日窗口内有效）
    public void IncrementVideoPlayFirstDay(int delta = 1)
    {
        if (delta <= 0)
        {
            return;
        }
        if (IsWithinFirstDayWindow())
        {
            _videoPlayCntFirstDay += delta;
            _changedFields.Add(nameof(videoPlayCntFirstDay));
        }
    }

    // 距离首日窗口结束还剩余多少秒（过期则为0）
    public long GetSecondsUntilFirstDayEnd()
    {
        DateTime nowLocal = DateTime.Now;
        DateTime end = GetFirstDayWindowEndLocal();
        if (nowLocal >= end)
        {
            return 0;
        }
        return (long)(end - nowLocal).TotalSeconds;
    }
}