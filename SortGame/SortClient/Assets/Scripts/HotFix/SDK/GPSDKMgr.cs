using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EwingSoft.GameSDK;
using FishFramework;
using FishTilePlay;
using Sirenix.Utilities;
using TileGameCore;
using UnityEngine;
using System.Collections;
#if UNITY_IOS
using System.Runtime.InteropServices;
// iOS 平台：把 SDK 名字别名到 iOS 适配层（EwingSoft.GameSDK.IosBridge.*），
// 业务代码（下方）无需改动即可走 iOS SDK；Android/其他平台仍用 EwingSoft.GameSDK.*。
using AdSdk = EwingSoft.GameSDK.IosBridge.AdSdk;
using BillingSdk = EwingSoft.GameSDK.IosBridge.BillingSdk;
using CMPSdk = EwingSoft.GameSDK.IosBridge.CMPSdk;
using RemoteDataSdk = EwingSoft.GameSDK.IosBridge.RemoteDataSdk;
using FirebaseSdk = EwingSoft.GameSDK.IosBridge.FirebaseSdk;
using ThinkingDataSdk = EwingSoft.GameSDK.IosBridge.ThinkingDataSdk;
using AttrSdk = EwingSoft.GameSDK.IosBridge.AttrSdk;
using UtilSdk = EwingSoft.GameSDK.IosBridge.UtilSdk;
using GameSDKCallbackListener = EwingSoft.GameSDK.IosBridge.GameSDKCallbackListener;
#endif

public class GPSDKMgr : MonoBehaviour
{
    public static event Action OnShortcutToggleGameRequested;

    [Serializable]
    public class JavaMsgData
    {
        public string key;
        public string content;
        public string uid;
    }

    [Serializable]
    public class JavaCodeMsgData
    {
        public int code;
        public string message;
    }

    public static GPSDKMgr Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindObjectOfType<GPSDKMgr>();
            }
            if (_instance == null)
            {
                var go = new GameObject("GPSDKMgr");
                _instance = go.AddComponent<GPSDKMgr>();
            }
            return _instance;
        }
    }
    private static GPSDKMgr _instance;

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass m_jc;
        AndroidJavaObject jo;
#endif
    private bool m_isAdInitSuc;//必须要AdSDK.InitAds成功才能开始加载广告
    private bool m_isAdInitCallbackSuc;
    private bool m_isAdInitInProgress;

    // 广告类型字段，与 SDKMgr 保持一致
    private int m_rewardAdType;
    private int m_intertistAdType;
    private const string AdDisplayTypeBanner = "banner";
    private const string AdDisplayTypeInterstitial = "interstitial";
    private const string AdDisplayTypeRewarded = "rewarded";
    private const string AdNoShowReasonInitFailed = "ad_init_failed";
    private const string AdNoShowReasonLoading = "ad_loading";

#if UNITY_IOS && !UNITY_EDITOR
    /// <summary>iOS MAX：banner 懒加载，LoadBannerAd 只允许触发一次。</summary>
    private bool m_hasEverLoadBannerAD = false;
#endif
    private bool m_hasEverLoadInterAD = false;
    private bool m_hasEverLoadRewardedAD = false;

    private bool mShowBanner = false;
    private bool m_isAdPlaying = false;

#if UNITY_IOS
    private const string RemoteConfigKeyIsUnderReviewIOS = "IsUnderReviewIOS";
    /// <summary>原生轮询约 40×0.25s；Unity 侧略放宽作兜底。</summary>
    private const float RemoteConfigWaitTimeoutSeconds = 12f;
    private bool m_isUnderReviewIOS = true;
    private bool m_remoteConfigResolved;
    private bool m_remoteConfigLoadSuccess;
    private bool m_firebaseSdkInited;
    private bool m_remoteConfigEventRegistered;
#endif

    // 回调函数，与旧 SDKMgr 签名一致（返回值忽略）
    public Func<string, string, string, string> onRecieveJavaDataFunc;

    // SDK回调处理器
    private GPSDKCallbackHandler m_callbackHandler;

    public bool IsAdPlaying => m_isAdPlaying;

#if UNITY_ANDROID
    private bool bAdDebug = false; // true 时使用 Google AdMob 官方测试单元 ID

    private string bannerIdDebug = "ca-app-pub-3940256099942544/9214589741";
    private string rewardedIdDebug = "ca-app-pub-3940256099942544/5224354917";
    private string interAdIdDebug = "ca-app-pub-3940256099942544/1033173712";

    // AdMob 瀑布流正式参数
    private string bannerIdRelease = "4|100|ca-app-pub-5248554588269117/5435166836";
    private string interIdHighRelease = "ca-app-pub-5248554588269117/8431478147";
    private string interIdLowRelease = "ca-app-pub-5248554588269117/8431478147";
    private string rewardIdHighRelease = "ca-app-pub-5248554588269117/5504368803";
    private string rewardIdLowRelease = "ca-app-pub-5248554588269117/5504368803";
#endif

#if UNITY_IOS
    // AppLovin MAX（iOS）正式广告位
    private string applovinSdkKey = "DizcTmE36zLt-qsaqyFJFNXlu2Jf8_RGt8P-H2cUvwD8SLvXZHLSALoaf9MrbdlyM8MTXmu0KwRTKH_0Hdkq4C";
    private string iosRewardedAdUnitId = "703ae74c32c6151a";
    private string iosInterstitialAdUnitId = "2734c44f3875760a";
    private string iosBannerAdUnitId = "008037475efcd1ad";
#endif

    //需要提供.
    private string remoteUrl = "https://sf-game-client-prod-41642565379.us-central1.run.app";
    private string remoteAppId = "puzzlematch"; // 与远端约定：上线前改为正式 appId
    private string remoteSecret = "91deeaf4-26bc-42b1-8bfd-dc1a66943a14";
    private string sofishAppId = "10061";

    // 数数参数。Android 在 CustomApplication.onCreate 初始化，此处供 iOS，并须与 Java 侧 appId/url 保持一致。
    string thinkingDataAppid = "86079bdae3774e77956692de3304ef36"; //先健给的appid

    string thinkingDataUrl = "https://api-shushu.sogamecdn.com";

    private bool m_baseSdkInited;
    private bool m_thinkingDataInited;
    public static bool IsThinkingDataReady => _instance != null && _instance.m_thinkingDataInited;
    private readonly List<(string key, string content)> m_pendingTrackEvents = new();

    //初始化方法
    string attrToken = "bDznX2HRT8hToQ7L6dgBSX"; //统一的似乎.
#if UNITY_IOS
    private const string appsFlyerAppleAppId = "6775471047";
    private const int appsFlyerAttWaitTimeoutSeconds = 60;
