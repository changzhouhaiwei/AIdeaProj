using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EwingSoft.GameSDK;
using FishFramework;
using FishTilePlay;

/// <summary>
/// 游戏玩法用户数据管理器
/// 负责数据的存储和持久化
/// </summary>
public class GPUserDataManager : MonoBehaviour
{
    private static GPUserDataManager _instance;
    public static GPUserDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("GPUserDataManager");
                _instance = go.AddComponent<GPUserDataManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public static bool HasInit() => _instance != null;
    public static bool HasLocalData() => PlayerPrefs.HasKey(LOCAL_STORAGE_KEY);

    public static GPUserData GPUserData => Instance.UserData;

    [SerializeField] private GPUserData _userData;

    // 数据变更事件
    public event Action<string> OnDataChanged;

    public void SetGmForceDailyVictoryActEnabled(bool enabled)
    {
        _userData.gmForceDailyVictoryActEnabled = enabled;
    }

    public bool IsGmForceDailyVictoryActEnabled()
    {
        return _userData.gmForceDailyVictoryActEnabled;
    }

    public void SetPendingDailyVictorySettlementDlg(bool pending)
    {
        _userData.dailyVictoryPendingSettlementDlg = pending;
    }

    public bool ConsumePendingDailyVictorySettlementDlg()
    {
        if (!_userData.dailyVictoryPendingSettlementDlg)
        {
            return false;
        }

        _userData.dailyVictoryPendingSettlementDlg = false;
        SaveData();
        return true;
    }

    public void SetHadWinYesterdayBeforeTodayFirstWin(bool hadWinYesterday)
    {
        _userData.dailyVictoryHadWinYesterdayBeforeTodayFirstWin = hadWinYesterday;
    }

    public bool HadWinYesterdayBeforeTodayFirstWin()
    {
        return _userData.dailyVictoryHadWinYesterdayBeforeTodayFirstWin;
    }

    //
    private bool _BShopRed = true;
    public bool BShopRed
    {
        get
        {
            return _BShopRed;
        }

        set
        {
            _BShopRed = value;
            OnDataChanged?.Invoke("");
        }
    }
    // 自动保存间隔（秒）
    [SerializeField] private float _autoSaveInterval = 1f;
    private Coroutine _autoSaveCoroutine;

    // 本地存储键名
    private const string LOCAL_STORAGE_KEY = "TileData007";

    public GPUserData UserData => _userData;

