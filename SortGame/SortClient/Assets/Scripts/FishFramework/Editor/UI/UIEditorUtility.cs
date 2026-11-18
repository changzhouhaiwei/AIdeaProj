using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FishFramework
{
    public class UIEditorUtility
    {
        public const string kUIBaseCode = @"using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;
using SuperScrollView;

public class ${ClassName} : ${BaseClassName}
{
    ${PanelLayer}
    ${PrefabPath}
    ${VariantsDefine}
    protected override void OnBindCompsAndEvents() 
    {${BindComps}${BindEvents}}

    protected override void OnUnbindCompsAndEvents() 
    {${UnbindEvents}${UnbindComps}}
}";

        public const string kUITemporaryCode = @"using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishFramework;

public class ${ClassName} : ${BaseClassName}
{
    protected override void OnCreating() { }

    protected override void OnDestroyed() { }

    protected override void OnOpen() { }

    protected override void OnClicked(Button button) { }

    private void OnReceiveMessage(NetData netData) { }
    ${PanelLifeCycleCode}
}";

        public const string kPanelLifeCycleCode = @"
    protected override void OnVisibleChanged(bool visible) { }
";

        public static void GenerateCode(string savePath, string content)
        {
            string saveDir = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }

            File.WriteAllText(savePath, content, Encoding.UTF8);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string scriptAssetPath = Path.Combine("Assets", Path.GetRelativePath(Application.dataPath, savePath));
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(scriptAssetPath));
        }

        /// <summary>
        /// Prefab 在 UICommon，脚本惯例落在 CommonUI（GeneratedBaseUI / UIModules）。
        /// </summary>
        public static string RemapGeneratedScriptSubPath(string subPath)
        {
            if (string.IsNullOrEmpty(subPath))
            {
                return subPath;
            }

            string normalized = subPath.Replace('\\', '/');
            const string from = "UICommon";
            const string to = "CommonUI";
            if (normalized.Equals(from, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(from + "/", StringComparison.OrdinalIgnoreCase))
            {
                return to + normalized.Substring(from.Length);
            }

            return subPath;
        }

        /// <summary>
        /// 保留字母数字下划线、首字母大写
        /// </summary>
        /// <param name="goName"></param>
        /// <returns></returns>
        public static string GetFormatedGoName(string goName)
        {
            goName = Regex.Replace(goName, @"[^a-zA-Z0-9_]", "");
            goName = Regex.Replace(goName, @"^\w", t => t.Value.ToLower());
            return goName;
        }

        /// <summary>
        /// 取组件短名
        /// 将过长的缩写即可，默认使用原名
        /// （主要是太长且多个单词的）
        /// </summary>
        /// <returns></returns>
        public static string GetCompShortName(string compName)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                { "VerticalLayoutGroup", "VLayoutGroup" },
                { "HorizontalLayoutGroup", "HLayoutGroup" },
                { "GridLayoutGroup", "GLayoutGroup" },

                { "TextMeshProUGUI", "TMPText" },
                { "TMP_Dropdown", "TMPDropdown" },
                { "TMP_InputField", "TMPInputField" },
            };

            return dict.ContainsKey(compName) ? dict[compName] : compName;
        }

        public static Texture GetIconByType(Type type)
        {
            //系统内置图标
            Texture systemIcon = EditorGUIUtility.ObjectContent(null, type).image;

            if (type == typeof(TButton))
            {
                systemIcon = EditorGUIUtility.ObjectContent(null, typeof(UnityEngine.UI.Button)).image;
            }
            else if (type == typeof(TToggle))
            {
                systemIcon = EditorGUIUtility.ObjectContent(null, typeof(UnityEngine.UI.Toggle)).image;
            }

            //自定义组件图标 
            Texture customIcon = null;

            //TMP 三个组件的图标
            if (type == typeof(TMPro.TMP_InputField))
            {
                customIcon =
                    (Texture2D)EditorGUIUtility.Load(
                        "Packages/com.unity.textmeshpro/Editor Resources/Gizmos/TMP - Input Field Icon.psd");
            }
            else if (type == typeof(TMPro.TMP_Dropdown))
            {
                customIcon =
                    (Texture2D)EditorGUIUtility.Load(
                        "Packages/com.unity.textmeshpro/Editor Resources/Gizmos/TMP - Dropdown Icon.psd");
            }
            else if (type == typeof(TMPro.TextMeshProUGUI))
            {
                customIcon =
                    (Texture2D)EditorGUIUtility.Load(
                        "Packages/com.unity.textmeshpro/Editor Resources/Gizmos/TMP - Text Component Icon.psd");
            }

            //todo其他自定义图标自行添加

            //默认图标
            Texture csScriptIcon = EditorGUIUtility.IconContent("cs Script Icon").image;

            return systemIcon ?? customIcon ?? csScriptIcon;
        }
    }
}