using UnityEngine;
using TileGameCore;
using System;
using DG.Tweening;
using System.Collections.Generic;
//using FishFramework;


namespace FishTilePlay
{
    public class TileActor : MonoBehaviour, ITappable, IDockable
    {
        private static readonly int OVERLAY_STRENGTH_ID = Shader.PropertyToID("_OverlayStrength");

        /// <summary>提交飞至 Dock 时传入 SetSortingOrder 的基准，图标与底图会按同一逻辑抬高，避免被棋盘或其它棋子遮挡。</summary>
        private const int SubmitFlightSortingOrder = 500;

        /// <summary>
        /// 上 dock / 长按飞行层级基数；每次 +1，保证后点的棋子始终盖住先点的。
        /// </summary>
        private const int DOCK_SUBMIT_SORT_BASE = 1000;
        private static int s_dockSubmitSortOrder = DOCK_SUBMIT_SORT_BASE;

        private const float LONG_PRESS_SCALE = 1.5f;
        private const float LONG_PRESS_BREATH_SCALE_OFFSET = 0.03f;
        private const float LONG_PRESS_BREATH_HALF_DURATION = 0.7f;

        /// <summary>HighLight 激活时，不可点击棋子的置灰颜色。</summary>
        private static readonly Color HighLightBlockedTintColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [SerializeField] SpriteRenderer iconSpriteRenderer;
        public SpriteRenderer IconSpriteRenderer => iconSpriteRenderer;
        [SerializeField] SpriteRenderer backgroundSpriteRenderer;

        [SerializeField] public int SpriteIndex;
        public SpriteRenderer BackgroundSpriteRenderer => backgroundSpriteRenderer;

        [SerializeField] Transform visuals;
        public Transform VisualsTransform => visuals;

        [SerializeField] BoxCollider boxCollider;
        public BoxCollider BoxCollider => boxCollider;

        public Transform Transform => transform;

        private bool isClickable;
        public bool IsClickable => isClickable;

        public int LayerID => elementPosition.LayerId;

        [SerializeField]
        protected ElementPosition elementPosition;
        public ElementPosition ElementPosition => elementPosition;

        protected bool isSubmitted;
        public bool IsSubmitted => isSubmitted;
        protected TileModel tileData;
        public TileModel TileModel => tileData;
        protected TileEffect effect;
        public TileEffect Effect => effect;

        public const int FissionMatchId = 917351;

        bool isFission;
        public bool IsFission => isFission;

        int visualEpoch;
        public int VisualEpoch => visualEpoch;

        public int UniqueElementID => isFission
            ? FissionMatchId
            : (tileData != null ? tileData.GetHashCode() : 0);

        public int MatchTypeId => isFission
            ? FissionMatchId
            : (tileData != null ? tileData.TileIndex : -1);

        private DG.Tweening.Tween backgroundColorTweenCase;
        private DG.Tweening.Tween iconColorTweenCase;
        private DG.Tweening.Tween m_longPressTweenCase;
        private Sequence m_longPressBreathTweenCase;
        private DG.Tweening.Tween m_shakeTweenCase;
        private DG.Tweening.Tween shakeTweenCase;
        private Sequence hintTweenCase;
        private Vector3 m_hintOriginalScale;
        private Vector3 m_preLocalPos;
        private Vector3 m_preLocalRot;
        private int m_currentSortingOrder;
        private static TileGameModel TileGameModel;

        private bool isShaing = false;

        private Renderer[] m_spawnTweenVisualRenderers;
        private Sprite defaultIconSprite;
        private static readonly HashSet<string> LoggedMissingTextureSkins = new();

        [NonSerialized]
        public int TileSkinIdx = -1;
        public string RuntimeSkinName { get; private set; } = "";
        public bool IsCoinReward =>
            LevelDirector.IsCoinRewardSkin(RuntimeSkinName) || LevelDirector.IsCoinRewardTile(tileData);

        //隐式转换类型
        public static implicit operator ElementPosition(TileActor tileBehavior) => tileBehavior.elementPosition;

        public virtual void Init(TileModel tileData, ElementPosition elementPosition)
        {
            this.tileData = tileData;
            isFission = false;
            IQChallengeVisual.HideBadge(this);

            if (defaultIconSprite == null && iconSpriteRenderer != null)
            {
                defaultIconSprite = iconSpriteRenderer.sprite;
            }

            // BackgroundSpriteRenderer.SetActiveEx(false);
            ApplyTileBackground();
            ApplyRuntimeSkin(tileData.RuntimeSkin);
            SetPosition(elementPosition);

            DisableEffect();
            ApplyIconRendererVisibility();
        }

        private void OnDestroy()
        {
            m_spawnTweenVisualRenderers = null;
            if (transform != null)
                transform.DOKill();
        }

        public void SetPosition(ElementPosition elementPosition)
        {
            this.elementPosition = elementPosition;
            SetSortingOrder(-(elementPosition.LayerId + 1) * 100 + elementPosition.X - elementPosition.Y);// + elementPosition.X - elementPosition.Y );// + elementPosition.X - elementPosition.Y);//;
        }

