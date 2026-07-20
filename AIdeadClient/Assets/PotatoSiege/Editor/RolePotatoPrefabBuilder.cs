using UnityEditor;
using UnityEngine;

namespace PotatoSiege.EditorTools
{
    /// <summary>
    /// Assembles RolePotato prefab from Assets/Art/Role/Parts (simple_mode: limb_l/r).
    /// Pivots/positions from Parts/parts_meta.json.
    /// </summary>
    public static class RolePotatoPrefabBuilder
    {
        const string PartsDir = "Assets/Art/Role/Parts";
        const string PrefabDir = "Assets/PotatoSiege/Prefabs";
        const string PrefabPath = PrefabDir + "/RolePotato.prefab";
        const float Ppu = 100f;

        // parts_meta.json — body pivot_canvas is prefab root origin
        static readonly Vector2 BodyPivotCanvas = new Vector2(400.698f, 406.818f);

        [MenuItem("PotatoSiege/Build RolePotato Prefab")]
        public static void Build()
        {
            EnsureFolder(PrefabDir);

            SetSpritePivot(PartsDir + "/role_potato_body.png", 281.698f / 566f, (723f - 402.818f) / 723f);
            SetSpritePivot(PartsDir + "/role_potato_limb_l.png", 140f / 191f, (267f - 157f) / 267f);
            SetSpritePivot(PartsDir + "/role_potato_limb_r.png", 50f / 193f, (263f - 150f) / 263f);
            SetSpritePivot(PartsDir + "/role_potato_eye_l.png", 0.5f, 0.5f);
            SetSpritePivot(PartsDir + "/role_potato_eye_r.png", 0.5f, 0.5f);

            var body = LoadSprite(PartsDir + "/role_potato_body.png");
            var limbL = LoadSprite(PartsDir + "/role_potato_limb_l.png");
            var limbR = LoadSprite(PartsDir + "/role_potato_limb_r.png");
            var eyeL = LoadSprite(PartsDir + "/role_potato_eye_l.png");
            var eyeR = LoadSprite(PartsDir + "/role_potato_eye_r.png");

            var root = new GameObject("RolePotato");
            try
            {
                AddPart(root.transform, "Body", body, Vector3.zero, 10);
                AddPart(root.transform, "Limb_L", limbL, CanvasToLocal(148f, 378f), 12);
                AddPart(root.transform, "Limb_R", limbR, CanvasToLocal(657f, 379f), 12);
                AddPart(root.transform, "Eye_L", eyeL, CanvasToLocal(351.432f, 381.385f), 14);
                AddPart(root.transform, "Eye_R", eyeR, CanvasToLocal(446.987f, 381.399f), 14);

                // Art is ~8 units tall at PPU=100; shrink for gameplay readability
                root.transform.localScale = Vector3.one * 0.35f;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log("[PotatoSiege] Built " + PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Vector3 CanvasToLocal(float cx, float cy)
        {
            return new Vector3(
                (cx - BodyPivotCanvas.x) / Ppu,
                (BodyPivotCanvas.y - cy) / Ppu,
                0f);
        }

        static void AddPart(Transform parent, string name, Sprite sprite, Vector3 localPos, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }

        static void SetSpritePivot(string assetPath, float pivotX, float pivotY)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new System.Exception("Missing texture: " + assetPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Ppu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(pivotX, pivotY);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new System.Exception("Sprite missing (check import): " + path);
            return sprite;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
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
