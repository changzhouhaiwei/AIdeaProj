using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PotatoSiege.EditorTools
{
    public static class PotatoSiegeSetup
    {
        const string ScenePath = "Assets/PotatoSiege/Scenes/PotatoSiege.unity";

        [MenuItem("PotatoSiege/Setup Scene And Sprites")]
        public static void SetupAll()
        {
            FixSpriteImports();
            CreateScene();
            Debug.Log("[PotatoSiege] Setup complete. Press Play. WASD move, Space skill.");
        }

        [MenuItem("PotatoSiege/Fix Sprite Imports")]
        public static void FixSpriteImports()
        {
            string[] folders =
            {
                "Assets/PotatoSiege/Resources/PotatoSiege/Sprites",
                "Assets/PotatoSiege/Art/Kenney/PNG"
            };
            int count = 0;
            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;
                    bool dirty = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        dirty = true;
                    }
                    if (importer.spritePixelsPerUnit != 48f)
                    {
                        importer.spritePixelsPerUnit = 48f;
                        dirty = true;
                    }
                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        dirty = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        dirty = true;
                    }
                    if (dirty)
                    {
                        importer.SaveAndReimport();
                        count++;
                    }
                }
            }
            Debug.Log($"[PotatoSiege] Reimported {count} sprites.");
        }

        [MenuItem("PotatoSiege/Create / Open Scene")]
        public static void CreateScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/PotatoSiege/Scenes"))
                AssetDatabase.CreateFolder("Assets/PotatoSiege", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var boot = new GameObject("PotatoSiegeBoot");
            boot.AddComponent<PotatoSiege.PotatoSiegeBoot>();

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 11f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.16f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            // Build settings
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) scenes.Add(s);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log($"[PotatoSiege] Scene ready: {ScenePath}");
        }
    }
}