        public void ResetSiblingIndex()
        {
            // SetPosition(elementPosition);
        }

        public int GetSiblingValue()
        {
            var v = elementPosition.X + elementPosition.Y * 30;
            gameObject.name = v + $"_x={elementPosition.X}:y={elementPosition.Y}";

            return (elementPosition.X) + ((100 - elementPosition.Y) * 30);
        }
        public void SetScale(Vector2 scale)
        {
            var smallerSize = scale.x > scale.y ? scale.y : scale.x;
            visuals.localScale = Vector3.one * smallerSize;
            boxCollider.size = scale * 0.95f;//防止短距离堆叠

            m_hintOriginalScale = transform.localScale;
        }

        public void InvalidateSpawnTweenVisualCache()
        {
            m_spawnTweenVisualRenderers = null;
        }

        /// <summary>
        /// 缩放类登场/洗牌：关掉 visuals 下所有 Renderer 绘制，避免 scale≈0 时仍出碎点。
        /// 不用 SetActive(false) 整棵 visuals，以免 OnDisable/再启用带来旋转或状态异常（表现为倒立、乱帧）。
        /// </summary>
        public void SetSpawnTweenVisualsActive(bool active)
        {
            if (!this)
                return;

            if (visuals != null)
            {
                if (active)
                {
                    m_spawnTweenVisualRenderers = visuals.GetComponentsInChildren<Renderer>(true);
                }
                else
                {
                    RefreshSpawnTweenVisualRenderersIfNeeded();
                }

                if (m_spawnTweenVisualRenderers == null)
                    return;

                var stackOccludedOverlay = (effect as IStackOccludedTileEffect)?.OverlayRenderer;

                for (int i = 0; i < m_spawnTweenVisualRenderers.Length; i++)
                {
                    var r = m_spawnTweenVisualRenderers[i];
                    if (!r)
                        continue;

                    if (active && stackOccludedOverlay != null && r == stackOccludedOverlay)
                        continue;

                    r.enabled = active;
                }

                if (active)
                {
                    // 显式恢复底板/花色，避免缓存列表遗漏或中途全局刷新导致只剩空白底板
                    if (backgroundSpriteRenderer != null)
                        backgroundSpriteRenderer.enabled = true;
                    ApplyIconRendererVisibility();
                    (effect as IStackOccludedTileEffect)?.RefreshVisibility();
                }
            }
            else
            {
                if (iconSpriteRenderer) iconSpriteRenderer.enabled = active;
                if (backgroundSpriteRenderer) backgroundSpriteRenderer.enabled = active;
                if (active)
                    ApplyIconRendererVisibility();
            }
        }

        bool ShouldHideIconRenderer() =>
            effect is UnknownTileEffect or CrateTileEffect;

        void ApplyIconRendererVisibility()
        {
            if (iconSpriteRenderer == null)
                return;

            iconSpriteRenderer.enabled = !ShouldHideIconRenderer();
        }

        void RefreshSpawnTweenVisualRenderersIfNeeded()
        {
            if (visuals == null)
            {
                m_spawnTweenVisualRenderers = null;
                return;
            }

            if (m_spawnTweenVisualRenderers == null)
            {
                m_spawnTweenVisualRenderers = visuals.GetComponentsInChildren<Renderer>(true);
                return;
            }

            for (int i = 0; i < m_spawnTweenVisualRenderers.Length; i++)
            {
                if (!m_spawnTweenVisualRenderers[i])
                {
                    m_spawnTweenVisualRenderers = visuals.GetComponentsInChildren<Renderer>(true);
                    return;
                }
            }
        }

        public void SetSortingOrder(int order)
        {
            m_currentSortingOrder = order;
            // Back 必须低于 Icon；二者同 sortingOrder 时，洗牌 SetParent 后绘制顺序不稳定，会表现为「空白棋子」（只见底板不见花色）
            backgroundSpriteRenderer.sortingOrder = order * 3 - 2;
            iconSpriteRenderer.sortingOrder = order * 3 - 1;
            if (effect != null)
                effect.OnSortingOrderChanged(order * 3);
            IQChallengeVisual.SyncSorting(this, order);
            var hintEff = TileLongPressHintEffect.Instance;
            if (hintEff != null && hintEff.gameObject.activeSelf)
                hintEff.SetSortingOrder(order);
        }

        /// <summary>
        /// 取下一个上 dock / 长按层级（基数 +1，连点也能保持后点在上）。
        /// </summary>
        public static int NextDockSubmitSortOrder()
        {
            s_dockSubmitSortOrder += 1;
            return s_dockSubmitSortOrder;
        }

        public static void ResetDockSubmitSortOrder()
        {
            s_dockSubmitSortOrder = DOCK_SUBMIT_SORT_BASE;
        }

        public void DockShuffleMove(Transform position, SimpleCallback onCompleted)
        {
            transform.DOMove(position.position, 0.15f).SetEase(DG.Tweening.Ease.OutSine).OnComplete(() =>
            {
                onCompleted?.Invoke();
            });
        }

