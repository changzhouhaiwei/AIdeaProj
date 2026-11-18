
using TileGameCore;
using Unity.VisualScripting;
using FishFramework;

public class UnlockData : BaseData
{
    private readonly int completeAdExtraAwardUnlockLv = 9; //结算额外激励：第八关结束开启
    private readonly int goldTreeUnlockLv = 14; //黄金树：通关第13关后（GetLevelNum+1 >= 14）
    private readonly int circleDailyAwardUnlockLv = 14; //转盘活动
    private readonly int freeShopCoinUnlockLv = 3; //免费金币
    private readonly int dailyVictoryUnlockLv = 4; // 火花连胜：通关第3关后
    private readonly int bannerAdUnlockLv = 5;//banner开启
    private readonly int interAdUnlockLv = 8;//插屏广告
    private readonly int rewardedLoadUnLockLv = 3; //激励广告开始加载
    private readonly int bannerLoadeUnlockLv = 4; //未使用 banner开始加载（展示见 bannerAdUnlockLv）

    private readonly int interAdLoadLv = 7; //未使用；插屏加载/开启见 InterAdV5Config

    public bool GetBannerAdBUnlock(int levelIdx = -1)
    {
        int compLevelIdx = levelIdx;
        if (levelIdx == -1)
        {
            compLevelIdx = GPUserDataManager.Instance.GetLevelNum();//从0开始
        }

        return (compLevelIdx + 1) >= bannerAdUnlockLv;
    }

    public bool GetCompleteAdExtraAwardUnlock(int levelIdx = -1)
    {
        int compLevelIdx = levelIdx;
        if (levelIdx == -1)
        {
            compLevelIdx = GPUserDataManager.Instance.GetLevelNum();
        }
        return (compLevelIdx + 1) >= completeAdExtraAwardUnlockLv;
    }

    public bool GetGoldTreeUnlock(int levelIdx = -1)
    {
        int compLevelIdx = levelIdx;
        if (levelIdx == -1)
        {
            compLevelIdx = GPUserDataManager.Instance.GetLevelNum();
        }
        return (compLevelIdx + 1) >= goldTreeUnlockLv;
    }

    public bool GetCircleDailyAwardUnlock(int levelIdx = -1)
    {
        int compLevelIdx = levelIdx;
        if (levelIdx == -1)
        {
            compLevelIdx = GPUserDataManager.Instance.GetLevelNum();
        }
        return (compLevelIdx + 1) >= circleDailyAwardUnlockLv;
    }

    public bool GetFreeShopCoinUnlock(int levelIdx = -1)
    {
        int compLevelIdx = levelIdx;
        if (levelIdx == -1)
        {
            compLevelIdx = GPUserDataManager.Instance.GetLevelNum();
        }
        return (compLevelIdx + 1) >= freeShopCoinUnlockLv;
    }

    public bool GetDailyVictoryUnlock(int levelIdx = -1)
    {
        int compLevelIdx = levelIdx;
        if (levelIdx == -1)
        {
            compLevelIdx = GPUserDataManager.Instance.GetLevelNum();
        }
        return (compLevelIdx + 1) >= dailyVictoryUnlockLv;
    }

    public bool GetInterAdUnlock(int levelIdx = -1)
    {
        int compLevelIdx = levelIdx;
        if (levelIdx == -1)
        {
            compLevelIdx = GPUserDataManager.Instance.GetLevelNum();
        }
        return (compLevelIdx + 1) >= interAdUnlockLv;
    }

    public bool GetRewardedAdUnlock(int levelIdx = -1)
    {
        int compLevelIdx = levelIdx;
        if (levelIdx == -1)
        {
            compLevelIdx = GPUserDataManager.Instance.GetLevelNum();
        }
        return (compLevelIdx + 1) >= rewardedLoadUnLockLv;
    }

    public override void OnInit()
    {

    }

    public override void OnRemove()
    {

    }

    public override void OnReset()
    {

    }




}