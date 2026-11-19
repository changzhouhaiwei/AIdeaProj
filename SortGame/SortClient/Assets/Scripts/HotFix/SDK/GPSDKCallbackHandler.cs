using System;
using UnityEngine;
using FishFramework;
using XSortGameCore;
using FishXSortPlay;

/// <summary>
/// SDK回调处理器，负责处理所有来自Java的SDK事件回调
/// </summary>
public class GPSDKCallbackHandler
{
    private string saveKey1 = "firstGiftBag";
    
    // 隐藏加载界面事件
    public event Action OnHideAdsLoading;
    
    // 礼包弹窗事件，UI层可以监听此事件
    public event Action OnShowGiftBagDlg;

    public void OnRecieveJavaData(string key, string contentJson, string uid)
    {
        try
        {
            HandleSDKCallback(key, contentJson, uid);
        }
        catch (Exception e)
        {
            Debug.LogError($"GPSDKCallbackHandler Exception when processing callback for key = {key}, error: {e.Message}\n{e.StackTrace}");
        }
    }

    private void HandleSDKCallback(string key, string contentJson, string uid)
    {
        GPUserDataManager.Instance.SetUID(uid);

        // 触发隐藏加载页面事件
        OnHideAdsLoading?.Invoke();

        //这里主要监听广告的
        switch (key)
        {
            case "AdCollapsed":
            case "AdsPlayFailed":
                // 激励失败也走 AdsPlayFailed，不能在这里放行进关 LoadLevel。
                // 进关插屏结束/超时由 GPSDKMgr 在插屏路径上 NotifyEnterLevelInterstitialFinished。
                AdsPlayStrategy.ClearBackgroundTime();
                var gameOver = UIController.GetPage<UIGameOver>();
                if (gameOver != null && gameOver.IsPageDisplayed)
                    gameOver.UnlockReviveAfterAdFail();
                break;
            case "RewardAdsPlaySuc"://激励广告播放成功
                AdsPlayStrategy.ClearBackgroundTime();
                GameBAData.CheckExcuteKSAdShow();
                GameBAData.AddRewardAdInOneLevel();
                CheckRewardAdSuc();
                break;
            case "AdsInterstitialSuc"://插屏广告结束
                AdsPlayStrategy.ClearBackgroundTime();
                GameBAData.CheckExcuteKSAdShow();
                CheckShowGiftBagDlg();
                break;
            case "SensitiveSuc":
                GameEvent.Send<string>((int)EventNameDef.SensitiveCheckSuc, contentJson);
                break;
            case "SensitiveException":
                GameEvent.Send<string>((int)EventNameDef.SensitiveCheckException, contentJson);
                break;
            case "SensitiveError":
                GameEvent.Send<string>((int)EventNameDef.SensitiveCheckError, contentJson);
                break;
            default:
                break;
        }
    }

    //视频播放成功后
    private void CheckRewardAdSuc()
    {
        int rewardType = GPSDKMgr.Instance.GetRewardAdType();
        if (rewardType == 1)//复活成功
        {
            GameBAData.ExcuteStageEvent(4); // 广告复活
            GameBAData.ExcuteMainStage(6);
            GameDirector.Revive();
            UIController.HidePage<UIGameOver>();
            UIController.ShowPage<UIGame>();
        }
        else if (rewardType == 2)//增加道具
        {
            GPUserDataManager.Instance.AddItemByAd(); //改成直接释放
            GameBAData.ExcuteStageEvent(8);
            GameEvent.Send<int>((int)EventNameDef.PUUIAdGetItem, GPUserDataManager.Instance.GetAdAddItemId());

            // var useId = GPUserDataManager.Instance.getwi
            // 保存时间戳，确保一天只能获得一次奖励
            PlayerPrefs.SetFloat("AdAddItem", Time.time);
            // PlayerPrefs.Save();
        }
        else if (rewardType == 3) //结算后的额外金币
        {
            GameEvent.Send((int)EventNameDef.UICompleteExtraAward);
        }
        else if (rewardType == 4) //直接释放技能
        {
            GameBAData.ExcuteStageEvent(8);
            PUDirector.UsePowerUp(PUType.Hint);
            GameBAData.ExcuteStageEvent(2, 1);
        }
        else if (rewardType == 5) //商店不够买后的免费金币
        {
            GameEvent.Send((int)EventNameDef.ShopFreeCoinPanelAnimPlay);
        }
        else if (rewardType == 6)// 转盘看广告 
        {
            GameEvent.Send((int)EventNameDef.ADCircleDailyGetAward);
        }
        else if (rewardType == 8)
        {
            GameEvent.Send((int)EventNameDef.AwardColorEggCoverPickAll);
        }
    }

    //判定是否显示礼包
    private void CheckShowGiftBagDlg()
    {
        int levelIndex = GPUserDataManager.Instance.GetLevelNum();
        if (levelIndex >= 10 && PlayerPrefs.GetInt(saveKey1, 0) == 0)
        {
            GameModule.UI.OpenPanel<UINoAdPanel>();

            // int bagId = GameData.Pay.
            // var oneCfg = ConfigHelper.Tables.TbPayProductCfg.Get(1001);
            // GameBAData.ExcuteGameTriggerGift(oneCfg.Id.ToString(), oneCfg.ProductName, oneCfg.ProductPrice, true);

            PlayerPrefs.SetInt(saveKey1, 1);
            PlayerPrefs.Save();

            OnShowGiftBagDlg?.Invoke();
        }
    }
}