        /// <summary>
        /// 棋盘 → Dock：根据起点在屏幕左/右半区选用不同侧向弯曲的三次贝塞尔控制点（XY 平面侧向 + 整体上拱）。
        /// </summary>
        private static void GetSubmitDockBezierControlPoints(Vector3 startWorld, Vector3 endWorld, out Vector3 c1, out Vector3 c2)
        {
            Vector3 chord = endWorld - startWorld;
            float dist = chord.magnitude;
            if (dist < 1e-4f)
            {
                c1 = startWorld;
                c2 = endWorld;
                return;
            }

            Vector3 chordDir = chord / dist;
            Vector3 lateral = new Vector3(-chordDir.y, chordDir.x, 0f);
            if (lateral.sqrMagnitude < 1e-6f)
                lateral = Vector3.right;
            else
                lateral.Normalize();

            bool leftHalfScreen;
            bool nearScreenCenter;
            var cam = Camera.main;
            if (cam != null)
            {
                float viewportX = cam.WorldToViewportPoint(startWorld).x;
                leftHalfScreen = viewportX < 0.5f;
                // 起点接近屏幕中线时，使用近似直线飞行，避免左右弧线带来的视觉偏摆。
                nearScreenCenter = Mathf.Abs(viewportX - 0.5f) <= 0.1f;
            }
            else
            {
                leftHalfScreen = startWorld.x <= 0f;
                nearScreenCenter = Mathf.Abs(startWorld.x) <= 0.25f;
            }

            if (nearScreenCenter)
            {
                c1 = startWorld + chord * (1f / 3f);
                c2 = startWorld + chord * (2f / 3f);
                return;
            }

            float lateralSign = leftHalfScreen ? 1f : -1f;
            float sag = dist * 0.24f;
            Vector3 lift = Vector3.up * (dist * 0.12f);

            c1 = startWorld + chord * 0.28f + lateral * (sag * lateralSign) + lift * 0.55f;
            c2 = startWorld + chord * 0.72f + lateral * (sag * lateralSign) + lift * 0.55f;
        }

        /// <param name="peakMul">沿路径先鼓包再收回的倍率（相对线性插值基准），如 1.3</param>
        private void PlaySubmitMoveBezier(
            Vector3 endWorld,
            float duration,
            Vector3 transformScaleStart,
            Vector3 transformScaleEnd,
            Vector3 visualsScaleStart,
            Vector3 visualsScaleEnd,
            float peakMul,
            TweenCallback onFlightEnded,
            bool unscaledTime = false)
        {
            Vector3 startWorld = transform.position;
            GetSubmitDockBezierControlPoints(startWorld, endWorld, out Vector3 c1, out Vector3 c2);

            // 目标 scale（不叠加呼吸系数），用于异常中断时强制收束到设定大小
            Vector3 finalTransformScale = transformScaleEnd;
            Vector3 finalVisualsScale = visualsScaleEnd;

            var tween = DOVirtual.Float(0f, 1f, duration, t =>
            {
                transform.position = Bezier.EvaluateCubic(startWorld, c1, c2, endWorld, t);
                float bell = Mathf.Sin(Mathf.PI * t);
                float bump = 1f + (peakMul - 1f) * bell;
                float breath = DockActor.GetWarningBreathingUniformFactor();
                Vector3 bVec = new Vector3(breath, breath, breath);
                Vector3 baseTs = Vector3.Lerp(transformScaleStart, transformScaleEnd, t);
                transform.localScale = Vector3.Scale(Vector3.Scale(baseTs, new Vector3(bump, bump, bump)), bVec);
                Vector3 baseVs = Vector3.Lerp(visualsScaleStart, visualsScaleEnd, t);
                visuals.localScale = Vector3.Scale(Vector3.Scale(baseVs, new Vector3(bump, bump, bump)), bVec);
            })
                .SetEase(DG.Tweening.Ease.OutSine)
                .SetUpdate(unscaledTime)
                .SetTarget(transform)
                .OnComplete(() =>
                {
                    float breath = DockActor.GetWarningBreathingUniformFactor();
                    Vector3 bVec = new Vector3(breath, breath, breath);
                    transform.localScale = Vector3.Scale(finalTransformScale, bVec);
                    visuals.localScale = Vector3.Scale(finalVisualsScale, bVec);
                    transform.DORotate(Vector3.zero, 0.1f);
                    onFlightEnded?.Invoke();
                })
                .OnKill(() =>
                {
                    // 如果因为异常（如 DOKill/停止动画）中断飞行动画，也强制收束到目标大小，避免停在中间缩放
                    if (this != null && visuals != null)
                    {
                        transform.localScale = finalTransformScale;
                        visuals.localScale = finalVisualsScale;
                    }
                });

            DOVirtual.DelayedCall(duration * (7f / 8f), DockActor.PlaySubmitFlightShake)
                .SetTarget(transform);
        }

