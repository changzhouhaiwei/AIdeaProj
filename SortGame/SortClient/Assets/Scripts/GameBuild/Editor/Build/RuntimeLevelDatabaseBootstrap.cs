#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishFramework;
using FishTilePlay;

/// <summary>
/// 编辑器启动时确保 Runtime Catalog 存在（levels 为空，仅 tiles/effects）。
/// 进 Play 前若 GameScene 仍挂完整 Catalog，自动改回 Runtime，避免反序列化全量关卡。
/// </summary>
[InitializeOnLoad]
public static class RuntimeLevelDatabaseBootstrap
{
    static RuntimeLevelDatabaseBootstrap()
    {
        EditorApplication.delayCall += Ensure;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void Ensure()
    {
        try
        {
            // 不 forceRefresh：已有空 levels 的 Runtime 时跳过全量 CopySerialized。
            BuildUtility.EnsureRuntimeLevelDatabase(LevelPlatform.Android);
            BuildUtility.EnsureRuntimeLevelDatabase(LevelPlatform.IOS);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[RuntimeLevelDatabaseBootstrap] " + e.Message);
        }
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
        {
            return;
        }

        try
        {
            // 先落盘校正 GameScene，再扫已加载场景（避免先改内存导致 Ensure 误判已正确而不保存）。
            LevelPlatform platform = LevelPlatformResolver.GetCurrentPlatform();
            BuildUtility.EnsureGameSceneUsesRuntimeCatalog(
                platform,
                forceRefreshRuntime: false,
                saveIfChanged: true);
            GuardLoadedLevelDirectorsUseRuntime();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[RuntimeLevelDatabaseBootstrap] Play 前 Runtime Catalog 校正失败: " + e.Message);
        }
    }

    /// <summary>已加载场景里的 LevelDirector 若挂了完整 Catalog，当场改回 Runtime（不依赖落盘时机）。</summary>
    static void GuardLoadedLevelDirectorsUseRuntime()
    {
        LevelPlatform platform = LevelPlatformResolver.GetCurrentPlatform();
        LevelCatalog runtime = BuildUtility.EnsureRuntimeLevelDatabase(platform);
        if (runtime == null)
        {
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                LevelDirector[] directors = roots[r].GetComponentsInChildren<LevelDirector>(true);
                for (int d = 0; d < directors.Length; d++)
                {
                    LevelDirector director = directors[d];
                    SerializedObject so = new SerializedObject(director);
                    SerializedProperty database = so.FindProperty("database");
                    if (database == null)
                    {
                        continue;
                    }

                    LevelCatalog current = database.objectReferenceValue as LevelCatalog;
                    if (current == runtime)
                    {
                        continue;
                    }

                    if (current != null && !BuildUtility.IsFullEditorLevelDatabase(current))
                    {
                        // 已是另一平台 Runtime 等非完整表：仍统一到当前平台 Runtime。
                        string path = AssetDatabase.GetAssetPath(current).Replace('\\', '/');
                        string expected = LevelPlatformResolver.GetRuntimeDatabasePath(platform);
                        if (path.Equals(expected, System.StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    database.objectReferenceValue = runtime;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.LogWarningFormat(
                        "[RuntimeLevelDatabaseBootstrap] Play 前将 LevelDirector.database 从完整 Catalog 改回 Runtime：{0}",
                        LevelPlatformResolver.GetRuntimeDatabasePath(platform));
                }
            }
        }
    }

    [MenuItem("Tools/Level Editor/Ensure Runtime Level Databases", false, 50)]
    static void MenuEnsure()
    {
        BuildUtility.EnsureRuntimeLevelDatabase(LevelPlatform.Android, forceRefresh: true);
        BuildUtility.EnsureRuntimeLevelDatabase(LevelPlatform.IOS, forceRefresh: true);
        BuildUtility.ApplyGameSceneLevelDatabaseForBuild();
        Debug.Log("[RuntimeLevelDatabaseBootstrap] Runtime Catalog 已刷新并挂到 GameScene。");
    }
}
#endif