    //广告临时存储
    private int m_willAddItemId;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartAutoSave();
    }

    private void OnDestroy()
    {
        StopAutoSave();
        SaveData();
        PlayerPrefs.Save();
    }

    // 移动端切后台/被系统回收前的可靠落盘时机：SetString 只写内存，必须 Save() 才落盘，
    // 否则本次会话内刚锁定的棋子底板等最近改动会在重启后丢失。
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveData();
            PlayerPrefs.Save();
        }
    }

    private void OnApplicationQuit()
    {
        SaveData();
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    private void InitializeData()
    {
        _userData = LoadDataFromLocal();
        if (_userData == null)
        {
            // 首次运行，创建默认数据并保存到PlayerPrefs
            _userData = new GPUserData();
            SaveData();
            Debug.Log("First time initialization: Default data created and saved to PlayerPrefs");
        }
        else
        {
            Debug.Log("Data loaded from PlayerPrefs successfully");
        }

        SyncToolLeft();
    }

    public string GetStorageKey()
    {
        return LOCAL_STORAGE_KEY;
    }

    // 兜底值：仅在极端情况下（GameSettings 完全取不到）使用，避免刚进游戏就误判为无尽模式。
    private const int FallbackMaxLevelNum = 1000;

    /// <summary>
    /// 有限关上限（进入无尽模式的起始关卡数）。按当前平台读取 GameSettings 中独立配置的
    /// Android / iOS 最大关卡数，不再依赖 LevelCatalog.AmountOfLevels。
    /// </summary>
    public int GetInfiniteModeStartLevelNum()
    {
        var settings = GameModule.Setting != null ? GameModule.Setting.GetGameSettings() : null;
        if (settings == null)
        {
            settings = Resources.Load<GameSettings>(nameof(GameSettings));
        }

        if (settings == null)
        {
            return FallbackMaxLevelNum;
        }

        var platform = LevelPlatformResolver.GetCurrentPlatform();
        return platform == LevelPlatform.IOS ? settings.iosMaxLevelNum : settings.androidMaxLevelNum;
    }

    /// <summary>
    /// 设置数据并触发变更事件
    /// </summary>
    public void SetLevelNum(int levelNum) //从0开始，那最大就是maxLevelNum但是不能使用
    {
        _userData.levelNum = levelNum;
        OnDataChanged?.Invoke(nameof(GPUserData.levelNum));
    }

    public void SetInfinitLevelNum(int infinitLevelNum)
    {
        _userData.infinitLevelNum = infinitLevelNum;
        OnDataChanged?.Invoke(nameof(GPUserData.infinitLevelNum));
    }

    public void InceaseLevelNum()
    {
        int cap = GetInfiniteModeStartLevelNum();
        _userData.levelNum++;
        if (_userData.levelNum >= cap)
        {
            int completedFiniteLevel = cap;
            int finiteTileBase = _userData.GetLevelTileBaseId(completedFiniteLevel);
            if (finiteTileBase >= 0)
            {
                _userData.SetTileBaseExcludeForNextInfinite(finiteTileBase);
            }

            _userData.levelNum = cap;
            IncreaseInfinitLevelNum();//增加无尽关卡数量
        }
        OnDataChanged?.Invoke(nameof(GPUserData.levelNum));
    }

    public void IncreaseInfinitLevelNum()
    {
        _userData.infinitLevelNum++;
        OnDataChanged?.Invoke(nameof(GPUserData.infinitLevelNum));
    }

    //实际关卡index
    public int GetLevelNum() //默认模式关卡index
    {
        return Mathf.Max(0, _userData.levelNum);
    }

    //展示index
    public int GetInfinitLevelNum()//无尽关卡index
    {
        return Mathf.Max(0, _userData.infinitLevelNum);
    }

    public int GetFinalLevelNum()
    {
        if (GetBInfinitMode())
        {
            int infiNum = GetInfinitLevelNum();
            return InfinitLevelFunc.Instance.GetRealLevelNumByIdx(infiNum + 1);
        }
        else
        {
            return GetLevelNum();
        }
    }


    //展示的管卡序号
    public int GetShowLevelNum()
    {
        if (GetBInfinitMode())
        {
            return GetInfinitLevelNum();
        }
        else
        {
            return GetLevelNum();
        }
    }

    public int GetPassLevelCnt()
    {
        return GetInfinitLevelNum() + GetLevelNum();
    }

    public bool GetBInfinitMode()
    {
        return GetLevelNum() >= GetInfiniteModeStartLevelNum();
    }

    public void SetCoin(int coin)
    {
        _userData.coin = coin;
        OnDataChanged?.Invoke(nameof(GPUserData.coin));
    }

    //加钱-减钱
    public void AddCoin(int coin)
    {
        _userData.coin += (int)coin;
        OnDataChanged?.Invoke(nameof(GPUserData.coin));
    }

    public void DelayAddCoin(int coin, float time)
    {
        Utility.DGDelayedCall(time, () =>
        {
            AddCoin(coin);
        });
    }

    public void DelayAddCoinBatched(int coin, float startDelay = 1.0f, float interval = 0.2f)
    {
        if (coin <= 0)
        {
            return;
        }

        int v1 = (int)(coin * 0.25f);
        int v2 = (int)(coin * 0.25f);
        int v3 = (int)(coin * 0.25f);
        int v4 = coin - v1 - v2 - v3;
        int[] batches = { v1, v2, v3, v4 };

        for (int i = 0; i < batches.Length; i++)
        {
            int addValue = batches[i];
            if (addValue <= 0)
            {
                continue;
            }

            Utility.DGDelayedCall(startDelay + i * interval, () =>
            {
                AddCoin(addValue);
            });
        }
    }

    public bool DelCoin(int coin)
    {
        if (_userData.coin >= coin)
        {
            _userData.coin -= (int)coin;
            OnDataChanged?.Invoke(nameof(GPUserData.coin));
            return true;
        }
        return false;
    }

    public int GetCoin()
    {
        return _userData.coin;
    }

    public void SetTime(int time)
    {
        _userData.time = time;
        OnDataChanged?.Invoke(nameof(GPUserData.time));
    }

    // UID相关方法
    public void SetUID(string uid)
    {
        _userData.uid = uid;
        OnDataChanged?.Invoke(nameof(GPUserData.uid));
    }

    public string GetUID()
    {
        return _userData.uid;
    }

    //countryCode
    public void SetCountryCode(string code)
    {
        Debug.Log("setCountryCode +++ " + code);

        if (code.Length > 0)
        {
            _userData.countryCode = code;
        }
    }

    public string GetCountryCode()
    {
        return _userData != null ? _userData.countryCode : "";
    }

    /// <summary>已写入有效两位国家码。unknown、空串都不算取到。</summary>
    public bool HasResolvedSdkCountryCode()
    {
        return IsResolvedCountryCode(GetCountryCode());
    }

    public static bool IsResolvedCountryCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        code = code.Trim();
        if (code.Equals("unknown", StringComparison.OrdinalIgnoreCase))
            return false;
        if (code.Length != 2)
            return false;

        return char.IsLetter(code[0]) && char.IsLetter(code[1]);
    }

    /// <summary>
    /// 进关 / 出关各请求一次 SDK getCountryCode。已有有效国家码则不再请求。
    /// 取到非 unknown 的两位码后写入用户数据。
    /// </summary>
    public void TryCaptureSdkCountryCode(string reason)
    {
        if (_userData == null)
            return;

        if (HasResolvedSdkCountryCode())
        {
            Debug.Log($"[国家码] {reason} 已保存={GetCountryCode()}，不再请求");
            return;
        }

        string raw = "unknown";
        try
        {
            raw = UtilSdk.GetCountryCode();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[国家码] " + reason + " 请求失败: " + e.Message);
        }

        if (!IsResolvedCountryCode(raw))
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log($"[国家码] {reason} sdk={(string.IsNullOrEmpty(raw) ? "empty" : raw)} 未取到，广告先按T3");
#else
            Debug.Log($"[国家码] {reason} sdk={(string.IsNullOrEmpty(raw) ? "empty" : raw)} 未取到");
#endif
            return;
        }

        string code = raw.Trim().ToUpperInvariant();
        SetCountryCode(code);
        SaveData();
        Debug.Log($"[国家码] {reason} sdk={raw} 已写入={code}，之后不再请求");
    }

    public bool GetBBRCountry()
    {
        return GetCountryCode().Equals("BR");
    }

    /// <summary>是否 T3 国家。系统 Locale 国家码，不用 SDK。见 GPSDKMgr.GetBT3Country。</summary>
    public bool GetBT3Country()
    {
        return GPSDKMgr.Instance != null && GPSDKMgr.Instance.GetBT3Country();
    }

    //广告增加
    public void SetAdAddItemId(int id)
    {
        m_willAddItemId = id;
    }



    public int GetAdAddItemId()
    {
        return m_willAddItemId;
    }

    public void AddItemByAd()
    {
        if (m_willAddItemId == 1)
        {
            AddHintItem();
        }
        else if (m_willAddItemId == 2)
        {
            AddShuffleItem();
        }
        else if (m_willAddItemId == 3)
        {
            AddUndoItem();
        }
        else if (m_willAddItemId == 5)
        {
            AddHighLightItem();
        }
    }

    //通过金币快速购买道具
    public void BuyHintItemDirectly(int price)
    {
        if (DelCoin(price))
        {
            AddHintItem();
        }
    }

    public void BuyShuffleItemDirectly(int price)
    {
        if (DelCoin(price))
        {
            AddShuffleItem();
        }
    }

    public void BuyUndoItemDirectly(int price)
    {
        if (DelCoin(price))
        {
            AddUndoItem();
        }
    }

    public void BuyHighLightItemDirectly(int price)
    {
        if (DelCoin(price))
        {
            AddHighLightItem();
        }
    }


    //通用增加用户道具信息
    public void AddCommonItemData(int id, int value)
    {
        int Return(Action<int> action, int arg)
        {
            action(arg);
            return 0;
        }

        _ = id switch
        {
            1 => Return(AddHintItem, value),
            2 => Return(AddShuffleItem, value),
            3 => Return(AddUndoItem, value),
            5 => Return(AddHighLightItem, value),
            _ => Return(AddCoin, value)
        };
    }

    public int GetItemNumberById(int id)
    {
        int v = id switch
        {
            1 => GetHintItemCount(),
            2 => GetShuffleItemCount(),
            3 => GetUndoItemCount(),
            5 => GetHighLightItemCount(),
            _ => GetCoin()
        };

        return v;
    }


    // 提示道具相关方法
    public void SetHintItemCount(int count)
    {
        _userData.inGameItemHintCnt = count;
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemHintCnt));
        SyncToolLeft();
    }

    public void AddHintItem(int count = 1)
    {
        _userData.AddHintItem(count);
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemHintCnt));
        SyncToolLeft();
    }

    public bool UseHintItem(int count = 1)
    {
        bool success = _userData.UseHintItem(count);
        if (success)
        {
            OnDataChanged?.Invoke(nameof(GPUserData.inGameItemHintCnt));
            SyncToolLeft();
        }
        return success;
    }

    public int GetHintItemCount()
    {
        return _userData.inGameItemHintCnt;
    }

    // 打乱道具相关方法
    public void SetShuffleItemCount(int count)
    {
        _userData.inGameItemShuffleCnt = count;
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemShuffleCnt));
        SyncToolLeft();
    }

    public void AddShuffleItem(int count = 1)
    {
        _userData.AddShuffleItem(count);
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemShuffleCnt));
        SyncToolLeft();
    }

    public bool UseShuffleItem(int count = 1)
    {
        bool success = _userData.UseShuffleItem(count);
        if (success)
        {
            OnDataChanged?.Invoke(nameof(GPUserData.inGameItemShuffleCnt));
            SyncToolLeft();
        }
        return success;
    }

    public int GetShuffleItemCount()
    {
        return _userData.inGameItemShuffleCnt;
    }

    // 撤销道具相关方法
    public void SetUndoItemCount(int count)
    {
        _userData.inGameItemUndoCnt = count;
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemUndoCnt));
        SyncToolLeft();
    }

    public void AddUndoItem(int count = 1)
    {
        _userData.AddUndoItem(count);
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemUndoCnt));
        SyncToolLeft();
    }

    public bool UseUndoItem(int count = 1)
    {
        bool success = _userData.UseUndoItem(count);
        if (success)
        {
            OnDataChanged?.Invoke(nameof(GPUserData.inGameItemUndoCnt));
            SyncToolLeft();
        }
        return success;
    }

    public int GetUndoItemCount()
    {
        return _userData.inGameItemUndoCnt;
    }

    // HighLight 道具相关方法
    public void SetHighLightItemCount(int count)
    {
        _userData.inGameItemHighLightCnt = count;
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemHighLightCnt));
        SyncToolLeft();
    }

    public void AddHighLightItem(int count = 1)
    {
        _userData.AddHighLightItem(count);
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemHighLightCnt));
        SyncToolLeft();
    }

    public bool UseHighLightItem(int count = 1)
    {
        bool success = _userData.UseHighLightItem(count);
        if (success)
        {
            OnDataChanged?.Invoke(nameof(GPUserData.inGameItemHighLightCnt));
            SyncToolLeft();
        }
        return success;
    }

    public int GetHighLightItemCount()
    {
        return _userData.inGameItemHighLightCnt;
    }


    public void EnableNoAd()
    {
        _userData.noAd = true;
        OnDataChanged?.Invoke(nameof(GPUserData.noAd));
    }

    public bool GetBNoAd()
    {
        return _userData.noAd;
    }

    public const int LegacyMaxFreeReviveCount = 5;
    /// <summary>历史新号默认上限；现仅作档位参考，新号初始为 UnlockedMaxFreeReviveCount。</summary>
    public const int NewMaxFreeReviveCount = 3;
    /// <summary>未锁定：首败时按关卡写入真实上限。</summary>
    public const int UnlockedMaxFreeReviveCount = -1;

    /// <summary>按首次失败时的展示关（1-based）返回终身免费复活上限。</summary>
    public static int ResolveFreeReviveCap(int displayLevel)
    {
        if (displayLevel <= 0)
            return 0;
        if (displayLevel <= 3)
            return 3;
        if (displayLevel <= 5)
            return 2;
        if (displayLevel <= 14)
            return 1;
        return 0;
    }

    /// <summary>主线首次正式失败时锁定上限；已锁定（含老档）不改。</summary>
    public void TryLockFreeReviveCapOnFirstFail(int displayLevel)
    {
        if (_userData.maxFreeReviveCount >= 0)
            return;

        _userData.maxFreeReviveCount = ResolveFreeReviveCap(displayLevel);
        OnDataChanged?.Invoke(nameof(GPUserData.maxFreeReviveCount));
    }

    public int GetFreeReviveRemainCount()
    {
        int cap = _userData.maxFreeReviveCount;
        // 未锁定：还没发额度，剩余 0（勿把 0/-1 退回老档 5）
        if (cap < 0)
            return 0;
        return Mathf.Max(0, cap - _userData.freeReviveUsedCnt);
    }

    public bool TryConsumeFreeRevive()
    {
        if (GetFreeReviveRemainCount() <= 0)
        {
            return false;
        }

        _userData.freeReviveUsedCnt++;
        OnDataChanged?.Invoke(nameof(GPUserData.freeReviveUsedCnt));
        return true;
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void SelfCheckFreeReviveCapTiers()
    {
        Debug.Assert(ResolveFreeReviveCap(1) == 3, "FreeReviveCap lv1");
        Debug.Assert(ResolveFreeReviveCap(3) == 3, "FreeReviveCap lv3");
        Debug.Assert(ResolveFreeReviveCap(4) == 2, "FreeReviveCap lv4");
        Debug.Assert(ResolveFreeReviveCap(5) == 2, "FreeReviveCap lv5");
        Debug.Assert(ResolveFreeReviveCap(6) == 1, "FreeReviveCap lv6");
        Debug.Assert(ResolveFreeReviveCap(14) == 1, "FreeReviveCap lv14");
        Debug.Assert(ResolveFreeReviveCap(15) == 0, "FreeReviveCap lv15");
    }
#endif

    // 重置所有道具
    public void ResetAllItems()
    {
        _userData.ResetAllItems();
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemHintCnt));
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemShuffleCnt));
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemUndoCnt));
        OnDataChanged?.Invoke(nameof(GPUserData.inGameItemHighLightCnt));
        SyncToolLeft();
    }

    void SyncToolLeft()
    {
        GameBAData.SyncToolLeft();
    }

    public void IncrementWinCount()
    {
        _userData.IncrementWinCount();
        OnDataChanged?.Invoke(nameof(GPUserData.continusWinCnt));
    }


    public void IncrementFailCount()
    {
        _userData.IncrementFailCount();
        OnDataChanged?.Invoke(nameof(GPUserData.continusFailCnt));
    }

    public int GetMaxComboCnt()
    {
        return _userData.maxComboCnt;
    }

    public void TryUpdateMaxCombo(int comboSum)
    {
        if (comboSum <= _userData.maxComboCnt)
            return;

        _userData.maxComboCnt = comboSum;
        OnDataChanged?.Invoke(nameof(GPUserData.maxComboCnt));
    }


    //增加过关的积分
    public void IncrementLevelPassPoint(float addPoint)
    {
        _userData.levelPassActivityPoint += addPoint;
        OnDataChanged?.Invoke(nameof(GPUserData.levelPassActivityPoint));
    }

    public float GetLevelPassPoint()
    {
        return UserData.levelPassActivityPoint;
    }

    public void ResetLevelPassPoint()
    {
        _userData.levelPassActivityPoint = 0;
        OnDataChanged?.Invoke(nameof(GPUserData.levelPassActivityPoint));
    }

    //自增过关阶段
    public void IncrementLevelPassStage()
    {
        int maxStage = GameData.LevelPass.GetMaxStage();
        _userData.levelPassActivityStage++;
        _userData.levelPassActivityStage = Math.Min(maxStage, _userData.levelPassActivityStage);

        OnDataChanged?.Invoke(nameof(GPUserData.levelPassActivityStage));
    }


    //刷新数据
    public void CheckSetMediaSourceType()
    {
        if (GPSDKMgr.Instance.GetBIsMediaSourceType1())
        {
            GPUserData.adsourceType = 1;
        }
    }

    public bool GetAdSourceTypeB1()
    {
        return GPUserData.adsourceType == 1;
    }

    private void AddVersion()
    {
        _userData.version = _userData.version + 1;
    }

    public int GetVersion()
    {
        return _userData.version;
    }


    private string gameVersionKey = "gameVersionKey";
    public string GetLocalGameVersion()
    {
        return PlayerPrefs.GetString(gameVersionKey, "");
    }

    public string SaveLocalGameVersion()
    {
        string str = GameModule.Setting.GetGameSettings().GetVersion();
        PlayerPrefs.SetString(gameVersionKey, str);
        return str;
    }



    /// <summary>
    /// 记录关卡进入次数
    /// </summary>
    /// <param name="levelId">关卡ID</param>
    public void RecordLevelEntry(int levelId)
    {
        _userData.IncrementLevelPlayCount(levelId);
        OnDataChanged?.Invoke(nameof(GPUserData.levelPlayRecord));
    }

    /// <summary>
    /// 获取关卡进入次数
    /// </summary>
    /// <param name="levelId">关卡ID</param>
    /// <returns>进入次数</returns>
    public int GetLevelPlayCount(int levelId)
    {
        return _userData.GetLevelPlayCount(levelId);
    }

    public bool IsGoldenTreeLevelFailed(int levelId)
    {
        return _userData.IsGoldenTreeLevelFailed(levelId);
    }

    public void MarkGoldenTreeLevelFailed(int levelId)
    {
        _userData.MarkGoldenTreeLevelFailed(levelId);
        OnDataChanged?.Invoke(nameof(GPUserData.goldenTreeFailedLevelIds));
    }

    public void ClearGoldenTreeFailedLevels()
    {
        _userData.ClearGoldenTreeFailedLevels();
        OnDataChanged?.Invoke(nameof(GPUserData.goldenTreeFailedLevelIds));
    }

    /// <summary>
    /// 获取所有关卡记录
    /// </summary>
    /// <returns>关卡记录列表</returns>
    public List<LevelPlayRecord> GetAllLevelRecords()
    {
        return _userData.levelPlayRecord;
    }

    /// <summary>
    /// 获取关卡记录摘要信息
    /// </summary>
    /// <returns>关卡记录摘要字符串</returns>
    public string GetLevelRecordsSummary()
    {
        if (_userData.levelPlayRecord == null || _userData.levelPlayRecord.Count == 0)
        {
            return "无关卡记录";
        }

        var summary = new System.Text.StringBuilder("关卡记录: ");
        for (int i = 0; i < _userData.levelPlayRecord.Count; i++)
        {
            var record = _userData.levelPlayRecord[i];
            summary.Append($"关卡{record.levelId}:{record.playCount}次");
            if (i < _userData.levelPlayRecord.Count - 1)
            {
                summary.Append(", ");
            }
        }
        return summary.ToString();
    }

    /// <summary>
    /// 清除指定关卡的记录
    /// </summary>
    /// <param name="levelId">关卡ID</param>
    public void ClearLevelRecord(int levelId)
    {
        if (_userData.levelPlayRecord != null)
        {
            for (int i = _userData.levelPlayRecord.Count - 1; i >= 0; i--)
            {
                if (_userData.levelPlayRecord[i].levelId == levelId)
                {
                    _userData.levelPlayRecord.RemoveAt(i);
                    OnDataChanged?.Invoke(nameof(GPUserData.levelPlayRecord));
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 清除所有关卡记录
    /// </summary>
    public void ClearAllLevelRecords()
    {
        if (_userData.levelPlayRecord != null)
        {
            _userData.levelPlayRecord.Clear();
            OnDataChanged?.Invoke(nameof(GPUserData.levelPlayRecord));
        }
    }


    /// <summary>
    /// 保存数据到本地
    /// </summary>
    public void SaveData()
    {
        try
        {
            AddVersion();
            string json = _userData.GetFullDataJson();
            PlayerPrefs.SetString(LOCAL_STORAGE_KEY, json);
            // version 递增会留下脏标记。这里清掉，否则自动保存会每秒重写整包。
            _userData.ClearChanges();
            // PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save GPUserData: {e.Message}");
        }
    }

    //获得json数据字符串
    public string GetFullDataJson()
    {
        return _userData.GetFullDataJson();
    }


    /// <summary>
    /// 从本地加载数据
    /// </summary>
    private GPUserData LoadDataFromLocal()
    {
        try
        {
            if (PlayerPrefs.HasKey(LOCAL_STORAGE_KEY))
            {
                string json = PlayerPrefs.GetString(LOCAL_STORAGE_KEY);
                var data = new GPUserData();
                data.LoadFromJson(json);
                Debug.Log("GPUserData loaded from local storage");
                return data;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load GPUserData: {e.Message}");
        }

        return null;
    }

    /// <summary>
    /// 开始自动保存
    /// </summary>
    private void StartAutoSave()
    {
        StopAutoSave();
        _autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
    }

    /// <summary>
    /// 停止自动保存
    /// </summary>
    private void StopAutoSave()
    {
        if (_autoSaveCoroutine != null)
        {
            StopCoroutine(_autoSaveCoroutine);
            _autoSaveCoroutine = null;
        }
    }

    /// <summary>
    /// 自动保存协程
    /// </summary>
    private IEnumerator AutoSaveCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_autoSaveInterval);

            if (_userData.HasChanges)
            {
                SaveData();
            }
        }
    }


    /// <summary>
    /// 重置数据
    /// </summary>
    public void ResetData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        _userData.Reset();
        // Reset() 末尾会清掉变更标记；再显式清当日连胜，保证 UI / 存档都拿到 0
        _userData.ResetWinFailCount();
        SetPendingDailyVictorySettlementDlg(false);
        SetHadWinYesterdayBeforeTodayFirstWin(false);
        // 数据被重置，棋子背景会话状态需重新初始化
        FishTilePlay.TileBaseThemeResolver.ResetSession();
        // 狗狗大冒险独立 PlayerPrefs + 静态缓存；DeleteAll 清磁盘后仍需清内存，否则倒计时会被写回
        PuppyAdventureFlow.ClearPending();
        PuppyAdventureDemoState.ResetAll();
        PiggyBankState.ResetAll();
        // 重置后立即保存默认值到PlayerPrefs
        SaveData();
        OnDataChanged?.Invoke(nameof(GPUserData.continusWinCnt));
        OnDataChanged?.Invoke(nameof(GPUserData.continusFailCnt));
        OnDataChanged?.Invoke("reset");
        SyncToolLeft();
    }

    /// <summary>
    /// 初始化默认数据并保存到PlayerPrefs
    /// </summary>
    public void InitializeDefaultData()
    {
        _userData = new GPUserData();
        // 数据实例被替换，棋子背景会话状态需重新初始化
        FishTilePlay.TileBaseThemeResolver.ResetSession();
        SaveData();
        SyncToolLeft();
        Debug.Log("Default data initialized and saved to PlayerPrefs");
    }

    public void SyncDataFromServer(GPUserData svrData)
    {
        _userData = svrData;
        // 数据实例被替换（云同步/换号），棋子背景会话状态需重新初始化
        FishTilePlay.TileBaseThemeResolver.ResetSession();
        SaveData();
        SyncToolLeft();
    }

    /// <summary>
    /// 获取数据摘要（用于调试）
    /// </summary>
    public string GetDataSummary()
    {
        return $"Level: {_userData.levelNum}, Score: {_userData.coin}, Time: {_userData.time}, UID: {_userData.uid}, " +
               $"Hint: {_userData.inGameItemHintCnt}, Shuffle: {_userData.inGameItemShuffleCnt}, Undo: {_userData.inGameItemUndoCnt}, HighLight: {_userData.inGameItemHighLightCnt}, " +
               $"HasChanges: {_userData.HasChanges}\n";
    }

    public bool IsAvatarFrameUnlocked(int frameId)
    {
        if (frameId <= 0)
        {
            return false;
        }

        var cfg = ConfigHelper.Tables.TbAvatarFrameCfg.GetOrDefault(frameId);
        if (cfg == null)
        {
            return false;
        }

        if (cfg.IsLimit == 0)
        {
            return true;
        }

        // 连胜头像框：总胜场 ≥ 7 即可佩戴（与 honor 门槛一致）。
        // 不依赖是否点过第 7 天领取，避免 GM 设连胜 / 漏领导致有天数却换不了框。
        if (IsWinStreakAvatarFrameId(frameId))
        {
            return _userData != null && _userData.dailyVictoryTotalWinDays >= 7;
        }

        var record = FindAvatarFrameRecord(frameId);
        return record != null && record.expireUtcTicks > DateTime.UtcNow.Ticks;
    }

    public TimeSpan? GetAvatarFrameRemain(int frameId)
    {
        var cfg = ConfigHelper.Tables.TbAvatarFrameCfg.GetOrDefault(frameId);
        if (cfg == null || cfg.IsLimit == 0)
        {
            return null;
        }

        var record = FindAvatarFrameRecord(frameId);
        if (record == null || record.expireUtcTicks <= DateTime.UtcNow.Ticks)
        {
            return null;
        }

        return TimeSpan.FromTicks(record.expireUtcTicks - DateTime.UtcNow.Ticks);
    }

    public static string FormatAvatarFrameRemain(TimeSpan remain)
    {
        if (remain.TotalDays >= 1)
        {
            return $"{Math.Min(7, (int)remain.TotalDays)}D";
        }

        if (remain.TotalHours >= 1)
        {
            return $"{Math.Min(24, (int)remain.TotalHours)}H";
        }

        return $"{Math.Max(1, Math.Min(60, (int)remain.TotalMinutes))}M";
    }

    public string GetAvatarFrameRemainText(int frameId)
    {
        var remain = GetAvatarFrameRemain(frameId);
        return remain.HasValue ? FormatAvatarFrameRemain(remain.Value) : string.Empty;
    }

    public void GrantAvatarFrame(int frameId)
    {
        var cfg = ConfigHelper.Tables.TbAvatarFrameCfg.GetOrDefault(frameId);
        if (cfg == null || cfg.IsLimit == 0)
        {
            return;
        }

        // 连胜头像框：持续连胜期间不过期，断连胜时由 RevokeWinStreakAvatarFrames 收回
        long expireUtcTicks = IsWinStreakAvatarFrameId(frameId) || cfg.LimitDay <= 0
            ? DateTime.MaxValue.Ticks
            : DateTime.UtcNow.AddDays(cfg.LimitDay).Ticks;

        var ownedFrames = _userData.ownedAvatarFrames;
        var record = FindAvatarFrameRecord(frameId);
        if (record != null)
        {
            record.expireUtcTicks = expireUtcTicks;
        }
        else
        {
            ownedFrames.Add(new AvatarFrameRecord
            {
                frameId = frameId,
                expireUtcTicks = expireUtcTicks,
            });
        }

        _userData.userframe = frameId;
        SaveData();
        GameEvent.Send((int)EventNameDef.UserFrameChange);
    }

    /// <summary>
    /// 断开每日连胜后收回连胜头像框，需重新连到第 7 天才能领取。
    /// </summary>
    public void RevokeWinStreakAvatarFrames()
    {
        if (_userData == null)
        {
            return;
        }

        var ownedFrames = _userData.ownedAvatarFrames;
        bool changed = false;
        for (int i = ownedFrames.Count - 1; i >= 0; i--)
        {
            if (IsWinStreakAvatarFrameId(ownedFrames[i].frameId))
            {
                ownedFrames.RemoveAt(i);
                changed = true;
            }
        }

        if (_userData.userframe > 0 && IsWinStreakAvatarFrameId(_userData.userframe))
        {
            _userData.userframe = GPUserData.DefaultAvatarFrameId;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        SaveData();
        GameEvent.Send((int)EventNameDef.UserFrameChange);
    }

    public static bool IsWinStreakAvatarFrameId(int frameId)
    {
        if (frameId <= 0 || ConfigHelper.Tables?.TbDailyVictoryRewardCfg?.DataList == null)
        {
            return false;
        }

        foreach (var cfg in ConfigHelper.Tables.TbDailyVictoryRewardCfg.DataList)
        {
            if (cfg.AvatarFrameId == frameId)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasActiveWinStreakAvatarFrame()
    {
        // 连胜未满 7 天：不算拥有，honor 也不应显示
        if (_userData == null || _userData.dailyVictoryTotalWinDays < 7)
        {
            return false;
        }

        if (ConfigHelper.Tables?.TbDailyVictoryRewardCfg?.DataList == null)
        {
            return false;
        }

        foreach (var cfg in ConfigHelper.Tables.TbDailyVictoryRewardCfg.DataList)
        {
            if (cfg.AvatarFrameId > 0 && IsAvatarFrameUnlocked(cfg.AvatarFrameId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 连胜总天数 &lt; 7 时强制收回连胜头像框。
    /// </summary>
    public void SyncWinStreakAvatarFrameWithStreak()
    {
        if (_userData == null || _userData.dailyVictoryTotalWinDays >= 7)
        {
            return;
        }

        RevokeWinStreakAvatarFrames();
    }

    public void TryCleanupExpiredAvatarFrames()
    {
        var ownedFrames = _userData.ownedAvatarFrames;
        if (ownedFrames.Count == 0)
        {
            return;
        }

        long nowTicks = DateTime.UtcNow.Ticks;
        bool removed = false;
        for (int i = ownedFrames.Count - 1; i >= 0; i--)
        {
            if (ownedFrames[i].expireUtcTicks <= nowTicks)
            {
                ownedFrames.RemoveAt(i);
                removed = true;
            }
        }

        if (removed)
        {
            SaveData();
        }

        if (_userData.userframe > 0 && !IsAvatarFrameUnlocked(_userData.userframe))
        {
            _userData.userframe = GPUserData.DefaultAvatarFrameId;
            SaveData();
            GameEvent.Send((int)EventNameDef.UserFrameChange);
        }
    }

    private AvatarFrameRecord FindAvatarFrameRecord(int frameId)
    {
        var ownedFrames = _userData.ownedAvatarFrames;
        for (int i = 0; i < ownedFrames.Count; i++)
        {
            if (ownedFrames[i].frameId == frameId)
            {
                return ownedFrames[i];
            }
        }

        return null;
    }
}