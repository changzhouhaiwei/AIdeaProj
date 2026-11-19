using System;
using System.Collections.Generic;
using System.IO;
using FishXSortPlay;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

public class XSortBaseThemePackWindow : EditorWindow
{
    const string CfgPath = "Assets/CABRes/Configs/game_tbgameXSortbasecontrolcfg.json";

    int nextId;
    string themeName = "";
    Vector2 scrollPos;
    readonly List<XSortBaseThemePack> existing = new List<XSortBaseThemePack>();
    string status = "";

    [MenuItem("游戏工具/新加主题配置", priority = 21)]
    static void Open()
    {
        var window = GetWindow<XSortBaseThemePackWindow>("新加主题配置");
        window.minSize = new Vector2(420, 360);
        window.Show();
    }

    void OnEnable()
    {
        RefreshExisting();
        nextId = NextFreeId();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "生成 Themes/{id}.asset，运行时按这个 id 加载。\n" +
            "还要在 XSortTheme 表加一行 XSortbaseId + theme，再导 Luban。\n" +
            "关卡 theme 配该 id（如 2 或 0|1|2）。空字段用预制体默认。",
            MessageType.Info);

        EditorGUILayout.Space(6);
        nextId = EditorGUILayout.IntField("XSortbaseId", nextId);
        themeName = EditorGUILayout.TextField("theme 名", themeName);