#endif

    public void InitBaseSdk()
    {
        if (m_baseSdkInited)
        {
            return;
        }

        BaseSdk.InitBaseSDK(false);
        m_baseSdkInited = true;
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void OnRecieveAndroidMsg(string msg1)
    {
        if (string.IsNullOrEmpty(msg1))
        {
            return;
        }

        JavaMsgData retVal;

        try
        {
            retVal = JsonUtility.FromJson<JavaMsgData>(msg1);
        }
        catch (Exception e)
        {
            Debug.LogError($"OnRecieveAndroidMsg parse failed: {e.Message}");
            return;
        }

        if (retVal == null || string.IsNullOrEmpty(retVal.key))
        {
            return;
        }

        ApplyLegacyJavaKeyState(retVal.key, retVal.content);
        TriggerJavaDataCallback(retVal.key, retVal.content, retVal.uid);
    }

    private string m_completeOrdersStr;
    private string m_countryCode = "";
    private string m_deiveId = "";
    private long m_userRegisterTime;
    private bool m_bShowFeedback;

    void ApplyLegacyJavaKeyState(string key, string content)
    {
        switch (key)
        {
            case "InitSdkSuc":
                m_deiveId = content ?? "";
                break;
            case "LoginCountry":
                m_countryCode = content ?? "";
                break;
            case "QueryCompleteOrdersSuc":
                m_completeOrdersStr = content;
                break;
            case "SetUserRegisterTime":
                if (!string.IsNullOrEmpty(content) && long.TryParse(content, out var t))
                    m_userRegisterTime = t;
                break;
        }
    }

    /// <summary>与 com.zzxz.gamesdk.engineadapter.UnityAdapter 中 unityCallbackPath 一致，否则 Android 侧 UnitySendMessage 无法命中监听。</summary>
    const string UnitySdkCallbackGameObjectName = "UnityCallbackDelegate";

    static void EnsureGameSDKCallbackListenerObject()
    {
#if !UNITY_IOS
        // Android：监听器是 MonoBehaviour，需要手动挂到固定命名的物体上（UnitySendMessage 命中点）。
        // iOS：EwingSoft.GameSDK.Ios.GameSDKCallbackListener 通过 RuntimeInitializeOnLoadMethod
        // 自举为 UnityCallbackDelegate_iOS；此处别名指向的是静态事件桥，无需也不能实例化。
        var existing = UnityEngine.Object.FindObjectOfType<GameSDKCallbackListener>();
        if (existing != null)
        {
            if (existing.gameObject.name != UnitySdkCallbackGameObjectName)
                existing.gameObject.name = UnitySdkCallbackGameObjectName;
            return;
        }
        var go = new GameObject(UnitySdkCallbackGameObjectName);
        go.AddComponent<GameSDKCallbackListener>();
        UnityEngine.Object.DontDestroyOnLoad(go);
#endif
    }

    public void InitAdSdk()
    {
        EnsureGameSDKCallbackListenerObject();
#if UNITY_IOS && !UNITY_EDITOR
        // iOS：可能已在 UIPrivacyDlg 弹出时提前 Init；此处保证只执行一次
        InitFirebaseSDKOnce();
#else
        FirebaseSdk.InitFirebaseSDK();
#endif

        bool adDebug = false;
#if UNITY_IOS
        // MAX：须在 CMPSdk.StartCMP/StartGDPR 之前调用，并传入 applovinSdkKey。
        adDebug = GameModule.Setting != null && GameModule.Setting.GmMode;
        AdSdk.InitAdSDK(
            iosRewardedAdUnitId,
            iosInterstitialAdUnitId,
            iosBannerAdUnitId,
            string.Empty,
            adDebug,
            applovinSdkKey);
#elif UNITY_ANDROID
        adDebug = bAdDebug;
        List<string> strList = new();
        if (adDebug)
        {
            AdSdk.InitAdSDK(rewardedIdDebug, rewardedIdDebug, interAdIdDebug, interAdIdDebug, bannerIdDebug, strList.ToArray());
        }
        else
        {
            AdSdk.InitAdSDK(rewardIdHighRelease, rewardIdLowRelease, interIdHighRelease, interIdLowRelease, bannerIdRelease, strList.ToArray());
        }
#endif

        RegisterAdsEvent();
        Debug.Log($"[AdDiag] InitAdSdk done debug={adDebug}");
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void Tile2_ForceFetchRemoteConfig();

    /// <summary>0=idle 1=in_flight 2=success 3=fail</summary>
    [DllImport("__Internal")]
    private static extern int Tile2_GetForceFetchResult();

    /// <summary>
    /// iOS：只执行一次。有隐私弹窗时在 OnOpen 调用，强制重拉 RC（不走自动 fetch 轮询）。
    /// </summary>
    public void InitFirebaseSDKOnce()
    {
        if (m_firebaseSdkInited)
        {
            Debug.Log("[GPSDKMgr][RCDiag] InitFirebaseSDKOnce skip (already inited)");
            return;
        }

        EnsureGameSDKCallbackListenerObject();
        EnsureRemoteConfigEventRegistered();
        m_firebaseSdkInited = true;
        // 允许本次强制拉取覆盖此前结果
        m_remoteConfigResolved = false;
        m_remoteConfigLoadSuccess = false;
        Debug.Log("[GPSDKMgr][RCDiag] InitFirebaseSDKOnce → Tile2_ForceFetchRemoteConfig");
        Tile2_ForceFetchRemoteConfig();
    }

    void EnsureRemoteConfigEventRegistered()
    {
        if (m_remoteConfigEventRegistered)
        {
            return;
        }

        m_remoteConfigEventRegistered = true;
        GameSDKCallbackListener.OnRemoteConfigLoadedEvent += OnGPRemoteCfgLoadSuc;
    }

    /// <summary>
    /// 混淆包可能导致 UnitySendMessage 找不到 OnRemoteConfigLoaded；用原生状态位兜底。
    /// </summary>
    bool TryApplyForceFetchNativeStatus()
    {
        if (m_remoteConfigResolved)
        {
            return true;
        }

        int result = Tile2_GetForceFetchResult();
        if (result == 2)
        {
            ApplyIosRemoteConfigResult(true, "force_fetch_status");
            return true;
        }

        if (result == 3)
        {
            ApplyIosRemoteConfigResult(false, "force_fetch_status");
            return true;
        }

        return false;
    }
#endif

    public void InitRemoteDataSdk()
    {
        Debug.Log($"[AdDiag] InitRemoteDataSdk appId={remoteAppId} url={remoteUrl} uid={RemoteDataSdk.GetUid()}");
        RemoteDataSdk.InitRemoteDataSdk(remoteUrl, remoteAppId);
        RemoteDataSdk.SetRemoteAppSecret(remoteSecret);
    }

    public void InitBillingSdk()
    {
        // iOS InitBillingSDK 必须按「消耗｜非消耗｜订阅」分组；裸 & 拼接会被原生直接拒绝，Billing 永远 state=0。
        // Android 仍接受历史的 productId&productId& 扁平串。
#if UNITY_IOS && !UNITY_EDITOR
        string prodIdsStr = BuildIosGroupedBillingProductIds();
#else
        string prodIdsStr = "";
        var list = ConfigHelper.Tables.TbPayProductCfg.DataList;
        foreach (var item in list)
        {
            if (!string.IsNullOrEmpty(item.ProductIdGoogle))
            {
                prodIdsStr += item.ProductIdGoogle + "&";
            }
        }
#endif

        Debug.Log($"[GPSDKMgr] InitBillingSdk productIds={prodIdsStr}");
        BillingSdk.InitBillingSDK(prodIdsStr);
        BillingSdk.SetRemoteMode(remoteUrl, remoteAppId, remoteSecret);
        // Init 的扁平串没有 |，SDK 会把去广告放进消耗型。这里再用 SetProductIds 改分类。
        // handleConsumePurchase 对非消耗型只 acknowledge。
        ApplyAndroidBillingProductTypes();
#if UNITY_EDITOR
        string noAdsCheckId = GameData.Pay.GetNoAdsGId();
        Debug.Assert(ShouldDeliverStoreReward(noAdsCheckId, false, false));
        Debug.Assert(!ShouldDeliverStoreReward(noAdsCheckId, true, false));
        Debug.Assert(ShouldDeliverStoreReward(noAdsCheckId + ".coin", true, false));
        Debug.Assert(!ShouldDeliverStoreReward(noAdsCheckId + ".coin", false, true));
#endif

        RegisterBillingEvent();
        BeginWatchBillingReady();
    }

    Coroutine m_billingReadyWatch;

    /// <summary>
    /// 商店 Billing 是否已经可以发起购买。编辑器没有原生 Billing，直接放行。
    /// </summary>
    public bool IsBillingReady()
    {
#if UNITY_EDITOR
        return true;
#else
        return BillingSdk.IsReady();
#endif
    }

    void BeginWatchBillingReady()
    {
#if UNITY_EDITOR
        return;
#else
        if (IsBillingReady())
        {
            return;
        }

        if (m_billingReadyWatch != null)
        {
            StopCoroutine(m_billingReadyWatch);
        }

        m_billingReadyWatch = StartCoroutine(WatchBillingReadyCoroutine());
#endif
    }

    IEnumerator WatchBillingReadyCoroutine()
    {
        while (!IsBillingReady())
        {
            yield return new WaitForSecondsRealtime(0.5f);
        }

        m_billingReadyWatch = null;
        // 价格可能先到、Billing 后 ready。商店和免广告页靠这个事件把菊花换成可购买。
        GameEvent.Send((int)EventNameDef.ReqQueryShopProductSuc);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    void ApplyAndroidBillingProductTypes()
    {
        string noAdsId = GameData.Pay.GetNoAdsGId();
        var consumeIds = new HashSet<string>();
        var nonConsumeIds = new HashSet<string>();
        var list = ConfigHelper.Tables.TbPayProductCfg.DataList;
        foreach (var item in list)
        {
            if (string.IsNullOrEmpty(item.ProductIdGoogle))
            {
                continue;
            }

            if (item.ProductIdGoogle == noAdsId)
            {
                nonConsumeIds.Add(item.ProductIdGoogle);
            }
            else
            {
                consumeIds.Add(item.ProductIdGoogle);
            }
        }

        BillingSdk.SetProductIds(consumeIds, nonConsumeIds, new HashSet<string>());
    }
#else
    void ApplyAndroidBillingProductTypes()
    {
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    /// <summary>
    /// iOS 分组格式：consume1&consume2|nonConsume1&nonConsume2|sub1&sub2
    /// 免广告为 non-consumable，其余内购按 consumable；当前无订阅段留空。
    /// </summary>
    static string BuildIosGroupedBillingProductIds()
    {
        var consumeIds = new List<string>();
        var nonConsumeIds = new List<string>();
        string noAdsId = GameData.Pay.GetNoAdsGId();
        var list = ConfigHelper.Tables.TbPayProductCfg.DataList;
        foreach (var item in list)
        {
            if (string.IsNullOrEmpty(item.ProductIdIos))
            {
                continue;
            }

            if (item.ProductIdIos == noAdsId)
            {
                if (!nonConsumeIds.Contains(item.ProductIdIos))
                {
                    nonConsumeIds.Add(item.ProductIdIos);
                }
            }
            else if (!consumeIds.Contains(item.ProductIdIos))
            {
                consumeIds.Add(item.ProductIdIos);
            }
        }

        return string.Join("&", consumeIds) + "|" + string.Join("&", nonConsumeIds) + "|";
    }
#endif

    public void InitAppFlyerSDK()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // iOS：Unity 侧初始化 AppsFlyer（Android 在 CustomApplication 原生层已初始化）。
        // attWaitTimeout 配合后续 StartCMP 触发的 ATT；须在 CMP 之前调用。
        AttrSdk.InitAppsFlyer(attrToken, appsFlyerAppleAppId, appsFlyerAttWaitTimeoutSeconds, GameModule.Setting.GmMode);
#endif
    }

    public void InitThinkingDataSDK()
    {
        if (m_thinkingDataInited)
        {
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // 5.7.0 起数数已在 CustomApplication.onCreate 初始化，勿在 Unity 侧重复 Init。
        _ = thinkingDataAppid;
        _ = thinkingDataUrl;
#else
        ThinkingDataSdk.Init(thinkingDataAppid, thinkingDataUrl, GameModule.Setting.GmMode);
#endif
        m_thinkingDataInited = true;
        FlushPendingTrackEvents();
        GameBAData.SyncToolLeft();
    }

    private void FlushPendingTrackEvents()
    {
        foreach (var (key, content) in m_pendingTrackEvents)
        {
            ThinkingDataSdk.LogEvent(key, content);
        }

        m_pendingTrackEvents.Clear();
    }

    //初始化cmp SDK
    public void InitCMPSDK()
    {
        Debug.Log("[AdDiag] InitCMPSDK");
        CMPSdk.InitCMPSdk(false);
#if UNITY_IOS && !UNITY_EDITOR
        // iOS UMP/CMP 需要隐私政策与服务条款 URL；Android 侧原生已有配置，不在此处改动。
        CMPSdk.SetURL(GameData.PrivacyUrl, GameData.ServiceUrl);
#endif
    }

    public void StartCMPSDK()
    {
        Debug.Log("[AdDiag] StartCMPSDK");
        CMPSdk.StartCMP();
    }

    public void UpdateCMPSDK()
    {
        CMPSdk.UpdateCMP();
    }

    public void InitNoticationSDK()
    {
        GPSDKMgr.Instance.EnableDailyNotification();
        // 本地 11:00 推送由 EnableDailyNotification 开启，不在此自动调度
    }

    private const string unityPlayerClassPath = "com.unity3d.player.UnityPlayer";
    private const string PrefKeyNotificationRequested = "notification_permission_requested";

    /// <summary>
    /// 是否应展示通知权限申请弹窗。仅展示一次，除非用户清理缓存。
    /// 展示 UIApplyNotificationDlg 前应调用此方法。
    /// </summary>
    public bool ShouldShowNotificationPrompt()
    {
        return PlayerPrefs.GetInt(PrefKeyNotificationRequested, 0) == 0;
    }

    public void SetNotificationSet()
    {
        PlayerPrefs.SetInt(PrefKeyNotificationRequested, 1);
    }

    /// <summary>与旧 SDKMgr.SetNotificationRequested 一致。</summary>
    public void SetNotificationRequested()
    {
        SetNotificationSet();
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 开启每日 11:00 本地推送。仅调度闹钟，不弹系统权限框。
    /// 由 Unity 在合适的时机申请通知权限后调用。
    /// </summary>
    public void EnableDailyNotification()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass(unityPlayerClassPath))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity != null)
            {
                using (var dailyRemind = new AndroidJavaClass("com.unity3d.player.DailyRemindReceiver"))
                {
                    dailyRemind.CallStatic("scheduleDailyAlarm", activity);
                }
            }
        }
#endif
    }

    /// <summary>
    /// 请求通知权限（Android 13+ 需调用）。由 Unity 选择时机弹出。
    /// 仅弹一次，除非用户清理缓存。
    /// </summary>
    public void RequestNotificationPermission()
    {
        if (PlayerPrefs.GetInt(PrefKeyNotificationRequested, 0) != 0)
            return;
#if UNITY_ANDROID && !UNITY_EDITOR
        var requestAttempted = false;
        using (var unityPlayer = new AndroidJavaClass(unityPlayerClassPath))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            if (activity != null)
            {
                try
                {
                    using (var permissionHelper = new AndroidJavaClass("com.unity3d.player.NotificationPermissionHelper"))
                    {
                        permissionHelper.CallStatic("requestIfNeeded", activity);
                        requestAttempted = true;
                        Debug.Log("RequestNotificationPermission: called NotificationPermissionHelper.requestIfNeeded");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"RequestNotificationPermission: java helper failed, fallback to Unity permission API. {e.Message}");
                }
            }
        }

        if (!requestAttempted)
        {
            // Fallback: avoid Java reflection strip/mismatch issues in release builds.
            const string postNotificationsPermission = "android.permission.POST_NOTIFICATIONS";
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(postNotificationsPermission))
            {
                UnityEngine.Android.Permission.RequestUserPermission(postNotificationsPermission);
                requestAttempted = true;
                Debug.Log("RequestNotificationPermission: requested via UnityEngine.Android.Permission");
            }
            else
            {
                requestAttempted = true;
                Debug.Log("RequestNotificationPermission: already authorized");
            }
        }

        if (requestAttempted)
        {
            PlayerPrefs.SetInt(PrefKeyNotificationRequested, 1);
            PlayerPrefs.Save();
        }
