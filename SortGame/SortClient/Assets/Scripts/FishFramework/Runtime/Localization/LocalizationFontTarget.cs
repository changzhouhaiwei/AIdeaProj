using TMPro;
using UnityEngine;

/// <summary>
/// 挂在无 I2 Localize、但仍需随语言换字体的 TMP 上。
/// OnEnable 注册进列表，切语言时只扫注册表，不做 UI 树查找。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class LocalizationFontTarget : MonoBehaviour
{
    TMP_Text _text;

    public TMP_Text Text => _text != null ? _text : (_text = GetComponent<TMP_Text>());

    void OnEnable()
    {
        LocalizationFontSwitcher.Register(this);
    }

    void OnDisable()
    {
        LocalizationFontSwitcher.Unregister(this);
    }
}
