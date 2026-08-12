// AUTOBUILD_TOUCH
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Art.UI.Editor
{
    /// <summary>
    /// Builds a uGUI prefab from layout.json + Parts/*.png.
    /// Menu: Art/UI/Build Prefab From Layout
    /// </summary>
    public static class BuildUiPrefabFromLayout
    {
        const float DefaultPpu = 100f;
        const string BuildAllRequestPath = "Assets/Art/UI/Editor/.build_all_request";
        const string CommonPartsDir = "Assets/Art/UI/Common/Parts";
        const string CommonSpritePrefix = "Common/";

        [InitializeOnLoadMethod]
        static void AutoBuildOnRequest()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(BuildAllRequestPath))
                    return;
                try
                {
                    File.Delete(BuildAllRequestPath);
                }
                catch
                {
                    // ignore
                }

                Debug.Log("[UI] Detected .build_all_request ï¿½?building all UI prefabsï¿½?);
                BuildAll(logErrorsAsDialog: false);
            };
        }

        [Serializable]
        class LayoutRoot
        {
            public string screenId;
            public LayoutCanvas canvas;
            public LayoutNode[] nodes;
        }

        [Serializable]
        class LayoutCanvas
        {
            public int width;
            public int height;
        }

        [Serializable]
        class LayoutRect
        {
            public float x;
            public float y;
            public float w;
            public float h;
        }

        [Serializable]
        class LayoutNode
        {
            public string id;
            public string type;
            public string parent;
            public LayoutRect rect;
            public float anchorMinX = 0.5f;
            public float anchorMinY = 0.5f;
            public float anchorMaxX = 0.5f;
            public float anchorMaxY = 0.5f;
            public float pivotX = 0.5f;
            public float pivotY = 0.5f;
            public string sprite;
            public int nineSliceL;
            public int nineSliceR;
            public int nineSliceT;
            public int nineSliceB;
            public int useNineSlice;
            public string text;
            public int fontSize;
            public int sortingOrder;
        }

        [MenuItem("Art/UI/Build Prefab From Layout")]
        public static void BuildFromMenu()
        {
            string layoutPath = null;
            if (Selection.activeObject != null)
            {
                var sel = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(sel) && sel.EndsWith("layout.json", StringComparison.OrdinalIgnoreCase))
                    layoutPath = sel;
            }

            if (string.IsNullOrEmpty(layoutPath))
            {
                var abs = EditorUtility.OpenFilePanel("Select layout.json", "Assets/Art/UI", "json");
                if (string.IsNullOrEmpty(abs))
                    return;
                layoutPath = ToAssetPath(abs);
                if (string.IsNullOrEmpty(layoutPath))
                {
                    EditorUtility.DisplayDialog("Build UI Prefab", "Please select a layout.json under Assets/.", "OK");
                    return;
                }
            }

            try
            {
                var prefab = Build(layoutPath);
                if (prefab != null)
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                    Debug.Log("[UI] Prefab built: " + AssetDatabase.GetAssetPath(prefab));
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Build UI Prefab", e.Message, "OK");
            }
        }

        [MenuItem("Art/UI/Build All Prefabs From Layout")]
        public static void BuildAllFromMenu()
        {
            var ok = BuildAll(logErrorsAsDialog: true);
            if (ok)
                EditorUtility.DisplayDialog("Build UI Prefab", "All UI prefabs built under Assets/Art/UI.", "OK");
        }

        /// <summary>Unity batchmode entry: -executeMethod Art.UI.Editor.BuildUiPrefabFromLayout.BuildAllBatch</summary>
        public static void BuildAllBatch()
        {
            int code = 0;
            try
            {
                if (!BuildAll(logErrorsAsDialog: false))
                    code = 1;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 2;
            }
            EditorApplication.Exit(code);
        }

        public static bool BuildAll(bool logErrorsAsDialog)
        {
            var layoutPaths = new List<string>();
            var uiRoot = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Art", "UI");
            if (Directory.Exists(uiRoot))
            {
                foreach (var file in Directory.GetFiles(uiRoot, "layout.json", SearchOption.AllDirectories))
                {
                    var asset = ToAssetPath(file.Replace('\\', '/'));
                    if (!string.IsNullOrEmpty(asset))
                        layoutPaths.Add(asset);
                }
            }

            layoutPaths.Sort(StringComparer.OrdinalIgnoreCase);
            if (layoutPaths.Count == 0)
            {
                Debug.LogError("[UI] No layout.json found under Assets/Art/UI");
                if (logErrorsAsDialog)
                    EditorUtility.DisplayDialog("Build UI Prefab", "No layout.json found under Assets/Art/UI", "OK");
                return false;
            }

            bool allOk = true;
            foreach (var path in layoutPaths)
            {
                try
                {
                    var prefab = Build(path);
                    Debug.Log("[UI] Prefab built: " + (prefab != null ? AssetDatabase.GetAssetPath(prefab) : "(null)") + " from " + path);
                }
                catch (Exception e)
                {
                    allOk = false;
                    Debug.LogError("[UI] Failed building " + path + ": " + e.Message);
                    Debug.LogException(e);
                    if (logErrorsAsDialog)
                        EditorUtility.DisplayDialog("Build UI Prefab", path + "\n" + e.Message, "OK");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return allOk;
        }

        public static GameObject Build(string layoutAssetPath)
        {
            if (string.IsNullOrEmpty(layoutAssetPath) || !layoutAssetPath.EndsWith(".json"))
                throw new InvalidOperationException("Invalid layout path: " + layoutAssetPath);

            var absLayout = Path.GetFullPath(layoutAssetPath);
            if (!File.Exists(absLayout))
            {
                // Unity asset path
                absLayout = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), layoutAssetPath));
            }

            if (!File.Exists(absLayout))
                throw new FileNotFoundException("layout.json not found", layoutAssetPath);

            var json = File.ReadAllText(absLayout);
            var layout = JsonUtility.FromJson<LayoutRoot>(json);
            if (layout == null || layout.nodes == null || layout.nodes.Length == 0)
                throw new InvalidOperationException("Failed to parse layout nodes.");

            var screenId = string.IsNullOrEmpty(layout.screenId)
                ? Path.GetFileName(Path.GetDirectoryName(absLayout))
                : layout.screenId;

            var screenDir = Path.GetDirectoryName(layoutAssetPath).Replace('\\', '/');
            var partsDir = screenDir + "/Parts";
            var prefabDir = screenDir + "/Prefabs";
            var prefabPath = prefabDir + "/UI_" + screenId + ".prefab";

            EnsureFolder(prefabDir);
            EnsureFolder(CommonPartsDir);
            ConfigurePartImports(partsDir, layout.nodes);
            ConfigurePartImports(CommonPartsDir, layout.nodes);

            int canvasW = layout.canvas != null && layout.canvas.width > 0 ? layout.canvas.width : 1080;
            int canvasH = layout.canvas != null && layout.canvas.height > 0 ? layout.canvas.height : 1920;

            var byId = new Dictionary<string, LayoutNode>(StringComparer.Ordinal);
            foreach (var n in layout.nodes)
            {
                if (n == null || string.IsNullOrEmpty(n.id))
                    continue;
                byId[n.id] = n;
            }

            var rootGo = new GameObject("UI_" + screenId);
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = rootGo.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(canvasW, canvasH);
            scaler.matchWidthOrHeight = 0.5f;
            rootGo.AddComponent<GraphicRaycaster>();

            var created = new Dictionary<string, RectTransform>(StringComparer.Ordinal);
            var rootRt = rootGo.GetComponent<RectTransform>();
            if (rootRt == null)
                rootRt = rootGo.AddComponent<RectTransform>();
            created["root"] = rootRt;

            // Create in dependency order (parents before children)
            var pending = new List<LayoutNode>(layout.nodes);
            pending.Sort((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
            int guard = pending.Count * pending.Count + 8;
            while (pending.Count > 0 && guard-- > 0)
            {
                bool progress = false;
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    var node = pending[i];
                    if (node == null || string.IsNullOrEmpty(node.id))
                    {
                        pending.RemoveAt(i);
                        continue;
                    }

                    if (string.Equals(node.type, "Root", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(node.id, "root", StringComparison.OrdinalIgnoreCase))
                    {
                        ApplyRect(rootRt, node, null, canvasW, canvasH, isCanvasRoot: true);
                        created[node.id] = rootRt;
                        pending.RemoveAt(i);
                        progress = true;
                        continue;
                    }

                    var parentId = string.IsNullOrEmpty(node.parent) ? "root" : node.parent;
                    if (!created.TryGetValue(parentId, out var parentRt))
                        continue;

                    byId.TryGetValue(parentId, out var parentNode);
                    var go = new GameObject(node.id);
                    var rt = go.AddComponent<RectTransform>();
                    rt.SetParent(parentRt, false);
                    ApplyRect(rt, node, parentNode, canvasW, canvasH, isCanvasRoot: false);
                    BuildVisual(go, node, partsDir);
                    created[node.id] = rt;
                    pending.RemoveAt(i);
                    progress = true;
                }

                if (!progress)
                    break;
            }

            if (pending.Count > 0)
            {
                var ids = string.Join(", ", pending.ConvertAll(n => n.id));
                Debug.LogWarning("[UI] Unresolved nodes (missing parent?): " + ids);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, prefabPath);
            UnityEngine.Object.DestroyImmediate(rootGo);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return prefab;
        }

        static void BuildVisual(GameObject go, LayoutNode node, string partsDir)
        {
            var type = node.type ?? "";
            bool isButton = type.Equals("Button", StringComparison.OrdinalIgnoreCase);
            bool isIcon = type.Equals("Icon", StringComparison.OrdinalIgnoreCase);
            bool isPanel = type.Equals("Panel", StringComparison.OrdinalIgnoreCase);
            bool isImage = type.Equals("Image", StringComparison.OrdinalIgnoreCase);
            bool hasSprite = !string.IsNullOrEmpty(node.sprite);

            // Avoid stacking empty transparent Images (overdraw / fake alpha).
            // Only create Image when we have a sprite, or need an opaque hit target for Button.
            Image image = null;
            if (hasSprite || isPanel || isImage || isIcon || isButton)
            {
                if (hasSprite || isPanel || isImage || isIcon || isButton)
                {
                    bool needGraphic = hasSprite || isButton || isPanel;
                    if (needGraphic)
                    {
                        image = go.AddComponent<Image>();
                        image.raycastTarget = isButton || isPanel;

                        if (hasSprite)
                        {
                            var sprite = LoadSprite(partsDir, node.sprite);
                            if (sprite != null)
                            {
                                image.sprite = sprite;
                                image.type = node.useNineSlice != 0 ? Image.Type.Sliced : Image.Type.Simple;
                                image.preserveAspect = isIcon;
                                image.color = Color.white;
                            }
                            else
                            {
                                Debug.LogWarning("[UI] Missing sprite: " + partsDir + "/" + node.sprite);
                                // Opaque solid fallback â never leave semi-transparent empty Image.
                                image.sprite = null;
                                image.color = new Color(0.92f, 0.86f, 0.72f, 1f);
                            }
                        }
                        else if (isButton)
                        {
                            // Text-only / hit-area button: fully transparent graphic (no texture alpha overdraw).
                            image.sprite = null;
                            image.color = new Color(1f, 1f, 1f, 0f);
                            image.raycastTarget = true;
                        }
                        else if (isPanel)
                        {
                            image.sprite = null;
                            image.color = new Color(0.96f, 0.92f, 0.84f, 1f);
                        }
                    }
                }
            }

            if (isButton)
            {
                var btn = go.AddComponent<Button>();
                if (image != null)
                    btn.targetGraphic = image;
            }

            bool wantsText = !string.IsNullOrEmpty(node.text)
                || type.Equals("Text", StringComparison.OrdinalIgnoreCase);

            // Buttons may also show label text (TMP), but avoid creating Text Image+TMP stacks without need.
            if (!string.IsNullOrEmpty(node.text) && (wantsText || isButton))
            {
                GameObject textGo;
                if (type.Equals("Text", StringComparison.OrdinalIgnoreCase) && image == null)
                {
                    textGo = go;
                }
                else
                {
                    textGo = new GameObject("Label");
                    var tr = textGo.AddComponent<RectTransform>();
                    tr.SetParent(go.transform, false);
                    tr.anchorMin = Vector2.zero;
                    tr.anchorMax = Vector2.one;
                    // Leave side padding so icons/arrows are not covered when present.
                    float padL = isButton && HasChildIconHint(node) ? 90f : 16f;
                    float padR = isButton && HasChildIconHint(node) ? 56f : 16f;
                    tr.offsetMin = new Vector2(padL, 8f);
                    tr.offsetMax = new Vector2(-padR, -8f);
                }

                var tmp = textGo.GetComponent<TextMeshProUGUI>();
                if (tmp == null)
                    tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text = node.text;
                tmp.fontSize = node.fontSize > 0 ? node.fontSize : 36;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.25f, 0.15f, 0.1f, 1f);
                tmp.raycastTarget = false;
            }
        }

        static bool HasChildIconHint(LayoutNode node)
        {
            // Heuristic: settings list rows use left icon + right arrow; keep label padding.
            var id = node.id ?? "";
            return id.StartsWith("row_", StringComparison.OrdinalIgnoreCase);
        }

        static void ApplyRect(
            RectTransform rt,
            LayoutNode node,
            LayoutNode parentNode,
            int canvasW,
            int canvasH,
            bool isCanvasRoot)
        {
            if (isCanvasRoot)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                return;
            }

            var rect = node.rect ?? new LayoutRect();
            // Nested layout rects are absolute (canvas top-left). Prefer center anchors + relative pos.
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(node.pivotX, node.pivotY);

            var childCenter = RectCenterInCanvasSpace(rect, canvasW, canvasH);
            Vector2 parentCenter;
            if (parentNode == null
                || string.Equals(parentNode.type, "Root", StringComparison.OrdinalIgnoreCase)
                || string.Equals(parentNode.id, "root", StringComparison.OrdinalIgnoreCase)
                || parentNode.rect == null)
            {
                parentCenter = Vector2.zero;
            }
            else
            {
                parentCenter = RectCenterInCanvasSpace(parentNode.rect, canvasW, canvasH);
            }

            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.w);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.h);
            rt.anchoredPosition = childCenter - parentCenter;
        }

        /// <summary>Convert top-left origin rect to Unity canvas-centered coords (Y up).</summary>
        static Vector2 RectCenterInCanvasSpace(LayoutRect rect, int canvasW, int canvasH)
        {
            float cx = rect.x + rect.w * 0.5f;
            float cyTop = rect.y + rect.h * 0.5f;
            return new Vector2(cx - canvasW * 0.5f, canvasH * 0.5f - cyTop);
        }

        static void ConfigurePartImports(string partsDir, LayoutNode[] nodes)
        {
            if (!AssetDatabase.IsValidFolder(partsDir))
                return;

            var borders = new Dictionary<string, Vector4>(StringComparer.Ordinal);
            foreach (var n in nodes)
            {
                if (n == null || string.IsNullOrEmpty(n.sprite) || n.useNineSlice == 0)
                    continue;
                borders[n.sprite] = new Vector4(n.nineSliceL, n.nineSliceB, n.nineSliceR, n.nineSliceT);
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { partsDir });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (Mathf.Abs(importer.spritePixelsPerUnit - DefaultPpu) > 0.01f)
                {
                    importer.spritePixelsPerUnit = DefaultPpu;
                    dirty = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }

                var fileName = Path.GetFileName(path);
                // layout may reference Common/btn_close.png — match either form
                Vector4 border;
                bool hasBorder = borders.TryGetValue(fileName, out border)
                    || borders.TryGetValue(CommonSpritePrefix + fileName, out border);

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                // 9-slice needs FullRect; icons/buttons use Tight to cut transparent overdraw.
                var meshType = hasBorder
                    ? SpriteMeshType.FullRect
                    : SpriteMeshType.Tight;
                if (settings.spriteMeshType != meshType)
                {
                    settings.spriteMeshType = meshType;
                    importer.SetTextureSettings(settings);
                    dirty = true;
                }

                if (hasBorder)
                {
                    importer.spriteBorder = border;
                    dirty = true;
                }

                if (dirty)
                    importer.SaveAndReimport();
            }
        }

        static string ResolveSpritePath(string partsDir, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;
            fileName = fileName.Replace('\\', '/');
            if (fileName.StartsWith(CommonSpritePrefix, StringComparison.OrdinalIgnoreCase))
                return CommonPartsDir + "/" + fileName.Substring(CommonSpritePrefix.Length);
            return partsDir + "/" + fileName;
        }

        static Sprite LoadSprite(string partsDir, string fileName)
        {
            var path = ResolveSpritePath(partsDir, fileName);
            if (string.IsNullOrEmpty(path))
                return null;

            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null)
                return sp;

            // Fallback: ensure importer is Sprite then reimport.
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (dirty || sp == null)
                    importer.SaveAndReimport();
                sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            if (sp == null)
            {
                // Last resort: copy Common → local Parts under the same filename.
                if (fileName.StartsWith(CommonSpritePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var localName = fileName.Substring(CommonSpritePrefix.Length);
                    var localPath = partsDir + "/" + localName;
                    var absCommon = Path.GetFullPath(path);
                    var absLocal = Path.GetFullPath(localPath);
                    if (File.Exists(absCommon))
                    {
                        EnsureFolder(partsDir);
                        File.Copy(absCommon, absLocal, true);
                        AssetDatabase.ImportAsset(localPath, ImportAssetOptions.ForceUpdate);
                        sp = AssetDatabase.LoadAssetAtPath<Sprite>(localPath);
                    }
                }
            }

            if (sp == null)
                Debug.LogWarning("[UI] Missing sprite: field='" + fileName + "' path='" + path + "'");
            return sp;
        }

        static string ToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return null;
            absolutePath = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            if (absolutePath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                return "Assets" + absolutePath.Substring(dataPath.Length);
            var idx = absolutePath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                return absolutePath.Substring(idx + 1);
            return null;
        }

        static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;
            var parts = assetPath.Split('/');
            var cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
#endif
