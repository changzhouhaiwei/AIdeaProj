using System;
using System.Collections.Generic;
using System.Linq;

namespace FishFramework
{
    public static partial class Utility
    {
        public static List<T> GetRandomValues<T>(List<T> list, int count)
        {
            if (count > list.Count)
            {
                return list;
            }

            Random random = new Random();
            List<T> randomValues = list.OrderBy(x => random.Next()).Take(count).ToList();

            return randomValues;
        }
    }
}