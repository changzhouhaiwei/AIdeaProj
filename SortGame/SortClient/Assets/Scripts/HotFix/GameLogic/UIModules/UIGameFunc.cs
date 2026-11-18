

using System;
using FishTilePlay;
using TileGameCore;
using UnityEngine;
using FishFramework;

public static class UIGameFunc
{
    public static CircleDailyAwardUI OpenCircleAwardActivity()
    {
        if (GameData.UnlockData == null || !GameData.UnlockData.GetCircleDailyAwardUnlock())
            return null;
        if (!UIController.IsDisplayed<UIMainMenu>() || GameDirector.IsGameActive)
            return null;
        if (GPSDKMgr.Instance != null && GPSDKMgr.Instance.IsAdPlaying)
            return null;
        return GameModule.UI.OpenPanel<CircleDailyAwardUI>();
    }

    public static void CloseCircleAwardActivity()
    {
        if (GameModule.UI == null)
            return;
        if (GameModule.UI.ExistPanel<CircleDailyAwardRuleUI>())
            GameModule.UI.ClosePanel<CircleDailyAwardRuleUI>();
        if (GameModule.UI.ExistPanel<CircleDailyAwardUI>())
            GameModule.UI.ClosePanel<CircleDailyAwardUI>();
    }

    public static void OpenDailyWinRewardOnFirstVictory(Action onContinue)
    {
        if (!GameData.BFromStartScene || GameData.Activity == null || onContinue == null)
        {
            return;
        }

        if (!GameData.Activity.IsDailyVictoryActEnabled())
        {
            onContinue.Invoke();
            return;
        }

        var openParam = new DailyWinRewardDlgOpenParam
        {
            FromSettlement = true,
            OnContinue = onContinue,
        };
        GameModule.UI.OpenPanel<UIDailyWinRewardDlg, DailyWinRewardDlgOpenParam>(
            PanelLayer.Tips, PanelOpenType.Single, openParam);
    }

    /// <summary>
    /// 主城进入时补发未领取的火花连胜奖励：第 1–6 天静默发放，第 7 天弹 UIDailyRewardGet；有弹窗时返回 true，关闭后执行 onContinue。
    /// </summary>
    public static bool TryShowPendingDailyVictoryRewardGetDlg(Action onContinue = null)
    {
        if (!GameData.BFromStartScene || GameData.Activity == null)
        {
            onContinue?.Invoke();
            return false;
        }

        if (!GameData.Activity.IsDailyVictoryActEnabled())
        {
            onContinue?.Invoke();
            return false;
        }

        if (!GameData.Activity.HasClaimableStreakReward())
        {
            onContinue?.Invoke();
            return false;
        }

        GPUserDataManager.Instance.SetPendingDailyVictorySettlementDlg(false);

        int streakDay = GameData.Activity.GetDailyVictoryStreakDay();
        // 1–6 天：静默发道具，不发头像框、不弹 UserFrameNode 领奖窗
        if (streakDay < 7 || !GameData.Activity.CanClaimDailyVictoryAvatarFrame())
        {
            GameData.Activity.ClaimCurrentStreakReward();
            GPUserDataManager.Instance.SaveData();
            onContinue?.Invoke();
            return false;
        }

        if (GameModule.UI.ExistPanel<UIDailyRewardGet>())
        {
            return true;
        }

        GPUserDataManager.Instance.SaveData();

        var panel = GameModule.UI.OpenPanel<UIDailyRewardGet>(PanelLayer.Tips, PanelOpenType.Single);
        panel.SetCloseAction(() =>
        {
            GPUserDataManager.Instance.SaveData();
            onContinue?.Invoke();
        });
        panel.Open();
        return true;
    }

    public static UIRankView OpenRankView()
    {
        GameData.Rank?.TryApplyRobotIdleGrowth();
        UIRankView rankview = UIRankView.Instance;
        if (rankview == null || !GameModule.UI.ExistPanel<UIRankView>())
        {
            UIRankView.Instance = null;
            rankview = GameModule.UI.OpenPanel<UIRankView>();
        }

        rankview.ShowMe();
        return rankview;
    }

    public static UIRankChangeDlg OpenRankChangeDlg()
    {
        UIRankChangeDlg rankChangeDlg = UIRankChangeDlg.Instance;
        if (rankChangeDlg == null || !GameModule.UI.ExistPanel<UIRankChangeDlg>())
        {
            UIRankChangeDlg.Instance = null;
            rankChangeDlg = GameModule.UI.OpenPanel<UIRankChangeDlg>();
        }
        else
        {
            rankChangeDlg.ShowMe();
        }
        return rankChangeDlg;
    }

    public static UIGoldTreeHelp OpenGoldTreeHelp()
    {
        if (GameModule.UI.ExistPanel<UIGoldTreeHelp>())
        {
            var panel = GameModule.UI.GetPanel<UIGoldTreeHelp>();
            panel.Open();
            return panel;
        }

        return GameModule.UI.OpenPanel<UIGoldTreeHelp>();
    }