        //提交移动动画
        public void SubmitMove(Transform position, Vector3 scale, SimpleCallback onCompleted)
        {
            GameDirector.RecordLastPlayTime();
            SetSortingOrder(SubmitFlightSortingOrder);
            StopAllAnimation();

            Vector3 endWorld = position.position;
            var smallerSize = scale.x > scale.y ? scale.y : scale.x;
            Vector3 ts0 = transform.localScale;
            Vector3 vs0 = visuals.localScale;
            // 与旧版落地抖动结束一致：transform=1、visuals=槽位较小边（不再额外 *0.95，否则整体偏小）
            Vector3 ts1 = Vector3.one;
            Vector3 vs1 = Vector3.one * smallerSize;

            float dist = Vector3.Distance(transform.position, endWorld);
            // 距离越近鼓包越小，距离越远鼓包越大
            const float minPeakMul = 1.3f;
            const float maxPeakMul = 1.55f;
            const float peakDistNear = 3.7f;
            const float peakDistFar = 6.0f;
            float peakT = Mathf.InverseLerp(peakDistNear, peakDistFar, dist);
            float submitPeakMul = Mathf.Lerp(minPeakMul, maxPeakMul, peakT);

            PlaySubmitMoveBezier(endWorld, 0.3f, ts0, ts1, vs0, vs1, submitPeakMul, () => onCompleted?.Invoke());
        }

        public void SubmitMove(Vector3 position, Vector3 scale, SimpleCallback onCompleted)
        {
            SubmitMove(position, scale, onCompleted, unscaledTime: false);
        }

        public void SubmitMove(Vector3 position, Vector3 scale, SimpleCallback onCompleted, bool unscaledTime)
        {
            GameDirector.RecordLastPlayTime();
            SetSortingOrder(SubmitFlightSortingOrder);

            var smallerSize = scale.x > scale.y ? scale.y : scale.x;
            float dist = Vector3.Distance(transform.position, position);
            Vector3 ts0 = transform.localScale;
            Vector3 vs0 = visuals.localScale;
            Vector3 ts1 = Vector3.one;
            Vector3 vs1 = Vector3.one * smallerSize;

            const float minPeakMul = 1.2f;
            const float maxPeakMul = 1.45f;
            const float peakDistNear = 2.5f;
            const float peakDistFar = 6.0f;
            float peakT = Mathf.InverseLerp(peakDistNear, peakDistFar, dist);
            float submitPeakMul = Mathf.Lerp(minPeakMul, maxPeakMul, peakT);

            PlaySubmitMoveBezier(position, 0.2f, ts0, ts1, vs0, vs1, submitPeakMul, () => onCompleted?.Invoke(), unscaledTime);
        }

        //没有啥状态,就是可以点击还是不可点击
        public void SetIsClickable(bool isAble, bool withAnimation = true)
        {
            if (isClickable != isAble && effect != null)
            {
                effect.OnStateChanged(isAble);
            }
            isClickable = isAble;

            // HighLight 激活时才置灰不可点击棋子；未激活/洗牌压制时保持全白（现网观感）
            Color tintColor = Color.white;
            if (PUHighLightBehavior.ShouldApplyBlockedTint && !isAble)
                tintColor = HighLightBlockedTintColor;
            SetColor(tintColor, withAnimation);
        }

        public void SetColor(Color color, bool withAnimation)
        {
            if (iconSpriteRenderer != null)
                iconSpriteRenderer.color = color;
            if (backgroundSpriteRenderer != null)
                backgroundSpriteRenderer.color = color;
            // Unknown / GoldenTree 等可见层在独立 overlay 上，需一并着色
            effect?.ApplyTintColor(color);
        }

        //点击开始
        public virtual void OnObjectClicked(out bool isValid)
        {
            GameDirector.RecordLastPlayTime(true);
            isValid = false;
            if (isSubmitted)
                return;

            bool isClickAllowed = true;

            if (!LevelDirector.SubmitIsAllowed())
            {
                isClickAllowed = false;
            }

            if (effect != null && !effect.IsClickAllowed())
            {
                isClickAllowed = false;
            }

            SetIsClickable(LevelDirector.LevelGenService.IsTileUnconcealed(this));

            if (!isClickable)
            {
                isClickAllowed = false;
            }

            if (isClickAllowed)
            {
                PlayLongPresssAnim();
                isValid = true;
            }
            else
            {
                PlayBlockedSound();

                if (isShaing) return;
                isShaing = true;
                PlayShake();
            }
        }

        //长按效果
        public virtual void PlayLongPresssAnim()
        {
            StopAllAnimation();
            // 与上 dock 共用递增层级，连点时当前按住的棋子始终在最上层
            SetSortingOrder(NextDockSubmitSortOrder());
            Vector3 originalPosition = transform.localPosition;
            Vector3 originalRotation = transform.localEulerAngles;
            m_preLocalPos = originalPosition;
            m_preLocalRot = originalRotation;

            LevelDirector.Dock.ShowOrHideLongPressAnim(this, false);
            ShowLongPressHintEffect();
            // 先放大，完成后只开 Dock 长按特效（不再左右旋转）
            m_longPressTweenCase = transform.DOScale(LONG_PRESS_SCALE, 0.3f).SetEase(DG.Tweening.Ease.OutSine).OnComplete(() =>
            {
                m_longPressTweenCase = null;
                StartLongPressBreath();
                LevelDirector.Dock.ShowOrHideLongPressAnim(this, true);
            });
        }

