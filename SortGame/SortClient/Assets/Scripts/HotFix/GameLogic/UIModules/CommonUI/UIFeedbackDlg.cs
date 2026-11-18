using System;
using UnityEngine.UI;
using FishFramework;

public class UIFeedbackDlg : UIFeedBackBase
{
    protected override void OnClicked(Button button)
    {
        if (button == closeButton)
        {
            CloseSelf();
        }
        else if (button == customerBtn)
        {
            if (inputFieldLegacy.text.Length > 0)
            {
                GPSDKMgr.Instance.UserFeedbackInGame(inputFieldLegacy.text);
                CloseSelf();
                //反馈
            }
        }
    }
}