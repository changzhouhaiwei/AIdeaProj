using System.Collections.Generic;

namespace FishTilePlay
{
    /// <summary>
    /// 关卡 theme 表达式：空/"0" 走默认；"1" 强制；"0|1" 等概率候选。
    /// </summary>
    public static class ThemeExprParser
    {
        public static List<int> Parse(string expr)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(expr))
            {
                return ids;
            }

            string[] parts = expr.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i] != null ? parts[i].Trim() : "";
                if (part.Length == 0)
                {
                    continue;
                }

                if (!int.TryParse(part, out int id) || id < 0)
                {
                    continue;
                }

                if (!ids.Contains(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SelfCheck()
        {
            // ponytail: parser is the only non-trivial new logic; fail fast in editor/play.
            var empty = Parse("");
            UnityEngine.Debug.Assert(empty.Count == 0, "ThemeExprParser empty");
            var zero = Parse("0");
            UnityEngine.Debug.Assert(zero.Count == 1 && zero[0] == 0, "ThemeExprParser 0");
            var one = Parse("1");
            UnityEngine.Debug.Assert(one.Count == 1 && one[0] == 1, "ThemeExprParser 1");
            var both = Parse("0|1");
            UnityEngine.Debug.Assert(both.Count == 2 && both[0] == 0 && both[1] == 1, "ThemeExprParser 0|1");
            var spaced = Parse(" 0 | 1 ");
            UnityEngine.Debug.Assert(spaced.Count == 2 && spaced[0] == 0 && spaced[1] == 1, "ThemeExprParser trim");
        }
    }
}