#endif
    }

    /// <summary>旧版 Java login；Tile2 使用 PostUserData/RemoteDataSdk。</summary>
    public void LoginSDK()
    {
    }

    // InitAds 成功后只拉激励。插屏在进关前 TryLoadInterstitialIfNeeded，加载关读 InterAdV5Config。
    public void CheckLoadAllAd()
    {
        LoadRewardAd();
    }
    public void CheckShowAdBanner()
    {
        if (mShowBanner)
        {
            ShowADBanner();
        }
    }

    public void ShowADBanner()
    {
        if (GPUserDataManager.Instance.GetBNoAd())
        {
            return;
        }

        mShowBanner = true;
        if (m_isAdInitSuc)
        {
#if UNITY_IOS && !UNITY_EDITOR
            // iOS MAX：banner 懒加载，须先 LoadBannerAd 创建 loader，再 Show；Android 由 SDK 内部队列处理。
            SafeLoadBannerAd();
#endif
            AdSdk.ShowBannerAd(true);
        }
    }

    public void HideADBanner()
    {

        mShowBanner = false;
        if (m_isAdInitSuc)
        {
            AdSdk.ShowBannerAd(false);
        }
    }

#if UNITY_IOS && !UNITY_EDITOR
    /// <summary>
    /// Banner 只能加载一次。SDK 的加载成功/失败回调没有作用，失败不能靠回调清标记再 Load。
    /// iOS MAX：InitAds 成功后须先 LoadBannerAd 再 ShowBannerAd。Android 由 InitAds 内部加载，不要调用这里。
    /// </summary>
    private void SafeLoadBannerAd()
    {
        if (!m_isAdInitSuc || m_hasEverLoadBannerAD)
        {
            return;
        }

        if (AdSdk.IsBannerLoaded())
        {
            return;
        }

        m_hasEverLoadBannerAD = true;
        Debug.Log("[GPSDKMgr][MaxAdapter] LoadBannerAd begin");
        AdSdk.LoadBannerAd(
            () => Debug.Log("[GPSDKMgr][MaxAdapter] LoadBannerAd success"),
            error => Debug.LogWarning($"[GPSDKMgr][MaxAdapter] LoadBannerAd fail error={error}"));
    }
#endif

    /// <summary>
    /// 插屏只能加载一次。SDK 的加载成功/失败回调没有作用，失败不能靠回调清标记再 Load。
    /// 就绪状态用 AdSdk.IsFullAdLoaded 查询。
    /// 不在这里做归因/关卡门禁，门禁在 LoadFullAd。
    /// </summary>
    void SafeLoadFullAd()
    {
        if (!m_isAdInitSuc)
        {
            Debug.Log("[AdDiag] SafeLoadFullAd skip initFlag=false");
            return;
        }
        if (m_hasEverLoadInterAD)
        {
            Debug.Log($"[AdDiag] SafeLoadFullAd skip alreadyRequested callback={m_isAdInitCallbackSuc} sdkLoaded={AdSdk.IsFullAdLoaded()}");
            return;
        }

        // SDK 已有缓存时不再 load，但必须置标记，否则开播会被记成 ad_not_load。
        m_hasEverLoadInterAD = true;
        bool sdkLoaded = AdSdk.IsFullAdLoaded();
        Debug.Log($"[AdDiag] SafeLoadFullAd request sdkLoaded={sdkLoaded} callback={m_isAdInitCallbackSuc}");
        if (sdkLoaded)
            return;

        GameBAData.ExcuteAdLoad(AdDisplayTypeInterstitial);
        // 加载成功/失败回调不会进入，这里只发起一次，失败不重试。
        AdSdk.LoadFullAd();
    }

    public void LoadFullAd()
    {
        // 未到当前档加载关则跳过，且不置 m_hasEverLoadInterAD。加载关读 InterAdV5Config。
        if (!InterADABTestConfig.ShouldLoadInterstitialNow())
        {
            Debug.Log($"[AdDiag] LoadFullAd skip notYet level={GPUserDataManager.Instance.GetLevelNum()} ever={m_hasEverLoadInterAD}");
            return;
        }

        SafeLoadFullAd();
    }

    /// <summary>进关前尝试加载插屏；内部受 ShouldLoadInterstitialNow + 一次性门禁约束。</summary>
    public void TryLoadInterstitialIfNeeded()
    {
        LoadFullAd();
    }

    public bool GetInterstitAdLoaded()
    {
        return m_isAdInitSuc && AdSdk.IsFullAdLoaded();
    }

    public string GetAdNoShowReason(string adTypeStr)
    {
        if (!m_isAdInitSuc)
        {
            return AdNoShowReasonInitFailed;
        }

        return AdNoShowReasonLoading;
    }

    public void PlayInterstitialAd(string placeStr, int adType, Action<int> onClosed = null)//胜利后1 失败后2
    {
        m_intertistAdType = adType;

        // InitAds 回调成功且插屏 Load 已发起，才允许 Show。未就绪不是 no_fill。
        if (!m_isAdInitCallbackSuc || !m_hasEverLoadInterAD)
        {
            Debug.Log($"[AdDiag] PlayInterstitialAd not_load place={placeStr} callback={m_isAdInitCallbackSuc} initFlag={m_isAdInitSuc} everLoad={m_hasEverLoadInterAD} sdkLoaded={AdSdk.IsFullAdLoaded()}");
            GameBAData.ExcuteInterAdNoShow(placeStr, BAInterAdNoShowData.ReasonAdNotLoad);
            AdsPlayStrategy.NotifyEnterLevelInterstitialFinished();
            return;
        }

        // 不再预判是否加载完成。no_fill 由关闭回调的 errorCode 决定。
        ShowFullAd(errorCode =>
        {
            if (errorCode != 0)
                GameBAData.ExcuteInterAdNoShow(placeStr, BAInterAdNoShowData.ReasonNoFill, errorCode);
            onClosed?.Invoke(errorCode);
            // 编辑器桩直接调 callback，不派发 OnFullAdClosedEvent。真机上该事件已先消费。
            CompleteInterstitialCloseIfStillPlaying(errorCode);
        }, null, null, placeStr);
    }

    void CompleteInterstitialCloseIfStillPlaying(int errorCode)
    {
        if (!m_isAdPlaying || m_currentAdPlaybackType != AdPlaybackType.Interstitial)
            return;
        OnGPFullAdClosed(errorCode);
    }

    /// <summary>
    /// 激励只能加载一次。SDK 的加载成功/失败回调没有作用，失败不能靠回调清标记再 Load。
    /// 就绪状态用 AdSdk.IsRewardAdLoaded 查询。
    /// </summary>
    public void LoadRewardAd()
    {
        if (!m_isAdInitSuc)
        {
            Debug.Log("[AdDiag] LoadRewardAd skip initFlag=false");
            return;
        }

        if (m_hasEverLoadRewardedAD)
        {
            Debug.Log($"[AdDiag] LoadRewardAd skip alreadyRequested sdkLoaded={AdSdk.IsRewardAdLoaded()}");
            return;
        }

        if (AdSdk.IsRewardAdLoaded())
        {
            Debug.Log("[AdDiag] LoadRewardAd skip sdk already loaded");
            return;
        }

        m_hasEverLoadRewardedAD = true;
        Debug.Log("[AdDiag] LoadRewardAd request");
        GameBAData.ExcuteAdLoad(AdDisplayTypeRewarded);
        // 加载成功/失败回调不会进入，这里只发起一次，失败不重试。
        AdSdk.LoadRewardAd();
    }

    public void PlayRewardedAd(String placeStr, int adType = 1)
    {
        m_rewardAdType = adType;

        // SDK 限制：LoadRewardAd 仅允许调用一次，已在 InitAds 成功后由 CheckLoadAllAd 触发；
        // 未加载好直接走播放失败回调以关闭外层 UIAdsLoading
        if (!m_isAdInitSuc || !AdSdk.IsRewardAdLoaded())
        {
            Debug.Log($"[AdDiag] PlayRewardedAd fail place={placeStr} initFlag={m_isAdInitSuc} callback={m_isAdInitCallbackSuc} everLoad={m_hasEverLoadRewardedAD} sdkLoaded={AdSdk.IsRewardAdLoaded()}");
            TriggerJavaDataCallback("AdsPlayFailed", "", "");
            return;
        }

        AdsPlayStrategy.MarkRewardedAdTriggeredInCurrentLevel();
        ShowRewardAd(null, null, null, null, placeStr);
    }


    /// <summary>
    /// 获取SDK回调处理器
    /// </summary>
    public GPSDKCallbackHandler GetCallbackHandler()
    {
        return m_callbackHandler;
    }


    private static string mCurAdPlace = "";
    private enum AdPlaybackType
    {
        None = 0,
        Reward = 1,
        Interstitial = 2,
    }

    /// <summary>底时；到点看焦点，无焦点可续期。</summary>
    private const float AdCallbackTimeoutSeconds = 20f;
    /// <summary>无焦点时最多再续次数（总等待最坏 = 20s × (1+3)）。</summary>
    private const int AdCallbackTimeoutMaxExtendCount = 2;
    private AdPlaybackType m_currentAdPlaybackType = AdPlaybackType.None;
    private int m_adPlaybackSessionId = 0;
    /// <summary>已结束（含超时假失败）的 session / 类型，用于丢弃迟到或串台的 SDK 回调。</summary>
    private int m_adFinishedSessionId = 0;
    private AdPlaybackType m_adFinishedPlaybackType = AdPlaybackType.None;
    /// <summary>已经写过 CD 锚点的插屏 session，防止 SDK 重复关播回调反复顺延 CD。</summary>
    private int m_lastRecordedInterstitialCloseSessionId = -1;
    private int m_adTimeoutExtendCount;
    private bool m_adGotImpression;
    /// <summary>激励已开播、尚未 Viewed/用户主动重开。超时假失败后仍为 true，好让迟到的 Viewed 补发奖励。</summary>
    private bool m_pendingRewardGrant;
    private float m_ignoreClickUntilUnscaled;
    private Coroutine m_adCallbackTimeoutCoroutine;
    private Coroutine m_deferredPrefsSaveCoroutine;
    private static readonly WaitForSecondsRealtime s_deferredPrefsDelay = new WaitForSecondsRealtime(0.25f);

    public bool HasPendingRewardGrant => m_pendingRewardGrant;

    public bool ShouldIgnorePostAdClick()
    {
        return Time.unscaledTime < m_ignoreClickUntilUnscaled;
    }

    public void MarkIgnoreClickAfterAd()
    {
        m_ignoreClickUntilUnscaled = Time.unscaledTime + 1.2f;
    }

    public void ClearPendingRewardGrant()
    {
        m_pendingRewardGrant = false;
    }

    private void StartAdCallbackTimeout()
    {
        CancelAdCallbackTimeout();
        int sessionId = m_adPlaybackSessionId;
        m_adCallbackTimeoutCoroutine = StartCoroutine(CoAdCallbackTimeout(sessionId));
    }

    private void CancelAdCallbackTimeout()
    {
        if (m_adCallbackTimeoutCoroutine == null)
        {
            return;
        }

        StopCoroutine(m_adCallbackTimeoutCoroutine);
        m_adCallbackTimeoutCoroutine = null;
    }

    private IEnumerator CoAdCallbackTimeout(int sessionId)
    {
        yield return new WaitForSecondsRealtime(AdCallbackTimeoutSeconds);
        if (sessionId != m_adPlaybackSessionId || !m_isAdPlaying)
        {
            yield break;
        }

        HandleAdCallbackTimeout();
    }

    private void HandleAdCallbackTimeout()
    {
        // Unity 播激励时经常仍 isFocused，20s 不能当成「人已经回来」。
        // 无焦点或已有 impression：广告多半还在播，续期。
        bool adLikelyStillShowing = !Application.isFocused || m_adGotImpression;
        if (adLikelyStillShowing && m_adTimeoutExtendCount < AdCallbackTimeoutMaxExtendCount)
        {
            m_adTimeoutExtendCount++;
            StartAdCallbackTimeout();
            return;
        }

        var adType = m_currentAdPlaybackType;
        EndAdPlayback();

        // 兜底回调：避免 SDK 偶发不回调导致业务流程卡死
        switch (adType)
        {
            case AdPlaybackType.Reward:
                TriggerJavaDataCallback("AdsPlayFailed", "", "");
                break;
            case AdPlaybackType.Interstitial:
                TriggerJavaDataCallback("AdsPlayFailed", "", "");
                // 进关插屏超时也要放行 LoadLevel；激励失败不得走这条
                AdsPlayStrategy.NotifyEnterLevelInterstitialFinished();
                break;
        }
    }

    private void BeginAdPlayback()
    {
        if (m_isAdPlaying)
        {
            return;
        }

        m_isAdPlaying = true;
        m_adTimeoutExtendCount = 0;
        m_adGotImpression = false;
        m_adPlaybackSessionId++;
        if (m_currentAdPlaybackType == AdPlaybackType.Reward)
        {
            m_pendingRewardGrant = true;
        }

        StartAdCallbackTimeout();

        // 开播即清掉待机计时，防止广告焦点抖动把「真后台」时间接到关播后的 EnterForeground
        AdsPlayStrategy.ClearBackgroundTime();

        if (GameModule.Setting != null)
        {
            GameModule.Setting.PauseGame();
        }

#if UNITY_IOS && !UNITY_EDITOR
        // PauseGame 只停 FishFramework BGM；关卡 BGM 走 TileGameCore.MusicEmitter，iOS 广告需单独暂停
        PauseTileGameMusicForAd();
#endif
    }

    private void EndAdPlayback(bool markIgnoreClick = true)
    {
        if (!m_isAdPlaying)
        {
            return;
        }

        if (markIgnoreClick)
            MarkIgnoreClickAfterAd();
        CancelAdCallbackTimeout();
        m_adFinishedSessionId = m_adPlaybackSessionId;
        m_adFinishedPlaybackType = m_currentAdPlaybackType;
        m_isAdPlaying = false;
        m_adTimeoutExtendCount = 0;
        m_currentAdPlaybackType = AdPlaybackType.None;

#if UNITY_IOS && !UNITY_EDITOR
        ResumeTileGameMusicAfterAd();
#endif

        if (GameModule.Setting != null)
        {
            GameModule.Setting.ResumeGame();
        }

        // 关播后再异步落盘，避开广告回前台瞬间的主线程峰值
        ScheduleDeferredPlayerPrefsSave();
    }

    /// <summary>
    /// 仅当仍在播、类型匹配当前 session 时消费一次关播/结果回调。
    /// 迟到回调、激励/插屏串台回调直接丢弃。
    /// </summary>
    private bool TryConsumeAdResultCallback(AdPlaybackType expectedType)
    {
        return m_isAdPlaying && m_currentAdPlaybackType == expectedType;
    }

    private void MarkAdImpression(AdPlaybackType expectedType)
    {
        if (!m_isAdPlaying || m_currentAdPlaybackType != expectedType)
        {
            return;
        }

        m_adGotImpression = true;
    }

    /// <summary>
    /// 将 PlayerPrefs.Save 错开回前台峰值后再写盘，避免同步落盘造成主线程 ANR。
    /// （PlayerPrefs 本身非线程安全，此处用协程错峰，而非真正后台线程写盘）
    /// </summary>
    private void ScheduleDeferredPlayerPrefsSave()
    {
        if (m_deferredPrefsSaveCoroutine != null)
        {
            StopCoroutine(m_deferredPrefsSaveCoroutine);
        }

        m_deferredPrefsSaveCoroutine = StartCoroutine(CoDeferredPlayerPrefsSave());
    }

    private IEnumerator CoDeferredPlayerPrefsSave()
    {
        // 错开回前台当帧/次帧峰值，再落盘
        yield return null;
        yield return s_deferredPrefsDelay;
        try
        {
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Deferred PlayerPrefs.Save failed: {e.Message}");
        }

        m_deferredPrefsSaveCoroutine = null;
    }

