using System;
using System.Collections.Generic;
using FishXSortPlay;
using UnityEngine;
using FishFramework;
using EwingSoft.GameSDK;
#if UNITY_IOS
using FirebaseSdk = EwingSoft.GameSDK.IosBridge.FirebaseSdk;
using AttrSdk = EwingSoft.GameSDK.IosBridge.AttrSdk;
using ThinkingDataSdk = EwingSoft.GameSDK.IosBridge.ThinkingDataSdk;
#endif

public static partial class GameBAData
{
    private static BAGuideData BAGuide = new();
    private static BALoadingData BALoading = new();
    private static BAMainStageData BAMainStage = new();
    private static BAStageEventDta BAStageEvent = new();
    private static BATkCommonData BATkCommon = new();
    private static BAADImporessData BAADImporessData = new();
    private static BAADClickData BAADClickData = new();
    private static BAADNoShowData BAADNoShowData = new();
    private static BAInterAdNoShowData BAInterAdNoShow = new();
    private static BAGameTriggerGiftData BAGameTriggerGiftData = new();//触发条件礼包
    private static BAGameTriggerEventData BAGameTriggerEventData = new();//触发事件弹窗
    private static BAGameTriggerActivityData BAGameTriggerActivityData = new();

    private static BACreateOrderData BACreateOrder = new();
    private static BAPaymentResultData BAPaymentResult = new();
    private static BAAdLoadData BAAdLoad = new();
    private static BAPolicyData BAPolicyData = new();//隐私弹窗
    private static BACommentData BACommentData = new();//评分（XSort2 评弹窗仍上报）
    private static BASignData BASignData = new();//火花连胜签到页
    private static BAMainStageRewardData BAMainStageReward = new();//结算额外奖励
    private static BARankCloseData BARankCloseData = new();
    private static BAActivityData BAActivityData = new();
    private static BAEggSmashData BAEggSmashData = new();

    private static float m_levelStartTime;
    private static int m_operCnt;//操作次数
    private static int ad_num;//局内激励广告次数
    // 本局是否无尽。进关时锁定；胜利打点晚于 InceaseLevelNum，不能用打点当时的模式。
    private static bool s_sessionInfinite;

    private static int m_clearItemCnt;//单次操作消除记录
    private static int m_skillItemReleaseCnt;//单次操作技能释放记录
    private static int m_skillItemCombineCnt;//单次操作合成记录
    private static bool m_justHurtObstacle;//单次操作是否消除障碍物记录



    private static Dictionary<int, int> m_toolUseDic = new();

    /*
        public string guide_id;
        public string guide_name;
        public string guide_state;
    */
    public static void ExcuteGuide(string _guideId, string _guideName, string _guideState)
    {
        BAGuide.ClearData();

        BAGuide.guide_id = _guideId;
        BAGuide.guide_name = _guideName;
        BAGuide.guide_state = _guideState;

        GPSDKMgr.Instance.TrackEvent(BAGuide.key, BAGuide.ToJson());
    }

    /*
    public readonly string key = "loading_event";
    public string client_event_fail_code;
    public string client_event_fail_msg;
    public string client_event_id;
    public string client_event_name;
    public bool is_key_event_step;
    public string app_version;
    */
    public static void ExcuteLoading(string failCode, string failMsg, string eventId, string eventName, bool isKeyEventStep)
    {
        BALoading.client_event_fail_code = failCode;
        BALoading.client_event_fail_msg = failMsg;
        BALoading.client_event_id = eventId;
        BALoading.client_event_name = eventName;
        BALoading.is_key_event_step = isKeyEventStep;
        BALoading.app_version = GameModule.Setting.GetGameSettings().GetVersion();

        // BALoadingData
        GPSDKMgr.Instance.TrackEvent(BALoading.key, BALoading.ToJson());
    }

    public static void ExcuteOnline()//每一分钟上报一次
    {
        GPSDKMgr.Instance.TrackEvent("online", "");
    }

    public static int GetCurrentCostTime()
    {
        var endTime = Time.realtimeSinceStartup;
        return (int)(endTime - m_levelStartTime);
    }


    public static void AddRewardAdInOneLevel()
    {
        ad_num++;
    }

