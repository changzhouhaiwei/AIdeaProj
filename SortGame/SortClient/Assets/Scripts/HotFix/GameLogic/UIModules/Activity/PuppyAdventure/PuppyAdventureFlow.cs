using System;
using FishFramework;
using UnityEngine;

public static class PuppyAdventureFlow
{
    static Action continuation;
    static bool winPresentationPending;
    static bool returnToSettlementAfterMatch;
    /// <summary>OpenMain 替换旧面板时抑制 FinishMain，避免刚写入的 continuation 被 OnDestroying 清掉。</summary>
    static bool suppressMainFinishOnDestroy;

    /// <summary>从主界面入口打开。局内结算弹活动时为 false，感叹号和关闭按钮不显示。</summary>
    public static bool BrowseFromHome { get; private set; }

    /// <summary>胜利页开启弹窗点 Start 后：匹配页用 tapToContinue 回胜利页，不进主界面。</summary>
    public static bool ShouldReturnToSettlementAfterMatch => returnToSettlementAfterMatch;

    /// <summary>GM ResetData：清挂起的结算/弹窗续流，避免删档后仍弹出冒险界面。</summary>
    public static void ClearPending()
    {
        continuation = null;
        winPresentationPending = false;
        returnToSettlementAfterMatch = false;
    }

    public static void RecordLevelWinAtCompletion()
    {
        winPresentationPending = PuppyAdventureDemoState.RecordLevelWin();
    }

    public static bool TryShowLevelWin(Action next)
    {
        if (!winPresentationPending)
            return false;

        winPresentationPending = false;
        continuation = next;
        BrowseFromHome = false;
        OpenMain();
        return true;
    }

    public static bool TryShowGiveUp(Action next)
    {
        if (!PuppyAdventureDemoState.RecordGiveUp())
            return false;

        continuation = next;
        BrowseFromHome = false;
        OpenMain();
        return true;
    }

    public static void OpenFromEntry()
    {
        if (GameModule.UI == null)
        {
            Debug.LogError("[PuppyAdventure] OpenFromEntry: UI module missing");
            return;
        }

        if (!PuppyAdventureDemoState.IsMainEntryVisible && !PuppyAdventureDemoState.IsRunning)
            return;

        Debug.Log($"[PuppyAdventure] OpenFromEntry running={PuppyAdventureDemoState.IsRunning} mode={PuppyAdventureDemoState.StartMode}");
        if (PuppyAdventureDemoState.IsRunning)
        {
            // 主界面入口只看当前落点。局内胜利那次跳跃已经播过，这里不再重播，也不出「点击继续」。
            PuppyAdventureDemoState.SetPresentation(PuppyAdventurePresentation.Current);
            continuation = null;
            BrowseFromHome = true;
            OpenMain();
            return;
        }

        BrowseFromHome = true;
        PuppyAdventureDemoState.OpenPopup(PuppyAdventurePopupReason.Manual);
        GameModule.UI.OpenPanel<UIPuppyAdventureStart>();
    }

    public static bool TryShowPendingPopup(Action next = null)
    {
        if (GameModule.UI == null || !PuppyAdventureDemoState.HasPendingAutoPopup())
            return false;

        PuppyAdventurePopupReason reason = PuppyAdventureDemoState.GetPendingAutoPopup();
        PuppyAdventureDemoState.MarkPopupShown(reason);
        continuation = next;
        BrowseFromHome = false;
        // 胜利页传入 continuation：匹配结束后回胜利页；主界面自动弹窗无 continuation，仍进主界面
        returnToSettlementAfterMatch = next != null;
        // game_trigger_activity：仅胜利页自动弹出时报 status=0；主界面手动/自动不打
        if (returnToSettlementAfterMatch)
        {
            GameBAData.ExcuteGameTriggerActivity(
                "4",
                "puppy",
                0,
                PuppyAdventureDemoState.GetActivityUniqueId(upcoming: true));
        }
        GameModule.UI.OpenPanel<UIPuppyAdventureStart>();
        return true;
    }

    public static void FinishMain()
    {
        Action next = continuation;
        continuation = null;
        returnToSettlementAfterMatch = false;
        next?.Invoke();
    }

    /// <summary>Main 异常关闭（未交接 Reward、非 OpenMain 替换）时兜底续流。</summary>
    public static void FinishMainIfNotSuppressed()
    {
        if (suppressMainFinishOnDestroy)
            return;
        FinishMain();
    }

    public static void FinishReward()
    {
        // 升阶 Start 只由胜利页 TryShowPendingPopup 触发，关闭 Reward 不弹
        FinishMain();
    }

    public static void FinishStartPopup()
    {
        FinishMain();
    }

    public static void HandleTimeout()
    {
        if (!PuppyAdventureDemoState.CheckTimeout())
            return;

        winPresentationPending = false;
        if (GameModule.UI == null)
            return;

        if (GameModule.UI.ExistPanel<UIPuppyAdventureMatch>())
            GameModule.UI.DestroyPanel<UIPuppyAdventureMatch>();
        if (GameModule.UI.ExistPanel<UIPuppyAdventureMain>())
            GameModule.UI.DestroyPanel<UIPuppyAdventureMain>();
        FishXSortPlay.UIGameOver gameOver = UnityEngine.Object.FindObjectOfType<FishXSortPlay.UIGameOver>(true);
        if (gameOver != null)
        {
            gameOver.RefreshStageOverTips();
        }
        continuation = null;
        returnToSettlementAfterMatch = false;
    }

    /// <summary>匹配页进入主界面：按浏览态显示 Close/Info，隐藏 tapBtn。</summary>
    public static void EnterMainFromMatch()
    {
        BrowseFromHome = true;
        OpenMain();
    }

    static void OpenMain()
    {
        if (GameModule.UI.ExistPanel<UIPuppyAdventureMain>())
        {
            suppressMainFinishOnDestroy = true;
            try
            {
                GameModule.UI.DestroyPanel<UIPuppyAdventureMain>();
            }
            finally
            {
                suppressMainFinishOnDestroy = false;
            }
        }
        GameModule.UI.OpenPanel<UIPuppyAdventureMain>();
    }
}

public sealed class PuppyAdventureRuntime : MonoBehaviour
{
    float nextTick;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<PuppyAdventureRuntime>() != null)
            return;
        var go = new GameObject("[PuppyAdventureRuntime]");
        DontDestroyOnLoad(go);
        go.AddComponent<PuppyAdventureRuntime>();
    }

    void Update()
    {
        if (Time.unscaledTime < nextTick)
            return;
        nextTick = Time.unscaledTime + 1f;
        if (!GPUserDataManager.HasInit() || !GameData.BFromStartScene)
            return;
        PuppyAdventureFlow.HandleTimeout();
    }
}
