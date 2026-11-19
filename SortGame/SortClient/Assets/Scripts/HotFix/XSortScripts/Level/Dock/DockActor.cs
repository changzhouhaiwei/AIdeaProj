using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XSortGameCore;
using Spine.Unity;
using FishFramework;
using DG.Tweening;
using TMPro;


namespace FishXSortPlay
{
    [System.Serializable]
    public class DockActor : MonoBehaviour
    {
        private static DockActor instance;

        [SerializeField] GameObject trailPrefab;
        [SerializeField] AnimationCurve positionYCurve;
        [SerializeField] GameObject slotPrefab;
        [SerializeField] Transform backgroundTran;
        /// <summary> 背景与所有 Slot 的共同父节点；不填则使用本 Dock 根 <see cref="transform"/>，用于 <see cref="VisualGroup"/> 整体动画。 </summary>
        [SerializeField] Transform dockVisualRoot;

        Transform dockBgTheme;
        Transform dockBgThemeIce;
        Transform levelLbRoot;
        Transform dockGlowSet;
        Transform dockGlowSetBoxCover;
        Sprite cachedDefaultDockBackgroundSprite;
        Sprite cachedBoxCoverDockBackgroundSprite;
        Vector2 cachedDockBackgroundSize = new Vector2(8.08f, 3.2f);
        bool cachedDockBackgroundSizeReady;

        const string DefaultBoxCoverDockSpritePath =
            "Assets/CABRes/SpriteAtlas/GamePlay/XSort_fangzhidi_0.png";

        [SerializeField] Transform shakeRoot;
        [SerializeField] TextMeshPro comboText;
        private static List<SlotActor> slots;

        private readonly DockVisualCluster dockVisualGroup = new DockVisualCluster();


        private Pool trailPool;
        private LevelDirector levelController;
        private Vector3 defaultContainerPosition;
        private IDockable lastPickedObject;
        /// <summary> 用户收集顺序（含点击/提示等经 SubmitToSlot 的棋子），用于撤销最后拾取而非 Dock 最右槽。 </summary>
        private readonly List<IDockable> pickedHistory = new List<IDockable>();

        public bool IsFilled
        {
            get
            {
                if (slots == null || slots.Count == 0)
                    return false;
                return slots[^1].IsOccupied;
            }
            set
            {
                // value
            }
        }//=> slots[^2].IsOccupied;
        public bool IsEmpty => slots == null || slots.Count == 0 || !slots[0].IsOccupied;

        private TweenClip delayTweenCase;
        private int addedDepth = 0;

        public static IDockable LastPickedObject => instance.lastPickedObject;
        public static AnimationCurve PositionYCurve => instance.positionYCurve;

        /// <summary> Dock 背景、槽位与已提交棋子的整体动画入口。 </summary>
        public DockVisualCluster VisualGroup => dockVisualGroup;

        public float BoxCoverTopClearance => Mathf.Max(0f, boxCoverTopClearance);
        public float BoxCoverBoardClearance => Mathf.Max(0f, boxCoverBoardClearance);
        public float BoxCoverDockUpOffset => boxCoverDockUpOffset;

        public float GetRequiredBoxCoverVisualTopWorldY()
        {
            if (boxCoverVisualTopBoundary == null)
                throw new MissingReferenceException(
                    "DockActor.boxCoverVisualTopBoundary must be wired on the Dock prefab.");
            return boxCoverVisualTopBoundary.position.y;
        }

        public float GetRequiredBoxCoverBoardTopWorldY()
        {
            if (boxCoverBoardTopBoundary == null)
                throw new MissingReferenceException(
                    "DockActor.boxCoverBoardTopBoundary must be wired on the Dock prefab.");
            return boxCoverBoardTopBoundary.position.y;
        }

        /// <summary>
        /// Dock 槽位可视下边缘的世界坐标 Y（用于棋盘居中上界）。
        /// Dock 登场动画会把槽位根节点 scale 压到 0，直接量 Renderer bounds 会随进关时机漂移；
        /// 这里把根节点动画缩放除掉，按“静止状态”的精灵半高推算，结果与动画时机无关。
        /// </summary>
        public float GetSlotsBottomWorldY()
        {
            float minY = float.MaxValue;
            bool any = false;

            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot == null)
                    {
                        continue;
                    }

                    Transform slotRoot = slot.transform;
                    float parentLossyY = slotRoot.parent != null
                        ? Mathf.Abs(slotRoot.parent.lossyScale.y)
                        : 1f;

