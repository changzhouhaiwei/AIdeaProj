using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 预跑种子：关卡号 × 1000000 + 档位 × 10000 + 局号。
    /// 档位 0–4 对应 defaultCfg、win1–win4。
    /// 2148 关起超过 int，种子用 long。2147 关及以前的随机序列保持原来的 int 种子。
    /// </summary>
    public static class LevelDifficultySeed
    {
        public const int LevelScale = 1000000;
        public const int SlotScale = 10000;
        public const int SlotCount = 5;

        static readonly string[] SlotNames = { "defaultCfg", "win1", "win2", "win3", "win4" };

        public static int PendingInitState;
        public static bool IsReplayLoad;

        public static string SlotName(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotNames.Length)
                return "?";
            return SlotNames[slotIndex];
        }

        public static long Encode(int levelId, int slotIndex, int runIndex)
        {
            return (long)levelId * LevelScale + (long)slotIndex * SlotScale + runIndex;
        }

        public static bool TryDecode(long seed, out int levelId, out int slotIndex, out int runIndex)
        {
            levelId = 0;
            slotIndex = 0;
            runIndex = 0;
            if (seed < LevelScale)
                return false;

            levelId = (int)(seed / LevelScale);
            long rest = seed - (long)levelId * LevelScale;
            slotIndex = (int)(rest / SlotScale);
            runIndex = (int)(rest - (long)slotIndex * SlotScale);
            return levelId >= 1 && slotIndex >= 0 && slotIndex < SlotCount && runIndex >= 0;
        }

        public static void SetPending(long seed)
        {
            int state = ToRandomState(seed);
            PendingInitState = state == 0 ? 1 : state;
        }

        public static int ToRandomState(long seed)
        {
            if (seed > 0 && seed <= int.MaxValue)
                return (int)seed;

            unchecked
            {
                ulong x = (ulong)seed;
                return (int)(x ^ (x >> 32));
            }
        }

        public static void ApplyPendingInitState()
        {
            if (PendingInitState == 0)
                return;

            Random.InitState(PendingInitState);
            PendingInitState = 0;
        }
    }
}
