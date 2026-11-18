
using System;
using UnityEngine.UI;

class UIAdBreakDlg : UIAdBreakDlgBase
{
    public Action afterAct;
    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == emptyButton)
        {
            afterAct?.Invoke();
            CloseSelf();
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        Delay(1.5f, () =>
        {
            afterAct?.Invoke();
            CloseSelf();
        });
    }
}