                    var renderers = slot.GetComponentsInChildren<SpriteRenderer>(true);
                    for (int r = 0; r < renderers.Length; r++)
                    {
                        var rend = renderers[r];
                        if (rend == null || rend.sprite == null || !rend.enabled)
                        {
                            continue;
                        }

                        // 沿局部层级累计到槽位根（不含根自身的动画缩放）
                        float localScaleY = 1f;
                        float localOffsetY = 0f;
                        Transform t = rend.transform;
                        bool chainOk = true;
                        while (t != null && t != slotRoot)
                        {
                            localOffsetY = t.localPosition.y + t.localScale.y * localOffsetY;
                            localScaleY *= t.localScale.y;
                            t = t.parent;
                        }
                        if (t == null)
                        {
                            chainOk = false;
                        }
                        if (!chainOk)
                        {
                            continue;
                        }

                        float worldScaleY = Mathf.Abs(localScaleY) * parentLossyY;
                        if (worldScaleY <= 1e-4f)
                        {
                            continue;
                        }

                        float centerY = slot.Position.y
                            + (localOffsetY + rend.sprite.bounds.center.y * localScaleY) * parentLossyY;
                        float y = centerY - rend.sprite.bounds.extents.y * worldScaleY;
                        if (y < minY)
                        {
                            minY = y;
                        }

                        any = true;
                    }
                }
            }

            if (any)
            {
                return minY;
            }

            return transform.position.y - LevelFitter.SlotSize.y * 0.5f;
        }

        public Vector3 GetHudAnchorWorldPosition()
        {
            EnsureHudAnchor();
            return hudAnchor != null
                ? hudAnchor.position
                : transform.TransformPoint(BoxCoverLayout.DefaultHudAnchorLocal);
        }

        public Vector3 GetHudAnchorLayoutWorldPosition()
        {
            Vector3 world = GetHudAnchorWorldPosition();
            if (holdBoxCoverStillDuringBreathing)
                return world;
            return UnscaleFromDockBreathing(world, hudAnchor);
        }

        public bool TryGetBoxCoverBreathingFollow(out Vector3 restWorld, out Vector3 currentWorld)
        {
            restWorld = currentWorld = default;
            if (holdBoxCoverStillDuringBreathing)
                return false;
            EnsureBoxPandaRoot();
            Transform follow = boxPandaRoot != null ? boxPandaRoot : hudAnchor;
            if (follow == null || !IsUnderBreathingRoot(follow))
                return false;
            if (warningBreathingCase == null || !warningBreathingCase.IsActive)
                return false;

            currentWorld = follow.position;
            restWorld = UnscaleFromDockBreathing(currentWorld, follow);
            return true;
        }

        bool IsUnderBreathingRoot(Transform target)
        {
            Transform visualRoot = dockVisualGroup.VisualRoot;
            if (target == null || visualRoot == null)
                return false;
            return target == visualRoot || target.IsChildOf(visualRoot);
        }

        Vector3 UnscaleFromDockBreathing(Vector3 world, Transform source)
        {
            if (!IsUnderBreathingRoot(source))
                return world;

            float factor = dockVisualGroup.GetBreathingUniformFactor();
            if (factor < 1e-4f || Mathf.Abs(factor - 1f) < 1e-5f)
                return world;

            Transform visualRoot = dockVisualGroup.VisualRoot;
            Vector3 pivot = visualRoot.position;
            return pivot + (world - pivot) / factor;
        }

        void EnsureHudAnchor()
        {
            if (hudAnchor != null)
            {
                EnsureHudAnchorUnderVisualRoot();
                return;
            }

            Transform found = transform.Find("HudAnchor");
            if (found == null && backgroundTran != null)
            {
                found = backgroundTran.Find("HudAnchor");
            }

            if (found != null)
            {
                hudAnchor = found;
                EnsureHudAnchorUnderVisualRoot();
                return;
            }

            GameObject created = new GameObject("HudAnchor");
            Transform parent = backgroundTran != null ? backgroundTran : transform;
            created.transform.SetParent(parent, false);
            created.transform.localPosition = BoxCoverLayout.DefaultHudAnchorLocal;
            hudAnchor = created.transform;
        }

        /// <summary>
        /// 警告呼吸作用在 dockVisualRoot/Background 上。有目标栏时狗狗和收集面板不跟这段缩放。
        /// </summary>
        void EnsureHudAnchorUnderVisualRoot()
        {
            if (hudAnchor == null)
            {
                return;
            }

            Transform visualRoot = backgroundTran != null
                ? backgroundTran
                : dockVisualGroup.VisualRoot;
            if (visualRoot == null || hudAnchor.parent == visualRoot)
            {
                return;
            }

            hudAnchor.SetParent(visualRoot, true);
        }

        public static event Action<IDockable> ElementAdded;
        public static event Action<List<IDockable>> MatchCombined;

        [SerializeField] SkeletonAnimation stateAnim;
        // [SerializeField] SkeletonAnimation stateClearAnim;
        [SerializeField] SkeletonAnimation longPressAnim;

        [SerializeField] XSortLongPressHintEffect longPressHintEff;

        [SerializeField] GameObject fingerAnimEff;

        [SerializeField] GameObject stateClearEff;

        [SerializeField] GameObject warningEFF;

        [Header("Dock 将满时 VisualRoot 整体呼吸（CheckWarningState：剩余空位≤1）；请配置 dockVisualRoot 含背景+槽位")]
        [SerializeField] float warningPulseScaleMultiplier = 1.045f;
        [SerializeField] float warningPulseHalfDuration = 0.6f;

        private bool canShowMatchClear;
        private TweenClip warningBreathingCase;
        private DG.Tweening.Tween submitFlightDockShakeTween;
        Transform boxPandaShakeHomeParent;
        int boxPandaShakeHomeSibling;
        Vector3 boxPandaShakeHomeLocalPos;
        Quaternion boxPandaShakeHomeLocalRot;
        Vector3 boxPandaShakeHomeLocalScale;
        bool boxPandaShakeDetached;
        private Vector3 dockVisualRootScaleBaseline = Vector3.one;

        [SerializeField] Transform hudAnchor;
        [SerializeField] Transform boxPandaRoot;
        [Header("BoxCover Layout Boundaries")]
        [SerializeField] Transform boxCoverVisualTopBoundary;
        [SerializeField] Transform boxCoverBoardTopBoundary;
        [SerializeField, Min(0f)] float boxCoverTopClearance = 0.08f;
        [SerializeField, Min(0f)] float boxCoverBoardClearance = 0.25f;
        [SerializeField] float boxCoverDockUpOffset = 0.18f;
        SkeletonAnimation boxPanda;
        GameObject boxPandaInstance;
        [SerializeField] Transform iqPandaRoot;
        GameObject iqPandaInstance;
        int iqPandaShownLevel;
        DG.Tweening.Tween iqPandaPromote;
        Vector3 iqPandaRootAuthoredScale = new Vector3(-1f, 1f, 1f);
        bool iqPandaRootScaleCached;
        Vector3 boxPandaRootAuthoredScale = new Vector3(-1f, 1f, 1f);
        bool boxPandaRootScaleCached;
        Vector3 hudAnchorAuthoredLocalPos;
        Vector3 hudAnchorAuthoredLocalScale = Vector3.one;
        bool hudAnchorPoseCached;
        bool holdBoxCoverStillDuringBreathing;
        Vector3 authoredContainerPosition;
        bool hasAuthoredContainerPosition;
        bool m_isBoxCoverQuickFinishing;
        bool m_boxCoverVictoryClearing;
        Coroutine m_boxCoverQuickFinishCoroutine;
        Action m_boxCoverQuickFinishCallback;
        bool m_boxCoverTimeScaleOverride;
        float m_boxCoverTimeScaleBefore = 1f;
        // Spine 已换为 BoxDog，动画名 Idle / HS / Out 与原先 BoxPanda 对应
        const string BoxPandaIdleAnimName = "BoxDog_Idle";
        const string BoxPandaCollectAnimName = "BoxDog_HS";
        const string BoxPandaOutAnimName = "BoxDog_Out";
        const string BoxPandaPrefabPath = "Assets/CABRes/Prefabs/BoxCover/BoxDog.prefab";
        const string IqPandaPrefabFolder = "Assets/CABRes/Prefabs/IQChallenge/";
        const string IqPandaChangeEffPath = IqPandaPrefabFolder + "ChangeEff.prefab";
        const string IqPandaIdleAnimName = "Idle";
        const int IqPandaLevelCount = 5;
        const int IqPandaSortingOrder = -175;
        const float IqPandaPromoteDuration = 0.2f;
        // guideCaptureBg 是 -25，收集栏本体是 -5 起。狗预制体是 -175，引导截图会把它盖住。
        const int BoxPandaSortingDuringGuide = -24;
        int boxPandaSortingBeforeGuide;
        bool boxPandaGuideSortingSaved;
        Action boxPandaExitCallback;
        Action boxPandaOutStartedCallback;
        DG.Tweening.Tween boxPandaOutStartDelay;
        DG.Tweening.Tween boxPandaExitDelay;
        bool boxPandaNeedsIntro;

        private bool m_isFinalAniming;//跳起动画控制.
        private bool m_isClearAniming;//clear动画

        private bool m_isQuickFinishing = false; // 是否正在进行快速结算
        bool m_quickFinishPending; // 胜利条已出，1s 后才真正开始
        DG.Tweening.Tween m_quickFinishDelay;
        private Coroutine m_quickFinishCoroutine; // 快速结算协程

        [SerializeField] float quickFinishXSortInterval = 0.33f; // 快速消除时每个棋子提交的间隔时间，可以修改

        [SerializeField] ImpackEffectPool impackEffectPool;

        int m_lastRestCnt = 0;//上次剩余棋子数
        int m_comboCnt = 0;//
        /// <summary>本局已结束各段连击的峰值累加（不含当前未断段）。</summary>
        int m_comboPeakSum = 0;
        // 用于忽略被后续消除覆盖的延迟提示回调，避免旧回调重置新连击的倍率文字。
        int m_comboPromptVersion = 0;
        int m_matchClearGeneration = 0;
        int m_mergeSoundPitchStep;
        Sequence m_comboTextHideSequence;
        DG.Tweening.Tween m_comboTextScaleTween;
        int m_comboEffectSession;
        float m_lastComboUpdateTime;
        const float ComboIdleHideDelay = 2f;
        const float ComboNumberFallbackAppearDuration = 0.32f;
        const string ComboSpineAnimName = "lv_6";

        [SerializeField] GameObject happyTextEffect;
        Transform happyTextRoot;
        [SerializeField] GameObject comboTextEffect;
        Transform comboNumberAnchor;
        ParticleSystem comboWordParticle;
        [SerializeField] Vector3 comboEffectMiddleXSortOffset = new(1.2f, 0f, 0f);
        [SerializeField] Vector2 comboEffectViewportXRange = new(0.08f, 0.72f);
        [SerializeField] Vector2 comboEffectRightViewportPosition = new(0.8f, 0.64f);

        public void Init(LevelDirector levelController)
        {
            instance = this;

            this.levelController = levelController;

            CaptureAuthoredPositionIfNeeded();
            defaultContainerPosition = authoredContainerPosition;
            transform.position = authoredContainerPosition;

            ClearPickHistory();

            m_lastRestCnt = 0;
            m_comboCnt = 0;
            m_comboPeakSum = 0;
            // 上一关遗留的延迟回调不应影响本关的连击提示。
            m_comboPromptVersion++;

            slots = new List<SlotActor>();
            transform.GetComponentsInChildren(slots);

            slots.Sort((slot1, slot2) => (int)((-slot2.Position.x + slot1.Position.x) * 100));

            dockVisualGroup.Bind(dockVisualRoot != null ? dockVisualRoot : transform, backgroundTran, () => slots);

            if (dockVisualGroup.VisualRoot != null)
            {
                dockVisualRootScaleBaseline = dockVisualGroup.VisualRoot.localScale;
            }

            CacheBoxPandaRootAuthoredScale();

            if (impackEffectPool == null)
                impackEffectPool = GetComponentInChildren<ImpackEffectPool>(true);

            impackEffectPool?.ResetPlaybackAfterSettlement();

            EnsureTextEffects();

            // 每次进入关卡前隐藏动画
            HideAnimations();

            // 确保警告状态正确
            StopWarning();
            ApplyTheme();
        }

        /// <summary>
        /// 按本局 XSortbaseId / 可乐箱或裂变棋状态换备战栏底板 / 槽位图；空字段保持预制体默认。
        /// 特殊玩法：挂了 columnBoxCover 才换；否则主题关仍用 column，无主题才用 XSort_fangzhidi_0。
        /// 普通：优先 ThemePack.column，否则预制体默认底板。
        /// </summary>
        public void ApplyTheme()
        {
            EnsureTrailPool();
            EnsureDockBgTheme();
            CacheDefaultDockBackgroundSprite();

            XSortBaseThemePack pack = ThemeUiApplier.Get();
            bool specialDock = SpecialPlayDock.UsesNoLevelIdPlate;
            Sprite columnSprite = ResolveDockColumnSprite(pack, specialDock);
            bool showColumn = columnSprite != null;
            if (dockBgTheme != null)
            {
                dockBgTheme.gameObject.SetActive(showColumn);
                if (showColumn && dockBgThemeIce != null)
                {
                    dockBgThemeIce.gameObject.SetActive(true);
                    SpriteRenderer renderer = dockBgThemeIce.GetComponent<SpriteRenderer>();
                    ApplyDockPlateSprite(renderer, columnSprite);
                }
            }

            SpriteRenderer backgroundRenderer = backgroundTran != null
                ? backgroundTran.GetComponent<SpriteRenderer>()
                : null;
            if (backgroundRenderer != null)
            {
                backgroundRenderer.enabled = !showColumn;
                if (!showColumn)
                {
                    // 仅「无主题底板」时，可乐箱 / 裂变棋才落到全局 XSort_fangzhidi_0
                    Sprite bgSprite = specialDock
                        ? GetBoxCoverDockBackgroundSprite()
                        : cachedDefaultDockBackgroundSprite;
                    ApplyDockPlateSprite(backgroundRenderer, bgSprite);
                }
            }

            // 目标栏占用备战栏区域，隐藏关卡号
            SetLevelLbVisible(!specialDock);

            Sprite slotSprite = pack != null ? pack.slot : null;
            if (slots == null)
            {
                HideWarningGlow();
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i]?.ApplySlotUi(slotSprite);
            }

            HideWarningGlow();
        }

        void SetLevelLbVisible(bool visible)
        {
            EnsureLevelLb();
            if (levelLbRoot != null)
            {
                levelLbRoot.gameObject.SetActive(visible);
            }
        }

        void EnsureLevelLb()
        {
            if (levelLbRoot != null)
            {
                return;
            }

            if (backgroundTran != null)
            {
                levelLbRoot = backgroundTran.Find("LevelLb");
            }

            if (levelLbRoot == null)
            {
                levelLbRoot = transform.Find("LevelLb");
            }

            if (levelLbRoot == null)
            {
                Transform visualRoot = dockVisualRoot != null ? dockVisualRoot : transform;
                levelLbRoot = visualRoot.Find("LevelLb");
            }
        }

        static Sprite ResolveDockColumnSprite(XSortBaseThemePack pack, bool specialDock)
        {
            if (pack == null)
            {
                return null;
            }

            if (specialDock)
            {
                // 只有挂点显式配置了才换；空挂点则继续用主题 column
                if (pack.columnBoxCover != null)
                {
                    return pack.columnBoxCover;
                }

                return pack.column;
            }

            return pack.column;
        }

        void ShowWarningGlow()
        {
            EnsureDockGlowSets();
            bool useBoxCoverGlow = ShouldUseBoxCoverWarningGlow();
            if (dockGlowSet != null)
                dockGlowSet.gameObject.SetActive(!useBoxCoverGlow);
            if (dockGlowSetBoxCover != null)
                dockGlowSetBoxCover.gameObject.SetActive(useBoxCoverGlow);
        }

        void HideWarningGlow()
        {
            EnsureDockGlowSets();
            if (dockGlowSet != null)
                dockGlowSet.gameObject.SetActive(false);
            if (dockGlowSetBoxCover != null)
                dockGlowSetBoxCover.gameObject.SetActive(false);
            if (warningEFF != null)
                warningEFF.SetActive(false);
        }

        bool ShouldUseBoxCoverWarningGlow()
        {
            if (!SpecialPlayDock.HasGoalHud || dockGlowSetBoxCover == null)
                return false;

            XSortBaseThemePack pack = ThemeUiApplier.Get();
            return pack != null && pack.columnBoxCover != null;
        }

        void EnsureDockGlowSets()
        {
            Transform root = backgroundTran != null ? backgroundTran : transform;
            if (dockGlowSet == null)
                dockGlowSet = root.Find("GlowSet");
            if (dockGlowSetBoxCover == null)
                dockGlowSetBoxCover = root.Find("GlowSet_01");
        }

        void CacheDefaultDockBackgroundSprite()
        {
            if (backgroundTran == null)
            {
                return;
            }

            SpriteRenderer backgroundRenderer = backgroundTran.GetComponent<SpriteRenderer>();
            if (backgroundRenderer == null)
            {
                return;
            }

            CacheDockBackgroundSize(backgroundRenderer);

            if (cachedDefaultDockBackgroundSprite == null)
            {
                cachedDefaultDockBackgroundSprite = backgroundRenderer.sprite;
            }
        }

        void CacheDockBackgroundSize(SpriteRenderer renderer)
        {
            if (cachedDockBackgroundSizeReady || renderer == null)
            {
                return;
            }

            // 无 border 的主题图在 Simple 下 size 会变成原图像素尺寸，不能当成备战栏基准。
            bool sliced = renderer.drawMode == SpriteDrawMode.Sliced || renderer.drawMode == SpriteDrawMode.Tiled;
            bool bordered = renderer.sprite != null && renderer.sprite.border.sqrMagnitude > 0.0001f;
            if (!sliced || !bordered || renderer.size.x <= 0.01f || renderer.size.y <= 0.01f)
            {
                return;
            }

            cachedDockBackgroundSize = renderer.size;
            cachedDockBackgroundSizeReady = true;
        }

        /// <summary>
        /// 备战栏默认图有 9-slice border，主题图多数没有。
        /// 无 border 仍用 Sliced + 图集打包时会采到邻图，所以改 Simple，再用缩放拉回基准尺寸。
        /// 切回普通关必须把缩放和 Sliced size 都还原，否则会一直停在主题图的原始大小。
        /// </summary>
        void ApplyDockPlateSprite(SpriteRenderer renderer, Sprite sprite)
        {
            if (renderer == null)
            {
                return;
            }

            CacheDockBackgroundSize(renderer);
            if (sprite == null)
            {
                renderer.transform.localScale = Vector3.one;
                return;
            }

            if (sprite.border.sqrMagnitude > 0.0001f)
            {
                renderer.transform.localScale = Vector3.one;
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.sprite = sprite;
                renderer.size = cachedDockBackgroundSize;
                return;
            }

            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.sprite = sprite;
            float ppu = sprite.pixelsPerUnit > 1f ? sprite.pixelsPerUnit : 100f;
            float nativeW = Mathf.Max(0.01f, sprite.rect.width / ppu);
            float nativeH = Mathf.Max(0.01f, sprite.rect.height / ppu);
            // 底板节点下没有槽位和关卡号，只缩放这张图，避免主题原图把整条备战栏撑大。
            if (renderer.transform.childCount == 0)
            {
                renderer.transform.localScale = new Vector3(
                    cachedDockBackgroundSize.x / nativeW,
                    cachedDockBackgroundSize.y / nativeH,
                    1f);
            }
        }

        Sprite GetBoxCoverDockBackgroundSprite()
        {
            if (cachedBoxCoverDockBackgroundSprite != null)
            {
                return cachedBoxCoverDockBackgroundSprite;
            }

            cachedBoxCoverDockBackgroundSprite = UILoadHelper.LoadAsset<Sprite>(DefaultBoxCoverDockSpritePath);
            if (cachedBoxCoverDockBackgroundSprite == null)
            {
                cachedBoxCoverDockBackgroundSprite = cachedDefaultDockBackgroundSprite;
            }

            return cachedBoxCoverDockBackgroundSprite;
        }

        public static bool NotifyBoxPanda(bool visible, bool collectReaction)
        {
            if (instance == null)
                return false;
            if (!visible)
            {
                instance.HideBoxPanda();
                return false;
            }

            if (collectReaction)
            {
                instance.PlayBoxPandaCollect();
                return false;
            }

            return instance.PlayBoxPandaIdle(false);
        }

        public static void HideIqPanda()
        {
            if (instance == null)
                return;
            instance.HideIqPandaNow();
        }

        public static void ShowIqPanda(int level, bool promote)
        {
            if (instance == null)
                return;
            instance.ShowIqPandaNow(level, promote);
        }

        /// <summary>目标栏狗：收集反应或进场。进场时返回 true，供 HUD 同步淡入。</summary>
        public static bool PresentGoalHudDog(bool collectReaction, bool allowIntro)
        {
            bool dogIntro = NotifyBoxPanda(true, collectReaction);
            if (!dogIntro && allowIntro)
                dogIntro = RestartIntroIfPlaying();
            return dogIntro;
        }

        public static void PlayGoalHudAppearIfDogIntro(bool dogIntro)
        {
            if (!dogIntro)
                return;

            float introDuration = GetBoxPandaOutDuration();
            UIController.GetPage<UIGame>()?.PlayBoxCoverHudAppear(introDuration, introDuration);
        }

        public static bool RestartIntroIfPlaying()
        {
            if (instance == null || !instance.IsPlayingReverseIntro())
                return false;

            return instance.PlayBoxPandaIntro();
        }

        public static void PlayBoxPandaExitThen(Action onOutStarted, Action onFinished)
        {
            if (instance == null)
            {
                onOutStarted?.Invoke();
                onFinished?.Invoke();
                return;
            }

            instance.PlayBoxPandaExitThenNow(onOutStarted, onFinished);
        }

        public static float GetBoxPandaOutDuration()
        {
            if (instance == null
                || !instance.TryGetBoxPandaState(out Spine.AnimationState _, out Spine.SkeletonData data))
                return 0.4f;

            Spine.Animation outAnim = data.FindAnimation(BoxPandaOutAnimName);
            return outAnim != null && outAnim.Duration > 0.05f ? outAnim.Duration : 0.4f;
        }

        void EnsureBoxPandaRoot()
        {
            EnsureHudAnchor();
            if (boxPandaRoot != null)
                return;

            Transform hud = hudAnchor != null ? hudAnchor : transform.Find("HudAnchor");
            if (hud == null)
                return;

            Transform found = hud.Find("boxPandaRoot");
            if (found != null)
            {
                boxPandaRoot = found;
                return;
            }

            GameObject created = new GameObject("boxPandaRoot");
            created.transform.SetParent(hud, false);
            created.transform.localPosition = Vector3.zero;
            created.transform.localScale = boxPandaRootAuthoredScale;
            boxPandaRoot = created.transform;
            boxPandaRootScaleCached = true;
        }

        void CacheBoxPandaRootAuthoredScale()
        {
            EnsureBoxPandaRoot();
            if (boxPandaRoot == null || boxPandaRootScaleCached)
                return;
            boxPandaRootAuthoredScale = boxPandaRoot.localScale;
            boxPandaRootScaleCached = true;
        }

        void SyncBoxPandaAgainstDockScale()
        {
            EnsureBoxPandaRoot();
            SyncIqPandaRootAgainstDockScale();
            Transform visualRoot = dockVisualGroup.VisualRoot;
            if (boxPandaRoot == null || visualRoot == null)
                return;
            if (boxPandaRoot != visualRoot && !boxPandaRoot.IsChildOf(visualRoot))
                return;

            float factor = dockVisualGroup.GetBreathingUniformFactor();
            if (factor < 1e-4f)
                factor = 1f;
            Vector3 compensated = boxPandaRootAuthoredScale / factor;
            if ((boxPandaRoot.localScale - compensated).sqrMagnitude > 1e-8f)
                boxPandaRoot.localScale = compensated;
        }

        void RestoreBoxPandaRootScale()
        {
            if (boxPandaRoot != null)
                boxPandaRoot.localScale = boxPandaRootAuthoredScale;
            if (iqPandaRoot != null && iqPandaRootScaleCached)
                iqPandaRoot.localScale = iqPandaRootAuthoredScale;
        }

        void EnsureIqPandaRoot()
        {
            if (iqPandaRoot != null)
                return;

            Transform hud = hudAnchor != null ? hudAnchor : transform.Find("HudAnchor");
            if (hud == null)
                return;

            Transform found = hud.Find("IQPandaRoot");
            if (found != null)
            {
                iqPandaRoot = found;
                return;
            }

            var created = new GameObject("IQPandaRoot");
            created.transform.SetParent(hud, false);
            if (boxPandaRoot != null)
            {
                created.transform.localPosition = boxPandaRoot.localPosition;
                created.transform.localRotation = boxPandaRoot.localRotation;
                created.transform.localScale = boxPandaRoot.localScale;
            }
            else
            {
                created.transform.localPosition = new Vector3(-0.048f, -0.163f, 0f);
                created.transform.localScale = new Vector3(-1f, 1f, 1f);
            }

            iqPandaRoot = created.transform;
        }

        void CacheIqPandaRootAuthoredScale()
        {
            EnsureIqPandaRoot();
            if (iqPandaRoot == null || iqPandaRootScaleCached)
                return;
            iqPandaRootAuthoredScale = iqPandaRoot.localScale;
            iqPandaRootScaleCached = true;
        }

        void SyncIqPandaRootAgainstDockScale()
        {
            EnsureIqPandaRoot();
            CacheIqPandaRootAuthoredScale();
            Transform visualRoot = dockVisualGroup.VisualRoot;
            if (iqPandaRoot == null || visualRoot == null)
                return;
            if (iqPandaRoot != visualRoot && !iqPandaRoot.IsChildOf(visualRoot))
                return;

            float factor = dockVisualGroup.GetBreathingUniformFactor();
            if (factor < 1e-4f)
                factor = 1f;
            Vector3 compensated = iqPandaRootAuthoredScale / factor;
            if ((iqPandaRoot.localScale - compensated).sqrMagnitude > 1e-8f)
                iqPandaRoot.localScale = compensated;
        }

        void HideIqPandaNow()
        {
            iqPandaPromote?.Kill();
            iqPandaPromote = null;
            if (iqPandaInstance != null)
                Destroy(iqPandaInstance);
            iqPandaInstance = null;
            iqPandaShownLevel = 0;
            ClearIqPandaChangeEff();
        }

        void ShowIqPandaNow(int level, bool promote)
        {
            level = Mathf.Clamp(level, 1, IqPandaLevelCount);
            EnsureIqPandaRoot();
            CacheIqPandaRootAuthoredScale();
            if (iqPandaRoot == null)
                return;
            if (iqPandaInstance != null && iqPandaShownLevel == level)
                return;

            GameObject incoming = CreateIqPanda(level);
            if (incoming == null)
                return;

            if (!promote || iqPandaInstance == null)
            {
                if (iqPandaInstance != null)
                    Destroy(iqPandaInstance);
                incoming.transform.localScale = Vector3.one;
                iqPandaInstance = incoming;
                iqPandaShownLevel = level;
                return;
            }

            GameObject outgoing = iqPandaInstance;
            iqPandaInstance = incoming;
            iqPandaShownLevel = level;
            PlayIqPandaChangeEff();
            outgoing.transform.localScale = Vector3.one;
            incoming.transform.localScale = Vector3.zero;
            iqPandaPromote?.Kill();
            Sequence seq = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            seq.Join(outgoing.transform.DOScale(Vector3.zero, IqPandaPromoteDuration).SetEase(Ease.OutQuad));
            seq.Join(incoming.transform.DOScale(Vector3.one, IqPandaPromoteDuration).SetEase(Ease.OutQuad));
            seq.OnComplete(() =>
            {
                if (outgoing != null)
                    Destroy(outgoing);
            });
            iqPandaPromote = seq;
        }

        GameObject CreateIqPanda(int level)
        {
            string path = IqPandaPrefabFolder + "Dog_lv" + level + ".prefab";
            GameObject prefab = UILoadHelper.LoadAsset<GameObject>(path);
#if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#endif
            if (prefab == null)
            {
                Debug.LogError($"[IQ] panda prefab is missing: {path}");
                return null;
            }

            GameObject go = Instantiate(prefab, iqPandaRoot, false);
            go.name = "Dog_lv" + level;
            go.layer = iqPandaRoot.gameObject.layer;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            SkeletonAnimation anim = go.GetComponent<SkeletonAnimation>();
            if (anim == null)
                anim = go.GetComponentInChildren<SkeletonAnimation>(true);
            if (anim != null)
            {
                anim.PhysicsPositionInheritanceFactor = Vector2.zero;
                anim.PhysicsRotationInheritanceFactor = 0f;
                if (anim.AnimationState != null)
                    anim.AnimationState.SetAnimation(0, IqPandaIdleAnimName, true);
            }

            MeshRenderer mesh = go.GetComponent<MeshRenderer>();
            if (mesh == null)
                mesh = go.GetComponentInChildren<MeshRenderer>(true);
            if (mesh != null)
                mesh.sortingOrder = IqPandaSortingOrder;
            return go;
        }

        void PlayIqPandaChangeEff()
        {
            if (iqPandaRoot == null)
                return;

            GameObject prefab = UILoadHelper.LoadAsset<GameObject>(IqPandaChangeEffPath);
#if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(IqPandaChangeEffPath);
#endif
            if (prefab == null)
            {
                Debug.LogError($"[IQ] panda change effect is missing: {IqPandaChangeEffPath}");
                return;
            }

            GameObject go = Instantiate(prefab, iqPandaRoot, false);
            go.name = "ChangeEff";
            go.layer = iqPandaRoot.gameObject.layer;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            float life = 0.5f;
            ParticleSystem[] systems = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                if (system == null)
                    continue;

                ParticleSystem.MainModule main = system.main;
                life = Mathf.Max(life, main.duration + main.startLifetime.constantMax);
                system.Play(true);
            }

            PlaceChangeEffBetweenPandaAndDock(go);
            Destroy(go, life + 0.15f);
        }

        void PlaceChangeEffBetweenPandaAndDock(GameObject go)
        {
            if (go == null)
                return;

            int dockOrder = -4;
            int sortingLayerId = 0;
            SpriteRenderer dockPlate = backgroundTran != null ? backgroundTran.GetComponent<SpriteRenderer>() : null;
            if (dockPlate != null)
            {
                dockOrder = dockPlate.sortingOrder;
                sortingLayerId = dockPlate.sortingLayerID;
            }

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            int maxOrder = int.MinValue;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    maxOrder = Mathf.Max(maxOrder, renderers[i].sortingOrder);
            }

            if (maxOrder == int.MinValue)
                return;

            int shift = (dockOrder - 1) - maxOrder;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.sortingLayerID = sortingLayerId;
                int order = renderer.sortingOrder + shift;
                if (order <= IqPandaSortingOrder)
                    order = IqPandaSortingOrder + 1;
                renderer.sortingOrder = order;
            }
        }

        void ClearIqPandaChangeEff()
        {
            if (iqPandaRoot == null)
                return;

            for (int i = iqPandaRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = iqPandaRoot.GetChild(i);
                if (child != null && child.name == "ChangeEff")
                    Destroy(child.gameObject);
            }
        }

        void CacheHudAnchorAuthoredPose()
        {
            EnsureHudAnchor();
            if (hudAnchor == null)
                return;
            hudAnchorAuthoredLocalPos = hudAnchor.localPosition;
            hudAnchorAuthoredLocalScale = hudAnchor.localScale;
            hudAnchorPoseCached = true;
        }

        void PinHudAnchorAgainstDockBreathing()
        {
            if (!hudAnchorPoseCached)
                CacheHudAnchorAuthoredPose();
            if (hudAnchor == null || !IsUnderBreathingRoot(hudAnchor))
                return;

            float factor = dockVisualGroup.GetBreathingUniformFactor();
            if (factor < 1e-4f)
                factor = 1f;
            Vector3 scale = hudAnchorAuthoredLocalScale / factor;
            Vector3 pos = hudAnchorAuthoredLocalPos / factor;
            if ((hudAnchor.localScale - scale).sqrMagnitude > 1e-8f)
                hudAnchor.localScale = scale;
            if ((hudAnchor.localPosition - pos).sqrMagnitude > 1e-8f)
                hudAnchor.localPosition = pos;
        }

        void RestoreHudAnchorPose()
        {
            if (!hudAnchorPoseCached || hudAnchor == null)
                return;
            hudAnchor.localPosition = hudAnchorAuthoredLocalPos;
            hudAnchor.localScale = hudAnchorAuthoredLocalScale;
        }

        void RestoreBoxCoverFromDockBreathing()
        {
            holdBoxCoverStillDuringBreathing = false;
            RestoreHudAnchorPose();
            RestoreBoxPandaRootScale();
            BoxCoverManager.RestoreHudFromDockBreathing();
        }

        void SyncBoxCoverAgainstDockScale()
        {
            if (holdBoxCoverStillDuringBreathing)
            {
                PinHudAnchorAgainstDockBreathing();
                RestoreBoxPandaRootScale();
                BoxCoverManager.RestoreHudFromDockBreathing();
                return;
            }

            SyncBoxPandaAgainstDockScale();
            BoxCoverManager.SyncHudToDockBreathing();
        }

        void EnsureBoxPanda()
        {
            if (boxPanda != null)
                return;

            EnsureBoxPandaRoot();
            if (boxPandaRoot == null)
                return;

            GameObject prefab = UILoadHelper.LoadAsset<GameObject>(BoxPandaPrefabPath);
#if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BoxPandaPrefabPath);
#endif
            if (prefab == null)
            {
                Debug.LogError($"[BoxCover] BoxDog prefab is missing: {BoxPandaPrefabPath}");
                return;
            }

            boxPandaInstance = Instantiate(prefab, boxPandaRoot, false);
            boxPandaInstance.name = "BoxDog";
            boxPandaInstance.layer = boxPandaRoot.gameObject.layer;
            boxPandaNeedsIntro = true;
            boxPanda = boxPandaInstance.GetComponent<SkeletonAnimation>();
            if (boxPanda == null)
                boxPanda = boxPandaInstance.GetComponentInChildren<SkeletonAnimation>(true);
            if (boxPanda != null)
            {
                boxPanda.PhysicsPositionInheritanceFactor = Vector2.zero;
                boxPanda.PhysicsRotationInheritanceFactor = 0f;
            }
            SyncBoxCoverAgainstDockScale();
        }

        void HideBoxPanda()
        {
            ClearBoxPandaExit();
            boxPandaNeedsIntro = false;
            if (boxPandaInstance != null)
                Destroy(boxPandaInstance);
            boxPandaInstance = null;
            boxPanda = null;
        }

        bool PlayBoxPandaIntro()
        {
            if (!TryGetBoxPandaState(out Spine.AnimationState state, out Spine.SkeletonData data))
            {
                boxPandaNeedsIntro = false;
                return false;
            }

            Spine.Animation outAnim = data.FindAnimation(BoxPandaOutAnimName);
            Spine.Animation idle = data.FindAnimation(BoxPandaIdleAnimName);
            if (outAnim == null)
            {
                boxPandaNeedsIntro = false;
                if (idle != null)
                    PlayBoxPandaForward(state.SetAnimation(0, idle, true));
                return false;
            }

            Spine.TrackEntry entry = state.SetAnimation(0, outAnim, false);
            if (entry == null)
            {
                boxPandaNeedsIntro = false;
                return false;
            }

            entry.MixDuration = 0f;
            entry.MixTime = 0f;
            entry.Delay = 0f;
            entry.Reverse = true;
            entry.TimeScale = 1f;
            entry.TrackTime = 0f;
            if (idle != null)
            {
                Spine.TrackEntry idleEntry = state.AddAnimation(0, idle, true, 0f);
                PlayBoxPandaForward(idleEntry);
                if (idleEntry != null)
                    idleEntry.MixDuration = 0f;
            }

            if (boxPanda != null)
                boxPanda.Update(0f);
            boxPandaNeedsIntro = false;
            return true;
        }

        bool PlayBoxPandaIdle(bool restart)
        {
            if (!TryGetBoxPandaState(out Spine.AnimationState state, out Spine.SkeletonData data))
                return false;
            if (boxPandaNeedsIntro && !restart)
                return PlayBoxPandaIntro();
            if (data.FindAnimation(BoxPandaIdleAnimName) == null)
                return false;
            if (!restart && IsBoxPandaPlaying(state, BoxPandaIdleAnimName))
                return false;
            if (!restart && IsBoxPandaPlaying(state, BoxPandaCollectAnimName))
                return false;
            if (!restart && IsBoxPandaPlaying(state, BoxPandaOutAnimName))
                return false;

            PlayBoxPandaForward(state.SetAnimation(0, BoxPandaIdleAnimName, true));
            return false;
        }

        static void PlayBoxPandaForward(Spine.TrackEntry entry)
        {
            if (entry == null)
                return;

            entry.Reverse = false;
            entry.TimeScale = 1f;
        }

        void PlayBoxPandaExitThenNow(Action onOutStarted, Action onFinished)
        {
            ClearBoxPandaExit();
            boxPandaOutStartedCallback = onOutStarted;
            boxPandaExitCallback = onFinished;
            if (!TryGetBoxPandaState(out Spine.AnimationState state, out Spine.SkeletonData data))
            {
                boxPandaNeedsIntro = false;
                FinishBoxPandaExit(true);
                return;
            }

            boxPandaNeedsIntro = false;

            Spine.Animation outAnim = data.FindAnimation(BoxPandaOutAnimName);
            if (outAnim == null)
            {
                FinishBoxPandaExit(true);
                return;
            }

            Spine.TrackEntry current = state.GetCurrent(0);
            float delayToOut = 0f;
            Spine.TrackEntry outEntry;
            if (current != null
                && current.Animation != null
                && current.Animation.Name == BoxPandaCollectAnimName
                && !current.IsComplete)
            {
                delayToOut = GetTrackRemaining(current);
                state.ClearNext(current);
                outEntry = state.AddAnimation(0, outAnim, false, 0f);
                PlayBoxPandaForward(outEntry);
                if (outEntry != null)
                {
                    outEntry.MixDuration = 0f;
                    outEntry.TrackTime = 0f;
                }
            }
            else if (IsBoxPandaPlaying(state, BoxPandaOutAnimName) && current != null && !current.Reverse)
            {
                outEntry = current;
                PlayBoxPandaForward(outEntry);
                delayToOut = 0f;
            }
            else
            {
                outEntry = state.SetAnimation(0, outAnim, false);
                PlayBoxPandaForward(outEntry);
                if (outEntry != null)
                {
                    outEntry.MixDuration = 0f;
                    outEntry.MixTime = 0f;
                    outEntry.Delay = 0f;
                    outEntry.TrackTime = 0f;
                }
            }

            if (outEntry == null)
            {
                FinishBoxPandaExit(true);
                return;
            }

            float outDuration = GetTrackRemaining(outEntry);
            if (outDuration <= 0.01f)
                outDuration = outAnim.Duration > 0.05f ? outAnim.Duration : 0.4f;

            ScheduleBoxPandaOutStarted(delayToOut);
            ScheduleBoxPandaExitFinished(delayToOut + outDuration);
        }

        static float GetTrackRemaining(Spine.TrackEntry entry)
        {
            if (entry == null || entry.IsComplete)
                return 0f;

            float scale = Mathf.Abs(entry.TimeScale);
            if (scale < 0.01f)
                scale = 1f;
            return Mathf.Max(0f, (entry.AnimationEnd - entry.TrackTime) / scale);
        }

        void ScheduleBoxPandaOutStarted(float delay)
        {
            boxPandaOutStartDelay?.Kill();
            boxPandaOutStartDelay = null;
            if (delay <= 0.01f)
            {
                InvokeBoxPandaOutStarted();
                return;
            }

            boxPandaOutStartDelay = DOVirtual.DelayedCall(delay, InvokeBoxPandaOutStarted)
                .SetUpdate(true)
                .SetTarget(this);
        }

        void ScheduleBoxPandaExitFinished(float delay)
        {
            boxPandaExitDelay?.Kill();
            float wait = Mathf.Max(0.05f, delay);
            boxPandaExitDelay = DOVirtual.DelayedCall(wait, () =>
            {
                boxPandaExitDelay = null;
                if (TryGetBoxPandaState(out Spine.AnimationState _, out _))
                {
                    Spine.TrackEntry current = boxPanda != null ? boxPanda.AnimationState.GetCurrent(0) : null;
                    if (current != null
                        && current.Animation != null
                        && current.Animation.Name == BoxPandaOutAnimName
                        && !current.Reverse)
                    {
                        current.Reverse = false;
                        current.TimeScale = 0f;
                        current.TrackTime = current.AnimationEnd;
                    }
                }

                FinishBoxPandaExit(false);
            }).SetUpdate(true).SetTarget(this);
        }

        void KillBoxPandaExitDelays()
        {
            boxPandaOutStartDelay?.Kill();
            boxPandaOutStartDelay = null;
            boxPandaExitDelay?.Kill();
            boxPandaExitDelay = null;
        }

        void InvokeBoxPandaOutStarted()
        {
            boxPandaOutStartDelay = null;
            Action callback = boxPandaOutStartedCallback;
            boxPandaOutStartedCallback = null;
            callback?.Invoke();
        }

        void ClearBoxPandaExit()
        {
            KillBoxPandaExitDelays();
            boxPandaOutStartedCallback = null;
            boxPandaExitCallback = null;
        }

        void FinishBoxPandaExit(bool invokeStarted)
        {
            KillBoxPandaExitDelays();
            if (invokeStarted)
                InvokeBoxPandaOutStarted();

            Action callback = boxPandaExitCallback;
            boxPandaExitCallback = null;
            callback?.Invoke();
        }

        void PlayBoxPandaCollect()
        {
            if (!TryGetBoxPandaState(out Spine.AnimationState state, out Spine.SkeletonData data))
            {
                boxPandaNeedsIntro = false;
                return;
            }

            boxPandaNeedsIntro = false;

            Spine.Animation collect = data.FindAnimation(BoxPandaCollectAnimName);
            Spine.Animation idle = data.FindAnimation(BoxPandaIdleAnimName);
            if (collect == null)
            {
                PlayBoxPandaIdle(false);
                return;
            }

            if (IsBoxPandaPlaying(state, BoxPandaCollectAnimName))
            {
                EnsureIdleQueuedAfterCollect(state, idle);
                return;
            }

            Spine.TrackEntry collectEntry = state.SetAnimation(0, collect, false);
            PlayBoxPandaForward(collectEntry);
            if (collectEntry != null)
                collectEntry.MixDuration = 0.12f;
            if (idle != null)
                PlayBoxPandaForward(state.AddAnimation(0, idle, true, 0f));
        }

        static void EnsureIdleQueuedAfterCollect(Spine.AnimationState state, Spine.Animation idle)
        {
            if (state == null || idle == null)
                return;

            Spine.TrackEntry current = state.GetCurrent(0);
            if (current == null)
                return;

            Spine.TrackEntry next = current.Next;
            while (next != null)
            {
                if (next.Animation == idle)
                    return;
                next = next.Next;
            }

            PlayBoxPandaForward(state.AddAnimation(0, idle, true, 0f));
        }

        bool TryGetBoxPandaState(out Spine.AnimationState state, out Spine.SkeletonData data)
        {
            state = null;
            data = null;
            EnsureBoxPanda();
            if (boxPanda == null)
                return false;
            if (!boxPanda.valid)
                boxPanda.Initialize(false);
            if (!boxPanda.valid || boxPanda.AnimationState == null)
                return false;

            state = boxPanda.AnimationState;
            data = boxPanda.Skeleton != null ? boxPanda.Skeleton.Data : null;
            return data != null;
        }

        bool IsPlayingReverseIntro()
        {
            if (!TryGetBoxPandaState(out Spine.AnimationState state, out _))
                return false;

            Spine.TrackEntry current = state.GetCurrent(0);
            return current != null
                && !current.IsComplete
                && current.Reverse
                && current.Animation != null
                && current.Animation.Name == BoxPandaOutAnimName;
        }

        static bool IsBoxPandaPlaying(Spine.AnimationState state, string animationName)
        {
            Spine.TrackEntry current = state.GetCurrent(0);
            return current != null
                && current.Animation != null
                && current.Animation.Name == animationName;
        }

        void EnsureTrailPool()
        {
            GameObject prefab = XSortBaseStyleTable.Current?.trail;
            if (prefab == null)
            {
                prefab = trailPrefab;
                XSortBaseThemePack pack = ThemeUiApplier.Get();
                if (pack != null && pack.trail != null)
                {
                    prefab = pack.trail;
                }
            }

            if (prefab == null)
            {
                return;
            }

            if (trailPool != null)
            {
                PoolManager.DestroyPool(trailPool);
                trailPool = null;
            }

            trailPool = new Pool(prefab, $"Trail_{prefab.name}");
        }

        void EnsureDockBgTheme()
        {
            if (backgroundTran == null || dockBgTheme != null)
            {
                return;
            }

            dockBgTheme = backgroundTran.Find("DockBgTheme");
            Transform ice = backgroundTran.Find("themeColumn");
            if (ice == null && dockBgTheme != null)
            {
                ice = dockBgTheme.Find("themeColumn");
            }

            if (dockBgTheme == null && !CanMutateDockThemeHierarchy())
            {
                // Edit-mode prefab instance: themeColumn stays under Background.
                if (ice != null)
                {
                    dockBgTheme = ice;
                    dockBgThemeIce = ice;
                }
                return;
            }

            if (dockBgTheme == null)
            {
                var go = new GameObject("DockBgTheme");
                go.transform.SetParent(backgroundTran, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                dockBgTheme = go.transform;
                if (ice != null)
                {
                    ice.SetParent(dockBgTheme, true);
                }
            }

            dockBgThemeIce = ice != null ? ice : dockBgTheme.Find("themeColumn");
            dockBgTheme.gameObject.SetActive(false);
        }

        bool CanMutateDockThemeHierarchy()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying
                && backgroundTran != null
                && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(backgroundTran))
            {
                return false;
            }
