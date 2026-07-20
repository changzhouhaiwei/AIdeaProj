#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Art.Role;

namespace Art.Role.Editor
{
    /// <summary>
    /// Builds cucumber enemy prefab + Chapter1 farm combat scene.
    /// Menu: Art/Role/Setup Chapter1 Farm Scene
    /// </summary>
    public static class SetupChapter1Scene
    {
        const string RoleDir = "Assets/Art/Role";
        const string PrefabDir = "Assets/Art/Role/Prefabs";
        const string SceneDir = "Assets/Art/Role/Scenes";
        const string ScenePath = SceneDir + "/Chapter1_Farm.unity";
        const string CucumberSpritePath = RoleDir + "/enemy_cucumber.png";
        const string CucumberPrefabPath = PrefabDir + "/Role_Cucumber.prefab";
        const string PotatoPrefabPath = PrefabDir + "/Role_Potato.prefab";
        const string BgPath = "Assets/Art/Bg/bg_battle_farm.png";
        const float Ppu = 100f;

        [MenuItem("Art/Role/Setup Chapter1 Farm Scene")]
        public static void Setup()
        {
            EnsureFolder(PrefabDir);
            EnsureFolder(SceneDir);

            ConfigureSprite(CucumberSpritePath, Ppu);
            ConfigureSprite(BgPath, 100f);

            var cucumberPrefab = BuildCucumberPrefab();
            if (cucumberPrefab == null)
            {
                Debug.LogError("[Chapter1] Failed to build cucumber prefab.");
                return;
            }

            var potatoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotatoPrefabPath);
            if (potatoPrefab == null)
            {
                Debug.LogError("[Chapter1] Missing Role_Potato.prefab — run Art/Role/Create Role_Potato Prefab first.");
                return;
            }

            EnsurePlayerScriptOnPrefab(potatoPrefab);

            BuildScene(cucumberPrefab, potatoPrefab);
            Debug.Log("[Chapter1] Ready: " + ScenePath + "  Controls: WASD move, mouse aim, LMB/Space shoot (auto when enemies exist).");
        }

        static GameObject BuildCucumberPrefab()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CucumberSpritePath);
            if (sprite == null)
            {
                AssetDatabase.ImportAsset(CucumberSpritePath, ImportAssetOptions.ForceUpdate);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CucumberSpritePath);
            }
            if (sprite == null)
            {
                Debug.LogError("[Chapter1] Missing sprite: " + CucumberSpritePath);
                return null;
            }

            var root = new GameObject("Role_Cucumber");
            root.transform.localScale = Vector3.one * 0.28f;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 12;

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = root.AddComponent<CircleCollider2D>();
            col.radius = 1.6f; // in prefab local space before scale ~0.45 world

            var chase = root.AddComponent<BadgeEnemyChase>();
            chase.maxHp = 24f;
            chase.moveSpeed = 2.5f;
            chase.contactDamage = 8f;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CucumberPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void EnsurePlayerScriptOnPrefab(GameObject potatoPrefab)
        {
            string path = AssetDatabase.GetAssetPath(potatoPrefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root.GetComponent<BadgePlayerController>() == null)
                root.AddComponent<BadgePlayerController>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void BuildScene(GameObject cucumberPrefab, GameObject potatoPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Camera
            var cam = Object.FindObjectOfType<Camera>();
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 7.2f;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // Remove default light if any (2D)
            foreach (var light in Object.FindObjectsOfType<Light>())
                Object.DestroyImmediate(light.gameObject);

            // Background
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
            if (bgSprite == null)
            {
                AssetDatabase.ImportAsset(BgPath, ImportAssetOptions.ForceUpdate);
                bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
            }
            var bg = new GameObject("Background_Farm");
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = bgSprite;
            bgSr.sortingOrder = -50;
            // Fit roughly to arena view
            if (bgSprite != null)
            {
                float worldH = cam.orthographicSize * 2f;
                float worldW = worldH * cam.aspect;
                float sw = bgSprite.bounds.size.x;
                float sh = bgSprite.bounds.size.y;
                float scale = Mathf.Max(worldW / Mathf.Max(0.01f, sw), worldH / Mathf.Max(0.01f, sh)) * 1.05f;
                bg.transform.localScale = Vector3.one * scale;
            }

            // Player
            var player = (GameObject)PrefabUtility.InstantiatePrefab(potatoPrefab);
            player.name = "Player_Potato";
            player.transform.position = Vector3.zero;
            var playerCtrl = player.GetComponent<BadgePlayerController>();
            if (playerCtrl == null) playerCtrl = player.AddComponent<BadgePlayerController>();
            playerCtrl.arenaHalf = 7.5f;

            // Spawner
            var spawnerGo = new GameObject("EnemySpawner");
            var spawner = spawnerGo.AddComponent<BadgeEnemySpawner>();
            spawner.enemyPrefab = cucumberPrefab.GetComponent<BadgeEnemyChase>();
            spawner.player = player.transform;
            spawner.arenaHalf = 7.5f;
            spawner.intervalStart = 1.2f;
            spawner.maxAlive = 24;

            // Bounds helper (invisible)
            var bounds = new GameObject("ArenaBounds");
            bounds.transform.position = Vector3.zero;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath);
            Selection.activeObject = player;
        }

        static void ConfigureSprite(string path, float ppu)
        {
            if (!File.Exists(path) && !File.Exists(Path.GetFullPath(path)))
            {
                // Unity asset path
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), path)))
                    return;
            }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

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
            if (Mathf.Abs(importer.spritePixelsPerUnit - ppu) > 0.01f)
            {
                importer.spritePixelsPerUnit = ppu;
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
            if (dirty) importer.SaveAndReimport();
        }

        static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
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
