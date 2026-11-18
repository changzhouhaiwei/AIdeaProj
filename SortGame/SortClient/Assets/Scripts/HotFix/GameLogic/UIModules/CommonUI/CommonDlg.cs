using System;
using UnityEngine.UI;
using FishFramework;

public class CommonDlg : CommonDlgBase
{
    private Action completed;

    protected override void OnCreating()
    {
    }

    private void InitUI(string desc, Action action)
    {
        descText.text = desc;
        completed = action;
    }

    protected override void OnClicked(Button button)
    {
        if (button == sureButton)
        {
            DeleteMe();
        }
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        DeleteMe();
    }

    private void DeleteMe()
    {
        CloseSelf();
        completed?.Invoke();
    }

    protected override void OnDestroyed()
    {
    }

    public static CommonDlg ShowDlg(string desc, Action action)
    {
        CommonDlg commonDlg = GameModule.UI.OpenPanel<CommonDlg>();
        commonDlg.InitUI(desc, action);
        return commonDlg;
    }
}