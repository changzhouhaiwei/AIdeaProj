using System;
using FishTilePlay;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public class UIShopFreeCoinDlg : ShopFreeCoinDlgBase
{
    private const string PrefWatchCount = "ShopFreeCoinAdWatchCount";
    private const string PrefLastShowUnix = "ShopFreeCoinLastShowUnix";
    private const int FreeAdWatchLimit = 3;
    private const int ShowIntervalSeconds = 120;

    private string shopFreeAdKey = "ShopFreeCoin";
    [SerializeField] private int freeCoinReward = 50;

    public static bool CanShow()
    {
        int watchCount = PlayerPrefs.GetInt(PrefWatchCount, 0);
        if (watchCount < FreeAdWatchLimit)
        {
            return true;
        }

        long lastShowUnix = 0;
        long.TryParse(PlayerPrefs.GetString(PrefLastShowUnix, "0"), out lastShowUnix);
        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
        return now - lastShowUnix >= ShowIntervalSeconds;
    }

    static void MarkShown()
    {
        PlayerPrefs.SetString(PrefLastShowUnix, DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
        PlayerPrefs.Save();
    }

    static void MarkAdWatched()
    {
        int watchCount = PlayerPrefs.GetInt(PrefWatchCount, 0);
        PlayerPrefs.SetInt(PrefWatchCount, watchCount + 1);
        PlayerPrefs.Save();
    }

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == closeBtn)
        {
            CloseSelf();
        }
        else if (button == watchBtn)
        {
#if UNITY_EDITOR
            GameEvent.Send((int)EventNameDef.ShopFreeCoinPanelAnimPlay);
#else
            PlayAd();
#endif
        }
    }

    protected override bool OnEscButtonPressed()
    {
        if (mCanBack)
        {
            CloseSelf();
        }
        return true;
    }


    void PlayAd()
    {
        GameBAData.ExcuteAdClick(shopFreeAdKey);

        if (!AdsPlayStrategy.CheckRewardAdsPlay(shopFreeAdKey))
        {
            return;
        }
#if UNITY_EDITOR

#else
            // 广告加载成功，开始播放广告
            TileGameCore.UIController.ShowPage<UIAdsLoading>();
            GPSDKMgr.Instance.PlayRewardedAd(shopFreeAdKey, 5);
#endif


    }

    bool mCanBack = false;
    protected override void OnOpen()
    {
        base.OnOpen();
        MarkShown();
        GameBAData.ExcuteAdImpress(shopFreeAdKey);
        RaycastController.Disable();
        AddEvent();

        Utility.DGDelayedCall(1.0f, () =>
        {
            mCanBack = true;
        });
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        RaycastController.Enable();
        RemoveEvent();
    }

    void AddEvent()
    {
        GameEvent.AddEventListener((int)EventNameDef.ShopFreeCoinPanelAnimPlay, AddCoinAnim);
    }

    void RemoveEvent()
    {
        GameEvent.RemoveEventListener((int)EventNameDef.ShopFreeCoinPanelAnimPlay, AddCoinAnim);
    }

    void AddCoinAnim()
    {
        Utility.DGDelayedCall(0.7f, () =>
        {
            TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.coinAdd);
        });

        MarkAdWatched();
        GPUserDataManager.Instance.DelayAddCoinBatched(freeCoinReward, 1.0f, 0.2f);
        CloseSelf();
    }

}