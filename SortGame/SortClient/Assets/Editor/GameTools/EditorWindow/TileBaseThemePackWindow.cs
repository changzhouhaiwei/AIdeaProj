using System;
using System.Collections.Generic;
using System.IO;
using FishTilePlay;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

public class TileBaseThemePackWindow : EditorWindow
{
    const string CfgPath = "Assets/CABRes/Configs/game_tbgametilebasecontrolcfg.json";

    int nextId;
    string themeName = "";
    Vector2 scrollPos;
    readonly List<TileBaseThemePack> existing = new List<TileBaseThemePack>();
    string status = "";

    [MenuItem("游戏工具/新加主题配置", priority = 21)]
    static void Open()
    {
        var window = GetWindow<TileBaseThemePackWindow>("新加主题配置");
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
            "还要在 TileTheme 表加一行 tilebaseId + theme，再导 Luban。\n" +
            "关卡 theme 配该 id（如 2 或 0|1|2）。空字段用预制体默认。",
            MessageType.Info);

        EditorGUILayout.Space(6);
        nextId = EditorGUILayout.IntField("tilebaseId", nextId);
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
            TileBaseThemePack pack = existing[i];
            if (pack == null)
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField($"{pack.tilebaseId}  {pack.theme}", pack, typeof(TileBaseThemePack), false);
            if (GUILayout.Button("选中", GUILayout.Width(48)))
            {
                nextId = pack.tilebaseId;
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
        if (AssetDatabase.LoadAssetAtPath<TileBaseThemePack>(path) != null)
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

        var pack = ScriptableObject.CreateInstance<TileBaseThemePack>();
        pack.tilebaseId = nextId;
        pack.theme = themeName.Trim();
        AssetDatabase.CreateAsset(pack, path);

        int filled = FillPackFromCfg(pack, out List<string> missing);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = pack;
        EditorGUIUtility.PingObject(pack);
        status = filled > 0
            ? $"已创建 {path}，从配置表填了 {filled} 个引用。" + MissingSuffix(missing)
            : $"已创建 {path}。表里没有 tilebaseId={pack.tilebaseId} 或资源名都为空。";
        RefreshExisting();
        nextId = NextFreeId();
        themeName = "";
        GUI.FocusControl(null);
    }

    void FillSelectedFromCfg()
    {
        RefreshExisting();
        TileBaseThemePack pack = FindPack(nextId);
        if (pack == null)
        {
            status = $"没有 Themes/{nextId}.asset，先创建或选一个已有 id。";
            return;
        }

        if (!TryLoadCfg(out Dictionary<int, TileBaseControlCfgRow> map, out string error))
        {
            status = error;
            return;
        }

        if (!map.TryGetValue(nextId, out TileBaseControlCfgRow row))
        {
            status = $"配置表没有 tilebaseId={nextId}。";
            return;
        }

        var missing = new List<string>();
        int filled = FillPackFromRow(pack, row, missing);
        AssetDatabase.SaveAssets();
        Selection.activeObject = pack;
        EditorGUIUtility.PingObject(pack);
        status = filled > 0
            ? $"已填充 tilebaseId={nextId}，{filled} 个引用。" + MissingSuffix(missing)
            : $"tilebaseId={nextId} 没有新引用可填。" + MissingSuffix(missing);
    }

    TileBaseThemePack FindPack(int tilebaseId)
    {
        for (int i = 0; i < existing.Count; i++)
        {
            if (existing[i] != null && existing[i].tilebaseId == tilebaseId)
            {
                return existing[i];
            }
        }

        return AssetDatabase.LoadAssetAtPath<TileBaseThemePack>($"{ThemeUiApplier.PackFolder}/{tilebaseId}.asset");
    }

    int FillPackFromCfg(TileBaseThemePack pack, out List<string> missing)
    {
        missing = new List<string>();
        if (!TryLoadCfg(out Dictionary<int, TileBaseControlCfgRow> map, out _))
        {
            return 0;
        }

        if (!map.TryGetValue(pack.tilebaseId, out TileBaseControlCfgRow row))
        {
            return 0;
        }

        return FillPackFromRow(pack, row, missing);
    }

    static int FillPackFromRow(TileBaseThemePack pack, TileBaseControlCfgRow row, List<string> missing)
    {
        var so = new SerializedObject(pack);
        int n = 0;
        n += Assign<Sprite>(so, "tileBase", row.TileBase_UI, missing);
        n += Assign<Sprite>(so, "unknown", row.Unknown_ui, missing);
        n += Assign<AudioClip>(so, "collectSound", row.Effect_collect_sound, missing);
        n += Assign<GameObject>(so, "trail", row.trail_effect, missing);
        n += Assign<Material>(so, "brokenMat", row.tile_baroken_mat, missing);
        n += Assign<GameObject>(so, "brokenEffect", row.tile_baroken_effect, missing);
        n += Assign<AudioClip>(so, "brokenSound", row.tile_baroken_sound, missing);
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

    static bool TryLoadCfg(out Dictionary<int, TileBaseControlCfgRow> map, out string error)
    {
        map = new Dictionary<int, TileBaseControlCfgRow>();
        error = null;
        string abs = Path.Combine(Application.dataPath, "CABRes/Configs/game_tbgametilebasecontrolcfg.json");
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

        TileBaseControlCfgFile file = JsonUtility.FromJson<TileBaseControlCfgFile>("{\"items\":" + json + "}");
        if (file?.items == null)
        {
            error = "配置表解析失败";
            return false;
        }

        for (int i = 0; i < file.items.Length; i++)
        {
            TileBaseControlCfgRow row = file.items[i];
            map[row.tilebaseId] = row;
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

        string[] guids = AssetDatabase.FindAssets("t:TileBaseThemePack", new[] { ThemeUiApplier.PackFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TileBaseThemePack pack = AssetDatabase.LoadAssetAtPath<TileBaseThemePack>(path);
            if (pack != null)
            {
                existing.Add(pack);
            }
        }

        existing.Sort((a, b) => a.tilebaseId.CompareTo(b.tilebaseId));
    }

    int NextFreeId()
    {
        int max = -1;
        for (int i = 0; i < existing.Count; i++)
        {
            if (existing[i] != null && existing[i].tilebaseId > max)
            {
                max = existing[i].tilebaseId;
            }
        }

        return max + 1;
    }

    [Serializable]
    class TileBaseControlCfgFile
    {
        public TileBaseControlCfgRow[] items;
    }

    [Serializable]
    class TileBaseControlCfgRow
    {
        public int tilebaseId;
        public string theme;
        public string TileBase_UI;
        public string Unknown_ui;
        public string Effect_collect_sound;
        public string trail_effect;
        public string tile_baroken_mat;
        public string tile_baroken_effect;
        public string tile_baroken_sound;
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
