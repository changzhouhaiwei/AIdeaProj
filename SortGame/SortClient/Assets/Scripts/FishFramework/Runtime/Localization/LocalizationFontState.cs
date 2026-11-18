using TMPro;
using UnityEngine;

/// <summary>
/// 缓存 TMP 在默认语言下的字体/材质，便于切回非英/法时恢复。
/// 运行时按需挂到同一 GameObject，不暴露给 Inspector。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")]
public sealed class LocalizationFontState : MonoBehaviour
{
    public bool HasSnapshot { get; private set; }
    public TMP_FontAsset OriginalFont { get; private set; }
    public Material OriginalMaterial { get; private set; }

    public static LocalizationFontState GetOrCreate(TMP_Text tmp)
    {
        var state = tmp.GetComponent<LocalizationFontState>();
        if (state == null)
        {
            state = tmp.gameObject.AddComponent<LocalizationFontState>();
            state.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInInspector;
        }

        return state;
    }

    public void Capture(TMP_Text tmp)
    {
        if (tmp == null)
            return;
        Capture(tmp.font, tmp.fontSharedMaterial);
    }

    public void Capture(TMP_FontAsset font, Material material)
    {
        OriginalFont = font;
        OriginalMaterial = material;
        HasSnapshot = true;
    }
}
