using FishFramework;
using UnityEngine;
using UnityEngine.UI;

public class UIGoldTreeHelp : UIGoldTreeHelpBase
{
    protected override void OnOpen()
    {
        ShowMe();
        base.OnOpen();
    }

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == backBtn)
        {
            if (TileGameCore.AudioController.AudioClips != null)
                TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.buttonSound);
            HideMe();
        }
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        if (bgClickEventType == UIPanelBgClickEventType.CloseSelf)
        {
            HideMe();
            return;
        }

        base.OnBackgroundClicked(bgClickEventType);
    }

    protected override bool OnEscButtonPressed()
    {
        HideMe();
        return true;
    }

    public void HideMe()
    {
        rectTransform.anchoredPosition = new Vector2(-3500f, 0f);
        SetSelfVisible(false);
    }

    void ShowMe()
    {
        rectTransform.anchoredPosition = Vector2.zero;
        SetSelfVisible(true);
        GameModule.UI.BringPanelToFront(this);
        ReplayOpenAnim();
    }

    void ReplayOpenAnim()
    {
        var animation = gameObject.GetComponent<Animation>();
        if (animation == null || animation.clip == null)
        {
            return;
        }

        var clipName = animation.clip.name;
        animation.Stop();
        animation[clipName].time = 0f;
        animation.Play(clipName);
    }
}
