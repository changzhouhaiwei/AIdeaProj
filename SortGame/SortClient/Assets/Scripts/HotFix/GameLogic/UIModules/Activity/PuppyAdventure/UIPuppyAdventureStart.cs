using DG.Tweening;
using FishFramework;
using TileGamePlay;
using UnityEngine.UI;

public class UIPuppyAdventureStart : UIPuppyAdventureStartBase
{
    Tween timerTween;
    bool dismissing;
    bool handoffToMatch;

    protected override void OnOpen()
    {
        dismissing = false;
        handoffToMatch = false;
        RefreshView();
        timerTween = DOVirtual.DelayedCall(1f, RefreshView)
            .SetLoops(-1)
            .SetUpdate(true);
        base.OnOpen();
    }

    void RefreshView()
    {
        LocalizedTMPRefresher.SetText(titleTex, () => PuppyAdventureLoc.Get("JungleQuest_Title"));
        LocalizedTMPRefresher.SetText(prizeLabelTex, () => PuppyAdventureLoc.Get("JungleQuest_GrandPrize"));
        // 失败冷却中用 FailCooldownDes；其余用阶段 startDesKey
        string desKey = PuppyAdventureDemoState.StartMode == PuppyAdventureStartMode.Cooldown
            ? "JungleQuest_FailCooldownDes"
            : PuppyAdventureDemoState.StageCfg.startDesKey;
        LocalizedTMPRefresher.SetText(desTex, () => PuppyAdventureLoc.Get(desKey));

        if (prizeNumTex != null)
            prizeNumTex.text = PuppyAdventureDemoState.PrizePool.ToString();

        bool ready = PuppyAdventureDemoState.StartMode == PuppyAdventureStartMode.Ready;
        if (startBtn != null)
            startBtn.gameObject.SetActive(ready);
        if (startGreyBtn != null)
            startGreyBtn.gameObject.SetActive(!ready);

        if (cooldownTex != null)
        {
            PuppyAdventureStartMode mode = PuppyAdventureDemoState.StartMode;
            cooldownTex.gameObject.SetActive(!ready);
            cooldownTex.text = mode == PuppyAdventureStartMode.DailyClosed
                ? PuppyAdventureLoc.Get("JungleQuest_DailyClosedDes") + "\n"
                  + PuppyAdventureDemoState.FormatTime(PuppyAdventureDemoState.DailyClosedSecondsLeft)
                : PuppyAdventureDemoState.FormatTime(PuppyAdventureDemoState.CooldownSecondsLeft);
        }
    }

    protected override void OnClicked(Button button)
    {
        if (button == closeBtn)
        {
            // 小狗点 X = 拒绝开启 → activity status=9（排位点 X 仍开启，不打 9）
            string refuseUniqueId = PuppyAdventureDemoState.GetActivityUniqueId(upcoming: true);
            int stage = PuppyAdventureDemoState.Stage;
            int key = PuppyAdventureDemoState.CurWin;
            PuppyAdventureDemoState.ClosePopupByX();
            GameBAData.ExcutePuppyActivity(9, stage, key, refuseUniqueId);
            DismissStart(PuppyAdventureFlow.FinishStartPopup);
            return;
        }

        if (button == startBtn && PuppyAdventureDemoState.StartChallenge())
        {
            if (dismissing)
                return;
            dismissing = true;
            handoffToMatch = true;
            // CloseSelf 内已播 buttonSound
            CloseSelf(() => GameModule.UI.OpenPanel<UIPuppyAdventureMatch>());
        }
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        // prefab CloseSelf：默认 CloseSelf(null) 会丢掉 UnlockNextLevelAfterActivity
        if (dismissing || handoffToMatch)
            return;
        string refuseUniqueId = PuppyAdventureDemoState.GetActivityUniqueId(upcoming: true);
        int stage = PuppyAdventureDemoState.Stage;
        int key = PuppyAdventureDemoState.CurWin;
        PuppyAdventureDemoState.ClosePopupByX();
        GameBAData.ExcutePuppyActivity(9, stage, key, refuseUniqueId);
        DismissStart(PuppyAdventureFlow.FinishStartPopup);
    }

    void DismissStart(System.Action onFinish)
    {
        if (dismissing)
            return;
        dismissing = true;
        CloseSelf(onFinish);
    }

    protected override void OnDestroying()
    {
        timerTween?.Kill();
        timerTween = null;
        // 未交接 Match 时兜底续流（点背景/异常 Destroy 漏调 FinishStartPopup）
        if (!handoffToMatch)
            PuppyAdventureFlow.FinishMainIfNotSuppressed();
        base.OnDestroying();
    }
}
