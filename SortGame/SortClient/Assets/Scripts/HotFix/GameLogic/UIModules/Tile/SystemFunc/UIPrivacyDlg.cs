using System;
using TileGameCore;
using UnityEngine;
using UnityEngine.UI;

public class UIPrivacyDlg : UIPrivacyDlgBase
{
    public Action afterClickAction;

    protected override void OnClicked(Button button)
    {
        if (button == okBtn)
        {
            GameBAData.ExcutePolicyEvent(1);

            CloseSelf();
            afterClickAction?.Invoke();
        }
        else if (button == privacyBtn)
        {
            Application.OpenURL(GameData.PrivacyUrl);
            if (AudioController.AudioClips != null)
                AudioController.PlaySound(AudioController.AudioClips.buttonSoundSpread);
        }
        else if (button == serviceBtn)
        {
            Application.OpenURL(GameData.ServiceUrl);
            if (AudioController.AudioClips != null)
                AudioController.PlaySound(AudioController.AudioClips.buttonSoundSpread);

        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        GameBAData.ExcutePolicyEvent(0);
#if UNITY_IOS && !UNITY_EDITOR
        // 弹窗出现即强制重拉 Remote Config（只一次），不依赖启动期自动 fetch
        if (GPSDKMgr.Instance != null)
        {
            GPSDKMgr.Instance.InitFirebaseSDKOnce();
        }
#endif
    }

}