    public static void OpenGoldTreeStart(Action clickAction = null)
    {
        var node = GameModule.UI.OpenPanel<UIGoldTreeStart>();
        node.ClickAction = clickAction;
    }

    public static void OpenFeedbackDlg(Action onClosed = null)
    {
        var dlg = GameModule.UI.OpenPanel<UIFeedbackDlg>();
        if (dlg != null && onClosed != null)
            dlg.closeExtraAct = onClosed;
        else
            onClosed?.Invoke();
    }

    public static void CheckShowRateDlg(Action onClosedOrNoDialog = null)
    {
#if UNITY_IOS
        // iOS 暂不弹评价；但结算页靠此回调解锁「下一关」，必须继续往下走。
        onClosedOrNoDialog?.Invoke();
        return;
#endif
        MigrateLegacyLaterRateGame();

        int state = PlayerPrefs.GetInt(UIRatesDlg.RateUsStateKey, 0);
        if (state == UIRatesDlg.RateUsStateCompleted)
        {
            onClosedOrNoDialog?.Invoke();
            return;
        }

        int levelIndex = GPUserDataManager.Instance.GetLevelNum();

        if (levelIndex == 8 && state == UIRatesDlg.RateUsStateInitial)
        {
            var dlg = GameModule.UI.OpenPanel<UIRatesDlg>();
            dlg.SetCloseAction(onClosedOrNoDialog);
            return;
        }

        if (levelIndex == 50 && state == UIRatesDlg.RateUsStateLater)
        {
            var dlg = GameModule.UI.OpenPanel<UIRatesDlg>();
            dlg.SetCloseAction(onClosedOrNoDialog);
            return;
        }

        onClosedOrNoDialog?.Invoke();
    }

    const string LegacyLaterRateGameKey = "laterRateGame";

    static void MigrateLegacyLaterRateGame()
    {
        if (PlayerPrefs.GetInt(LegacyLaterRateGameKey, 0) != 1)
            return;

        int state = PlayerPrefs.GetInt(UIRatesDlg.RateUsStateKey, 0);
        if (state != UIRatesDlg.RateUsStateCompleted)
            PlayerPrefs.SetInt(UIRatesDlg.RateUsStateKey, UIRatesDlg.RateUsStateLater);

        PlayerPrefs.DeleteKey(LegacyLaterRateGameKey);
        PlayerPrefs.Save();
    }

    static string privacyKey = "privacyKey4";
    public static void CheckShowPrivacyDlg(Action action)
    {
        if (GPUserDataManager.Instance.GetLevelNum() < 5 && PlayerPrefs.GetInt(privacyKey, 0) == 0)
        {
            var dlg = GameModule.UI.OpenPanel<UIPrivacyDlg>();
            LoadingZzSpinePlayback.SetVisible(false);
            dlg.afterClickAction = () =>
            {
                PlayerPrefs.SetInt(privacyKey, 1);
                LoadingZzSpinePlayback.SetVisible(true);
                action?.Invoke();
            };
        }
        else
        {
            action?.Invoke();
        }
    }


    //购买获得奖励 
    public static void ShowCommonBuySucDlg(string payId)
    {
        var cfg = GameData.Shop.GetCfgBuyPayID(payId);
        if (cfg == null)
        {
            Debug.LogError($"ShowCommonBuySucDlg: can not find shop cfg for payId/storeProdId = {payId}");
            return;
        }
        var node = GameModule.UI.OpenPanel<UIShopGetDlg>();
        node.ShowAwards(cfg);
    }

    public static void ShowNoAdsBuySucDlg()
    {
        GPUserDataManager.Instance.EnableNoAd();
        GameModule.UI.OpenPanel<UIPackNoAdGetDlg>();
    }

    /// <summary>
    /// 免广告 IAP 成功：<see cref="UIPackNoAdGetDlg"/> 内发放金币等；延后派发 <see cref="EventNameDef.BuyNoAdPackSuccess"/>。
    /// </summary>
    public static void ShowNoAdsPurchaseSuccessFlow()
    {
        ShowNoAdsBuySucDlg();
        Utility.DGDelayedCall(0.12f, () =>
            GameEvent.Send((int)EventNameDef.BuyNoAdPackSuccess));
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下模拟真机 IAP 成功（含与普通内购一致的事件派发，便于商店等界面刷新）。
    /// </summary>
    public static void SimulateEditorIapSuccess(string payId)
    {
        if (payId == GameData.Pay.GetNoAdsPayId())
        {
            ShowNoAdsPurchaseSuccessFlow();
        }
        else
        {
            ShowCommonBuySucDlg(payId);
            GameEvent.Send<string>((int)EventNameDef.BuyProductSuccess, payId);
        }
    }
#endif

}
