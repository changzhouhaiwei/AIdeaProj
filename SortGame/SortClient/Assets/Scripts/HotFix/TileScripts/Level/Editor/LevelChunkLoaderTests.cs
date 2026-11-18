#if UNITY_INCLUDE_TESTS
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishTilePlay.Tests
{
    public class LevelChunkLoaderTests
    {
        const string GameScenePath = "Assets/CABRes/Scenes/GameScene.unity";

        [Test]
        public void GetChunkId_UsesChunkSize50()
        {
            Assert.AreEqual(0, LevelChunkLoader.GetChunkId(0));
            Assert.AreEqual(0, LevelChunkLoader.GetChunkId(49));
            Assert.AreEqual(1, LevelChunkLoader.GetChunkId(50));
            Assert.AreEqual(1, LevelChunkLoader.GetChunkId(99));
            Assert.AreEqual(19, LevelChunkLoader.GetChunkId(999));
        }

        [Test]
        public void GetLevelPath_FollowsDatabaseListIndex()
        {
            AssertLevelPathMatchesDatabaseSlot(9);
        }

        [Test]
        public void GetLevelPath_SecondPage_ResolvesOnDemand()
        {
            AssertLevelPathMatchesDatabaseSlot(LevelChunkLoader.PlayOrderPageSize);
        }

        static void AssertLevelPathMatchesDatabaseSlot(int index)
        {
            LevelCatalog database = LevelPlatformResolver.LoadDatabaseInEditor();
            var serialized = database != null ? new SerializedObject(database) : null;
            var levels = serialized != null ? serialized.FindProperty("levels") : null;
            if (levels == null || !levels.isArray || levels.arraySize <= index)
            {
                Assert.Ignore("Current platform Level Database has no level at index " + index);
            }

            int instanceId = levels.GetArrayElementAtIndex(index).objectReferenceInstanceIDValue;
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instanceId, out string guid, out long _));
            string expected = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');

            LevelChunkLoader.ClearPlayOrderCache();
            string actual = LevelChunkLoader.GetLevelPath(index).Replace('\\', '/');
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ShouldPackLevel_RespectsMax()
        {
            Assert.IsTrue(LevelChunkLoader.ShouldPackLevel(1, 1000));
            Assert.IsTrue(LevelChunkLoader.ShouldPackLevel(1000, 1000));
            Assert.IsFalse(LevelChunkLoader.ShouldPackLevel(1001, 1000));
            Assert.IsFalse(LevelChunkLoader.ShouldPackLevel(0, 1000));
        }

        [Test]
        public void GameScene_DoesNotHostTileLevelDirector()
        {
            Assert.IsTrue(File.Exists(GameScenePath), "GameScene missing: " + GameScenePath);

            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool openedByTest = !scene.IsValid() || !scene.isLoaded;
            if (openedByTest)
            {
                scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
            }

            try
            {
                LevelDirector director = FindLevelDirector(scene);
                Assert.IsNull(director, "GameScene 不再挂 Tile 关卡。");
            }
            finally
            {
                if (openedByTest && scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static LevelDirector FindLevelDirector(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                LevelDirector found = roots[i].GetComponentInChildren<LevelDirector>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
#endif
