using System;
using System.Collections.Generic;
using SuperScrollView;
using UnityEngine;
using FishFramework;

public class AccountUI : AccountUIBase, IUIOpen<Action<string>>
{
    private int totalCount;
    private List<string> accountIds = new List<string>();
    private Action<string> callback;

    public void OnOpen(Action<string> p1)
    {
        callback = p1;
        accountIds = AccountData.GetAccountIds();
        totalCount = accountIds.Count;
        loopListView.InitListView(totalCount, OnGetItemByIndex);
    }

    private LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index)
    {
        if (index < 0 || index >= totalCount)
        {
            return null;
        }

        LoopListViewItem2 item = listView.NewListViewItem("AccountCell");
        if (item.IsInitHandlerCalled == false)
        {
            item.IsInitHandlerCalled = true;
            item.WidgetId = item.ItemId.ToString();
            CreateWidget<AccountCell>(item.WidgetId, item.GetComponent<UIWidgetBehaviour>()).Init((str) =>
            {
                callback(str);
                CloseSelf();
            });
        }

        GetWidget<AccountCell>(item.WidgetId).SetItemData(accountIds[index]);
        return item;
    }
}