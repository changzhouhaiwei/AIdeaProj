using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace FishFramework
{
    public static partial class Utility
    {
        // 随机返回其中一个元素
        public static T Choose<T>(T[] chances)
        {
            return chances[Random.Range(0, chances.Length)];
        }

        public static T Choose<T>(List<T> chances)
        {
            return Choose(chances.ToArray());
        }
        
        public static bool Range(int value)
        {
            // 生成一个随机整数（包括min，但不包括max）
            return Random.Range(1, 100) <= value;
        }
    }
}