using TMPro;
using UnityEngine;
using TileGamePlay;

public enum AvatarFrameDecorScene
{
    MainMenu,
    Preview,
    FrameListLocked,
    FrameListUnlocked,
    RewardGetStatic,
}

public static class AvatarFrameUiHelper
{
    public static bool IsLimitedAvatarFrame(int frameId)
    {
        if (frameId <= 0)
        {
            return false;
        }

        var cfg = ConfigHelper.Tables.TbAvatarFrameCfg.GetOrDefault(frameId);
        return cfg != null && cfg.IsLimit == 1;
    }

    public static bool IsWinStreakAvatarFrame(int frameId)
    {
        return GPUserDataManager.IsWinStreakAvatarFrameId(frameId);
    }

    public static Transform FindHonorNode(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        var honor = root.Find("honor") ?? root.Find("hornor");
        if (honor != null)
        {
            return honor;
        }

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root)
            {
                continue;
            }

            if (child.name == "honor" || child.name == "hornor")
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>
    /// 只控制 honor，不改 frame。连胜总天数达到 7 或 forceShow 时显示。
    /// </summary>
    public static void RefreshHonorNode(Transform root, bool forceShow = false)
    {
        if (root == null)
        {
            return;
        }

        var honor = FindHonorNode(root);
        if (honor != null)
        {
            honor.gameObject.SetActiveEx(forceShow || HasActiveWinStreakHonor());
        }
    }

    static bool HasActiveWinStreakHonor()
    {
        return GPUserDataManager.HasInit()
            && GPUserDataManager.Instance.HasActiveWinStreakAvatarFrame();
    }

    public static void BindWinStreakCountRefs(Transform root, ref GameObject winStreakRoot, ref TextMeshProUGUI winStreakText)
    {
        if (winStreakRoot != null)
        {
            return;
        }

        var streakTran = root.Find("winStreakCount");
        if (streakTran == null)
        {
            return;
        }

        winStreakRoot = streakTran.gameObject;
        winStreakText = streakTran.GetComponent<TextMeshProUGUI>();
    }

    public static void BindWinStreakCountFixedRefs(
        Transform root,
        ref GameObject winStreakFixedRoot,
        ref TextMeshProUGUI winStreakFixedText)
    {
        if (winStreakFixedRoot != null)
        {
            return;
        }

        var streakTran = root.Find("winStreakCountFixed");
        if (streakTran == null)
        {
            return;
        }

        winStreakFixedRoot = streakTran.gameObject;
        winStreakFixedText = streakTran.GetComponent<TextMeshProUGUI>();
    }

    public static void BindTimeRemainingRefs(
        Transform root,
        ref GameObject timeRemainingRoot,
        ref TextMeshProUGUI timeRemainingText)
    {
        if (timeRemainingRoot != null)
        {
            return;
        }

        var timeTran = root.Find("TimeRemaining");
        if (timeTran == null)
        {
            return;
        }

        timeRemainingRoot = timeTran.gameObject;
        var textTran = timeTran.Find("Text (TMP)") ?? timeTran.Find("Image/Text (TMP)");
        timeRemainingText = textTran != null
            ? textTran.GetComponent<TextMeshProUGUI>()
            : timeTran.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public static TextMeshProUGUI FindTotalWinCountText(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        var direct = root.Find("TotalWinCount") ?? root.Find("honor/TotalWinCount");
        if (direct != null)
        {
            return direct.GetComponent<TextMeshProUGUI>();
        }

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "TotalWinCount")
            {
                return child.GetComponent<TextMeshProUGUI>();
            }
        }