#if UNITY_IOS && !UNITY_EDITOR
    private bool m_tileMusicPausedForAd;

    void PauseTileGameMusicForAd()
    {
        m_tileMusicPausedForAd = false;
        var music = MusicEmitter.ActiveMusicSource;
        if (music == null || music.AudioSource == null || !music.AudioSource.isPlaying)
        {
            return;
        }

        music.AudioSource.Pause();
        m_tileMusicPausedForAd = true;
    }

    void ResumeTileGameMusicAfterAd()
    {
        if (!m_tileMusicPausedForAd)
        {
            return;
        }

        m_tileMusicPausedForAd = false;
        var music = MusicEmitter.ActiveMusicSource;
        if (music == null || music.AudioSource == null)
        {
            return;
        }

        music.AudioSource.UnPause();
    }
#endif

    public static void ShowRewardAd(GameSdkNoneParamVoidCallback rewardedAdViewedCallback = null,
                        GameSdkNoneParamVoidCallback rewardedAdCanceledCallback = null,
                        GameSdkStringParamVoidCallback rewardedAdClickedCallback = null,
                        GameSdkStringParamVoidCallback rewardedAdImpressionCallback = null, string ad_place = "")
    {
        mCurAdPlace = string.IsNullOrEmpty(ad_place) ? "unknown" : ad_place;
        MilestoneSdk.RememberRewardScene(mCurAdPlace);
        if (Instance != null)
        {
            Instance.m_currentAdPlaybackType = AdPlaybackType.Reward;
            Instance.BeginAdPlayback();
        }
        AdSdk.ShowRewardAd(mCurAdPlace, "", "", rewardedAdViewedCallback, rewardedAdCanceledCallback, rewardedAdClickedCallback, rewardedAdImpressionCallback);
    }

    public static void ShowFullAd(GameSdkIntParamVoidCallback fullAdClosedCallback = null,
                        GameSdkStringParamVoidCallback fullAdClickedCallback = null,
                        GameSdkStringParamVoidCallback fullAdImpressionCallback = null, string ad_place = "")
    {
        mCurAdPlace = string.IsNullOrEmpty(ad_place) ? "unknown" : ad_place;

        if (Instance != null)
        {
            Instance.m_currentAdPlaybackType = AdPlaybackType.Interstitial;
            Instance.BeginAdPlayback();
        }
        AdSdk.ShowFullAd(mCurAdPlace, "", "", fullAdClosedCallback, fullAdClickedCallback, fullAdImpressionCallback);
    }

    public void RegisterAdsEvent()
    {
        // 初始化SDK回调处理器
        if (m_callbackHandler == null)
        {
            m_callbackHandler = new GPSDKCallbackHandler();
        }

        GameSDKCallbackListener.OnRewardAdViewedEvent += OnGPRewardAdViewed;
        GameSDKCallbackListener.OnRewardAdCanceledEvent += OnGPRewardAdCanceled;
        GameSDKCallbackListener.OnRewardAdClickedEvent += OnGPRewardAdClicked;
        GameSDKCallbackListener.OnRewardAdImpressionEvent += OnGPRewardAdImpression;

        // 注册插屏广告回调
        GameSDKCallbackListener.OnFullAdClosedEvent += OnGPFullAdClosed;
        GameSDKCallbackListener.OnFullAdClickedEvent += OnGPFullAdClicked;
        GameSDKCallbackListener.OnFullAdImpressionEvent += OnGPFullAdImpression;

        // Banner 加载在 SDK 内部循环刷新，业务侧不消费加载成功/失败事件
#if UNITY_IOS && !UNITY_EDITOR
        EnsureRemoteConfigEventRegistered();
#else
        GameSDKCallbackListener.OnRemoteConfigLoadedEvent += OnGPRemoteCfgLoadSuc;
#endif

        //注册 cmp 加载成功
        GameSDKCallbackListener.OnCMPFinishedEvent += OnCMPFinishedEvent;
    }

    public void OnGPFullAdClosed(int param)
    {
        // m_intertistAdLoaded = false; // 播放后需要重新加载
        bool isCurrentSession = TryConsumeAdResultCallback(AdPlaybackType.Interstitial);
        int closeSessionId = isCurrentSession
            ? m_adPlaybackSessionId
            : (!m_isAdPlaying
                && m_adFinishedPlaybackType == AdPlaybackType.Interstitial
                && m_adFinishedSessionId == m_adPlaybackSessionId
                    ? m_adFinishedSessionId
                    : -1);

        if (closeSessionId < 0 || closeSessionId == m_lastRecordedInterstitialCloseSessionId)
        {
            return;
        }

        if (param != 0)
        {
            if (isCurrentSession)
                EndAdPlayback(false);
            m_lastRecordedInterstitialCloseSessionId = closeSessionId;
            AdsPlayStrategy.NotifyEnterLevelInterstitialFinished();
            return;
        }

        if (isCurrentSession)
        {
            // 先结束播控再跑业务，避免仍在 Pause/IsAdPlaying 时叠 Revive/弹窗
            EndAdPlayback();
        }

        m_lastRecordedInterstitialCloseSessionId = closeSessionId;
        InterADABTestConfig.OnInterstitialClosed();
        MilestoneSdk.OnFullAdClosed(param);

        // 超时已经执行过失败兜底，只补记真实关播时间，不能再次推进业务回调。
        if (!isCurrentSession)
        {
            Debug.Log($"[GPSDKMgr] recorded late interstitial close session={closeSessionId}");
            return;
        }

        TriggerJavaDataCallback("AdsInterstitialSuc", "", "");
        AdsPlayStrategy.NotifyEnterLevelInterstitialFinished();
    }

    public void OnGPFullAdClicked(string param)
    {
        // 插屏广告点击
    }

    public void OnGPFullAdImpression(string param)
    {
        MarkAdImpression(AdPlaybackType.Interstitial);
    }

    public void OnCMPFinishedEvent(bool param1, int param2)
    {
        Debug.Log($"[AdDiag] CMP finished applies={param1} code={param2} initFlag={m_isAdInitSuc} callback={m_isAdInitCallbackSuc}");
        InitAdsBeforeLoad();
    }

    public void OnGPRewardAdViewed()
    {
        // m_rewardAdLoaded = false; // 播放后需要重新加载
        if (!TryConsumeAdResultCallback(AdPlaybackType.Reward))
        {
            // 超时已假失败时，看完仍应发奖（30s 广告常见），避免回到失败页点空白变成重开。
            // 当前若已在播另一条广告（通常是插屏），不能把激励发奖叠进去。
            if (m_pendingRewardGrant && !m_isAdPlaying && m_adFinishedPlaybackType == AdPlaybackType.Reward)
            {
                m_pendingRewardGrant = false;
                MilestoneSdk.OnRewardedAdClosed();
                TriggerJavaDataCallback("RewardAdsPlaySuc", "", "");
            }

            return;
        }

        EndAdPlayback();
        m_pendingRewardGrant = false;
        MilestoneSdk.OnRewardedAdClosed();
        TriggerJavaDataCallback("RewardAdsPlaySuc", "", "");
    }

    public void OnGPRewardAdCanceled()
    {
        if (!TryConsumeAdResultCallback(AdPlaybackType.Reward))
        {
            // 看完后点「返回游戏」有的 SDK 会再发 Cancel；迟到 Cancel 不撤掉待发奖。
            return;
        }

        EndAdPlayback();
        m_pendingRewardGrant = false;
        TriggerJavaDataCallback("AdsPlayFailed", "", "");
    }

    public void OnGPRewardAdClicked(string param1)
    {

    }

    public void OnGPRewardAdImpression(string param2)
    {
        MarkAdImpression(AdPlaybackType.Reward);
    }

    void OnGPRemoteCfgLoadSuc(bool param)
    {
#if UNITY_IOS && !UNITY_EDITOR
        ApplyIosRemoteConfigResult(param, param ? "native_callback_ok" : "native_callback_fail");
#else
        MilestoneSdk.NotifyRemoteConfigLoaded(param);
        TileAppUpdate.NotifyRemoteConfigLoaded(param);
#endif
        InterADABTestConfig.MarkRemoteConfigReady();
        if (InterADABTestConfig.TryAssignGroup() && GPUserDataManager.HasInit())
            GameBAData.ExcuteCommonEvent();
    }

