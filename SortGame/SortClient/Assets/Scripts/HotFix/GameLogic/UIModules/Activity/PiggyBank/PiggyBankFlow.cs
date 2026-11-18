using System;
using FishFramework;

public static class PiggyBankFlow
{
    static Action settlementContinue;

    public static bool TryShowClaimBeforeButtons(Action onContinue)
    {
        if (!PiggyBankState.PendingClaim)
        {
            onContinue?.Invoke();
            return false;
        }

        settlementContinue = onContinue;
        if (GameModule.UI.ExistPanel<UIPiggyBankMain>())
            GameModule.UI.DestroyPanel<UIPiggyBankMain>();
        GameModule.UI.OpenPanel<UIPiggyBankMain>(PanelLayer.Tips, PanelOpenType.Single);
        return true;
    }

    public static void FinishClaim()
    {
        Action cont = settlementContinue;
        settlementContinue = null;
        cont?.Invoke();
    }

    /// <summary>
    /// 主城最高优先级补领：杀进程未领完时下次登录先弹，关闭后再走其它补领链。
    /// 返回 true 表示已弹出（或已在弹），关闭后会调 onContinue；false 表示无待领，由调用方继续。
    /// </summary>
    public static bool TryShowPendingClaimOnHome(Action onContinue = null)
    {
        if (!PiggyBankState.PendingClaim)
            return false;

        if (GameModule.UI.ExistPanel<UIPiggyBankMain>())
        {
            settlementContinue = onContinue;
            return true;
        }

        settlementContinue = onContinue;
        GameModule.UI.OpenPanel<UIPiggyBankMain>(PanelLayer.Tips, PanelOpenType.Single);
        return true;
    }
}