        return null;
    }

    /// <summary>
    /// 刷新 honor 与 TotalWinCount。不改 frame 显隐。
    /// forceShowHonor 用于第 7 天领奖弹窗等必须展示 honor 的场景。
    /// </summary>
    public static void RefreshTotalWinCount(Transform root, int frameId = 0, bool forceShowHonor = false)
    {
        RefreshHonorNode(root, forceShowHonor);

        var honor = FindHonorNode(root);
        if (honor != null && !honor.gameObject.activeSelf)
        {
            return;
        }

        RefreshTotalWinCountText(FindTotalWinCountText(root));
    }

    public static void RefreshTotalWinCountText(TextMeshProUGUI totalWinCount)
    {
        if (totalWinCount == null || GameData.Activity == null)
        {
            return;
        }

        totalWinCount.gameObject.SetActiveEx(true);
        LocalizedTMPRefresher.SetText(
            totalWinCount,
            () => GameData.Activity.GetDailyVictoryTotalWinDays().ToString());
    }

    public static void ApplyFrameDecor(
        Transform root,
        int frameId,
        bool unlocked,
        AvatarFrameDecorScene scene,
        string remainText = null)
    {
        RefreshTotalWinCount(root, frameId);

        GameObject winStreakRoot = null;
        TextMeshProUGUI winStreakText = null;
        GameObject winStreakFixedRoot = null;
        TextMeshProUGUI winStreakFixedText = null;
        GameObject timeRemainingRoot = null;
        TextMeshProUGUI timeRemainingText = null;

        BindWinStreakCountRefs(root, ref winStreakRoot, ref winStreakText);
        BindWinStreakCountFixedRefs(root, ref winStreakFixedRoot, ref winStreakFixedText);
        BindTimeRemainingRefs(root, ref timeRemainingRoot, ref timeRemainingText);

        if (!IsLimitedAvatarFrame(frameId))
        {
            HideAllFrameDecor(winStreakRoot, winStreakFixedRoot, timeRemainingRoot);
            return;
        }

        switch (scene)
        {
            case AvatarFrameDecorScene.MainMenu:
                RefreshWinStreakCountFixed(winStreakFixedRoot, winStreakFixedText, frameId, visible: false);
                RefreshTimeRemaining(timeRemainingRoot, timeRemainingText, frameId, unlocked, visible: false);
                RefreshWinStreakCount(winStreakRoot, winStreakText, frameId, unlocked, visible: true);
                break;

            case AvatarFrameDecorScene.Preview:
                RefreshWinStreakCountFixed(winStreakFixedRoot, winStreakFixedText, frameId, visible: false);
                RefreshWinStreakCount(winStreakRoot, winStreakText, frameId, unlocked, visible: true);
                RefreshTimeRemaining(
                    timeRemainingRoot,
                    timeRemainingText,
                    frameId,
                    unlocked,
                    visible: true,
                    remainText,
                    useLiveRemain: true);
                break;

            case AvatarFrameDecorScene.FrameListLocked:
                RefreshWinStreakCount(winStreakRoot, winStreakText, frameId, unlocked, visible: false);
                RefreshTimeRemaining(timeRemainingRoot, timeRemainingText, frameId, unlocked, visible: false);
                RefreshWinStreakCountFixed(winStreakFixedRoot, winStreakFixedText, frameId, visible: true);
                break;

            case AvatarFrameDecorScene.FrameListUnlocked:
                RefreshWinStreakCountFixed(winStreakFixedRoot, winStreakFixedText, frameId, visible: false);
                RefreshWinStreakCount(winStreakRoot, winStreakText, frameId, unlocked, visible: true);
                RefreshTimeRemaining(
                    timeRemainingRoot,
                    timeRemainingText,
                    frameId,
                    unlocked,
                    visible: true,
                    remainText,
                    useLiveRemain: true);
                break;

            case AvatarFrameDecorScene.RewardGetStatic:
                RefreshWinStreakCountForRewardGet(winStreakRoot, winStreakText, frameId);
                RefreshWinStreakCountFixed(winStreakFixedRoot, winStreakFixedText, frameId, visible: false);
                RefreshTimeRemaining(
                    timeRemainingRoot,
                    timeRemainingText,
                    frameId,
                    unlocked: true,
                    visible: true,
                    remainText: null,
                    useLiveRemain: false);
                break;
        }
    }

    public static void RefreshWinStreakCountForRewardGet(
        GameObject winStreakRoot,
        TextMeshProUGUI winStreakText,
        int frameId)
    {
        bool show = frameId > 0
            && (IsWinStreakAvatarFrame(frameId) || IsLimitedAvatarFrame(frameId));

        if (winStreakRoot != null)
        {
            winStreakRoot.SetActiveEx(show);
        }

        if (!show || winStreakText == null || GameData.Activity == null)
        {
            return;
        }

        LocalizedTMPRefresher.SetText(
            winStreakText,
            () => GameData.Activity.GetDailyVictoryTotalWinDays().ToString());
    }

    public static void RefreshWinStreakCount(
        GameObject winStreakRoot,
        TextMeshProUGUI winStreakText,
        int frameId,
        bool unlocked,
        bool visible)
    {
        bool show = visible
            && frameId > 0
            && unlocked
            && IsLimitedAvatarFrame(frameId);

        if (winStreakRoot != null)
        {
            winStreakRoot.SetActiveEx(show);
        }

        if (!show || winStreakText == null || GameData.Activity == null)
        {
            return;
        }

        LocalizedTMPRefresher.SetText(
            winStreakText,
            () => GameData.Activity.GetDailyVictoryTotalWinDays().ToString());
    }

    public static void RefreshWinStreakCountFixed(
        GameObject winStreakFixedRoot,
        TextMeshProUGUI winStreakFixedText,
        int frameId,
        bool visible)
    {
        bool show = visible && frameId > 0 && IsLimitedAvatarFrame(frameId);

        if (winStreakFixedRoot != null)
        {
            winStreakFixedRoot.SetActiveEx(show);
        }

        if (!show || winStreakFixedText == null)
        {
            return;
        }

        var cfg = ConfigHelper.Tables.TbAvatarFrameCfg.GetOrDefault(frameId);
        if (cfg != null && cfg.LimitDay > 0)
        {
            winStreakFixedText.text = cfg.LimitDay.ToString();
        }
    }

    public static void RefreshTimeRemaining(
        GameObject timeRemainingRoot,
        TextMeshProUGUI timeRemainingText,
        int frameId,
        bool unlocked,
        bool visible,
        string remainText = null,
        bool useLiveRemain = true)
    {
        bool showRemain = false;
        string displayText = null;

        if (visible && frameId > 0 && IsLimitedAvatarFrame(frameId))
        {
            if (useLiveRemain)
            {
                if (unlocked && GPUserDataManager.HasInit())
                {
                    if (string.IsNullOrEmpty(remainText))
                    {
                        remainText = GPUserDataManager.Instance.GetAvatarFrameRemainText(frameId);
                    }

                    displayText = remainText;
                    showRemain = !string.IsNullOrEmpty(displayText);
                }
            }
            else
            {
                displayText = GetStaticLimitDayText(frameId);
                showRemain = !string.IsNullOrEmpty(displayText);
            }
        }

        if (timeRemainingRoot != null)
        {
            timeRemainingRoot.SetActiveEx(showRemain);
        }

        if (showRemain && timeRemainingText != null)
        {
            timeRemainingText.text = displayText;
        }
    }

    public static void HideAllFrameDecor(
        GameObject winStreakRoot,
        GameObject winStreakFixedRoot,
        GameObject timeRemainingRoot)
    {
        if (winStreakRoot != null)
        {
            winStreakRoot.SetActiveEx(false);
        }

        if (winStreakFixedRoot != null)
        {
            winStreakFixedRoot.SetActiveEx(false);
        }

        HideTimeRemaining(timeRemainingRoot);
    }

    public static void HideTimeRemaining(GameObject timeRemainingRoot)
    {
        if (timeRemainingRoot != null)
        {
            timeRemainingRoot.SetActiveEx(false);
        }
    }

    static string GetStaticLimitDayText(int frameId)
    {
        var cfg = ConfigHelper.Tables.TbAvatarFrameCfg.GetOrDefault(frameId);
        return cfg != null && cfg.LimitDay > 0 ? $"{cfg.LimitDay}D" : string.Empty;
    }
}