#if UNITY_IOS && !UNITY_EDITOR
    float m_remoteConfigWaitStartRealtime = -1f;

    static string FormatInternetReachability()
    {
        return Application.internetReachability.ToString();
    }

    void ApplyIosRemoteConfigResult(bool success, string source)
    {
        if (m_remoteConfigResolved)
        {
            return;
        }

        m_remoteConfigResolved = true;
        m_remoteConfigLoadSuccess = success;
        if (success)
        {
            m_isUnderReviewIOS = FirebaseSdk.GetBoolRemoteConfig(RemoteConfigKeyIsUnderReviewIOS);
        }
        else
        {
            // 拉取失败：按提审处理，避免首启误进第一关
            m_isUnderReviewIOS = true;
        }

        float elapsed = m_remoteConfigWaitStartRealtime >= 0f
            ? Time.realtimeSinceStartup - m_remoteConfigWaitStartRealtime
            : -1f;
        string net = FormatInternetReachability();
        if (success)
        {
            Debug.Log(
                $"[GPSDKMgr][RCDiag] READY ok source={source} elapsed={elapsed:F2}s net={net} " +
                $"{RemoteConfigKeyIsUnderReviewIOS}={m_isUnderReviewIOS}");
        }
        else
        {
            // source=native_callback_fail → 原生轮询结束仍未更新，常见于启动期无网/VPN；对照 Xcode 里 -1009 / FirebaseDiag fetch error
            // source=unity_timeout → Unity 12s 内未收到 OnRemoteConfigLoaded，查原生是否卡死/未发回调
            Debug.LogWarning(
                $"[GPSDKMgr][RCDiag] READY fail source={source} elapsed={elapsed:F2}s net={net} " +
                $"{RemoteConfigKeyIsUnderReviewIOS}={m_isUnderReviewIOS} (fallback under-review). " +
                "Hint: NotReachable/VPN → 网络；Reachable 仍 fail → 看原生 FirebaseDiag/Crashlytics -1009 或 RC key/默认值。");
        }

        InterADABTestConfig.MarkRemoteConfigReady();
        MilestoneSdk.NotifyRemoteConfigLoaded(success);
        if (InterADABTestConfig.TryAssignGroup() && GPUserDataManager.HasInit())
            GameBAData.ExcuteCommonEvent();
    }

    /// <summary>
    /// iOS 首启：等 Remote Config 回调（或原生 ForceFetch 状态位）后再做进关决策；超时按提审兜底。
    /// </summary>
    public IEnumerator WaitForRemoteConfigResolved()
    {
        m_remoteConfigWaitStartRealtime = Time.realtimeSinceStartup;
        TryApplyForceFetchNativeStatus();
        Debug.Log(
            $"[GPSDKMgr][RCDiag] WAIT begin timeout={RemoteConfigWaitTimeoutSeconds}s " +
            $"alreadyResolved={m_remoteConfigResolved} forceFetchResult={Tile2_GetForceFetchResult()} " +
            $"net={FormatInternetReachability()}");

        float elapsed = 0f;
        float nextHeartbeat = 2f;
        while (!m_remoteConfigResolved && elapsed < RemoteConfigWaitTimeoutSeconds)
        {
            if (TryApplyForceFetchNativeStatus())
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= nextHeartbeat)
            {
                Debug.Log(
                    $"[GPSDKMgr][RCDiag] WAIT heartbeat elapsed={elapsed:F1}s/{RemoteConfigWaitTimeoutSeconds}s " +
                    $"net={FormatInternetReachability()} resolved={m_remoteConfigResolved} " +
                    $"forceFetchResult={Tile2_GetForceFetchResult()}");
                nextHeartbeat += 2f;
            }

            yield return null;
        }

        if (!m_remoteConfigResolved)
        {
            TryApplyForceFetchNativeStatus();
        }

        if (!m_remoteConfigResolved)
        {
            ApplyIosRemoteConfigResult(false, "unity_timeout");
        }
        else
        {
            Debug.Log(
                $"[GPSDKMgr][RCDiag] WAIT end already-resolved success={m_remoteConfigLoadSuccess} " +
                $"elapsed={elapsed:F2}s net={FormatInternetReachability()} " +
                $"{RemoteConfigKeyIsUnderReviewIOS}={m_isUnderReviewIOS}");
        }
    }
