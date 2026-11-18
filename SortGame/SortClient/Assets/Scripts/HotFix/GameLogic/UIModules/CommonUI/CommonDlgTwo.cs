using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;

public class CommonDlgTwo : CommonDlgTwoBase
{
    protected override void OnCreating() { }

    protected override void OnDestroyed() { }

    protected override void OnOpen() { }

    protected override void OnClicked(Button button)
    {
        if (button == sureButton)
        {
            Application.Quit();
        }
        else if (button == cancelButton)
        {
            CloseSelf();
        }
        else if (button == closeButton)
        {
            CloseSelf();
        }
    }

    // private void OnReceiveMessage(NetData netData) { }

    protected override void OnVisibleChanged(bool visible) { }

}