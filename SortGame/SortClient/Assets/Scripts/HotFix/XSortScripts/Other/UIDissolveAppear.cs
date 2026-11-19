using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FishXSortPlay
{
    /// <summary>
    /// UI 反向溶解出现：节点启用后自动从 Progress 0→1，播完停在全显。
    /// 也可由 ThemeUiApplier 按 ThemePack.buttonAppearMaterial 驱动。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class UIDissolveAppear : MonoBehaviour
    {
        static readonly int ProgressId = Shader.PropertyToID("_Progress");

        [Header("Target")]
        [SerializeField] Graphic targetGraphic;
        [Tooltip("DissolveAppear 材质；为空则用 Image 上已有材质")]
        [SerializeField] Material sourceMaterial;
        [SerializeField] bool instantiateMaterial = true;

        [Header("Auto Play")]
        [SerializeField] bool playOnEnable = true;
        [SerializeField, Min(0.01f)] float duration = 2f;
        [SerializeField] bool useUnscaledTime = true;
        [SerializeField] AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] bool resetToHiddenOnDisable = true;

        Material materialInstance;
        Tweener progressTween;
        float progress;
        bool drivenByTheme;

        public float Duration
        {
            get => duration;
            set => duration = Mathf.Max(0.01f, value);
        }

        public float Progress => progress;

        /// <summary>主题是否已套上溶解材质（普通关 Clear 后为 false）。</summary>
        public bool HasThemeAppearMaterial => sourceMaterial != null;

        void Awake()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }
        }

        void OnEnable()
        {
            // 主题驱动只走 PlayButtonThemeAppear（结算在 Result02 入场后再播），
            // 避免父级刚激活 / Animator 开关节点时提前把 Progress 跑完。
            if (drivenByTheme)
            {
                return;
            }

            if (playOnEnable && sourceMaterial != null)
            {
                Play();
            }
        }

        void OnDisable()
        {
            KillTween();
            // 主题结算时 Result02 Animator 会短暂开关子节点；若此处重置 Progress，
            // 会导致之后再也看不到溶解（卡在 0）。
            if (resetToHiddenOnDisable && !drivenByTheme && materialInstance != null)
            {
                SetProgress(0f);
            }
        }

        void OnDestroy()
        {
            KillTween();
            DestroyMaterialInstance();
        }

        /// <summary>
        /// ThemePack 配置驱动：有材质则实例化并播放；切换材质会重建实例。
        /// </summary>
        public void ApplyThemeMaterial(Material material, bool play = true)
        {
            drivenByTheme = true;
            playOnEnable = false;
            resetToHiddenOnDisable = false;
            KillTween();
            DestroyMaterialInstance();

            sourceMaterial = material;
            if (material == null)
            {
                ClearGraphicMaterial();
                return;
            }

            if (play)
            {
                Play();
            }
            else if (EnsureMaterial())
            {
                // 先藏住，等界面真正可见再 Play（如结算 Result02）
                SetProgress(0f);
            }
        }

        /// <summary>
        /// 清除主题溶解：还原默认 UI 材质，并不再自动播放。
        /// </summary>
        public void ClearThemeMaterial()
        {
            drivenByTheme = true;
            playOnEnable = false;
            KillTween();
            DestroyMaterialInstance();
            sourceMaterial = null;
            ClearGraphicMaterial();
            progress = 1f;
        }

        [ContextMenu("Play Appear")]
        public void Play()
        {
            KillTween();
            if (!EnsureMaterial())
            {
                return;
            }

            SetProgress(0f);
            float t = 0f;
            var curve = progressCurve != null && progressCurve.length > 0
                ? progressCurve
                : AnimationCurve.Linear(0f, 0f, 1f, 1f);

            progressTween = DOTween.To(
                    () => t,
                    value =>
                    {
                        t = value;
                        SetProgress(curve.Evaluate(value));
                    },
                    1f,
                    duration)
                .SetEase(Ease.Linear)
                .SetUpdate(useUnscaledTime)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => SetProgress(1f));
        }

        public void StopAndShowFull()
        {
            KillTween();
            SetProgress(1f);
        }

        public void SetProgress(float value)
        {
            progress = Mathf.Clamp01(value);
            if (materialInstance == null && !EnsureMaterial())
            {
                return;
            }

            if (materialInstance == null)
            {
                return;
            }

            materialInstance.SetFloat(ProgressId, progress);
            if (targetGraphic != null)
            {
                targetGraphic.SetMaterialDirty();
            }
        }

        bool EnsureMaterial()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (targetGraphic == null)
            {
                return false;
            }

            Material source = sourceMaterial;
            if (source == null)
            {
                return false;
            }

            if (materialInstance != null)
            {
                if (materialInstance.shader == source.shader)
                {
                    return true;
                }

                DestroyMaterialInstance();
            }

            if (instantiateMaterial)
            {
                materialInstance = Instantiate(source);
                materialInstance.name = source.name + " (Appear)";
                targetGraphic.material = materialInstance;
            }
            else
            {
                materialInstance = source;
                targetGraphic.material = source;
            }

            return materialInstance != null;
        }

        void ClearGraphicMaterial()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (targetGraphic != null)
            {
                targetGraphic.material = null;
            }
        }

        void KillTween()
        {
            if (progressTween != null && progressTween.IsActive())
            {
                progressTween.Kill();
            }

            progressTween = null;
        }

        void DestroyMaterialInstance()
        {
            if (materialInstance == null)
            {
                return;
            }

            if (instantiateMaterial)
            {
                if (Application.isPlaying)
                {
                    Destroy(materialInstance);
                }
                else
                {
                    DestroyImmediate(materialInstance);
                }
            }

            materialInstance = null;
        }
    }
}