        EditorGUI.BeginDisabledGroup(nextId < 0 || string.IsNullOrWhiteSpace(themeName));
        if (GUILayout.Button("创建主题 Pack", GUILayout.Height(28)))
        {
            CreatePack();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(nextId < 0);
        if (GUILayout.Button("从配置表填充引用", GUILayout.Height(28)))
        {
            FillSelectedFromCfg();
        }
        EditorGUI.EndDisabledGroup();

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.HelpBox(status, MessageType.None);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("已有主题", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < existing.Count; i++)
        {
            XSortBaseThemePack pack = existing[i];
            if (pack == null)
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField($"{pack.XSortbaseId}  {pack.theme}", pack, typeof(XSortBaseThemePack), false);
            if (GUILayout.Button("选中", GUILayout.Width(48)))
            {
                nextId = pack.XSortbaseId;
                themeName = pack.theme ?? "";
                Selection.activeObject = pack;
                EditorGUIUtility.PingObject(pack);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("刷新列表"))
        {
            RefreshExisting();
            nextId = NextFreeId();
        }
    }

    void CreatePack()
    {
        string folder = ThemeUiApplier.PackFolder;
        string path = $"{folder}/{nextId}.asset";
        if (AssetDatabase.LoadAssetAtPath<XSortBaseThemePack>(path) != null)
        {
            status = $"已存在 {path}";
            return;
        }

        if (!AssetDatabase.IsValidFolder(folder))
        {
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                status = $"无法创建目录 {folder}";
                return;
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        var pack = ScriptableObject.CreateInstance<XSortBaseThemePack>();
        pack.XSortbaseId = nextId;
        pack.theme = themeName.Trim();
        AssetDatabase.CreateAsset(pack, path);

        int filled = FillPackFromCfg(pack, out List<string> missing);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = pack;
        EditorGUIUtility.PingObject(pack);
        status = filled > 0
            ? $"已创建 {path}，从配置表填了 {filled} 个引用。" + MissingSuffix(missing)
            : $"已创建 {path}。表里没有 XSortbaseId={pack.XSortbaseId} 或资源名都为空。";
        RefreshExisting();
        nextId = NextFreeId();
        themeName = "";
        GUI.FocusControl(null);
    }

    void FillSelectedFromCfg()
    {
        RefreshExisting();
        XSortBaseThemePack pack = FindPack(nextId);
        if (pack == null)
        {
            status = $"没有 Themes/{nextId}.asset，先创建或选一个已有 id。";
            return;
        }

        if (!TryLoadCfg(out Dictionary<int, XSortBaseControlCfgRow> map, out string error))
        {
            status = error;
            return;
        }

        if (!map.TryGetValue(nextId, out XSortBaseControlCfgRow row))
        {
            status = $"配置表没有 XSortbaseId={nextId}。";
            return;
        }

        var missing = new List<string>();
        int filled = FillPackFromRow(pack, row, missing);
        AssetDatabase.SaveAssets();
        Selection.activeObject = pack;
        EditorGUIUtility.PingObject(pack);
        status = filled > 0
            ? $"已填充 XSortbaseId={nextId}，{filled} 个引用。" + MissingSuffix(missing)
            : $"XSortbaseId={nextId} 没有新引用可填。" + MissingSuffix(missing);
    }

    XSortBaseThemePack FindPack(int XSortbaseId)
    {
        for (int i = 0; i < existing.Count; i++)
        {
            if (existing[i] != null && existing[i].XSortbaseId == XSortbaseId)
            {
                return existing[i];
            }
        }

        return AssetDatabase.LoadAssetAtPath<XSortBaseThemePack>($"{ThemeUiApplier.PackFolder}/{XSortbaseId}.asset");
    }

    int FillPackFromCfg(XSortBaseThemePack pack, out List<string> missing)
    {
        missing = new List<string>();
        if (!TryLoadCfg(out Dictionary<int, XSortBaseControlCfgRow> map, out _))
        {
            return 0;
        }

        if (!map.TryGetValue(pack.XSortbaseId, out XSortBaseControlCfgRow row))
        {
            return 0;
        }

        return FillPackFromRow(pack, row, missing);
    }

    static int FillPackFromRow(XSortBaseThemePack pack, XSortBaseControlCfgRow row, List<string> missing)
    {
        var so = new SerializedObject(pack);
        int n = 0;
        n += Assign<Sprite>(so, "XSortBase", row.XSortBase_UI, missing);
        n += Assign<Sprite>(so, "unknown", row.Unknown_ui, missing);
        n += Assign<AudioClip>(so, "collectSound", row.Effect_collect_sound, missing);
        n += Assign<GameObject>(so, "trail", row.trail_effect, missing);
        n += Assign<Material>(so, "brokenMat", row.XSort_baroken_mat, missing);
        n += Assign<GameObject>(so, "brokenEffect", row.XSort_baroken_effect, missing);
        n += Assign<AudioClip>(so, "brokenSound", row.XSort_baroken_sound, missing);
        n += Assign<GameObject>(so, "combo5Effect", row.combo5_effect, missing);
        n += Assign<AudioClip>(so, "combo5Sound", row.combo5_sound, missing);
        n += Assign<Sprite>(so, "column", row.column_ui, missing);
        n += Assign<Sprite>(so, "slot", row.slot_ui, missing);
        n += Assign<GameObject>(so, "winEffect", row.win_effect, missing);
        n += Assign<SkeletonDataAsset>(so, "winTitle", row.win_title_effect, missing);
        n += Assign<SkeletonDataAsset>(so, "winGift", row.win_gift_effect, missing);
        n += Assign<GameObject>(so, "winCelebrateEffect", row.win_Celebrate_effect, missing);
        n += Assign<GameObject>(so, "failEffect", row.Fail_effect, missing);
        n += Assign<SkeletonDataAsset>(so, "failClock", row.Fail_clock, missing);
        n += Assign<AudioClip>(so, "failSound", row.Fail_sound, missing);
        n += Assign<Sprite>(so, "mainBg", row.Main_bg_UI, missing);
        n += Assign<Sprite>(so, "button", row.button_effect, missing);
        n += Assign<GameObject>(so, "buttonClickVfx", row.button_click_vfx, missing);
        n += Assign<AudioClip>(so, "buttonClickSound", row.button_click_sound, missing);
        n += Assign<Sprite>(so, "levelBg", row.Level_bg_UI, missing);
        if (n > 0)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pack);
        }

        return n;
    }

