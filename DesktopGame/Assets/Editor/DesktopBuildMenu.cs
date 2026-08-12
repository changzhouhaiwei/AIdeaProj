using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// 一键打 Windows 独立桌面程序（StandaloneWindows64）。
/// 菜单：AIdeaDesktop → …
/// </summary>
public static class DesktopBuildMenu
{
    const string MenuRoot = "AIdeaDesktop/";

    static string BuildDir =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Windows64"));

    static string ExePath =>
        Path.Combine(BuildDir, PlayerSettings.productName + ".exe");

    [MenuItem(MenuRoot + "一键打包 Windows 桌面版", false, 1)]
    public static void BuildWindowsDesktop()
    {
        if (!BuildPlayer())
            return;

        EditorUtility.RevealInFinder(ExePath);
        EditorUtility.DisplayDialog(
            "打包完成",
            $"已输出到：\n{ExePath}\n\n桌面壁纸模式启动加参数 --desktop\n退出：Ctrl+Shift+Q",
            "确定");
    }

    [MenuItem(MenuRoot + "一键打包并运行（桌面壁纸）", false, 2)]
    public static void BuildAndRunDesktop()
    {
        if (!BuildPlayer())
            return;
        RunBuiltPlayer("--desktop");
    }

    [MenuItem(MenuRoot + "一键打包并运行（窗口预览）", false, 3)]
    public static void BuildAndRunPreview()
    {
        if (!BuildPlayer())
            return;
        RunBuiltPlayer("--preview");
    }

    [MenuItem(MenuRoot + "打开输出目录", false, 20)]
    public static void OpenBuildFolder()
    {
        Directory.CreateDirectory(BuildDir);
        EditorUtility.RevealInFinder(BuildDir);
    }

    static bool BuildPlayer()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("打包失败", "Build Settings 里没有启用的场景。", "确定");
            return false;
        }

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
        {
            EditorUtility.DisplayDialog("打包失败", "无法切换到 StandaloneWindows64 平台。", "确定");
            return false;
        }

        Directory.CreateDirectory(BuildDir);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = ExePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });

        var summary = report.summary;
        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError(
                $"[AIdeaDesktop] 打包失败: {summary.result}, errors={summary.totalErrors}");
            EditorUtility.DisplayDialog(
                "打包失败",
                $"结果：{summary.result}\n错误数：{summary.totalErrors}\n请看 Console。",
                "确定");
            return false;
        }

        Debug.Log(
            $"[AIdeaDesktop] 打包成功 → {ExePath} " +
            $"({summary.totalSize / (1024f * 1024f):F1} MB, {summary.totalTime.TotalSeconds:F1}s)");
        return true;
    }

    static void RunBuiltPlayer(string args)
    {
        if (!File.Exists(ExePath))
        {
            EditorUtility.DisplayDialog("启动失败", $"找不到可执行文件：\n{ExePath}", "确定");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = ExePath,
            Arguments = args ?? string.Empty,
            WorkingDirectory = BuildDir,
            UseShellExecute = true
        });
        Debug.Log($"[AIdeaDesktop] 已启动: {ExePath} {args}");
    }
}
