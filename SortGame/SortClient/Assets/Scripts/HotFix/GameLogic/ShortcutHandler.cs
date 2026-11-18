using System;
using UnityEngine;
using FishFramework;
using FishTilePlay;

/// <summary>
/// App Shortcut：若在游戏中则回主菜单，否则进入当前进度关卡。
/// </summary>
[DefaultExecutionOrder(-100)]
public class ShortcutHandler : MonoBehaviour
{
    private static bool s_pendingShortcutToggle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureHandlerExists()
    {
        var existing = FindObjectOfType<ShortcutHandler>();
        if (existing != null) return;
        var go = new GameObject("[ShortcutHandler]");
        DontDestroyOnLoad(go);
        go.AddComponent<ShortcutHandler>();
    }

    private void Awake()
    {
        GPSDKMgr.OnShortcutToggleGameRequested += OnShortcutToggleGameRequested;
    }

    private void OnDestroy()
    {
        GPSDKMgr.OnShortcutToggleGameRequested -= OnShortcutToggleGameRequested;
    }

    private void OnShortcutToggleGameRequested()
    {
        TryExecuteShortcutToggle();
    }

    private void TryExecuteShortcutToggle()
    {
        if (FindObjectOfType<GameDirector>() == null)
        {
            s_pendingShortcutToggle = true;
            return;
        }
        ExecuteShortcutToggle();
    }

    private static void ExecuteShortcutToggle()
    {
        s_pendingShortcutToggle = false;
        var gameController = FindObjectOfType<GameDirector>();
        if (gameController == null) return;

        if (GameDirector.IsGameActive || LevelDirector.IsLevelLoaded)
        {
            GameDirector.ReturnToMenu();
        }
        else
        {
            var level = GPUserDataManager.Instance.GetFinalLevelNum();
            GameDirector.LoadLevel(level);
        }
    }

    private void Update()
    {
        if (!s_pendingShortcutToggle) return;
        if (FindObjectOfType<GameDirector>() == null) return;
        ExecuteShortcutToggle();
    }
}
