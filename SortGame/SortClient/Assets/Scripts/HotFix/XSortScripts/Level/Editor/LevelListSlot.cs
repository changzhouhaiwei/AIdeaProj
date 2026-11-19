#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 和关卡编辑器右侧打开的是同一关：名单文件名和数据库引用一致时用引用，否则用进关名单里的资源。
    /// </summary>
    public static class LevelListSlot
    {
        public static LevelModel Resolve(LevelModel referenced, int index)
        {
            string playPath = LevelChunkLoader.GetLevelPath(index);
            if (string.IsNullOrEmpty(playPath)
                || playPath.IndexOf("__missing_play_order_", System.StringComparison.Ordinal) >= 0)
            {
                return referenced;
            }

            string playName = Path.GetFileNameWithoutExtension(playPath);
            if (referenced != null && string.Equals(referenced.name, playName, System.StringComparison.Ordinal))
            {
                return referenced;
            }

            LevelModel listed = AssetDatabase.LoadAssetAtPath<LevelModel>(playPath);
            return listed != null ? listed : referenced;
        }
    }
}
#endif
