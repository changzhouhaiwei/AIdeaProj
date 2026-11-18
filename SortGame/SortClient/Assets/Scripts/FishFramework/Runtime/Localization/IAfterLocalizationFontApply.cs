using TMPro;

/// <summary>
/// LocalizationFontSwitcher.ApplyTo 完成后再回调，避免其它 OnLocalize 监听把字体改回去。
/// </summary>
public interface IAfterLocalizationFontApply
{
    void OnAfterLocalizationFontApply(TMP_Text tmp);
}