#endif

    /// <summary>
    /// iOS 提审/价格审核开关（Firebase Remote Config）。true 时不自动进入第一关。
    /// Android / Editor 恒为 false，保持原逻辑。
    /// </summary>
    public bool IsUnderReviewIOS
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            // 未就绪时按提审；成功后再读一次远端值；失败保持 true
            if (!m_remoteConfigResolved)
            {
                return true;
            }

            if (m_remoteConfigLoadSuccess)
            {
                m_isUnderReviewIOS = FirebaseSdk.GetBoolRemoteConfig(RemoteConfigKeyIsUnderReviewIOS);
            }

            return m_isUnderReviewIOS;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// 初始化广告。Android Banner 由 InitAds 内部加载一次，业务侧不要再调用 LoadBannerAd。
    /// 插屏、激励、Banner 的加载都只能调用一次；这三个加载函数的回调没有作用，失败不能清标记再 Load。
    /// </summary>
    public void InitAdsBeforeLoad()
    {
        if(m_isAdInitSuc)
        {
            Debug.Log($"[AdDiag] InitAdsBeforeLoad skip already initFlag callback={m_isAdInitCallbackSuc}");
            return;
        }
        m_isAdInitSuc = true;
        Debug.Log("[AdDiag] InitAds call, waiting callback");

        AdSdk.InitAds(() =>
        {
            m_isAdInitCallbackSuc = true;
            Debug.Log($"[AdDiag] InitAds callback fullLoaded={AdSdk.IsFullAdLoaded()} rewardLoaded={AdSdk.IsRewardAdLoaded()}");
            GameBAData.ExcuteAdInitSuccess();
            CheckLoadAllAd();
#if UNITY_IOS && !UNITY_EDITOR
            // iOS MAX：banner 懒加载后再尝试展示；Android 保持旧行为（不在 init 回调里 CheckShow）。
            SafeLoadBannerAd();
            CheckShowAdBanner();
#endif
        });
    }


    // 以下渠道需特殊处理: 早开广告按钮(3关)、adsourceType=1、显示TotalWin
    // exmox=exmox_int, prodege=inboxdollars_int, ayet=ayetstudios_int, almedia=freecash_int, adjoe=adjoe_int
    // maf=myappfre_int, torox=offertoronew_int, mistplay=mistplay_int, snakzy=helisplaynnf_int, taurus=taurus_int, playio=gnacompany_int
    // adaction/adaction5/appsamurai/primeis 与 AJStoryMatch3 激励渠道列表保持一致

    string[] mediaSourceArr = new[] { "exmox", "inboxdollars", "ayetstudios", "freecash", "adjoe", "myappfre", "offertoronew", "mistplay", "helisplaynnf", "taurus", "gnacompany", "adaction", "adaction5", "appsamurai", "primeis" };

    /// <summary>
    /// 是否为需特殊处理的媒体渠道(早开广告、adsourceType=1等)
    /// </summary>
    public bool GetBIsMediaSourceType1()
    {
        if (GPUserDataManager.Instance.GetAdSourceTypeB1())//如果本地数据有了就返回true
        {
            return true;
        }


        Debug.Log("networkya   " + AttrSdk.GetAttrNetwork());
        var networkStr = AttrSdk.GetAttrNetwork();
        foreach (var item in mediaSourceArr)
        {
            if (networkStr.Contains(item, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 是否显示TotalWin（与 TileClient 保持一致）
    /// </summary>
    public bool GetBShowTotalWin()
    {
        string[] channelNameList = new[] { "Freecash", "Adjoe", "MyFreeApp", "Torox", "Mistplay", "Snakzy", "Taurus", "AdAction", "AdAction5", "AppSamurai", "PrimeIS" };
        var mediaSource = GetMediaSource();
        foreach (var item in channelNameList)
        {
            if (mediaSource.Contains(item, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return channelNameList.Contains(mediaSource);
    }

    //用户数据约等于登录 
    public void PostUserData(Action uidGottonAct) //正常情况下这就是登录.
    {
        var usrDataMgr = GPUserDataManager.Instance;
        string remoteUid = RemoteDataSdk.GetUid();
        bool hasUid = false;

        Debug.Log($"[AdDiag] PostUserData begin remoteUid={remoteUid} localUid={usrDataMgr.GetUID()} head={usrDataMgr.UserData.userhead}");
        if (!remoteUid.IsNullOrWhitespace() && remoteUid.Length > 2)
        {
            usrDataMgr.SetUID(remoteUid);
            uidGottonAct?.Invoke();
            hasUid = true;

            ThinkingDataSdk.BoundAccountId(remoteUid);
            if (InterADABTestConfig.TryAssignGroup())
                GameBAData.ExcuteCommonEvent();
        }

        string uid = usrDataMgr.GetUID();
        string data = "";
        string properties = "{}";
        string name = usrDataMgr.UserData.username;
        string avatar = usrDataMgr.UserData.userhead.ToString();
        Dictionary<string, string> rankParam = new();

        RemoteDataSdk.PostUserData(() =>
        {
            string backUid = RemoteDataSdk.GetUid();
            Debug.Log($"[AdDiag] PostUserData callback remoteUid={backUid} hadUid={hasUid}");
            usrDataMgr.SetUID(backUid);
            ThinkingDataSdk.BoundAccountId(backUid);
            if (InterADABTestConfig.TryAssignGroup())
                GameBAData.ExcuteCommonEvent();
            if (!hasUid)
            {
                uidGottonAct?.Invoke();
            }
            hasUid = true;
        }, uid, data, properties, name, avatar, rankParam);
    }


    //平台模块代码
    private void CallAndroid(string funcName, params object[] args)
    {

#if UNITY_ANDROID && !UNITY_EDITOR
            if (m_jc == null)
            {
                InitAndroidSdk();
            }

            try
            {
                if (jo != null)
                {
                    jo.Call(funcName, args);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
#endif
    }

    public void InitAndroidSdk()
    {
        Debug.Log("InitAndroidSdk");
#if (UNITY_ANDROID && !UNITY_EDITOR)
            m_jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            jo = m_jc.GetStatic<AndroidJavaObject>("currentActivity");
#endif
    }


    private void CallPlatformFunc(string funcName, params object[] args)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        CallAndroid(funcName, args);
#endif
    }


#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void Tile2HideSplash();
#endif

    public void HideSplashScreen()
    {
#if UNITY_IOS && !UNITY_EDITOR
        Tile2HideSplash();
#elif UNITY_ANDROID && !UNITY_EDITOR
        CallPlatformFunc("hideSplashScreen");
#endif
    }


    //敏感词相关
    public void CheckSensitiveWords(string checkStr)
    {

    }


    public bool GetBHideBanner()
    {
        return false;
    }

    public string GetDeviceId() => m_deiveId;

    public long GetReisterTime() => m_userRegisterTime;

    public string GetCountryCode() => m_countryCode;

    /// <summary>
    /// 是否 T3 国家。只用系统 Locale 国家码，不用 SDK LoginCountry / 存档。
    /// Android：java.util.Locale.getDefault().getCountry()。
    /// 巴西归 T2，墨西哥归 T3。葡语 / 西语不按语言兜成 T3。
    /// </summary>
    public bool GetBT3Country()
    {
        return IsT3CountryCode(GetSystemCountryCode());
    }

    /// <summary>国家码是否在 T1 或 T2 表里。不读系统 Locale，也不看系统语言。</summary>
    public bool IsT1T2CountryCode(string countryCode)
    {
        return IsT1CountryCode(countryCode) || IsT2CountryCode(countryCode);
    }

    public bool IsT1CountryCode(string countryCode)
    {
        return !string.IsNullOrEmpty(countryCode) && T1CountryCodes.Contains(countryCode);
    }

    public bool IsT2CountryCode(string countryCode)
    {
        return !string.IsNullOrEmpty(countryCode) && T2CountryCodes.Contains(countryCode);
    }

    /// <summary>按已有国家码分 T1/T2 与 T3。不在表里时，仍用系统语言兜底。</summary>
    public bool IsT3CountryCode(string countryCode)
    {
        if (!string.IsNullOrEmpty(countryCode))
        {
            if (IsT1T2CountryCode(countryCode))
                return false;
            if (T3CountryCodes.Contains(countryCode))
                return true;
        }

        return IsT3SystemLanguage(Application.systemLanguage);
    }

    /// <summary>系统国家码。Android 走原生 Locale.getDefault()，不经过 GameSDK。</summary>
    public string GetSystemCountryCode()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var localeClass = new AndroidJavaClass("java.util.Locale"))
            using (var locale = localeClass.CallStatic<AndroidJavaObject>("getDefault"))
            {
                string country = locale != null ? locale.Call<string>("getCountry") : "";
                if (!string.IsNullOrEmpty(country))
                    return country;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GPSDKMgr] Locale.getDefault getCountry failed: " + e.Message);
        }
#endif
        return TryGetSystemLocaleCountryCode(out string localeCountry) ? localeCountry : "";
    }

    static bool TryGetSystemLocaleCountryCode(out string countryCode)
    {
        countryCode = "";
        string name = CultureInfo.CurrentCulture?.Name;
        if (string.IsNullOrEmpty(name))
            return false;

        int dash = name.LastIndexOf('-');
        if (dash < 0 || dash + 3 != name.Length)
            return false;

        countryCode = name.Substring(dash + 1);
        return countryCode.Length == 2;
    }

    static bool IsT3SystemLanguage(SystemLanguage language)
    {
        switch (language)
        {
            case SystemLanguage.Indonesian:
            case SystemLanguage.Turkish:
            case SystemLanguage.Ukrainian:
            case SystemLanguage.Russian:
            case SystemLanguage.Thai:
            case SystemLanguage.Vietnamese:
            case SystemLanguage.Arabic:
            case SystemLanguage.Hindi:
                return true;
            default:
                return false;
        }
    }

    // 传统买量梯度：高 eCPM 发达市场为 T1，南欧 / 中东欧 / 海湾 / 港澳台为 T2。巴西按官方常归更低档，这里固定放 T2。
    static readonly HashSet<string> T1CountryCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "US", "GB", "UK", "CA", "AU", "NZ",
        "DE", "FR", "NL", "CH", "AT", "BE", "IE", "LU", "IS", "LI",
        "SE", "NO", "DK", "FI",
        "JP", "KR", "SG"
    };

    static readonly HashSet<string> T2CountryCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "BR",
        "IT", "ES", "PT", "GR", "CY", "MT",
        "PL", "CZ", "HU", "RO", "SK", "SI", "HR", "EE", "LV", "LT",
        "TW", "HK", "MO",
        "IL", "AE", "SA", "QA", "KW", "BH", "OM"
    };

    static readonly HashSet<string> T3CountryCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ID", "TR", "RU", "UA", "IN", "VN", "TH", "PH",
        "MX",
        "PK", "BD", "MY", "CO", "AR", "PE", "CL",
        "VE", "EC", "BO", "GT", "HN", "NI", "SV", "PY", "UY", "DO",
        "IQ", "IR", "NP", "LK", "MM", "KH",
        // 非洲一律 T3
        "DZ", "AO", "BJ", "BW", "BF", "BI", "CM", "CV", "CF", "TD",
        "KM", "CG", "CD", "CI", "DJ", "EG", "GQ", "ER", "SZ", "ET",
        "GA", "GM", "GH", "GN", "GW", "KE", "LS", "LR", "LY", "MG",
        "MW", "ML", "MR", "MU", "MA", "MZ", "NA", "NE", "NG", "RW",
        "ST", "SN", "SC", "SL", "SO", "ZA", "SS", "SD", "TZ", "TG",
        "TN", "UG", "ZM", "ZW", "EH"
    };

    public string GetMediaSource()
    {
        return AttrSdk.GetAttrNetwork();
    }

    public bool GetBShowNotificationRequested() => ShouldShowNotificationPrompt();

    public string GetCompleteOrderStr() => m_completeOrdersStr;

    /// <summary>
    /// 启动/主界面查询未完成订单。
    /// iOS：消耗型补单新逻辑；Android：保持 origin 旧 Restore 路径，避免双端行为被 iOS 改造带偏。
    /// </summary>
    public void QueryCompleteOrders()
    {
#if UNITY_IOS
        QueryConsumableCompleteOrders();
#else
        ReqRestoreInappPurchase();
#endif
    }

    public void ShowUMP()
    {
        UpdateCMPSDK();
    }

    public void InitSDK()
    {
    }

    public void InitOthers()
    {
    }

    public void InitFirebase()
    {
#if UNITY_IOS && !UNITY_EDITOR
        InitFirebaseSDKOnce();
#else
        FirebaseSdk.InitFirebaseSDK();
#endif
    }

    public void UserFeedbackInGame(string str)
    {
        UtilSdk.SendEmail("support@example.com", "feedback", str ?? "");
    }

    public void UserFeedbackRate(string str, int star)
    {
        UtilSdk.SendEmail("support@example.com", "rate", $"{star}\n{str}");
    }

    public void RateInGoogleStore()
    {
        Application.OpenURL(GameData.AppUrl);
    }

    public void OnShortcutAction(string msg1)
    {
        if (msg1 != null && msg1.Equals("JumpBugReport"))
        {
            m_bShowFeedback = true;
        }
    }

    public bool GetBShowFeedback() => m_bShowFeedback;

    public void ResetBShowFeedback()
    {
        m_bShowFeedback = false;
    }


    public bool GetBCMPShow()
    {
        return CMPSdk.IsGDPRCountry();
    }

    public void OpenServiceCenter()
    {
        string uid = GPUserDataManager.Instance != null ? GPUserDataManager.Instance.GetUID() : "";
        CallPlatformFunc("OpenServiceCenter", sofishAppId, uid, "", "");
    }

    //时间接口绑定
    public void TrackEvent(string key, string content)
    {
        if (!m_thinkingDataInited)
        {
            m_pendingTrackEvents.Add((key, content));
            return;
        }

        ThinkingDataSdk.LogEvent(key, content);
    }

    public void TrackCommonEvent(string eventName, string param)
    {
        if (!m_thinkingDataInited)
        {
            m_pendingTrackEvents.Add((eventName, param));
            return;
        }

        ThinkingDataSdk.LogEvent(eventName, param);
    }

    public void TrackAppsFlyerEvent(string eventName, string param)
    {
        var dict = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(param))
        {
            dict["payload"] = param;
        }
        AttrSdk.TrackEvent(eventName, dict);
    }

    public void TrackFirebaseEvent(string eventName, string param)
    {
        var dict = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(param))
        {
            dict["payload"] = param;
        }
        FirebaseSdk.LogEvent(eventName, dict);
    }

    // ========== 以下方法用于匹配 SDKMgr 的广告接口 ==========

    //奖励广告类型
    public int GetRewardAdType()
    {
        return m_rewardAdType;
    }

    //插屏广告类型
    public int GetInterstitAdType()
    {
        return m_intertistAdType;
    }



    // 触发 Java 数据回调，用于兼容 SDKMgr 的回调机制
    public void TriggerJavaDataCallback(string key, string content, string uid)
    {
        // 优先使用SDK回调处理器处理业务逻辑
        if (m_callbackHandler != null)
        {
            m_callbackHandler.OnRecieveJavaData(key, content, uid);
        }

        // 兼容旧的回调机制
        if (onRecieveJavaDataFunc != null)
        {
            try
            {
                onRecieveJavaDataFunc.Invoke(key, content, uid);
            }
            catch (Exception e)
            {
                Debug.LogError($"TriggerJavaDataCallback: Exception when invoking callback for key = {key}, error: {e.Message}");
            }
        }

        // UIAdsLoading 仅在播放阶段显示，加载阶段不显示，所以只关心播放结果
        switch (key)
        {
            case "ShortcutToggleGame":
                NotifyShortcutToggleGameRequested();
                break;
            case "AdsPlayFailed":
            case "RewardAdsPlaySuc":
            case "AdsInterstitialSuc":
                UIController.HidePage<UIAdsLoading>();
                break;
        }
    }

    public void NotifyShortcutToggleGameRequested()
    {
        OnShortcutToggleGameRequested?.Invoke();
    }


    //数数ID回传给attrSDK
    //数数和appflyer互动
    //BindShushuAndAppflyer


    // public void BindShushuAndAppflyer()
    // {
    //     Debug.Log("GetAttrNetwork  = " + AttrSdk.GetAttrNetwork());
    //     var eventName = "shushu_message";
    //     Dictionary<string, object> dict = new();
    //     dict["event_value"] = ThinkingDataSdk.GetDistinctId();
    //     AttrSdk.TrackEvent(eventName, dict);
    // }

    private bool m_bPayQuerySuc = false;
#if UNITY_IOS
    private bool m_payQueryRetryScheduled = false;
#endif

    //支付相关
    public void QueryCurrencyInfo()
    {
        if (m_bPayQuerySuc)
        {
            return;
        }

#if UNITY_IOS
        BillingSdk.QueryInappProductDetails(OnQueryCurrencyInfoSuccess, OnQueryCurrencyInfoFail);
#else
        // Android：保持旧行为（失败不自动重试）
        BillingSdk.QueryInappProductDetails(() =>
        {
            m_bPayQuerySuc = true;
            GameEvent.Send((int)EventNameDef.ReqQueryShopProductSuc);
        }, null);
#endif
    }

