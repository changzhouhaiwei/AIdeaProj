using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using I2.Loc;
using TileGameCore;
using TileGamePlay;

namespace FishTilePlay
{
    [RequireComponent(typeof(TMP_Text))]
    public class UILevelNumberText : MonoBehaviour, IAfterLocalizationFontApply
    {
        private static readonly HashSet<UILevelNumberText> Instances = new();
        private static int currentLevelNumber;
        private static bool hasCurrentLevelNumber;

        private static readonly int GradientScaleId = Shader.PropertyToID("_GradientScale");
        private static readonly int ScaleRatioAId = Shader.PropertyToID("_ScaleRatioA");
        private static readonly int ScaleRatioBId = Shader.PropertyToID("_ScaleRatioB");
        private static readonly int ScaleRatioCId = Shader.PropertyToID("_ScaleRatioC");
        private static readonly int PerspectiveFilterId = Shader.PropertyToID("_PerspectiveFilter");

        // LevelLb_3D：蓝字 + Material_blue 深蓝描边；运行时只锁 SSD / padding，不改字重和颜色。
        // Softness=0 在 3D 会锯齿，给一个很小的下限做 AA。
        private const float MinOutlineSoftness = 0.04f;
        // Mikado 字重偏满，英文字母略加字距；中文 Arial 保持 0。
        private const float MikadoCharacterSpacing = 6f;
        private const string MobileDistanceFieldSsdShader = "TextMeshPro/Mobile/Distance Field SSD";
        private const string DistanceFieldSsdShader = "TextMeshPro/Distance Field SSD";
        private const string DistanceFieldShader = "TextMeshPro/Distance Field";

        [SerializeField] TMP_Text levelNumberText;
        [SerializeField] Material levelLbSoftMaterial;
        [SerializeField] Shader mobileSsdShader;
        [SerializeField] Shader ssdShader;
        [SerializeField] Shader distanceFieldShader;

        private void Reset()
        {
            if (levelNumberText == null)
                levelNumberText = GetComponent<TMP_Text>();
            if (mobileSsdShader == null)
                mobileSsdShader = Shader.Find(MobileDistanceFieldSsdShader);
            if (ssdShader == null)
                ssdShader = Shader.Find(DistanceFieldSsdShader);
            if (distanceFieldShader == null)
                distanceFieldShader = Shader.Find(DistanceFieldShader);
        }

        private void Awake()
        {
            if (levelNumberText == null)
            {
                levelNumberText = GetComponent<TMP_Text>();
            }

            ApplyLevelLbSoftEdge();
        }

        private void OnEnable()
        {
            Instances.Add(this);
            if (hasCurrentLevelNumber)
            {
                ApplyLevelNumber(currentLevelNumber);
            }
            else
            {
                ApplyLevelLbSoftEdge();
            }
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        public void OnAfterLocalizationFontApply(TMP_Text tmp)
        {
            if (tmp != null)
                levelNumberText = tmp;
            ApplyLevelLbSoftEdge();
        }

        public void UpdateLevelNumber(int number)
        {
            currentLevelNumber = number;
            hasCurrentLevelNumber = true;

            foreach (var instance in Instances)
            {
                if (instance != null)
                {
                    instance.ApplyLevelNumber(number);
                }
            }
        }

        private void ApplyLevelNumber(int number)
        {
            if (levelNumberText == null)
            {
                levelNumberText = GetComponent<TMP_Text>();
            }

            LocalizedTMPRefresher.SetText(levelNumberText, () => FormatLevelNumber(number));

            // 动态字库刷新中文后可能把材质打回默认硬边，设完文案必须再锁一次柔边材质
            ApplyLevelLbSoftEdge();
        }

        static string FormatLevelNumber(int number)
        {
            string name = I2LocalizeMgr.GetString("LevelName").TrimEnd();
            bool infinite = GPUserDataManager.HasInit() && GPUserDataManager.Instance.GetBInfinitMode();
            return infinite ? "∞ " + name + " " + number : name + " " + number;
        }

        /// <summary>
        /// Dock 3D LevelLb：按语言绑定 Arial/Mikado LevelLb_3D，强制 SSD。
        /// 蓝字 + 深蓝描边由 LevelLb_3D 材质提供，这里不覆盖颜色。
        /// </summary>
        private void ApplyLevelLbSoftEdge()
        {
            if (levelNumberText == null)
            {
                return;
            }

            SyncLevelLbFontAndMaterial();

            // 字体切换器可能已将材质映射到 Mikado，必须优先使用当前材质。
            // 若继续使用序列化的 Arial 材质，会形成 Mikado 字体 + Arial 图集而导致 UV 纹理错乱。
            var source = levelNumberText.fontSharedMaterial != null
                ? levelNumberText.fontSharedMaterial
                : levelLbSoftMaterial;

            if (source == null)
            {
                return;
            }

            if (source != levelLbSoftMaterial &&
                source.name.IndexOf("LevelLb_3D", StringComparison.Ordinal) < 0)
            {
                return;
            }

            // 3D + 非均匀小 scale（0.05/0.05/0.1）用 SSD，按像素 UV 导数算 AA，比顶点 scale 更稳。
            // 真机优先 Mobile SSD；不要只靠 Shader.Find（未进 Always Included 时会被裁掉）。
            var df = ResolveLevelLbShader(out bool usedSsd);
            if (df != null && source.shader != df)
            {
                source.shader = df;
            }

            source.EnableKeyword("OUTLINE_ON");
            source.DisableKeyword("UNDERLAY_ON");
            source.DisableKeyword("GLOW_ON");

            var look = CaptureLevelLbLook(source);
            ApplySoftEdgeParams(source, usedSsd, look);

            levelNumberText.fontSharedMaterial = source;
            var mr = levelNumberText.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = source;
            }

            levelNumberText.extraPadding = true;
            levelNumberText.UpdateMeshPadding();
            levelNumberText.SetMaterialDirty();
            levelNumberText.SetVerticesDirty();
            levelNumberText.havePropertiesChanged = true;
            levelNumberText.ForceMeshUpdate(true, true);

            // TMP 刷新 mesh 可能按字库 atlasPadding 改写 GradientScale / ScaleRatio，必须再锁一次
            ApplySoftEdgeParams(source, usedSsd, look);
        }

