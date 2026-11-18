using System.Collections.Generic;
using FishFramework;
using UnityEngine;

public class UIShopAssetNodeWidget : ShopAssetNodeBase
{
    List<UIShopAssetCellWidget> mWidgetList = new();

    protected override void OnOpen()
    {
        base.OnOpen();
        InitAssetCells();
    }

    protected override void OnDestroyed()
    {

    }

    void InitAssetCells()
    {
        BindCoinsShow();

        // 道具栏只动态创建非金币资产；金币用 SafeArea/coinsshow 预制节点
        // 从左到右：撤销 / 提示 / 洗牌 / 高亮（shop_dj_3 / 1 / 2 / 5）
        List<ShopData.ShopAssetData> curList = new()
        {
            new ShopData.ShopAssetData { id = 3, cnt = 1 },
            new ShopData.ShopAssetData { id = 1, cnt = 1 },
            new ShopData.ShopAssetData { id = 2, cnt = 1 },
            new ShopData.ShopAssetData { id = 5, cnt = 1 },
        };

        for (int i = 0; i < curList.Count; i++)
        {
            UIShopAssetCellWidget widget;
            if (i == 0)
            {
                // 复用预制体 cell1，避免多出一个未刷图标的占位格
                widget = CreateWidget<UIShopAssetCellWidget>("assetNode" + i, cell1.GetComponent<UIWidgetBehaviour>());
            }
            else
            {
                widget = CreateWidget<UIShopAssetCellWidget>("assetNode" + i, gameObject.transform);
            }

            widget.UpdateData(curList[i]);
            mWidgetList.Add(widget);
        }
    }

    void BindCoinsShow()
    {
        var parent = gameObject.transform.parent;
        if (parent == null)
            return;

        var coinsShow = parent.Find("coinsshow");
        if (coinsShow == null)
            return;

        var behaviour = coinsShow.GetComponent<UIWidgetBehaviour>();
        if (behaviour == null)
            return;

        var widget = CreateWidget<UIShopAssetCellWidget>("coinsshow", behaviour);
        widget.UpdateData(new ShopData.ShopAssetData { id = 4, cnt = 1 });
        mWidgetList.Add(widget);
    }

    public void UpdateAll()
    {
        foreach (var item in mWidgetList)
        {
            item.UpdateCount();
        }
    }
}