    static int Assign<T>(SerializedObject so, string field, string assetName, List<string> missing)
        where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(assetName))
        {
            return 0;
        }

        T asset = FindByName<T>(assetName);
        if (asset == null)
        {
            if (!missing.Contains(assetName))
            {
                missing.Add(assetName);
            }

            return 0;
        }

        SerializedProperty prop = so.FindProperty(field);
        if (prop == null || prop.objectReferenceValue == asset)
        {
            return 0;
        }

        prop.objectReferenceValue = asset;
        return 1;
    }

    static T FindByName<T>(string assetName) where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets(assetName);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.Equals(Path.GetFileNameWithoutExtension(path), assetName, StringComparison.Ordinal))
            {
                continue;
            }

            if (typeof(T) == typeof(Sprite))
            {
                UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int j = 0; j < all.Length; j++)
                {
                    if (all[j] is Sprite sprite)
                    {
                        return sprite as T;
                    }
                }
            }

            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }
        }

        return null;
    }

    static bool TryLoadCfg(out Dictionary<int, XSortBaseControlCfgRow> map, out string error)
    {
        map = new Dictionary<int, XSortBaseControlCfgRow>();
        error = null;
        string abs = Path.Combine(Application.dataPath, "CABRes/Configs/game_tbgameXSortbasecontrolcfg.json");
        if (!File.Exists(abs))
        {
            error = $"找不到配置表 {CfgPath}";
            return false;
        }

        string json = File.ReadAllText(abs);
        string trimmed = json.TrimStart();
        if (!trimmed.StartsWith("["))
        {
            error = "配置表不是 JSON 数组";
            return false;
        }

        XSortBaseControlCfgFile file = JsonUtility.FromJson<XSortBaseControlCfgFile>("{\"items\":" + json + "}");
        if (file?.items == null)
        {
            error = "配置表解析失败";
            return false;
        }

        for (int i = 0; i < file.items.Length; i++)
        {
            XSortBaseControlCfgRow row = file.items[i];
            map[row.XSortbaseId] = row;
        }

        return true;
    }

    static string MissingSuffix(List<string> missing)
    {
        if (missing == null || missing.Count == 0)
        {
            return "";
        }

        int show = Mathf.Min(missing.Count, 8);
        string names = string.Join(", ", missing.GetRange(0, show));
        if (missing.Count > show)
        {
            names += $" ...(+{missing.Count - show})";
        }

        return " 未找到: " + names;
    }

    void RefreshExisting()
    {
        existing.Clear();
        if (!AssetDatabase.IsValidFolder(ThemeUiApplier.PackFolder))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:XSortBaseThemePack", new[] { ThemeUiApplier.PackFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            XSortBaseThemePack pack = AssetDatabase.LoadAssetAtPath<XSortBaseThemePack>(path);
            if (pack != null)
            {
                existing.Add(pack);
            }
        }

        existing.Sort((a, b) => a.XSortbaseId.CompareTo(b.XSortbaseId));
    }

    int NextFreeId()
    {
        int max = -1;
        for (int i = 0; i < existing.Count; i++)
        {
            if (existing[i] != null && existing[i].XSortbaseId > max)
            {
                max = existing[i].XSortbaseId;
            }
        }

        return max + 1;
    }

    [Serializable]
    class XSortBaseControlCfgFile
    {
        public XSortBaseControlCfgRow[] items;
    }

    [Serializable]
    class XSortBaseControlCfgRow
    {
        public int XSortbaseId;
        public string theme;
        public string XSortBase_UI;
        public string Unknown_ui;
        public string Effect_collect_sound;
        public string trail_effect;
        public string XSort_baroken_mat;
        public string XSort_baroken_effect;
        public string XSort_baroken_sound;
        public string combo5_effect;
        public string combo5_sound;
        public string column_ui;
        public string slot_ui;
        public string win_effect;
        public string win_title_effect;
        public string win_gift_effect;
        public string win_Celebrate_effect;
        public string Fail_effect;
        public string Fail_clock;
        public string Fail_sound;
        public string Main_bg_UI;
        public string button_effect;
        public string button_click_vfx;
        public string button_click_sound;
        public string Level_bg_UI;
    }
}
