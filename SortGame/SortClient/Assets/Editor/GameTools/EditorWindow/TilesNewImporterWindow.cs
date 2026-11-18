#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class TilesNewImporterWindow : EditorWindow
{
    const string PrefSrcFolder = "TilesNewImporter_SrcFolder";
    const string PrefDstFolder = "TilesNewImporter_DstFolder";
    const string PrefPixelsPerUnit = "TilesNewImporter_PixelsPerUnit";
    const string PrefTextureCompression = "TilesNewImporter_TextureCompression";
    const string PrefMaxTextureSize = "TilesNewImporter_MaxTextureSize";
    const string PrefCompressionQuality = "TilesNewImporter_CompressionQuality";
    const string PrefCrunchedCompression = "TilesNewImporter_CrunchedCompression";
    const string PrefOverrideAndroid = "TilesNewImporter_OverrideAndroid";
    const string PrefAndroidFormat = "TilesNewImporter_AndroidFormat";

    static readonly int[] MaxSizeOptions = { 256, 512, 1024, 2048, 4096 };
    static readonly GUIContent[] MaxSizeLabels =
    {
        new GUIContent("256"),
        new GUIContent("512"),
        new GUIContent("1024"),
        new GUIContent("2048"),
        new GUIContent("4096"),
    };

    static readonly TextureImporterFormat[] AndroidFormatOptions =
    {
        TextureImporterFormat.Automatic,
        TextureImporterFormat.ASTC_4x4,
        TextureImporterFormat.ASTC_5x5,
        TextureImporterFormat.ASTC_6x6,
        TextureImporterFormat.ASTC_8x8,
        TextureImporterFormat.ETC2_RGBA8,
        TextureImporterFormat.RGBA32,
    };

    static readonly GUIContent[] AndroidFormatLabels =
    {
        new GUIContent("Automatic（自动）"),
        new GUIContent("ASTC 4x4（高质量）"),
        new GUIContent("ASTC 5x5"),
        new GUIContent("ASTC 6x6（推荐）"),
        new GUIContent("ASTC 8x8（更小）"),
        new GUIContent("ETC2 RGBA8"),
        new GUIContent("RGBA32（不压缩）"),
    };

    string srcFolder = "";
    string dstFolder = "";
    float pixelsPerUnit = TilesNewImporterTool.DefaultPixelsPerUnit;
    TilesNewImporterTool.CompressionSettings compression = TilesNewImporterTool.CompressionSettings.Default;
    Vector2 scrollPos;

    public static void ShowWindow()
    {
        var window = GetWindow<TilesNewImporterWindow>("TilesNew 导入");
        window.minSize = new Vector2(520, 420);
        window.Show();
    }

    void OnEnable()
    {
        srcFolder = EditorPrefs.GetString(PrefSrcFolder, "");
        dstFolder = EditorPrefs.GetString(PrefDstFolder, TilesNewImporterTool.GetDefaultDstFolderAbs());
        if (string.IsNullOrWhiteSpace(dstFolder))
            dstFolder = TilesNewImporterTool.GetDefaultDstFolderAbs();
        pixelsPerUnit = EditorPrefs.GetFloat(PrefPixelsPerUnit, TilesNewImporterTool.DefaultPixelsPerUnit);

        var defaults = TilesNewImporterTool.CompressionSettings.Default;
        compression.TextureCompression = (TextureImporterCompression)EditorPrefs.GetInt(
            PrefTextureCompression, (int)defaults.TextureCompression);
        compression.MaxTextureSize = EditorPrefs.GetInt(PrefMaxTextureSize, defaults.MaxTextureSize);
        compression.CompressionQuality = EditorPrefs.GetInt(PrefCompressionQuality, defaults.CompressionQuality);
        compression.CrunchedCompression = EditorPrefs.GetBool(PrefCrunchedCompression, defaults.CrunchedCompression);
        compression.OverrideAndroid = EditorPrefs.GetBool(PrefOverrideAndroid, defaults.OverrideAndroid);
        compression.AndroidFormat = (TextureImporterFormat)EditorPrefs.GetInt(
            PrefAndroidFormat, (int)defaults.AndroidFormat);
    }

    void OnDisable()
    {
        SavePrefs();
    }

    void SavePrefs()
    {
        EditorPrefs.SetString(PrefSrcFolder, srcFolder ?? "");
        EditorPrefs.SetString(PrefDstFolder, dstFolder ?? "");
        EditorPrefs.SetFloat(PrefPixelsPerUnit, pixelsPerUnit);
        EditorPrefs.SetInt(PrefTextureCompression, (int)compression.TextureCompression);
        EditorPrefs.SetInt(PrefMaxTextureSize, compression.MaxTextureSize);
        EditorPrefs.SetInt(PrefCompressionQuality, compression.CompressionQuality);
        EditorPrefs.SetBool(PrefCrunchedCompression, compression.CrunchedCompression);
        EditorPrefs.SetBool(PrefOverrideAndroid, compression.OverrideAndroid);
        EditorPrefs.SetInt(PrefAndroidFormat, (int)compression.AndroidFormat);
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("TilesNew 贴图导入工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        DrawFolderField("源文件夹", ref srcFolder, "选择 TilesNew 源文件夹", true);
        DrawFolderField("目标文件夹", ref dstFolder, "选择 TilesNew 目标文件夹", false);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Pixels Per Unit");
        pixelsPerUnit = EditorGUILayout.FloatField(pixelsPerUnit);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("花色纹理压缩（导入后 / 打包图集）", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        compression.TextureCompression = (TextureImporterCompression)EditorGUILayout.EnumPopup(
            "压缩模式", compression.TextureCompression);

        int maxSizeIndex = System.Array.IndexOf(MaxSizeOptions, compression.MaxTextureSize);
        if (maxSizeIndex < 0)
            maxSizeIndex = System.Array.IndexOf(MaxSizeOptions, 1024);
        maxSizeIndex = EditorGUILayout.Popup(new GUIContent("Max Texture Size"), maxSizeIndex, MaxSizeLabels);
        compression.MaxTextureSize = MaxSizeOptions[Mathf.Clamp(maxSizeIndex, 0, MaxSizeOptions.Length - 1)];

        compression.CompressionQuality = EditorGUILayout.IntSlider("压缩质量", compression.CompressionQuality, 0, 100);
        compression.CrunchedCompression = EditorGUILayout.Toggle("Crunch Compression", compression.CrunchedCompression);

        compression.OverrideAndroid = EditorGUILayout.Toggle("覆盖 Android 格式", compression.OverrideAndroid);
        using (new EditorGUI.DisabledScope(!compression.OverrideAndroid))
        {
            int formatIndex = System.Array.IndexOf(AndroidFormatOptions, compression.AndroidFormat);
            if (formatIndex < 0)
                formatIndex = System.Array.IndexOf(AndroidFormatOptions, TextureImporterFormat.ASTC_6x6);
            formatIndex = EditorGUILayout.Popup(new GUIContent("Android Format"), formatIndex, AndroidFormatLabels);
            compression.AndroidFormat = AndroidFormatOptions[Mathf.Clamp(formatIndex, 0, AndroidFormatOptions.Length - 1)];
        }

        EditorGUILayout.Space(12);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("导入", GUILayout.Width(120), GUILayout.Height(28)))
            ExecuteImport();

        GUILayout.Space(12);

        if (GUILayout.Button("仅应用压缩", GUILayout.Width(120), GUILayout.Height(28)))
            ExecuteApplyCompressionOnly();

        GUILayout.Space(12);

        var prevColor = GUI.color;
        GUI.color = new Color(1f, 0.55f, 0.55f);
        if (GUILayout.Button("一键清除图片", GUILayout.Width(120), GUILayout.Height(28)))
            ExecuteClear();
        GUI.color = prevColor;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(12);
        EditorGUILayout.HelpBox(
            "文件名格式：颜色_类型_编号.png（如 black_a_1.png）\n" +
            "导入时会按颜色放入对应子文件夹，缺失的颜色文件夹会自动创建。\n" +
            "导入后贴图会设为 Sprite，并应用上方的 Pixels Per Unit 与压缩设置。\n" +
            "压缩设置会同时写入花色 PNG 与各色 SpriteAtlas，保证 Android 打包后的图集格式与此处一致。\n" +
            "「仅应用压缩」可在不重新导入的情况下，同步当前压缩设置到目标目录已有花色图与图集。\n" +
            "一键清除仅删除 11 个标准颜色文件夹内的图片，图集（.spriteatlasv2）及 tile_qizi、tile_qizi2 会保留。",
            MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    void DrawFolderField(string label, ref string folderPath, string panelTitle, bool allowEmptyDefault)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        folderPath = EditorGUILayout.TextField(folderPath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string startDir = string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)
                ? (allowEmptyDefault ? "" : TilesNewImporterTool.GetDefaultDstFolderAbs())
                : folderPath;
            string selected = EditorUtility.OpenFolderPanel(panelTitle, startDir, "");
            if (!string.IsNullOrEmpty(selected))
                folderPath = selected;
        }
        EditorGUILayout.EndHorizontal();
    }

    TilesNewImporterTool.CompressionSettings GetCurrentCompression() => compression;

    void ExecuteImport()
    {
        SavePrefs();

        if (string.IsNullOrWhiteSpace(srcFolder) || !Directory.Exists(srcFolder))
        {
            EditorUtility.DisplayDialog("导入 TilesNew", "请先选择有效的源文件夹。", "确定");
            return;
        }

        if (string.IsNullOrWhiteSpace(dstFolder))
        {
            EditorUtility.DisplayDialog("导入 TilesNew", "请先选择目标文件夹。", "确定");
            return;
        }

        if (pixelsPerUnit <= 0f)
        {
            EditorUtility.DisplayDialog("导入 TilesNew", "Pixels Per Unit 必须大于 0。", "确定");
            return;
        }

        if (!TilesNewImporterTool.IsValidMaxTextureSize(compression.MaxTextureSize))
        {
            EditorUtility.DisplayDialog("导入 TilesNew", "Max Texture Size 必须是 32~8192 之间的 2 的幂。", "确定");
            return;
        }

        var result = TilesNewImporterTool.Import(srcFolder, dstFolder, pixelsPerUnit, GetCurrentCompression());
        if (!result.Success)
        {
            EditorUtility.DisplayDialog("导入 TilesNew", result.ErrorMessage, "确定");
            return;
        }

        EditorUtility.DisplayDialog("导入 TilesNew", TilesNewImporterTool.BuildImportSummary(result), "确定");
    }

    void ExecuteApplyCompressionOnly()
    {
        SavePrefs();

        if (string.IsNullOrWhiteSpace(dstFolder) || !Directory.Exists(dstFolder))
        {
            EditorUtility.DisplayDialog("应用压缩", "请先选择有效的目标文件夹。", "确定");
            return;
        }

        if (!TilesNewImporterTool.IsValidMaxTextureSize(compression.MaxTextureSize))
        {
            EditorUtility.DisplayDialog("应用压缩", "Max Texture Size 必须是 32~8192 之间的 2 的幂。", "确定");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "应用压缩",
            $"将把当前压缩设置同步到：\n{dstFolder}\n\n" +
            $"设置：{compression.ToSummaryLine()}\n\n" +
            "会修改各色花色 PNG 与 SpriteAtlas（不改 tile_qizi），是否继续？",
            "应用",
            "取消");
        if (!confirmed)
            return;

        var result = TilesNewImporterTool.ApplyCompressionOnly(dstFolder, GetCurrentCompression());
        if (!result.Success)
        {
            EditorUtility.DisplayDialog("应用压缩", result.ErrorMessage, "确定");
            return;
        }

        EditorUtility.DisplayDialog(
            "应用压缩",
            $"花色图已更新: {result.Replaced.Count}\n图集已更新: {result.AtlasUpdatedCount}\n\n{result.Compression.ToSummaryLine()}",
            "确定");
    }

    void ExecuteClear()
    {
        SavePrefs();

        if (string.IsNullOrWhiteSpace(dstFolder) || !Directory.Exists(dstFolder))
        {
            EditorUtility.DisplayDialog("清除 TilesNew 图片", "请先选择有效的目标文件夹。", "确定");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "清除 TilesNew 图片",
            $"目标目录：\n{dstFolder}\n\n将删除 11 个标准颜色文件夹内的图片，图集及 tile_qizi、tile_qizi2 会保留。\n此操作不可撤销，是否继续？",
            "清除",
            "取消");

        if (!confirmed)
            return;

        var result = TilesNewImporterTool.ClearColorFolderImages(dstFolder);
        if (!result.Success)
        {
            EditorUtility.DisplayDialog("清除 TilesNew 图片", result.ErrorMessage, "确定");
            return;
        }

        EditorUtility.DisplayDialog("清除 TilesNew 图片", TilesNewImporterTool.BuildClearSummary(result), "确定");
    }
}
#endif