#endif
            return true;
        }

        void OnDisable()
        {
            // The Dock can be re-enabled before Init runs on the next level.
            // Turn off one-shot text effects now so their Play On Awake particles cannot flash.
            HideAnimations();
        }

        public void PlayWarning()
        {
            StopWarning();
            ShowWarningGlow();
            StartDockWarningBreathing();
        }

        /// <summary> Dock 警告呼吸进行时，VisualRoot 相对基准的均匀缩放系数；供飞入贝塞尔内与其它缩放相乘。未呼吸时为 1。 </summary>
        public static float GetWarningBreathingUniformFactor()
        {
            if (instance == null)
                return 1f;
            if (instance.warningBreathingCase == null || !instance.warningBreathingCase.IsActive)
                return 1f;
            if (instance.dockVisualGroup.VisualRoot == null)
                return 1f;
            return instance.dockVisualGroup.GetBreathingUniformFactor();
        }

        /// <summary>
        /// 提交飞入落定后挂到 <see cref="dockVisualRoot"/>（即 <see cref="DockVisualCluster.VisualRoot"/>），随 dock 整体呼吸/位移动画。
        /// 离场时仍由 <see cref="XSortActor.Clear"/> 等挂回对象池。
        /// </summary>
        public static void ParentSubmitPieceToDockVisualRoot(Transform piece)
        {
            if (instance == null || piece == null)
                return;
            Transform root = instance.dockVisualGroup.VisualRoot;
            if (root == null)
                return;
            if (piece.IsChildOf(root))
                return;
            piece.SetParent(root, true);
        }

        /// <summary> 棋子提交飞入落定后在落点播放撞击特效（内存池）。 </summary>
        public static void PlaySubmitImpactAt(Vector3 worldPosition)
        {
            instance?.impackEffectPool?.PlayAt(worldPosition);
        }

        /// <summary> 胜利/失败结算界面弹出时：停止并隐藏 effImpact 及其池实例，并禁止到下一局/复活前再播放。 </summary>
        public static void HideSubmitImpactEffectsForSettlement()
        {
            instance?.impackEffectPool?.HideAllForSettlement();
        }

        /// <summary> 关卡胜利进入结算时：立刻收起 Combo 数字与 Spine。 </summary>
        public static void HideComboEffectForSettlement()
        {
            instance?.HideComboEffect();
        }

        /// <summary>本局连击峰值总和（已结束各段峰值 + 当前未断段）。</summary>
        public static int GetComboPeakSum()
        {
            if (instance == null)
                return 0;

            return instance.m_comboPeakSum + instance.m_comboCnt;
        }

        /// <summary>
        /// 失败结算：把已占槽棋子压回槽位低排序，并收起 Combo / Warning，避免飞入或长按高排序盖住 UI。
        /// 不清理槽位数据，保留给复活。
        /// </summary>
        public static void NormalizeOccupiedXSortSortingForSettlement()
        {
            if (instance == null)
                return;

            instance.StopWarning();
            instance.HideComboEffect();

            if (slots == null)
                return;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || !slot.IsOccupied)
                    continue;

                slot.RestoreOccupiedXSortSorting();
            }
        }

        public static void ResetSubmitImpactPlayback()
        {
            instance?.impackEffectPool?.ResetPlaybackAfterSettlement();
        }

        /// <summary> 棋子飞入暂存区动画剩余约 1/8 时间时调用；shakeRoot 为 dockVisualRoot 父节点，仅做位移动震动以免缩放搅动子层级导致棋子 localScale 显示错误。 </summary>
        public static void PlaySubmitFlightShake()
        {
            if (instance == null || instance.shakeRoot == null)
                return;
            if (instance.warningBreathingCase != null && instance.warningBreathingCase.IsActive)
                return;

            instance.submitFlightDockShakeTween?.Kill();
            instance.shakeRoot.localScale = Vector3.one;
            instance.shakeRoot.localPosition = Vector3.zero;
            instance.DetachBoxPandaFromSubmitShake();
            var tw = instance.shakeRoot.DOShakePosition(0.14f, 0.06f, 12, 90f, false, true);
            tw.SetTarget(instance.shakeRoot);
            tw.OnComplete(() => instance.FinishSubmitFlightShake(tw));
            tw.OnKill(() => instance.FinishSubmitFlightShake(tw));
            instance.submitFlightDockShakeTween = tw;
        }

        void DetachBoxPandaFromSubmitShake()
        {
            if (boxPandaShakeDetached || boxPandaRoot == null || shakeRoot == null)
                return;
            if (!boxPandaRoot.IsChildOf(shakeRoot))
                return;

            boxPandaShakeHomeParent = boxPandaRoot.parent;
            boxPandaShakeHomeSibling = boxPandaRoot.GetSiblingIndex();
            boxPandaShakeHomeLocalPos = boxPandaRoot.localPosition;
            boxPandaShakeHomeLocalRot = boxPandaRoot.localRotation;
            boxPandaShakeHomeLocalScale = boxPandaRoot.localScale;

            Transform stable = shakeRoot.parent != null ? shakeRoot.parent : transform;
            boxPandaRoot.SetParent(stable, true);
            boxPandaShakeDetached = true;
        }

        void FinishSubmitFlightShake(DG.Tweening.Tween tween)
        {
            if (submitFlightDockShakeTween != tween)
                return;

            submitFlightDockShakeTween = null;
            if (shakeRoot != null)
            {
                shakeRoot.localPosition = Vector3.zero;
                shakeRoot.localScale = Vector3.one;
            }

            if (!boxPandaShakeDetached)
                return;

            boxPandaShakeDetached = false;
            if (boxPandaRoot == null || boxPandaShakeHomeParent == null)
                return;

            boxPandaRoot.SetParent(boxPandaShakeHomeParent, false);
            boxPandaRoot.localPosition = boxPandaShakeHomeLocalPos;
            boxPandaRoot.localRotation = boxPandaShakeHomeLocalRot;
            boxPandaRoot.localScale = boxPandaShakeHomeLocalScale;
            int siblingCount = boxPandaShakeHomeParent.childCount;
            boxPandaRoot.SetSiblingIndex(Mathf.Clamp(boxPandaShakeHomeSibling, 0, siblingCount - 1));
        }

        /// <summary> 启动 VisualRoot 呼吸；飞入中的棋子由 <see cref="XSortActor"/> 内乘系数，避免与贝塞尔缩放每帧互抢。 </summary>
        void StartDockWarningBreathing()
        {
            if (dockVisualGroup.VisualRoot == null)
                return;

            EnsureHudAnchor();
            CacheBoxPandaRootAuthoredScale();

            dockVisualGroup.KillWholeTween();
            warningBreathingCase = null;
            dockVisualGroup.VisualRoot.localScale = dockVisualRootScaleBaseline;
            if (holdBoxCoverStillDuringBreathing)
                RestoreHudAnchorPose();
            holdBoxCoverStillDuringBreathing = SpecialPlayDock.HasGoalHud;
            if (holdBoxCoverStillDuringBreathing)
                CacheHudAnchorAuthoredPose();

            warningBreathingCase = dockVisualGroup.TweenBreathingLocalScale(
                warningPulseScaleMultiplier,
                warningPulseHalfDuration,
                Ease.InOutSine);
            SyncBoxCoverAgainstDockScale();
        }

        /// <summary> 只停呼吸并复位 dock 缩放，不关 warning 特效。 </summary>
        void StopDockWarningBreathingOnly()
        {
            dockVisualGroup.KillWholeTween();
            warningBreathingCase = null;
            if (dockVisualGroup.VisualRoot != null)
                dockVisualGroup.VisualRoot.localScale = dockVisualRootScaleBaseline;
            RestoreBoxCoverFromDockBreathing();
        }

        public void StopWarning()
        {
            if (IsGuiding)
            {
                return;
            }

            StopDockWarningBreathingOnly();
            HideWarningGlow();
        }

        public void Unload()
        {
            dockVisualGroup.KillWholeTween();

            if (trailPool != null)
            {
                PoolManager.DestroyPool(trailPool);

                trailPool = null;
            }
        }

        public void PlayAppearAnimation()
        {
            // Init 仅在进游戏时调一次；每关 LoadLevel 只调此处，须解除结算锁否则 effImpact 整关都不会播
            if (impackEffectPool == null)
                impackEffectPool = GetComponentInChildren<ImpackEffectPool>(true);
            impackEffectPool?.ResetPlaybackAfterSettlement();

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].transform.SetLocalScale(1f);
                slots[i].RefreshAdShow();
            }

            canShowMatchClear = false;
            m_isFinalAniming = false;
            m_isClearAniming = false;
            m_lastRestCnt = 0;
            m_comboCnt = 0;
            m_comboPeakSum = 0;
            m_comboEffectSession = 0;
            // 上一关遗留的延迟回调不应影响本关的连击提示。
            m_comboPromptVersion++;
            HideComboEffect();

        }

        public void HideSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].transform.localScale = Vector3.zero;
            }
        }

        public void DisposeQuickly()
        {
            delayTweenCase.KillActive();
            dockVisualGroup.KillWholeTween();
            StopWarning();

            ClearPickHistory();

            // 停止快速结算
            if (m_quickFinishCoroutine != null)
            {
                StopCoroutine(m_quickFinishCoroutine);
                m_quickFinishCoroutine = null;
            }
            m_isQuickFinishing = false;
            m_quickFinishPending = false;
            m_quickFinishDelay?.Kill();
            m_quickFinishDelay = null;
            m_boxCoverVictoryClearing = false;
            StopBoxCoverQuickFinish(false);

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                slot.Clear();
            }

            for (int i = 0; i < addedDepth * 3; i++)
            {
                var slot = slots[slots.Count - 1];
                slots.RemoveAt(slots.Count - 1);

                slot.Clear();
                Destroy(slot.gameObject);
            }

            addedDepth = 0;
            HideComboEffect();
        }

        public void DisableRevert()
        {
            ClearPickHistory();
        }

        private void ClearPickHistory()
        {
            pickedHistory.Clear();
            lastPickedObject = null;
        }

        private void SyncLastPickedFromHistory()
        {
            lastPickedObject = pickedHistory.Count > 0 ? pickedHistory[^1] : null;
        }

        private void RemoveFromPickHistory(IDockable element)
        {
            if (element == null) return;
            pickedHistory.Remove(element);
            SyncLastPickedFromHistory();
        }

        private void RemoveFromPickHistory(IEnumerable<IDockable> elements)
        {
            if (elements == null) return;
            foreach (var element in elements)
            {
                if (element != null)
                    pickedHistory.Remove(element);
            }
            SyncLastPickedFromHistory();
        }

        /// <summary> Dock 中是否仍存在可撤销的最后拾取棋子。 </summary>
        public static bool HasUndoablePickedObject()
        {
            if (instance == null || instance.lastPickedObject == null || slots == null)
                return false;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsOccupied || slot.SlotCase.Behavior != instance.lastPickedObject)
                    continue;
                if (slot.SlotCase.IsMoving || slot.SlotCase.IsBeingRemoved)
                    return false;
                return true;
            }

            return false;
        }

        //判断条件
        //1.全部清除.
        //2.中间消除过的时候,积累了一定数量,再次消除的时候播放.避免上来三消就播clear
        private void PlayClearSpineEffect()
        {
            if (m_isClearAniming)
            {
                return;
            }

            stateClearEff.SetActive(false);
            stateClearEff.SetActive(true);
            m_isClearAniming = true;
            AudioController.PlaySound(AudioController.AudioClips.allClear, 1f);

            Utility.DGDelayedCall(2.0f, () =>
            {
                m_isClearAniming = false;
                stateClearEff.SetActive(false);
            }, this);
        }

        private float m_lastPlayTime = 0f;
        private float m_lastPlayCVTime = 0f;

        private RandomEffectService m_randomFunc = new();
        Spine.AnimationState.TrackEntryDelegate m_stateAnimCompleteHandler;

        public void PlayClearHappyEffect(bool bCombo = false)
        {
            // 检查是否清除完毕（这里需要根据实际逻辑判断）
            bool isAllCleared = LevelDirector.LevelGenService.ActXSorts.Count == 0;

            // Combo 走 PlayComboEffect，此处不处理
            if (bCombo)
                return;

            if (isAllCleared)
            {
                PlayClearSpineEffect();
                return;
            }

            // 使用加权随机，让CVPerfect有30%的概率
            float randomValue = UnityEngine.Random.Range(0f, 1f);
            int randomCase;

            if (randomValue < 0.3f) // 30%概率不播放
            {
                randomCase = 999; //不展示特效
            }
            else
            {
                randomCase = m_randomFunc.Next();
            }

            if (Time.realtimeSinceStartup - m_lastPlayTime < 1) //1s内只能播放一次
            {
                return;
            }

            m_lastPlayTime = Time.realtimeSinceStartup;
            // 根据音效播放对应的stateAnim动画
            PlayCVSound(randomCase);

            switch (randomCase)
            {
                case 0:
                    PlayHappyTextEffect("WellDone");
                    break;
                case 1:
                    PlayHappyTextEffect("Fantastic");
                    break;
                case 2:
                    PlayHappyTextEffect("Wonderful");
                    break;
                case 3:
                    PlayHappyTextEffect("Outstanding");
                    break;
                case 4://unbelievable
                    // AudioController.PlaySound(AudioController.AudioClips.CVPerfect, 1.1f);
                    PlayHappyTextEffect("Incredible");
                    break;
                default:
                    // 静音情况（不播放音效和动画）
                    break;
            }
        }

        //声音间隔处理
        private void PlayCVSound(int idx)
        {
            if (!CvVoiceSettings.IsEnabled)
            {
                return;
            }

            if (Time.realtimeSinceStartup - m_lastPlayCVTime <= 1.0f)
            {
                return;
            }

            m_lastPlayCVTime = Time.realtimeSinceStartup;
            var dic = AudioController.AudioClips;
            AudioClip[] audios = new[] { dic.CVGreate, dic.CVAwesome, dic.CVAmazing, dic.CVExcellent, dic.CVUnbelivable, dic.CVPerfect };
            if (idx < audios.Length)
            {
                AudioController.PlaySound(audios[idx], 1.1f);
            }
        }

        /// <summary>
        /// 播放stateAnim动画
        /// </summary>
        void EnsureTextEffects()
        {
            if (happyTextEffect != null && happyTextRoot == null)
            {
                happyTextRoot = happyTextEffect.transform.Find("EffectText");
                SetDirectChildrenActive(happyTextRoot, false);
                StopParticleSystems(happyTextEffect.transform);
                happyTextEffect.SetActive(false);
            }

            if (comboTextEffect != null && comboNumberAnchor == null)
            {
                comboNumberAnchor = comboTextEffect.transform.Find("ComboSet/ComboNumberAnchor");
                StopParticleSystems(comboTextEffect.transform);
                comboTextEffect.SetActive(false);
                AttachComboTextToEffect();
            }

            if (comboTextEffect != null && comboWordParticle == null)
            {
                // Text01 is the particle system that draws the word "Combo".  The
                // number below it reads this system's curves so both always share
                // the same timing, even when the particle is adjusted in the prefab.
                comboWordParticle = comboTextEffect
                    .GetComponentsInChildren<ParticleSystem>(true)
                    .FirstOrDefault(particle => particle.name == "Text01");
            }

            if (stateAnim != null)
                stateAnim.gameObject.SetActive(false);
        }

        void AttachComboTextToEffect()
        {
            if (comboText == null || comboNumberAnchor == null)
                return;

            var boneFollower = comboText.GetComponent<BoneFollower>();
            if (boneFollower != null)
                boneFollower.enabled = false;

            comboText.transform.SetParent(comboNumberAnchor, false);
            comboText.transform.localPosition = Vector3.zero;
            comboText.transform.localRotation = Quaternion.identity;
            comboText.transform.localScale = Vector3.one;
            comboText.alignment = TextAlignmentOptions.Left;

            if (comboText.transform is RectTransform rectTransform)
            {
                // The anchor marks the left edge of the number, so changing the anchor
                // in the Dock prefab moves the entire number without it crossing "Combo".
                rectTransform.pivot = new Vector2(0f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
            }

            comboText.gameObject.SetActive(false);
        }

        void PlayHappyTextEffect(string nodeName)
        {
            EnsureTextEffects();
            if (happyTextEffect == null || happyTextRoot == null)
                return;

            Transform node = happyTextRoot.Find(nodeName);
            if (node == null)
            {
                Debug.LogWarning($"Dock happy text node not found: {nodeName}");
                return;
            }

            happyTextEffect.SetActive(false);
            SetDirectChildrenActive(happyTextRoot, false);
            node.gameObject.SetActive(true);
            happyTextEffect.SetActive(true);
            StopParticleSystems(happyTextEffect.transform);
            RestartParticleSystems(node);
        }

        float PlayComboParticleEffect()
        {
            EnsureTextEffects();
            if (comboTextEffect == null)
                return 0f;

            comboTextEffect.SetActive(false);
            comboTextEffect.SetActive(true);
            RestartParticleSystems(comboTextEffect.transform);
            return GetParticleEffectDuration(comboTextEffect.transform);
        }

        void HideHappyTextEffect()
        {
            if (happyTextEffect != null)
                happyTextEffect.SetActive(false);
        }

        static void SetDirectChildrenActive(Transform root, bool active)
        {
            if (root == null)
                return;

            for (int i = 0; i < root.childCount; i++)
                root.GetChild(i).gameObject.SetActive(active);
        }

        static void StopParticleSystems(Transform root)
        {
            if (root == null)
                return;

            foreach (ParticleSystem particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Clear(true);
            }
        }

        static void RestartParticleSystems(Transform root)
        {
            if (root == null)
                return;

            foreach (ParticleSystem particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (!particleSystem.gameObject.activeInHierarchy)
                    continue;

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }

        static float GetParticleEffectDuration(Transform root)
        {
            if (root == null)
                return 0f;

            float maxDuration = 0f;
            foreach (ParticleSystem particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particleSystem.main;
                if (main.loop)
                    continue;

                float duration = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
                maxDuration = Mathf.Max(maxDuration, duration);
            }

            return maxDuration;
        }

        private void PlayStateAnim(string animationName)
        {
            if (stateAnim == null)
                return;

            ClearStateAnimCompleteHandler();

            if (!PrepareStateAnimForPlay())
                return;

            stateAnim.AnimationState.ClearTracks();
            stateAnim.Skeleton.SetToSetupPose();
            var entry = stateAnim.AnimationState.SetAnimation(0, animationName, false);
            if (entry == null)
                return;

            entry.TimeScale = 1.0f;
            entry.MixDuration = 0f;
            stateAnim.Update(0f);
            stateAnim.LateUpdate();

            m_stateAnimCompleteHandler = (trackEntry) =>
            {
                if (trackEntry.Animation == null || trackEntry.Animation.Name != animationName)
                    return;

                // lv_6 也可能从随机 happy 通路播到；结束后统一收起，避免停在透明尾帧只剩 TMP
                if (animationName == ComboSpineAnimName)
                {
                    ScheduleHideComboAfterIdle();
                    return;
                }

                HideComboText();
                SetStateAnimVisible(false);
            };
            stateAnim.AnimationState.Complete += m_stateAnimCompleteHandler;
        }

        void ClearStateAnimCompleteHandler()
        {
            if (stateAnim == null || m_stateAnimCompleteHandler == null)
                return;

            if (stateAnim.AnimationState != null)
                stateAnim.AnimationState.Complete -= m_stateAnimCompleteHandler;
            m_stateAnimCompleteHandler = null;
        }

        /// <summary>
        /// taxiaozi 反复 SetActive(false) 后 Mesh 可能失效，表现为 GO 仍激活、comboText 可见但 Spine 永久不画。
        /// 播放前确保组件有效并打开渲染。
        /// </summary>
        bool PrepareStateAnimForPlay()
        {
            if (stateAnim == null)
                return false;

            stateAnim.gameObject.SetActive(true);

            var meshFilter = stateAnim.GetComponent<MeshFilter>();
            bool meshBroken = meshFilter != null && meshFilter.sharedMesh == null;
            if (!stateAnim.valid || meshBroken)
                stateAnim.Initialize(true);

            if (!stateAnim.valid || stateAnim.AnimationState == null || stateAnim.Skeleton == null)
                return false;

            // updateWhenInvisible=Nothing 时，Renderer 被关闭后 Spine 会停在 UpdateMode.Nothing。
            // 必须显式恢复，否则 Update/LateUpdate 都不会推进动画或重建 Mesh，
            // 表现为 brilliant 不显示且 Complete 永远不回调。
            stateAnim.UpdateMode = Spine.Unity.UpdateMode.FullUpdate;

            var meshRenderer = stateAnim.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.enabled = true;

            return true;
        }

        void SetStateAnimVisible(bool visible)
        {
            if (stateAnim == null)
                return;

            if (!visible)
            {
                if (stateAnim.AnimationState != null)
                    stateAnim.AnimationState.ClearTracks();
                if (stateAnim.valid && stateAnim.Skeleton != null)
                    stateAnim.Skeleton.SetToSetupPose();

                // 只关渲染、保持 GO 激活，避免 SkeletonAnimation 反复失活后 Mesh 坏掉且再也画不出来
                var meshRenderer = stateAnim.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    meshRenderer.enabled = false;
                else
                    stateAnim.gameObject.SetActive(false);
                return;
            }

            PrepareStateAnimForPlay();
        }

        float PlayComboSpineAnim(int session)
        {
            if (stateAnim == null)
                return 0f;

            ClearStateAnimCompleteHandler();

            if (!PrepareStateAnimForPlay())
                return 0f;

            stateAnim.AnimationState.ClearTracks();
            stateAnim.Skeleton.SetToSetupPose();
            var entry = stateAnim.AnimationState.SetAnimation(0, ComboSpineAnimName, false);
            if (entry == null)
                return 0f;

            entry.TimeScale = 1f;
            entry.MixDuration = 0f;
            stateAnim.Update(0f);
            stateAnim.LateUpdate();

            m_stateAnimCompleteHandler = (trackEntry) =>
            {
                if (trackEntry.Animation == null || trackEntry.Animation.Name != ComboSpineAnimName)
                    return;
                if (session != m_comboEffectSession)
                    return;

                ScheduleHideComboAfterIdle();
            };
            stateAnim.AnimationState.Complete += m_stateAnimCompleteHandler;

            // Complete 正常触发时会重排为“结束后再等 2 秒”；
            // 此返回值仅作为 Complete 丢失时的最终兜底。
            return entry.Animation.Duration + ComboIdleHideDelay + 0.25f;
        }

        void PlayComboEffect(int comboCount, Vector3 comboAnchorPosition, bool useRightSidePosition)
        {
            m_lastComboUpdateTime = Time.realtimeSinceStartup;
            m_lastPlayTime = m_lastComboUpdateTime;
            ++m_comboEffectSession;

            m_comboTextHideSequence?.Kill();
            m_comboTextHideSequence = null;

            PlayCVSound(5);
            HideHappyTextEffect();
            PositionComboEffect(comboAnchorPosition, useRightSidePosition);
            float effectDuration = PlayComboParticleEffect();
            ShowComboText(comboCount);
            // 不能只依赖 Spine Complete：组件不可见、停用或运行时异常都会让回调丢失，
            // 从而让 comboText 永久残留。兜底时间包含完整动画时长，不会中途截断 lv_6。
            ScheduleHideComboAfterDelay(
                effectDuration > 0f ? effectDuration + ComboIdleHideDelay : ComboIdleHideDelay);
        }

        void PositionComboEffect(Vector3 comboAnchorPosition, bool useRightSidePosition)
        {
            if (comboTextEffect == null)
                return;

            Vector3 desiredPosition = comboAnchorPosition + comboEffectMiddleXSortOffset;
            Camera camera = Camera.main;
            if (camera == null)
            {
                comboTextEffect.transform.position = desiredPosition;
                return;
            }

            if (useRightSidePosition)
            {
                // The last three dock slots use a fixed screen-space placement.
                // Position ComboSet itself so the complete word-and-number effect
                // lands inside the upper-right safe area, independent of XSort motion.
                Transform comboSet = comboNumberAnchor != null ? comboNumberAnchor.parent : null;
                Vector3 targetViewportPosition = camera.WorldToViewportPoint(desiredPosition);
                targetViewportPosition.x = comboEffectRightViewportPosition.x;
                targetViewportPosition.y = comboEffectRightViewportPosition.y;
                Vector3 targetComboSetPosition = camera.ViewportToWorldPoint(targetViewportPosition);
                desiredPosition = comboSet != null
                    ? targetComboSetPosition - (comboSet.position - comboTextEffect.transform.position)
                    : targetComboSetPosition;
            }

            Vector3 viewportPosition = camera.WorldToViewportPoint(desiredPosition);
            if (viewportPosition.z <= 0f)
            {
                comboTextEffect.transform.position = desiredPosition;
                return;
            }

            float minX = Mathf.Min(comboEffectViewportXRange.x, comboEffectViewportXRange.y);
            float maxX = Mathf.Max(comboEffectViewportXRange.x, comboEffectViewportXRange.y);
            viewportPosition.x = Mathf.Clamp(viewportPosition.x, minX, maxX);
            comboTextEffect.transform.position = camera.ViewportToWorldPoint(viewportPosition);
        }

        void HideComboText()
        {
            if (comboText == null)
                return;

            m_comboTextScaleTween?.Kill();
            m_comboTextScaleTween = null;
            comboText.SetActiveEx(false);
            var color = comboText.color;
            color.a = 0f;
            comboText.color = color;
        }

        void HideComboEffect()
        {
            m_comboTextHideSequence?.Kill();
            m_comboTextHideSequence = null;
            if (comboTextEffect != null)
                comboTextEffect.SetActive(false);
            HideComboText();
        }

        void ShowComboText(int comboCount)
        {
            if (comboText == null)
                return;

            // comboText 的 Animator 会在 1 秒内淡出到透明。快速连续连击时，
            // 必须主动关闭再打开并重启 Animator，否则文字虽更新但仍完全透明。
            var comboTextObject = comboText.gameObject;
            m_comboTextScaleTween?.Kill();
            m_comboTextScaleTween = null;
            comboTextObject.SetActive(false);
            comboText.text = Utility.ConvertSpriteAssetNumber(comboCount);
            comboText.transform.localScale = Vector3.zero;
            comboTextObject.SetActive(true);

            var animator = comboText.GetComponent<Animator>();
            if (animator != null)
            {
                // When the Combo particle is available it also controls the number's
                // opacity.  The legacy Animator is only a fallback for old prefabs.
                animator.enabled = comboWordParticle == null;
                if (animator.enabled && comboTextObject.activeInHierarchy)
                {
                    animator.Rebind();
                    animator.Update(0f);
                }
            }

            StartComboNumberScaleAnimation();
        }

        void StartComboNumberScaleAnimation()
        {
            if (comboText == null || !comboText.gameObject.activeInHierarchy)
                return;

            if (comboWordParticle != null)
            {
                var main = comboWordParticle.main;
                var sizeOverLifetime = comboWordParticle.sizeOverLifetime;
                var sizeCurve = sizeOverLifetime.size;

                if (main.startLifetime.mode == ParticleSystemCurveMode.Constant &&
                    sizeOverLifetime.enabled &&
                    sizeCurve.mode == ParticleSystemCurveMode.Curve &&
                    sizeCurve.curve != null &&
                    sizeCurve.curve.length > 0)
                {
                    float duration = main.startLifetime.constant;
                    float curveEndTime = sizeCurve.curve.keys[^1].time;
                    float finalSize = sizeCurve.curve.Evaluate(curveEndTime) * sizeCurve.curveMultiplier;

                    if (duration > 0f && finalSize > Mathf.Epsilon)
                    {
                        var colorOverLifetime = comboWordParticle.colorOverLifetime;
                        Gradient colorCurve = null;
                        if (colorOverLifetime.enabled &&
                            colorOverLifetime.color.mode == ParticleSystemGradientMode.Gradient)
                        {
                            colorCurve = colorOverLifetime.color.gradient;
                        }

                        // Use the Combo particle's real size/alpha curves instead of
                        // a fixed tween. Normalize its settled size to 1 so the number
                        // keeps its designed layout while matching the pop and rebound.
                        m_comboTextScaleTween = DG.Tweening.DOVirtual.Float(0f, duration, duration, elapsed =>
                        {
                            float normalizedTime = Mathf.Clamp01(elapsed / duration);
                            float relativeScale = sizeCurve.curve.Evaluate(normalizedTime) * sizeCurve.curveMultiplier / finalSize;
                            comboText.transform.localScale = Vector3.one * Mathf.Max(0f, relativeScale);

                            var color = comboText.color;
                            color.a = colorCurve != null ? colorCurve.Evaluate(normalizedTime).a : 1f;
                            comboText.color = color;
                        }).SetEase(Ease.Linear);
                        return;
                    }
                }
            }

            // Compatibility fallback for a Combo prefab without the Text01 particle.
            var fallbackColor = comboText.color;
            fallbackColor.a = 1f;
            comboText.color = fallbackColor;
            m_comboTextScaleTween = comboText.transform
                .DOScale(Vector3.one, ComboNumberFallbackAppearDuration)
                .SetEase(Ease.OutCubic);
        }

        void ScheduleHideComboAfterIdle()
        {
            ScheduleHideComboAfterDelay(ComboIdleHideDelay);
        }

        void ScheduleHideComboAfterDelay(float delay)
        {
            int session = m_comboEffectSession;
            m_comboTextHideSequence?.Kill();
            m_comboTextHideSequence = DOTween.Sequence();
            m_comboTextHideSequence.AppendInterval(delay);
            m_comboTextHideSequence.AppendCallback(() =>
            {
                if (session != m_comboEffectSession)
                    return;
                if (Time.realtimeSinceStartup - m_lastComboUpdateTime >= ComboIdleHideDelay - 0.05f)
                    HideComboEffect();
            });
        }

        /// <summary>
        /// 隐藏所有动画
        /// </summary>
        private void HideAnimations()
        {
            HideComboEffect();
            HideHappyTextEffect();

            if (stateClearEff != null)
            {
                stateClearEff.SetActive(false);
            }
        }


        /// <summary>
        /// 检查是否清除完毕（关卡完成条件）
        /// </summary>
        private bool CheckIfClearCompleted()
        {
            // 检查是否所有棋子都被消除且码头为空
            // 这是关卡完成的条件，与LevelController.OnMatchCompleted()中的逻辑一致
            return LevelDirector.LevelGenService.ActXSorts.Count == 0;// && IsEmpty
        }

        //最后移动一下.
        private void RunFinalMatchAnimation(float addTime, List<SlotCase> slotsToRemove)
        {
            Vector3 dstPos = Vector3.zero;
            for (int i = 0; i < slotsToRemove.Count; i++)
            {
                var slotCase = slotsToRemove[i];
                if (i == 0)
                {
                    m_isFinalAniming = true;
                    slotCase.Behavior.FinalMatchAnimation(addTime, () =>
                    {
                        m_isFinalAniming = false;
                        slotCase.Behavior.MatchAnimation(i * 0.1f);
                        PlayMergeSound(slotCase.Behavior);
                    });
                    dstPos = slotCase.Behavior.Transform.position;
                }
                else
                {
                    slotCase.Behavior.MatchAnimation(addTime + i * 0.05f);
                    slotCase.Behavior.FinalMatchMoveAnimation(dstPos);
                }
            }
        }

        private bool CheckShowComboAnim(int restCnt)
        {
            int deltaCnt = m_lastRestCnt - restCnt;
            return deltaCnt <= 3;
        }

        void PlayMergeSound(IDockable sample = null)
        {
            float pitch = 0.9f + (m_mergeSoundPitchStep++ % 4) * 0.1f;
            AudioClips audioClips = AudioController.AudioClips;
            AudioClip fallback = audioClips != null && audioClips.mergeSound != null
                ? audioClips.mergeSound
                : null;

            bool isCoin = sample is XSortActor XSort && XSort.IsCoinReward;
            AudioClip clip;
            if (isCoin)
            {
                clip = XSortSkinCatalog.LoadXSortBaseSound(
                    XSortSkinCatalog.CoinRewardBrokenSoundName,
                    fallback);
            }
            else
            {
                AudioClip styleBroken = XSortBaseStyleTable.Current?.brokenSound;
                AudioClip themeBroken = ThemeUiApplier.Get()?.brokenSound;
                clip = styleBroken != null
                    ? styleBroken
                    : themeBroken != null
                    ? themeBroken
                    : XSortSkinCatalog.LoadXSortBaseSound(
                        XSortSkinCatalog.GetBrokenSoundName(LevelDirector.LoadedLevelTheme),
                        fallback);
            }

            if (clip != null)
                AudioController.PlaySound(clip, 0.9f, pitch);
        }
        private void ClearMatchedGroup(List<IDockable> charactersToRemove)
        {
            if (LevelDifficultySim.IsRunning)
            {
                InstantClearMatchedGroup(charactersToRemove);
                return;
            }

            NotifyIfFissionMatch(charactersToRemove);
            RemoveFromPickHistory(charactersToRemove);

            var slotsToRemove = new List<SlotCase>();
            SlotActor comboAnchorSlot = null;
            int lastRemovedSlotIndex = -1;
            for (int i = 0; i < slots.Count; i++)
            {
                var slotCase = slots[i].SlotCase;

                if (slotCase != null && charactersToRemove.Contains(slotCase.Behavior))
                {
                    slotsToRemove.Add(slotCase);
                    lastRemovedSlotIndex = i;
                    if (slotsToRemove.Count == 2)
                        comboAnchorSlot = slots[i];
                    slotCase.IsBeingRemoved = true;
                }
            }

            float addTime = 0f;
            int restCnt = LevelDirector.LevelGenService.GetRestXSortCount();
            bool bShowComboAnim = false;

            // 待消立刻扩临时格（不回收待消槽，避免棋子短暂游离导致 BA temp+XSort_left 非 3 倍数）
            EnsureTempSlotsWhenNoEmpty();

            if (m_lastRestCnt != 0)
            {
                Debug.Log("棋子差距数量=== " + (m_lastRestCnt - restCnt));
                if (!m_isFinalAniming)//最后一下不显示
                {
                    bShowComboAnim = CheckShowComboAnim(restCnt);
                }
            }

            // 连击次数必须在消除发生时立即更新，不能依赖 0.2 秒后的提示回调。
            // 否则快速连续消除时，下一次会拿到尚未初始化的 0，误走普通提示分支。
            if (bShowComboAnim)
            {
                m_comboCnt++;
            }
            else
            {
                if (m_comboCnt > 0)
                {
                    m_comboPeakSum += m_comboCnt;
                }

                m_comboCnt = 0;
            }

            m_lastRestCnt = restCnt;
            int comboPromptVersion = ++m_comboPromptVersion;
            int comboCountForThisMatch = m_comboCnt;
            Vector3 comboAnchorPosition = comboAnchorSlot != null
                ? comboAnchorSlot.SlotAnchorWorldPosition
                : transform.position;
            // A match occupying the last three dock slots uses the dedicated
            // upper-right Combo position so the complete effect remains visible.
            bool useRightSideComboPosition = slotsToRemove.Count >= 3 &&
                lastRemovedSlotIndex == slots.Count - 1;
            int occupiedAfterMatch = GetOccupiedSlotsCount() - slotsToRemove.Count;
            bool fissionMerge = IsIqFissionMerge(slotsToRemove);
            if (fissionMerge)
            {
                addTime = 0.12f;
                PlayMergeSound(slotsToRemove[1].Behavior);
                IQChallengeVisual.PlayFissionMerge(
                    slotsToRemove[0].Behavior as XSortActor,
                    slotsToRemove[1].Behavior as XSortActor,
                    slotsToRemove[2].Behavior as XSortActor,
                    IQChallengeManager.OnFissionMergeShattered,
                    IQChallengeManager.OnFissionMergePresentationFinished);
            }
            else if (restCnt == 0 && occupiedAfterMatch <= 0 && !m_isFinalAniming)//如果余下棋子没有了需要做特殊动画
            {
                addTime = 0.45f;
                RunFinalMatchAnimation(addTime, slotsToRemove);
            }
            else
            {
                PlayMergeSound(slotsToRemove.Count > 0 ? slotsToRemove[0].Behavior : null);
                for (int i = 0; i < slotsToRemove.Count; i++)
                {
                    var slotCase = slotsToRemove[i];
                    slotCase.Behavior.MatchAnimation(i * 0.1f);
                }
            }


            Utility.DGDelayedCall(0.2f + addTime, () =>
            {
                // 多次消除可能发生在延迟期间。旧回调不能关闭或重置较新消除的倍率提示。
                if (comboPromptVersion != m_comboPromptVersion)
                {
                    return;
                }

                if (bShowComboAnim)
                {
                    PlayComboEffect(comboCountForThisMatch, comboAnchorPosition, useRightSideComboPosition);
                }
                else
                {
                    HideComboEffect();
                    PlayClearHappyEffect();
                }
            }, this);

            Debug.Log("剩余棋子数 " + restCnt);
            int matchClearGen = m_matchClearGeneration;
            Utility.DGDelayedCall(0.4f + addTime, () =>
            {
                if (matchClearGen != m_matchClearGeneration || !GameDirector.IsGameActive)
                    return;

                for (int i = 0; i < slotsToRemove.Count; i++)
                {
                    var slotCase = slotsToRemove[i];
                    var element = slotCase.Behavior;
                    if (element == null)
                        continue;

                    slotCase.IsBeingRemoved = false;

                    element.Clear();

                    for (int j = 0; j < slots.Count; j++)
                    {
                        var slot = slots[j];

                        if (slot.SlotCase == slotCase)
                        {
                            slot.RemoveSlot();
                            break;
                        }
                    }
                }

                ShiftAllLeft();

                if (addedDepth > 0 && slots.Count >= 3)
                {
                    bool tempsEmpty = true;
                    for (int i = 0; i < 3; i++)
                    {
                        if (slots[slots.Count - 1 - i].IsOccupied)
                        {
                            tempsEmpty = false;
                            break;
                        }
                    }

                    if (tempsEmpty)
                    {
                        addedDepth--;
                        for (int i = 0; i < 3; i++)
                        {
                            var tempSlot = slots[^1];
                            slots.RemoveAt(slots.Count - 1);

                            tempSlot.Clear();
                            Destroy(tempSlot.gameObject);
                        }
                    }
                }

                SnapSubmittedXSortsToSlotAnchorsIfIdle();

                levelController.OnMatchCompleted();
                CheckShowClearEffect();

                // 延迟检查快速结算条件（等待所有动画和状态更新完成）
                // 确保棋子移除、ShiftAllLeft、UpdateClickableStates 都已完成
                // 在一次消除后检查快速结算条件
                Utility.DGDelayedCall(0.6f + addTime, () =>
                {
                    Debug.Log($"[QuickFinish] 一次消除后检查快速结算条件，场上剩余棋子数: {LevelDirector.LevelGenService.ActXSorts.Count}");
                    TryCheckQuickFinishLevel();
                }, this);
            }, this);
        }

        private void CheckShowClearEffect()
        {

            foreach (var item in slots)
            {
                if (item.IsOccupied)
                {
                    canShowMatchClear = true;
                    break;
                }
            }

            if (IsEmpty)
            {
                if (!(LevelDirector.LevelGenService.ActXSorts.Count == 0) && canShowMatchClear)
                {
                    PlayClearSpineEffect();
                    canShowMatchClear = false;
                }
            }
        }

        static bool IsIqFissionMerge(List<SlotCase> slotsToRemove)
        {
            if (!IQChallengeManager.HasActiveLevel || slotsToRemove == null || slotsToRemove.Count != 3)
                return false;

            for (int i = 0; i < slotsToRemove.Count; i++)
            {
                XSortActor XSort = slotsToRemove[i].Behavior as XSortActor;
                if (XSort == null || !XSort.IsFission)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 场上已经没有棋子，暂存区也组不出三消时，脑花进度到不了第 4 格。
        /// 这时改走脑花自己的结算，避免停在空棋盘。
        /// </summary>
        void TrySettleIqIfBoardStuck()
        {
            if (!IQChallengeManager.HasActiveLevel
                || IQChallengeManager.IsVictorySequenceStarted
                || IQChallengeManager.IsReadyToWin)
                return;

            LevelGenService gen = LevelDirector.LevelGenService;
            if (gen == null || gen.ActXSorts == null || gen.ActXSorts.Count > 0)
                return;

            if (HasActiveSlotMotion() || HasBeingRemovedXSorts() || IsClearingAnimation())
                return;

            if (!IsEmpty && CheckMatch(false))
                return;

            IQChallengeManager.SettleIfBoardExhausted();
        }

        void InstantClearMatchedGroup(List<IDockable> charactersToRemove)
        {
            NotifyIfFissionMatch(charactersToRemove);
            if (charactersToRemove != null && charactersToRemove.Count == 3)
            {
                bool fission = IQChallengeManager.HasActiveLevel;
                for (int i = 0; fission && i < charactersToRemove.Count; i++)
                {
                    XSortActor XSort = charactersToRemove[i] as XSortActor;
                    fission = XSort != null && XSort.IsFission;
                }

                if (fission)
                {
                    IQChallengeManager.OnFissionMergeShattered();
                    IQChallengeManager.OnFissionMergePresentationFinished();
                }
            }
            RemoveFromPickHistory(charactersToRemove);
            m_matchClearGeneration++;

            var slotsToRemove = new List<SlotCase>();
            for (int i = 0; i < slots.Count; i++)
            {
                var slotCase = slots[i].SlotCase;
                if (slotCase != null && charactersToRemove.Contains(slotCase.Behavior))
                    slotsToRemove.Add(slotCase);
            }

            for (int i = 0; i < slotsToRemove.Count; i++)
            {
                var slotCase = slotsToRemove[i];
                slotCase.IsBeingRemoved = false;
                slotCase.IsMoving = false;
                slotCase.Behavior?.Clear();

                for (int j = 0; j < slots.Count; j++)
                {
                    if (slots[j].SlotCase == slotCase)
                    {
                        slots[j].RemoveSlot();
                        break;
                    }
                }
            }

            ShiftAllLeft();

            if (addedDepth > 0 && slots.Count >= 3)
            {
                bool tempsEmpty = true;
                for (int i = 0; i < 3; i++)
                {
                    if (slots[slots.Count - 1 - i].IsOccupied)
                    {
                        tempsEmpty = false;
                        break;
                    }
                }

                if (tempsEmpty)
                {
                    addedDepth--;
                    for (int i = 0; i < 3; i++)
                    {
                        var tempSlot = slots[^1];
                        slots.RemoveAt(slots.Count - 1);
                        tempSlot.Clear();
                        Destroy(tempSlot.gameObject);
                    }
                }
            }

            levelController.OnMatchCompleted();
        }

        private bool CheckMatch(bool remove = true)
        {
            if (IsEmpty) return false;

            return CheckDockMatch(remove);
        }

        public static List<IDockable> GetHintSlots()
        {
            SlotActor[] elementsArray = slots.FindAll(x => x.IsOccupied).GroupBy(x => x.SlotCase.Behavior.UniqueElementID).OrderByDescending(g => g.Count()).SelectMany(g => g).ToArray();

            if (!elementsArray.IsNullOrEmpty())
            {
                List<IDockable> tempSlotElements = new List<IDockable>();
                tempSlotElements.Add(elementsArray[0].SlotCase.Behavior);

                for (int i = 1; i < elementsArray.Length; i++)
                {
                    if (elementsArray[i].SlotCase.Behavior.IsSameType(elementsArray[0].SlotCase.Behavior))
                    {
                        tempSlotElements.Add(elementsArray[i].SlotCase.Behavior);
                    }
                }

                return tempSlotElements;
            }

            return null;
        }

        static void NotifyIfFissionMatch(List<IDockable> charactersToRemove)
        {
            if (!IQChallengeManager.HasActiveLevel || charactersToRemove == null || charactersToRemove.Count < 3)
                return;

            Vector3 origin = Vector3.zero;
            bool hasOrigin = false;
            for (int i = 0; i < 3; i++)
            {
                XSortActor XSort = charactersToRemove[i] as XSortActor;
                if (XSort == null || !XSort.IsFission)
                    return;
                if (!hasOrigin)
                {
                    origin = XSort.transform.position;
                    hasOrigin = true;
                }
            }

            if (charactersToRemove.Count > 1)
            {
                XSortActor mid = charactersToRemove[1] as XSortActor;
                if (mid != null)
                    origin = mid.transform.position;
            }

            IQChallengeManager.OnFissionMatched(origin);
        }

        private bool CheckDockMatch(bool remove = true)
        {
            int counter = 1;
            var comparableRefference = slots[0].SlotCase.Behavior;
            var list = new List<IDockable> { slots[0].SlotCase.Behavior };

            for (int i = 1; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (!slot.IsOccupied) return false;

                var slotCase = slot.SlotCase;
                var element = slotCase.Behavior;

                if (counter == 0)
                {
                    comparableRefference = element;
                }

                if (element.IsSameType(comparableRefference) && !slotCase.IsBeingRemoved && (!slot.SlotCase.IsMoving || !remove))
                {
                    counter++;
                    list.Add(element);

                    if (counter == 3)
                    {
                        if (remove)
                        {
                            ClearMatchedGroup(list);

                            MatchCombined?.Invoke(list);

                            // BoxCover 快速结算/清盘会瞬时挪棋，BA 守恒校验会误报「出现异常=-=」。
                            if (GameData.BFromStartScene
                                && !m_isBoxCoverQuickFinishing
                                && !m_boxCoverVictoryClearing)
                            {
                                GameBAData.ExcuteStageEvent(1);
                            }
                        }

                        return true;
                    }
                }
                else if (!slotCase.IsBeingRemoved)
                {
                    counter = 1;
                    comparableRefference = element;
                    list = new List<IDockable> { element };
                }
                else
                {
                    counter = 0;
                    list = new List<IDockable>();
                }
            }

            return false;
        }

        private int CalculateIndexSlots(IDockable XSortBehavior)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsOccupied) return i;

                if (XSortBehavior.IsSameType(slot.SlotCase.Behavior))
                {
                    for (int j = i + 1; j < slots.Count; j++)
                    {
                        var nextSlot = slots[j];

                        if (!nextSlot.IsOccupied) return j;

                        if (!slot.IsOccupied || !XSortBehavior.IsSameType(nextSlot.SlotCase.Behavior))
                        {
                            return j;
                        }
                    }
                }
            }

            return -1;
        }

        public static int GetSlotsAvailable()
        {
            int counter = 0;
            //更改
            for (int i = LevelDirector.DockAb.GetMaxSlotCnt() - 1; i >= 0; i--)
            {
                if (!slots[i].IsOccupied) counter++;
            }

            return counter;
        }

        bool HasEmptySlot()
        {
            if (slots == null)
                return false;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsOccupied)
                    return true;
            }
            return false;
        }

        public void SetOverlayPosition()
        {
            transform.position = defaultContainerPosition.SetZ(1.0f);
        }

        public void SetDefaultPosition()
        {
            transform.position = defaultContainerPosition;
        }

        public void RestoreAuthoredPosition()
        {
            CaptureAuthoredPositionIfNeeded();
            defaultContainerPosition = authoredContainerPosition;
            transform.position = authoredContainerPosition;
        }

        public void ApplyWorldYOffset(float deltaY)
        {
            Vector3 position = transform.position;
            position.y += deltaY;
            transform.position = position;
            defaultContainerPosition = position;
        }

        void CaptureAuthoredPositionIfNeeded()
        {
            if (hasAuthoredContainerPosition)
                return;

            authoredContainerPosition = transform.position;
            hasAuthoredContainerPosition = true;
        }

        public bool SubmitToSlot(IDockable element, bool instant)
        {
            // 无空槽且可三消/待消时先扩临时格；待消棋子仍占槽计入 BA，不 RemoveSlot 回收
            if (!HasEmptySlot())
                TryAddTempSlotsIfMatchPending();

            int index = CalculateIndexSlots(element);

            if (index < 0 || index >= slots.Count || WouldInsertEjectLast(index))
            {
                if (GameDirector.IsGameActive)
                    levelController.OnSlotsFilled();
                return false;
            }

            SlotCase slotCase = new SlotCase(element);
            if (trailPool != null && !LevelDifficultySim.IsRunning)
            {
                GameObject trailGo = trailPool.GetPooledObject();
                if (trailGo != null)
                    slotCase.AddTrail(trailGo);
            }

            slotCase.IsMoving = true;
            slotCase.MoveType = DockMoveType.Submit;

            if (slots[index].IsOccupied)
            {
                Insert(slotCase, index, instant);
            }
            else
            {
                slots[index].Assign(slotCase, instant);
            }

            lastPickedObject = element;
            pickedHistory.Add(element);

            ElementAdded?.Invoke(element);

            if (CheckMatch(false))
            {
                if (addedDepth < MaxTempDepth)
                    ExpandTempSlotsLayer();
            }
            else if (HasBeingRemovedXSorts() && !HasEmptySlot())
            {
                if (addedDepth < MaxTempDepth)
                    ExpandTempSlotsLayer();
            }
            else if (GetEffectiveOccupiedSlotsCount() >= LevelDirector.DockAb.GetMaxSlotCnt()
                && GameDirector.IsGameActive)
            {
                levelController.OnSlotsFilled();
            }

            return true;
        }

        const int MaxTempDepth = 4;

        void TryAddTempSlotsIfMatchPending()
        {
            if (addedDepth >= MaxTempDepth || HasEmptySlot())
                return;
            if (!CheckMatch(false) && !HasBeingRemovedXSorts())
                return;

            ExpandTempSlotsLayer();
        }

        void EnsureTempSlotsWhenNoEmpty()
        {
            while (!HasEmptySlot() && addedDepth < MaxTempDepth)
                ExpandTempSlotsLayer();
        }

        void ExpandTempSlotsLayer()
        {
            addedDepth++;
            for (int i = 0; i < 3; i++)
                slots.Add(SlotActor.GetTempSlot(slots[^1], slots[^2]));
        }

        bool WouldInsertEjectLast(int index)
        {
            if (index < 0 || index >= slots.Count)
                return true;
            if (!slots[index].IsOccupied)
                return false;
            return slots[^1].IsOccupied;
        }

        public static List<IDockable> RemoveObjects(int count)
        {
            List<IDockable> removedXSorts = new List<IDockable>();

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                SlotActor slot = slots[i];
                if (slot.IsOccupied)
                {
                    IDockable XSortBehavior = slot.SlotCase.Behavior;

                    // 避免在提交动画或移除动画过程中的撤销引发竞态
                    if (slot.SlotCase.IsMoving || slot.SlotCase.IsBeingRemoved)
                    {
                        continue;
                    }

                    slot.SlotCase.Clear(false);
                    slot.RemoveSlot();

                    instance.RemoveFromPickHistory(XSortBehavior);

                    removedXSorts.Add(XSortBehavior);

                    if (removedXSorts.Count >= count)
                        break;
                }
            }

            instance.ShiftAllLeft();

            return removedXSorts;
        }

        /// <summary>
        /// 按用户收集顺序从后往前移除棋子（撤销道具用），而非 Dock 最右侧槽位。
        /// </summary>
        public static List<IDockable> RemoveLastPickedObjects(int count)
        {
            List<IDockable> removedXSorts = new List<IDockable>();
            if (instance == null || slots == null || count <= 0)
                return removedXSorts;

            while (removedXSorts.Count < count && instance.pickedHistory.Count > 0)
            {
                IDockable objToReturn = instance.pickedHistory[^1];
                SlotActor targetSlot = null;

                for (int i = 0; i < slots.Count; i++)
                {
                    SlotActor slot = slots[i];
                    if (!slot.IsOccupied || slot.SlotCase.Behavior != objToReturn)
                        continue;

                    // 动画中不可撤，保留历史并停止（与 HasActiveSlotMotion 配合）
                    if (slot.SlotCase.IsMoving || slot.SlotCase.IsBeingRemoved)
                    {
                        instance.SyncLastPickedFromHistory();
                        if (removedXSorts.Count > 0)
                            instance.ShiftAllLeft();
                        return removedXSorts;
                    }

                    targetSlot = slot;
                    break;
                }

                // 先确认可移除再弹出历史；陈旧项丢弃后继续更早的拾取
                instance.pickedHistory.RemoveAt(instance.pickedHistory.Count - 1);
                if (targetSlot == null)
                    continue;

                targetSlot.SlotCase.Clear(false);
                targetSlot.RemoveSlot();
                removedXSorts.Add(objToReturn);
            }

            instance.SyncLastPickedFromHistory();

            if (removedXSorts.Count > 0)
                instance.ShiftAllLeft();

            return removedXSorts;
        }

        public bool HasActiveSlotMotion()
        {
            if (slots == null || slots.Count == 0)
                return false;

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (!s.IsOccupied) continue;
                if (s.SlotCase.IsMoving || s.SlotCase.IsBeingRemoved)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否正在进行消除动画
        /// </summary>
        public bool IsClearingAnimation()
        {
            return m_isFinalAniming || m_isClearAniming;
        }

        /// <summary>
        /// 终止所有动画，包括协程、DOTween动画和延迟调用
        /// </summary>
        public void StopAllAnimations()
        {
            // 停止所有协程
            dockVisualGroup.KillWholeTween();
            warningBreathingCase = null;
            if (dockVisualGroup.VisualRoot != null)
            {
                dockVisualGroup.VisualRoot.DOKill();
                dockVisualGroup.VisualRoot.localScale = dockVisualRootScaleBaseline;
            }

            RestoreBoxCoverFromDockBreathing();

            if (m_quickFinishCoroutine != null)
            {
                StopCoroutine(m_quickFinishCoroutine);
                m_quickFinishCoroutine = null;
            }

            // 终止 delayTweenCase
            delayTweenCase.KillActive();

            // 终止所有 SlotCase 的动画
            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot.IsOccupied && slot.SlotCase != null)
                    {
                        slot.SlotCase.Clear(false);
                    }
                }
            }

            // 终止所有与此对象相关的 DOTween 动画（包括 DGDelayedCall）
            DOTween.Kill(this);

            // 终止 transform 上的所有 DOTween 动画
            transform.DOKill();

            submitFlightDockShakeTween?.Kill();
            submitFlightDockShakeTween = null;
            if (shakeRoot != null)
            {
                shakeRoot.DOKill();
                shakeRoot.localScale = Vector3.one;
                shakeRoot.localPosition = Vector3.zero;
            }

            // 警告呼吸已在上方 KillWholeTween 并复位 VisualRoot

            // 重置状态标志
            m_isFinalAniming = false;
            m_isClearAniming = false;
            m_isQuickFinishing = false;
            m_quickFinishPending = false;
            m_quickFinishDelay = null;
            m_matchClearGeneration++;
            HideComboEffect();
        }

        /// <summary>
        /// 暂停所有动画，包括协程、DOTween动画和延迟调用
        /// </summary>
        public void PauseAllAnimations()
        {
            // 暂停所有协程（通过暂停 MonoBehaviour）
            enabled = false;

            // 暂停 delayTweenCase
            if (delayTweenCase != null && delayTweenCase.IsActive)
            {
                delayTweenCase.Pause();
            }

            // 暂停所有 SlotCase 的动画
            if (slots != null)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot.IsOccupied && slot.SlotCase != null)
                    {
                        // SlotCase 内部使用 TweenCaseCollection，需要通过 Behavior 来暂停
                        var behavior = slot.SlotCase.Behavior;
                        if (behavior != null && behavior.Transform != null)
                        {
                            behavior.Transform.DOPause();
                        }
                    }
                }
            }

            // 暂停所有与此对象相关的 DOTween 动画（包括 DGDelayedCall）
            DOTween.Pause(this);

            // 暂停 transform 上的所有 DOTween 动画
            transform.DOPause();

            dockVisualGroup.PauseWholeTween();
        }

        public static IDockable RemoveLastPicked()
        {
            List<IDockable> removed = RemoveLastPickedObjects(1);
            return removed.Count > 0 ? removed[0] : null;
        }

        public void Insert(SlotCase slotCase, int index, bool instant = false)
        {
            var freeCase = slotCase;

            for (int i = index; i < slots.Count; i++)
            {
                var slot = slots[i];

                var caseToShift = slot.RemoveSlot();

                if (freeCase != null)
                {
                    if (freeCase.IsMoving && freeCase.MoveType == DockMoveType.Submit)
                    {
                        slot.Assign(freeCase, instant);
                    }
                    else
                    {
                        slot.AssingFast(freeCase);
                    }
                }

                freeCase = caseToShift;
            }

            if (freeCase != null)
                RestoreEjectedSlotCase(freeCase);
        }

        void RestoreEjectedSlotCase(SlotCase ejected)
        {
            var XSort = ejected.Behavior as XSortActor;
            ejected.Clear(false);
            if (XSort == null)
                return;

            RemoveFromPickHistory(XSort);
            LevelDirector.RevertElement(XSort);
        }
        bool HasAnySlotMotion()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsOccupied || slot.SlotCase == null) continue;
                if (slot.SlotCase.IsMoving || slot.SlotCase.IsBeingRemoved)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// <see cref="ShiftAllLeft"/> 等布局调整后：若无飞入/移位/移除中的棋子，把已提交棋子世界坐标对齐槽位锚点（修正与 dock 呼吸等叠加的偏差）。
        /// </summary>
        void SnapSubmittedXSortsToSlotAnchorsIfIdle()
        {
            if (HasAnySlotMotion())
                return;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || !slot.IsOccupied || slot.SlotCase == null) continue;
                if (slot.SlotCase.IsMoving || slot.SlotCase.IsBeingRemoved) continue;
                var tr = slot.SlotCase.Behavior?.Transform;
                if (tr == null) continue;
                // 刷新世界坐标的同时，刷新缩放，避免之前飞行动画中断后残留错误 scale
                tr.position = slot.SlotAnchorWorldPosition;
                tr.localScale = Vector3.one;

                // 若棋子有 XSortActor 组件，则根据当前槽位最终尺寸刷新其内部 visuals 缩放
                if (slot.SlotCase.Behavior is XSortActor XSort)
                {
                    // 这里的目标是“落定后的视觉尺寸”，与提交动画的终点保持一致
                    var smallerSize = slot.Scale.x > slot.Scale.y
                        ? slot.Scale.y
                        : slot.Scale.x;
                    var visualsTransform = XSort.VisualsTransform;
                    if (visualsTransform != null)
                    {
                        visualsTransform.localScale = Vector3.one * smallerSize;
                    }
                }
            }
        }

        public void ShiftAllLeft()
        {
            var lastIndex = -1;

            for (int i = 0; i < slots.Count - 1; i++)
            {
                var recepient = slots[i];

                if (recepient.IsOccupied) continue;

                bool found = false;
                for (int j = i + 1; j < slots.Count; j++)
                {
                    var donor = slots[j];

                    if (!donor.IsOccupied) continue;

                    var slotCase = donor.RemoveSlot();
                    if (slotCase.IsMoving && slotCase.MoveType == DockMoveType.Submit)
                    {
                        recepient.Assign(slotCase);
                    }
                    else
                    {
                        recepient.AssingFast(slotCase);
                    }

                    found = true;

                    break;
                }

                lastIndex = i;
                if (!found) break;
            }

            if (lastIndex == -1) return;

            for (int i = lastIndex; i < slots.Count; i++)
            {
                var slot = slots[i];
                slot.RestoreColor(Color.white);
            }
        }

        public void LateUpdate()
        {
            if (slots == null || slots.Count == 0)
                return;

            if (GameDirector.IsGameActive && Time.frameCount % 15 == 0 && !IsEmpty)
            {
                if (!HasActiveSlotMotion()
                    && !CheckMatch()
                    && GetEffectiveOccupiedSlotsCount() >= LevelDirector.DockAb.GetMaxSlotCnt())
                {
                    levelController.OnSlotsFilled();
                }
            }

            if (!LevelDifficultySim.SkipPresentation && !HasActiveSlotMotion())
                CheckWarningState();

            if (SpecialPlayDock.UsesNoLevelIdPlate
                && warningBreathingCase != null
                && warningBreathingCase.IsActive)
            {
                SyncBoxCoverAgainstDockScale();
            }
        }

        private void CheckWarningState()
        {
            // int occupiedSlotsCount = GetOccupiedSlotsCount();
            // if (occupiedSlotsCount >= 6)

            int restCnt = LevelDirector.DockAb.GetRestEmptyCnt();
            if (restCnt <= 1)
            {
                ShowWarningGlow();

                if (warningBreathingCase == null || !warningBreathingCase.IsActive)
                    StartDockWarningBreathing();
            }
            else
            {
                StopWarning();
            }
        }

        public int GetOccupiedSlotsCount()
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsOccupied)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>暂存区从左到右，不含正在消除的棋。</summary>
        public void CopyOccupiedXSortsLeftToRight(List<XSortActor> dst)
        {
            dst.Clear();
            if (slots == null)
                return;

            for (int i = 0; i < slots.Count; i++)
            {
                SlotActor slot = slots[i];
                if (!slot.IsOccupied || slot.SlotCase == null || slot.SlotCase.IsBeingRemoved)
                    continue;
                if (slot.SlotCase.Behavior is XSortActor XSort && XSort.XSortModel != null)
                    dst.Add(XSort);
            }
        }

        /// <summary>逻辑占用：待消不占容量；已可三消时预扣 3，起飞即可继续收。</summary>
        public int GetEffectiveOccupiedSlotsCount()
        {
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsOccupied || slot.SlotCase == null)
                    continue;
                if (slot.SlotCase.IsBeingRemoved)
                    continue;
                count++;
            }

            // 含飞入中的待三消：预扣 3 格，不必等 IsBeingRemoved
            if (count >= 3 && CheckMatch(false))
                count -= 3;

            return count;
        }

        public bool HasBeingRemovedXSorts()
        {
            if (slots == null || slots.Count == 0)
                return false;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsOccupied && slot.SlotCase != null && slot.SlotCase.IsBeingRemoved)
                    return true;
            }
            return false;
        }

        /// <summary>是否允许再收集：只看有效占用。物理满槽由 Submit 扩临时格，待消仍占物理槽。</summary>
        public bool CanAcceptMoreSubmits()
        {
            return GetEffectiveOccupiedSlotsCount() < LevelDirector.DockAb.GetMaxSlotCnt();
        }

        //是否有相同的
        public bool GetHasThreeSameTypeOccupiedSlot()
        {
            if (slots == null || slots.Count == 0) return false;

            Dictionary<int, int> typeCounts = new Dictionary<int, int>();
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsOccupied) continue;

                var behavior = slot.SlotCase.Behavior as XSortActor;
                if (behavior == null || behavior.XSortModel == null) continue;

                int typeId = behavior.XSortModel.XSortIndex;
                if (!typeCounts.ContainsKey(typeId)) typeCounts[typeId] = 0;
                typeCounts[typeId]++;
                if (typeCounts[typeId] >= 3) return true;
            }

            return false;
        }

        public List<XSortActor> GetOccupiedXSortIndices()
        {
            var indices = new List<XSortActor>();
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsOccupied) continue;
                var behavior = slot.SlotCase.Behavior as XSortActor;
                if (behavior != null && behavior.XSortModel != null)
                {
                    indices.Add(behavior);
                }
            }
            return indices;
        }

        public static void OnMovementEnded(SlotCase slotCase, DockMoveType moveType)
        {
            instance.OnMoveEnded(slotCase, moveType);
        }

        public void OnMoveEnded(SlotCase slotCase, DockMoveType moveType)
        {
            if (!GameDirector.IsGameActive)
                return;

            // BoxCover 胜利清盘期间忽略飞入回调，避免 ForceClear 后 BA temp+XSort_left 被打乱。
            if (m_boxCoverVictoryClearing)
                return;

            CheckMatch();

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (slot.IsOccupied && (slot.SlotCase.IsMoving || slot.SlotCase.IsBeingRemoved)) return;
            }

            ShiftAllLeft();

            SnapSubmittedXSortsToSlotAnchorsIfIdle();

            int tempCount = addedDepth * 3;
            bool canDropTemps = tempCount > 0 && tempCount <= slots.Count;
            for (int i = 0; i < tempCount && canDropTemps; i++)
            {
                if (slots[slots.Count - 1 - i].IsOccupied)
                    canDropTemps = false;
            }

            if (canDropTemps)
            {
                for (int i = 0; i < tempCount; i++)
                {
                    var slot = slots[slots.Count - 1];
                    slots.RemoveAt(slots.Count - 1);

                    slot.Clear();
                    Destroy(slot.gameObject);
                }

                addedDepth = 0;
            }

            GameEvent.Send((int)EventNameDef.SubmitOver);

            // 提交动画结束后，若占用数达到5，则触发引导
            try
            {
                int restCnt = LevelDirector.DockAb.GetRestEmptyCnt(); // GetOccupiedSlotsCount();
                if (restCnt == 2)
                {
                    var ui = UIController.GetPage<UIGame>();
                    if (ui != null)
                    {
                        var bSuc = ui.CheckShowGuide();
                        if (bSuc)
                        {
                            RaycastController.Disable();
                        }
                    }
                }
            }
            catch { }

            if (GetEffectiveOccupiedSlotsCount() >= LevelDirector.DockAb.GetMaxSlotCnt()
                && !CheckMatch(false)
                && !HasBeingRemovedXSorts()
                && GameDirector.IsGameActive
                && !m_isBoxCoverQuickFinishing
                && !m_boxCoverVictoryClearing)
            {
                levelController.OnSlotsFilled();
            }

            TrySettleIqIfBoardStuck();

            // 检查快速结算条件（在移动结束后，确保状态已更新）
            Utility.DGDelayedCall(0.3f, () =>
            {
                TryCheckQuickFinishLevel();
            }, this);
        }

        public void AddExtraSlot()
        {
        }

        private IEnumerator WaitAndSpawnExtraSlotCoroutine()
        {
            while (addedDepth != 0) yield return null;

            SpawnExtraSlot();
        }

        private void SpawnExtraSlot()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                slot.ChangePosition(slot.transform.position.SetX(-LevelFitter.SlotSize.x * 8f / 2f + (i + 0.5f) * LevelFitter.SlotSize.x));
            }

            var newSlot = Instantiate(slotPrefab).GetComponent<SlotActor>();

            var position = slots[0].transform.position.SetX(-LevelFitter.SlotSize.x * 8f / 2f + (7 + 0.5f) * LevelFitter.SlotSize.x);
            var scale = Vector3.one * LevelFitter.SlotSize;

            newSlot.Init(7, position, scale);

            newSlot.transform.localScale = Vector3.zero;
            newSlot.transform.DOScale(1, 0.1f).SetDelay(0.025f);

            slots.Add(newSlot);
            newSlot.ApplySlotUi(ThemeUiApplier.Get()?.slot);

            Utility.DGDelayedCall(0.1f, () => LevelDirector.IsRaycastEnabled = true, this);
        }

        public int CountXSorts(XSortActor XSort)
        {
            var counter = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (slot.IsOccupied)
                {
                    if (XSort.IsSameType(slot.SlotCase.Behavior))
                    {
                        counter++;
                    }
                }
            }

            return counter;
        }

        public void ShowOrHideLongPressAnim(XSortActor beh, bool b)
        {
            if (b)
            {
                longPressAnim.transform.position = beh.transform.position;
            }
            longPressAnim.SetActiveEx(b);
        }

        /// <summary>
        /// 常驻长按提示特效：显示/隐藏，并设置位置与层级。
        /// </summary>
        public void ShowOrHideLongPressHintEff(XSortActor beh, bool show, int sortingOrder)
        {
            if (longPressHintEff == null)
                longPressHintEff = XSortLongPressHintEffect.Instance;
            if (longPressHintEff == null) return;

            if (show && beh != null)
                longPressHintEff.ShowOn(beh, sortingOrder);
            else
                longPressHintEff.Hide();
        }

        public void ShowFingerAnim(Vector3 pos)
        {
            return; // TODO: 暂时关闭点赞特效

            fingerAnimEff.SetActive(false);
            fingerAnimEff.SetActive(true);

            // fingerAnim.SetActiveEx(true);
            // 重置动画到第一帧
            // fingerAnim.AnimationState.SetEmptyAnimation(0, 0f);
            // fingerAnim.AnimationState.SetAnimation(0, "hand", false);
            fingerAnimEff.transform.position = pos;
            // fingerAnim
            Utility.DGDelayedCall(2.0f, () =>
            {
                fingerAnimEff.SetActive(false);
            }, this);
        }

        private bool IsGuiding = false;
        public void ShowOrHideGuide(bool b)
        {
            int maxSlotCnt = LevelDirector.DockAb.GetMaxSlotCnt();

            IsGuiding = b;
            var slots = GetComponentsInChildren<SlotActor>(true);
            if (slots != null && slots.Length > 0)
            {
                System.Array.Sort(slots, (a, c) => a.transform.position.x.CompareTo(c.transform.position.x));

                if (b)
                {
                    for (int i = 0; i < slots.Length; i++)
                    {
                        bool highlight = i >= maxSlotCnt - 2; // 右侧最后两个slot
                        slots[i].ShowOrHideOutline(highlight);
                    }
                }
                else
                {
                    for (int i = 0; i < slots.Length; i++)
                    {
                        slots[i].ShowOrHideOutline(false);
                    }
                }
            }

            if (b)
            {
                PlayWarning();
            }
            else
            {
                StopWarning();
            }

            ApplyBoxPandaGuideSorting(b);
        }

        void ApplyBoxPandaGuideSorting(bool show)
        {
            if (boxPanda == null)
                return;
            MeshRenderer renderer = boxPanda.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            if (show)
            {
                if (!boxPandaGuideSortingSaved)
                {
                    boxPandaSortingBeforeGuide = renderer.sortingOrder;
                    boxPandaGuideSortingSaved = true;
                }
                renderer.sortingOrder = BoxPandaSortingDuringGuide;
                return;
            }

            if (!boxPandaGuideSortingSaved)
                return;
            renderer.sortingOrder = boxPandaSortingBeforeGuide;
            boxPandaGuideSortingSaved = false;
        }

        public static void TryCheckQuickFinishLevel(float delay = 0f)
        {
            if (instance == null || !AutoCompleteSettings.IsEnabled || AutoCompleteSettings.IsBlockedByCurrentLevelConfig()
                || !GameDirector.IsGameActive || LevelDirector.IsBusy)
                return;

            if (delay <= 0f)
            {
                instance.CheckQuickFinishLevel();
                return;
            }

            Utility.DGDelayedCall(delay, () => instance?.CheckQuickFinishLevel(), instance);
        }

        public void CheckQuickFinishLevel()
        {
            if (!AutoCompleteSettings.IsEnabled)
                return;

            if (AutoCompleteSettings.IsBlockedByCurrentLevelConfig())
            {
                if (LevelDirector.BInCoinRewardLevel)
                    Debug.Log("[QuickFinish] 金币关卡，不触发自动完成");
                else if (BoxCoverManager.HasActiveLevel
                    || IQChallengeManager.HasActiveLevel
                    || AwardColorEggCoverManager.HasActiveLevel)
                    Debug.Log("[QuickFinish] 特殊关卡自行结算，不触发普通自动完成");
                else
                    Debug.Log("[QuickFinish] 分层拆组配置(eh=3/4/5)，不触发自动完成");
                return;
            }

            // 如果已经在快速结算中，不重复触发
            if (m_isQuickFinishing || m_quickFinishPending)
            {
                Debug.Log("[QuickFinish] 已经在快速结算中，跳过检查");
                return;
            }

            LevelGenService levelGen = LevelDirector.LevelGenService;
            if (levelGen == null)
                return;

            if (!MeetsMinInitialGroupRule(levelGen))
            {
                Debug.Log($"[QuickFinish] 初始组数 {levelGen.InitialGroupCount} < {AutoCompleteSettings.MinInitialGroupCount}，不满足条件");
                return;
            }

            // 检查场上所有剩余棋子是否都可以被点击（没有被遮挡）
            if (!AreAllXSortsClickable())
            {
                Debug.Log("[QuickFinish] 场上还有棋子被遮挡，不满足条件");
                return;
            }

            if (HasBlockingSpecialEffectXSorts())
            {
                Debug.Log("[QuickFinish] 剩余棋子中有问号/冰块，不满足条件");
                return;
            }

            int currentRemaining = GetCurrentRemainingXSortCount();
            int initialTotal = levelGen.InitialXSortCount;
            if (!MeetsAutoCompleteProgressRule(currentRemaining, initialTotal))
            {
                float ratio = initialTotal > 0 ? (float)currentRemaining / initialTotal : 1f;
                Debug.Log($"[QuickFinish] 场上剩余 {currentRemaining}/{initialTotal} ({ratio:P1})，不满足比例或最小剩余数条件");
                return;
            }

            // 飞入未落地时先不弹胜利条。落地后 OnMoveEnded 会先判满槽失败，再复查自动完成。
            if (HasActiveSlotMotion())
                return;

            // 已经是无法消除的满槽：失败优先，不走自动完成。
            if (IsDockLoss())
            {
                levelController.OnSlotsFilled();
                return;
            }

            Debug.Log("[QuickFinish] 开始快速消除！");

            var autocompleteClip = AudioController.AudioClips?.autocomplete;
            if (autocompleteClip != null)
                AudioController.PlaySound(autocompleteClip);

            m_quickFinishPending = true;
            // 禁用玩家操作（与 AAXSortExploreLike 一致：胜利页展示前即锁住，避免 1s 延迟内误触）
            RaycastController.Disable();
            LevelDirector.SetBusyState(true);
            // 打断进行中的长按，避免与快速结算飞入动画抢 transform / 特效
            RaycastController.ResetLongPress();

            // 开始快速消除
            UIQuickVictory.Show();
            m_quickFinishDelay = DOVirtual.DelayedCall(1.0f, OnQuickFinishDelayElapsed).SetUpdate(true).SetTarget(this);
        }

        /// <summary>有效占用已满、当前不能三消、也没有正在消除的棋子。</summary>
        bool IsDockLoss()
        {
            return GetEffectiveOccupiedSlotsCount() >= LevelDirector.DockAb.GetMaxSlotCnt()
                && !CheckMatch(false)
                && !HasBeingRemovedXSorts();
        }

        /// <summary>
        /// 1s 等待期间备战栏被填成失败。收掉胜利条，等飞入结束后走失败。
        /// </summary>
        public void CancelPendingQuickFinishIfLoss()
        {
            if (m_isQuickFinishing || !m_quickFinishPending || !IsDockLoss())
                return;

            m_quickFinishPending = false;
            m_quickFinishDelay?.Kill();
            m_quickFinishDelay = null;
            UIQuickVictory.HideAll();
            RaycastController.Enable();
            LevelDirector.SetBusyState(false);
        }

        void OnQuickFinishDelayElapsed()
        {
            m_quickFinishDelay = null;
            if (this == null || !GameDirector.IsGameActive)
            {
                m_quickFinishPending = false;
                UIQuickVictory.HideAll();
                return;
            }

            if (IsDockLoss())
            {
                m_quickFinishPending = false;
                UIQuickVictory.HideAll();
                RaycastController.Enable();
                LevelDirector.SetBusyState(false);
                levelController.OnSlotsFilled();
                return;
            }

            // 等待期间又有棋子飞入（且不是失败满槽），落地后再开始，避免和飞入抢槽。
            if (HasActiveSlotMotion() || IsClearingAnimation())
            {
                m_quickFinishDelay = DOVirtual.DelayedCall(0.2f, OnQuickFinishDelayElapsed).SetUpdate(true).SetTarget(this);
                return;
            }

            m_quickFinishPending = false;
            StartQuickFinish();
        }

        int GetCurrentRemainingXSortCount()
        {
            LevelGenService levelGen = LevelDirector.LevelGenService;
            if (levelGen == null)
                return 0;

            // 比例与最小剩余数仅统计场上棋子，不含已收集到卡槽中的棋子
            return levelGen.GetRestXSortCount();
        }

        static bool MeetsMinInitialGroupRule(LevelGenService levelGen)
        {
            return levelGen.InitialGroupCount >= AutoCompleteSettings.MinInitialGroupCount;
        }

        static bool MeetsAutoCompleteProgressRule(int currentRemaining, int initialTotal)
        {
            if (initialTotal <= 0)
                return false;

            if (currentRemaining <= AutoCompleteSettings.MinRemainingXSorts)
                return false;

            return (float)currentRemaining / initialTotal <= AutoCompleteSettings.MaxRemainingRatio;
        }

        bool HasBlockingSpecialEffectXSorts()
        {
            LevelGenService levelGen = LevelDirector.LevelGenService;
            List<XSortActor> boardXSorts = levelGen?.ActXSorts;
            if (boardXSorts != null)
            {
                foreach (var XSort in boardXSorts)
                {
                    if (XSort.IsSubmitted)
                        continue;

                    if (IsBlockingSpecialEffect(XSort))
                        return true;
                }
            }

            return false;
        }

        static bool IsBlockingSpecialEffect(XSortActor XSort)
        {
            if (XSort?.Effect == null)
                return false;

            XSortEffectType effectType = XSort.Effect.EffectType;
            return effectType == XSortEffectType.Unknown || effectType == XSortEffectType.Ice;
        }

        /// <summary>
        /// 检查场上所有剩余棋子是否都可以被点击（没有被遮挡）
        /// </summary>
        private bool AreAllXSortsClickable()
        {
            LevelGenService levelGen = LevelDirector.LevelGenService;
            List<XSortActor> allActiveXSorts = levelGen?.ActXSorts;

            if (allActiveXSorts == null || allActiveXSorts.Count == 0)
                return false;

            // 必须用遮挡关系判断，不能只用 IsClickable：进关/恢复时生成动画里会把棋子标成可点，
            // 而 UpdateClickableStates 可能在 Dock OnMoveEnded 触发的快速结算检查之后才执行，会误触发快速结算。
            foreach (var XSort in allActiveXSorts)
            {
                if (!XSort.IsSubmitted && !levelGen.IsXSortUnconcealed(XSort))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 开始快速消除
        /// </summary>
        private void StartQuickFinish()
        {
            if (m_isQuickFinishing)
            {
                return;
            }

            m_isQuickFinishing = true;

            // 禁用玩家操作
            RaycastController.Disable();
            LevelDirector.SetBusyState(true);
            RaycastController.ResetLongPress();

            // 开始快速消除协程
            if (m_quickFinishCoroutine != null)
            {
                StopCoroutine(m_quickFinishCoroutine);
            }
            m_quickFinishCoroutine = StartCoroutine(QuickFinishCoroutine());
        }

        // [SerializeField] float quickFinishXSortInterval = 0.15f; // 快速消除时每个棋子提交的间隔时间，可以修改

        /// <summary>
        /// 快速消除协程
        /// </summary>
        private IEnumerator QuickFinishCoroutine()
        {
            float XSortSubmitInterval = quickFinishXSortInterval; // 每个棋子提交的间隔时间

            while (true)
            {
                if (!GameDirector.IsGameActive)
                {
                    Debug.Log("[QuickFinish] 关卡已结束，停止快速结算");
                    break;
                }

                // 检查场上是否还有棋子
                List<XSortActor> activeXSorts = LevelDirector.LevelGenService.ActXSorts;
                if (activeXSorts == null || activeXSorts.Count == 0)
                {
                    Debug.Log("[QuickFinish] 场上棋子已全部消除，结束快速结算");
                    break;
                }

                // 如果暂存区已满且无法匹配，停止快速结算
                if (IsFilled && !CheckMatch(false))
                {
                    Debug.Log("[QuickFinish] 暂存区有效已满且无法匹配，停止快速结算");
                    break;
                }

                // 获取下一个要提交的棋子（优先选择能形成三消的）
                XSortActor nextXSort = PickNextSubmitXSort();

                if (nextXSort == null)
                {
                    Debug.Log("[QuickFinish] 无法找到下一个要提交的棋子，退出循环");
                    break;
                }

                Debug.Log($"[QuickFinish] 准备提交棋子: {nextXSort.XSortModel?.XSortIndex}");

                // 提交单个棋子（类似 PUHintBehavior，传递 bFromHintItem=true）
                nextXSort.MarkAsSubmitted(true);
                nextXSort.DisableEffect();
                nextXSort.SetIsClickable(true, false);

                // 手动执行提交逻辑，确保bFromHintItem=true被正确传递
                bool bSuc = SubmitToSlot(nextXSort, false);
                if (!bSuc)
                {
                    nextXSort.ResetSubmitState();
                    Debug.Log("[QuickFinish] 提交失败，停止快速结算");
                    break;
                }

                var handPos = nextXSort.transform.position;
                int preCnt = LevelDirector.LevelGenService.GetCalucuteFitXSortCnt(nextXSort);
                LevelDirector.LevelGenService.RemoveObject(nextXSort);
                LevelDirector.LevelGenService.UpdateClickableStates(true);
                int afterCnt = LevelDirector.LevelGenService.GetCalucuteFitXSortCnt(nextXSort);

                if (afterCnt > preCnt)
                {
                    ShowFingerAnim(handPos);
                }

                // 只等待间隔时间，不等待动画完成（可以连续提交）
                yield return new WaitForSeconds(XSortSubmitInterval);
            }

            Debug.Log("[QuickFinish] 快速结算结束，恢复玩家操作");
            m_isQuickFinishing = false;
            m_quickFinishCoroutine = null;
            // 满槽待死亡结算时保持禁操作，避免飞入未完成时再点一颗把棋子挤出 Dock
            if (!GameDirector.IsGameActive
                || (GetEffectiveOccupiedSlotsCount() >= LevelDirector.DockAb.GetMaxSlotCnt()
                    && !CheckMatch(false)
                    && !HasBeingRemovedXSorts()))
                yield break;
            RaycastController.Enable();
            LevelDirector.SetBusyState(false);
        }

        /// <summary>
        /// 获取下一个要提交的棋子（优先选择能形成三消的，确保能通关）
        /// </summary>
        private XSortActor PickNextSubmitXSort()
        {
            // 如果暂存区已满，无法继续提交
            if (IsFilled)
            {
                Debug.Log("[QuickFinish] PickNextSubmitXSort: 暂存区已满");
                return null;
            }

            // 使用 GetActiveXSorts(false) 获取所有棋子，包括有特效的
            // 快速通关时应该可以提交所有可点击的棋子
            List<XSortActor> activeXSorts = LevelDirector.GetActiveXSorts(false);
            if (activeXSorts == null || activeXSorts.Count == 0)
            {
                Debug.Log("[QuickFinish] PickNextSubmitXSort: 场上没有可用的棋子");
                return null;
            }

            // 统计暂存区中每种类型的数量
            Dictionary<int, int> dockTypeCounts = new Dictionary<int, int>();
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsOccupied) continue;

                var behavior = slot.SlotCase.Behavior as XSortActor;
                if (behavior == null || behavior.XSortModel == null) continue;

                int typeId = behavior.XSortModel.XSortIndex;
                if (!dockTypeCounts.ContainsKey(typeId))
                {
                    dockTypeCounts[typeId] = 0;
                }
                dockTypeCounts[typeId]++;
            }

            // 优先选择能形成三消的棋子类型（暂存区已有1个或2个的）
            XSortActor bestXSort = null;
            int bestPriority = -1; // 优先级：2个同色 > 1个同色 > 其他

            foreach (var kvp in dockTypeCounts)
            {
                if (kvp.Value >= 1 && kvp.Value < 3)
                {
                    int neededCount = 3 - kvp.Value;
                    int priority = kvp.Value; // 2个同色的优先级更高

                    // 检查场上是否有足够的该类型可点击棋子
                    List<XSortActor> availableXSorts = activeXSorts
                        .Where(t => !t.IsSubmitted &&
                                    t.IsClickable &&
                                    t.XSortModel != null &&
                                    t.XSortModel.XSortIndex == kvp.Key)
                        .ToList();

                    if (availableXSorts.Count >= neededCount && priority > bestPriority)
                    {
                        // 选择该类型中能最快形成三消的棋子（优先选择能暴露更多棋子的）
                        XSortActor candidate = SelectBestXSortForMatch(availableXSorts, neededCount);
                        if (candidate != null)
                        {
                            bestXSort = candidate;
                            bestPriority = priority;
                        }
                    }
                }
            }

            // 如果找到了能形成三消的棋子，返回它
            if (bestXSort != null)
            {
                Debug.Log($"[QuickFinish] PickNextSubmitXSort: 选择能形成三消的棋子，类型={bestXSort.XSortModel.XSortIndex}, 暂存区已有={bestPriority}个");
                return bestXSort;
            }

            // 如果没有能形成三消的，选择能最快形成三消的棋子类型
            // 优先选择场上数量多的类型，这样更容易形成三消
            var typeGroups = activeXSorts
                .Where(t => !t.IsSubmitted && t.IsClickable && t.XSortModel != null)
                .GroupBy(t => t.XSortModel.XSortIndex)
                .OrderByDescending(g => g.Count())
                .ToList();

            foreach (var group in typeGroups)
            {
                int typeId = group.Key;
                List<XSortActor> availableXSorts = group.ToList();

                // 检查暂存区是否有足够空间
                int slotsAvailable = GetSlotsAvailable();
                int dockCount = dockTypeCounts.ContainsKey(typeId) ? dockTypeCounts[typeId] : 0;
                int neededCount = 3 - dockCount;

                if (neededCount > 0 && slotsAvailable >= neededCount && availableXSorts.Count >= neededCount)
                {
                    XSortActor candidate = SelectBestXSortForMatch(availableXSorts, neededCount);
                    if (candidate != null)
                    {
                        Debug.Log($"[QuickFinish] PickNextSubmitXSort: 选择能最快形成三消的棋子，类型={typeId}, 场上数量={availableXSorts.Count}");
                        return candidate;
                    }
                }
            }

            // 如果都没有，选择任意一个可点击的棋子
            XSortActor fallbackXSort = activeXSorts.FirstOrDefault(t => !t.IsSubmitted && t.IsClickable);
            if (fallbackXSort != null)
            {
                Debug.Log($"[QuickFinish] PickNextSubmitXSort: 选择任意可点击的棋子，类型={fallbackXSort.XSortModel?.XSortIndex}");
                return fallbackXSort;
            }

            Debug.Log("[QuickFinish] PickNextSubmitXSort: 无法找到合适的棋子");
            return null;
        }

        /// <summary>
        /// 从候选棋子中选择最佳棋子（优先选择能暴露更多棋子的）
        /// </summary>
        private XSortActor SelectBestXSortForMatch(List<XSortActor> candidates, int neededCount)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            // 优先选择能暴露更多棋子的（移除后能让更多棋子可点击）
            XSortActor bestXSort = null;
            int maxRevealedCount = -1;

            foreach (var XSort in candidates)
            {
                // 模拟移除这个棋子后，能暴露多少棋子
                int revealedCount = EstimateRevealedXSortsAfterRemove(XSort);

                if (revealedCount > maxRevealedCount)
                {
                    maxRevealedCount = revealedCount;
                    bestXSort = XSort;
                }
            }

            return bestXSort ?? candidates[0]; // 如果都差不多，返回第一个
        }

        /// <summary>
        /// 估算移除某个棋子后能暴露多少棋子
        /// </summary>
        private int EstimateRevealedXSortsAfterRemove(XSortActor XSort)
        {
            // 简单估算：检查该棋子下方的棋子数量
            int revealedCount = 0;
            ElementPosition pos = XSort.ElementPosition;

            // 检查下方相邻位置的棋子
            ElementPosition bottomPos = pos.BottomNeighbourPos;
            if (LevelDirector.LevelGenService.IsXSortExists(bottomPos))
            {
                var bottomXSort = LevelDirector.GetXSort(bottomPos);
                if (bottomXSort != null && !bottomXSort.IsSubmitted && !bottomXSort.IsClickable)
                {
                    revealedCount++;
                }
            }

            return revealedCount;
        }

        public void StartBoxCoverQuickFinish(Action onFinished)
        {
            if (m_isBoxCoverQuickFinishing)
                return;

            m_boxCoverQuickFinishCallback = onFinished;
            m_isBoxCoverQuickFinishing = true;
            ApplyBoxCoverQuickFinishTimeScale();
            LevelDirector.WinState = 3;
            RaycastController.Disable();
            LevelDirector.SetBusyState(true);
            RaycastController.ResetLongPress();

            if (m_boxCoverQuickFinishCoroutine != null)
                StopCoroutine(m_boxCoverQuickFinishCoroutine);
            m_boxCoverQuickFinishCoroutine = StartCoroutine(BoxCoverQuickFinishCoroutine());
        }

        IEnumerator BoxCoverQuickFinishCoroutine()
        {
            float interval = quickFinishXSortInterval / 1.5f;
            int occupied = GetOccupiedSlotsCount();
            int forceGroups = occupied >= 7 ? 2 : occupied == 6 ? 1 : 0;
            if (forceGroups > 0)
                yield return ForceSeedBoxCoverMatches(forceGroups, interval);

            while (true)
            {
                if (!GameDirector.IsGameActive)
                {
                    FinishBoxCoverQuickFinish(false);
                    yield break;
                }

                List<XSortActor> activeXSorts = LevelDirector.LevelGenService?.ActXSorts;
                if (activeXSorts == null || activeXSorts.Count == 0)
                    break;

                int maxSlotCnt = LevelDirector.DockAb.GetMaxSlotCnt();
                int effectiveCnt = GetEffectiveOccupiedSlotsCount();
                if (effectiveCnt >= maxSlotCnt && !CheckMatch(false) && !HasBeingRemovedXSorts())
                    break;
                if (effectiveCnt >= maxSlotCnt)
                {
                    yield return new WaitForSeconds(interval);
                    continue;
                }

                XSortActor nextXSort = PickBoxCoverQuickFinishXSort();
                if (nextXSort == null)
                    break;

                if (!TrySubmitBoxCoverQuickXSort(nextXSort))
                    break;

                yield return new WaitForSeconds(interval);
            }

            ForceClearRemainingBoxCoverXSorts();
            FinishBoxCoverQuickFinish(true);
        }

        IEnumerator ForceSeedBoxCoverMatches(int groupCount, float interval)
        {
            List<int> types = PickBoxCoverForceTypes(groupCount);
            for (int typeIndex = 0; typeIndex < types.Count; typeIndex++)
            {
                int typeId = types[typeIndex];
                int have = CountDockXSortsOfType(typeId);
                int need = 3 - have;
                for (int i = 0; i < need; i++)
                {
                    if (!GameDirector.IsGameActive)
                        yield break;

                    EnsureTempSlotsForBoxCoverForce();
                    XSortActor XSort = FindBoardXSortOfType(typeId);
                    if (XSort == null || !TrySubmitBoxCoverQuickXSort(XSort))
                        yield break;

                    yield return new WaitForSeconds(interval);
                }
            }
        }

        List<int> PickBoxCoverForceTypes(int groupCount)
        {
            Dictionary<int, int> dockCounts = BuildDockTypeCounts();
            Dictionary<int, int> boardCounts = BuildBoardTypeCounts();
            List<int> candidates = new List<int>();
            foreach (KeyValuePair<int, int> pair in dockCounts)
            {
                if (pair.Value < 1 || pair.Value >= 3)
                    continue;

                int need = 3 - pair.Value;
                int board = boardCounts.TryGetValue(pair.Key, out int count) ? count : 0;
                if (board >= need)
                    candidates.Add(pair.Key);
            }

            candidates.Sort((left, right) =>
            {
                int compare = dockCounts[right].CompareTo(dockCounts[left]);
                return compare != 0 ? compare : left.CompareTo(right);
            });

            if (candidates.Count > groupCount)
                candidates.RemoveRange(groupCount, candidates.Count - groupCount);
            return candidates;
        }

        Dictionary<int, int> BuildDockTypeCounts()
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            if (slots == null)
                return counts;

            for (int i = 0; i < slots.Count; i++)
            {
                SlotActor slot = slots[i];
                if (slot == null || !slot.IsOccupied)
                    continue;

                XSortActor XSort = slot.SlotCase.Behavior as XSortActor;
                if (XSort?.XSortModel == null)
                    continue;

                int typeId = XSort.XSortModel.XSortIndex;
                counts.TryGetValue(typeId, out int count);
                counts[typeId] = count + 1;
            }

            return counts;
        }

        static Dictionary<int, int> BuildBoardTypeCounts()
        {
            Dictionary<int, int> counts = new Dictionary<int, int>();
            List<XSortActor> XSorts = LevelDirector.LevelGenService?.ActXSorts;
            if (XSorts == null)
                return counts;

            for (int i = 0; i < XSorts.Count; i++)
            {
                XSortActor XSort = XSorts[i];
                if (XSort == null || XSort.IsSubmitted || XSort.XSortModel == null)
                    continue;

                int typeId = XSort.XSortModel.XSortIndex;
                counts.TryGetValue(typeId, out int count);
                counts[typeId] = count + 1;
            }

            return counts;
        }

        int CountDockXSortsOfType(int typeId)
        {
            BuildDockTypeCounts().TryGetValue(typeId, out int count);
            return count;
        }

        static XSortActor FindBoardXSortOfType(int typeId)
        {
            List<XSortActor> XSorts = LevelDirector.LevelGenService?.ActXSorts;
            if (XSorts == null)
                return null;

            for (int i = 0; i < XSorts.Count; i++)
            {
                XSortActor XSort = XSorts[i];
                if (XSort == null || XSort.IsSubmitted || XSort.XSortModel == null)
                    continue;
                if (XSort.XSortModel.XSortIndex == typeId)
                    return XSort;
            }

            return null;
        }

        XSortActor PickBoxCoverQuickFinishXSort()
        {
            if (!CanAcceptMoreSubmits())
                return null;

            List<XSortActor> activeXSorts = LevelDirector.GetActiveXSorts(false);
            if (activeXSorts == null || activeXSorts.Count == 0)
                return null;

            Dictionary<int, int> dockTypeCounts = BuildDockTypeCounts();
            XSortActor bestXSort = null;
            int bestPriority = -1;
            foreach (KeyValuePair<int, int> pair in dockTypeCounts)
            {
                if (pair.Value < 1 || pair.Value >= 3)
                    continue;

                int neededCount = 3 - pair.Value;
                List<XSortActor> availableXSorts = new List<XSortActor>();
                for (int i = 0; i < activeXSorts.Count; i++)
                {
                    XSortActor XSort = activeXSorts[i];
                    if (XSort == null
                        || XSort.IsSubmitted
                        || !XSort.IsClickable
                        || XSort.XSortModel == null
                        || XSort.XSortModel.XSortIndex != pair.Key)
                    {
                        continue;
                    }

                    availableXSorts.Add(XSort);
                }

                if (availableXSorts.Count < neededCount || pair.Value <= bestPriority)
                    continue;

                XSortActor candidate = SelectBestXSortForMatch(availableXSorts, neededCount);
                if (candidate != null)
                {
                    bestXSort = candidate;
                    bestPriority = pair.Value;
                }
            }

            if (bestXSort != null)
                return bestXSort;

            var typeGroups = activeXSorts
                .Where(XSort => !XSort.IsSubmitted && XSort.IsClickable && XSort.XSortModel != null)
                .GroupBy(XSort => XSort.XSortModel.XSortIndex)
                .OrderByDescending(group => group.Count());
            foreach (var group in typeGroups)
            {
                List<XSortActor> availableXSorts = group.ToList();
                int slotsAvailable = GetSlotsAvailable();
                int dockCount = dockTypeCounts.TryGetValue(group.Key, out int count) ? count : 0;
                int neededCount = 3 - dockCount;
                if (neededCount <= 0 || slotsAvailable < neededCount || availableXSorts.Count < neededCount)
                    continue;

                XSortActor candidate = SelectBestXSortForMatch(availableXSorts, neededCount);
                if (candidate != null)
                    return candidate;
            }

            return activeXSorts.FirstOrDefault(XSort => !XSort.IsSubmitted);
        }

        void EnsureTempSlotsForBoxCoverForce()
        {
            if (addedDepth >= MaxTempDepth || HasEmptySlot())
                return;

            ExpandTempSlotsLayer();
        }

        bool TrySubmitBoxCoverQuickXSort(XSortActor XSort)
        {
            if (XSort == null)
                return false;

            XSort.MarkAsSubmitted(true);
            XSort.DisableEffect();
            XSort.SetIsClickable(true, false);
            if (!SubmitToSlot(XSort, false))
            {
                XSort.ResetSubmitState();
                return false;
            }

            LevelDirector.LevelGenService.RemoveObject(XSort);
            LevelDirector.LevelGenService.UpdateClickableStates(true);
            return true;
        }

        void ForceClearRemainingBoxCoverXSorts()
        {
            // 胜利已锁定：把场上+备战栏一次性清干净，保持 temp+XSort_left 为 0（3 的倍数）。
            // 旧逻辑只 Destroy 场上残留、不处理备战栏，会把守恒打成非 3 倍数并狂刷「出现异常=-=」。
            m_boxCoverVictoryClearing = true;
            m_matchClearGeneration++;

            LevelGenService gen = LevelDirector.LevelGenService;
            if (gen?.ActXSorts != null)
            {
                List<XSortActor> remaining = new List<XSortActor>(gen.ActXSorts);
                for (int i = 0; i < remaining.Count; i++)
                {
                    XSortActor XSort = remaining[i];
                    if (XSort == null)
                        continue;

                    if (!XSort.IsSubmitted)
                    {
                        XSort.MarkAsSubmitted(true);
                        XSort.DisableEffect();
                    }

                    gen.RemoveObject(XSort);
                    if (XSort != null && XSort.gameObject != null)
                        XSort.gameObject.SetActive(false);
                }
            }

            InstantClearEntireDockForBoxCoverVictory();
        }

        void InstantClearEntireDockForBoxCoverVictory()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Count; i++)
            {
                SlotActor slot = slots[i];
                if (slot == null || !slot.IsOccupied)
                    continue;

                SlotCase slotCase = slot.SlotCase;
                if (slotCase == null)
                    continue;

                // Kill 飞入 tween，避免 Clear 后 OnMoveEnded 再入 CheckMatch。
                if (slotCase.Behavior != null)
                    slotCase.Clear(true);
                else
                {
                    slotCase.IsMoving = false;
                    slotCase.IsBeingRemoved = false;
                }

                slot.RemoveSlot();
            }

            ShiftAllLeft();

            while (addedDepth > 0 && slots.Count >= 3)
            {
                addedDepth--;
                for (int i = 0; i < 3; i++)
                {
                    SlotActor tempSlot = slots[^1];
                    slots.RemoveAt(slots.Count - 1);
                    tempSlot.Clear();
                    Destroy(tempSlot.gameObject);
                }
            }
        }

        void FinishBoxCoverQuickFinish(bool invokeCallback)
        {
            m_isBoxCoverQuickFinishing = false;
            m_boxCoverVictoryClearing = false;
            m_boxCoverQuickFinishCoroutine = null;
            RestoreBoxCoverQuickFinishTimeScale();
            Action callback = m_boxCoverQuickFinishCallback;
            m_boxCoverQuickFinishCallback = null;
            if (invokeCallback)
                callback?.Invoke();
        }

        void StopBoxCoverQuickFinish(bool invokeCallback)
        {
            if (m_boxCoverQuickFinishCoroutine != null)
            {
                StopCoroutine(m_boxCoverQuickFinishCoroutine);
                m_boxCoverQuickFinishCoroutine = null;
            }

            m_isBoxCoverQuickFinishing = false;
            m_boxCoverVictoryClearing = false;
            RestoreBoxCoverQuickFinishTimeScale();
            Action callback = m_boxCoverQuickFinishCallback;
            m_boxCoverQuickFinishCallback = null;
            if (invokeCallback)
                callback?.Invoke();
        }

        void ApplyBoxCoverQuickFinishTimeScale()
        {
            if (!m_boxCoverTimeScaleOverride)
            {
                m_boxCoverTimeScaleBefore = Time.timeScale;
                m_boxCoverTimeScaleOverride = true;
            }

            Time.timeScale = 2f;
        }

        void RestoreBoxCoverQuickFinishTimeScale()
        {
            if (!m_boxCoverTimeScaleOverride)
                return;

            m_boxCoverTimeScaleOverride = false;
            if (Mathf.Approximately(Time.timeScale, 2f))
            {
                Time.timeScale = m_boxCoverTimeScaleBefore > 0.0001f
                    ? m_boxCoverTimeScaleBefore
                    : 1f;
            }
        }

    }
}
