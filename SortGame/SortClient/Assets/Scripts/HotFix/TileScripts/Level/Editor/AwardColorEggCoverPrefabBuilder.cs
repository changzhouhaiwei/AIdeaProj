#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FishTilePlay
{
    public static class AwardColorEggCoverPrefabBuilder
    {
        const string PrefabPath = "Assets/CABRes/Prefabs/AwardColorEggCover/AwardColorEggCover_2x2.prefab";
        const string SpritePath = "Assets/CABRes/Sprites/AwardColorEggCover/EggPurple.png";

        [MenuItem("Tools/Award Color Egg Cover/Rebuild Prefab")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/CABRes/Prefabs", "AwardColorEggCover");
            GameObject root = new GameObject(
                "AwardColorEggCover_2x2",
                typeof(AwardColorEggCoverBehavior),
                typeof(BoxCollider));
            try
            {
                GameObject visuals = new GameObject("Visuals");
                visuals.transform.SetParent(root.transform, false);
                GameObject body = new GameObject("Body", typeof(SpriteRenderer));
                body.transform.SetParent(visuals.transform, false);
                SpriteRenderer renderer = body.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
