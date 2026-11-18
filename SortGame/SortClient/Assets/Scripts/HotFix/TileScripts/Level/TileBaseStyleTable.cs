using System;
using FishFramework;

namespace FishTilePlay
{
    /// <summary>
    /// 经典底板从 TileBaseStyleCatalog 读。按名字认，不用序号。
    /// </summary>
    public static class TileBaseStyleTable
    {
        public const string CatalogPath = "Assets/CABRes/LevelScriptableObject/TileBaseStyleCatalog.asset";
        public const string DefaultStyleName = "default";

        static TileBaseStyleCatalog catalog;
        static bool checkedRows;

        public static int Count
        {
            get
            {
                TileBaseStyleCatalog asset = Load();
                return asset != null && asset.styles != null ? asset.styles.Count : 0;
            }
        }

        public static int DefaultIndex => IndexOf(DefaultStyleName);

        public static TileBaseStyleCatalog.Entry Current
        {
            get
            {
                int index = LevelDirector.LoadedTileBaseStyle;
                return index >= 0 ? Get(index) : null;
            }
        }

        public static TileBaseStyleCatalog.Entry Get(int index)
        {
            TileBaseStyleCatalog asset = Load();
            if (asset == null || asset.styles == null || index < 0 || index >= asset.styles.Count)
            {
                return null;
            }

            return asset.styles[index];
        }

        public static int IndexOf(string styleName)
        {
            TileBaseStyleCatalog asset = Load();
            if (asset == null || asset.styles == null || string.IsNullOrEmpty(styleName))
            {
                return -1;
            }

            for (int i = 0; i < asset.styles.Count; i++)
            {
                TileBaseStyleCatalog.Entry entry = asset.styles[i];
                if (entry != null && string.Equals(entry.styleName, styleName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public static string NameAt(int index)
        {
            return Get(index)?.styleName;
        }

        static TileBaseStyleCatalog Load()
        {
            if (catalog != null)
            {
                return catalog;
            }

            catalog = UILoadHelper.LoadAsset<TileBaseStyleCatalog>(CatalogPath);
            if (!checkedRows)
            {
                checkedRows = true;
                int count = catalog != null && catalog.styles != null ? catalog.styles.Count : 0;
                UnityEngine.Debug.Assert(count == 9, "TileBaseStyleCatalog expected default + 8 styles");
            }

            return catalog;
        }
    }
}
