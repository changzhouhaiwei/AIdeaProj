using System;
using FishFramework;

namespace FishXSortPlay
{
    /// <summary>
    /// 经典底板从 XSortBaseStyleCatalog 读。按名字认，不用序号。
    /// </summary>
    public static class XSortBaseStyleTable
    {
        public const string CatalogPath = "Assets/CABRes/LevelScriptableObject/XSortBaseStyleCatalog.asset";
        public const string DefaultStyleName = "default";

        static XSortBaseStyleCatalog catalog;
        static bool checkedRows;

        public static int Count
        {
            get
            {
                XSortBaseStyleCatalog asset = Load();
                return asset != null && asset.styles != null ? asset.styles.Count : 0;
            }
        }

        public static int DefaultIndex => IndexOf(DefaultStyleName);

        public static XSortBaseStyleCatalog.Entry Current
        {
            get
            {
                int index = LevelDirector.LoadedXSortBaseStyle;
                return index >= 0 ? Get(index) : null;
            }
        }

        public static XSortBaseStyleCatalog.Entry Get(int index)
        {
            XSortBaseStyleCatalog asset = Load();
            if (asset == null || asset.styles == null || index < 0 || index >= asset.styles.Count)
            {
                return null;
            }

            return asset.styles[index];
        }

        public static int IndexOf(string styleName)
        {
            XSortBaseStyleCatalog asset = Load();
            if (asset == null || asset.styles == null || string.IsNullOrEmpty(styleName))
            {
                return -1;
            }

            for (int i = 0; i < asset.styles.Count; i++)
            {
                XSortBaseStyleCatalog.Entry entry = asset.styles[i];
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

        static XSortBaseStyleCatalog Load()
        {
            if (catalog != null)
            {
                return catalog;
            }

            catalog = UILoadHelper.LoadAsset<XSortBaseStyleCatalog>(CatalogPath);
            if (!checkedRows)
            {
                checkedRows = true;
                int count = catalog != null && catalog.styles != null ? catalog.styles.Count : 0;
                UnityEngine.Debug.Assert(count == 9, "XSortBaseStyleCatalog expected default + 8 styles");
            }

            return catalog;
        }
    }
}
