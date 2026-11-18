#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FishTilePlay
{
    public static class BoxCoverPrefabBuilder
    {
        private const string PrefabFolder = "Assets/CABRes/Prefabs/BoxCover/Coke";
        private static readonly Vector2[] ContentNormalizedPositions =
        {
            new Vector2(-0.22f, 0.14f),
            new Vector2(0.22f, 0.14f),
            new Vector2(0f, -0.18f),
        };

        [MenuItem("Tools/Box Cover/Rebuild Visual Prefabs")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/CABRes/Prefabs", "BoxCover");
            EnsureFolder("Assets/CABRes/Prefabs/BoxCover", "Coke");
            Build(BoxCoverSize.Size2x2);
            Build(BoxCoverSize.Size2x3);
            Build(BoxCoverSize.Size3x3);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BoxCover] Coke visual prefabs rebuilt (Bottom / Content / Lid).");
        }

        [MenuItem("Tools/Box Cover/Rebuild HUD Prefab")]
        public static void RebuildHudPrefab()
        {
            EnsureFolder("Assets/CABRes/Prefabs", "BoxCover");

            GameObject root = new GameObject(
                "BoxCoverHUD",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(BoxCoverHUD));
            try
            {
                root.layer = 5;
                RectTransform rootRect = (RectTransform)root.transform;
                rootRect.anchorMin = new Vector2(1f, 1f);
                rootRect.anchorMax = new Vector2(1f, 1f);
                rootRect.pivot = new Vector2(1f, 1f);
                rootRect.sizeDelta = new Vector2(250f, 70f);
                rootRect.anchoredPosition = Vector2.zero;

                GameObject content = new GameObject(
                    "Content",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                content.layer = 5;
                RectTransform contentRect = (RectTransform)content.transform;
                contentRect.SetParent(rootRect, false);
                contentRect.anchorMin = Vector2.zero;
                contentRect.anchorMax = Vector2.one;
                contentRect.offsetMin = Vector2.zero;
                contentRect.offsetMax = Vector2.zero;
                contentRect.pivot = new Vector2(0.5f, 0.5f);

                Image background = content.GetComponent<Image>();
                background.sprite = LoadSprite("Assets/CABRes/SpriteAtlas/DailyWin/DailyWin_dhk.png");
                background.type = Image.Type.Sliced;
                background.color = new Color(1f, 0.96f, 0.82f, 0.98f);
                background.raycastTarget = false;

                CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/CABRes/Fonts/Arial-Unicode-Bold-RSDF.asset");
                if (font == null)
                    font = TMP_Settings.defaultFontAsset;
                Image pandaIcon = CreateHudImage(
                    "Panda",
                    contentRect,
                    new Vector2(0f, 0.5f),
                    new Vector2(32f, 0f),
                    new Vector2(50f, 60f),
                    "Assets/CABRes/Sprites/BoxCover/DailyWin_panda.png");
                Image collectibleIcon = CreateHudImage(
                    "Collectible",
                    contentRect,
                    new Vector2(0f, 0.5f),
                    new Vector2(82f, -10f),
                    new Vector2(34f, 34f),
                    BoxCoverThemePaths.GetCollectibleSpritePath(BoxCoverType.Coke));
                TextMeshProUGUI progressText = CreateHudText(
                    "Progress",
                    contentRect,
                    font,
                    new Vector2(105f, 5f),
                    new Vector2(-12f, -5f),
                    30f,
                    new Color(0.35f, 0.17f, 0.06f, 1f),
                    "0/0");
                TextMeshProUGUI fullCollectionText = CreateHudText(
                    "FullCollection",
                    contentRect,
                    font,
                    new Vector2(103f, 2f),
                    new Vector2(-10f, -2f),
                    23f,
                    new Color(1f, 0.82f, 0.22f, 1f),
                    "Full Collection");
                fullCollectionText.gameObject.SetActive(false);

                SerializedObject serializedHud = new SerializedObject(root.GetComponent<BoxCoverHUD>());
                serializedHud.FindProperty("root").objectReferenceValue = rootRect;
                serializedHud.FindProperty("contentRoot").objectReferenceValue = contentRect;
                serializedHud.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
                serializedHud.FindProperty("pandaIcon").objectReferenceValue = pandaIcon;
                serializedHud.FindProperty("collectibleIcon").objectReferenceValue = collectibleIcon;
                serializedHud.FindProperty("progressText").objectReferenceValue = progressText;
                serializedHud.FindProperty("fullCollectionText").objectReferenceValue = fullCollectionText;
                serializedHud.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, BoxCoverHUD.PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[BoxCover] HUD prefab rebuilt: {BoxCoverHUD.PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Image CreateHudImage(
            string name,
            RectTransform parent,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            string spritePath)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)imageObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.sprite = LoadSprite(spritePath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateHudText(
            string name,
            RectTransform parent,
            TMP_FontAsset font,
            Vector2 offsetMin,
            Vector2 offsetMax,
            float fontSize,
            Color color,
            string text)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
            if (font != null && font.material != null)
            {
                tmp.font = font;
                tmp.fontSharedMaterial = font.material;
            }
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18f;
            tmp.fontSizeMax = fontSize;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.Midline;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = color;
            tmp.text = text;
            return tmp;
        }

        private static void Build(BoxCoverSize size)
        {
            BoxCoverType type = BoxCoverType.Coke;
            string key = BoxCoverSizeUtility.GetDisplayName(size);
            Sprite opened = LoadSprite(BoxCoverThemePaths.GetOpenSpritePath(type, size));
            Sprite closed = LoadSprite(BoxCoverThemePaths.GetClosedSpritePath(type, size));
            Sprite lid = LoadSprite(BoxCoverThemePaths.GetLidSpritePath(type, size));
            if (lid == null)
                lid = closed;
            if (opened == null || lid == null)
            {
                Debug.LogError($"[BoxCover] Sprite import is incomplete for Coke {key}.");
                return;
            }

            Sprite collectible = LoadSprite(BoxCoverThemePaths.GetCollectibleSpritePath(type));
            GameObject root = new GameObject($"Coke_BoxCover_{key}");
            try
            {
                BoxCoverBehavior behavior = root.AddComponent<BoxCoverBehavior>();
                BoxCollider boxCollider = root.AddComponent<BoxCollider>();

                GameObject visuals = new GameObject("Visuals");
                visuals.transform.SetParent(root.transform, false);

                GameObject bottom = new GameObject("Bottom");
                bottom.transform.SetParent(visuals.transform, false);
                SpriteRenderer bottomRenderer = bottom.AddComponent<SpriteRenderer>();
                bottomRenderer.sprite = opened;

                GameObject content = new GameObject("Content");
                content.transform.SetParent(visuals.transform, false);
                Vector2 boxSize = opened.bounds.size;
                Vector2Int cellSize = BoxCoverSizeUtility.GetCellSize(size);
                for (int i = 0; i < BoxCoverController.ItemsPerBox; i++)
                {
                    Sprite itemSprite = LoadSprite(
                        BoxCoverThemePaths.GetContentItemSpritePath(type, size, i));
                    if (itemSprite == null)
                        itemSprite = collectible;

                    GameObject item = new GameObject($"Item_{i + 1}");
                    item.transform.SetParent(content.transform, false);
                    SpriteRenderer itemRenderer = item.AddComponent<SpriteRenderer>();
                    itemRenderer.sprite = itemSprite;
                    Vector2 normalized = ContentNormalizedPositions[
                        Mathf.Clamp(i, 0, ContentNormalizedPositions.Length - 1)];
                    item.transform.localPosition = new Vector3(
                        normalized.x * boxSize.x,
                        normalized.y * boxSize.y,
                        -0.02f);
                    if (itemSprite != null)
                    {
                        float tileWidth = boxSize.x / Mathf.Max(1, cellSize.x);
                        float tileHeight = boxSize.y / Mathf.Max(1, cellSize.y);
                        float targetDiameter = Mathf.Min(tileWidth, tileHeight) * 0.82f;
                        Vector2 spriteSize = itemSprite.bounds.size;
                        float scale = Mathf.Min(
                            spriteSize.x > 0.0001f ? targetDiameter / spriteSize.x : 1f,
                            spriteSize.y > 0.0001f ? targetDiameter / spriteSize.y : 1f);
                        item.transform.localScale = Vector3.one * scale;
                    }
                }

                GameObject lidObject = new GameObject("Lid");
                lidObject.transform.SetParent(visuals.transform, false);
                SpriteRenderer lidRenderer = lidObject.AddComponent<SpriteRenderer>();
                lidRenderer.sprite = lid;
                BoxCoverBehavior.ApplyHierarchySortingOrders(visuals.transform, 0);

                SerializedObject serializedBehavior = new SerializedObject(behavior);
                serializedBehavior.FindProperty("visuals").objectReferenceValue = visuals.transform;
                serializedBehavior.FindProperty("bottomRenderer").objectReferenceValue = bottomRenderer;
                serializedBehavior.FindProperty("contentRoot").objectReferenceValue = content.transform;
                serializedBehavior.FindProperty("lidRenderer").objectReferenceValue = lidRenderer;
                serializedBehavior.FindProperty("boxCollider").objectReferenceValue = boxCollider;
                serializedBehavior.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    BoxCoverThemePaths.GetPrefabPath(type, size));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Sprite LoadSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
                return sprite;

            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is Sprite subSprite)
                    return subSprite;
            }

            return null;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
