using System;
using System.IO;
using UnityEngine;

namespace RopeSort
{
    public static class RopeLevelCatalog
    {
        public const int Count = 100;
        public const int Last = Count - 1;

        [Serializable]
        class FileDto
        {
            public EntryDto[] levels;
        }

        [Serializable]
        class EntryDto
        {
            public int level;
            public string raw;
        }

        static string[] raws;

        public static string GetRaw(int index)
        {
            Ensure();
            index = Mathf.Clamp(index, 0, Last);
            return raws[index];
        }

        static void Ensure()
        {
            if (raws != null)
                return;

            raws = new string[Count];
            string json = null;
            TextAsset asset = Resources.Load<TextAsset>("RopeSort/levels_100");
            if (asset != null)
                json = asset.text;

            if (string.IsNullOrEmpty(json))
            {
                string path = Path.Combine(Application.streamingAssetsPath, "RopeSort/levels_100.json");
                if (File.Exists(path))
                    json = File.ReadAllText(path);
            }

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[RopeSort] 找不到 levels_100");
                return;
            }

            FileDto file = JsonUtility.FromJson<FileDto>(json);
            if (file?.levels == null)
                return;

            for (int i = 0; i < file.levels.Length; i++)
            {
                EntryDto entry = file.levels[i];
                if (entry == null || entry.level < 1 || entry.level > Count || string.IsNullOrEmpty(entry.raw))
                    continue;
                raws[entry.level - 1] = entry.raw;
            }
        }
    }
}
