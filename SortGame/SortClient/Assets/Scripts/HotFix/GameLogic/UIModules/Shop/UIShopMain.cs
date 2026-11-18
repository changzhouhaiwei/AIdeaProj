using UnityEngine;

using UnityEngine.UI;
using SuperScrollView;
using FishFramework;
using System.Collections.Generic;
using System;

public class UIShopMain : UIShopMainBase, IUIOpen<bool>
{
    private const float DefaultListBottom = 150f;

    private int totalCount = 10;
    private bool mBSpread = false;
    private bool hasBuy = false;

    private bool mCloseShowBanner = false;
    private bool mInGame = false;

    List<cfg.Game.ShopCfg> mDataList;

    UIShopAssetNodeWidget mAssetWidget;

    bool mCanBack = false;

    protected override void OnOpen()
    {
        OnOpenCore(false);
    }

    public void OnOpen(bool inGame)
    {
        OnOpenCore(inGame);
    }

    void OnOpenCore(bool inGame)
    {
        mInGame = inGame;
        mCloseShowBanner = inGame;
        base.OnOpen();
        ApplyOpenModeVisuals(inGame);
        InitAssetNode();
        InitLoopListView();
        AddEvent();

        if (inGame)
        {
            UIBottomNav.Hide();
        }
        else
        {
            UIBottomNav.Show(BottomNavTab.Shop);
        }

        if (!GameData.Pay.QuerySuccess)
        {
            GPSDKMgr.Instance.QueryCurrencyInfo();
        }
#if UNITY_IOS
        GPSDKMgr.Instance.QueryCompleteOrders();
        while (GPSDKMgr.Instance.CheckConsumableRestoreOrders())
        {
        }
#else
        GPSDKMgr.Instance.CheckShowRestoreItem();
#endif

        Utility.DGDelayedCall(1.0f, () =>
        {
            mCanBack = true;
        });
    }

    void ApplyOpenModeVisuals(bool inGame)
    {
        if (backButton != null)
        {
            backButton.gameObject.SetActive(inGame);
        }

        if (loopListView == null)
        {
            return;
        }

        var rt = loopListView.transform as RectTransform;
        if (rt == null)
        {
            return;
        }

        var offsetMin = rt.offsetMin;
        offsetMin.y = inGame ? 0f : DefaultListBottom;
        rt.offsetMin = offsetMin;
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        RemoveEvent();
    }


