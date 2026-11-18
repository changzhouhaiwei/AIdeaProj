using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 按语言切换 TMP 字体 / Material Preset 的配置。
/// 启动后 BuildCache，运行时 O(1) 查表。
/// </summary>
[CreateAssetMenu(menuName = "Localization/Font Config", fileName = "LocalizationFontConfig")]
public class LocalizationFontConfig : ScriptableObject
{
    [Serializable]
    public struct MaterialPair
    {
        [Tooltip("默认字体下的 Material Preset（如 Arial-Unicode-Bold-RSDF3 btn_blue）")]
        public Material fromDefault;

        [Tooltip("替换字体下的对应 Material Preset（如 MIKADO-BLACK SDF3 btn_blue）")]
        public Material toAlternate;
    }

    [Header("Fonts")]
    public TMP_FontAsset defaultFont;
    public TMP_FontAsset alternateFont;

    [Header("Languages using alternate font (I2 CurrentLanguage names)")]
    public string[] alternateLanguageNames = { "English", "French" };

    [Header("Material Preset pairs (default -> alternate)")]
    public MaterialPair[] materialPairs;

    readonly Dictionary<int, Material> _defaultToAlternate = new Dictionary<int, Material>(64);
    readonly Dictionary<int, Material> _alternateToDefault = new Dictionary<int, Material>(64);
    readonly Dictionary<string, Material> _defaultNameToAlternate = new Dictionary<string, Material>(64);
    readonly Dictionary<string, Material> _alternateNameToDefault = new Dictionary<string, Material>(64);
    readonly HashSet<string> _alternateLanguages = new HashSet<string>(StringComparer.Ordinal);
    bool _cacheBuilt;

    public void EnsureCache()
    {
        if (_cacheBuilt)
            return;
        RebuildCache();
    }

    public void RebuildCache()
    {
        _defaultToAlternate.Clear();
        _alternateToDefault.Clear();
        _defaultNameToAlternate.Clear();
        _alternateNameToDefault.Clear();
        _alternateLanguages.Clear();

        if (alternateLanguageNames != null)
        {
            foreach (var name in alternateLanguageNames)
            {
                if (!string.IsNullOrEmpty(name))
                    _alternateLanguages.Add(name);
            }
        }

        if (materialPairs != null)
        {
            foreach (var pair in materialPairs)
            {
                if (pair.fromDefault == null || pair.toAlternate == null)
                    continue;
                _defaultToAlternate[pair.fromDefault.GetInstanceID()] = pair.toAlternate;
                _alternateToDefault[pair.toAlternate.GetInstanceID()] = pair.fromDefault;
                _defaultNameToAlternate[MaterialKey(pair.fromDefault)] = pair.toAlternate;
                _alternateNameToDefault[MaterialKey(pair.toAlternate)] = pair.fromDefault;
            }
        }

        _cacheBuilt = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        _cacheBuilt = false;
    }
#endif

    public bool UsesAlternateFont(string i2LanguageName)
    {
        EnsureCache();
        return !string.IsNullOrEmpty(i2LanguageName) && _alternateLanguages.Contains(i2LanguageName);
    }

    public bool IsManagedFont(TMP_FontAsset font)
    {
        return font != null && (IsSameFont(font, defaultFont) || IsSameFont(font, alternateFont));
    }

    public bool IsSameFont(TMP_FontAsset a, TMP_FontAsset b)
    {
        if (a == null || b == null)
            return false;
        return a == b || a.name == b.name;
    }

    /// <summary>
    /// 用默认字体侧的材质查找替换材质；未配置返回 false。
    /// </summary>
    public bool TryGetAlternateMaterial(Material defaultSideMaterial, out Material alternateMaterial)
    {
        return TryMapMaterial(defaultSideMaterial, _defaultToAlternate, _defaultNameToAlternate, out alternateMaterial);
    }

    public bool TryGetDefaultMaterial(Material alternateSideMaterial, out Material defaultMaterial)
    {
        return TryMapMaterial(alternateSideMaterial, _alternateToDefault, _alternateNameToDefault, out defaultMaterial);
    }

    bool TryMapMaterial(
        Material source,
        Dictionary<int, Material> byInstance,
        Dictionary<string, Material> byName,
        out Material mapped)
    {
        EnsureCache();
        mapped = null;
        if (source == null)
            return false;
        if (byInstance.TryGetValue(source.GetInstanceID(), out mapped) && mapped != null)
            return true;
        return byName.TryGetValue(MaterialKey(source), out mapped) && mapped != null;
    }

    static string MaterialKey(Material mat)
    {
        var name = mat.name;
        const string instanceSuffix = " (Instance)";
        if (name.EndsWith(instanceSuffix, StringComparison.Ordinal))
            name = name.Substring(0, name.Length - instanceSuffix.Length);
        return name;
    }
}
