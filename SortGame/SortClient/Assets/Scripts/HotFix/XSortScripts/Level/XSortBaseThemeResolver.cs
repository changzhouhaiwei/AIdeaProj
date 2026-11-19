using System.Collections.Generic;
using XSortGameCore;

namespace FishXSortPlay
{
    /// <summary>
    /// 关卡主题只读关卡表。单个非 0（如 "1"）用该主题；空 / "0" 用 0。不轮抽，不看旧存档。
    /// 无限关用映射到的真实关卡号读同一列。
    /// </summary>
    public static class XSortBaseThemeResolver
    {
        private const int DefaultXSortBaseId = 0;

        public static void ResetSession()
        {
        }

        /// <summary>
        /// 关卡表 theme 强制结果：单值非 0 强制；"0|1" 等概率抽取（含 0）也强制。
        /// 空 / "0" 不强制。
        /// </summary>
        public static bool TryGetForcedTheme(int level, out int XSortbaseId, bool allowZero = true)
        {
            XSortbaseId = DefaultXSortBaseId;
            var cfg = LevelDynamicConfigProvider.GetLevelConfig(level);
            if (cfg == null)
            {
                return false;
            }

            List<int> ids = ThemeExprParser.Parse(string.IsNullOrEmpty(cfg.ThemeExpr) ? cfg.Theme.ToString() : cfg.ThemeExpr);
            if (ids.Count == 0)
            {
                return false;
            }

            if (ids.Count == 1)
            {
                if (ids[0] == 0)
                {
                    return false;
                }

                XSortbaseId = ids[0];
                return true;
            }

            if (!allowZero)
            {
                return false;
            }

            XSortbaseId = ids[UnityEngine.Random.Range(0, ids.Count)];
            return true;
        }

        /// <summary>
        /// 读指定 0-based 关卡的主题。只看关卡表。
        /// </summary>
        public static int PeekXSortBaseIdForLevelIndex(int levelIndex0Based)
        {
            if (!ActivityData.IsRandomXSortBaseThemeEnabled())
            {
                return DefaultXSortBaseId;
            }

            int level = levelIndex0Based + 1;
            bool infinite = GPUserDataManager.Instance != null && GPUserDataManager.Instance.GetBInfinitMode();
            int configLevel = infinite
                ? InfinitLevelFunc.Instance.GetRealLevelNumByIdx(level)
                : level;
            return ConfiguredThemeId(configLevel);
        }

        /// <summary>当前关卡主题。只看关卡表。</summary>
        public static int ResolveCurrentLevelXSortBaseId()
        {
            GPUserDataManager mgr = GPUserDataManager.Instance;
            if (mgr == null || !ActivityData.IsRandomXSortBaseThemeEnabled())
            {
                return DefaultXSortBaseId;
            }

            if (mgr.GetBInfinitMode())
            {
                int infiniteLevel = mgr.GetInfinitLevelNum() + 1;
                return ConfiguredThemeId(InfinitLevelFunc.Instance.GetRealLevelNumByIdx(infiniteLevel));
            }

            return ConfiguredThemeId(mgr.GetShowLevelNum() + 1);
        }

        /// <summary>表里写死单个非 0 主题才算主题关，否则 0。</summary>
        static int ConfiguredThemeId(int configLevel)
        {
            return TryGetPinnedTheme(configLevel, out int pinned) ? pinned : DefaultXSortBaseId;
        }

        static bool TryGetPinnedTheme(int level, out int XSortbaseId)
        {
            XSortbaseId = DefaultXSortBaseId;
            var cfg = LevelDynamicConfigProvider.GetLevelConfig(level);
            if (cfg == null)
            {
                return false;
            }

            List<int> ids = ThemeExprParser.Parse(string.IsNullOrEmpty(cfg.ThemeExpr) ? cfg.Theme : cfg.ThemeExpr);
            if (ids.Count != 1 || ids[0] == 0)
            {
                return false;
            }

            XSortbaseId = ids[0];
            return true;
        }
    }
}
