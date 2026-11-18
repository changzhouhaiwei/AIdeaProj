using System;
using UnityEngine.UI;

public class AccountCell: AccountCellBase
{
    private Action<string> callback;

    public void Init(Action<string> action)
    {
        callback = action;
    } 

    protected override void OnClicked(Button button)
    {
        callback(nameText.text);
    }

    public void SetItemData(string accountId)
    {
        nameText.text = accountId;
    }
}