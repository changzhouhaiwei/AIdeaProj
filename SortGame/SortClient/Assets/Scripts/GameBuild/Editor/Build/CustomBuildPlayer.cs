using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CustomBuildPlayer
{
    public static void BuildAndroid()
    {
        if (Application.isPlaying) return;

        if (BuildTarget.Android != EditorUserBuildSettings.activeBuildTarget)
        {
            EditorUtility.DisplayDialog("打包平台错误", "去 File--Build Settings 设置平台转换", "确定");
            return;
        }

        BuildPlayer("Release", BuildTargetGroup.Android, BuildTarget.Android, BuildOptions.CompressWithLz4);
    }

    public static bool ExportAndroidProject(string versionName = null, int buildNumber = 0, bool forceFullExport = false)
    {
        if (Application.isPlaying) return false;

        if (BuildTarget.Android != EditorUserBuildSettings.activeBuildTarget)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("切换 Android 平台失败");
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(versionName))
        {
            PlayerSettings.bundleVersion = versionName.Trim();
        }

        if (buildNumber > 0)
        {
            PlayerSettings.Android.bundleVersionCode = buildNumber;
        }

        AssetDatabase.SaveAssets();

        var levels = BuildUtility.GetLevelsFromBuildSettings();
        if (levels.Length == 0)
        {
            Debug.LogError("Nothing to export.");
            return false;
        }

        var exportPath = GetAndroidProjectExportPath();
        Directory.CreateDirectory(exportPath);

        // AcceptExternalModificationsToPlayer 表示追加导出；必须同时开启 Export Project，否则会抛 The build cannot be appended
        // 包体分析需要完整导出，追加模式往往不写 PackedAssets，SourceAssets 会为空
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

        var buildOptions = BuildOptions.CompressWithLz4;
        if (!forceFullExport)
        {
            buildOptions |= BuildOptions.AcceptExternalModificationsToPlayer;
        }
        else
        {
            Debug.Log("包体分析：强制完整导出 Android 工程（禁用 append）");
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = levels,
            locationPathName = exportPath,
            targetGroup = BuildTargetGroup.Android,
            target = BuildTarget.Android,
            options = buildOptions
        };

        BuildReport report;
        try
        {
            if (forceFullExport)
            {
                report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            }
            else
            {
                try
                {
                    report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                }
                catch (InvalidOperationException e) when (e.Message.IndexOf("cannot be appended", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // 已有工程残缺/不兼容时无法 append，改为完整导出覆盖
                    Debug.LogWarning($"Android 工程无法追加导出，改为完整导出: {e.Message}");
                    buildPlayerOptions.options = BuildOptions.CompressWithLz4;
                    report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                }
            }
        }
        finally
        {
            // IPostprocessBuildWithReport 成功时会还原；失败或异常时这里兜底。
            TMPFontAtlasCompressor.RestoreArialFontAfterBuild();
        }

        var success = report.summary.result == BuildResult.Succeeded;
        if (success)
        {
            KeepAndroidGradleWrapper();
            Debug.Log("Android 工程导出完成: " + exportPath);
        }
        else
        {
            Debug.LogError($"Android 工程导出失败: {report.summary.result}");
        }

        return success;
    }

    public static void ExportAndroidProjectCommandLine()
    {
        var versionName = GetCommandLineValue("-VersionName");
        var buildNumberText = GetCommandLineValue("-BuildNumber");
        int.TryParse(buildNumberText, out var buildNumber);

        var success = ExportAndroidProject(versionName, buildNumber);
        EditorApplication.Exit(success ? 0 : 1);
    }

    public static void BuildAndroidDevelopment()
    {
        if (Application.isPlaying) return;

        if (BuildTarget.Android != EditorUserBuildSettings.activeBuildTarget)
        {
            EditorUtility.DisplayDialog("打包平台错误", "去 File--Build Settings 设置平台转换", "确定");
            return;
        }

        BuildPlayer("Debug", BuildTargetGroup.Android, BuildTarget.Android,
            BuildOptions.CompressWithLz4 | BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler);
    }

    public static void BuildWindows()
    {
        if (Application.isPlaying) return;

        if (BuildTarget.StandaloneWindows64 != EditorUserBuildSettings.activeBuildTarget)
        {
            EditorUtility.DisplayDialog("打包平台错误", "去 File--Build Settings 设置平台转换", "确定");
            return;
        }

        BuildPlayer("Release", BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, BuildOptions.CompressWithLz4);
    }

    private static void BuildPlayer(string extraName, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, BuildOptions buildOptions)
    {
        var path = GetPathToBuilt();
        if (path.Length == 0) return;
        var levels = BuildUtility.GetLevelsFromBuildSettings();
        if (levels.Length == 0)
        {
            Debug.Log("Nothing to build.");
            return;
        }

        var buildTargetName = GetBuildTargetName(buildTarget, extraName);
        if (buildTargetName == null) return;

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = levels,
            locationPathName = path + buildTargetName,
            targetGroup = buildTargetGroup,
            target = buildTarget,
            options = buildOptions
        };
        try
        {
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
        finally
        {
            // IPostprocessBuildWithReport 成功时会还原；失败时这里兜底。
            TMPFontAtlasCompressor.RestoreArialFontAfterBuild();
        }

        EditorUtility.OpenWithDefaultApp(path);
    }

    public static string GetPathToBuilt()
    {
        return $"Players/{xasset.editor.Settings.GetPlatformName()}";
    }

    public static string GetAndroidProjectExportPath()
    {
        var unityProjectPath = Directory.GetParent(Application.dataPath)?.FullName;
        var repoPath = Directory.GetParent(unityProjectPath ?? Application.dataPath)?.FullName ?? Application.dataPath;
        return Path.Combine(repoPath, "SortAndroid");
    }

    private static void KeepAndroidGradleWrapper()
    {
        var wrapperPath = Path.Combine(GetAndroidProjectExportPath(), "gradle", "wrapper", "gradle-wrapper.properties");
        var keepWrapperPath = Path.Combine(Application.dataPath, "Editor/AndroidKeepFiles/gradle/wrapper/gradle-wrapper.properties");
        Directory.CreateDirectory(Path.GetDirectoryName(wrapperPath));

        if (File.Exists(keepWrapperPath))
        {
            File.Copy(keepWrapperPath, wrapperPath, true);
            return;
        }

        File.WriteAllText(wrapperPath,
            "distributionUrl=https\\://services.gradle.org/distributions/gradle-8.13-bin.zip\r\n",
            new UTF8Encoding(true));
    }

    private static string GetCommandLineValue(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return string.Empty;
    }

    private static string GetTimeForNow()
    {
        return DateTime.Now.ToString("yyyyMMdd-HHmmss");
    }

    private static string GetBuildTargetName(BuildTarget target, string extraName)
    {
        var targetName = $"/{PlayerSettings.productName}-{extraName}-v{PlayerSettings.bundleVersion}-{GetTimeForNow()}";
        switch (target)
        {
            case BuildTarget.Android:
                return targetName + ".apk";
            case BuildTarget.StandaloneWindows64:
                return targetName + ".exe";
            case BuildTarget.StandaloneOSX:
                return targetName + ".app";
            default:
                return targetName;
        }
    }
}