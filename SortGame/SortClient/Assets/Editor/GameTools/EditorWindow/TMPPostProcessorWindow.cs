using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Luban.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class TMPPostProcessorWindow : OdinEditorWindow
{
    [MenuItem("游戏工具/TMP字体图集优化工具", priority = 100)]
    private static void ShowTMPPostProcessorWindow()
    {
        var tmpWindow = GetWindow<TMPPostProcessorWindow>();
        tmpWindow.Show();
    }

    public class TMPPostProcessorSetting
    {
        [LabelText("TMPAsset图集资产")] public TMP_FontAsset TMPFontAsset;
        [LabelText("最大分辨率")] public int MaxSize = 4096;
        [LabelText("压缩格式")] public TextureImporterFormat TextureImporterFormat = TextureImporterFormat.ASTC_6x6;
        [LabelText("压缩质量")] public TextureCompressionQuality TextureCompressionQuality = TextureCompressionQuality.Best;
    }

    [LabelText("要后处理的TMP字体文件")]
    public List<TMPPostProcessorSetting> TMPPostProcessorSettings = new List<TMPPostProcessorSetting>();

    [Button("一键优化", ButtonSizes.Medium)]
    public void Execute()
    {
        int changed = 0;
        foreach (var setting in TMPPostProcessorSettings)
        {
            if (setting?.TMPFontAsset == null)
            {
                continue;
            }

            if (TMPFontAtlasCompressor.Process(
                    setting.TMPFontAsset,
                    setting.MaxSize,
                    setting.TextureImporterFormat,
                    setting.TextureCompressionQuality))
            {
                changed++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TMPFontAtlasCompressor] window processed {changed} fonts.");
    }

    [Button("Arial字体设为静态", ButtonSizes.Medium)]
    public void SetArialStatic()
    {
        TMPFontAtlasCompressor.SetArialFontStatic();
    }
}

/// <summary>
/// 将 TMP SDF 内嵌 Alpha8 图集抽成独立 PNG，Android/iPhone ASTC 6x6，并重绑 Material Preset。
/// </summary>
public static class TMPFontAtlasCompressor
{
    public const string FontsFolder = "Assets/CABRes/Fonts";
    public const string ArialFontAssetPath = "Assets/CABRes/Fonts/Arial-Unicode-Bold-RSDF.asset";
    public const string ArialSourceTtfPath = "Assets/Font/Arial-Unicode-Bold.ttf";
    public const string ArialAtlasPngPath = "Assets/CABRes/Fonts/Arial-Unicode-Bold-RSDF.png";

    public static readonly string[] DefaultFontPaths =
    {
        "Assets/CABRes/Fonts/MIKADO-BLACK SDF.asset",
        ArialFontAssetPath,
    };

    [MenuItem("游戏工具/Arial字体设为静态", priority = 102)]
    public static void SetArialFontStaticMenu()
    {
        if (SetArialFontStatic())
        {
            EditorUtility.DisplayDialog("Arial 字体", "已设为静态字体，已覆盖原图集 PNG，并断开源 TTF 引用。", "OK");
        }
    }

    [MenuItem("游戏工具/Arial字体设为动态", priority = 103)]
    public static void SetArialFontDynamicMenu()
    {
        if (BindArialSourceFontAndSetDynamic())
        {
            EditorUtility.DisplayDialog("Arial 字体", "已设为动态字体。图集已改为未压缩 Alpha8，可用 Font Asset Creator 重新 Generate 后 Save。", "OK");
        }
    }

    [MenuItem("游戏工具/收集字体", priority = 104)]
    public static void CollectFontMenu()
    {
        var config = AssetDatabase.LoadAssetAtPath<LubanExportConfig>("Assets/Luban.asset");
        if (config == null)
        {
            Debug.LogError("[TMPFontAtlasCompressor] 找不到 Assets/Luban.asset，无法生成配表。");
            EditorUtility.DisplayDialog("收集字体", "找不到 Assets/Luban.asset。", "OK");
            return;
        }

        Debug.Log("[TMPFontAtlasCompressor] 开始执行 Luban 生成（完成后将导出语言包并收集字体）。");
        config.Gen();
    }

    /// <summary>
    /// 打包专用：图集已是原 png 时不 Blit 重编码，只切 Static + ASTC 并断开 TTF。
    /// 图集仍嵌在 asset 里时退回 <see cref="SetArialFontStatic"/>。
    /// </summary>
    public static bool PrepareArialFontForBuild()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ArialFontAssetPath);
        if (font == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 找不到字体: {ArialFontAssetPath}");
            return false;
        }

        Texture srcAtlas = font.atlasTexture;
        if (srcAtlas == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 无图集: {ArialFontAssetPath}");
            return false;
        }

        string atlasPath = AssetDatabase.GetAssetPath(srcAtlas);
        bool atlasIsCanonicalPng = string.Equals(atlasPath, ArialAtlasPngPath, StringComparison.OrdinalIgnoreCase);
        if (!atlasIsCanonicalPng)
        {
            Debug.LogWarning(
                $"[TMPFontAtlasCompressor] 打包前图集不是 {ArialAtlasPngPath}（当前: {atlasPath}），退回完整静态导出。");
            return SetArialFontStatic();
        }

        ApplyImporterSettings(
            ArialAtlasPngPath,
            4096,
            TextureImporterFormat.ASTC_6x6,
            TextureCompressionQuality.Best);

        var png = AssetDatabase.LoadAssetAtPath<Texture2D>(ArialAtlasPngPath);
        if (png == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 加载 PNG 失败: {ArialAtlasPngPath}");
            return false;
        }

        font.atlasTextures = new Texture2D[] { png };
        if (font.material != null)
        {
            font.material.mainTexture = png;
        }

        SetStaticAndUnbindSourceFont(font);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TMPFontAtlasCompressor] 打包前 {font.name} 已临时设为 Static（未重编码 PNG），已断开源 TTF。");
        return true;
    }

    /// <summary>打包结束后还原：动态 + 绑定源 TTF + 开发用 Alpha8 图集。</summary>
    public static bool RestoreArialFontAfterBuild()
    {
        return BindArialSourceFontAndSetDynamic();
    }

    /// <summary>
    /// 从 LocalizationConfig.csv 收集字符写入 Arial 动态图集（保持 Dynamic，不切静态）。
    /// </summary>
    public static bool CollectArialCharactersFromLocalizationConfig()
    {
        const string csvPath = "Assets/Resources/LocalizationConfig.csv";
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 找不到语言包 CSV: {csvPath}");
            return false;
        }

        if (!BindArialSourceFontAndSetDynamic())
        {
            return false;
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ArialFontAssetPath);
        if (font == null)
        {
            return false;
        }

        string characters = ExtractUniqueCharactersFromCsv(File.ReadAllText(csvPath, Encoding.UTF8));
        if (string.IsNullOrEmpty(characters))
        {
            Debug.LogWarning("[TMPFontAtlasCompressor] LocalizationConfig.csv 未解析到可收集字符。");
            return true;
        }

        int before = font.characterTable != null ? font.characterTable.Count : 0;
        bool allAdded = font.TryAddCharacters(characters, out string missing);
        int after = font.characterTable != null ? font.characterTable.Count : 0;
        int added = Math.Max(0, after - before);

        if (!WriteDynamicAtlasBackToPng(font))
        {
            return false;
        }

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(missing))
        {
            Debug.LogWarning(
                $"[TMPFontAtlasCompressor] 收集字体完成：新增 {added}，仍缺 {missing.Length} 字（图集可能已满或源字体无字形）。缺字预览: {TruncateForLog(missing, 120)}");
        }
        else
        {
            Debug.Log($"[TMPFontAtlasCompressor] 收集字体完成：新增 {added}，全部写入成功（allAdded={allAdded}）。");
        }

        return true;
    }

    static string ExtractUniqueCharactersFromCsv(string csv)
    {
        if (string.IsNullOrEmpty(csv))
        {
            return string.Empty;
        }

        var set = new HashSet<char>();
        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];
            if (char.IsControl(c) || c == '\uFEFF')
            {
                continue;
            }

            set.Add(c);
        }

        if (set.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(set.Count);
        foreach (char c in set)
        {
            sb.Append(c);
        }

        return sb.ToString();
    }

    static bool WriteDynamicAtlasBackToPng(TMP_FontAsset font)
    {
        Texture srcAtlas = font.atlasTexture;
        if (srcAtlas == null)
        {
            Debug.LogError("[TMPFontAtlasCompressor] 收集后无图集可写回。");
            return false;
        }

        ExportEmbeddedAtlas(srcAtlas, ArialAtlasPngPath);
        AssetDatabase.ImportAsset(ArialAtlasPngPath, ImportAssetOptions.ForceUpdate);
        ApplyDevelopmentAtlasImporter(ArialAtlasPngPath);

        var png = AssetDatabase.LoadAssetAtPath<Texture2D>(ArialAtlasPngPath);
        if (png == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 写回 PNG 失败: {ArialAtlasPngPath}");
            return false;
        }

        string oldPath = AssetDatabase.GetAssetPath(srcAtlas);
        if (!string.Equals(oldPath, ArialAtlasPngPath, StringComparison.OrdinalIgnoreCase))
        {
            RetargetMaterials(font, ArialFontAssetPath, srcAtlas, png);
            DestroyExtraAtlasTextures(font, png);
        }

        font.atlasTextures = new Texture2D[] { png };
        if (font.material != null)
        {
            font.material.mainTexture = png;
        }

        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        return true;
    }

    static string TruncateForLog(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
        {
            return value ?? string.Empty;
        }

        return value.Substring(0, max) + "...";
    }

    public static bool SetArialFontStatic()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ArialFontAssetPath);
        if (font == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 找不到字体: {ArialFontAssetPath}");
            return false;
        }

        Texture srcAtlas = font.atlasTexture;
        if (srcAtlas == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 无图集: {ArialFontAssetPath}");
            return false;
        }

        Texture2D oldPng = AssetDatabase.LoadAssetAtPath<Texture2D>(ArialAtlasPngPath);
        ExportEmbeddedAtlas(srcAtlas, ArialAtlasPngPath);
        AssetDatabase.ImportAsset(ArialAtlasPngPath, ImportAssetOptions.ForceUpdate);
        ApplyImporterSettings(
            ArialAtlasPngPath,
            4096,
            TextureImporterFormat.ASTC_6x6,
            TextureCompressionQuality.Best);

        var png = AssetDatabase.LoadAssetAtPath<Texture2D>(ArialAtlasPngPath);
        if (png == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 覆盖 PNG 失败: {ArialAtlasPngPath}");
            return false;
        }

        RetargetMaterials(font, ArialFontAssetPath, srcAtlas, png);
        if (oldPng != null && oldPng != srcAtlas && oldPng != png)
        {
            RetargetMaterials(font, ArialFontAssetPath, oldPng, png);
        }

        DestroyExtraAtlasTextures(font, png);

        font.atlasTextures = new Texture2D[] { png };
        if (font.material != null)
        {
            font.material.mainTexture = png;
        }

        SetStaticAndUnbindSourceFont(font);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TMPFontAtlasCompressor] {font.name} 已设为 Static，已覆盖 {ArialAtlasPngPath}，已断开源 TTF 引用。");
        return true;
    }

    /// <summary>绑定 Arial TTF、设为 Dynamic，并打开图集 Read/Write 以便收集缺字。</summary>
    public static bool BindArialSourceFontAndSetDynamic()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ArialFontAssetPath);
        var source = AssetDatabase.LoadAssetAtPath<Font>(ArialSourceTtfPath);
        if (font == null || source == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 找不到字体或源 TTF: {ArialFontAssetPath} / {ArialSourceTtfPath}");
            return false;
        }

        string guid = AssetDatabase.AssetPathToGUID(ArialSourceTtfPath);
        var so = new SerializedObject(font);
        so.FindProperty("m_SourceFontFileGUID").stringValue = guid;
        so.FindProperty("m_SourceFontFile_EditorRef").objectReferenceValue = source;
        so.ApplyModifiedPropertiesWithoutUndo();
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        ApplyDevelopmentAtlasImporter(ArialAtlasPngPath);

        var png = AssetDatabase.LoadAssetAtPath<Texture2D>(ArialAtlasPngPath);
        if (png != null)
        {
            font.atlasTextures = new Texture2D[] { png };
            if (font.material != null)
            {
                font.material.mainTexture = png;
            }
        }

        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        Debug.Log($"[TMPFontAtlasCompressor] {font.name} 已设为 Dynamic，已绑定 {ArialSourceTtfPath}。");
        return true;
    }

    public static void SetStaticAndUnbindSourceFont(TMP_FontAsset font)
    {
        if (font == null)
        {
            return;
        }

        font.atlasPopulationMode = AtlasPopulationMode.Static;
        var so = new SerializedObject(font);
        var sourceProp = so.FindProperty("m_SourceFontFile");
        if (sourceProp != null)
        {
            sourceProp.objectReferenceValue = null;
        }

        var editorRef = so.FindProperty("m_SourceFontFile_EditorRef");
        if (editorRef != null)
        {
            editorRef.objectReferenceValue = null;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(font);
    }

    [MenuItem("游戏工具/压缩 CABRes 字体图集", priority = 101)]
    public static void ProcessCabResFontsMenu()
    {
        int n = ProcessCabResFonts();
        EditorUtility.DisplayDialog("TMP 图集压缩", $"已处理 {n} 个字体。请等待 Reimport 完成。", "OK");
    }

    public static int ProcessCabResFonts()
    {
        int n = 0;
        try
        {
            for (int i = 0; i < DefaultFontPaths.Length; i++)
            {
                string path = DefaultFontPaths[i];
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font == null)
                {
                    Debug.LogError($"[TMPFontAtlasCompressor] 找不到字体: {path}");
                    continue;
                }

                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayProgressBar("压缩字体图集", path, (float)i / DefaultFontPaths.Length);
                }

                if (Process(font, 4096, TextureImporterFormat.ASTC_6x6, TextureCompressionQuality.Best))
                {
                    n++;
                }
            }
        }
        finally
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.ClearProgressBar();
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TMPFontAtlasCompressor] processed {n} fonts.");
        return n;
    }

    /// <summary>供 Unity -executeMethod 批处理调用。</summary>
    public static void ProcessCabResFontsBatch()
    {
        ProcessCabResFonts();
    }

    public static bool Process(
        TMP_FontAsset font,
        int maxSize,
        TextureImporterFormat format,
        TextureCompressionQuality quality)
    {
        if (font == null)
        {
            return false;
        }

        string fontPath = AssetDatabase.GetAssetPath(font);
        if (string.IsNullOrEmpty(fontPath))
        {
            Debug.LogError("[TMPFontAtlasCompressor] 字体不在 AssetDatabase 中。");
            return false;
        }

        Texture oldAtlas = font.atlasTexture;
        if (oldAtlas == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 无图集: {fontPath}");
            return false;
        }

        string texturePath = Path.ChangeExtension(fontPath, ".png").Replace('\\', '/');
        string oldAtlasPath = AssetDatabase.GetAssetPath(oldAtlas);
        bool atlasIsSubAsset = oldAtlasPath == fontPath;
        bool atlasIsCanonicalPng = string.Equals(oldAtlasPath, texturePath, System.StringComparison.OrdinalIgnoreCase);

        // 只覆盖同路径 PNG，不另存新文件。动态收集产生的内嵌图集在出包前写回原 PNG。
        if (atlasIsSubAsset || !atlasIsCanonicalPng)
        {
            ExportEmbeddedAtlas(oldAtlas, texturePath);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
        }

        ApplyImporterSettings(texturePath, maxSize, format, quality);

        var newAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (newAtlas == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 导入 PNG 失败: {texturePath}");
            return false;
        }

        RetargetMaterials(font, fontPath, oldAtlas, newAtlas);

        if (font.atlasTextures == null || font.atlasTextures.Length == 0)
        {
            font.atlasTextures = new Texture2D[1];
        }

        font.atlasTextures[0] = newAtlas;
        if (font.material != null)
        {
            font.material.mainTexture = newAtlas;
        }

        SetStaticAndUnbindSourceFont(font);
        DestroyExtraAtlasTextures(font, newAtlas);

        if (atlasIsSubAsset && oldAtlas != newAtlas)
        {
            AssetDatabase.RemoveObjectFromAsset(oldAtlas);
            Object.DestroyImmediate(oldAtlas, true);
        }

        EditorUtility.SetDirty(font);
        Debug.Log($"[TMPFontAtlasCompressor] {font.name} -> {texturePath}, Static, {format}");
        return true;
    }

    static void ExportEmbeddedAtlas(Texture src, string texturePath)
    {
        int w = src.width;
        int h = src.height;
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        Graphics.Blit(src, rt);
        RenderTexture.active = rt;
        var readable = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        readable.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        readable.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        File.WriteAllBytes(texturePath, readable.EncodeToPNG());
        Object.DestroyImmediate(readable);
    }

    /// <summary>
    /// Font Asset Creator 的 Save 用 CopyTexture，源图集是 Alpha8。
    /// 目标必须是未压缩 Alpha8，且不能走 Android ASTC，否则会拷失败并把字图弄乱。
    /// </summary>
    static void ApplyDevelopmentAtlasImporter(string texturePath)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 无 TextureImporter: {texturePath}");
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.alphaIsTransparency = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var def = importer.GetDefaultPlatformTextureSettings();
        def.overridden = true;
        def.format = TextureImporterFormat.Alpha8;
        def.maxTextureSize = 4096;
        def.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(def);

        ClearPlatformOverride(importer, "Android");
        ClearPlatformOverride(importer, "iPhone");
        importer.SaveAndReimport();
    }

    static void ClearPlatformOverride(TextureImporter importer, string platform)
    {
        var settings = importer.GetPlatformTextureSettings(platform);
        settings.overridden = false;
        importer.SetPlatformTextureSettings(settings);
    }

    static void DestroyExtraAtlasTextures(TMP_FontAsset font, Texture keep)
    {
        if (font == null)
        {
            return;
        }

        string fontPath = AssetDatabase.GetAssetPath(font);
        var textures = font.atlasTextures;
        if (textures == null)
        {
            return;
        }

        for (int i = 0; i < textures.Length; i++)
        {
            var tex = textures[i];
            if (tex == null || tex == keep)
            {
                continue;
            }

            string path = AssetDatabase.GetAssetPath(tex);
            if (path != fontPath)
            {
                continue;
            }

            AssetDatabase.RemoveObjectFromAsset(tex);
            Object.DestroyImmediate(tex, true);
        }
    }

    static void ApplyImporterSettings(
        string texturePath,
        int maxSize,
        TextureImporterFormat format,
        TextureCompressionQuality quality)
    {
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[TMPFontAtlasCompressor] 无 TextureImporter: {texturePath}");
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.alphaIsTransparency = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.textureCompression = TextureImporterCompression.Compressed;

        ApplyPlatform(importer, "Android", maxSize, format, quality);
        ApplyPlatform(importer, "iPhone", maxSize, format, quality);
        importer.SaveAndReimport();
    }

    static void ApplyPlatform(
        TextureImporter importer,
        string platform,
        int maxSize,
        TextureImporterFormat format,
        TextureCompressionQuality quality)
    {
        var settings = importer.GetPlatformTextureSettings(platform);
        settings.overridden = true;
        settings.format = format;
        settings.maxTextureSize = maxSize;
        settings.textureCompression = TextureImporterCompression.Compressed;
        settings.crunchedCompression = false;
        settings.compressionQuality = (int)quality;
        importer.SetPlatformTextureSettings(settings);
    }

    static void RetargetMaterials(TMP_FontAsset font, string fontPath, Texture oldAtlas, Texture2D newAtlas)
    {
        int changed = 0;
        var objects = AssetDatabase.LoadAllAssetsAtPath(fontPath);
        for (int i = 0; i < objects.Length; i++)
        {
            if (RetargetMaterial(objects[i] as Material, oldAtlas, newAtlas))
            {
                changed++;
            }
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { FontsFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (RetargetMaterial(mat, oldAtlas, newAtlas))
            {
                changed++;
            }
        }

        if (font.material != null && RetargetMaterial(font.material, oldAtlas, newAtlas))
        {
            changed++;
        }

        Debug.Log($"[TMPFontAtlasCompressor] retargeted {changed} materials for {font.name}");
    }

    static bool RetargetMaterial(Material mat, Texture oldAtlas, Texture2D newAtlas)
    {
        if (mat == null || oldAtlas == null || !mat.HasProperty("_MainTex"))
        {
            return false;
        }

        if (mat.GetTexture("_MainTex") != oldAtlas)
        {
            return false;
        }

        mat.SetTexture("_MainTex", newAtlas);
        EditorUtility.SetDirty(mat);
        return true;
    }
}

/// <summary>
/// Luban.Editor 不能引用本程序集，故在此把「收集字体」挂到语言包导出成功回调。
/// </summary>
[InitializeOnLoad]
static class ArialFontCollectAfterI2Hook
{
    static ArialFontCollectAfterI2Hook()
    {
        I2LocalizationOneClickImporter.OnLanguagePackageExported += OnLanguagePackageExported;
    }

    static void OnLanguagePackageExported()
    {
        TMPFontAtlasCompressor.CollectArialCharactersFromLocalizationConfig();
    }
}
