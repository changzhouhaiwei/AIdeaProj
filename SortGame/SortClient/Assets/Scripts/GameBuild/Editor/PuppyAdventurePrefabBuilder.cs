#if UNITY_EDITOR
using System.IO;
using FishFramework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PuppyAdventurePrefabBuilder
{
    const string Atlas = "Assets/CABRes/SpriteAtlas/PuppyAdventure";
    const string PrefabFolder = "Assets/CABRes/Prefabs/UIActivity/PuppyAdventure";
    const string WidgetFolder = "Assets/CABRes/Prefabs/UIMain/Widgets";
    const string FontPath = "Assets/CABRes/Fonts/Arial-Unicode-Bold-RSDF.asset";
    const string ClosePath = "Assets/CABRes/SpriteAtlas/GPCommon/XSort_btn_close.png";

    [MenuItem("Tools/Puppy Adventure/Rebuild Prefabs")]
    public static void Rebuild()
    {
        ConfigureSprites();
        KnockoutSpriteBackgrounds();
        RebuildUi();
    }

    [MenuItem("Tools/Puppy Adventure/Knockout Sprite Backgrounds")]
    public static void KnockoutMenu()
    {
        KnockoutSpriteBackgrounds();
        Debug.Log("[PuppyAdventure] Sprite backgrounds knocked out.");
    }

    [MenuItem("Tools/Puppy Adventure/Rebuild UI")]
    public static void RebuildUi()
    {
        EnsureFolder("Assets/CABRes/Prefabs/UIActivity", "PuppyAdventure");
        EnsureFolder("Assets/CABRes/Prefabs/UIMain", "Widgets");
        BuildStart();
        BuildMatch();
        BuildMain();
        BuildReward();
        BuildHelp();
        BuildIcon();
        AttachIconToMainMenu();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PuppyAdventure] UI prefabs rebuilt.");
    }

    [MenuItem("Tools/Puppy Adventure/Attach Widgets")]
    public static void AttachWidgets()
    {
        AttachIconToMainMenu();
        AssetDatabase.SaveAssets();
        Debug.Log("[PuppyAdventure] Widgets attached.");
    }

    [MenuItem("Tools/Puppy Adventure/Open Start")]
    public static void OpenStart()
    {
        if (!Application.isPlaying || GameModule.UI == null)
        {
            Debug.LogWarning("[PuppyAdventure] Enter Play Mode first.");
            return;
        }

        GameModule.UI.OpenPanel<UIPuppyAdventureStart>();
    }

    [MenuItem("Tools/Puppy Adventure/Set Start Ready")]
    public static void SetReady()
    {
        PuppyAdventureDemoState.SetDebugStartMode(PuppyAdventureStartMode.Ready);
    }

    [MenuItem("Tools/Puppy Adventure/Set Start Cooldown")]
    public static void SetCooldown()
    {
        PuppyAdventureDemoState.SetDebugStartMode(PuppyAdventureStartMode.Cooldown);
    }

    [MenuItem("Tools/Puppy Adventure/Set Start Daily Closed")]
    public static void SetDailyClosed()
    {
        PuppyAdventureDemoState.SetDebugStartMode(PuppyAdventureStartMode.DailyClosed);
    }

    [MenuItem("Tools/Puppy Adventure/Run Self Test")]
    public static void RunSelfTest()
    {
        Debug.Log("[PuppyAdventure] " + PuppyAdventureDemoState.RunSelfTest());
    }

    static void ConfigureSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Atlas });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 100;
            if (path.EndsWith("pa_start_panel.png"))
                importer.spriteBorder = new Vector4(80, 90, 80, 90);
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    static void KnockoutSpriteBackgrounds()
    {
        string[] files =
        {
            "pa_start_panel", "pa_btn_start", "pa_btn_start_grey", "pa_trail_node",
            "pa_avatar_1", "pa_avatar_2", "pa_avatar_3", "pa_avatar_4",
            "pa_avatar_5", "pa_avatar_6", "pa_avatar_7", "pa_avatar_8",
            "pa_icon", "pa_prize_bowl", "pa_reward_chest", "pa_coin",
            "pa_coin_glow", "pa_help_1", "pa_help_2", "pa_help_3", "pa_help_4",
            "pa_hud_chip", "pa_reddot", "pa_icon_progress"
        };
        for (int i = 0; i < files.Length; i++)
        {
            string path = $"{Atlas}/{files[i]}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
                continue;

            Color32[] pixels = tex.GetPixels32();
            Color32 key = pixels[0];
            int w = tex.width;
            int h = tex.height;
            for (int p = 0; p < pixels.Length; p++)
            {
                int dr = pixels[p].r - key.r;
                int dg = pixels[p].g - key.g;
                int db = pixels[p].b - key.b;
                int dist = dr * dr + dg * dg + db * db;
                if (dist < 2200)
                {
                    byte a = dist < 900 ? (byte)0 : (byte)Mathf.Clamp(pixels[p].a * (dist / 2200f), 0, 255);
                    pixels[p].a = a;
                }
            }

            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels32(pixels);
            string fullPath = Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName, path));
            File.WriteAllBytes(fullPath, outTex.EncodeToPNG());
            Object.DestroyImmediate(outTex);
            AssetDatabase.ImportAsset(path);
        }
    }

    static void BuildStart()
    {
        GameObject root = CreatePanelRoot("UIPuppyAdventureStart");
        RectTransform anim = Child(root.transform, "AnimNode", 640, 960, Vector2.zero);
        Image(anim, "Panel", Sp("pa_start_panel"), 640, 960, Vector2.zero);
        Image(anim, "Hero", Sp("pa_start_hero"), 460, 280, new Vector2(0, 20));
        var title = Tmp(anim, "titleTex", 36, new Vector2(0, 205), 520, 64);
        title.color = new Color(0.45f, 0.24f, 0.08f, 1f);
        title.overflowMode = TextOverflowModes.Overflow;
        Tmp(anim, "desTex", 26, new Vector2(0, -150), 520, 70);
        Image(anim, "PrizeBowl", Sp("pa_prize_bowl"), 180, 100, new Vector2(-120, -235));
        Tmp(anim, "prizeLabelTex", 22, new Vector2(90, -215), 260, 32);
        var prize = Tmp(anim, "prizeNumTex", 36, new Vector2(90, -258), 260, 56);
        prize.color = new Color(0.85f, 0.45f, 0.08f, 1f);
        prize.overflowMode = TextOverflowModes.Overflow;
        TButton(anim, "StartBtn", Sp("pa_btn_start"), 463, 140, new Vector2(0, -355), "UI_Start");
        TButton grey = TButton(anim, "StartGreyBtn", Sp("pa_btn_start_grey"), 463, 140, new Vector2(0, -340), "UI_Start");
        grey.gameObject.SetActive(false);
        Tmp(grey.transform, "cooldownTex", 22, new Vector2(0, -48), 400, 32);
        TButton(anim, "CloseBtn", Close(), 72, 72, new Vector2(270, 420));
        FinishPanel(root, anim, UIPanelType.Popup, UIPanelBgClickEventType.CloseSelf);
        BindOps(root,
            "AnimNode/StartBtn|TButton",
            "AnimNode/StartGreyBtn|TButton",
            "AnimNode/CloseBtn|TButton",
            "AnimNode/titleTex|TMP",
            "AnimNode/desTex|TMP",
            "AnimNode/prizeNumTex|TMP",
            "AnimNode/prizeLabelTex|TMP",
            "AnimNode/StartGreyBtn/cooldownTex|TMP");
        Save(root, $"{PrefabFolder}/UIPuppyAdventureStart.prefab");
    }

    static void BuildMatch()
    {
        GameObject root = CreatePanelRoot("UIPuppyAdventureMatch");
        RectTransform anim = Child(root.transform, "AnimNode", 0, 0, Vector2.zero, true);
        Image(anim, "BgFar", Sp("pa_bg_far"), 720, 1560, Vector2.zero, true);
        Image(anim, "BgMid", Sp("pa_bg_mid"), 720, 420, new Vector2(0, 80));
        Image(anim, "BgNear", Sp("pa_bg_near"), 720, 220, new Vector2(0, -620));
        Tmp(anim, "findTex", 32, new Vector2(0, 560), 600, 48);
        Tmp(anim, "playerNumTex", 64, new Vector2(0, 470), 400, 80);
        Image(anim, "PrizeBowl", Sp("pa_prize_bowl"), 220, 126, new Vector2(0, 340));
        Tmp(anim, "prizeNumTex", 36, new Vector2(0, 250), 240, 48);
        RectTransform avatars = Child(anim, "Avatars", 640, 120, new Vector2(0, 80));
        for (int i = 0; i < 8; i++)
        {
            float x = -280 + i * 80;
            Image(avatars, $"A{i + 1}", Sp($"pa_avatar_{i + 1}"), 72, 72, new Vector2(x, 0));
        }

        FinishPanel(root, anim, UIPanelType.FullScreen, UIPanelBgClickEventType.DontRespone);
        BindOps(root,
            "AnimNode/findTex|TMP",
            "AnimNode/playerNumTex|TMP",
            "AnimNode/prizeNumTex|TMP",
            "AnimNode/Avatars|RT");
        Save(root, $"{PrefabFolder}/UIPuppyAdventureMatch.prefab");
    }

    static void BuildMain()
    {
        GameObject root = CreatePanelRoot("UIPuppyAdventureMain");
        RectTransform anim = Child(root.transform, "AnimNode", 0, 0, Vector2.zero, true);
        Image(anim, "BgFar", Sp("pa_bg_far"), 720, 1560, Vector2.zero, true);
        Image(anim, "BgMid", Sp("pa_bg_mid"), 720, 420, new Vector2(0, 40));
        Image(anim, "BgNear", Sp("pa_bg_near"), 720, 220, new Vector2(0, -640));

        RectTransform top = Child(anim, "Top", 680, 220, new Vector2(0, 620));
        RectTransform success = Child(top, "SuccessRoot", 680, 220, Vector2.zero);
        Image(success, "HudBg", Sp("pa_hud_top"), 680, 180, Vector2.zero);
        Chip(success, "ProgressChip", "progressTex", new Vector2(-170, 20));
        Chip(success, "PlayerChip", "playersTex", new Vector2(170, 20));
        Tmp(success, "remainDesTex", 24, new Vector2(0, -50), 600, 40);
        Tmp(success, "timerTex", 22, new Vector2(0, -88), 400, 32);
        RectTransform fail = Child(top, "FailRoot", 680, 180, Vector2.zero);
        Image(fail, "FailBanner", Sp("pa_fail_banner"), 680, 160, Vector2.zero);
        Tmp(fail, "failTex", 30, Vector2.zero, 560, 60);
        fail.gameObject.SetActive(false);

        RectTransform trail = Child(anim, "Trail", 560, 780, new Vector2(0, -20));
        Image(trail, "Line", Sp("pa_trail_line"), 24, 520, new Vector2(0, 20));
        RectTransform nodes = Child(trail, "trailNodes", 560, 780, Vector2.zero);
        Vector2[] pts =
        {
            new Vector2(-80, -300),
            new Vector2(90, -190),
            new Vector2(-70, -80),
            new Vector2(80, 30),
            new Vector2(-60, 140),
            new Vector2(70, 250),
            new Vector2(0, 360)
        };
        for (int i = 0; i < pts.Length; i++)
            Image(nodes, $"N{i + 1}", Sp("pa_trail_node"), 120, 90, pts[i]);

        Image(trail, "PlayerAvatar", Sp("pa_avatar_1"), 88, 88, pts[0] + new Vector2(0, 48));

        TButton(anim, "InfoBtn", Sp("pa_hud_chip"), 64, 64, new Vector2(300, 700), "?");
        TButton(anim, "AdvanceBtn", Sp("pa_btn_start"), 220, 72, new Vector2(-160, -680), ">>");
        TButton(anim, "FailBtn", Sp("pa_btn_start_grey"), 220, 72, new Vector2(160, -680), "X");
        TButton tap = TButton(anim, "tapBtn", Sp("pa_btn_start"), 463, 120, new Vector2(0, -680), "UI_TaptoContinue");
        tap.gameObject.SetActive(false);

        FinishPanel(root, anim, UIPanelType.FullScreen, UIPanelBgClickEventType.DontRespone);
        BindOps(root,
            "AnimNode/Top/SuccessRoot/ProgressChip/progressTex|TMP",
            "AnimNode/Top/SuccessRoot/PlayerChip/playersTex|TMP",
            "AnimNode/Top/SuccessRoot/remainDesTex|TMP",
            "AnimNode/Top/SuccessRoot/timerTex|TMP",
            "AnimNode/Top/FailRoot/failTex|TMP",
            "AnimNode/InfoBtn|TButton",
            "AnimNode/AdvanceBtn|TButton",
            "AnimNode/FailBtn|TButton",
            "AnimNode/tapBtn|TButton",
            "AnimNode/Trail/PlayerAvatar|RT",
            "AnimNode/Top/FailRoot|RT",
            "AnimNode/Top/SuccessRoot|RT",
            "AnimNode/BgMid|RT",
            "AnimNode/BgNear|RT",
            "AnimNode/Trail/trailNodes|RT",
            "AnimNode/Top/CloseBtn|TButton");
        Save(root, $"{PrefabFolder}/UIPuppyAdventureMain.prefab");
    }

    static void Chip(RectTransform parent, string name, string valueName, Vector2 pos)
    {
        RectTransform chip = Child(parent, name, 200, 80, pos);
        Image(chip, "Bg", Sp("pa_hud_chip"), 200, 80, Vector2.zero);
        Tmp(chip, valueName, 28, Vector2.zero, 180, 48);
    }

    static void BuildReward()
    {
        GameObject root = CreatePanelRoot("UIPuppyAdventureReward");
        RectTransform anim = Child(root.transform, "AnimNode", 640, 860, Vector2.zero);
        Image(anim, "Panel", Sp("pa_start_panel"), 640, 860, Vector2.zero);
        Tmp(anim, "titleTex", 40, new Vector2(0, 310), 500, 52);
        Image(anim, "Chest", Sp("pa_reward_chest"), 240, 200, new Vector2(0, 100));
        Image(anim, "CoinGlow", Sp("pa_coin_glow"), 140, 140, new Vector2(0, -50));
        Image(anim, "Coin", Sp("pa_coin"), 88, 88, new Vector2(0, -50));
        var coinNum = Tmp(anim, "coinNumTex", 44, new Vector2(0, -145), 360, 64);
        coinNum.color = new Color(0.85f, 0.45f, 0.08f, 1f);
        coinNum.overflowMode = TextOverflowModes.Overflow;
        Tmp(anim, "shareTex", 24, new Vector2(0, -210), 520, 50);
        Tmp(anim, "nextTex", 24, new Vector2(0, -258), 520, 40);
        TButton(anim, "TapBtn", Sp("pa_btn_start"), 463, 130, new Vector2(0, -340), "UI_TaptoContinue");
        TButton(anim, "CloseBtn", Close(), 72, 72, new Vector2(270, 380));
        FinishPanel(root, anim, UIPanelType.Popup, UIPanelBgClickEventType.CloseSelf);
        BindOps(root,
            "AnimNode/TapBtn|TButton",
            "AnimNode/CloseBtn|TButton",
            "AnimNode/titleTex|TMP",
            "AnimNode/coinNumTex|TMP",
            "AnimNode/shareTex|TMP",
            "AnimNode/nextTex|TMP",
            "AnimNode/CoinGlow|RT",
            "AnimNode/Chest|RT",
            "AnimNode/Coin|RT");
        Save(root, $"{PrefabFolder}/UIPuppyAdventureReward.prefab");
    }

    static void BuildHelp()
    {
        GameObject root = CreatePanelRoot("UIPuppyAdventureHelp");
        RectTransform anim = Child(root.transform, "AnimNode", 600, 800, Vector2.zero);
        Image(anim, "Panel", Sp("pa_start_panel"), 600, 800, Vector2.zero);
        Tmp(anim, "titleTex", 36, new Vector2(0, 330), 480, 48);
        float[] ys = { 190, 50, -90, -230 };
        for (int i = 0; i < 4; i++)
        {
            RectTransform row = Child(anim, $"Row{i + 1}", 540, 130, new Vector2(0, ys[i]));
            Image(row, "Icon", Sp($"pa_help_{i + 1}"), 150, 104, new Vector2(-170, 0));
            var helpTex = Tmp(row, $"help{i + 1}", 22, new Vector2(90, 0), 340, 110);
            helpTex.overflowMode = TextOverflowModes.Overflow;
        }

        TButton(anim, "CloseBtn", Close(), 72, 72, new Vector2(270, 360));
        FinishPanel(root, anim, UIPanelType.Popup, UIPanelBgClickEventType.CloseSelf);
        BindOps(root,
            "AnimNode/CloseBtn|TButton",
            "AnimNode/titleTex|TMP",
            "AnimNode/Row1/help1|TMP",
            "AnimNode/Row2/help2|TMP",
            "AnimNode/Row3/help3|TMP",
            "AnimNode/Row4/help4|TMP");
        Save(root, $"{PrefabFolder}/UIPuppyAdventureHelp.prefab");
    }

    static void BuildIcon()
    {
        GameObject root = new GameObject("PuppyAdventureIcon", typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(128, 121);
        var hitArea = root.AddComponent<UnityEngine.UI.Image>();
        hitArea.sprite = Sp("pa_icon");
        hitArea.color = new Color(1f, 1f, 1f, 0f);
        hitArea.raycastTarget = true;
        var button = root.AddComponent<TButton>();
        button.transition = Selectable.Transition.None;
        var iconImg = Image(rt, "Icon", Sp("pa_icon"), 128, 121, Vector2.zero);
        iconImg.raycastTarget = true;
        Image(rt, "Progress", Sp("pa_icon_progress"), 48, 48, new Vector2(40, -36));
        Image(rt, "RedDot", Sp("pa_reddot"), 28, 28, new Vector2(48, 46));
        var time = Tmp(rt, "Time", 20, new Vector2(0, -52), 128, 28);
        time.color = Color.white;
        time.outlineColor = new Color32(91, 51, 24, 255);
        time.outlineWidth = 0.22f;
        var icon = root.AddComponent<PuppyAdventureIcon>();
        var so = new SerializedObject(icon);
        so.FindProperty("redDot").objectReferenceValue = rt.Find("RedDot").gameObject;
        so.FindProperty("timeTex").objectReferenceValue = rt.Find("Time").GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedPropertiesWithoutUndo();
        Save(root, $"{WidgetFolder}/PuppyAdventureIcon.prefab");
    }

    static void BuildHud()
    {
        GameObject root = new GameObject("PuppyAdventureHud", typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 72);
        Image(rt, "Bg", Sp("pa_hud_ingame"), 160, 72, Vector2.zero);
        Tmp(rt, "Value", 22, new Vector2(16, 0), 120, 40);
        var hud = root.AddComponent<PuppyAdventureHud>();
        var so = new SerializedObject(hud);
        so.FindProperty("progressLb").objectReferenceValue = rt.Find("Value").GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedPropertiesWithoutUndo();
        Save(root, $"{WidgetFolder}/PuppyAdventureHud.prefab");
    }

    static void AttachIconToMainMenu()
    {
        const string host = "Assets/CABRes/Prefabs/UIMain/Canvas/UI Main Menu.prefab";
        AttachWidget(host, "Safe Zone", "PuppyAdventureIcon", $"{WidgetFolder}/PuppyAdventureIcon.prefab",
            new Vector2(1f, 1f), new Vector2(-654.3f, -650f), new Vector2(128f, 121f));
    }

    static void AttachHudToGamePlay()
    {
        const string host = "Assets/CABRes/Prefabs/UIMain/Canvas/UI Game Play.prefab";
        AttachWidget(host, "Safe Area", "PuppyAdventureHud", $"{WidgetFolder}/PuppyAdventureHud.prefab",
            new Vector2(0f, 1f), new Vector2(135f, -530f), new Vector2(160f, 72f));
    }

    static void AttachWidget(string hostPath, string parentName, string childName, string widgetPath, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject host = AssetDatabase.LoadAssetAtPath<GameObject>(hostPath);
        GameObject widgetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(widgetPath);
        if (host == null || widgetPrefab == null)
            return;

        GameObject instance = PrefabUtility.LoadPrefabContents(hostPath);
        try
        {
            Transform parent = FindDeep(instance.transform, parentName);
            if (parent == null)
                return;

            Transform existing = parent.Find(childName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(widgetPrefab, parent);
            child.name = childName;
            SetLayerRecursively(child, LayerMask.NameToLayer("UI"));
            var rt = child.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(instance, hostPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(instance);
        }
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        for (int i = 0; i < root.transform.childCount; i++)
            SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    static GameObject CreatePanelRoot(string name)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UIPanelBehaviour));
        root.layer = LayerMask.NameToLayer("UI");
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return root;
    }

    static RectTransform Child(Transform parent, string name, float w, float h, Vector2 pos, bool stretch = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (stretch)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = pos;
        }

        return rt;
    }

    static Image Image(RectTransform parent, string name, Sprite sprite, float w, float h, Vector2 pos, bool stretch = false)
    {
        var rt = Child(parent, name, w, h, pos, stretch);
        var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;
        if (name == "Panel")
        {
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.preserveAspect = false;
        }

        return img;
    }

    static TextMeshProUGUI Tmp(Transform parent, string name, int size, Vector2 pos, float w, float h)
    {
        var rt = Child(parent, name, w, h, pos);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = PuppyAdventureUiUtil.Brown;
        tmp.raycastTarget = false;
        tmp.text = name;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    static TButton TButton(RectTransform parent, string name, Sprite sprite, float w, float h, Vector2 pos, string label = null)
    {
        var rt = Child(parent, name, w, h, pos);
        var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.raycastTarget = true;
        var btn = rt.gameObject.AddComponent<TButton>();
        btn.transition = Selectable.Transition.None;
        if (!string.IsNullOrEmpty(label))
        {
            var tmp = Tmp(rt, "Label", 32, Vector2.zero, w - 20, h - 16);
            tmp.text = label.StartsWith("UI_") ? PuppyAdventureLoc.Get(label) : label;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        return btn;
    }

    static void FinishPanel(GameObject root, RectTransform anim, UIPanelType type, UIPanelBgClickEventType click)
    {
        var behaviour = root.GetComponent<UIPanelBehaviour>();
        var so = new SerializedObject(behaviour);
        so.FindProperty("m_PanelType").enumValueIndex = (int)type;
        bool popup = type == UIPanelType.Popup || type == UIPanelType.Tips;
        so.FindProperty("m_HasBg").boolValue = type != UIPanelType.Float;
        so.FindProperty("m_BgShowType").enumValueIndex = popup || type == UIPanelType.System
            ? (int)UIPanelBgShowType.HalfAlphaBlack
            : (int)UIPanelBgShowType.Alpha;
        so.FindProperty("m_BgClickEventType").enumValueIndex = (int)click;
        so.FindProperty("m_Thickness").intValue = 10;
        so.FindProperty("m_AnimNode").objectReferenceValue = popup ? anim : null;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BindOps(GameObject root, params string[] binds)
    {
        var behaviour = root.GetComponent<UIPanelBehaviour>();
        var so = new SerializedObject(behaviour);
        var list = so.FindProperty("m_OpElementList");
        list.ClearArray();
        for (int i = 0; i < binds.Length; i++)
        {
            string[] pair = binds[i].Split('|');
            Transform t = root.transform.Find(pair[0]);
            if (t == null)
                continue;
            Component c = null;
            if (pair[1] == "TButton")
                c = t.GetComponent<TButton>();
            else if (pair[1] == "TMP")
                c = t.GetComponent<TextMeshProUGUI>();
            else if (pair[1] == "RT")
                c = t.GetComponent<RectTransform>();
            if (c == null)
                continue;
            list.arraySize++;
            SerializedProperty el = list.GetArrayElementAtIndex(list.arraySize - 1);
            el.FindPropertyRelative("m_Target").objectReferenceValue = t.gameObject;
            SerializedProperty cl = el.FindPropertyRelative("m_ComponentList");
            cl.ClearArray();
            cl.arraySize = 1;
            cl.GetArrayElementAtIndex(0).objectReferenceValue = c;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Save(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    static Sprite Sp(string file)
    {
        return LoadSprite($"{Atlas}/{file}.png");
    }

    static Sprite Close()
    {
        return LoadSprite(ClosePath);
    }

    static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;
        Object[] subs = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < subs.Length; i++)
        {
            if (subs[i] is Sprite s)
                return s;
        }

        return null;
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
