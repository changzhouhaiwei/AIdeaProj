using System;
using I2.Loc;
using TMPro;
using UnityEngine;

namespace XSortGamePlay
{
    /// <summary>
    /// 挂在 TMP_Text 上。语言切换时取同物体 Text，按已注册的规则重新赋值。
    /// 赋值请用 SetText，不要直接 text.text =（否则切语言时无法还原拼接逻辑）。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("XSort/Localization/Localized TMP Refresher")]
    public class LocalizedTMPRefresher : MonoBehaviour
    {
        TMP_Text _text;
        Func<string> _getLocalizedText;

        void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            LocalizationManager.OnLocalizeEvent -= Refresh;
            LocalizationManager.OnLocalizeEvent += Refresh;
            LocalizationFontSwitcher.ApplyTo(_text != null ? _text : GetComponent<TMP_Text>());
        }

        void OnDisable()
        {
            LocalizationManager.OnLocalizeEvent -= Refresh;
        }

        /// <summary>
        /// 设置文案并绑定刷新逻辑（推荐代替 text.text =）。
        /// 未挂本组件时仅赋值，不注册。
        /// </summary>
        public static void SetText(TMP_Text target, Func<string> getLocalizedText)
        {
            if (target == null || getLocalizedText == null)
                return;

            target.text = getLocalizedText();
            EnableRichTextIfColored(target);
            LocalizationFontSwitcher.ApplyTo(target);

            var refresher = target.GetComponent<LocalizedTMPRefresher>();
            if (refresher != null)
                refresher._getLocalizedText = getLocalizedText;
        }

        /// <summary>
        /// 清掉自定义委托，回到同物体上的 I2 Localize（如结算按钮默认 NextLevel）。
        /// </summary>
        public static void RestoreLocalize(TMP_Text target)
        {
            if (target == null)
                return;

            var refresher = target.GetComponent<LocalizedTMPRefresher>();
            if (refresher != null)
                refresher._getLocalizedText = null;

            var localize = target.GetComponent<Localize>();
            if (localize != null)
                localize.OnLocalize();
            else
                LocalizationFontSwitcher.ApplyTo(target);
        }

        void Refresh()
        {
            if (_text == null)
                _text = GetComponent<TMP_Text>();
            if (_text == null)
                return;

            if (_getLocalizedText != null)
            {
                _text.text = _getLocalizedText();
                EnableRichTextIfColored(_text);
            }
            else
            {
                var localize = GetComponent<Localize>();
                if (localize != null)
                    localize.OnLocalize();
            }

            LocalizationFontSwitcher.ApplyTo(_text);
        }

        static void EnableRichTextIfColored(TMP_Text tmp)
        {
            if (tmp == null || tmp.richText || string.IsNullOrEmpty(tmp.text))
                return;
            if (tmp.text.IndexOf("<color", StringComparison.OrdinalIgnoreCase) >= 0)
                tmp.richText = true;
        }
    }
}