    /// <summary>
    /// 0/8/9/10 是进关，用当时的无尽状态并锁住；胜利/失败等沿用进关时的锁。
    /// 最后一关有限关清盘会先 InceaseLevelNum，胜利晚 1 秒才打，不能重读。
    /// </summary>
    internal static bool ResolveSessionInfinite(int status, bool liveInfinite, bool latched)
    {
        if (status == 0 || status == 8 || status == 9 || status == 10)
            return liveInfinite;
        return latched;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void SelfCheckSessionInfinite()
    {
        Debug.Assert(!ResolveSessionInfinite(0, false, false), "finite enter");
        Debug.Assert(!ResolveSessionInfinite(1, true, false), "last finite win stays main_stage");
        Debug.Assert(ResolveSessionInfinite(0, true, false), "infinite enter");
        Debug.Assert(ResolveSessionInfinite(1, true, true), "infinite win");
    }

    //3_4_5_7状态没有
    public static void ExcuteMainStage(int status, int recoverCostTime = 0)//0 进入 1胜利 2失败 4.中途退出 6复活 8 重新自动开始 9 重新开始 
    {
        if (!GameData.BFromStartScene) return;

        s_sessionInfinite = ResolveSessionInfinite(status, GPUserDataManager.Instance.GetBInfinitMode(), s_sessionInfinite);

        //判定开始结束
        if (status == 0 || status == 8 || status == 9) //开始
        {
            BAMainStage.cost_time = 0;
            m_operCnt = 0;
            m_toolUseDic.Clear();
            ad_num = 0; //清理局内次数
            SetLevelStartTime(); //统计开始时间 
        }
        else if (status == 10)//从存档开始，需要获取上次的cost_time
        {
            BAMainStage.is_saved = true;
            BAMainStage.cost_time = recoverCostTime;
            SetLevelStartTime(); //统计开始时间             
        }
        else
        {
            var endTime = Time.realtimeSinceStartup;
            BAMainStage.cost_time = (int)(endTime - m_levelStartTime);
            PlayerPrefs.Save();
        }


        int levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
        int levelId = levelIndex + 1;

        BAMainStage.stage_id = levelId.ToString();
        BAMainStage.stage_form_id = LevelDirector.LevelGenService.GetDynamicCfgStr(); //"1";
        FillPlayType(BAMainStage.playtype);
        BAMainStage.is_nonet = NetDetectController.GetIsLoseNet();
        BAMainStage.XSortbase = LevelDirector.LoadedLevelTheme.ToString();
        int XSortBaseStyle = LevelDirector.LoadedXSortBaseStyle;
        BAMainStage.XSortbase2 = XSortBaseStyle < 0
            ? "theme"
            : (XSortBaseStyleTable.NameAt(XSortBaseStyle) ?? "");
        BAMainStage.round = GPUserDataManager.Instance.GetLevelPlayCount(levelIndex);//第多少次进入关卡

        //进入关卡时连胜连败计数

        var winCnt = GPUserDataManager.Instance.UserData.continusWinCnt;
        var failCnt = GPUserDataManager.Instance.UserData.continusFailCnt;
        BAMainStage.straight_win = 0;

        if (winCnt > 0)
        {
            BAMainStage.straight_win = winCnt;
        }
        else if (failCnt > 0)
        {
            BAMainStage.straight_win = -1 * failCnt;
        }

        var dynamicData = LevelDirector.LevelGenService.GetDynamicData();

        //开局棋子类型数
        BAMainStage.type = dynamicData.number;
        //总棋子数
        BAMainStage.XSort = LevelDirector.Level.GetAmountOfFilledCells();
        //总层数
        BAMainStage.layer = LevelDirector.Level.AmountOfLayers;
        //XSortLeft
        BAMainStage.XSort_left = LevelDirector.LevelGenService.GetRestXSortCount();
        //剩余层数
        BAMainStage.layer_left = LevelDirector.LevelGenService.GetRestLayerCount();
        //暂存区棋子数
        BAMainStage.temp = LevelDirector.Dock.GetOccupiedSlotsCount();
        FillFailChessLists(status);
        //操作次数 包含正常点击棋子和技能点击.
        BAMainStage.count = m_operCnt;
        //局内广告播放次数
        BAMainStage.ad_num = ad_num;

        BAMainStage.is_skip = false;
        BAMainStage.status = status;
        BAMainStage.rank = "";
        BAMainStage.rank2 = "";
        BAMainStage.challenge_score_cur = "";
        BAMainStage.challenge_score = "";
        BAMainStage.unique_id = "";

        foreach (var item in m_toolUseDic)
        {
            var oneData = new BaseAnaData.BAMetaData
            {
                id = item.Key.ToString(),
                num = item.Value
            };
            BAMainStage.tool_use.Add(oneData);
        }

        string eventkey = BAMainStage.key;
        if (s_sessionInfinite)
        {
            eventkey = "infinity_stage";
        }
        GPSDKMgr.Instance.TrackEvent(eventkey, BAMainStage.ToJson());
        // 只有进入（重新开始不算进入）才刷新 last_stage / last_stage_form
        if (status == 0)
            ExcuteCommonEvent(BAMainStage.stage_id, BAMainStage.stage_form_id);
        else
            ExcuteCommonEvent();

    }

    /*
        1 - 三消
        2 - 使用道具
        3 - 免费复活
        4 - 广告复活
        5 - 金币购买空格 无
        6 - 广告获得空格 无
        
        7 - 金币购买道具 暂无
        8 - 广告获得道具 暂无
        9 - 空间不足（未失败）

        12 - 出现广告提示.
    */
    /// <summary>
    /// 0 普通，1 boxcover，2 彩蛋，3 IQ，4 颜色挑战。同时开着的都报，没有特殊玩法才报 0。
    /// </summary>
    static void FillPlayType(List<string> dest)
    {
        dest.Clear();
        if (BoxCoverManager.HasActiveLevel)
            dest.Add("1");
        if (AwardColorEggCoverManager.HasActiveLevel)
            dest.Add("2");
        if (IQChallengeManager.HasActiveLevel)
            dest.Add("3");
        int levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
        if (LevelDirector.UsesColorChallenge(levelIndex))
            dest.Add("4");
        if (dest.Count == 0)
            dest.Add("0");
    }

    /// <summary>
    /// status=2/6：暂存区从左到右。cost = 凑满三连还缺的颗数 + 把缺的埋棋揭开必须先点掉的盖子数。
    /// </summary>
    static void FillFailChessLists(int status)
    {
        BAMainStage.chess_list_fail.Clear();
        BAMainStage.chess_cost_fail.Clear();
        if (status != 2 && status != 6)
            return;

        DockActor dock = LevelDirector.Dock;
        LevelGenService gen = LevelDirector.LevelGenService;
        if (dock == null || gen == null)
            return;

        var dockXSorts = new List<XSortActor>();
        dock.CopyOccupiedXSortsLeftToRight(dockXSorts);
        if (dockXSorts.Count == 0)
            return;

        var typeCounts = new Dictionary<int, int>();
        for (int i = 0; i < dockXSorts.Count; i++)
        {
            int typeId = dockXSorts[i].MatchTypeId;
            typeCounts.TryGetValue(typeId, out int n);
            typeCounts[typeId] = n + 1;
        }

        List<XSortActor> actXSorts = gen.ActXSorts;
        var costByType = new Dictionary<int, int>();
        foreach (var pair in typeCounts)
        {
            int shortage = Mathf.Max(0, 3 - pair.Value);
            int buriedNeed = shortage;
            if (shortage > 0 && actXSorts != null)
            {
                int clickable = 0;
                for (int i = 0; i < actXSorts.Count; i++)
                {
                    XSortActor XSort = actXSorts[i];
                    if (XSort != null
                        && XSort.XSortModel != null
                        && XSort.MatchTypeId == pair.Key
                        && PlayBotClickCost.CanPlayerClick(XSort))
                        clickable++;
                }
                buriedNeed = Mathf.Max(0, shortage - clickable);
            }

            int extra = 0;
            if (buriedNeed > 0
                && actXSorts != null
                && PlayBotClickCost.TryGetUncover(pair.Key, buriedNeed, actXSorts, out int uncover))
                extra = uncover;

            costByType[pair.Key] = shortage + extra;
        }

        for (int i = 0; i < dockXSorts.Count; i++)
        {
            XSortActor XSort = dockXSorts[i];
            BAMainStage.chess_list_fail.Add(XSort.XSortModel.XSortIndex);
            BAMainStage.chess_cost_fail.Add(costByType[XSort.MatchTypeId]);
        }
    }

    public static void ExcuteStageEvent(int status, int tool_id = 0)
    {
        if (!GameData.BFromStartScene) return;

        int levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
        int levelId = levelIndex + 1;

        BAStageEvent.stage_id = levelId.ToString();
        BAStageEvent.stage_form_id = LevelDirector.LevelGenService.GetDynamicCfgStr();
        FillPlayType(BAStageEvent.playtype);
        BAStageEvent.is_nonet = NetDetectController.GetIsLoseNet();
        BAStageEvent.XSortbase = LevelDirector.LoadedLevelTheme.ToString();

        BAStageEvent.round = GPUserDataManager.Instance.GetLevelPlayCount(levelIndex);//第多少次进入关卡
        //连胜连败数据
        var winCnt = GPUserDataManager.Instance.UserData.continusWinCnt;
        var failCnt = GPUserDataManager.Instance.UserData.continusFailCnt;
        BAStageEvent.straight_win = 0;
        if (winCnt > 0)
        {
            BAStageEvent.straight_win = winCnt;
        }
        else if (failCnt > 0)
        {
            BAStageEvent.straight_win = -1 * failCnt;
        }

        var dynamicData = LevelDirector.LevelGenService.GetDynamicData();
        //开局棋子类型数
        BAStageEvent.type = dynamicData.number;
        BAStageEvent.XSort = LevelDirector.Level.GetAmountOfFilledCells();
        //总层数
        BAStageEvent.layer = LevelDirector.Level.AmountOfLayers;
        //XSortLeft
        BAStageEvent.XSort_left = LevelDirector.LevelGenService.GetRestXSortCount();
        //剩余层数
        BAStageEvent.layer_left = LevelDirector.LevelGenService.GetRestLayerCount();
        //暂存区棋子数
        BAStageEvent.temp = LevelDirector.Dock.GetOccupiedSlotsCount();
        //局内广告播放次数
        BAStageEvent.ad_num = ad_num;

        //操作次数
        BAStageEvent.count = m_operCnt;
        BAStageEvent.status = status;


#if UNITY_EDITOR
        int total = BAStageEvent.temp + BAStageEvent.XSort_left;
        if (total % 3 != 0)
        {
            Debug.LogError("出现异常=-=");
            AlertDlg.ShowDlg("error error");
        }
#endif

        var endTime = Time.realtimeSinceStartup;
        BAStageEvent.cost_time = (int)(endTime - m_levelStartTime);
        BAStageEvent.tool_id = tool_id;

        BAStageEvent.tool_use.Clear();
        foreach (var item in m_toolUseDic)
        {
            var oneData = new BaseAnaData.BAMetaData
            {
                id = item.Key.ToString(),
                num = item.Value
            };
            BAStageEvent.tool_use.Add(oneData);
        }

        BAStageEvent.is_saved = BAMainStage.is_saved;

        string eventkey = BAStageEvent.key;
        if (s_sessionInfinite)
        {
            eventkey = "infinity_stage_event";
        }
        GPSDKMgr.Instance.TrackEvent(eventkey, BAStageEvent.ToJson());
    }

    //广告位埋点
    public static void ExcuteAdImpress(string placeStr = "")
    {
        BAADImporessData.ad_place = placeStr;
        GPSDKMgr.Instance.TrackEvent("ad_impress", BAADImporessData.ToJson());
    }

    public static void ExcuteAdClick(string placeStr = "")
    {
        BAADClickData.ad_place = placeStr;

        GPSDKMgr.Instance.TrackEvent("ad_click", BAADClickData.ToJson());
    }

    private static string lastNoAdShowKey = "";
    public static void ExcuteAdNoShow(string adTypeStr = "", string placeStr = "", string reason = "")//REWARDED/INTER
    {
        if (string.IsNullOrEmpty(reason))
        {
            reason = GPSDKMgr.Instance.GetAdNoShowReason(adTypeStr);
        }

        string key = adTypeStr + placeStr + reason;
        if (key.Equals(lastNoAdShowKey))
        {
            return;
        }

        lastNoAdShowKey = key;
        BAADNoShowData.ad_type = adTypeStr;
        BAADNoShowData.ad_place = placeStr;
        BAADNoShowData.reason = reason;
        GPSDKMgr.Instance.TrackEvent("ad_no_show", BAADNoShowData.ToJson());
    }

    /// <summary>插屏 / 激励未能播放。带 reason，不去重。旧 ExcuteAdNoShow 仅留给未改完的点位。</summary>
    public static void ExcuteAdNoShowReason(string adTypeStr, string placeStr, string reason, int errorCode = 0)
    {
        if (string.IsNullOrWhiteSpace(adTypeStr) || string.IsNullOrWhiteSpace(placeStr) || string.IsNullOrWhiteSpace(reason))
            return;

        BAInterAdNoShow.ClearData();
        BAInterAdNoShow.ad_type = adTypeStr.Trim();
        BAInterAdNoShow.ad_place = placeStr;
        BAInterAdNoShow.reason = reason.Trim();
        bool reportErrorCode = BAInterAdNoShow.ad_type != BAInterAdNoShowData.AdTypeRewarded
            && BAInterAdNoShow.reason == BAInterAdNoShowData.ReasonNoFill;
        BAInterAdNoShow.errorCode = reportErrorCode ? errorCode : 0;
        GPSDKMgr.Instance.TrackEvent(BAInterAdNoShowData.EventKey, BAInterAdNoShow.ToTrackJson());
    }

    public static void ExcuteInterAdNoShow(string placeStr, string reason, int errorCode = 0)
    {
        ExcuteAdNoShowReason(BAInterAdNoShowData.AdTypeInter, placeStr, reason, errorCode);
    }

    public static void ExcuteRewardAdNoShow(string placeStr, string reason)
    {
        ExcuteAdNoShowReason(BAInterAdNoShowData.AdTypeRewarded, placeStr, reason);
    }

    public static void ExcuteAdLoad(string adDisplayType)
    {
        BAAdLoad.ClearData();
        BAAdLoad.ad_display_type = adDisplayType;
        GPSDKMgr.Instance.TrackEvent("ad_load", BAAdLoad.ToJson());
    }

    public static void ExcuteAdInitSuccess()
    {
        GPSDKMgr.Instance.TrackEvent("ad_init_success", "");
    }

    public static void ExcuteGMMode()
    {
        GPSDKMgr.Instance.TrackEvent("gm_mode", "");
    }

    //结束特定关卡Firebase
    public static void ExcuteFinishUniqueLevelFirebase()
    {
        int levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
        int levelId = levelIndex + 1;

        if (s_sessionInfinite)
        {
            return;
        }

        List<int> list = new()
        {
            9,11,12,16
        };

        if (list.Contains(levelId))
        {
            string eventName = "pass_" + levelId;
            FirebaseSdk.LogEvent(eventName);
        }
    }


    private static readonly HashSet<int> s_totalWinsMilestones = new()
    {
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
        25, 30, 35, 40, 45, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200
    };

    public static void ExcuteFinishTotalWinsAppFlyer()
    {
        int cnt = GPUserDataManager.Instance.GetPassLevelCnt();
        if (cnt > 5000)
        {
            return;
        }

        bool shouldReport = s_totalWinsMilestones.Contains(cnt) ||
            (cnt > 200 && cnt < 800 && cnt % 20 == 0) ||
            (cnt >= 800 && cnt % 100 == 0);

        if (shouldReport)
        {
            string eventName = "Total_Wins_" + cnt;
            AttrSdk.TrackEvent(eventName);
        }
    }


    //快手原因添加广告埋点事件
    public static void CheckExcuteKSAdShow()
    {
        var userData = GPUserDataManager.Instance.UserData;

        if (userData.IsWithinFirstDayWindow())
        {
            userData.IncrementVideoPlayFirstDay();
            int cnt = userData.videoPlayCntFirstDay;
            if (cnt == 1 || cnt == 3 || cnt == 5)
            {
                GPSDKMgr.Instance.TrackEvent("ad_show_" + cnt, "");
            }
        }
        else
        {

        }
    }

    /*
        public string product_id;
        public string product_name;
        public float price;//美元
        public bool is_first;//是否首次弹出
    */
    public static void ExcuteGameTriggerGift(string product_id, string product_name, float price, bool is_first)//触发条件礼包
    {
        BAGameTriggerGiftData.product_id = product_id;
        BAGameTriggerGiftData.product_name = product_name;
        BAGameTriggerGiftData.price = GameData.Pay.ConvertPriceToCent(price);
        BAGameTriggerGiftData.is_first = is_first;
        GPSDKMgr.Instance.TrackEvent("game_trigger_gift", BAGameTriggerGiftData.ToJson());
    }

    public static void ExcuteGameTriggerEvent(string name)//触发事件弹窗
    {
        BAGameTriggerEventData.name = name;
        GPSDKMgr.Instance.TrackEvent("game_trigger_event", BAGameTriggerEventData.ToJson());
    }

    public static void ExcuteGameTriggerActivity(string activityId, string activityName, int status, string uniqueId = "")
    {
        BAGameTriggerActivityData.ClearData();
        BAGameTriggerActivityData.activity_id = activityId ?? "";
        BAGameTriggerActivityData.activity_name = activityName ?? "";
        BAGameTriggerActivityData.unique_id = uniqueId ?? "";
        BAGameTriggerActivityData.status = status;
        GPSDKMgr.Instance.TrackEvent("game_trigger_activity", BAGameTriggerActivityData.ToJson());
    }

    /// <summary>
    /// 创建订单埋点
    /// </summary>
    public static void ExcuteCreateOrder(string storeId, string orderId, string receiptId)
    {
        BACreateOrder.ClearData();
        var payCfg = GameData.Pay.GetPayCfgByStoreProdId(storeId);
        float price = 0f;
        string currency = "USD";
        if (payCfg != null)
        {
            price = payCfg.ProductPrice;
            currency = payCfg.Currency;
        }

        BACreateOrder.price = (float)Math.Round(price, 2);
        BACreateOrder.currency = currency;
        BACreateOrder.quantity = 1;
        BACreateOrder.order_id = "";
        BACreateOrder.product_id = storeId;
        BACreateOrder.receipt_id = "";

        BAPaymentResult.role_id = GPUserDataManager.Instance.GetUID() ?? ThinkingDataSdk.GetDistinctId() ?? "";
        BAPaymentResult.role_name = GPUserDataManager.Instance.UserData.username ?? "";

        ThinkingDataSdk.LogEvent(BACreateOrder.key, BACreateOrder.ToJson());
    }

    /// <summary>
    /// 支付结果埋点（支付成功/支付失败/消费成功）
    /// </summary>
    public static void ExcutePaymentResult(string storeId, string orderId, string serverOrderId, bool success, int httpCode, string resultKey)
    {
        BAPaymentResult.ClearData();
        var payCfg = GameData.Pay.GetPayCfgByStoreProdId(storeId);

        float price = 0f;
        string currency = "USD";
        if (payCfg != null)
        {
            price = payCfg.ProductPrice;
            currency = payCfg.Currency;
        }

        BAPaymentResult.success = success;
        BAPaymentResult.price = (float)Math.Round(price, 2);
        BAPaymentResult.currency = currency;
        BAPaymentResult.quantity = 1;
        BAPaymentResult.order_id = orderId;
        BAPaymentResult.product_id = storeId;
        BAPaymentResult.receipt_id = "";
        BAPaymentResult.server_order_id = serverOrderId;
        BAPaymentResult.http_code = httpCode;
        BAPaymentResult.http_message = "";
        BAPaymentResult.role_id = GPUserDataManager.Instance.GetUID() ?? ThinkingDataSdk.GetDistinctId() ?? "";
        BAPaymentResult.role_name = GPUserDataManager.Instance.UserData.username ?? "";

        ThinkingDataSdk.LogEvent(resultKey, BAPaymentResult.ToJson());
    }

    public static void ExcuteCommonEvent(string lastStage = "", string lastStageForm = "")//公共事件
    {
        BATkCommon.stage = GPUserDataManager.Instance.GetLevelNum();
        BATkCommon.coin = GPUserDataManager.Instance.GetCoin();
        BATkCommon.test_group = InterADABTestConfig.GetTestGroupForAnalytics();
        BATkCommon.ad_strategy_Cooldown = InterADABTestConfig.GetEffectiveIntervalSeconds();
        BATkCommon.ad_strategy_timestamp = InterADABTestConfig.GetCooldownUnixForAnalytics();
        // FillRemoteConfigTracking(BATkCommon);

        ThinkingDataSdk.SetPublicProperty("stage", BATkCommon.stage.ToString());
        ThinkingDataSdk.SetPublicProperty("coin", BATkCommon.coin.ToString());
        if (!string.IsNullOrWhiteSpace(BATkCommon.test_group))
        {
            ThinkingDataSdk.SetPublicProperty("test_group", BATkCommon.test_group);
        }
        ThinkingDataSdk.SetPublicProperty("ad_strategy_Cooldown", BATkCommon.ad_strategy_Cooldown.ToString());
        ThinkingDataSdk.SetPublicProperty("ad_strategy_timestamp", BATkCommon.ad_strategy_timestamp.ToString());
        SyncToolLeft();

        // 进关才传入。形态配置可能为空，不能因此丢掉 last_stage。
        if (!string.IsNullOrEmpty(lastStage))
        {
            BATkCommon.last_stage = lastStage;
            BATkCommon.last_stage_form = lastStageForm ?? "";
            ThinkingDataSdk.SetPublicProperty("last_stage", lastStage);
            ThinkingDataSdk.SetPublicProperty("last_stage_form", BATkCommon.last_stage_form);
        }

        // tkCommonEvent 未使用，cfg 由 ReportThinkingDataGroup 写入
        // ThinkingDataSdk.LogEvent("tkCommonEvent", BATkCommon.ToJson());
    }

    /// <summary>
    /// 公共属性 tool_left：字符串列表，依次为提示、洗牌、撤回、高亮的剩余数量。
    /// </summary>
    public static void SyncToolLeft()
    {
        if (!GPUserDataManager.HasInit() || !GPSDKMgr.IsThinkingDataReady)
            return;

        var mgr = GPUserDataManager.Instance;
        string hint = mgr.GetHintItemCount().ToString();
        string shuffle = mgr.GetShuffleItemCount().ToString();
        string undo = mgr.GetUndoItemCount().ToString();
        string highlight = mgr.GetHighLightItemCount().ToString();
        BATkCommon.tool_left = new[] { hint, shuffle, undo, highlight };
        ThinkingDataSdk.SetPublicProperties(
            "{\"tool_left\":[\"" + hint + "\",\"" + shuffle + "\",\"" + undo + "\",\"" + highlight + "\"]}");
    }

    private static void FillRemoteConfigTracking(BATkCommonData data)
    {
        data.cfg = Array.Empty<BATkCommonData.BACfgData>();

        string version = InterADABTestConfig.GetThinkingDataConfigVersion(data.test_group);
        if (string.IsNullOrWhiteSpace(version))
            return;

        data.cfg = new[]
        {
            new BATkCommonData.BACfgData
            {
                name = InterADABTestConfig.ThinkingDataConfigName,
                version = version
            }
        };
    }

    public static void ExcutePolicyEvent(int status)//0弹窗 1点击OK
    {
        BAPolicyData.status = status;
        ThinkingDataSdk.LogEvent("policy", BAPolicyData.ToJson());
    }

    /// <summary>XSort2 评星弹窗埋点（Mahjong 侧已注释，此处保留接口）。</summary>
    public static void ExcuteCommentEvent(int status, int star = -1)
    {
        BACommentData.status = status;
        BACommentData.star = star;
        ThinkingDataSdk.LogEvent("comment", BACommentData.ToJson());
    }

    /// <summary>结算额外奖励。status: 0按钮出现 1点击按钮 2收到奖励。id 为抽中的奖励串。</summary>
    public static void ExcuteMainStageReward(int status, string rewardId)
    {
        BAMainStageReward.ClearData();
        BAMainStageReward.status = status;
        BAMainStageReward.id = rewardId ?? "";
        GPSDKMgr.Instance.TrackEvent(BAMainStageReward.key, BAMainStageReward.ToJson());
    }

    /// <summary>火花连胜页打开埋点。status: 0结算弹出 4主城进入。</summary>
    public static void ExcuteSignEvent(int status, int day, int type = 1)
    {
        BASignData.ClearData();
        BASignData.status = status;
        BASignData.day = day;
        BASignData.type = type;
        GPSDKMgr.Instance.TrackEvent(BASignData.key, BASignData.ToJson());
    }

    /// <summary>
    /// 小狗大冒险 activity。rank 不报(0)；rank2=阶段；key=当前已赢关数；status=2 时 reward_id=金币。
    /// unique_id=累计点 Start 次数。
    /// </summary>
    public static void ExcutePuppyActivity(int status, int rank2, int key, string uniqueId, int rewardId = 0)
    {
        BAActivityData.ClearData();
        BAActivityData.activity_id = 2;
        BAActivityData.activity_name = "puppyadventure";
        BAActivityData.status = status;
        BAActivityData.rank = 0;
        BAActivityData.rank2 = rank2;
        BAActivityData.unique_id = uniqueId ?? "";
        BAActivityData.key = key;
        BAActivityData.reward_id = status == 2 ? rewardId : 0;
        GPSDKMgr.Instance.TrackEvent("activity", BAActivityData.ToJson());
    }

    /// <summary>
    /// 金猪 activity。没有 status=9。
    /// rank 不报(0)；rank2=该档目标 3/5/7；key=当前进度。
    /// unique_id=累计「当天第一次连胜」次数。status=2 时 reward_id=最终金币。
    /// </summary>
    public static void ExcutePiggyActivity(int status, int rank2, int key, string uniqueId, int rewardId = 0)
    {
        BAActivityData.ClearData();
        BAActivityData.activity_id = 3;
        BAActivityData.activity_name = "piggybank";
        BAActivityData.status = status;
        BAActivityData.rank = 0;
        BAActivityData.rank2 = rank2;
        BAActivityData.unique_id = uniqueId ?? "";
        BAActivityData.key = key;
        BAActivityData.reward_id = status == 2 ? rewardId : 0;
        GPSDKMgr.Instance.TrackEvent("activity", BAActivityData.ToJson());
    }

    /// <summary>
    /// 彩蛋玩法单独事件 eggSmash。
    /// status: 0界面弹出 1砸蛋 2奖品面板关闭。
    /// type: 0正常收集完成弹出 1主城补领弹出。
    /// choice: 0 Claim 1 ClaimAll。
    /// </summary>
    public static void ExcuteEggSmash(int status, int rewardGroup, string rewardId, int type, int choiceState)
    {
        BAEggSmashData.ClearData();
        BAEggSmashData.status = status;
        BAEggSmashData.rewardgroup = rewardGroup;
        BAEggSmashData.rewardid = rewardId ?? "";
        BAEggSmashData.type = type;
        BAEggSmashData.choice = choiceState;
        Debug.Log($"[eggSmash] {BAEggSmashData.ToJson()}");
        GPSDKMgr.Instance.TrackEvent(BAEggSmashData.key, BAEggSmashData.ToJson());
    }


    //以下是一些需要用到的变量方法
    public static void SetLevelStartTime()
    {
        m_levelStartTime = Time.realtimeSinceStartup;
    }

    public static bool GetBValidOperate()
    {
        bool retB = false;
        return retB;
    }

    public static void IncreateOperCnt()
    {
        m_operCnt++;
    }

    public static int GetOperCnt()
    {
        return m_operCnt;
    }

    //道具使用次数
    public static void AddToolUseCnt(int id)
    {
        if (!GameData.BFromStartScene) return;

        if (m_toolUseDic.ContainsKey(id))
        {
            int cnt = m_toolUseDic[id] + 1;
            m_toolUseDic[id] = cnt;
        }
        else
        {
            m_toolUseDic[id] = 1;
        }
    }

    public static int GetToolUseTotalCnt()
    {
        int retCnt = 0;
        foreach (var item in m_toolUseDic)
        {
            retCnt += item.Value;
        }
        return retCnt;
    }

    public static void ClearBeforeOperate()
    {
        m_clearItemCnt = 0;
        m_skillItemReleaseCnt = 0;
        m_skillItemCombineCnt = 0;
        m_justHurtObstacle = false;
    }


    //统计item消除次数
    public static void IncreaseItemClearCnt()
    {
        m_clearItemCnt++;
    }

    //统计特技棋子释放次数
    public static void IncreaseSkillItemReleaseCnt()
    {
        m_skillItemReleaseCnt++;
    }

    public static void IncreaseSkillItemCombineCnt()
    {
        m_skillItemCombineCnt++;
    }

    public static void SetJustHurtObstacle()
    {
        m_justHurtObstacle = true;
    }

    public static void ClearBeforeNewGame()
    {
        BAMainStage.ClearData();
        BAStageEvent.ClearData();
        BAGuide.ClearData();
        BALoading.ClearData();
    }
}
