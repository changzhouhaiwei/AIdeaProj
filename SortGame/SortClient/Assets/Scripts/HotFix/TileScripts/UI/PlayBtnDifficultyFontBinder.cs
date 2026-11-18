using TMPro;
using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// 独立 MonoBehaviour，供 LocalizationFontSwitcher.GetComponent 接口回调找到。
    /// 嵌套在 static 类里时 AOT/HybridCLR 下经常取不到，语言切换会把难度材质改回去。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class PlayBtnDifficultyFontBinder : MonoBehaviour, IAfterLocalizationFontApply
    {
        TMP_Text _text;
        int _difficulty;
        bool _restoreOriginal;
        bool _isCoinLevel;

        public void Configure(TMP_Text text, int difficulty, bool restoreOriginal, bool isCoinLevel)
        {
            _text = text;
            _difficulty = difficulty;
            _restoreOriginal = restoreOriginal;
            _isCoinLevel = isCoinLevel;
        }

        public void OnAfterLocalizationFontApply(TMP_Text tmp)
        {
            if (_restoreOriginal)
                return;
            if (tmp != null)
                _text = tmp;
            ApplyNow();
        }

        public void ApplyNow()
        {
            PlayBtnDifficultyFont.ApplyConfigured(_text, _difficulty, _restoreOriginal, _isCoinLevel);
        }
    }
}
