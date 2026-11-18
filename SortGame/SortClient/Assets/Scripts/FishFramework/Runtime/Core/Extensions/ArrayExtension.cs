using System.Collections.Generic;

namespace FishFramework
{
    public static class ArrayExtension
    {
        // 返回二维数组的列表
        public static List<T> GetList<T>(this T[,] arr)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < arr.GetLength(0); ++i)
            {
                for (int j = 0; j < arr.GetLength(1); ++j)
                {
                    if (arr[i, j] != null)
                    {
                        list.Add(arr[i, j]);
                    }
                }
            }

            return list;
        }
    }
}