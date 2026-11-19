using System.Collections;
using UnityEngine;
using FishFramework;
using FishXSortPlay;
using DG.Tweening;
public class GameApp : Singleton<GameApp>
{
    public static void Entrance()
    {
        MainBehaviour.Instance.AddOnApplicationFocusListener(Instance.OnApplicationFocus);
        Instance.StartGameLogic();
    }

    /// <summary>
    /// 待机插屏只认「真切后台」。播广告期间的焦点变化由 IsAdPlaying 门禁忽略，
    /// 避免激励/插屏关掉后被当成回前台再弹一次插屏。
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!GameData.BFromStartScene)
        {
            return;
        }

        var sdk = GPSDKMgr.Instance;
        if (sdk != null && sdk.IsAdPlaying)
        {
            // 待机插屏仍忽略这次焦点；但必须先解冻，否则 SDK 回调前 timeScale=0，复活 DelayCall 永不触发。
            if (hasFocus)
            {
                if (GameModule.Setting != null)
                {
                    GameModule.Setting.ResumeGame();
                }

                sdk.MarkIgnoreClickAfterAd();
            }

            return;
        }

        if (hasFocus && sdk != null && sdk.HasPendingRewardGrant)
        {
            sdk.MarkIgnoreClickAfterAd();
        }

        if (hasFocus)
        {
            if (!AdsPlayStrategy.HasSavedBackgroundTime)
            {
                return;
            }

            Utility.DGDelayedCall(0.5f, () =>
            {
                if (GPSDKMgr.Instance != null && GPSDKMgr.Instance.IsAdPlaying)
                {
                    return;
                }

                AdsPlayStrategy.EnterForeground();
            });
        }
        else
        {
            AdsPlayStrategy.SaveBackgroundTime();
        }
    }

    /// <summary>
    /// 开始游戏业务层逻辑。
    /// <remarks>显示UI、加载场景等。</remarks>
    /// </summary>
    private void StartGameLogic()
    {
        MainBehaviour.Instance.StartCoroutine(CorStartLogic());
    }


    IEnumerator CorStartLogic()
    {
        yield return null;

        // GameModule.Initialize 已在 Main 中完成（xasset 加载 LoginScene 的前置依赖）
        LocalizationFontSwitcher.Initialize();
        I2LocalizeMgr.SetLanguage(GameModule.Setting.GetCurrentLanguage());
        yield return null;

        // BaseSdk 需早于加载埋点。Android 数数已在 CustomApplication 初始化，此处只冲刷队列；iOS 仍在此 Init。
        GPSDKMgr.Instance.InitBaseSdk();
        yield return null;
        GPSDKMgr.Instance.InitThinkingDataSDK();
        yield return null;

        GameModule.DataAnylasis.ExcuteLoadingEvent10008();
        yield return null;
        GameModule.DataAnylasis.ExcuteLoadingEvent10009();
        yield return null;

        I2LocalizeMgr.Instance.UpdateSources();
        yield return null;
        GameModule.DataAnylasis.ExcuteLoadingEvent10014();
        yield return null;
        try
        {
            ConfigHelper.Initialize();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        yield return null;
        GameModule.DataAnylasis.ExcuteLoadingEvent10015();
        yield return null;
        GameModule.DataAnylasis.ExcuteLoadingEvent10016();
        yield return null;
        //加载全局资源,此段成疑,暂无配置全局资源
        GameData.RegisterData();
        GameData.BFromStartScene = true;
        yield return null;

        // Log
        if (Debug.unityLogger.logEnabled == false)
        {
            GameObject go = GameObject.Find("Reporter");
            if (go)
            {
                go.SetActive(false);
            }
        }

        InitCommonUI();//通用UI节点添加
        yield return null;
        GameModule.DataAnylasis.ExcuteLoadingEvent10017();
        yield return null;
        InitLoginUI();
        yield return null;
    }

    private void InitLoginUI()
    {
        GameModule.UI.OpenPanel<LoginUI>();
    }

    private static void InitGMView()
    {
        const PanelLayer panelLayer = PanelLayer.GM;
        GMDlg gmDlg = new GMDlg
        {
            Layer = panelLayer
        };
        gmDlg.Create(nameof(gmDlg), GameModule.UI.GetLayerRect(panelLayer));
        int sortingOrder = GameModule.UI.GetIncrementedSortingOrder(panelLayer);
        gmDlg.SetSortingOrder(sortingOrder);
        gmDlg.Open();
    }

    private static void InitCommonUI()
    {
        //转菊花 
        GameObject loadingObj = Object.Instantiate(UILoadHelper.LoadAsset<GameObject>("Assets/CABRes/Prefabs/UICommon/LoadingCircleUI.prefab"));
        var rect = loadingObj.GetComponent<RectTransform>();
        rect.SetParent(GameModule.UI.GetLayerRect(PanelLayer.Guide));
        rect.ResetToFullScreen();
        GameModule.UI.SetLoadingObj(loadingObj);

    }

    public void ReLogin()
    {
        GameData.SnapshotGmServerTimeForRelogin();
        Timer.CancelAllRegisteredTimers();
        RedDotMgr.Instance.Dispose();
        GameModule.UI.DestroyAllPanel();
        DOTween.KillAll();
        GameData.Clear();
        GameData.ApplyGmServerTimeSnapshotAfterClear();
        xasset.Scene.LoadAsync($"Assets/CABRes/Scenes/LoginScene.unity");
    }
}