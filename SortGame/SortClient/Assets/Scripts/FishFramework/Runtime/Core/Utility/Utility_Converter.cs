using System;
using System.Globalization;

namespace FishFramework
{
    public static partial class Utility
    {
        /// <summary>
        /// 将双精度浮点数转为带有K、M、G的简化字符串，并保留两位小数
        /// </summary>
        /// <param name="number"></param>
        /// <param name="decimalPlaces"></param>
        /// <returns></returns>
        public static string FormatNumberToKMG(double number, int decimalPlaces = 2)
        {
            double formattedNumber = ConvertToKMG(number, out string unit, decimalPlaces);
            // if (formattedNumber % 1 == 0)
            // {
            //     return formattedNumber.ToString("F0") + unit;
            // }

            return formattedNumber.ToString(CultureInfo.InvariantCulture) + unit;
        }

        /// <summary>
        /// 将双精度浮点数转为带有K、M、G的简化字符串，并保留两位小数
        /// </summary>
        /// <param name="data">双精度浮点数</param>
        /// <param name="unit"></param>
        /// <param name="decimalPlaces"></param>
        /// <returns>简化字符串</returns>
        private static double ConvertToKMG(double data, out string unit, int decimalPlaces = 2)
        {
            const int numK = 1000;
            if (Math.Abs(data) < numK)
            {
                unit = "";
                return Math.Round(data, decimalPlaces);
            }

            if (Math.Abs(data) < (Math.Pow(numK, 2)))
            {
                unit = "K";
                return Math.Round((data / numK), decimalPlaces); //kb
            }

            if (Math.Abs(data) < Math.Pow(numK, 3))
            {
                unit = "M";
                return Math.Round((data / Math.Pow(numK, 2)), decimalPlaces); //M
            }

            if (Math.Abs(data) < Math.Pow(numK, 4))
            {
                unit = "G";
                return Math.Round((data / Math.Pow(numK, 3)), decimalPlaces); //G
            }

            unit = "T";
            return Math.Round((data / Math.Pow(numK, 4)), decimalPlaces); //T
        }

        public static string GetMinSec(long timestamp)
        {
            // 将时间戳转换为 TimeSpan
            TimeSpan timeSpan = TimeSpan.FromSeconds(timestamp);

            int minutes = (int)timeSpan.TotalMinutes;
            int seconds = timeSpan.Seconds;

            // 将时间格式化为 "mm:ss" 字符串
            string formattedTime = minutes.ToString("D2") + ":" + seconds.ToString("D2");
            return formattedTime;
        }

        public static string GetHourMinSec(long timestamp)
        {
            // 将时间戳转换为 TimeSpan
            TimeSpan timeSpan = TimeSpan.FromSeconds(timestamp);

            // 计算总小时数，包括天数
            int hours = (int)timeSpan.TotalHours;
            int minutes = timeSpan.Minutes;
            int seconds = timeSpan.Seconds;

            // 将时间格式化为 "HH:mm:ss" 字符串
            string formattedTime = hours.ToString("D2") + ":" + minutes.ToString("D2") + ":" + seconds.ToString("D2");
            return formattedTime;
        }

        /// <summary>
        /// 如果大于一天只显示天，否则是00:00:00
        /// </summary>
        public static string GetDayOrHourMinSec(long timestamp, string format)
        {
            long daySec = 24 * 60 * 60;
            if (timestamp > daySec)
            {
                long day = timestamp / daySec;
                return string.Format(format, day);
            }
            else
            {
                return GetHourMinSec(timestamp);
            }
        }

        /// <summary>
        /// 只显示天
        /// </summary>
        public static string GetDay(long timestamp, string format)
        {
            long daySec = 24 * 60 * 60;
            if (timestamp > daySec)
            {
                long day = timestamp / daySec;
                return string.Format(format, day);
            }

            return string.Format(format, 0);
        }

        /// <summary>
        /// 钱字前缀
        /// </summary>
        public static string MoneyUI(float str)
        {
            return $"￥{str}";
        }

        //combo数字使用 spriteasset完成.,数字转换.
        public static string ConvertSpriteAssetNumber(int number)
        {
            string numStr = number.ToString();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // 前面加 x 号 sprite
            sb.Append("<sprite=10 tint=1>");

            for (int i = 0; i < numStr.Length; i++)
            {
                int digit = numStr[i] - '0';
                sb.Append($"<sprite={digit} tint=1>");
            }

            return sb.ToString();
        }
    }
}