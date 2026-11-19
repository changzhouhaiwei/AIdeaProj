using System.Linq;
using Core.Tools.MethodExtension;
using FishFramework;
using UnityEngine.UI;

/// <summary>
/// 免广告购买/恢复成功反馈：按商店配置发放奖励（金币等）。
/// </summary>
class UIPackNoAdGetDlg : UIPackNoAdGetDlgBase
{
    protected override void OnOpen()
    {
        base.OnOpen();
        XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.rewardGet);

        GrantNoAdShopAwards();
    }

    static void GrantNoAdShopAwards()
    {
        var cfg = GameData.Shop.GetCfgBuyPayID(GameData.Pay.GetNoAdsPayId());
        if (cfg?.Award == null || cfg.Award.Count == 0)
            return;

        foreach (var awardStr in cfg.Award)
        {
            var parts = awardStr.Split(",").ToList();
            if (parts.Count != 2)
                continue;
            int id = parts[0].ToInt();
            int num = parts[1].ToInt();
            GPUserDataManager.Instance.AddCommonItemData(id, num);
        }
    }

    protected override void OnClicked(Button button)
    {
        CloseSelf();
    }

    protected override bool OnEscButtonPressed()
    {
        CloseSelf();
        return true;
    }
}