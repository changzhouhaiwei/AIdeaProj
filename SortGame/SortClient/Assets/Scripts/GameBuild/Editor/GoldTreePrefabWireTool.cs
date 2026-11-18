#if UNITY_EDITOR
using System.IO;
using FishTilePlay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click wiring for GoldTree UI nodes copied from TileClient prefabs.
/// Menu: Tile2 / GoldTree / Wire Prefab References
/// </summary>
public static class GoldTreePrefabWireTool
{
    const string TileClientRoot = @"D:\GitProject\tileexplorerlike\TileClient\Assets";
    const string Tile2MainMenuPath = "Assets/CABRes/Prefabs/UIMain/Canvas/UI Main Menu.prefab";
    const string Tile2GamePath = "Assets/CABRes/Prefabs/UIMain/Canvas/UI Game.prefab";
    const string Tile2DockPath = "Assets/CABRes/Prefabs/DockPrefab/Dock.prefab";
    const string SrcMainMenuPath = TileClientRoot + "/GameRes/Prefabs/TUI/Canvas/UI Main Menu.prefab";
    const string SrcGamePath = TileClientRoot + "/GameRes/Prefabs/TUI/Canvas/UI Game.prefab";
    const string SrcDockPath = TileClientRoot + "/GameRes/Prefabs/TDock/Dock.prefab";
    const string SrcAnimDir = TileClientRoot + "/Arts/NewEff/Animation";
    const string DstAnimDir = "Assets/Arts/NewEff/Animation";
    const string SrcRankboardDir = TileClientRoot + "/GameRes/ImageAtlas/Rankboard";
    const string DstRankboardDir = "Assets/CABRes/SpriteAtlas/Rankboard";

    static readonly string[] GoldTreeAnimFiles =
    {
        "UIGoldTreeResult_Anim.anim",
        "UIGoldTreeStart_Anim.anim",
        "UIGoldTreeHelp_Anim.anim",
        "UIGoldTreePerfectCollect_Anim.anim",
        "UIGoldTreePerfectCollect_MainLoop_Anim.anim",
        "UIGoldTreeResult_RankAwardCell_Move_Anim.anim",
        "UIRankView_In_Anim.anim",
        "UIGoldPetfectCollect_Main.controller",
        "GoldTreeMove_Anim.anim",
        "GoldTreeGameBegin_Anim.anim",
        "GoldTreeGameBegin_LeavesLoop_Anim.anim",
        "GoldTreeEventEntrance_Anim.anim",
        "GoldTreeAllCollect_Anim.anim",
        "GoldTreeAllCollect.controller",
        "RankTreeCollect1-6_Anim.anim",
    };

    [MenuItem("Tile2/GoldTree/Sync Rankboard Atlas From TileClient")]
    public static void SyncRankboardAtlasFromTileClient()
    {
        if (!Directory.Exists(SrcRankboardDir))
        {
            EditorUtility.DisplayDialog("GoldTree", "TileClient Rankboard folder not found.", "OK");
            return;
        }

        Directory.CreateDirectory(DstRankboardDir);
        CopyDirectory(SrcRankboardDir, DstRankboardDir);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("GoldTree", $"Synced Rankboard atlas to {DstRankboardDir}.", "OK");
    }

