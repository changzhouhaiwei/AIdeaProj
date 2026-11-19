using System;
using System.Collections.Generic;
using FishFramework;
using UnityEngine;

namespace FishXSortPlay
{
    [Serializable]
    public sealed class AwardColorEggCoverRewardRow
    {
        public float rate;
        public string a;
        public string b;
        public string c;
    }

    [Serializable]
    public sealed class AwardColorEggCoverConfigFile
    {
        public int[] levels;
    }

    public readonly struct AwardColorEggCoverReward
    {
        public readonly int ItemId;
        public readonly int Count;
        public readonly bool Lucky;

        public AwardColorEggCoverReward(int itemId, int count, bool lucky)
        {
            ItemId = itemId;
            Count = count;
            Lucky = lucky;
        }

        public string CountLabel => "x" + Count;
    }

    public static class AwardColorEggCoverConfig
    {
        public const string ConfigPath = "Assets/CABRes/Configs/award_color_egg_cover.json";
        public const int EggCount = 3;

        static bool loaded;
        static bool levelsFromTable;
        static readonly HashSet<int> Levels = new();
        static readonly List<AwardColorEggCoverRewardRow> Rows = new();

        public static bool IsEnabled(int levelId)
        {
            EnsureLoaded();
            return levelId > 0 && Levels.Contains(levelId);
        }

        public static bool IsEnabledForLoadedLevel()
        {
            return IsEnabled(LevelDirector.LoadedLevelIndex + 1);
        }

        public static AwardColorEggCoverReward[] RollRewards()
        {
            return RollRewards(out _);
        }

        public static AwardColorEggCoverReward[] RollRewards(out int groupId)
        {
            cfg.Game.CSVColorEggReward row = PickConfigRow();
            groupId = row != null ? row.Id : 0;
            AwardColorEggCoverReward[] rewards =
            {
                RollSlot(row != null ? row.Rewards1 : "4_10"),
                RollSlot(row != null ? row.Rewards2 : "4_20"),
                RollSlot(row != null ? row.Rewards3 : "4_30"),
            };
            Shuffle(rewards);
            return rewards;
        }

        static cfg.Game.CSVColorEggReward PickConfigRow()
        {
            var list = ConfigHelper.Tables?.TbCSVColorEggReward?.DataList;
            if (list == null || list.Count == 0)
                return null;

            float total = 0f;
            for (int i = 0; i < list.Count; i++)
                total += Mathf.Max(0f, list[i].Passrate);
            if (total <= 0f)
                return list[0];

            float roll = UnityEngine.Random.value * total;
            float cursor = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                cursor += Mathf.Max(0f, list[i].Passrate);
                if (roll <= cursor)
                    return list[i];
            }

            return list[list.Count - 1];
        }

        static void EnsureLoaded()
        {
            if (!loaded)
            {
                loaded = true;
                ApplyDefaults();
                // JSON 仅作无表时的兜底；正式关卡列表以 TbCSVColorEggLevel 为准
                TextAsset asset = UILoadHelper.LoadAsset<TextAsset>(ConfigPath);
#if UNITY_EDITOR
                if (asset == null)
                    asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigPath);
#endif
                if (asset != null && !string.IsNullOrEmpty(asset.text))
                {
                    AwardColorEggCoverConfigFile file =
                        JsonUtility.FromJson<AwardColorEggCoverConfigFile>(asset.text);
                    if (file?.levels != null && file.levels.Length > 0)
                    {
                        Levels.Clear();
                        for (int i = 0; i < file.levels.Length; i++)
                        {
                            if (file.levels[i] > 0)
                                Levels.Add(file.levels[i]);
                        }
                    }
                }

                if (Levels.Count == 0)
                    Levels.Add(9);

                LoadRewardRows();
            }

