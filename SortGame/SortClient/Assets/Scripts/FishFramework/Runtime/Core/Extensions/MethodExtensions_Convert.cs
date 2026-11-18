using System;
using UnityEngine;

namespace Core.Tools.MethodExtension
{
    public static partial class MethodExtensions
    {
        #region Base
        public static bool ToBool(this string pString)
        {
            if (string.IsNullOrEmpty(pString))
            {
                return false;
            }

            return pString.Equals("1") || pString.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
        }

        public static int ToInt(this string pString)
        {
            if (!int.TryParse(pString, out int tResult))
            {
                Debug.LogError($"ToInt Failed! —— {pString}");
            }
            return tResult;
        }

        public static long ToLong(this string pString)
        {
            if (!long.TryParse(pString, out long tResult))
            {
                Debug.LogError($"ToLong Failed! —— {pString}");
            }
            return tResult;
        }

        public static float ToFloat(this string pString)
        {
            if (!float.TryParse(pString, out float tResult))
            {
                Debug.LogError($"ToFloat Failed! —— {pString}");
            }
            return tResult;
        }

        public static double ToDouble(this string pString)
        {
            if (!double.TryParse(pString, out double tResult))
            {
                Debug.LogError($"ToDouble Failed! —— {pString}");
            }
            return tResult;
        }

        public static string[] ToStringArray(this string pString, char pSplit = ',')
        {
            return string.IsNullOrEmpty(pString) ? new string[0] : pString.Split(pSplit);
        }

        public static bool[] ToBoolArray(this string pString, char pSplit = ',')
        {
            var tSplitedStrings = pString.ToStringArray(pSplit);
            var tResult = new bool[tSplitedStrings.Length];
            for (int i = 0; i < tSplitedStrings.Length; i++)
            {
                tResult[i] = tSplitedStrings[i].ToBool();
            }
            return tResult;
        }

        public static int[] ToIntArray(this string pString, char pSplit = ',')
        {
            var tSplitedStrings = pString.ToStringArray(pSplit);
            var tResult = new int[tSplitedStrings.Length];
            for (int i = 0; i < tSplitedStrings.Length; i++)
            {
                tResult[i] = tSplitedStrings[i].ToInt();
            }
            return tResult;
        }

        public static long[] ToLongArray(this string pString, char pSplit = ',')
        {
            var tSplitedStrings = pString.ToStringArray(pSplit);
            var tResult = new long[tSplitedStrings.Length];
            for (int i = 0; i < tSplitedStrings.Length; i++)
            {
                tResult[i] = tSplitedStrings[i].ToLong();
            }
            return tResult;
        }

        public static float[] ToFloatArray(this string pString, char pSplit = ',')
        {
            var tSplitedStrings = pString.ToStringArray(pSplit);
            var tResult = new float[tSplitedStrings.Length];
            for (int i = 0; i < tSplitedStrings.Length; i++)
            {
                tResult[i] = tSplitedStrings[i].ToFloat();
            }
            return tResult;
        }

        public static double[] ToDoubleArray(this string pString, char pSplit = ',')
        {
            var tSplitedStrings = pString.ToStringArray(pSplit);
            var tResult = new double[tSplitedStrings.Length];
            for (int i = 0; i < tSplitedStrings.Length; i++)
            {
                tResult[i] = tSplitedStrings[i].ToDouble();
            }
            return tResult;
        }
        #endregion

        #region Enum
        public static int ToInt<T>(this T pEnum) where T : Enum => Convert.ToInt32(pEnum);

        public static TEnum ToEnum<TEnum>(this string pString, bool pLog = true) where TEnum : struct
        {
            if (!Enum.TryParse<TEnum>(pString, out var tResult))
            {
                if (pLog)
                {
                    Debug.LogError($"ToEnum<{typeof(TEnum).Name}> Failed! —— {pString}");
                }
            }
            return tResult;
        }
        #endregion
      
        #region Vector
        public static Vector3 ToVector(this string pString)
        {
            if (string.IsNullOrEmpty(pString))
            {
                return Vector3.zero;
            }

            pString = pString.Replace("(", "").Replace(")", "");
            var tPosition = pString.ToFloatArray();
            return new Vector3(tPosition.Length > 0 ? tPosition[0] : 0,
                               tPosition.Length > 1 ? tPosition[1] : 0,
                               tPosition.Length > 2 ? tPosition[2] : 0);
        }
        #endregion
    }
}