    [MenuItem("Tile2/GoldTree/Sync Animation Assets From TileClient")]
    public static void SyncAnimationAssetsFromTileClient()
    {
        if (!Directory.Exists(SrcAnimDir))
        {
            EditorUtility.DisplayDialog("GoldTree", "TileClient animation folder not found.", "OK");
            return;
        }

        var copied = 0;
        foreach (var fileName in GoldTreeAnimFiles)
        {
            if (CopyAnimFile(fileName))
                copied++;
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("GoldTree", $"Synced {copied} animation assets to {DstAnimDir}.", "OK");
    }

    static void CopyDirectory(string srcDir, string dstDir)
    {
        foreach (var file in Directory.GetFiles(srcDir))
        {
            var name = Path.GetFileName(file);
            File.Copy(file, Path.Combine(dstDir, name), true);
        }

        foreach (var dir in Directory.GetDirectories(srcDir))
        {
            var name = Path.GetFileName(dir);
            var subDst = Path.Combine(dstDir, name);
            Directory.CreateDirectory(subDst);
            CopyDirectory(dir, subDst);
        }
    }

    static bool CopyAnimFile(string fileName)
    {
        var src = Path.Combine(SrcAnimDir, fileName);
        var dst = Path.Combine(DstAnimDir, fileName);
        if (!File.Exists(src))
            return false;

        Directory.CreateDirectory(DstAnimDir);
        File.Copy(src, dst, true);
        var srcMeta = src + ".meta";
        if (File.Exists(srcMeta))
            File.Copy(srcMeta, dst + ".meta", true);
        return true;
    }

    const string LocalRankTreeDir = "Assets/Arts/NewEff/UI_Imasgs/UI_NewRank";
    const string TileClientRankTreeDir = TileClientRoot + "/Arts/NewEff/UI_Imasgs/UI_NewRank";
    const string DstRankTreeDir = "Assets/CABRes/SpriteAtlas/GoldTree/UI_NewRank";

    [MenuItem("Tile2/GoldTree/Sync Main Menu Rank Tree Sprites")]
    public static void SyncMainMenuRankTreeSprites()
    {
        var copied = 0;
        var missingTiers = new System.Collections.Generic.List<int>();
        Directory.CreateDirectory(DstRankTreeDir);

        for (int tier = GoldTreeRobotRankRewardHelper.MinTierId; tier <= GoldTreeRobotRankRewardHelper.MaxTierId; tier++)
        {
            var fileName = $"lv{tier}.png";
            if (!TryCopyRankTreeSprite(fileName))
            {
                missingTiers.Add(tier);
                continue;
            }

            copied++;
        }

        AssetDatabase.Refresh();
        var message = copied > 0
            ? $"Synced {copied} rank tree sprites to {DstRankTreeDir}. Rebuild xasset bundles before device build."
            : "No rank tree sprites copied.";
        if (missingTiers.Count > 0)
        {
            message += $"\n\nMissing lv png for tiers: {string.Join(", ", missingTiers)}."
                + $"\nPlace source files under {LocalRankTreeDir} or {TileClientRankTreeDir}.";
        }

        EditorUtility.DisplayDialog("GoldTree", message, "OK");
    }

    static bool TryCopyRankTreeSprite(string fileName)
    {
        foreach (var srcDir in new[] { LocalRankTreeDir, TileClientRankTreeDir })
        {
            var src = Path.Combine(srcDir, fileName);
            if (!File.Exists(src))
                continue;

            var dst = Path.Combine(DstRankTreeDir, fileName);
            File.Copy(src, dst, true);
            return true;
        }

        return false;
    }

    [MenuItem("Tile2/GoldTree/Wire Main Menu goldActivityBtn Only")]
    public static void WireMainMenuOnly()
    {
        if (!File.Exists(SrcMainMenuPath))
        {
            EditorUtility.DisplayDialog("GoldTree", "TileClient UI Main Menu.prefab not found.", "OK");
            return;
        }

        WireMainMenu();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("GoldTree", "UI Main Menu goldActivityBtn wiring finished.", "OK");
    }

    [MenuItem("Tile2/GoldTree/Wire Game goldTreeNode Only")]
    public static void WireGameOnly()
    {
        WireGame();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("GoldTree", "UI Game goldTreeNode + GoldTreeEffectNode wiring finished.", "OK");
    }

    /// <summary>Batch/CI friendly — no dialog.</summary>
    public static void WireGameGoldTreeEffectBatch()
    {
        WireGame();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tile2/GoldTree/Wire Prefab References")]
    public static void WirePrefabReferences()
    {
        if (!File.Exists(SrcMainMenuPath) || !File.Exists(SrcGamePath))
        {
            EditorUtility.DisplayDialog("GoldTree", "TileClient source prefabs not found. Check TileClientRoot path in GoldTreePrefabWireTool.cs", "OK");
            return;
        }

        WireMainMenu();
        WireGame();
        WireDock();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("GoldTree", "Prefab wiring finished. Review UI Main Menu / UI Game / Dock in the Inspector.", "OK");
    }

    static void WireMainMenu()
    {
        var src = LoadPrefabContents(SrcMainMenuPath);
        var dst = LoadPrefabContents(Tile2MainMenuPath);
        if (src == null || dst == null) return;

        var srcBtn = FindDeep(src.transform, "goldActivityBtn");
        if (srcBtn == null)
        {
            Debug.LogWarning("goldActivityBtn not found in source main menu.");
            return;
        }

        var existing = FindDeep(dst.transform, "goldActivityBtn");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var parent = FindDeep(dst.transform, "Safe Zone");
        if (parent == null)
            parent = dst.transform;

        var clone = Object.Instantiate(srcBtn.gameObject, parent);
        clone.name = "goldActivityBtn";

        var menu = dst.GetComponent<UIMainMenu>();
        if (menu != null)
        {
            var so = new SerializedObject(menu);
            var goldBtnProp = so.FindProperty("goldActivityButton");
            var goldCountProp = so.FindProperty("goldActivityCount");
            if (goldBtnProp == null || goldCountProp == null)
            {
                Debug.LogError("GoldTreePrefabWireTool: goldActivityButton/goldActivityCount not found on UIMainMenu.");
            }
            else
            {
                var srcMenu = src.GetComponent<UIMainMenu>();
                if (srcMenu != null)
                {
                    var srcGoldBtnProp = new SerializedObject(srcMenu).FindProperty("goldActivityButton");
                    CopyMenuButtonAnimSettings(goldBtnProp, srcGoldBtnProp);
                }

                var rect = clone.GetComponent<RectTransform>();
                var button = clone.GetComponent<Button>() ?? clone.GetComponentInChildren<Button>(true);
                goldBtnProp.FindPropertyRelative("rect").objectReferenceValue = rect;
                goldBtnProp.FindPropertyRelative("button").objectReferenceValue = button;

                var count = clone.GetComponent<UIGoldTreeCount>() ?? clone.GetComponentInChildren<UIGoldTreeCount>(true);
                goldCountProp.objectReferenceValue = count;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        SaveAndUnload(dst, Tile2MainMenuPath);
        PrefabUtility.UnloadPrefabContents(src);
    }

    static void WireGame()
    {
        var src = LoadPrefabContents(SrcGamePath);
        var dst = LoadPrefabContents(Tile2GamePath);
        if (src == null || dst == null) return;

        var safeArea = FindDeep(dst.transform, "Safe Area");
        var parent = safeArea != null ? safeArea : dst.transform;
        EnsureGoldTreeHudNode(src, parent);
        CopyChildIfMissing(src.transform, parent, "GoldTreeEffectNode");
        WireGoldTreeEffectMgr(dst.transform);

        var uiGame = dst.GetComponent<UIGame>();
        if (uiGame != null)
        {
            var goldTree = FindDeep(dst.transform, "goldTreeNode")?.GetComponent<UIGoldTreeGame>();
            var so = new SerializedObject(uiGame);
            so.FindProperty("goldTreeGameUI").objectReferenceValue = goldTree;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        SaveAndUnload(dst, Tile2GamePath);
        PrefabUtility.UnloadPrefabContents(src);
    }

    static void WireDock()
    {
        var src = LoadPrefabContents(SrcDockPath);
        var dst = LoadPrefabContents(Tile2DockPath);
        if (src == null || dst == null) return;

        CopyChildIfMissing(src.transform, dst.transform, "goldTreeNode");

        var dock = dst.GetComponent<DockActor>();
        if (dock != null)
        {
            var node = FindDeep(dst.transform, "goldTreeNode")?.gameObject;
            var so = new SerializedObject(dock);
            so.FindProperty("goldTreeNode").objectReferenceValue = node;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        SaveAndUnload(dst, Tile2DockPath);
        PrefabUtility.UnloadPrefabContents(src);
    }

    static GameObject LoadPrefabContents(string path) => PrefabUtility.LoadPrefabContents(path);

    static void SaveAndUnload(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void CopyChildIfMissing(Transform srcRoot, Transform dstParent, string name)
    {
        if (FindDeep(dstParent, name) != null) return;
        var src = FindDeep(srcRoot, name);
        if (src == null) return;

        var clone = Object.Instantiate(src.gameObject, dstParent);
        clone.name = name;

        var dstRt = clone.GetComponent<RectTransform>();
        var srcRt = src.GetComponent<RectTransform>();
        if (dstRt != null && srcRt != null)
        {
            dstRt.anchorMin = srcRt.anchorMin;
            dstRt.anchorMax = srcRt.anchorMax;
            dstRt.pivot = srcRt.pivot;
            dstRt.anchoredPosition = srcRt.anchoredPosition;
            dstRt.sizeDelta = srcRt.sizeDelta;
            dstRt.localScale = srcRt.localScale;
        }
    }

    static void EnsureGoldTreeHudNode(GameObject srcUiGame, Transform dstParent)
    {
        var existing = FindDeep(dstParent, "goldTreeNode");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var srcNode = FindDeep(srcUiGame.transform, "goldTreeNode");
        if (srcNode != null)
        {
            var clone = Object.Instantiate(srcNode.gameObject, dstParent);
            clone.name = "goldTreeNode";
            var dstRt = clone.GetComponent<RectTransform>();
            var srcRt = srcNode.GetComponent<RectTransform>();
            if (dstRt != null && srcRt != null)
            {
                dstRt.anchorMin = srcRt.anchorMin;
                dstRt.anchorMax = srcRt.anchorMax;
                dstRt.pivot = srcRt.pivot;
                dstRt.anchoredPosition = srcRt.anchoredPosition;
                dstRt.sizeDelta = srcRt.sizeDelta;
                dstRt.localScale = srcRt.localScale;
            }
            return;
        }

        const string hudPath = "Assets/CABRes/Prefabs/UIMain/Widgets/GoldTreeHud.prefab";
        var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(hudPath);
        if (hudPrefab == null)
        {
            Debug.LogError($"GoldTreePrefabWireTool: goldTreeNode not found in source UI Game and {hudPath} is missing.");
            return;
        }

        var hud = PrefabUtility.InstantiatePrefab(hudPrefab, dstParent) as GameObject;
        if (hud != null)
            hud.name = "goldTreeNode";
    }

    static void WireGoldTreeEffectMgr(Transform uiGameRoot)
    {
        var effectNode = FindDeep(uiGameRoot, "GoldTreeEffectNode");
        if (effectNode == null)
        {
            Debug.LogWarning("GoldTreePrefabWireTool: GoldTreeEffectNode not found on UI Game.");
            return;
        }

        var effMgr = effectNode.GetComponent<GoldTreeEffMgr>();
        if (effMgr == null)
        {
            Debug.LogWarning("GoldTreePrefabWireTool: GoldTreeEffMgr missing on GoldTreeEffectNode.");
            return;
        }

        var dstIcon = FindDeep(uiGameRoot, "goldTreeIcon");
        if (dstIcon == null)
            dstIcon = FindDeep(uiGameRoot, "goldTreeNode");

        if (dstIcon == null)
        {
            Debug.LogWarning("GoldTreePrefabWireTool: goldTreeIcon/goldTreeNode not found for dstObj.");
            return;
        }

        var so = new SerializedObject(effMgr);
        so.FindProperty("dstObj").objectReferenceValue = dstIcon;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>Copies UIMainMenuButton animation settings (not rect/button refs).</summary>
    static void CopyMenuButtonAnimSettings(SerializedProperty dst, SerializedProperty src)
    {
        if (dst == null || src == null) return;

        var showCurve = dst.FindPropertyRelative("showStoreAdButtonsCurve");
        var srcShowCurve = src.FindPropertyRelative("showStoreAdButtonsCurve");
        if (showCurve != null && srcShowCurve != null)
            showCurve.animationCurveValue = srcShowCurve.animationCurveValue;

        var hideCurve = dst.FindPropertyRelative("hideStoreAdButtonsCurve");
        var srcHideCurve = src.FindPropertyRelative("hideStoreAdButtonsCurve");
        if (hideCurve != null && srcHideCurve != null)
            hideCurve.animationCurveValue = srcHideCurve.animationCurveValue;

        var duration = dst.FindPropertyRelative("showHideDuration");
        var srcDuration = src.FindPropertyRelative("showHideDuration");
        if (duration != null && srcDuration != null)
            duration.floatValue = srcDuration.floatValue;
    }
}
#endif