    void InitAssetNode()
    {
        mAssetWidget = CreateWidget<UIShopAssetNodeWidget>(safeArea.transform, assetNode_UIWidgetBehaviour);
    }

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == backButton)
        {
            CloseFromBottomNav();
        }
    }


    protected override bool OnEscButtonPressed()
    {
        if (mCanBack)
        {
            CloseFromBottomNav();
        }
        return true;
    }

    public void CloseFromBottomNav()
    {
        CloseSelf();
        CheckShowAdCoin();
        if (!mInGame)
        {
            UIBottomNav.SetTab(BottomNavTab.Home);
        }
    }


    void CheckShowAdCoin()
    {
        if (!hasBuy)
        {
            if (GameData.UnlockData.GetFreeShopCoinUnlock() && UIShopFreeCoinDlg.CanShow())
            {
                // Tips 层在 Panel 之上，确保盖住 UIMapMain / 底栏
                var dlg = GameModule.UI.OpenPanel<UIShopFreeCoinDlg>(PanelLayer.Tips, PanelOpenType.Single);
                dlg.Open();

                if (mCloseShowBanner)
                {
                    GPSDKMgr.Instance.ShowADBanner();
                }

            }
        }

    }

    void InitLoopListView()
    {
        mBSpread = false;
        mDataList = GameData.Shop.GetShopCfgList(bSpread: mBSpread);
        totalCount = mDataList.Count + 1;
        loopListView.InitListView(totalCount, OnGetItemByIndex);
    }

    void ReloadListView()
    {
        mBSpread = true;
        mDataList = GameData.Shop.GetShopCfgList(bSpread: mBSpread);
        totalCount = mDataList.Count;
        loopListView.SetListItemCount(totalCount);
        loopListView.RefreshAllShownItem();
    }

    void RefreshUI()//重刷UI
    {
        mBSpread = true;
        mAssetWidget.UpdateAll();

        mDataList = GameData.Shop.GetShopCfgList(bSpread: mBSpread);
        totalCount = mDataList.Count;
        loopListView.SetListItemCount(totalCount, false);
        loopListView.RefreshAllShownItem();
#if UNITY_IOS
        while (GPSDKMgr.Instance.CheckConsumableRestoreOrders())
        {
        }
#else
        GPSDKMgr.Instance.CheckShowRestoreItem();
#endif
    }

    private LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
    {
        if (index < 0 || index >= totalCount)
        {
            return null;
        }

        cfg.Game.ShopCfg oneData = null;
        LoopListViewItem2 item = null;
        int cellType = 0;

        bool bShowSpreadBtn = (mBSpread == false && index == totalCount - 1);
        if (bShowSpreadBtn)//展示展开按钮
        {
            item = listView.NewListViewItem("ShopCellSpread");
        }
        else
        {
            oneData = mDataList[index];
            cellType = oneData.ShopCellType;
            item = cellType switch
            {
                1 => listView.NewListViewItem("ShopCellBig"),
                2 => listView.NewListViewItem("ShopCellNoAds"),
                3 => listView.NewListViewItem("ShopCellGold"),
                _ => listView.NewListViewItem("ShopCellGold")
            };

        }

        if (item.IsInitHandlerCalled == false)
        {
            item.IsInitHandlerCalled = true;
            item.WidgetId = item.ItemId.ToString();

            if (bShowSpreadBtn)
            {
                CreateWidget<UIShopCellSpreadWidget>(item.WidgetId, item.GetComponent<UIWidgetBehaviour>());
            }
            else
            {
                object dd1 = cellType switch
                {
                    1 => CreateWidget<UIShopCellBigWidget>(item.WidgetId, item.GetComponent<UIWidgetBehaviour>()),
                    2 => CreateWidget<UIShopCellNoAdsWidget>(item.WidgetId, item.GetComponent<UIWidgetBehaviour>()),
                    3 => CreateWidget<UIShopCellGoldWidget>(item.WidgetId, item.GetComponent<UIWidgetBehaviour>()),
                    _ => CreateWidget<UIShopCellGoldWidget>(item.WidgetId, item.GetComponent<UIWidgetBehaviour>())
                };
            }
        }

        if (bShowSpreadBtn)
        {
            GetWidget<UIShopCellSpreadWidget>(item.WidgetId).SetClickAction(() =>
            {
                ReloadListView();
            });
        }
        else
        {
            object dd = cellType switch
            {
                1 => GetWidget<UIShopCellBigWidget>(item.WidgetId).UpdateData(oneData),
                2 => GetWidget<UIShopCellNoAdsWidget>(item.WidgetId).UpdateData(oneData),
                3 => GetWidget<UIShopCellGoldWidget>(item.WidgetId).UpdateData(oneData),
                _ => GetWidget<UIShopCellGoldWidget>(item.WidgetId).UpdateData(oneData)
            };

        }
        return item;
    }


    protected void AddEvent()
    {
        GameEvent.AddEventListener((int)EventNameDef.BuyNoAdPackSuccess, CbBuyNoAdSuc);
        GameEvent.AddEventListener<string>((int)EventNameDef.BuyProductSuccess, CbBuyProductSuc);
        GameEvent.AddEventListener((int)EventNameDef.ReqQueryShopProductSuc, CbQuerySuc);
    }

    protected void RemoveEvent()
    {
        GameEvent.RemoveEventListener((int)EventNameDef.BuyNoAdPackSuccess, CbBuyNoAdSuc);
        GameEvent.RemoveEventListener((int)EventNameDef.ReqQueryShopProductSuc, CbQuerySuc);
        GameEvent.RemoveEventListener<string>((int)EventNameDef.BuyProductSuccess, CbBuyProductSuc);
    }

    void CbBuyNoAdSuc()
    {
        // GameModule.UI.OpenPanel<UIPackNoAdGetDlg>();
        Delay(0.1f, () =>
        {
            RefreshUI();
        });
        hasBuy = true;
    }

    void CbQuerySuc()
    {
        RefreshUI();
    }

    void CbBuyProductSuc(string payId) //购买普通产品完成.
    {
        // var cfg = GameData.Shop.GetCfgBuyPayID(payId);
        // var node = GameModule.UI.OpenPanel<UIShopGetDlg>();
        // node.ShowAwards(cfg);

        Delay(0.1f, () =>
        {
            RefreshUI();
        });
        hasBuy = true;
    }

    public void SetCloseShowBanner(bool b)
    {
        mCloseShowBanner = b;
    }
}