#if UNITY_IOS
    void OnQueryCurrencyInfoSuccess()
    {
        m_bPayQuerySuc = true;
        m_payQueryRetryScheduled = false;
        GameEvent.Send((int)EventNameDef.ReqQueryShopProductSuc);
    }

    void OnQueryCurrencyInfoFail(int errorCode)
    {
        Debug.Log($"QueryInappProductDetails failed, errorCode={errorCode}");
        if (m_bPayQuerySuc || m_payQueryRetryScheduled)
        {
            return;
        }

        m_payQueryRetryScheduled = true;
        MainBehaviour.Instance.StartCoroutine(RetryQueryCurrencyInfoCoroutine());
    }

    IEnumerator RetryQueryCurrencyInfoCoroutine()
    {
        yield return new WaitForSecondsRealtime(2f);
        if (m_bPayQuerySuc)
        {
            yield break;
        }

        BillingSdk.QueryInappProductDetails(OnQueryCurrencyInfoSuccess, code =>
        {
            Debug.Log($"QueryInappProductDetails retry failed, errorCode={code}");
        });
    }
#endif

    public bool GetBPayQuerySuccess()
    {
        return m_bPayQuerySuc;
    }

    //支付相关
    public void TryPay(string storeProdId)
    {
        if (!IsBillingReady())
        {
            Debug.LogWarning($"[GPSDKMgr] Billing not ready, skip purchase. productId={storeProdId}");
            return;
        }

        if (storeProdId == GameData.Pay.GetNoAdsGId())
        {
            StartCoroutine(TryPayNoAdProductCoroutine(storeProdId));
            return;
        }

        DoTryPay(storeProdId);
    }

    void DoTryPay(string storeProdId)
    {
        if (!IsBillingReady())
        {
            Debug.LogWarning($"[GPSDKMgr] Billing not ready, skip purchase. productId={storeProdId}");
            EndPayLoadingGuard();
            GameModule.UI.SetBLoading(false);
            return;
        }

        Debug.Log("购买商品ID为=  " + storeProdId);
        GameModule.UI.SetBLoading(true);
        mCurPurchaseId = storeProdId;
        BeginPayLoadingGuard();
        BillingSdk.Purchase(storeProdId, null, null);
    }

    Coroutine m_payLoadingGuardCoroutine;
    const float PayLoadingGuardSeconds = 30f;

    /// <summary>购买/恢复回调若因 native SendMessage 丢失，避免 loading 永久卡住。</summary>
    void BeginPayLoadingGuard()
    {
        if (m_payLoadingGuardCoroutine != null)
        {
            StopCoroutine(m_payLoadingGuardCoroutine);
        }

        m_payLoadingGuardCoroutine = StartCoroutine(PayLoadingGuardCoroutine());
    }

    void EndPayLoadingGuard()
    {
        if (m_payLoadingGuardCoroutine != null)
        {
            StopCoroutine(m_payLoadingGuardCoroutine);
            m_payLoadingGuardCoroutine = null;
        }
    }

    IEnumerator PayLoadingGuardCoroutine()
    {
        yield return new WaitForSecondsRealtime(PayLoadingGuardSeconds);
        m_payLoadingGuardCoroutine = null;
        Debug.LogWarning($"[GPSDKMgr] Pay loading guard timeout ({PayLoadingGuardSeconds}s), force hide loading. mCurPurchaseId={mCurPurchaseId}");
        GameModule.UI.SetBLoading(false);
        FinishBillingRestoreInFlight();
#if UNITY_IOS
        CompleteRestorePayWait(false);
#endif
    }

    /// <summary>
    /// iOS 免广告：删包后 App Store 仍可能记录已购。点「购买」时先尝试恢复（不二次扣费），
    /// 确认未购买过再走正常 IAP；反馈与设置页恢复购买一致。
    /// </summary>
    IEnumerator TryPayNoAdProductCoroutine(string storeProdId)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!GPUserDataManager.Instance.GetBNoAd())
        {
            GameModule.UI.SetBLoading(true);
            BeginPayLoadingGuard();
            BeginRestorePayWait();
            ReqManualRestoreNoAdPurchase();
            yield return WaitRestorePayComplete();
            if (GetRestorePaySucceeded() && TryGrantManualRestoredNoAd())
            {
                EndPayLoadingGuard();
                GameModule.UI.SetBLoading(false);
                yield break;
            }
        }
#endif
        yield return null;
        DoTryPay(storeProdId);
    }

    private string mCurPurchaseId = "";
    public void Purchase(string productId, GameSdkStringStringStringParamVoidCallback successCallback, GameSdkStringIntParamVoidCallback failCallback)
    {
        if (!IsBillingReady())
        {
            Debug.LogWarning($"[GPSDKMgr] Billing not ready, skip purchase. productId={productId}");
            return;
        }

        GameModule.UI.SetBLoading(true);
        BillingSdk.Purchase(productId, successCallback, failCallback);
        GameBAData.ExcuteCreateOrder(productId, "", "");
        mCurPurchaseId = productId;
    }

    public void RegisterBillingEvent()
    {
        GameSDKCallbackListener.OnPurchaseSuccessEvent += OnGPSDKPurchaseSuccessEvent;
        GameSDKCallbackListener.OnPurchaseFailEvent += OnGPSDKPurchaseFailEvent;
        GameSDKCallbackListener.OnConsumeSuccessEvent += OnGPSDKConsumeSuccessEvent;
    }

    //[[""]]//
    // 一个预先定义的(string,string)函数，表示恢复购买成功后要执行的回调。其中的
    // 1. 第一个string参数表示恢复购买成功的商品id。如果有多个商品id，这些id之间使用’&‘符号进行拼接。
    // 2. 第二个string参数表示恢复购买成功的商品对应的订单id。如果有多个订单id，这些id之间使用’&‘符号进行拼接。
    public void OnGPSDKPurchaseSuccessEvent(string param1, string param2, string param3)
    {
        EndPayLoadingGuard();
        GameModule.UI.SetBLoading(false);

        Debug.Log($"OnGPSDKPurchaseSuccessEvent p1 {param1} p2 {param2}");

        if (string.IsNullOrWhiteSpace(param1))
        {
            param1 = mCurPurchaseId;
        }

        DeliverThenConfirm(param1, fromRestore: false);
        GameBAData.ExcutePaymentResult(param1, param3, param2, true, 0, "purchase_success");
    }

    const string PendingFinalizePrefKey = "gp_billing_pending_finalize";

    /// <summary>去广告看本地权益；消耗型看「已发货未确认」标记。同一笔不再开一次领奖。</summary>
    static bool ShouldDeliverStoreReward(string storeProdId, bool alreadyNoAd, bool grantAlreadyPersisted)
    {
        if (IsRestorableNoAdsProduct(storeProdId))
        {
            return !alreadyNoAd;
        }

        return !grantAlreadyPersisted;
    }

    static bool IsGrantAlreadyDelivered(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return false;
        }

        string raw = PlayerPrefs.GetString(PendingFinalizePrefKey, "");
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        return raw.Split('&').Contains(productId);
    }

    static void MarkGrantDelivered(string productId)
    {
        if (string.IsNullOrEmpty(productId) || IsGrantAlreadyDelivered(productId))
        {
            return;
        }

        string raw = PlayerPrefs.GetString(PendingFinalizePrefKey, "");
        PlayerPrefs.SetString(PendingFinalizePrefKey, string.IsNullOrEmpty(raw) ? productId : raw + "&" + productId);
    }

    static void ClearGrantDelivered(string productId)
    {
        if (string.IsNullOrEmpty(productId))
        {
            return;
        }

        string raw = PlayerPrefs.GetString(PendingFinalizePrefKey, "");
        if (string.IsNullOrEmpty(raw))
        {
            return;
        }

        var left = raw.Split('&').Where(id => id.Length > 0 && id != productId).ToList();
        PlayerPrefs.SetString(PendingFinalizePrefKey, string.Join("&", left));
        PlayerPrefs.Save();
    }

    /// <summary>先发货并落盘，再确认订单。去广告的确认是 acknowledge。补单才看已发货标记。</summary>
    void DeliverThenConfirm(string storeProdId, bool fromRestore)
    {
        if (string.IsNullOrEmpty(storeProdId))
        {
            return;
        }

        bool deliver = ShouldDeliverStoreReward(
            storeProdId,
            GPUserDataManager.Instance.GetBNoAd(),
            fromRestore && IsGrantAlreadyDelivered(storeProdId));
        if (deliver)
        {
            if (IsRestorableNoAdsProduct(storeProdId))
            {
                UIGameFunc.ShowNoAdsPurchaseSuccessFlow();
            }
            else
            {
                BuySuccessSendItemToUsr(storeProdId);
                MarkGrantDelivered(storeProdId);
            }

            GPUserDataManager.Instance.SaveData();
            PlayerPrefs.Save();
        }

        ConfirmStorePurchase(storeProdId);
    }

    void ConfirmStorePurchase(string productId)
    {
        BillingSdk.Consume(productId, null, (id, code) =>
        {
            Debug.LogWarning($"[GPSDKMgr] purchase confirm failed id={id} code={code}");
        });
    }

    void BuySuccessSendItemToUsr(string storeProdId)
    {
        //购买回调成功.
        if (!storeProdId.Equals(GameData.Pay.GetNoAdsGId()))
        {
            UIGameFunc.ShowCommonBuySucDlg(storeProdId);
            GameEvent.Send<string>((int)EventNameDef.BuyProductSuccess, storeProdId);
        }
        else
        {
            UIGameFunc.ShowNoAdsPurchaseSuccessFlow();
        }
    }

    // 一个(string, int)函数，表示购买成功后要执行的回调。其中的
    // string参数表示购买的商品id
    // int参数表示错误代码，你可以参考：
    public void OnGPSDKPurchaseFailEvent(string param1, int param2)
    {
        EndPayLoadingGuard();
        GameModule.UI.SetBLoading(false);

        Debug.Log($"OnGPSDKPurchaseFailEvent p1 {param1} p2 {param2}");
        //失败没有orderId
        if (string.IsNullOrWhiteSpace(param1))
        {
            param1 = mCurPurchaseId;
        }
        GameBAData.ExcutePaymentResult(param1, "", "", true, param2, "payment_failed");
    }

    public void OnGPSDKConsumeSuccessEvent(string param1)
    {
        Debug.Log($"OnGPSDKConsumeSuccessEvent p1 {param1}");
        string confirmedId = param1;
        if (string.IsNullOrWhiteSpace(param1))
        {
            param1 = mCurPurchaseId;
        }
        GameBAData.ExcutePaymentResult(param1, "", "", true, 0, "consume_success");
        if (!string.IsNullOrWhiteSpace(confirmedId))
        {
            ClearGrantDelivered(confirmedId);
        }
    }

    private enum RestoreQueryPurpose
    {
        None,
        ConsumableOrders,
        ManualNoAdRestore,
    }

    private RestoreQueryPurpose m_restoreQueryPurpose;
    private bool m_billingRestoreInFlight;

#if UNITY_IOS
    /// <summary>启动/后台：查询消耗型补单，回调只入队 consumable，不处理免广告。</summary>
    void QueryConsumableCompleteOrders()
    {
        RestoreInappPurchaseWithPurpose(RestoreQueryPurpose.ConsumableOrders);
    }

    /// <summary>设置页：恢复免广告（non-consumable），与消耗型补单分离。</summary>
    public void ReqManualRestoreNoAdPurchase()
    {
        RestoreInappPurchaseWithPurpose(RestoreQueryPurpose.ManualNoAdRestore);
    }

    void RestoreInappPurchaseWithPurpose(RestoreQueryPurpose purpose)
    {
        if (m_billingRestoreInFlight)
        {
            Debug.Log($"RestoreInappPurchase skipped: in flight, requested purpose={purpose}");
            return;
        }

        m_restoreQueryPurpose = purpose;
        m_billingRestoreInFlight = true;
        BillingSdk.RestoreInappPurchase(CbInappProductRestoreId, CbInappProductRestoreFail);
    }
