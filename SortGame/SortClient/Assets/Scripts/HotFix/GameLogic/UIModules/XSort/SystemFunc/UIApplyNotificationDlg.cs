

using UnityEngine.UI;
using FishFramework;

public class UIApplyNotificationDlg : UIApplyNotificationDlgBase
{

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == noBtn || button == closeBtn)
        {
            CloseSelf();
        }
        else if (button == yesBtn)
        {
            //申请通知
            GPSDKMgr.Instance.RequestNotificationPermission();
            CloseSelf();
        }
    }


    protected override bool OnEscButtonPressed()
    {
        CloseSelf();
        return base.OnEscButtonPressed();
    }

    protected override void OnDestroyed()
    {
        base.OnDestroyed();
        GPSDKMgr.Instance.SetNotificationRequested();
    }


}