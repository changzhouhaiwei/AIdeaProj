using System.Collections.Generic;
using FishFramework;
using I2.Loc;
using TMPro;
using UnityEngine;
using xasset;

/// <summary>
/// 按 <see cref="LocalizationFontConfig"/> 在语言切换时替换托管 TMP 字体与材质。
/// 不扫 UI 树：由 I2 Localize 逐控件调用 + 注册表补充。
/// </summary>
public static class LocalizationFontSwitcher
{
    const string ConfigAssetPath = "Assets/CABRes/Fonts/LocalizationFontConfig.asset";

    static LocalizationFontConfig _config;
    static readonly HashSet<LocalizationFontTarget> Registered = new HashSet<LocalizationFontTarget>();
    static bool _listening;

    public static LocalizationFontConfig Config
    {
        get
        {
            if (_config == null && Versions.Initialized)
            {
                _config = UILoadHelper.LoadAsset<LocalizationFontConfig>(ConfigAssetPath);
                _config?.RebuildCache();
            }
#if UNITY_EDITOR
            // 独立运行 UI 场景时 xasset 可能尚未初始化，编辑器直接读取源资产。
            if (_config == null && !Versions.Initialized)
            {
                _config = UnityEditor.AssetDatabase.LoadAssetAtPath<LocalizationFontConfig>(ConfigAssetPath);
                _config?.RebuildCache();
            }
#endif
            return _config;
        }
        set
        {
            _config = value;
            _config?.RebuildCache();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureListening();
    }

    public static void EnsureListening()
    {
        if (_listening)
            return;
        LocalizationManager.OnLocalizeEvent += OnLocalizeEvent;
        FontLocalizeHook.OnTmpLocalized += OnTmpLocalized;
        _listening = true;
    }

    /// <summary>
    /// xasset 初始化完成后预加载配置，保证字体与 UI 引用来自同一套 AssetBundle 实例。
    /// </summary>
    public static bool Initialize()
    {
        EnsureListening();
        if (Config != null)
            return true;

        Debug.LogError($"LocalizationFontConfig 加载失败: {ConfigAssetPath}。请先重新构建 AssetBundle。");
        return false;
    }

    static void OnLocalizeEvent()
    {
        ApplyRegistered();
    }

    static void OnTmpLocalized(Component target)
    {
        ApplyTo(target as TMP_Text);
    }

    public static void Register(LocalizationFontTarget target)
    {
        if (target == null)
            return;
        EnsureListening();
        Registered.Add(target);
        ApplyTo(target.Text);
    }

    public static void Unregister(LocalizationFontTarget target)
    {
        if (target == null)
            return;
        Registered.Remove(target);
    }

    public static void ApplyRegistered()
    {
        if (Registered.Count == 0)
            return;

        // 避免遍历时集合被改动
        var buffer = ListPool.Get();
        foreach (var t in Registered)
            buffer.Add(t);
        for (int i = 0; i < buffer.Count; i++)
        {
            var t = buffer[i];
            if (t != null)
                ApplyTo(t.Text);
        }
        ListPool.Release(buffer);
    }

    /// <summary>对单个 TMP 应用当前语言对应的字体/材质。</summary>
    public static void ApplyTo(TMP_Text tmp)
    {
        if (tmp == null)
            return;

        var config = Config;
        if (config == null || config.defaultFont == null || config.alternateFont == null)
            return;

        config.EnsureCache();
        EnsureListening();

        var state = LocalizationFontState.GetOrCreate(tmp);
        if (!state.HasSnapshot)
        {
            if (!TryCaptureOriginal(tmp, state, config))
                return;
        }
        else
        {
            EnsureDefaultSideSnapshot(state, config);
        }

        bool useAlternate = config.UsesAlternateFont(LocalizationManager.CurrentLanguage);

        if (useAlternate)
            ApplyAlternate(tmp, state, config);
        else
            ApplyDefault(tmp, state, config);

        var after = tmp.GetComponent<IAfterLocalizationFontApply>();
        after?.OnAfterLocalizationFontApply(tmp);
    }

    /// <summary>
    /// 预制体直接挂了 alternate 字体时，首次快照可能把 Mikado 当成 original。
    /// 切回中文前必须把快照纠正到 default 侧，否则 ApplyDefault 会继续还原 Mikado。
    /// </summary>
    static void EnsureDefaultSideSnapshot(LocalizationFontState state, LocalizationFontConfig config)
    {
        if (state == null || config == null || !state.HasSnapshot)
            return;
        if (!config.IsSameFont(state.OriginalFont, config.alternateFont))
            return;

        Material defaultMat = null;
        if (state.OriginalMaterial != null)
            config.TryGetDefaultMaterial(state.OriginalMaterial, out defaultMat);
        if (defaultMat != null)
            state.Capture(config.defaultFont, defaultMat);
    }

    static void ApplyAlternate(TMP_Text tmp, LocalizationFontState state, LocalizationFontConfig config)
    {
        // SDF 字体与材质图集必须成对：显式映射优先；无 pair 时尽量保留预制体材质。
        var targetMat = ResolveAlternateMaterial(state.OriginalMaterial, state, config);
        if (targetMat == null)
        {
            ApplyDefault(tmp, state, config);
            return;
        }

        bool changed = false;
        if (tmp.font != config.alternateFont && !config.IsSameFont(tmp.font, config.alternateFont))
        {
            tmp.font = config.alternateFont;
            changed = true;
        }
        if (tmp.fontSharedMaterial != targetMat)
        {
            tmp.fontSharedMaterial = targetMat;
            changed = true;
        }

        if (changed)
            RefreshText(tmp);
    }

    static Material ResolveAlternateMaterial(Material originalMat, LocalizationFontState state, LocalizationFontConfig config)
    {
        if (originalMat == null)
            return config.alternateFont != null ? config.alternateFont.material : null;

        // 显式配置优先，便于美术精确控制。
        if (config.TryGetAlternateMaterial(originalMat, out var mapped) && mapped != null)
            return mapped;

        // 无 Material Pair：快照本身就是 alternate 侧预制体材质时，直接保留，不回退默认材质。
        if (state != null && config.IsSameFont(state.OriginalFont, config.alternateFont))
            return originalMat;

        if (config.alternateFont == null || config.alternateFont.material == null)
            return null;

        // 默认字体侧无映射：仍换到 alternate 可用默认材质（避免 Arial 材质套到 Mikado 上）。
        return config.alternateFont.material;
    }

    static void ApplyDefault(TMP_Text tmp, LocalizationFontState state, LocalizationFontConfig config)
    {
        // 中文等默认语言必须用 defaultFont。预制体若直接挂了 Mikado 且材质不在
        // materialPairs 里，快照会把 Mikado 当成 original，旧逻辑会还原西文字体导致汉字空白。
        TMP_FontAsset targetFont = config != null ? config.defaultFont : state.OriginalFont;
        Material targetMat = null;

        if (state != null && state.OriginalMaterial != null && config != null)
        {
            if (config.IsSameFont(state.OriginalFont, config.defaultFont))
                targetMat = state.OriginalMaterial;
            else
                config.TryGetDefaultMaterial(state.OriginalMaterial, out targetMat);
        }
        else if (state != null)
        {
            targetMat = state.OriginalMaterial;
        }

        if (targetMat == null && targetFont != null)
            targetMat = targetFont.material;

        bool changed = false;
        if (targetFont != null && tmp.font != targetFont && !SameFont(tmp.font, targetFont))
        {
            tmp.font = targetFont;
            changed = true;
        }
        if (targetMat != null && tmp.fontSharedMaterial != targetMat)
        {
            tmp.fontSharedMaterial = targetMat;
            changed = true;
        }

        if (changed)
            RefreshText(tmp);
    }

    static bool SameFont(TMP_FontAsset a, TMP_FontAsset b)
    {
        return a != null && b != null && (a == b || a.name == b.name);
    }

    static void RefreshText(TMP_Text tmp)
    {
        // Inspector 重新选择字体也会触发这些刷新；避免运行时引用正确但旧 mesh/UV 未更新。
        tmp.UpdateMeshPadding();
        tmp.havePropertiesChanged = true;
        tmp.SetAllDirty();
        tmp.ForceMeshUpdate(true);
    }

    /// <summary>
    /// 快照优先默认字体侧；若当前已是 alternate（例如启动即英/法），尽量反推默认材质。
    /// 反推失败（materialPairs 无对应项）时保留预制体自身的字体与材质。
    /// </summary>
    static bool TryCaptureOriginal(TMP_Text tmp, LocalizationFontState state, LocalizationFontConfig config)
    {
        if (config.IsSameFont(tmp.font, config.defaultFont))
        {
            state.Capture(config.defaultFont, tmp.fontSharedMaterial);
            return true;
        }

        if (config.IsSameFont(tmp.font, config.alternateFont))
        {
            Material defaultMat = null;
            if (tmp.fontSharedMaterial != null)
                config.TryGetDefaultMaterial(tmp.fontSharedMaterial, out defaultMat);

            if (defaultMat != null)
            {
                state.Capture(config.defaultFont, defaultMat);
            }
            else
            {
                // 无 Material Pair：保留预制体字体/材质，避免被替换成 Font Asset 默认材质。
                state.Capture(tmp.font, tmp.fontSharedMaterial);
            }
            return true;
        }

        return false;
    }

    static class ListPool
    {
        static readonly List<LocalizationFontTarget> Buffer = new List<LocalizationFontTarget>(128);

        public static List<LocalizationFontTarget> Get()
        {
            Buffer.Clear();
            return Buffer;
        }

        public static void Release(List<LocalizationFontTarget> list)
        {
            list.Clear();
        }
    }
}
