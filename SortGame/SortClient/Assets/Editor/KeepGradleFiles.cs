using UnityEngine;
using UnityEditor;
using UnityEditor.Android;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class KeepGradleFiles : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1;

    private class PathDic
    {
        public string unityPath;
        public string asPath;
    }

    public static bool SkipChangeGradle;

    private static readonly HashSet<string> PreserveGradlePropertyKeys = new()
    {
        "unityStreamingAssets",
        "unityTemplateVersion"
    };

    private readonly List<PathDic> pathArr = new();

    /// <summary>
    /// 与 Android Studio 中打开的工程一致：settings.gradle 所在目录为 Gradle 根目录。
    /// Unity 传入的 path 多为 unityLibrary 子目录，需取上一级。
    /// </summary>
    private static string ResolveGradleProjectRoot(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return path;

        if (File.Exists(Path.Combine(path, "settings.gradle")))
            return Path.GetFullPath(path);

        var parent = Path.GetFullPath(Path.Combine(path, ".."));
        if (File.Exists(Path.Combine(parent, "settings.gradle")))
            return parent;

        return Path.GetFullPath(path);
    }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        if (SkipChangeGradle)
            return;

        pathArr.Clear();
        pathArr.Add(new PathDic { unityPath = "Editor/AndroidKeepFiles/launcher/build.gradle", asPath = "launcher/build.gradle" });
        pathArr.Add(new PathDic { unityPath = "Editor/AndroidKeepFiles/launcher/strings.xml", asPath = "launcher/src/main/res/values/strings.xml" });
        pathArr.Add(new PathDic { unityPath = "Editor/AndroidKeepFiles/launcher/AndroidManifest.xml", asPath = "launcher/src/main/AndroidManifest.xml" });
        pathArr.Add(new PathDic { unityPath = "Editor/AndroidKeepFiles/unityLibrary/build.gradle", asPath = "unityLibrary/build.gradle" });
        pathArr.Add(new PathDic { unityPath = "Editor/AndroidKeepFiles/unityLibrary/proguard-adsurge.txt", asPath = "unityLibrary/proguard-adsurge.txt" });
        pathArr.Add(new PathDic { unityPath = "Editor/AndroidKeepFiles/local.properties", asPath = "local.properties" });
        pathArr.Add(new PathDic { unityPath = "Editor/AndroidKeepFiles/settings.gradle", asPath = "settings.gradle" });
        pathArr.Add(new PathDic { unityPath = "Editor/AndroidKeepFiles/build.gradle", asPath = "build.gradle" });
        pathArr.Add(new PathDic { unityPath = "Editor/AndroidKeepFiles/gradle/wrapper/gradle-wrapper.properties", asPath = "gradle/wrapper/gradle-wrapper.properties" });
        pathArr.Add(new PathDic { unityPath = "Plugins/Android/UnityPlayerActivity.java", asPath = "unityLibrary/src/main/java/com/unity3d/player/UnityPlayerActivity.java" });
        // 开屏 logo 放在 AndroidKeepFiles，导出后再拷进 unityLibrary。
        // 不能放 Plugins/Android/res，Unity 2022.3 导出时会直接失败。
        pathArr.Add(new PathDic { unityPath = "Editor/AndroidKeepFiles/unityLibrary/res/drawable/sofish_logo_white.png", asPath = "unityLibrary/src/main/res/drawable/sofish_logo_white.png" });

        string gradleRoot = ResolveGradleProjectRoot(path);
        Debug.Log($"[KeepGradleFiles] Gradle project root: {gradleRoot} (Unity path: {path})");

        foreach (var item in pathArr)
        {
            string customGradlePath = Path.Combine(Application.dataPath, item.unityPath);
            string destPath = Path.Combine(gradleRoot, item.asPath);

            if (!File.Exists(customGradlePath))
            {
                Debug.LogWarning("[KeepGradleFiles] Template not found: " + customGradlePath);
                continue;
            }

            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            File.Copy(customGradlePath, destPath, true);
            Debug.Log("[KeepGradleFiles] Copied to: " + destPath);
        }
        MergeGradleProperties(gradleRoot);
        EnsureAppUpdateConfigDefault(gradleRoot);
    }

    private static void MergeGradleProperties(string gradleRoot)
    {
        var templatePath = Path.Combine(Application.dataPath, "Editor/AndroidKeepFiles/gradle.properties");
        var targetPath = Path.Combine(gradleRoot, "gradle.properties");
        if (!File.Exists(templatePath) || !File.Exists(targetPath))
        {
            if (!File.Exists(templatePath))
            {
                Debug.LogWarning("[KeepGradleFiles] gradle.properties template not found: " + templatePath);
            }

            return;
        }

        var targetLines = File.ReadAllLines(targetPath).ToList();
        var templateLines = File.ReadAllLines(templatePath);

        foreach (var templateLine in templateLines)
        {
            if (string.IsNullOrWhiteSpace(templateLine) || templateLine.TrimStart().StartsWith("#"))
            {
                continue;
            }

            var separatorIndex = templateLine.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = templateLine.Substring(0, separatorIndex).Trim();
            if (PreserveGradlePropertyKeys.Contains(key))
            {
                continue;
            }

            var replaced = false;
            for (var i = 0; i < targetLines.Count; i++)
            {
                if (!targetLines[i].StartsWith(key + "=", System.StringComparison.Ordinal))
                {
                    continue;
                }

                targetLines[i] = templateLine;
                replaced = true;
                break;
            }

            if (!replaced)
            {
                targetLines.Add(templateLine);
            }
        }

        File.WriteAllLines(targetPath, targetLines);
        Debug.Log($"[KeepGradleFiles] Merged gradle.properties. Preserved keys: {string.Join(", ", PreserveGradlePropertyKeys)}");
    }

    const string AppUpdateConfigDefault =
        "{\"enabled\":false,\"store_url\":\"market://details?id=com.sort.game\",\"fallback_url\":\"https://play.google.com/store/apps/details?id=com.sort.game\",\"latest_version\":\"1.0.30\",\"min_required_version\":\"1.0.30\",\"force_update_versions\":[],\"min_show_level\":3,\"title\":\"\",\"message\":\"\"}";

    /// <summary>
    /// 不覆盖整份广告默认值，只在缺少 AppUpdateConfig 时补上关闭状态的强更默认项。
    /// </summary>
    static void EnsureAppUpdateConfigDefault(string gradleRoot)
    {
        string xmlPath = Path.Combine(gradleRoot, "unityLibrary/src/main/res/xml/remote_config_defaults.xml");
        if (!File.Exists(xmlPath))
        {
            Debug.LogWarning("[KeepGradleFiles] remote_config_defaults.xml not found: " + xmlPath);
            return;
        }

        string text = File.ReadAllText(xmlPath);
        if (text.Contains("<key>AppUpdateConfig</key>"))
        {
            return;
        }

        int end = text.LastIndexOf("</defaults>");
        if (end < 0)
        {
            Debug.LogWarning("[KeepGradleFiles] remote_config_defaults.xml 没有 </defaults>: " + xmlPath);
            return;
        }

        string entry =
            "  <entry>\n" +
            "    <key>AppUpdateConfig</key>\n" +
            "    <value>" + AppUpdateConfigDefault + "</value>\n" +
            "  </entry>\n";
        File.WriteAllText(xmlPath, text.Insert(end, entry));
        Debug.Log("[KeepGradleFiles] Inserted AppUpdateConfig into " + xmlPath);
    }
}