            // 表可能比首次 IsEnabled 更晚就绪；未吃到表之前每次重试，避免卡在调试 JSON
            if (!levelsFromTable)
                LoadLevelsFromTable();
        }

        static void LoadLevelsFromTable()
        {
            ConfigHelper.EnsureInitialized();
            var list = ConfigHelper.Tables?.TbCSVColorEggLevel?.DataList;
            if (list == null || list.Count == 0)
                return;

            Levels.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Level > 0)
                    Levels.Add(list[i].Level);
            }

            levelsFromTable = Levels.Count > 0;
        }

        static void LoadRewardRows()
        {
            ConfigHelper.EnsureInitialized();
            var list = ConfigHelper.Tables?.TbCSVColorEggReward?.DataList;
            if (list == null || list.Count == 0)
            {
                ApplyDefaultRows();
                return;
            }

            Rows.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                cfg.Game.CSVColorEggReward row = list[i];
                if (row == null || row.Passrate <= 0f)
                    continue;

                Rows.Add(new AwardColorEggCoverRewardRow
                {
                    rate = row.Passrate,
                    a = row.Rewards1,
                    b = row.Rewards2,
                    c = row.Rewards3,
                });
            }

            if (Rows.Count == 0)
                ApplyDefaultRows();
        }

        static void ApplyDefaults()
        {
            Levels.Clear();
            Levels.Add(9);
            ApplyDefaultRows();
        }

        static void ApplyDefaultRows()
        {
            Rows.Clear();
            Rows.Add(new AwardColorEggCoverRewardRow { rate = 0.1f, a = "4_10", b = "4_10", c = "4_20" });
            Rows.Add(new AwardColorEggCoverRewardRow { rate = 0.4f, a = "4_10", b = "4_20", c = "4_30" });
            Rows.Add(new AwardColorEggCoverRewardRow { rate = 0.4f, a = "4_10", b = "4_20", c = "4_50,1_1" });
            Rows.Add(new AwardColorEggCoverRewardRow { rate = 0.1f, a = "4_30", b = "4_50", c = "1_1,3_1,2_1" });
        }

        static AwardColorEggCoverRewardRow PickRow()
        {
            if (Rows.Count == 0)
                return null;

            float total = 0f;
            for (int i = 0; i < Rows.Count; i++)
                total += Mathf.Max(0f, Rows[i].rate);
            if (total <= 0f)
                return Rows[0];

            float roll = UnityEngine.Random.value * total;
            float cursor = 0f;
            for (int i = 0; i < Rows.Count; i++)
            {
                cursor += Mathf.Max(0f, Rows[i].rate);
                if (roll <= cursor)
                    return Rows[i];
            }

            return Rows[Rows.Count - 1];
        }

        static AwardColorEggCoverReward RollSlot(string slot)
        {
            string[] options = string.IsNullOrEmpty(slot)
                ? new[] { "4_10" }
                : slot.Split(',');
            string chosen = options[UnityEngine.Random.Range(0, options.Length)].Trim();
            int split = chosen.IndexOf('_');
            int itemId = 4;
            int count = 10;
            if (split > 0)
            {
                int.TryParse(chosen.Substring(0, split), out itemId);
                int.TryParse(chosen.Substring(split + 1), out count);
            }

            if (itemId <= 0)
                itemId = 4;
            if (count <= 0)
                count = 1;
            return new AwardColorEggCoverReward(itemId, count, itemId != 4);
        }

        static void Shuffle(AwardColorEggCoverReward[] rewards)
        {
            for (int i = rewards.Length - 1; i > 0; i--)
            {
                int swap = UnityEngine.Random.Range(0, i + 1);
                AwardColorEggCoverReward temp = rewards[i];
                rewards[i] = rewards[swap];
                rewards[swap] = temp;
            }
        }
    }

    public static class AwardColorEggCoverText
    {
        public static string Get(string key, string english, string chinese)
        {
            string value = I2LocalizeMgr.GetString(key);
            if (!string.IsNullOrEmpty(value) && value != key)
                return value;

            string language = I2LocalizeMgr.GetLanguage();
            if (!string.IsNullOrEmpty(language)
                && (language.IndexOf("Chinese", StringComparison.OrdinalIgnoreCase) >= 0
                    || language.IndexOf("中文", StringComparison.Ordinal) >= 0))
            {
                return chinese;
            }

            return english;
        }

        public static string BonusButton()
        {
            return Get("button_BonusLevel", "Bonus Level", "奖励关");
        }
    }
}
