#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LocalizationFontConfig))]
public class LocalizationFontConfigEditor : Editor
{
    const string DefaultPrefix = "Arial-Unicode-Bold-RSDF";
    const string AlternatePrefix = "MIKADO-BLACK SDF";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("按命名规则自动填充 Material Pairs（从选中文件夹）"))
        {
            AutoFillPairs((LocalizationFontConfig)target);
        }

        EditorGUILayout.HelpBox(
            "命名规则：Arial-Unicode-Bold-RSDF* → MIKADO-BLACK SDF*\n" +
            "例：Arial-Unicode-Bold-RSDF3 btn_blue → MIKADO-BLACK SDF3 btn_blue\n" +
            "先在 Project 选中含双方材质的文件夹（或 Fonts 根目录）再点按钮；已有 To 的条目不会覆盖。",
            MessageType.Info);
    }

    static void AutoFillPairs(LocalizationFontConfig config)
    {
        var folder = GetSelectedFolder();
        if (string.IsNullOrEmpty(folder))
        {
            EditorUtility.DisplayDialog("Localization Font Config",
                "请先在 Project 窗口选中一个文件夹（建议 Assets/CABRes/Fonts）。", "OK");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
        var byName = new Dictionary<string, Material>(guids.Length);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null && !byName.ContainsKey(mat.name))
                byName[mat.name] = mat;
        }

        var existing = new HashSet<int>();
        var list = new List<LocalizationFontConfig.MaterialPair>();
        if (config.materialPairs != null)
        {
            foreach (var pair in config.materialPairs)
            {
                list.Add(pair);
                if (pair.fromDefault != null)
                    existing.Add(pair.fromDefault.GetInstanceID());
            }
        }

        int added = 0;
        foreach (var kv in byName)
        {
            var name = kv.Key;
            if (!name.StartsWith(DefaultPrefix))
                continue;

            var altName = AlternatePrefix + name.Substring(DefaultPrefix.Length);
            if (!byName.TryGetValue(altName, out var altMat))
                continue;

            var fromMat = kv.Value;
            if (existing.Contains(fromMat.GetInstanceID()))
                continue;

            list.Add(new LocalizationFontConfig.MaterialPair
            {
                fromDefault = fromMat,
                toAlternate = altMat
            });
            existing.Add(fromMat.GetInstanceID());
            added++;
        }

        Undo.RecordObject(config, "Auto Fill Material Pairs");
        config.materialPairs = list.ToArray();
        config.RebuildCache();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Localization Font Config",
            $"新增 {added} 对材质映射（当前共 {list.Count} 对）。", "OK");
    }

    static string GetSelectedFolder()
    {
        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                continue;
            if (Directory.Exists(path))
                return path;
            if (File.Exists(path))
                return Path.GetDirectoryName(path)?.Replace('\\', '/');
        }

        return "Assets/CABRes/Fonts";
    }
}
#endif
