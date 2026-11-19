using FishFramework;
using System;
using XSortGamePlay;
using UnityEngine.UI;

public class UIPuppyAdventureHelp : UIPuppyAdventureHelpBase
{
    protected override void PlayOpenAnim(Action onFinish = null)
    {
        animState = UIPanelAnimState.Idle;
        onFinish?.Invoke();
    }

    protected override void OnOpen()
    {
        LocalizedTMPRefresher.SetText(titleTex, () => PuppyAdventureLoc.Get("JungleQuest_Title"));
        LocalizedTMPRefresher.SetText(help1, () => PuppyAdventureLoc.Get("JungleQuest_Help1"));
        LocalizedTMPRefresher.SetText(help2, () => PuppyAdventureLoc.Get("JungleQuest_Help2"));
        LocalizedTMPRefresher.SetText(help3, () => PuppyAdventureLoc.Get("JungleQuest_Help3"));
        LocalizedTMPRefresher.SetText(help4, () => PuppyAdventureLoc.Get("JungleQuest_Help4"));
        base.OnOpen();
    }

    protected override void OnClicked(Button button)
    {
        // CloseSelf 内已播 buttonSound
        if (button == closeBtn)
            CloseSelf();
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        // 与关按钮一致；Help 不持有 continuation
        CloseSelf();
    }
}
