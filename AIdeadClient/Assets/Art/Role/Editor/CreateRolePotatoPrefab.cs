#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Art.Role.Editor
{
    /// <summary>
    /// Builds Role_Potato prefab from Parts sprites (body / arms / guns / hands / eyes).
    /// Menu: Art/Role/Create Role_Potato Prefab
    /// </summary>
    public static class CreateRolePotatoPrefab
    {
        const string PartsDir = "Assets/Art/Role/Parts";
        const string PrefabDir = "Assets/Art/Role/Prefabs";
        const string PrefabPath = PrefabDir + "/Role_Potato.prefab";
        const float Ppu = 100f;
        const float VisualScale = 0.35f;
        const int CanvasW = 808;
        const int CanvasH = 731;

        [MenuItem("Art/Role/Create Role_Potato Prefab")]
        public static void Create()
        {
            EnsureFolder(PrefabDir);
            ConfigurePartImports();

            var body = LoadSprite("role_potato_body_full.png");
            var armL = LoadSprite("role_potato_arm_l_full.png");
            var armR = LoadSprite("role_potato_arm_r_full.png");
            var gunL = LoadSprite("role_potato_gun_l_full.png");
            var gunR = LoadSprite("role_potato_gun_r_full.png");
            var handL = LoadSprite("role_potato_hand_l_full.png");
            var handR = LoadSprite("role_potato_hand_r_full.png");
            var eye = LoadSprite("role_potato_eye.png");
            if (eye == null)
                eye = LoadSprite("role_potato_eye_l.png");

            if (body == null || armL == null || armR == null || gunL == null || gunR == null
                || handL == null || handR == null || eye == null)
            {
                Debug.LogError("[Role_Potato] Missing part sprites under " + PartsDir);
                return;
            }

            var handLPos = CanvasToLocal(148f, 378f);
            var handRPos = CanvasToLocal(657f, 379f);
            var eyeLPos = CanvasToLocal(351.432f, 381.385f);
            var eyeRPos = CanvasToLocal(446.987f, 381.399f);

            var root = new GameObject("Role_Potato");
            var visual = MakeChild(root.transform, "Visual");
            visual.transform.localScale = Vector3.one * VisualScale;

            AddSprite(MakeChild(visual.transform, "Body"), body, 0);

            var aimL = MakeChild(visual.transform, "Aim_L");
            aimL.transform.localPosition = new Vector3(handLPos.x, handLPos.y, 0f);
            var limbL = MakeChild(aimL.transform, "Limb_L");
            limbL.transform.localPosition = new Vector3(-handLPos.x, -handLPos.y, 0f);
            AddSprite(MakeChild(limbL.transform, "Arm_L"), armL, 1);
            AddSprite(MakeChild(limbL.transform, "Gun_L"), gunL, 2);
            AddSprite(MakeChild(limbL.transform, "Hand_L"), handL, 3);

            var aimR = MakeChild(visual.transform, "Aim_R");
            aimR.transform.localPosition = new Vector3(handRPos.x, handRPos.y, 0f);
            var limbR = MakeChild(aimR.transform, "Limb_R");
            limbR.transform.localPosition = new Vector3(-handRPos.x, -handRPos.y, 0f);
            AddSprite(MakeChild(limbR.transform, "Arm_R"), armR, 1);
            AddSprite(MakeChild(limbR.transform, "Gun_R"), gunR, 2);
            AddSprite(MakeChild(limbR.transform, "Hand_R"), handR, 3);

            var eyeL = MakeChild(visual.transform, "Eye_L");
            eyeL.transform.localPosition = new Vector3(eyeLPos.x, eyeLPos.y, 0f);
            AddSprite(eyeL, eye, 10);

            var eyeR = MakeChild(visual.transform, "Eye_R");
            eyeR.transform.localPosition = new Vector3(eyeRPos.x, eyeRPos.y, 0f);
            AddSprite(eyeR, eye, 10);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log("[Role_Potato] Prefab saved: " + PrefabPath);
            }
            else
            {
                Debug.LogError("[Role_Potato] Failed to save prefab.");
            }
        }

        static Vector2 CanvasToLocal(float px, float py)
        {
            float cx = CanvasW * 0.5f;
            float cy = CanvasH * 0.5f;
            return new Vector2((px - cx) / Ppu, (cy - py) / Ppu);
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

        static void ConfigurePartImports()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { PartsDir });
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(".png"))
                    continue;
                if (path.Contains("_preview"))
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
                if (Mathf.Abs(importer.spritePixelsPerUnit - Ppu) > 0.01f)
                {
                    importer.spritePixelsPerUnit = Ppu;
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

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteAlignment != (int)SpriteAlignment.Center)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                    importer.SetTextureSettings(settings);
                    dirty = true;
                }

                if (dirty)
                    importer.SaveAndReimport();
            }
        }

        static Sprite LoadSprite(string fileName)
        {
            var path = PartsDir + "/" + fileName;
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null && File.Exists(path))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return sp;
        }

        static GameObject MakeChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        static void AddSprite(GameObject go, Sprite sprite, int order)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
        }
    }
}
#endif
