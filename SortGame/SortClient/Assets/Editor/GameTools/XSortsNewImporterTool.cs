#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// 从外部文件夹导入 XSortsNew 贴图：按 black_a_1 这类文件名解析颜色，复制到对应颜色目录。
/// </summary>
public static class XSortsNewImporterTool
{
    public const string DefaultDstAssetsPath = "Assets/CABRes/SpriteAtlas/XSortsNew";
    public const float DefaultPixelsPerUnit = 128f;

    public static readonly IReadOnlyList<string> ValidColors = new[]
    {
        "black", "blue", "brown", "gray", "green", "orange", "pink", "purple", "red", "white", "yellow",
    };

    static readonly HashSet<string> ValidColorSet = new(ValidColors);
    static readonly HashSet<string> PreservedImageNames = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "XSort_qizi", "XSort_qizi2",
    };
    static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };

    /// <summary>花色纹理压缩设置（同时作用于源图 TextureImporter 与各色 SpriteAtlas）。</summary>
    public struct CompressionSettings
    {
        public TextureImporterCompression TextureCompression;
        public int MaxTextureSize;
        public int CompressionQuality;
        public bool CrunchedCompression;
        public bool OverrideAndroid;
        public TextureImporterFormat AndroidFormat;

        public static CompressionSettings Default => new CompressionSettings
        {
            TextureCompression = TextureImporterCompression.Compressed,
            MaxTextureSize = 1024,
            CompressionQuality = 50,
            CrunchedCompression = false,
            OverrideAndroid = true,
            AndroidFormat = TextureImporterFormat.ASTC_6x6,
        };

        public string ToSummaryLine()
        {
            string android = OverrideAndroid
                ? $"Android={AndroidFormat}"
                : "Android=跟随默认";
            return $"压缩={TextureCompression}, MaxSize={MaxTextureSize}, Quality={CompressionQuality}, Crunch={CrunchedCompression}, {android}";
        }
    }

    public sealed class ImportResult
    {
        public bool Success;
        public string ErrorMessage;
        public string SrcFolder;
        public string DstRoot;
        public float PixelsPerUnit = DefaultPixelsPerUnit;
        public CompressionSettings Compression = CompressionSettings.Default;
        public int AtlasUpdatedCount;
        public readonly List<string> Added = new();
        public readonly List<string> Replaced = new();
        public readonly List<string> Skipped = new();
        public readonly List<string> CreatedColorFolders = new();
    }

    public sealed class ClearResult
    {
        public bool Success;
        public string ErrorMessage;
        public string DstRoot;
        public readonly List<string> Deleted = new();
    }

    [MenuItem("游戏工具/导入XSortsNew贴图", priority = 20)]
    public static void OpenImporterWindow()
    {
        XSortsNewImporterWindow.ShowWindow();
    }

    public static string GetDefaultDstFolderAbs()
    {
        return ToAbsoluteProjectPath(DefaultDstAssetsPath);
    }

    public static ImportResult Import(
        string srcFolderAbs,
        string dstRootAbs,
        float pixelsPerUnit = DefaultPixelsPerUnit,
        CompressionSettings? compression = null)
    {
        var compressionSettings = compression ?? CompressionSettings.Default;
        var result = new ImportResult
        {
            SrcFolder = srcFolderAbs,
            DstRoot = dstRootAbs,
            PixelsPerUnit = pixelsPerUnit,
            Compression = compressionSettings,
        };

        if (string.IsNullOrWhiteSpace(srcFolderAbs) || !Directory.Exists(srcFolderAbs))
        {
            result.ErrorMessage = "源文件夹不存在。";
            return result;
        }

        if (string.IsNullOrWhiteSpace(dstRootAbs))
        {
            result.ErrorMessage = "目标文件夹不能为空。";
            return result;
        }

        if (pixelsPerUnit <= 0f)
        {
            result.ErrorMessage = "Pixels Per Unit 必须大于 0。";
            return result;
        }

        if (!IsValidMaxTextureSize(compressionSettings.MaxTextureSize))
        {
            result.ErrorMessage = "Max Texture Size 必须是 32~8192 之间的 2 的幂。";
            return result;
        }

        Directory.CreateDirectory(dstRootAbs);

        foreach (string srcFile in Directory.GetFiles(srcFolderAbs))
        {
            string ext = Path.GetExtension(srcFile);
            if (!IsImageExtension(ext))
                continue;

            string fileName = Path.GetFileName(srcFile);
            string skinName = Path.GetFileNameWithoutExtension(srcFile);
            if (!TryParseSkinName(skinName, out string color, out _, out _))
            {
                result.Skipped.Add(fileName);
                continue;
            }

            if (!ValidColorSet.Contains(color))
            {
                result.Skipped.Add($"{fileName} (未知颜色: {color})");
                continue;
            }

            string colorFolderAbs = Path.Combine(dstRootAbs, color);
            if (!Directory.Exists(colorFolderAbs))
            {
                Directory.CreateDirectory(colorFolderAbs);
                if (!result.CreatedColorFolders.Contains(color))
                {
                    result.CreatedColorFolders.Add(color);
                    Debug.Log($"[XSortsNew导入-新建文件夹] {color}");
                }
            }

            string dstFileAbs = Path.Combine(colorFolderAbs, fileName);
            bool exists = File.Exists(dstFileAbs);
            File.Copy(srcFile, dstFileAbs, true);

            string logPath = $"{color}/{fileName}";
            if (!exists)
            {
                string srcMeta = srcFile + ".meta";
                string dstMeta = dstFileAbs + ".meta";
                if (File.Exists(srcMeta) && !File.Exists(dstMeta))
                    File.Copy(srcMeta, dstMeta, false);

                result.Added.Add(logPath);
                Debug.Log($"[XSortsNew导入-新增] {logPath}");
            }
            else
            {
                result.Replaced.Add(logPath);
                Debug.Log($"[XSortsNew导入-替换] {logPath}");
            }
        }

        AssetDatabase.Refresh();
        ApplySpriteImportSettings(result);
        result.AtlasUpdatedCount = ApplyAtlasCompressionSettings(result.DstRoot, result.Compression);
        result.Success = true;
        Debug.Log($"[XSortsNew导入] 完成\n{BuildImportSummary(result)}");
        return result;
    }

    /// <summary>仅把压缩设置应用到目标目录下已有花色 PNG 与 SpriteAtlas（不复制文件）。</summary>
    public static ImportResult ApplyCompressionOnly(string dstRootAbs, CompressionSettings compression)
    {
        var result = new ImportResult
        {
            DstRoot = dstRootAbs,
            Compression = compression,
        };

        if (string.IsNullOrWhiteSpace(dstRootAbs) || !Directory.Exists(dstRootAbs))
        {
            result.ErrorMessage = "目标文件夹不存在。";
            return result;
        }

        if (!IsValidMaxTextureSize(compression.MaxTextureSize))
        {
            result.ErrorMessage = "Max Texture Size 必须是 32~8192 之间的 2 的幂。";
            return result;
        }

        var suitImages = new List<string>();
        foreach (string color in ValidColors)
        {
            string colorFolderAbs = Path.Combine(dstRootAbs, color);
            if (!Directory.Exists(colorFolderAbs))
                continue;

            foreach (string file in Directory.GetFiles(colorFolderAbs))
            {
                if (!IsImageExtension(Path.GetExtension(file)))
                    continue;
                if (IsPreservedImage(Path.GetFileName(file)))
                    continue;

                suitImages.Add($"{color}/{Path.GetFileName(file)}");
            }
        }

        try
        {
            AssetDatabase.StartAssetEditing();
            ApplySpriteImportSettingsForList(dstRootAbs, suitImages, DefaultPixelsPerUnit, compression, applyPixelsPerUnit: false);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        result.Replaced.AddRange(suitImages);
        result.AtlasUpdatedCount = ApplyAtlasCompressionSettings(dstRootAbs, compression);
        result.Success = true;
        Debug.Log($"[XSortsNew压缩] 完成，花色图 {suitImages.Count}，图集 {result.AtlasUpdatedCount}\n{compression.ToSummaryLine()}");
        return result;
    }

    static void ApplySpriteImportSettings(ImportResult result)
    {
        if (result.Added.Count == 0 && result.Replaced.Count == 0)
            return;

        try
        {
            AssetDatabase.StartAssetEditing();
            ApplySpriteImportSettingsForList(result.DstRoot, result.Added, result.PixelsPerUnit, result.Compression, applyPixelsPerUnit: true);
            ApplySpriteImportSettingsForList(result.DstRoot, result.Replaced, result.PixelsPerUnit, result.Compression, applyPixelsPerUnit: true);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
    }

    static void ApplySpriteImportSettingsForList(
        string dstRootAbs,
        List<string> relativePaths,
        float pixelsPerUnit,
        CompressionSettings compression,
        bool applyPixelsPerUnit)
    {
        foreach (string logPath in relativePaths)
        {
            string dstFileAbs = Path.Combine(dstRootAbs, logPath.Replace('/', Path.DirectorySeparatorChar));
            if (!TryToAssetsRelativePath(dstFileAbs, out string assetPath))
                continue;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            if (applyPixelsPerUnit)
                importer.spritePixelsPerUnit = pixelsPerUnit;

            ApplyCompressionToTextureImporter(importer, compression);
            importer.SaveAndReimport();
        }
    }

    static void ApplyCompressionToTextureImporter(TextureImporter importer, CompressionSettings compression)
    {
        importer.textureCompression = compression.TextureCompression;
        importer.compressionQuality = compression.CompressionQuality;
        importer.crunchedCompression = compression.CrunchedCompression;
        importer.maxTextureSize = compression.MaxTextureSize;

        var defaultSettings = new TextureImporterPlatformSettings
        {
            name = "DefaultTexturePlatform",
            overridden = false,
            maxTextureSize = compression.MaxTextureSize,
            format = TextureImporterFormat.Automatic,
            textureCompression = compression.TextureCompression,
            compressionQuality = compression.CompressionQuality,
            crunchedCompression = compression.CrunchedCompression,
        };
        importer.SetPlatformTextureSettings(defaultSettings);

        var android = importer.GetPlatformTextureSettings("Android");
        android.name = "Android";
        android.overridden = compression.OverrideAndroid;
        android.maxTextureSize = compression.MaxTextureSize;
        android.textureCompression = compression.TextureCompression;
        android.compressionQuality = compression.CompressionQuality;
        android.crunchedCompression = compression.CrunchedCompression;
        android.format = compression.OverrideAndroid
            ? compression.AndroidFormat
            : TextureImporterFormat.Automatic;
        importer.SetPlatformTextureSettings(android);
    }

    static int ApplyAtlasCompressionSettings(string dstRootAbs, CompressionSettings compression)
    {
        int updated = 0;
        if (string.IsNullOrWhiteSpace(dstRootAbs) || !Directory.Exists(dstRootAbs))
            return 0;

        foreach (string color in ValidColors)
        {
            string colorFolderAbs = Path.Combine(dstRootAbs, color);
            if (!Directory.Exists(colorFolderAbs))
                continue;

            foreach (string atlasFile in Directory.GetFiles(colorFolderAbs, "*.spriteatlasv2"))
            {
                if (!TryToAssetsRelativePath(atlasFile, out string assetPath))
                    continue;

                var importer = AssetImporter.GetAtPath(assetPath) as SpriteAtlasImporter;
                if (importer == null)
                    continue;

                // SpriteAtlasTextureSettings 公开属性不含压缩字段，需通过 SerializedObject 写入默认压缩。
                var so = new SerializedObject(importer);
                var textureSettings = so.FindProperty("m_TextureSettings");
                if (textureSettings != null)
                {
                    textureSettings.FindPropertyRelative("textureCompression").intValue = (int)compression.TextureCompression;
                    textureSettings.FindPropertyRelative("maxTextureSize").intValue = compression.MaxTextureSize;
                    textureSettings.FindPropertyRelative("compressionQuality").intValue = compression.CompressionQuality;
                    textureSettings.FindPropertyRelative("crunchedCompression").boolValue = compression.CrunchedCompression;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                var android = importer.GetPlatformSettings("Android");
                android.name = "Android";
                android.overridden = compression.OverrideAndroid;
                android.maxTextureSize = compression.MaxTextureSize;
                android.textureCompression = compression.TextureCompression;
                android.compressionQuality = compression.CompressionQuality;
                android.crunchedCompression = compression.CrunchedCompression;
                android.format = compression.OverrideAndroid
                    ? compression.AndroidFormat
                    : TextureImporterFormat.Automatic;
                importer.SetPlatformSettings(android);

                importer.SaveAndReimport();
                updated++;
                Debug.Log($"[XSortsNew导入-图集压缩] {assetPath} | {compression.ToSummaryLine()}");
            }
        }

        return updated;
    }

    public static ClearResult ClearColorFolderImages(string dstRootAbs)
    {
        var result = new ClearResult { DstRoot = dstRootAbs };

        if (string.IsNullOrWhiteSpace(dstRootAbs) || !Directory.Exists(dstRootAbs))
        {
            result.ErrorMessage = "目标文件夹不存在。";
            return result;
        }

        foreach (string color in ValidColors)
        {
            string colorFolderAbs = Path.Combine(dstRootAbs, color);
            if (!Directory.Exists(colorFolderAbs))
                continue;

            foreach (string file in Directory.GetFiles(colorFolderAbs))
            {
                string ext = Path.GetExtension(file);
                if (!IsImageExtension(ext))
                    continue;

                string fileName = Path.GetFileName(file);
                if (IsPreservedImage(fileName))
                    continue;

                string logPath = $"{color}/{fileName}";
                File.Delete(file);

                string metaPath = file + ".meta";
                if (File.Exists(metaPath))
                    File.Delete(metaPath);

                result.Deleted.Add(logPath);
                Debug.Log($"[XSortsNew清除] {logPath}");
            }
        }

        AssetDatabase.Refresh();
        result.Success = true;
        Debug.Log($"[XSortsNew清除] 完成，共删除 {result.Deleted.Count} 个图片文件。");
        return result;
    }

    public static string BuildImportSummary(ImportResult result)
    {
        var summary = new StringBuilder();
        summary.AppendLine($"源目录: {result.SrcFolder}");
        summary.AppendLine($"目标目录: {result.DstRoot}");
        summary.AppendLine($"Pixels Per Unit: {result.PixelsPerUnit}");
        summary.AppendLine($"压缩设置: {result.Compression.ToSummaryLine()}");
        summary.AppendLine($"新增: {result.Added.Count}");
        summary.AppendLine($"替换: {result.Replaced.Count}");
        summary.AppendLine($"跳过: {result.Skipped.Count}");
        summary.AppendLine($"新建颜色文件夹: {result.CreatedColorFolders.Count}");
        summary.AppendLine($"已同步图集: {result.AtlasUpdatedCount}");

        if (result.CreatedColorFolders.Count > 0)
        {
            summary.AppendLine();
            summary.AppendLine("--- 新建颜色文件夹 ---");
            foreach (string color in result.CreatedColorFolders)
                summary.AppendLine(color);
        }

        if (result.Added.Count > 0)
        {
            summary.AppendLine();
            summary.AppendLine("--- 新增 ---");
            foreach (string item in result.Added)
                summary.AppendLine(item);
        }

        if (result.Replaced.Count > 0)
        {
            summary.AppendLine();
            summary.AppendLine("--- 替换 ---");
            foreach (string item in result.Replaced)
                summary.AppendLine(item);
        }

        if (result.Skipped.Count > 0)
        {
            summary.AppendLine();
            summary.AppendLine("--- 跳过 ---");
            foreach (string item in result.Skipped)
                summary.AppendLine(item);
        }

        return summary.ToString();
    }

    public static string BuildClearSummary(ClearResult result)
    {
        var summary = new StringBuilder();
        summary.AppendLine($"目标目录: {result.DstRoot}");
        summary.AppendLine($"删除: {result.Deleted.Count}");

        if (result.Deleted.Count > 0)
        {
            summary.AppendLine();
            summary.AppendLine("--- 已删除 ---");
            foreach (string item in result.Deleted)
                summary.AppendLine(item);
        }

        return summary.ToString();
    }

    public static bool TryParseSkinName(string skinName, out string color, out string type, out int number)
    {
        color = "";
        type = "";
        number = 0;

        if (string.IsNullOrEmpty(skinName))
            return false;

        string[] parts = skinName.Split('_');
        if (parts.Length != 3 || !int.TryParse(parts[2], out number))
            return false;

        color = parts[0].ToLowerInvariant();
        type = parts[1];
        return !string.IsNullOrEmpty(color) && !string.IsNullOrEmpty(type);
    }

    public static bool IsPreservedImage(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        return PreservedImageNames.Contains(nameWithoutExt);
    }

    public static bool IsImageExtension(string ext)
    {
        if (string.IsNullOrEmpty(ext))
            return false;

        foreach (string imageExt in ImageExtensions)
        {
            if (ext.Equals(imageExt, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool IsValidMaxTextureSize(int size)
    {
        if (size < 32 || size > 8192)
            return false;
        return (size & (size - 1)) == 0;
    }

    public static string ToAbsoluteProjectPath(string assetsRelativePath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.GetFullPath(Path.Combine(projectRoot, assetsRelativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    public static bool TryToAssetsRelativePath(string absolutePath, out string assetsRelativePath)
    {
        assetsRelativePath = "";
        if (string.IsNullOrWhiteSpace(absolutePath))
            return false;

        string assetsRoot = Path.GetFullPath(Application.dataPath);
        string fullPath = Path.GetFullPath(absolutePath);
        if (!fullPath.StartsWith(assetsRoot, System.StringComparison.OrdinalIgnoreCase))
            return false;

        string relative = fullPath.Substring(assetsRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        assetsRelativePath = "Assets/" + relative.Replace(Path.DirectorySeparatorChar, '/');
        return true;
    }
}
#endif
