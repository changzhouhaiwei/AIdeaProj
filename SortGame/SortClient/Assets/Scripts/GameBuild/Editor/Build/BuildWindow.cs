// using HybridCLR.Editor.Commands;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class BuildWindow : OdinMenuEditorWindow
{
    [MenuItem("自定义窗口/打包编辑器", priority = 100)]
    private static void OpenWindow()
    {
        var window = GetWindow<BuildWindow>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 800);
        window.titleContent = new GUIContent($"打包编辑器");
        window.OnClose += AssetDatabase.SaveAssets;
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        OdinMenuTree tree = new OdinMenuTree
        {
            Selection =
            {
                SupportsMultiSelect = false
            }
        };

        tree.Add($"打包", new CreateBuild());
        tree.Add("配置", GameUtility.LoadGameSettings());
        return tree;
    }

    public class CreateBuild
    {
        public enum AndroidGradleExportType
        {
            [LabelText("导出 APK")]
            Apk = 1,
            [LabelText("导出 AAB")]
            Aab = 2,
            [LabelText("导出 APK 和 AAB")]
            ApkAndAab = 3
        }

        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        [BoxGroup("Android Gradle 一键打包", ShowLabel = true)]
        [LabelText("版本号 Version Name")]
        [Delayed]
        [OnValueChanged(nameof(SaveAndroidVersionDefaults))]
        public string AndroidVersionName = PlayerSettings.bundleVersion;

        [BoxGroup("Android Gradle 一键打包")]
        [LabelText("Build 号 Version Code")]
        [MinValue(1)]
        [Delayed]
        [OnValueChanged(nameof(SaveAndroidVersionDefaults))]
        public int AndroidBuildNumber = PlayerSettings.Android.bundleVersionCode > 0 ? PlayerSettings.Android.bundleVersionCode : 1;

        [BoxGroup("Android Gradle 一键打包")]
        [LabelText("导出选项")]
        [EnumToggleButtons]
        public AndroidGradleExportType AndroidExportType = AndroidGradleExportType.ApkAndAab;

        [BoxGroup("Android Gradle 一键打包")]
        [LabelText("同步写入 Unity PlayerSettings")]
        [OnValueChanged(nameof(SaveAndroidVersionDefaults))]
        public bool SaveToPlayerSettings = true;

        [BoxGroup("Android Gradle 一键打包")]
        [LabelText("是否打 AssetBundle")]
        public bool BuildAssetBundlesBeforeAndroid;

        [BoxGroup("Android Gradle 一键打包")]
        [LabelText("是否导出 symbols.zip")]
        public bool ExportSymbolsZip;

        [BoxGroup("Android Gradle 一键打包")]
        [LabelText("是否上传 Crashlytics mapping")]
        public bool UploadCrashlyticsMapping;

        [BoxGroup("Android Gradle 一键打包")]
        [LabelText("包体分析")]
        [Tooltip("勾选后强制完整导出（禁用 append），导出成功后自动打开 Last Build Report。报告仅含 Unity 导出工程，不含 Gradle 最终 APK/AAB。")]
        public bool OpenBuildReportAfterExport;

        [OnInspectorInit]
        private void SyncAndroidVersionDefaults()
        {
            if (!TryReadTile2AndroidVersion(out var versionName, out var buildNumber))
            {
                return;
            }

            AndroidVersionName = versionName;
            AndroidBuildNumber = buildNumber;
        }

        [BoxGroup("Android Gradle 一键打包")]
        [Button("一键导出 Android 包", ButtonSizes.Large), GUIColor(0, 1, 0)]
        public void BuildAndroidByGradle()
        {
            LaunchGradleBuild(exportUnityProject: true);
        }

        [BoxGroup("Android Gradle 一键打包")]
        [Button("仅 Gradle 打包（跳过 Unity 导出）", ButtonSizes.Medium), GUIColor(0.8f, 0.9f, 1f)]
        public void BuildAndroidGradleOnly()
        {
            LaunchGradleBuild(exportUnityProject: false);
        }

        [BoxGroup("Android Gradle 一键打包")]
        [Button("打开 Android 包输出目录", ButtonSizes.Medium), GUIColor(1, 1, 1)]
        public void OpenAndroidGradleOutput()
        {
            var outputPath = GetRepoPath("BuildOutputs/Android");
            Directory.CreateDirectory(outputPath);
            EditorUtility.OpenWithDefaultApp(outputPath);
        }

        private void LaunchGradleBuild(bool exportUnityProject)
        {
            if (!ValidateAndroidVersionInputs())
            {
                return;
            }

            SaveAndroidVersionDefaultsToDisk(AndroidVersionName.Trim(), AndroidBuildNumber, SaveToPlayerSettings, true);

            if (BuildAssetBundlesBeforeAndroid)
            {
                BuildAllBundles();
            }

            if (exportUnityProject)
            {
                if (!CustomBuildPlayer.ExportAndroidProject(
                        SaveToPlayerSettings ? AndroidVersionName.Trim() : null,
                        SaveToPlayerSettings ? AndroidBuildNumber : 0,
                        forceFullExport: OpenBuildReportAfterExport))
                {
                    EditorUtility.DisplayDialog("导出失败", "Android 工程导出失败，请查看 Console 日志。", "确定");
                    return;
                }

                if (OpenBuildReportAfterExport)
                {
                    TryOpenLastBuildReport();
                }
            }

            var scriptPath = GetRepoPath("BuildAndroid.ps1");
            if (!File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("找不到打包脚本", $"脚本不存在：{scriptPath}", "确定");
                return;
            }

            var args = $"-NoExit -NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" " +
                       $"-VersionName \"{AndroidVersionName.Trim()}\" " +
                       $"-BuildNumber {AndroidBuildNumber} " +
                       $"-BuildTarget {(int)AndroidExportType} " +
                       $"-SkipUnityExport true " +
                       $"-ExportSymbolsZip {ExportSymbolsZip} " +
                       $"-UploadCrashlyticsMapping {UploadCrashlyticsMapping}";

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                WorkingDirectory = GetRepoPath(),
                UseShellExecute = true
            });

            UnityEngine.Debug.Log($"已启动 Android Gradle 打包：VersionName={AndroidVersionName}, BuildNumber={AndroidBuildNumber}, ExportType={AndroidExportType}, UnityExport={(exportUnityProject ? "done" : "skipped")}, UploadCrashlyticsMapping={UploadCrashlyticsMapping}");
        }

        private bool ValidateAndroidVersionInputs()
        {
            if (string.IsNullOrWhiteSpace(AndroidVersionName))
            {
                EditorUtility.DisplayDialog("参数错误", "版本号不能为空", "确定");
                return false;
            }

            if (AndroidBuildNumber <= 0)
            {
                EditorUtility.DisplayDialog("参数错误", "Build 号必须大于 0", "确定");
                return false;
            }

            return true;
        }

        private static void TryOpenLastBuildReport()
        {
            // Build Report Inspector 未接入，避免编辑器程序集因缺包无法编译。
            UnityEngine.Debug.LogWarning("包体分析：当前工程未安装 Build Report Inspector，已跳过自动打开。");
        }

        private void SaveAndroidVersionDefaults()
        {
            if (string.IsNullOrWhiteSpace(AndroidVersionName) || AndroidBuildNumber <= 0)
            {
                return;
            }

            SaveAndroidVersionDefaultsToDisk(AndroidVersionName.Trim(), AndroidBuildNumber, SaveToPlayerSettings, false);
        }

        private static void SaveAndroidVersionDefaultsToDisk(string versionName, int buildNumber, bool saveToPlayerSettings, bool log)
        {
            if (saveToPlayerSettings)
            {
                PlayerSettings.bundleVersion = versionName;
                PlayerSettings.Android.bundleVersionCode = buildNumber;
                AssetDatabase.SaveAssets();
            }

            UpdateLauncherGradleVersionDefaults(GetRepoPath("Tile2Android/launcher/build.gradle"), versionName, buildNumber);
            UpdateLauncherGradleVersionDefaults(GetRepoPath("Tile2Client/Assets/Editor/AndroidKeepFiles/launcher/build.gradle"), versionName, buildNumber);
            AssetDatabase.Refresh();

            if (log)
            {
                UnityEngine.Debug.Log($"已保存 Android 版本号：VersionName={versionName}, VersionCode={buildNumber}");
            }
        }

        private static void UpdateLauncherGradleVersionDefaults(string launcherGradlePath, string versionName, int buildNumber)
        {
            if (!File.Exists(launcherGradlePath))
            {
                return;
            }

            var content = File.ReadAllText(launcherGradlePath);
            content = Regex.Replace(
                content,
                @"VERSION_CODE'\)\s*\?:\s*'\d+'",
                _ => $"VERSION_CODE') ?: '{buildNumber}'");
            content = Regex.Replace(
                content,
                @"VERSION_NAME'\)\s*\?:\s*'[^']+'",
                _ => $"VERSION_NAME') ?: '{versionName}'");
            content = Regex.Replace(
                content,
                @"versionCode\s+(\d+)",
                _ => $"versionCode {buildNumber}");
            content = Regex.Replace(
                content,
                @"versionName\s+'[^']+'",
                _ => $"versionName '{versionName}'");
            File.WriteAllText(launcherGradlePath, content);
        }

        private static bool TryReadTile2AndroidVersion(out string versionName, out int buildNumber)
        {
            versionName = PlayerSettings.bundleVersion;
            buildNumber = PlayerSettings.Android.bundleVersionCode > 0 ? PlayerSettings.Android.bundleVersionCode : 1;

            var launcherGradle = GetRepoPath("Tile2Android/launcher/build.gradle");
            if (!File.Exists(launcherGradle))
            {
                return false;
            }

            var content = File.ReadAllText(launcherGradle);
            var versionNameMatch = Regex.Match(content, @"VERSION_NAME'\)\s*\?:\s*'([^']+)'");
            if (!versionNameMatch.Success)
            {
                versionNameMatch = Regex.Match(content, @"versionName\s+'([^']+)'");
            }

            if (versionNameMatch.Success)
            {
                versionName = versionNameMatch.Groups[1].Value;
            }

            var buildNumberMatch = Regex.Match(content, @"VERSION_CODE'\)\s*\?:\s*'(\d+)'");
            if (!buildNumberMatch.Success)
            {
                buildNumberMatch = Regex.Match(content, @"versionCode\s+(\d+)");
            }

            if (buildNumberMatch.Success && int.TryParse(buildNumberMatch.Groups[1].Value, out var parsedBuildNumber))
            {
                buildNumber = parsedBuildNumber;
            }

            return versionNameMatch.Success || buildNumberMatch.Success;
        }

        private static string GetRepoPath(string relativePath = "")
        {
            var unityProjectPath = Directory.GetParent(Application.dataPath)?.FullName;
            var repoPath = Directory.GetParent(unityProjectPath ?? Application.dataPath)?.FullName ?? Application.dataPath;
            return string.IsNullOrEmpty(relativePath) ? repoPath : Path.Combine(repoPath, relativePath);
        }

        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        [BoxGroup("生成 Bundles", ShowLabel = true)]
        [Button("Build All Bundles", ButtonSizes.Large), GUIColor(1, 0, 0)]
        public void BuildAllBundles()
        {
            BuildUtility.BuildAllBundles();
        }
    }
}