        private Shader ResolveLevelLbShader(out bool usedSsd)
        {
            Shader mobile = mobileSsdShader != null
                ? mobileSsdShader
                : Shader.Find(MobileDistanceFieldSsdShader);
            Shader desktop = ssdShader != null
                ? ssdShader
                : Shader.Find(DistanceFieldSsdShader);
            Shader fallback = distanceFieldShader != null
                ? distanceFieldShader
                : Shader.Find(DistanceFieldShader);

#if UNITY_ANDROID || UNITY_IOS
            if (mobile != null)
            {
                usedSsd = true;
                return mobile;
            }

            if (desktop != null)
            {
                usedSsd = true;
                return desktop;
            }
#else
            if (desktop != null)
            {
                usedSsd = true;
                return desktop;
            }

            if (mobile != null)
            {
                usedSsd = true;
                return mobile;
            }
#endif
            usedSsd = false;
            return fallback;
        }

        /// <summary>
        /// 中文等 default 语言强制 Arial LevelLb_3D；英/法等 alternate 语言强制 Mikado LevelLb_3D。
        /// 预制体若直接挂了 Mikado，字体切换器快照会把 Mikado 当成 original，切中文不会回 Arial。
        /// </summary>
        private void SyncLevelLbFontAndMaterial()
        {
            if (levelLbSoftMaterial == null)
                return;

            var config = LocalizationFontSwitcher.Config;
            if (config == null)
                return;

            bool useAlternate = config.UsesAlternateFont(LocalizationManager.CurrentLanguage);

            if (useAlternate)
            {
                if (config.alternateFont != null)
                    levelNumberText.font = config.alternateFont;

                Material mikadoMat = null;
                config.TryGetAlternateMaterial(levelLbSoftMaterial, out mikadoMat);
                if (mikadoMat == null && levelNumberText.fontSharedMaterial != null
                    && levelNumberText.fontSharedMaterial.name.IndexOf("LevelLb_3D", StringComparison.Ordinal) >= 0)
                {
                    mikadoMat = levelNumberText.fontSharedMaterial;
                }
                if (mikadoMat != null)
                    levelNumberText.fontSharedMaterial = mikadoMat;

                levelNumberText.characterSpacing = MikadoCharacterSpacing;
            }
            else
            {
                if (config.defaultFont != null)
                    levelNumberText.font = config.defaultFont;
                levelNumberText.fontSharedMaterial = levelLbSoftMaterial;
                levelNumberText.characterSpacing = 0f;

                // 纠正「Mikado 当 original」的错误快照，否则随后 ApplyTo 会把中文再改回 Mikado
                var state = LocalizationFontState.GetOrCreate(levelNumberText);
                state.Capture(levelNumberText.font, levelLbSoftMaterial);
            }
        }

        private struct LevelLbLook
        {
            public float FaceDilate;
            public float OutlineWidth;
            public float OutlineSoftness;
            public float ScaleA;
            public float ScaleB;
            public float ScaleC;
            public Color FaceColor;
            public Color OutlineColor;
        }

        private static LevelLbLook CaptureLevelLbLook(Material source)
        {
            float softness = source.GetFloat(ShaderUtilities.ID_OutlineSoftness);
            if (softness < MinOutlineSoftness)
                softness = MinOutlineSoftness;

            return new LevelLbLook
            {
                FaceDilate = source.GetFloat(ShaderUtilities.ID_FaceDilate),
                OutlineWidth = source.GetFloat(ShaderUtilities.ID_OutlineWidth),
                OutlineSoftness = softness,
                ScaleA = source.GetFloat(ScaleRatioAId),
                ScaleB = source.GetFloat(ScaleRatioBId),
                ScaleC = source.GetFloat(ScaleRatioCId),
                FaceColor = source.GetColor(ShaderUtilities.ID_FaceColor),
                OutlineColor = source.GetColor(ShaderUtilities.ID_OutlineColor),
            };
        }

        private void ApplySoftEdgeParams(Material source, bool usedSsd, LevelLbLook look)
        {
            var font = levelNumberText.font;
            int atlasPadding = font != null ? font.atlasPadding : 8;
            float gradientScale = Mathf.Max(1f, atlasPadding + 1f);

            source.SetFloat(GradientScaleId, gradientScale);
            source.SetFloat(ShaderUtilities.ID_FaceDilate, look.FaceDilate);
            source.SetFloat(ShaderUtilities.ID_OutlineWidth, look.OutlineWidth);
            source.SetFloat(ShaderUtilities.ID_OutlineSoftness, look.OutlineSoftness);
            source.SetFloat(ScaleRatioAId, look.ScaleA);
            source.SetFloat(ScaleRatioBId, look.ScaleB);
            source.SetFloat(ScaleRatioCId, look.ScaleC);
            // 普通 SDF 透视下会把 scale 乘 (1-0.875)，3D 字边缘变硬；回退时关掉。
            source.SetFloat(PerspectiveFilterId, usedSsd ? 0.875f : 0f);
            source.SetColor(ShaderUtilities.ID_FaceColor, look.FaceColor);
            source.SetColor(ShaderUtilities.ID_OutlineColor, look.OutlineColor);
        }
    }
}
