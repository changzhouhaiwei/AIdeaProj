using System.Text.RegularExpressions;

namespace FishTilePlay
{
    /// <summary>
    /// 关卡文件号。文件名后缀是文件号，不是左侧列表的 # 序号。
    /// 文件号 = 1000000 + 原来名字里的数字，例如 Android_Level_35 → Android_Level_1000035。
    /// </summary>
    public static class LevelAssetNaming
    {
        public const int FileIdBase = 1000000;

        static readonly Regex TrailingNumber = new Regex(@"(\d+)$", RegexOptions.Compiled);

        public static int ToFileId(int numberInName)
        {
            if (numberInName < 0)
            {
                numberInName = 0;
            }

            return numberInName > FileIdBase ? numberInName : FileIdBase + numberInName;
        }

        public static bool TryParseTrailingNumber(string assetNameOrPath, out int number)
        {
            number = 0;
            if (string.IsNullOrEmpty(assetNameOrPath))
            {
                return false;
            }

            string name = assetNameOrPath;
            int slash = name.LastIndexOf('/');
            if (slash >= 0)
            {
                name = name.Substring(slash + 1);
            }

            slash = name.LastIndexOf('\\');
            if (slash >= 0)
            {
                name = name.Substring(slash + 1);
            }

            if (name.EndsWith(".asset"))
            {
                name = name.Substring(0, name.Length - 6);
            }

            Match match = TrailingNumber.Match(name);
            return match.Success && int.TryParse(match.Groups[1].Value, out number);
        }

        public static int ToFileIdFromName(string assetNameOrPath)
        {
            if (!TryParseTrailingNumber(assetNameOrPath, out int number))
            {
                return FileIdBase;
            }

            return ToFileId(number);
        }
    }
}