        void ShowLongPressHintEffect()
        {
            LevelDirector.Dock?.ShowOrHideLongPressHintEff(this, true, m_currentSortingOrder);
        }

        void HideLongPressHintEffect()
        {
            LevelDirector.Dock?.ShowOrHideLongPressHintEff(this, false, m_currentSortingOrder);
        }

        void StartLongPressBreath()
        {
            StopLongPressBreath();

            float expandedScale = LONG_PRESS_SCALE + LONG_PRESS_BREATH_SCALE_OFFSET;
            float contractedScale = LONG_PRESS_SCALE - LONG_PRESS_BREATH_SCALE_OFFSET;
            m_longPressBreathTweenCase = DOTween.Sequence()
                .Append(transform.DOScale(expandedScale, LONG_PRESS_BREATH_HALF_DURATION).SetEase(Ease.InOutSine))
                .Append(transform.DOScale(contractedScale, LONG_PRESS_BREATH_HALF_DURATION * 2f).SetEase(Ease.InOutSine))
                .Append(transform.DOScale(LONG_PRESS_SCALE, LONG_PRESS_BREATH_HALF_DURATION).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Restart);
        }

        void StopLongPressBreath(bool resetToLongPressScale = false)
        {
            if (m_longPressBreathTweenCase != null)
            {
                m_longPressBreathTweenCase.Kill();
                m_longPressBreathTweenCase = null;
            }

            if (resetToLongPressScale)
                transform.localScale = Vector3.one * LONG_PRESS_SCALE;
        }

        /// <summary>
        /// 调试：用常驻特效重新盖到当前棋子。
        /// </summary>
        public void DebugReplayLongPressHintEff()
        {
            RecoverLongPressAnim(true);
            StopAllAnimation();
            SetSortingOrder(NextDockSubmitSortOrder());
            m_preLocalPos = transform.localPosition;
            m_preLocalRot = transform.localEulerAngles;
            LevelDirector.Dock.ShowOrHideLongPressAnim(this, false);
            ShowLongPressHintEffect();
            m_longPressTweenCase = transform.DOScale(LONG_PRESS_SCALE, 0.3f).SetEase(DG.Tweening.Ease.OutSine).OnComplete(() =>
            {
                m_longPressTweenCase = null;
                StartLongPressBreath();
                LevelDirector.Dock.ShowOrHideLongPressAnim(this, true);
            });
        }

        public virtual void RecoverLongPressAnim(bool bResetTran = true)
        {
            LevelDirector.Dock.ShowOrHideLongPressAnim(this, false);
            HideLongPressHintEffect();
            StopLongPressBreath(true);
            // 停止长按动画
            if (m_longPressTweenCase != null)
            {
                m_longPressTweenCase.Kill();
                m_longPressTweenCase = null;
            }

            if (m_shakeTweenCase != null)
            {
                m_shakeTweenCase.Kill();
                m_shakeTweenCase = null;
            }

            SetSortingOrder(-(elementPosition.LayerId + 1) * 100 + elementPosition.X - elementPosition.Y);

            // 恢复原始大小
            if (bResetTran)
            {
                // 恢复到正常大小（1.0f），如果 hint 动画存在则使用 hint 的原始大小
                if (m_hintOriginalScale != Vector3.zero && m_hintOriginalScale != Vector3.one)
                {
                    // hint 动画的原始大小不是 1.0f，恢复到 hint 的原始大小
                    transform.localScale = m_hintOriginalScale;
                }
                else
                {
                    // 恢复到正常大小 1.0f
                    transform.SetLocalScale(1.0f);
                }
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                transform.localPosition = m_preLocalPos;
                transform.localEulerAngles = m_preLocalRot;
                SetPosition(elementPosition);
            }
        }

        public void PlayShake()
        {
            // 实现左右小幅度来回三次的抖动效果
            Vector3 originalPos = transform.localPosition;
            shakeTweenCase = DOTween.Sequence()
                // 向右移动
                .Append(transform.DOLocalMove(originalPos + Vector3.right * 0.1f, 0.06f))
                // 向左移动
                .Append(transform.DOLocalMove(originalPos + Vector3.left * 0.1f, 0.06f))
                // 向右移动
                .Append(transform.DOLocalMove(originalPos + Vector3.right * 0.1f, 0.06f))
                // 向左移动
                .Append(transform.DOLocalMove(originalPos + Vector3.left * 0.1f, 0.06f))
                // 向右移动
                .Append(transform.DOLocalMove(originalPos + Vector3.right * 0.1f, 0.06f))
                // 回到原位
                .Append(transform.DOLocalMove(originalPos, 0.06f))
                .OnComplete(() =>
                {
                    isShaing = false;
                });
        }

        void PlayReleaseSound()
        {
            AudioClip clip = TileBaseStyleTable.Current?.collectSound
                ?? ThemeUiApplier.Get()?.collectSound
                ?? TileSkinCatalog.LoadTileBaseSound(
                    TileSkinCatalog.GetCollectSoundName(LevelDirector.LoadedLevelTheme),
                    AudioController.AudioClips.tileClick);
            AudioController.PlaySound(clip, 1f, 1f);
        }

