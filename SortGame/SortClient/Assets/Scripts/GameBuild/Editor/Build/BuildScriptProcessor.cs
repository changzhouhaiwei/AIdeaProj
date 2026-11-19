using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using xasset.editor;

[InitializeOnLoad]
public class BuildBundlesProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    static BuildBundlesProcessor()
    {
        BuildScript.preprocessBuildBundles += PreprocessBuildBundles;
        BuildScript.postprocessBuildBundles += PostprocessBuildBundles;
        BuildScript.finallyBuildBundles += FinallyBuildBundles;
        BuildScript.finallyBuildPlayer += RestoreArialFontAfterBuildSafe;
    }

    private static void PreprocessBuildBundles(BuildTask task)
    {
        // 关卡平台一致性校验：Target 与 GameSettings.levelPlatform 不一致或平台数据库无效时阻止 AB 打包。
        if (!BuildUtility.ValidateLevelPlatformForBuild())
        {
            throw new System.Exception(
                "[BuildBundlesProcessor] 关卡平台与 Build Target 不一致或平台数据库无效，已阻止 AB 打包。详见 Console。");
        }

        BuildUtility.ApplyGameSceneLevelDatabaseForBuild();
        ApplyLevelPackFilters();
        PrepareArialFontForBuildOrThrow();

        Debug.LogFormat("Prepare build bundles for {0}", task.name);
    }

    /// <summary>
    /// 按 GameSettings.max 过滤关卡；错平台排除；PackByCustom 按 50 关分块。
    /// </summary>
    private static void ApplyLevelPackFilters()
    {
        ClearLevelPackFilters();

        Settings.extraExcludeFilter = BuildUtility.ShouldExcludeLevelAssetFromBuild;
        Group.customFilter = (_, path) => !BuildUtility.ShouldExcludeLevelAssetFromBuild(path);
        Group.customPacker = BuildUtility.PackLevelChunkBundleName;

        Debug.LogFormat(
            "[BuildBundlesProcessor] 关卡打包过滤已启用：max={0}，ChunkSize={1}，Target={2}",
            FishXSortPlay.LevelPlatformResolver.GetConfiguredMaxLevelNum(),
            FishXSortPlay.LevelChunkLoader.ChunkSize,
            EditorUserBuildSettings.activeBuildTarget);
    }

    private static void ClearLevelPackFilters()
    {
        Settings.extraExcludeFilter = null;
        Group.customFilter = null;
        Group.customPacker = null;
    }

    private static void PrepareArialFontForBuildOrThrow()
    {
        if (!TMPFontAtlasCompressor.PrepareArialFontForBuild())
        {
            throw new System.Exception(
                "[BuildBundlesProcessor] Arial 字体打包前静态化失败，已阻止打包。");
        }
    }

    private static void RestoreArialFontAfterBuildSafe()
    {
        if (!TMPFontAtlasCompressor.RestoreArialFontAfterBuild())
        {
            Debug.LogError("[BuildBundlesProcessor] Arial 字体打包后还原动态失败，请手动执行「游戏工具/Arial字体设为动态」。");
        }
    }

    private static void PostprocessBuildBundles(BuildTask task)
    {
        Settings.GetDefaultSettings().Initialize();
        Debug.LogFormat("Post build bundles for {0} with files: {1}", task.name,
            string.Join("\n", task.changes.ConvertAll(Settings.GetBuildPath)));

        var settings = GameUtility.LoadGameSettings();
        var version = settings.GetVersion();
        var destinationDir = Path.Combine(BuildUtility.GetBundleHotTile2Path(), version, Settings.GetPlatformName());
        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }
        foreach (var item in task.changes)
        {
            BuildUtility.Copy(item, destinationDir);
        }
    }

    private static void FinallyBuildBundles(BuildTask task)
    {
        ClearLevelPackFilters();
        RestoreArialFontAfterBuildSafe();
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        PrepareArialFontForBuildOrThrow();
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        RestoreArialFontAfterBuildSafe();
    }
}
