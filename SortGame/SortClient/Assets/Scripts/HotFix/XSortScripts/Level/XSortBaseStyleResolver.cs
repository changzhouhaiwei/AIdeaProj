using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 经典底板。有限关 1–20 用 default。21 关起在新开一局时抽一轮里的下一个。
    /// 失败重开、新关、没操作再进会抽；本局已经操作过再进来沿用原来的底板。
    /// </summary>
    public static class XSortBaseStyleResolver
    {
        public const int UseThemeBase = -1;
        const int FiniteRandomStartLevel = 21;

        public static int ResolveForCurrentLevel()
        {
            if (!ActivityData.IsClassicRandomXSortBaseEnabled())
            {
                return UseThemeBase;
            }

            GPUserDataManager mgr = GPUserDataManager.Instance;
            GPUserData user = mgr?.UserData;
            if (mgr == null || user == null)
            {
                return UseThemeBase;
            }

            bool infinite = mgr.GetBInfinitMode();
            int level = CurrentLevel(mgr, infinite);
            bool themed = PlayedLevelHasTheme(level, infinite);
            if (!infinite && level < FiniteRandomStartLevel)
            {
                return themed ? UseThemeBase : XSortBaseStyleTable.DefaultIndex;
            }

            if (themed && !user.TryGetXSortBaseStyle(level, infinite, out _, out _))
            {
                user.SetXSortBaseStyle(level, infinite, UseThemeBase, false);
                return UseThemeBase;
            }

            return Draw(user.XSortBaseBag(infinite), user, infinite);
        }

        public static void MarkCurrentLevelFailed()
        {
        }

        public static void CancelCurrentLevelReroll()
        {
        }

        static int Draw(XSortBaseStyleBag bag, GPUserData user, bool infinite)
        {
            int count = XSortBaseStyleTable.Count;
            if (count <= 0 || bag == null)
            {
                return UseThemeBase;
            }

            List<string> names = Names();
            bool opened = bag.remaining == null || bag.remaining.Count == 0;
            if (opened)
            {
                bag.remaining = new List<string>(names);
            }
            else
            {
                for (int i = 0; i < names.Count; i++)
                {
                    string name = names[i];
                    if (!Contains(bag.knownNames, name) && !Contains(bag.remaining, name))
                    {
                        bag.remaining.Add(name);
                    }
                }
            }

            bag.remaining.RemoveAll(name => XSortBaseStyleTable.IndexOf(name) < 0);
            if (bag.remaining.Count == 0)
            {
                bag.remaining = new List<string>(names);
                opened = true;
            }

            List<string> fresh = new List<string>();
            for (int i = 0; i < bag.remaining.Count; i++)
            {
                if (!Contains(bag.knownNames, bag.remaining[i]))
                {
                    fresh.Add(bag.remaining[i]);
                }
            }

            List<string> pool = fresh.Count > 0 ? fresh : bag.remaining;
            if (opened && pool.Count > 1 && Contains(pool, bag.lastDrawn))
            {
                pool = new List<string>(pool);
                pool.RemoveAll(name => string.Equals(name, bag.lastDrawn, StringComparison.OrdinalIgnoreCase));
            }

            string picked = pool[UnityEngine.Random.Range(0, pool.Count)];
            bag.remaining.RemoveAll(name => string.Equals(name, picked, StringComparison.OrdinalIgnoreCase));
            if (!Contains(bag.knownNames, picked))
            {
                bag.knownNames.Add(picked);
            }

            bag.lastDrawn = picked;
            user.TouchXSortBaseStyleBag(infinite);
            return XSortBaseStyleTable.IndexOf(picked);
        }

        static List<string> Names()
        {
            var names = new List<string>(XSortBaseStyleTable.Count);
            for (int i = 0; i < XSortBaseStyleTable.Count; i++)
            {
                string name = XSortBaseStyleTable.NameAt(i);
                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(name);
                }
            }

            return names;
        }

        static bool Contains(List<string> list, string name)
        {
            if (list == null || string.IsNullOrEmpty(name))
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        static bool PlayedLevelHasTheme(int level, bool infinite)
        {
            int configLevel = infinite ? InfinitLevelFunc.Instance.GetRealLevelNumByIdx(level) : level;
            return ActivityData.IsRandomXSortBaseThemeEnabled()
                && XSortBaseThemeResolver.TryGetForcedTheme(configLevel, out _);
        }

        static int CurrentLevel(GPUserDataManager mgr, bool infinite)
        {
            int level = infinite ? mgr.GetInfinitLevelNum() + 1 : mgr.GetShowLevelNum() + 1;
            return level < 1 ? 1 : level;
        }
    }
}