        void PlayBlockedSound()
        {
            if (LevelDirector.LoadedLevelTheme == 0)
                AudioController.PlaySound(AudioController.AudioClips.tileClickBlocked, 1, UnityEngine.Random.Range(0.8f, 1.2f));
            else
                AudioController.PlaySound(AudioController.AudioClips.WoodClickBlocked, 0.4f, UnityEngine.Random.Range(0.8f, 1.2f));
        }

        void CheckRemoveGoldenTree()
        {
            if (effect != null && effect.EffectType == TileEffectType.GoldenTree)
            {
                bool isX2 = effect is GoldenTreeEffect goldenTreeEffect && goldenTreeEffect.IsX2;
                int scoreAdd = isX2 ? 2 : 1;

                LevelDirector.GoldenTreeData.IncreaseCurrentCnt(scoreAdd);
                GoldTreeEffMgr.ExcuteMoveEff(this, isX2);
            }
        }

        public void OnObjectReleased()
        {
            if (isSubmitted)
                return;

            bool isClickAllowed = true;

            if (!LevelDirector.SubmitIsAllowed()) isClickAllowed = false;
            if (!isClickable) isClickAllowed = false;
            if (effect != null && !effect.IsClickAllowed()) isClickAllowed = false;

            if (isClickAllowed)
            {
                RecoverLongPressAnim(false);
                if (Effect == null || Effect.BeforeTileSubmitted())
                {
                    StopAllAnimation();
                    LevelDirector.SubmitElement(this);
                }

                if (effect != null && effect.EffectType == TileEffectType.GoldenTree)
                {
                    DetachAndDelayDisableGoldenTreeEffect(GoldTreeEffMgr.CollectFlyStartDelay);
                }

                PlayReleaseSound();
#if MODULE_HAPTIC
                        Haptic.Play(Haptic.HAPTIC_LIGHT);
#endif
            }
            else
            {
                PlayBlockedSound();

                if (isShaing) return;
                isShaing = true;
                PlayShake();

            }
        }
        public void MarkAsSubmitted(bool bFromHintItem = false)
        {
            isSubmitted = true;
            if (RaycastController.m_curClickObject == (ITappable)this)
                RaycastController.m_curClickObject = null;

            CheckRemoveGoldenTree();
            LevelDirector.OnTileSubmitted(this, bFromHintItem);

            if (GameData.BFromStartScene && !LevelDifficultySim.IsRunning)
            {
                RecoverLevelData.MarkUserProgress();
                RecoverLevelData.SaveCurrentLevelData();
            }
        }

        public void ResetSubmitState()
        {
            isSubmitted = false;
        }


        private void StopAllAnimation()
        {
            StopLongPressBreath();

            if (backgroundColorTweenCase != null)
            {
                backgroundColorTweenCase.Kill();
                backgroundColorTweenCase = null;
            }
            if (iconColorTweenCase != null)
            {
                iconColorTweenCase.Kill();
                iconColorTweenCase = null;
            }
            if (m_longPressTweenCase != null)
            {
                m_longPressTweenCase.Kill();
                m_longPressTweenCase = null;
            }
            if (m_shakeTweenCase != null)
            {
                m_shakeTweenCase.Kill();
                m_shakeTweenCase = null;
            }
            if (shakeTweenCase != null)
            {
                shakeTweenCase.Kill();
                shakeTweenCase = null;
            }

            if (hintTweenCase != null)
            {
                hintTweenCase.Kill();
                hintTweenCase = null;
            }
            transform.DOKill();
        }

        public void Clear()
        {
            if (RaycastController.m_curClickObject == (ITappable)this)
                RaycastController.ResetLongPress();

            visualEpoch++;
            gameObject.SetActive(false);
            StopAllAnimation();
            transform.SetParent(PoolManager.DefaultContainer);

            if (effect != null)
            {
                effect.Clear();

                Destroy(effect.gameObject);
            }

            effect = null;
            if (iconSpriteRenderer != null)
                iconSpriteRenderer.enabled = true;
            if (backgroundSpriteRenderer != null)
                backgroundSpriteRenderer.enabled = true;

            ResetSubmitState();
            isFission = false;
            IQChallengeVisual.HideBadge(this);
        }

