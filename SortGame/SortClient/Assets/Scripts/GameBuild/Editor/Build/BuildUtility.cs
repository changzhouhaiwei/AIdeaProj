using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FishFramework;
using FishXSortPlay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildUtility
{
    private const string GameScenePath = "Assets/CABRes/Scenes/GameScene.unity";
    private const string IosLevelsFolderMarker = "/LevelsIOS/";
    private const string AndroidLevelsFolderMarker = "/LevelsAndroid/";
    private const string IosLevelDatabaseFile = "Level Database IOS.asset";
    private const string AndroidLevelDatabaseFile = "Level Database Android.asset";
    private const string IosLevelAssetPrefix = "IOS_Level_";
    private const string AndroidLevelAssetPrefix = "Android_Level_";

    static readonly Regex LevelAssetNameRegex = new(
        @"^(Android_Level_|IOS_Level_)(\d+)\.asset$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>是否为 iOS 关卡目录 / 单关 / 编辑器 Catalog（不含 Runtime）。</summary>
    public static bool IsIosLevelDataPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        string normalized = path.Replace('\\', '/');
        if (normalized.IndexOf(IosLevelsFolderMarker, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (normalized.EndsWith("/" + IosLevelDatabaseFile, StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(IosLevelDatabaseFile, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string fileName = Path.GetFileName(normalized);
        return fileName.StartsWith(IosLevelAssetPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>是否为 Android 关卡目录 / 单关 / 编辑器 Catalog（不含 Runtime）。</summary>
    public static bool IsAndroidLevelDataPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        string normalized = path.Replace('\\', '/');
        if (normalized.IndexOf(AndroidLevelsFolderMarker, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (normalized.EndsWith("/" + AndroidLevelDatabaseFile, StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(AndroidLevelDatabaseFile, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string fileName = Path.GetFileName(normalized);
        return fileName.StartsWith(AndroidLevelAssetPrefix, StringComparison.OrdinalIgnoreCase);
    }

    static bool IsWrongPlatformRuntimeCatalog(string normalized)
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        if (normalized.EndsWith("/Level Database IOS Runtime.asset", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("Level Database IOS Runtime.asset", StringComparison.OrdinalIgnoreCase))
        {
            return target == BuildTarget.Android;
        }

        if (normalized.EndsWith("/Level Database Android Runtime.asset", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("Level Database Android Runtime.asset", StringComparison.OrdinalIgnoreCase))
        {
            return target == BuildTarget.iOS;
        }

        return false;
    }

    /// <summary>
    /// 判断是不是关卡资源，并取出文件名里的数字。该数字只是命名，不是左侧列表 #。
    /// </summary>
    public static bool TryParseLevelAssetNumber(string path, out int fileNumber, out bool isIos)
    {
        fileNumber = 0;
        isIos = false;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        string fileName = Path.GetFileName(path.Replace('\\', '/'));
        Match match = LevelAssetNameRegex.Match(fileName);
        if (!match.Success)
        {
            return false;
        }

        isIos = match.Groups[1].Value.StartsWith("IOS", StringComparison.OrdinalIgnoreCase);
        return int.TryParse(match.Groups[2].Value, out fileNumber);
    }

    /// <summary>
    /// 是否应从本次 AB 采集中排除。含：错平台关卡、超 maxLevelNum、完整编辑器 Catalog。
    /// </summary>
    public static bool ShouldExcludeLevelAssetFromBuild(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        string normalized = path.Replace('\\', '/');
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

        // 完整编辑器 Catalog 永不进包（运行时用 Runtime Catalog）。
        if (normalized.EndsWith("/" + AndroidLevelDatabaseFile, StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(AndroidLevelDatabaseFile, StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("/" + IosLevelDatabaseFile, StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(IosLevelDatabaseFile, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (IsWrongPlatformRuntimeCatalog(normalized))
        {
            return true;
        }

        if (target == BuildTarget.Android && IsIosLevelDataPath(normalized))
        {
            return true;
        }

        if (target == BuildTarget.iOS && IsAndroidLevelDataPath(normalized))
        {
            return true;
        }

        if (!TryParseLevelAssetNumber(normalized, out _, out bool isIos))
        {
            return false;
        }

        // 进包只认左侧列表 #。文件名里的数字不是关卡序号。
        if (!LevelChunkLoader.TryGetPlayIndex(normalized, out int playIndex))
        {
            return true;
        }

        int levelNum = playIndex + 1;

        LevelPlatform platform = isIos ? LevelPlatform.IOS : LevelPlatform.Android;
        int max = LevelPlatformResolver.GetConfiguredMaxLevelNum(platform);
        return !LevelChunkLoader.ShouldPackLevel(levelNum, max);
    }

    /// <summary>
    /// PackByCustom：列表第 123 关 → levels_android_chunk_2（ChunkSize=50）。
    /// </summary>
    public static string PackLevelChunkBundleName(string assetPath, string bundle, string group, string build)
    {
        if (!TryParseLevelAssetNumber(assetPath, out _, out bool isIos))
        {
            return string.IsNullOrEmpty(bundle) ? assetPath : bundle;
        }

        // 分块按左侧列表 #。文件名数字不参与分块。
        if (!LevelChunkLoader.TryGetPlayIndex(assetPath, out int playIndex))
        {
            return string.IsNullOrEmpty(bundle) ? assetPath : bundle;
        }

        int levelNum = playIndex + 1;
        int chunkId = (levelNum - 1) / LevelChunkLoader.ChunkSize;
        string platform = isIos ? "ios" : "android";
        return $"levels_{platform}_chunk_{chunkId}";
    }

    /// <summary>
    /// 打包前关卡校验：Build Target 与 levelPlatform 一致；max 盖住无尽池；
    /// 列出 1..max 缺失文件（警告）；max&lt;无尽池上限则失败。
    /// </summary>
    public static bool ValidateLevelPlatformForBuild()
    {
        var settings = GameUtility.LoadGameSettings();
        if (settings == null)
        {
            Debug.LogError("[BuildUtility] 无法加载 GameSettings，关卡平台校验失败，已阻止打包。");
            return false;
        }

        LevelPlatform levelPlatform = settings.levelPlatform;
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

        bool matches =
            (levelPlatform == LevelPlatform.Android && target == BuildTarget.Android) ||
            (levelPlatform == LevelPlatform.IOS && target == BuildTarget.iOS);

        if (!matches)
        {
            Debug.LogError(string.Format(
                "[BuildUtility] 打包被阻止：当前 Build Target = {0}，但 GameSettings.levelPlatform = {1}。" +
                "请将两者调成一致（Android↔Android / iOS↔IOS）后再打包。",
                target, levelPlatform));
            return false;
        }

        int max = LevelPlatformResolver.GetConfiguredMaxLevelNum(levelPlatform);
        if (max < LevelChunkLoader.InfinitePoolMaxLevel)
        {
            Debug.LogError(string.Format(
                "[BuildUtility] 打包被阻止：当前平台 {0} 最大关卡数 {1} < 无尽池上限 {2}。",
                LevelPlatformResolver.GetDisplayName(levelPlatform),
                max,
                LevelChunkLoader.InfinitePoolMaxLevel));
            return false;
        }

        string folder = LevelPlatformResolver.GetLevelsFolder(levelPlatform);
        var missing = new List<int>();
        int present = 0;
        for (int levelNum = 1; levelNum <= max; levelNum++)
        {
            string path = LevelChunkLoader.GetLevelPath(levelNum - 1);
            if (File.Exists(path) || AssetDatabase.LoadAssetAtPath<LevelModel>(path) != null)
            {
                present++;
            }
            else
            {
                missing.Add(levelNum);
            }
        }

        if (present <= 0)
        {
            Debug.LogError(string.Format(
                "[BuildUtility] 打包被阻止：当前平台 {0} 在 1..{1} 内找不到任何关卡文件：{2}",
                LevelPlatformResolver.GetDisplayName(levelPlatform), max, folder));
            return false;
        }

        // 无尽池必须完整（Android 有限/无尽共用 34–84；iOS 资源可能尚未铺齐，仅警告）
        if (levelPlatform == LevelPlatform.Android)
        {
            for (int levelNum = LevelChunkLoader.InfinitePoolMinLevel;
                 levelNum <= LevelChunkLoader.InfinitePoolMaxLevel;
                 levelNum++)
            {
                string path = LevelChunkLoader.GetLevelPath(levelNum - 1);
                if (AssetDatabase.LoadAssetAtPath<LevelModel>(path) == null)
                {
                    Debug.LogError(string.Format(
                        "[BuildUtility] 打包被阻止：无尽池关卡缺失 {0}", path));
                    return false;
                }
            }
        }

        if (missing.Count > 0)
        {
            int show = Mathf.Min(40, missing.Count);
            Debug.LogWarning(string.Format(
                "[BuildUtility] 平台 {0} 在 1..{1} 中缺失 {2} 个关卡文件（进关会失败）。前 {3} 个：{4}{5}",
                LevelPlatformResolver.GetDisplayName(levelPlatform),
                max,
                missing.Count,
                show,
                string.Join(",", missing.GetRange(0, show)),
                missing.Count > show ? "..." : ""));
        }

        Debug.LogFormat("[BuildUtility] 关卡平台校验通过：{0}，max={1}，已有文件 {2}，分块大小 {3}。",
            LevelPlatformResolver.GetDisplayName(levelPlatform), max, present, LevelChunkLoader.ChunkSize);
        return true;
    }

    /// <summary>
    /// 从完整编辑器 Catalog 生成/更新 Runtime Catalog（levels 为空），并挂到 GameScene。
    /// </summary>
    public static void ApplyGameSceneLevelDatabaseForBuild()
    {
        var settings = GameUtility.LoadGameSettings();
        if (settings == null)
        {
            throw new Exception("[BuildUtility] 无法加载 GameSettings，GameScene 关卡数据库同步失败。");
        }

        LevelPlatform levelPlatform = settings.levelPlatform;
        if (!EnsureGameSceneUsesRuntimeCatalog(levelPlatform, forceRefreshRuntime: true, saveIfChanged: true))
        {
            Debug.LogFormat(
                "[BuildUtility] GameScene 关卡库引用未改动（已是 Runtime，或场景里没有 LevelDirector）：{0}",
                LevelPlatformResolver.GetRuntimeDatabasePath(levelPlatform));
        }
    }

    /// <summary>
    /// 确保 GameScene.LevelDirector.database 指向 Runtime Catalog。
    /// 已正确时不 dirty 场景；forceRefreshRuntime 时仍刷新 Runtime 的 tiles/effects。
    /// </summary>
    /// <returns>是否改写并保存了场景。</returns>
    public static bool EnsureGameSceneUsesRuntimeCatalog(
        LevelPlatform platform,
        bool forceRefreshRuntime,
        bool saveIfChanged)
    {
        LevelCatalog runtimeDatabase = EnsureRuntimeLevelDatabase(platform, forceRefreshRuntime);
        if (runtimeDatabase == null)
        {
            throw new Exception(string.Format(
                "[BuildUtility] GameScene 关卡数据库同步失败：无法创建 Runtime Catalog：{0}",
                LevelPlatformResolver.GetRuntimeDatabasePath(platform)));
        }

        Scene scene = GetLoadedSceneByPath(GameScenePath);
        bool openedByUtility = !scene.IsValid() || !scene.isLoaded;
        if (openedByUtility)
        {
            scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
        }

        if (!scene.IsValid() || !scene.isLoaded)
        {
            throw new Exception(string.Format(
                "[BuildUtility] GameScene 关卡数据库同步失败：无法打开场景 {0}。",
                GameScenePath));
        }

        try
        {
            LevelDirector levelController = FindLevelController(scene);
            if (levelController == null)
            {
                Debug.Log(string.Format(
                    "[BuildUtility] GameScene 没有 LevelDirector，跳过 Tile 关卡库挂载：{0}",
                    GameScenePath));
                return false;
            }

            SerializedObject serializedObject = new SerializedObject(levelController);
            SerializedProperty database = serializedObject.FindProperty("database");

            if (database == null)
            {
                throw new Exception("[BuildUtility] GameScene 关卡数据库同步失败：LevelDirector 序列化字段缺失。");
            }

            if (database.objectReferenceValue == runtimeDatabase)
            {
                return false;
            }

            database.objectReferenceValue = runtimeDatabase;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (!saveIfChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.LogFormat(
                    "[BuildUtility] GameScene 已改挂 Runtime Catalog（未落盘）：{0}",
                    LevelPlatformResolver.GetRuntimeDatabasePath(platform));
                return true;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new Exception(string.Format(
                    "[BuildUtility] GameScene 关卡数据库同步失败：保存场景失败 {0}。",
                    GameScenePath));
            }

            AssetDatabase.SaveAssets();
            Debug.LogFormat("[BuildUtility] GameScene 已挂 Runtime Catalog（levels 为空）：{0}",
                LevelPlatformResolver.GetRuntimeDatabasePath(platform));
            return true;
        }
        finally
        {
            if (openedByUtility)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    public static bool IsFullEditorLevelDatabase(LevelCatalog catalog)
    {
        if (catalog == null)
        {
            return false;
        }

        string path = AssetDatabase.GetAssetPath(catalog).Replace('\\', '/');
        return path.Equals(LevelPlatformResolver.AndroidDatabasePath, StringComparison.OrdinalIgnoreCase)
            || path.Equals(LevelPlatformResolver.IOSDatabasePath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 确保 Runtime Catalog 存在。默认：已存在且 levels 为空则跳过全量 CopySerialized。
    /// 打包 / 手动菜单 / 平台切换传 forceRefresh=true。
    /// </summary>
    public static LevelCatalog EnsureRuntimeLevelDatabase(LevelPlatform platform, bool forceRefresh = false)
    {
        string path = LevelPlatformResolver.GetRuntimeDatabasePath(platform);
        LevelCatalog runtime = AssetDatabase.LoadAssetAtPath<LevelCatalog>(path);
        if (runtime != null && !forceRefresh && IsRuntimeLevelsEmpty(runtime))
        {
            return runtime;
        }

        LevelCatalog source = LevelPlatformResolver.LoadDatabaseInEditor(platform);
        if (source == null)
        {
            Debug.LogError(string.Format(
                "[BuildUtility] 完整 Catalog 不存在：{0}",
                LevelPlatformResolver.GetDatabasePath(platform)));
            return null;
        }

        if (runtime == null)
        {
            runtime = ScriptableObject.CreateInstance<LevelCatalog>();
            AssetDatabase.CreateAsset(runtime, path);
        }

        EditorUtility.CopySerialized(source, runtime);
        runtime.name = Path.GetFileNameWithoutExtension(path);

        SerializedObject dst = new SerializedObject(runtime);
        SerializedProperty levels = dst.FindProperty("levels");
        if (levels != null && levels.isArray)
        {
            levels.arraySize = 0;
        }

        dst.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(runtime);
        AssetDatabase.SaveAssets();
        return runtime;
    }

    static bool IsRuntimeLevelsEmpty(LevelCatalog runtime)
    {
        SerializedObject so = new SerializedObject(runtime);
        SerializedProperty levels = so.FindProperty("levels");
        return levels != null && levels.isArray && levels.arraySize == 0;
    }

    private static Scene GetLoadedSceneByPath(string scenePath)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.path == scenePath)
            {
                return scene;
            }
        }

        return default;
    }

    private static LevelDirector FindLevelController(Scene scene)
    {
        List<LevelDirector> controllers = new List<LevelDirector>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            controllers.AddRange(roots[i].GetComponentsInChildren<LevelDirector>(true));
        }

        if (controllers.Count > 1)
        {
            throw new Exception(string.Format(
                "[BuildUtility] GameScene 关卡数据库同步失败：场景 {0} 中存在多个 LevelDirector。",
                GameScenePath));
        }

        return controllers.Count == 1 ? controllers[0] : null;
    }

    public static string[] GetLevelsFromBuildSettings()
    {
        var levels = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
            if (scene.enabled)
                levels.Add(scene.path);
        return levels.ToArray();
    }

    public static void Copy(string filename, string destinationDir)
    {
        var from = xasset.editor.Settings.GetBuildPath(filename);
        if (File.Exists(from))
        {
            var dest = $"{destinationDir}/{filename}";
            File.Copy(from, dest, true);
        }
        else
        {
            Debug.LogErrorFormat("File not found: {0}", from);
        }
    }

    public static string GetBundleHotTile2Path()
    {
        var dir = $"BundleHotTile2";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return dir;
    }

    public static void BuildAllBundles(Action completed = null)
    {
        if (!ValidateLevelPlatformForBuild())
        {
            EditorUtility.DisplayDialog("打包被阻止",
                "关卡平台与 Build Target 不一致，或当前平台数据库无效，详见 Console 报错。",
                "OK");
            return;
        }

        var settings = GameUtility.LoadGameSettings();
        settings.AddVersion();

        // Build All Bundles must force-rebuild: class renames do not dirty prefab AssetFileHash,
        // and encrypted outputs were previously skipped when Unity's hash filename already existed.
        var builds = xasset.editor.Build.GetAllBuilds();
        var previousForceRebuild = new bool[builds.Length];
        for (int i = 0; i < builds.Length; i++)
        {
            previousForceRebuild[i] = builds[i].forceRebuild;
            builds[i].forceRebuild = true;
        }

        try
        {
            xasset.editor.MenuItems.BuildBundles();
        }
        finally
        {
            for (int i = 0; i < builds.Length; i++)
            {
                builds[i].forceRebuild = previousForceRebuild[i];
            }
        }

        completed?.Invoke();
    }
}