#else
    bool m_androidSettingsRestore;
    bool m_androidSettingsRestoreDone;
    bool m_androidSettingsFoundNoAd;
    Coroutine m_androidRestoreTimeout;

    public bool IsBillingRestoreInFlight => m_billingRestoreInFlight;
    public bool AndroidSettingsRestoreDone => m_androidSettingsRestoreDone;
    public bool AndroidSettingsFoundNoAd => m_androidSettingsFoundNoAd;

    /// <summary>Android 补单：Restore 成功后写入 restoreProductIdList，由 CheckShowRestoreItem 发奖。</summary>
    public void ReqRestoreInappPurchase()
    {
        if (m_billingRestoreInFlight)
        {
            Debug.Log("ReqRestoreInappPurchase skipped: in flight");
            return;
        }

        StartAndroidRestoreQuery(false);
    }

    /// <summary>设置页恢复去广告。本地已有权益时由调用方直接提示，这里只查单。</summary>
    public void BeginAndroidSettingsNoAdRestore()
    {
        m_androidSettingsRestoreDone = false;
        m_androidSettingsFoundNoAd = false;
        if (m_billingRestoreInFlight)
        {
            m_androidSettingsRestoreDone = true;
            return;
        }

        StartAndroidRestoreQuery(true);
    }

    void StartAndroidRestoreQuery(bool settings)
    {
        m_androidSettingsRestore = settings;
        m_billingRestoreInFlight = true;
        if (m_androidRestoreTimeout != null)
        {
            StopCoroutine(m_androidRestoreTimeout);
        }

        m_androidRestoreTimeout = StartCoroutine(CoAndroidRestoreTimeout());
        BillingSdk.RestoreInappPurchase(CbAndroidInappProductRestoreId, OnAndroidRestoreFail);
    }

    IEnumerator CoAndroidRestoreTimeout()
    {
        yield return new WaitForSecondsRealtime(12f);
        m_androidRestoreTimeout = null;
        if (!m_billingRestoreInFlight)
        {
            yield break;
        }

        Debug.LogWarning("[GPSDKMgr] Android restore query timeout");
        OnAndroidRestoreFail("", -1);
    }

    void StopAndroidRestoreTimeout()
    {
        if (m_androidRestoreTimeout == null)
        {
            return;
        }

        StopCoroutine(m_androidRestoreTimeout);
        m_androidRestoreTimeout = null;
    }

    private List<string> restoreProductIdList;

    void CbAndroidInappProductRestoreId(string param1, string param2, string param3)
    {
        Debug.Log("CbInappProductRestoreId " + param1 + "" + param2 + "" + param3);
        EndAndroidRestoreQuery();
        bool settings = m_androidSettingsRestore;
        m_androidSettingsRestore = false;

        if (string.IsNullOrWhiteSpace(param1))
        {
            restoreProductIdList = null;
            CompleteAndroidSettings(settings, false);
            return;
        }

        restoreProductIdList = param1.Split('&').Where(id => id.Length > 0).ToList();
        bool foundNoAd = restoreProductIdList.Any(id => IsRestorableNoAdsProduct(id))
            && !GPUserDataManager.Instance.GetBNoAd();
        if (settings && foundNoAd)
        {
            m_pendingManualNoAdRestore = true;
        }

        CompleteAndroidSettings(settings, foundNoAd);
    }

    void OnAndroidRestoreFail(string productId, int errorCode)
    {
        Debug.Log($"Android restore fail productId={productId} errorCode={errorCode}");
        EndAndroidRestoreQuery();
        bool settings = m_androidSettingsRestore;
        m_androidSettingsRestore = false;
        CompleteAndroidSettings(settings, false);
    }

    void EndAndroidRestoreQuery()
    {
        StopAndroidRestoreTimeout();
        FinishBillingRestoreInFlight();
    }

    void CompleteAndroidSettings(bool settings, bool foundNoAd)
    {
        if (!settings)
        {
            return;
        }

        m_androidSettingsFoundNoAd = foundNoAd;
        m_androidSettingsRestoreDone = true;
    }

    /// <summary>商店补单。去广告已有权益、或消耗型已发过货，只确认，不再开领奖窗。</summary>
    public bool CheckShowRestoreItem()
    {
        if (restoreProductIdList == null || restoreProductIdList.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < restoreProductIdList.Count; i++)
        {
            string item = restoreProductIdList[i];
            if (string.IsNullOrEmpty(item))
            {
                continue;
            }

            restoreProductIdList.RemoveAt(i);
            DeliverThenConfirm(item, fromRestore: true);
            return true;
        }

        return false;
    }

    void RemoveAndroidQueuedRestore(string productId)
    {
        if (string.IsNullOrEmpty(productId) || restoreProductIdList == null)
        {
            return;
        }

        restoreProductIdList.RemoveAll(id => id == productId);
    }
#endif

    void FinishBillingRestoreInFlight()
    {
        m_billingRestoreInFlight = false;
    }

#if UNITY_IOS
    private bool m_restorePayWaiting;
    private bool m_restorePayCompleted;
    private bool m_restorePaySucceeded;

    /// <summary>等待主界面消耗型补单 restore 结束，避免与设置页恢复并发导致回调丢失。</summary>
    public IEnumerator WaitBillingRestoreIdle(float timeoutSeconds = 8f)
    {
        float elapsed = 0f;
        while (m_billingRestoreInFlight && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (m_billingRestoreInFlight)
        {
            Debug.LogWarning("WaitBillingRestoreIdle timeout, force clear billing restore in-flight guard");
            FinishBillingRestoreInFlight();
        }
    }

    /// <summary>设置页手动恢复：等待 iOS Restore 成功/失败回调后再处理结果。</summary>
    public void BeginRestorePayWait()
    {
        m_restorePayWaiting = true;
        m_restorePayCompleted = false;
        m_restorePaySucceeded = false;
    }

    public bool GetRestorePaySucceeded() => m_restorePaySucceeded;

    public IEnumerator WaitRestorePayComplete(float timeoutSeconds = 15f)
    {
        float elapsed = 0f;
        while (!m_restorePayCompleted && elapsed < timeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!m_restorePayCompleted)
        {
            m_restorePayWaiting = false;
            m_restorePayCompleted = true;
            m_restorePaySucceeded = false;
            FinishBillingRestoreInFlight();
        }
    }

    void CompleteRestorePayWait(bool succeeded)
    {
        if (!m_restorePayWaiting)
        {
            return;
        }

        m_restorePayWaiting = false;
        m_restorePayCompleted = true;
        m_restorePaySucceeded = succeeded;
    }
#endif

    private List<string> m_pendingConsumableRestoreIds;
    private bool m_pendingManualNoAdRestore;

    private static bool IsRestorableNoAdsProduct(string storeProdId)
    {
        return !string.IsNullOrEmpty(storeProdId) && storeProdId == GameData.Pay.GetNoAdsGId();
    }

    private static bool IsConsumableStoreProduct(string storeProdId)
    {
        if (string.IsNullOrEmpty(storeProdId) || IsRestorableNoAdsProduct(storeProdId))
        {
            return false;
        }

        var list = ConfigHelper.Tables.TbPayProductCfg.DataList;
        foreach (var item in list)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (item.ProductIdIos == storeProdId)
            {
                return true;
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (item.ProductIdGoogle == storeProdId)
            {
                return true;
            }
#else
            if (item.ProductIdIos == storeProdId || item.ProductIdGoogle == storeProdId)
            {
                return true;
            }
#endif
        }

        return false;
    }

    void ResetRestoreQueryPurpose()
    {
        m_restoreQueryPurpose = RestoreQueryPurpose.None;
    }

    void CbInappProductRestoreId(string param1, string param2, string param3)
    {
        Debug.Log($"CbInappProductRestoreId purpose={m_restoreQueryPurpose} p1={param1} p2={param2} p3={param3}");

        FinishBillingRestoreInFlight();
        var purpose = m_restoreQueryPurpose;
        ResetRestoreQueryPurpose();

        if (string.IsNullOrWhiteSpace(param1))
        {
#if UNITY_IOS
            if (purpose == RestoreQueryPurpose.ManualNoAdRestore)
            {
                CompleteRestorePayWait(false);
            }
#endif
            return;
        }

        if (purpose == RestoreQueryPurpose.ManualNoAdRestore)
        {
            HandleManualNoAdRestoreCallback(param1);
            return;
        }

        if (purpose == RestoreQueryPurpose.ConsumableOrders)
        {
            HandleConsumableRestoreCallback(param1);
        }
    }

    void HandleManualNoAdRestoreCallback(string productIds)
    {
        if (GPUserDataManager.Instance.GetBNoAd())
        {
#if UNITY_IOS
            CompleteRestorePayWait(false);
#endif
            return;
        }

        bool foundNoAds = false;
        foreach (var id in productIds.Split('&'))
        {
            if (!IsRestorableNoAdsProduct(id))
            {
                continue;
            }

            m_pendingManualNoAdRestore = true;
            foundNoAds = true;
            break;
        }

#if UNITY_IOS
        CompleteRestorePayWait(foundNoAds);
#endif
    }

    void HandleConsumableRestoreCallback(string productIds)
    {
        m_pendingConsumableRestoreIds ??= new List<string>();
        foreach (var id in productIds.Split('&'))
        {
            if (!IsConsumableStoreProduct(id) || m_pendingConsumableRestoreIds.Contains(id))
            {
                continue;
            }

            m_pendingConsumableRestoreIds.Add(id);
        }
    }

    void CbInappProductRestoreFail(string productId, int errorCode)
    {
        Debug.Log($"CbInappProductRestoreFail purpose={m_restoreQueryPurpose} productId={productId} errorCode={errorCode}");

        FinishBillingRestoreInFlight();
        var purpose = m_restoreQueryPurpose;
        ResetRestoreQueryPurpose();

#if UNITY_IOS
        if (purpose == RestoreQueryPurpose.ManualNoAdRestore)
        {
            CompleteRestorePayWait(false);
        }
#endif
    }

    /// <summary>设置页恢复购买：仅免广告、本地无权益时发放一次，反馈同 IAP 购买。</summary>
    public bool TryGrantManualRestoredNoAd()
    {
        if (!m_pendingManualNoAdRestore || GPUserDataManager.Instance.GetBNoAd())
        {
            m_pendingManualNoAdRestore = false;
            return false;
        }

        m_pendingManualNoAdRestore = false;
        string noAdsGId = GameData.Pay.GetNoAdsGId();
#if !UNITY_IOS
        RemoveAndroidQueuedRestore(noAdsGId);
#endif
        DeliverThenConfirm(noAdsGId, fromRestore: true);
        return true;
    }

    /// <summary>进商店：处理消耗型补单队列，发奖并 consume（不含免广告）。</summary>
    public bool CheckConsumableRestoreOrders()
    {
        if (m_pendingConsumableRestoreIds == null || m_pendingConsumableRestoreIds.Count == 0)
        {
            return false;
        }

        for (int i = m_pendingConsumableRestoreIds.Count - 1; i >= 0; i--)
        {
            var item = m_pendingConsumableRestoreIds[i];
            if (string.IsNullOrEmpty(item) || !IsConsumableStoreProduct(item))
            {
                m_pendingConsumableRestoreIds.RemoveAt(i);
                continue;
            }

            m_pendingConsumableRestoreIds.RemoveAt(i);
            DeliverThenConfirm(item, fromRestore: true);
            return true;
        }

        return false;
    }

    //获得本地价格
    public string GetLocalPrice(string storeProdId, bool showCur)
    {
        var retStr = BillingSdk.GetLocalPrice(storeProdId, showCur);
        if (retStr.Length > 0)
        {
            return retStr;
        }

        return "??";
    }
}
