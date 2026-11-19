using UnityEngine.UI;
using FishFramework;

class UINoAdPanel : UIPackNoAdPanelBase
{
    bool mPriceReady;

    protected override void OnClicked(Button button)
    {
        if (button == closeBtn)
        {
            CloseSelf();
        }
        else if (button == buyBtn)
        {
            if (!mPriceReady)
            {
                return;
            }
#if UNITY_EDITOR
            CloseSelf();
            Utility.DGDelayedCall(0.05f, () => UIGameFunc.ShowNoAdsPurchaseSuccessFlow());
            return;
#endif
            CloseSelf();
            Utility.DGDelayedCall(0.06f, () =>
            {
                if (GameData.Pay.QuerySuccess)
                {
                    GPSDKMgr.Instance.TryPay(GameData.Pay.GetNoAdsGId());
                }
                else
                {
                    GPSDKMgr.Instance.QueryCurrencyInfo();
                }
            });
        }
    }

    protected override bool OnEscButtonPressed()
    {
        CloseSelf();
        return true;
    }

    protected override void OnCreating()
    {
        base.OnCreating();
        RefreshUI();
    }

    protected override void OnOpen()
    {
        AddEvent();
        if (GameData.Pay.IsEmpty() && !GameData.Pay.QuerySuccess)
        {
            GPSDKMgr.Instance.QueryCurrencyInfo();//支付信息
        }
        RefreshUI();

        base.OnOpen();

    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        RemoveEvent();
    }

    private void RefreshUI()
    {
        mPriceReady = ShopPriceButton.Apply(priceText, buyBtn, GameData.Pay.GetNoAdsGId(), LoadSprite("ShopIcon", "shopLoading"));
    }

    protected void AddEvent()
    {
        GameEvent.AddEventListener((int)EventNameDef.BuyNoAdPackSuccess, CbBuySuc);
        GameEvent.AddEventListener((int)EventNameDef.ReqQueryShopProductSuc, CbQuerySuc);
    }

    void CbBuySuc()
    {
        CloseSelf();
        // GameModule.UI.OpenPanel<UIPackNoAdGetDlg>();
    }

    void CbQuerySuc()
    {
        RefreshUI();
    }

    protected void RemoveEvent()
    {
        GameEvent.RemoveEventListener((int)EventNameDef.BuyNoAdPackSuccess, CbBuySuc);
        GameEvent.RemoveEventListener((int)EventNameDef.ReqQueryShopProductSuc, CbQuerySuc);
    }

    void BuyProduct()
    {
    }

}