        public void PlaySpawnAnimation(float delay = 0)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.5f).SetDelay(delay).SetEase(DG.Tweening.Ease.OutBack);
        }

        public bool IsSameType(IDockable other)
        {
            if (other == null)
                return false;

            var otherTile = (TileActor)other;
            if (otherTile == null)
                return false;

            if (isFission || otherTile.isFission)
                return isFission && otherTile.isFission;

            return otherTile.tileData == this.tileData;
        }

        public void SetFissionIdentity(bool value)
        {
            isFission = value;
        }

        public void SetFission(bool value, bool animate = false)
        {
            isFission = value;
            if (value)
            {
                if (animate)
                    IQChallengeVisual.PlayConvert(this);
                else
                    IQChallengeVisual.ApplyLook(this);
            }
            else
            {
                IQChallengeVisual.RestoreLook(this);
            }
        }

        public void RestoreDefaultVisuals()
        {
            if (backgroundSpriteRenderer != null)
                backgroundSpriteRenderer.color = Color.white;
            if (iconSpriteRenderer != null)
                iconSpriteRenderer.color = Color.white;

            ApplyTileBackground();
            if (IsCoinReward)
                ApplyCoinRewardSkin();
            else if (!string.IsNullOrEmpty(RuntimeSkinName))
                ApplyRuntimeSkin(TileSkinCatalog.GetSkin(RuntimeSkinName));
            else if (tileData != null)
                ApplyRuntimeSkin(tileData.RuntimeSkin);
        }

        /// <summary>
        /// 检查棋子是否允许被打乱
        /// 某些特殊棋子（如链接棋子）可能不允许被打乱
        /// </summary>
        /// <returns>如果允许打乱返回true，否则返回false</returns>
        public bool IsShuffleAllowed()
        {
            // 如果棋子有特殊效果，则询问效果是否允许打乱
            if (effect != null)
                return effect.IsShuffleAllowed();

            // 默认情况下，普通棋子都允许被打乱
            return true;
        }

        public void MatchAnimation(float delay)
        {
            if (LevelDirector.WinState != 3)
                transform.DOScale(0, 0.25f).SetDelay(delay);


            int theme = LevelDirector.LoadedLevelTheme;
            bool isCoin = IsCoinReward;
            string effectName = isCoin
                ? TileSkinCatalog.CoinRewardEffectPrefabName
                : TileSkinCatalog.GetEffectPrefabName(theme);
            if (!ParticlesDirector.HasParticle(effectName))
            {
                GameObject effectPrefab = TileSkinCatalog.LoadEffectPrefab(effectName);
                if (effectPrefab != null)
                    ParticlesDirector.RegisterParticle(effectName, effectPrefab);
            }

            DOVirtual.DelayedCall(0.05f + delay, () =>
            {
                var particleCase = ParticlesDirector.PlayParticle(effectName);
                if (particleCase == null || particleCase.ParticleSystem == null)
                    return;

                particleCase.SetDuration(2)
                    .SetPosition(transform.position + Vector3.back * 0.2f)
                    .SetRotation(Quaternion.Euler(Vector3.right * -90f));
                if (!isCoin && LevelDirector.LoadedTileBaseStyle < 0)
                {
                    TileSkinCatalog.ApplyTileBaseMaterial(particleCase.ParticleSystem.gameObject, effectName, theme);
                }
            });
        }

        public void FinalMatchMoveAnimation(Vector3 dstPos)
        {
            Vector3 pos1 = transform.position + Vector3.right * 1.5f;
            transform.DOMove(pos1, 0.2f).OnComplete(() =>
            {
                transform.DOMove(dstPos, 0.25f);
            });
        }

        public void FinalMatchAnimation(float delay, Action act = null)
        {
            //向上再乡下,期间旋转,然后回到初始位置,调用act
            Vector3 originalPosition = transform.position;
            Vector3 originalRotation = transform.eulerAngles;

            // 向上移动并旋转
            transform.DOMove(originalPosition - Vector3.up * 1.8f, delay / 2)
                .SetEase(DG.Tweening.Ease.OutSine)
                .OnComplete(() =>
                {
                    transform.DOMove(originalPosition, delay / 2)
                        .SetEase(DG.Tweening.Ease.InSine)
                        .OnComplete(() => act?.Invoke());
                });

            // 使用相对旋转实现 360 度旋转
            transform.DORotate(new Vector3(0, 0, 360f), delay, DG.Tweening.RotateMode.FastBeyond360)
                .SetRelative()
                .SetEase(DG.Tweening.Ease.Linear);

            // 向下移动回到原位置

        }

        private void OnDrawGizmos()
        {

        }

        // #region Effect
        public void ApplyEffect(TileEffect specialEffect)
        {
            DisableEffect();

            this.effect = specialEffect;

            specialEffect.OnCreated(this);
            ApplyIconRendererVisibility();
        }

        public void DisableEffect()
        {
            if (effect != null)
            {
                TileEffect tempEffect = effect;
                effect = null;
                tempEffect.OnDisabled(this);
                Destroy(tempEffect.gameObject);
            }
            m_spawnTweenVisualRenderers = null;
        }

        /// <summary>
        /// GoldenTree 收集时先从棋子解绑并留在棋盘原位，待 UI 飞行开始后再销毁。
        /// </summary>
        void DetachAndDelayDisableGoldenTreeEffect(float delay)
        {
            if (effect == null || effect.EffectType != TileEffectType.GoldenTree)
                return;

            TileEffect effectToDestroy = effect;
            effect = null;
            effectToDestroy.transform.SetParent(null, true);

            DOVirtual.DelayedCall(delay, () =>
            {
                if (effectToDestroy != null)
                {
                    effectToDestroy.OnDisabled(this);
                    Destroy(effectToDestroy.gameObject);
                }
            }).SetLink(gameObject);
        }


        public bool IsCollideWithTile(TileActor otherTile)
        {
            return TileStackUtil.AreTilesOverlappingXY(this, otherTile);
        }

        //前14帧不动，7帧放大到1.1倍 7帧缩小到1.0 7帧放大到1.1倍 7帧缩小到1.0  总共42帧循环
        public void PlayHintAnim()
        {
            StopHintAnim();

            // 每次播放时都重新设置原始大小，确保使用当前正确的大小
            m_hintOriginalScale = transform.localScale;

            float frame = 1f / 30f;
            // 使用 DOTween Sequence 创建循环动画
            hintTweenCase = DOTween.Sequence();

            // 前14帧不动
            hintTweenCase.AppendInterval(14f * frame);

            // 使用 transform 的实例 ID 作为唯一标识，避免多个棋子之间的动画冲突
            object hintId = transform.GetInstanceID();

            // 7帧放大到1.1倍
            hintTweenCase.Append(transform.DOScale(m_hintOriginalScale * 1.1f, 7f * frame).SetId(hintId));

            // 7帧缩小到1.0
            hintTweenCase.Append(transform.DOScale(m_hintOriginalScale * 1.0f, 7f * frame).SetId(hintId));

            // 7帧放大到1.1倍
            hintTweenCase.Append(transform.DOScale(m_hintOriginalScale * 1.1f, 7f * frame).SetId(hintId));

            // 7帧缩小到1.0
            hintTweenCase.Append(transform.DOScale(m_hintOriginalScale * 1.0f, 7f * frame).SetId(hintId));

            // 设置无限循环
            hintTweenCase.SetLoops(-1).SetId(hintId);
        }

        //停到提示动画
        public void StopHintAnim()
        {
            // 停止 hint 动画序列
            if (hintTweenCase != null)
            {
                hintTweenCase.Kill();
                hintTweenCase = null;
            }

            // 使用 transform 的实例 ID 停止当前棋子的 hint 动画，不影响其他棋子
            object hintId = transform.GetInstanceID();
            DOTween.Kill(hintId);

            // 立即恢复到原始大小
            if (m_hintOriginalScale != Vector3.zero)
            {
                transform.localScale = m_hintOriginalScale;
            }
        }

        public void UpdateSprite(Sprite sp)
        {
            iconSpriteRenderer.sprite = sp;
        }

        public void ApplyCoinRewardSkin()
        {
            RuntimeSkinName = LevelDirector.CoinRewardSkinName;
            Sprite sprite = TileSkinCatalog.LoadCoinRewardSprite();
            if (iconSpriteRenderer != null)
            {
                iconSpriteRenderer.sprite = sprite != null ? sprite : defaultIconSprite;
            }
        }

        public void UpdateRuntimeSkin(string skinName)
        {
            if (LevelDirector.IsCoinRewardSkin(skinName) || LevelDirector.IsCoinRewardTile(tileData))
            {
                ApplyCoinRewardSkin();
                return;
            }

            if (string.IsNullOrEmpty(skinName))
            {
                if (LoggedMissingTextureSkins.Add("empty"))
                {
                    Debug.LogWarning("[TileSkin] 缺少纹理资源 skinName 为空，存档/配置未写入花色名");
                }

                ApplyRuntimeSkin(null);
                return;
            }

            TileSkinRuntimeData skin = TileSkinCatalog.GetSkin(skinName);
            if (skin == null && LoggedMissingTextureSkins.Add(skinName))
            {
                Debug.LogWarning($"[TileSkin] 缺少纹理资源 skin={skinName}（配置表/图集中找不到该花色）");
            }

            ApplyRuntimeSkin(skin);
        }

        private void ApplyTileBackground()
        {
            if (backgroundSpriteRenderer == null)
            {
                return;
            }

            Sprite backgroundSprite = TileSkinCatalog.LoadTileBackgroundSprite(LevelDirector.LoadedLevelTheme);
            if (backgroundSprite != null)
            {
                backgroundSpriteRenderer.sprite = backgroundSprite;
            }

            if (isFission)
                IQChallengeVisual.ApplyLook(this);
        }

        private void ApplyRuntimeSkin(TileSkinRuntimeData skin)
        {
            if (LevelDirector.IsCoinRewardTile(tileData))
            {
                ApplyCoinRewardSkin();
                return;
            }

            RuntimeSkinName = skin != null ? skin.Name : "";
            Sprite sprite = skin?.LoadSprite();
            // skin==null 常见于存档恢复：Init 时尚无 RuntimeSkin，随后 UpdateRuntimeSkin 才会赋花色，不当作缺纹理。
            if (skin != null && sprite == null && LoggedMissingTextureSkins.Add(skin.Name))
            {
                Debug.LogWarning(
                    $"[TileSkin] 缺少纹理资源 skin={skin.Name}, color={skin.Color}, type={skin.Type}, " +
                    $"assetPath={skin.AssetPath}, atlasPath={skin.AtlasPath}"
                );
            }

            if (iconSpriteRenderer != null)
            {
                iconSpriteRenderer.sprite = sprite != null ? sprite : defaultIconSprite;
            }

            if (isFission)
                IQChallengeVisual.ApplyLook(this);
        }
    }
}
