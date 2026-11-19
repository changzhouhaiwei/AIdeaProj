using DG.Tweening;
using XSortGameCore;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 长按提示特效（场景常驻单例）。只显示/隐藏，并同步目标棋子位置、缩放与层级。
    /// </summary>
    public class XSortLongPressHintEffect : MonoBehaviour
    {
        public static XSortLongPressHintEffect Instance { get; private set; }

        static readonly Color DefaultFlashColor = new(0.314f, 0.847f, 1f, 1f);
        static readonly Color DefaultShineColor = new(2.12f, 2.40f, 2.75f, 1f);
        static readonly Color DefaultBackColor = new(0.434f, 0.375f, 0.375f, 0.2f);

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int ShineColorId = Shader.PropertyToID("_ShineColor");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");
        static readonly int ShineProgressId = Shader.PropertyToID("_ShineProgress");
        static readonly int BaseStrengthId = Shader.PropertyToID("_BaseStrength");
        static readonly int ShineStrengthId = Shader.PropertyToID("_ShineStrength");
        static readonly int ShineWidthId = Shader.PropertyToID("_ShineWidth");
        static readonly int ShineSoftnessId = Shader.PropertyToID("_ShineSoftness");
        static readonly int ShineAngleId = Shader.PropertyToID("_ShineAngle");
        static readonly int ShineFadeId = Shader.PropertyToID("_ShineFade");

        [Header("基础色 backColor")]
        [SerializeField] Color backColor = DefaultBackColor;

        [Header("闪烁 / 镜面扫光")]
        [SerializeField] Color flashColor = DefaultFlashColor;
        [SerializeField] float pulseMin = 0.15f;
        [SerializeField] float pulseMax = 0.55f;
        [SerializeField] float pulseDuration = 1.6f;
        [SerializeField] float baseStrength = 0.4f;
        [Tooltip("关闭后只保留底色闪烁，不播斜向扫光")]
        [SerializeField] bool enableShine = true;
        [SerializeField] Color shineColor = DefaultShineColor;
        [SerializeField] float shineDuration = 1.0f;
        [SerializeField] float shineInterval = 1.8f;
        [SerializeField] float shineStrength = 2.0f;
        [SerializeField] float shineWidth = 0.3f;
        [SerializeField] float shineSoftness = 0.46f;
        [Tooltip("以图片中心为原点计算的扫光角度：0° 从左向右，90° 从下向上")]
        [SerializeField] float shineAngle = 135f;
        [Tooltip("扫光越过图片边缘后的额外距离，确保光带完全离开可见范围")]
        [SerializeField] float shineOverscan = 0.08f;
        [Range(0f, 0.5f)]
        [Tooltip("扫光结束阶段的渐隐比例；0 表示只在完全出界后消失")]
        [SerializeField] float shineExitFade = 0.15f;

        [Header("轨道路径")]
        [SerializeField] float orbitWidth = 0.9f;
        [SerializeField] float orbitHeight = 0.9f;
        [SerializeField] float orbitCornerRadius = 0.15f;
        [SerializeField] float orbitMoveSpeed = 0.45f;
        [Tooltip("开启后用棋子底板尺寸覆盖 Orbit 宽高")]
        [SerializeField] bool fitOrbitToFlashSprite = false;
        [SerializeField] float orbitSizeMul = 1.05f;
        [Tooltip("轨道直线段的外凸弧高。正数向外弯曲，负数向内弯曲，0 为直线")]
        [SerializeField] float orbitTrailArc = 0.04f;

        SpriteRenderer backColorRenderer;
        SpriteRenderer flashRenderer;
        MultiSpriteRectOrbit orbit;
        Material flashMaterial;
        DG.Tweening.Tween pulseTween;
        Sequence shineSequence;
        bool isShowing;
        bool refsResolved;
        XSortActor followTarget;

        void Awake()
        {
            Instance = this;
            ResolveRefs();
            EnsureMaterialInstance();
        }

        void OnEnable()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            isShowing = false;
            followTarget = null;
            KillFlash();
            if (flashMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(flashMaterial);
                else
                    DestroyImmediate(flashMaterial);
                flashMaterial = null;
            }
        }

        void LateUpdate()
        {
            if (!isShowing || followTarget == null) return;
            SyncTransformToTarget();
        }

        /// <summary>
        /// 盖到指定棋子上显示。
        /// </summary>
        public void ShowOn(XSortActor XSort, int sortingOrder)
        {
            if (XSort == null) return;

            Instance = this;
            ResolveRefs();
            followTarget = XSort;

            var flashSprite = XSort.BackgroundSpriteRenderer != null
                ? XSort.BackgroundSpriteRenderer.sprite
                : null;
            if (flashSprite != null)
            {
                if (flashRenderer != null)
                    flashRenderer.sprite = flashSprite;
                if (backColorRenderer != null)
                    backColorRenderer.sprite = flashSprite;
            }

            SetSortingOrder(sortingOrder);
            SyncTransformToTarget();
            gameObject.SetActive(true);
            isShowing = true;
            ApplyParamsNow();
        }

        public void Hide()
        {
            isShowing = false;
            followTarget = null;
            KillFlash();
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public void SetSortingOrder(int orderBase)
        {
            ResolveRefs();
            int order = orderBase * 3;
            if (backColorRenderer != null)
                backColorRenderer.sortingOrder = order;
            if (flashRenderer != null)
                flashRenderer.sortingOrder = order + 1;
            if (orbit != null)
                orbit.SetSortingOrder(order + 2);
        }

        [ContextMenu("Apply Params Now")]
        public void ApplyParamsNow()
        {
            ResolveRefs();
            ApplyBackColor();
            EnsureMaterialInstance();
            ApplyStaticMaterialParams();
            ApplyOrbitPath(true);

            if (isShowing && gameObject.activeInHierarchy)
                PlayFlash();
        }

        void OnDisable()
        {
            KillFlash();
        }

        void SyncTransformToTarget()
        {
            if (followTarget == null) return;

            var t = followTarget.transform;
            transform.position = t.position;
            transform.rotation = t.rotation;

            float worldScale = followTarget.VisualsTransform != null
                ? followTarget.VisualsTransform.lossyScale.x
                : t.lossyScale.x;
            float parentScale = transform.parent != null ? transform.parent.lossyScale.x : 1f;
            if (Mathf.Abs(parentScale) < 1e-5f)
                parentScale = 1f;
            transform.localScale = Vector3.one * (worldScale / parentScale);
        }

        void ResolveRefs()
        {
            if (refsResolved && backColorRenderer != null && flashRenderer != null && orbit != null)
                return;

            if (backColorRenderer == null)
            {
                var t = transform.Find("backColor");
                if (t != null) backColorRenderer = t.GetComponent<SpriteRenderer>();
            }
            if (flashRenderer == null)
            {
                var t = transform.Find("FlashItem");
                if (t != null) flashRenderer = t.GetComponent<SpriteRenderer>();
            }
            if (orbit == null)
            {
                var t = transform.Find("Orbit");
                if (t != null) orbit = t.GetComponent<MultiSpriteRectOrbit>();
                if (orbit == null)
                    orbit = GetComponentInChildren<MultiSpriteRectOrbit>(true);
            }

            refsResolved = backColorRenderer != null && flashRenderer != null && orbit != null;
        }

        void ApplyBackColor()
        {
            if (backColorRenderer == null) return;
            backColorRenderer.color = backColor;
        }

        void ApplyOrbitPath(bool rebuild)
        {
            if (orbit == null) return;

            float w = orbitWidth;
            float h = orbitHeight;
            float corner = orbitCornerRadius;

            if (fitOrbitToFlashSprite && flashRenderer != null && flashRenderer.sprite != null)
            {
                var size = flashRenderer.sprite.bounds.size;
                var scale = flashRenderer.transform.localScale;
                w = size.x * scale.x * orbitSizeMul;
                h = size.y * scale.y * orbitSizeMul;
                corner = Mathf.Min(orbitCornerRadius, Mathf.Min(w, h) * 0.22f);
            }

            orbit.ApplyPath(w, h, corner, orbitMoveSpeed, orbitTrailArc, rebuild);
        }

        void EnsureMaterialInstance()
        {
            if (flashRenderer == null) return;
            if (flashMaterial != null) return;
            // Renderer.material 不能用于 Prefab 资源；特效材质只应由已加载场景中的运行时实例持有。
            if (!Application.isPlaying || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
                return;

            var shared = flashRenderer.sharedMaterial;
            Material instance;
            if (shared == null || !shared.HasProperty(PulseId))
            {
                var shader = Shader.Find("Sprites/XSortHintMirrorFlash") ?? Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    Debug.LogError("[XSortLongPressHintEffect] Missing shader: Sprites/XSortHintMirrorFlash");
                    return;
                }

                instance = new Material(shader);
            }
            else
            {
                instance = new Material(shared);
            }

            instance.name = (shared != null ? shared.name : instance.shader.name) + " (XSort Long Press Instance)";
            flashRenderer.sharedMaterial = instance;
            flashMaterial = instance;
        }

        void ApplyStaticMaterialParams()
        {
            if (flashMaterial == null) return;

            if (flashMaterial.HasProperty(ColorId))
                flashMaterial.SetColor(ColorId, flashColor);
            if (flashMaterial.HasProperty(ShineColorId))
                flashMaterial.SetColor(ShineColorId, shineColor);
            if (flashMaterial.HasProperty(BaseStrengthId))
                flashMaterial.SetFloat(BaseStrengthId, baseStrength);
            if (flashMaterial.HasProperty(ShineStrengthId))
                flashMaterial.SetFloat(ShineStrengthId, enableShine ? shineStrength : 0f);
            if (flashMaterial.HasProperty(ShineWidthId))
                flashMaterial.SetFloat(ShineWidthId, shineWidth);
            if (flashMaterial.HasProperty(ShineSoftnessId))
                flashMaterial.SetFloat(ShineSoftnessId, shineSoftness);
            if (flashMaterial.HasProperty(ShineAngleId))
                flashMaterial.SetFloat(ShineAngleId, shineAngle);
            if (flashMaterial.HasProperty(ShineFadeId))
                flashMaterial.SetFloat(ShineFadeId, 1f);
        }

        void GetShineProgressRange(out float start, out float end)
        {
            // 扫光坐标系以图片中心为 0。不同角度的投影长度不同，必须按投影半径补足出界距离。
            float radians = shineAngle * Mathf.Deg2Rad;
            float projectionHalfSpan = 0.5f * (Mathf.Abs(Mathf.Cos(radians)) + Mathf.Abs(Mathf.Sin(radians)));
            float outOfBoundsDistance = projectionHalfSpan + shineWidth * 0.5f + Mathf.Max(0f, shineOverscan);
            start = -outOfBoundsDistance;
            end = outOfBoundsDistance;
        }

        void PlayFlash()
        {
            EnsureMaterialInstance();
            if (flashMaterial == null || !flashMaterial.HasProperty(PulseId))
            {
                PlayFallbackAlphaFlash();
                return;
            }

            KillFlash();
            ApplyStaticMaterialParams();

            flashMaterial.SetFloat(PulseId, pulseMin);
            GetShineProgressRange(out float shineStart, out float shineEnd);
            flashMaterial.SetFloat(ShineProgressId, shineStart);

            pulseTween = flashMaterial
                .DOFloat(pulseMax, PulseId, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            if (!enableShine) return;

            shineSequence = DOTween.Sequence();
            shineSequence.AppendCallback(() =>
            {
                if (flashMaterial != null)
                {
                    flashMaterial.SetFloat(ShineProgressId, shineStart);
                    if (flashMaterial.HasProperty(ShineFadeId))
                        flashMaterial.SetFloat(ShineFadeId, 1f);
                }
            });
            shineSequence.Append(flashMaterial.DOFloat(shineEnd, ShineProgressId, shineDuration).SetEase(Ease.InOutSine));
            if (shineExitFade > 0f && flashMaterial.HasProperty(ShineFadeId))
            {
                float fadeDuration = shineDuration * shineExitFade;
                shineSequence.Insert(shineDuration - fadeDuration,
                    flashMaterial.DOFloat(0f, ShineFadeId, fadeDuration).SetEase(Ease.InQuad));
            }
            shineSequence.AppendInterval(shineInterval);
            shineSequence.SetLoops(-1, LoopType.Restart);
        }

        void PlayFallbackAlphaFlash()
        {
            if (flashRenderer == null) return;
            KillFlash();
            flashRenderer.color = flashColor.SetAlpha(0f);
            pulseTween = flashRenderer.DOFade(0.5f, pulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        void KillFlash()
        {
            if (pulseTween != null)
            {
                pulseTween.Kill();
                pulseTween = null;
            }
            if (shineSequence != null)
            {
                shineSequence.Kill();
                shineSequence = null;
            }
            if (flashMaterial != null)
            {
                flashMaterial.DOKill();
                if (flashMaterial.HasProperty(PulseId))
                {
                    flashMaterial.SetFloat(PulseId, 0f);
                    flashMaterial.SetFloat(ShineProgressId, 0f);
                    if (flashMaterial.HasProperty(ShineFadeId))
                        flashMaterial.SetFloat(ShineFadeId, 1f);
                }
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            refsResolved = false;
            ResolveRefs();

            if (!Application.isPlaying)
            {
                if (backColorRenderer != null)
                    backColorRenderer.color = backColor;
                return;
            }

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || !Application.isPlaying) return;
                if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;
                ApplyParamsNow();
            };
        }
#endif
    }
}
