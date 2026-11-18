using System;
using System.Collections.Generic;
using DG.Tweening;
using TileGameCore;
using UnityEngine;
using UnityEngine.UI;

namespace FishTilePlay
{
    /// <summary>
    /// Runtime presentation and input for one box cover. Prefab layers are
    /// Visuals / Bottom / Content / Lid; collection flies authored Content children.
    /// </summary>
    public sealed class BoxCoverBehavior : MonoBehaviour, ITappable
    {
        [SerializeField] Transform visuals;
        [SerializeField] SpriteRenderer bottomRenderer;
        [SerializeField] Transform contentRoot;
        [SerializeField] SpriteRenderer lidRenderer;
        [SerializeField] BoxCollider boxCollider;
        [SerializeField] GameObject iceSmoke;

        readonly List<SpriteRenderer> contentItems = new();
        readonly List<SpriteRenderer> flyingItems = new();
        readonly List<RectTransform> flyingUiItems = new();

        BoxCoverController controller;
        BoxCoverResolvedPlacement placement;
        Sprite closedSprite;
        Sprite openSprite;
        Sprite lidSprite;
        Vector2 localVisualSize;
        Vector3 bottomScale = Vector3.one;
        Vector3 lidScale = Vector3.one;
        Vector3 stableLocalPosition;
        Sequence mainSequence;
        Renderer[] spawnTweenVisualRenderers;
        int visualSortingBase;
        bool lastBlockerHintPlayed;
        bool isAnimating;
        const float CollectFeedbackLead = 0.12f;
        bool isOpen;
        bool isCollected;
        int collectedItemCount;
        int previousBlockerCount = -1;

        public BoxCoverResolvedPlacement Placement => placement;
        public string RuntimeId => placement.RuntimeId;
        public bool IsOpen => isOpen;
        public bool IsCollected => isCollected;

        public bool ImageCovers(Vector3 worldPoint)
        {
            if (isOpen || isCollected)
                return false;
            if (SpriteCoversPoint(lidRenderer, worldPoint))
                return true;
            return SpriteCoversPoint(bottomRenderer, worldPoint);
        }

        static bool SpriteCoversPoint(SpriteRenderer picture, Vector3 worldPoint)
        {
            if (picture == null || !picture.gameObject.activeInHierarchy)
                return false;
            return TileStackUtil.SpriteCoversPoint(picture, worldPoint);
        }
        public bool IsAnimating => isAnimating;
        public int CollectedItemCount => collectedItemCount;
        public Vector2 VisualLocalSize => localVisualSize;
        public Bounds WorldBounds => boxCollider != null
            ? boxCollider.bounds
            : new Bounds(transform.position, Vector3.zero);

        public void Initialize(
            BoxCoverController owner,
            BoxCoverResolvedPlacement resolvedPlacement,
            Sprite closed,
            Sprite opened,
            Sprite lid,
            Vector3 localPosition,
            Vector2 visualSize,
            int sortingOrder,
            bool restoredOpen,
            int restoredCollectedItems)
        {
            controller = owner;
            placement = resolvedPlacement;
            closedSprite = closed;
            openSprite = opened;
            lidSprite = lid;
            localVisualSize = visualSize;
            collectedItemCount = Mathf.Clamp(restoredCollectedItems, 0, BoxCoverController.ItemsPerBox);
            isOpen = restoredOpen || collectedItemCount > 0;
            isCollected = collectedItemCount >= BoxCoverController.ItemsPerBox;
            isAnimating = false;
            lastBlockerHintPlayed = false;
            previousBlockerCount = -1;

            EnsureRuntimeParts();
            CollectContentItems();
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            stableLocalPosition = transform.localPosition;

            Sprite bottomSprite = openSprite != null ? openSprite : closedSprite;
            ConfigureRenderer(bottomRenderer, bottomSprite);
            ConfigureRenderer(lidRenderer, lidSprite != null ? lidSprite : closedSprite);
            FitVisualsToSize();
            visualSortingBase = sortingOrder;
            ApplyVisualSortingOrders();
            bottomScale = bottomRenderer != null ? bottomRenderer.transform.localScale : Vector3.one;
            lidScale = lidRenderer != null ? lidRenderer.transform.localScale : Vector3.one;

            for (int i = 0; i < contentItems.Count; i++)
            {
                SpriteRenderer item = contentItems[i];
                if (item == null)
                    continue;

                item.color = Color.white;
                bool alreadyCollected = i < collectedItemCount;
                item.gameObject.SetActive(!alreadyCollected);
            }

            if (lidRenderer != null)
                lidRenderer.gameObject.SetActive(!isOpen);

            SetIceSmokeActive(false);

            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(
                Mathf.Max(0.01f, localVisualSize.x * 0.96f),
                Mathf.Max(0.01f, localVisualSize.y * 0.96f),
                0.18f);
            boxCollider.enabled = !isOpen && !isCollected;
            PrepareForSpawnAnimation();
        }

        public void PrepareForSpawnAnimation()
        {
            if (!this)
                return;

            transform.DOKill();
            transform.localScale = Vector3.zero;
            SetSpawnTweenVisualsActive(false);
        }

        public void CompleteSpawnAnimation()
        {
            if (!this)
                return;

            transform.DOKill();
            transform.localScale = Vector3.one;
            SetSpawnTweenVisualsActive(true);
        }

        /// <summary>
        /// 缩放类登场：关掉 visuals 下所有 Renderer，避免 scale≈0 时仍出碎点。
        /// </summary>
        public void SetSpawnTweenVisualsActive(bool active)
        {
            if (!this || visuals == null)
                return;

            if (active || spawnTweenVisualRenderers == null)
                spawnTweenVisualRenderers = visuals.GetComponentsInChildren<Renderer>(true);

            if (spawnTweenVisualRenderers == null)
                return;

            for (int i = 0; i < spawnTweenVisualRenderers.Length; i++)
            {
                Renderer renderer = spawnTweenVisualRenderers[i];
                if (renderer != null)
                    renderer.enabled = active;
            }
        }

        void EnsureRuntimeParts()
        {
            if (visuals == null)
            {
                Transform existing = transform.Find("Visuals");
                if (existing != null)
                {
                    visuals = existing;
                }
                else
                {
                    GameObject visualsObject = new GameObject("Visuals");
                    visualsObject.transform.SetParent(transform, false);
                    visuals = visualsObject.transform;
                }
            }

            if (bottomRenderer == null)
            {
                Transform bottom = visuals.Find("Bottom");
                if (bottom == null)
                    bottom = visuals.Find("Body");
                if (bottom == null)
                {
                    GameObject bottomObject = new GameObject("Bottom");
                    bottomObject.transform.SetParent(visuals, false);
                    bottom = bottomObject.transform;
                }
                bottomRenderer = bottom.GetComponent<SpriteRenderer>();
                if (bottomRenderer == null)
                    bottomRenderer = bottom.gameObject.AddComponent<SpriteRenderer>();
            }

            if (contentRoot == null)
            {
                Transform content = visuals.Find("Content");
                if (content == null)
                {
                    GameObject contentObject = new GameObject("Content");
                    contentObject.transform.SetParent(visuals, false);
                    content = contentObject.transform;
                }
                contentRoot = content;
            }

            if (lidRenderer == null)
            {
                Transform lid = visuals.Find("Lid");
                if (lid == null)
                {
                    GameObject lidObject = new GameObject("Lid");
                    lidObject.transform.SetParent(visuals, false);
                    lid = lidObject.transform;
                }
                lidRenderer = lid.GetComponent<SpriteRenderer>();
                if (lidRenderer == null)
                    lidRenderer = lid.gameObject.AddComponent<SpriteRenderer>();
            }

            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider>();
                if (boxCollider == null)
                    boxCollider = gameObject.AddComponent<BoxCollider>();
            }

            EnsureIceSmoke();
        }

        void EnsureIceSmoke()
        {
            if (iceSmoke != null)
                return;

            Transform found = visuals != null
                ? visuals.Find("Ice_Smk")
                : transform.Find("Visuals/Ice_Smk");
            if (found != null)
                iceSmoke = found.gameObject;
        }

        void SetIceSmokeActive(bool active)
        {
            EnsureIceSmoke();
            if (iceSmoke == null)
                return;

            ParticleSystem[] systems = iceSmoke.GetComponentsInChildren<ParticleSystem>(true);
            if (!active)
            {
                for (int i = 0; i < systems.Length; i++)
                {
                    if (systems[i] == null)
                        continue;
                    systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    systems[i].Clear(true);
                }

                iceSmoke.SetActive(false);
                return;
            }

            iceSmoke.SetActive(true);
            ApplyVisualSortingOrders();
            Renderer[] renderers = iceSmoke.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = true;
            }

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null)
                    systems[i].Play(true);
            }
        }

        void CollectContentItems()
        {
            contentItems.Clear();
            if (contentRoot == null)
                return;

            for (int i = 0; i < contentRoot.childCount; i++)
            {
                SpriteRenderer renderer = contentRoot.GetChild(i).GetComponent<SpriteRenderer>();
                if (renderer != null)
                    contentItems.Add(renderer);
            }
        }

        static void ConfigureRenderer(SpriteRenderer renderer, Sprite sprite)
        {
            if (renderer == null)
                return;

            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 固定绘制顺序：底板、可乐、冷气、盖子。冷气夹在可乐和盖子之间。
        /// </summary>
        void ApplyVisualSortingOrders()
        {
            EnsureIceSmoke();
            int sibling = 0;
            if (bottomRenderer != null)
                bottomRenderer.transform.SetSiblingIndex(sibling++);
            if (contentRoot != null)
                contentRoot.SetSiblingIndex(sibling++);
            if (iceSmoke != null)
                iceSmoke.transform.SetSiblingIndex(sibling++);
            if (lidRenderer != null)
                lidRenderer.transform.SetSiblingIndex(sibling++);

            int next = visualSortingBase;
            AssignSorting(bottomRenderer, ref next);
            AssignSpriteSubtree(contentRoot, ref next);
            AssignRendererSubtree(iceSmoke != null ? iceSmoke.transform : null, ref next);
            AssignSorting(lidRenderer, ref next);
        }

        /// <summary>
        /// Hierarchy sibling order: later children get a higher Order in Layer.
        /// Bottom, then Content items, then Lid on top.
        /// </summary>
        public static void ApplyHierarchySortingOrders(Transform visualsRoot, int baseOrder)
        {
            int next = baseOrder;
            AssignSortingRecursive(visualsRoot, ref next);
        }

        static void AssignSortingRecursive(Transform node, ref int next)
        {
            if (node == null)
                return;

            for (int i = 0; i < node.childCount; i++)
            {
                Transform child = node.GetChild(i);
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = next;
                    next++;
                }

                AssignSortingRecursive(child, ref next);
            }
        }

        static void AssignSpriteSubtree(Transform root, ref int next)
        {
            if (root == null)
                return;

            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                AssignSorting(renderers[i], ref next);
        }

        static void AssignRendererSubtree(Transform root, ref int next)
        {
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                AssignSorting(renderers[i], ref next);
        }

        static void AssignSorting(Renderer renderer, ref int next)
        {
            if (renderer == null)
                return;

            renderer.sortingOrder = next;
            next++;
        }

        void FitVisualsToSize()
        {
            if (visuals == null)
                return;

            Sprite reference = bottomRenderer != null && bottomRenderer.sprite != null
                ? bottomRenderer.sprite
                : lidRenderer != null ? lidRenderer.sprite : null;
            if (reference == null)
            {
                visuals.localScale = Vector3.one;
                return;
            }

            Vector2 spriteSize = reference.bounds.size;
            visuals.localScale = new Vector3(
                spriteSize.x > 0.0001f ? localVisualSize.x / spriteSize.x : 1f,
                spriteSize.y > 0.0001f ? localVisualSize.y / spriteSize.y : 1f,
                1f);
        }

        public void RefreshBlockers(int blockerCount, bool acceptCurrentWithoutHint = false)
        {
            if (isOpen || isCollected || isAnimating)
                return;

            if (acceptCurrentWithoutHint)
                previousBlockerCount = blockerCount;

            bool transitionedToLastBlocker = blockerCount == 1 && previousBlockerCount != 1;
            previousBlockerCount = blockerCount;
            if (transitionedToLastBlocker && !lastBlockerHintPlayed)
            {
                lastBlockerHintPlayed = true;
                PlayShake();
                PlayClickSound();
            }

            if (blockerCount == 0 && BoxCoverManager.IsSpawnReady)
                controller?.RequestOpen(this);
        }

        public void BeginOpenAndCollect(
            Func<Vector3> targetWorldPosition,
            Action<BoxCoverBehavior> onOpened,
            Action<BoxCoverBehavior> onItemCollected,
            Action<BoxCoverBehavior> onCompleted)
        {
            if (isCollected || isAnimating)
                return;

            isAnimating = true;
            isOpen = true;
            if (boxCollider != null)
                boxCollider.enabled = false;

            KillTweens();
            transform.localPosition = stableLocalPosition;
            transform.localRotation = Quaternion.identity;
            onOpened?.Invoke(this);

            if (bottomRenderer != null)
                bottomRenderer.transform.localScale = bottomScale * 0.92f;

            bool playLid = lidRenderer != null && lidRenderer.gameObject.activeSelf;
            if (playLid)
            {
                lidRenderer.color = Color.white;
                lidRenderer.transform.localPosition = Vector3.zero;
                lidRenderer.transform.localRotation = Quaternion.identity;
                lidRenderer.transform.localScale = lidScale;
            }

            SetIceSmokeActive(true);
            if (playLid)
                PlayClip(AudioController.AudioClips != null ? AudioController.AudioClips.boxCoverOpen : null);

            mainSequence = DOTween.Sequence().SetTarget(this);
            if (bottomRenderer != null)
            {
                mainSequence.Join(bottomRenderer.transform
                    .DOScale(bottomScale, 0.22f)
                    .SetEase(Ease.OutBack));
            }

            if (playLid)
            {
                float referenceHeight = GetReferenceSpriteHeight();
                float lift = Mathf.Max(0.2f, referenceHeight * 0.42f);
                mainSequence.Join(lidRenderer.transform
                    .DOLocalMoveY(lift, 0.42f)
                    .SetEase(Ease.OutCubic));
                mainSequence.Join(lidRenderer.transform
                    .DORotate(new Vector3(0f, 0f, -14f), 0.42f)
                    .SetEase(Ease.OutCubic));
                mainSequence.Join(lidRenderer.transform
                    .DOScale(lidScale * 1.06f, 0.42f)
                    .SetEase(Ease.OutSine));
                mainSequence.Join(lidRenderer
                    .DOFade(0f, 0.24f)
                    .SetDelay(0.18f));
            }

            mainSequence.OnComplete(() =>
            {
                mainSequence = null;
                if (lidRenderer != null)
                    lidRenderer.gameObject.SetActive(false);
                PlayRemainingItems(targetWorldPosition, onItemCollected, onCompleted);
            });
        }

        float GetReferenceSpriteHeight()
        {
            Sprite reference = bottomRenderer != null && bottomRenderer.sprite != null
                ? bottomRenderer.sprite
                : lidRenderer != null ? lidRenderer.sprite : null;
            return reference != null ? reference.bounds.size.y : 1f;
        }

        void PlayRemainingItems(
            Func<Vector3> targetWorldPosition,
            Action<BoxCoverBehavior> onItemCollected,
            Action<BoxCoverBehavior> onCompleted)
        {
            int remaining = BoxCoverController.ItemsPerBox - collectedItemCount;
            if (remaining <= 0)
            {
                FinishCollection(onCompleted);
                return;
            }

            int completedFlights = 0;
            void OnItemArrived()
            {
                NotifyItemArrived(onItemCollected);
            }

            void OnFlightFinished()
            {
                completedFlights++;
                if (completedFlights >= remaining)
                    FinishCollection(onCompleted);
            }

            for (int itemIndex = collectedItemCount; itemIndex < BoxCoverController.ItemsPerBox; itemIndex++)
            {
                SpriteRenderer itemRenderer = itemIndex < contentItems.Count
                    ? contentItems[itemIndex]
                    : null;
                if (itemRenderer == null)
                {
                    collectedItemCount = Mathf.Min(
                        BoxCoverController.ItemsPerBox,
                        collectedItemCount + 1);
                    onItemCollected?.Invoke(this);
                    completedFlights++;
                    if (completedFlights >= remaining)
                        FinishCollection(onCompleted);
                    continue;
                }

                itemRenderer.gameObject.SetActive(true);
                itemRenderer.color = Color.white;
                float delay = (itemIndex - collectedItemCount) * 0.22f;
                RectTransform flyUi = BoxCoverManager.CreateHudFlyItem(itemRenderer);
                if (flyUi != null)
                {
                    itemRenderer.gameObject.SetActive(false);
                    PlayUiItemFlight(
                        flyUi,
                        itemRenderer.bounds.center,
                        delay,
                        OnItemArrived,
                        OnFlightFinished);
                    continue;
                }

                Vector3 target = targetWorldPosition != null
                    ? targetWorldPosition()
                    : transform.position + Vector3.up * 3f;

                Transform itemTransform = itemRenderer.transform;
                Vector3 startPos = itemTransform.position;
                flyingItems.Add(itemRenderer);
                BoxCoverFlySettings flySettings = BoxCoverManager.GetFlySettings();
                float flyDuration = flySettings.SampleDuration();
                BoxCoverManager.TryGetHudFlyTarget(out _, out _, out float targetRotationZ);
                Vector3 startEuler = itemTransform.eulerAngles;
                float jumpWorld = Mathf.Max(0.35f, localVisualSize.y * 0.35f);
                Sequence flight = DOTween.Sequence().SetTarget(this);
                flight.AppendInterval(flySettings.startDelay + delay);
                flight.Append(DOTween.To(
                        () => 0f,
                        t =>
                        {
                            float progress = flySettings.EvaluateMove(t);
                            Vector3 pos = Vector3.Lerp(startPos, target, progress);
                            pos.y += flySettings.EvaluateHeight(t) * jumpWorld;
                            itemTransform.position = pos;
                        },
                        1f,
                        flyDuration));
                flight.Join(itemTransform
                    .DORotate(new Vector3(startEuler.x, startEuler.y, targetRotationZ), flyDuration, RotateMode.Fast)
                    .SetEase(Ease.InOutSine));
                flight.Join(itemRenderer
                    .DOFade(0.15f, 0.18f)
                    .SetDelay(Mathf.Max(0f, flyDuration - CollectFeedbackLead)));
                bool impactNotified = false;
                void NotifyImpact()
                {
                    if (impactNotified)
                        return;
                    impactNotified = true;
                    NotifyItemArrived(onItemCollected);
                }

                flight.InsertCallback(
                    flySettings.startDelay + delay + Mathf.Max(0f, flyDuration - CollectFeedbackLead),
                    NotifyImpact);
                flight.OnComplete(() =>
                {
                    NotifyImpact();
                    flyingItems.Remove(itemRenderer);
                    if (itemRenderer != null)
                        itemRenderer.gameObject.SetActive(false);

                    OnFlightFinished();
                });
            }
        }

        void NotifyItemArrived(Action<BoxCoverBehavior> onItemCollected)
        {
            collectedItemCount = Mathf.Min(
                BoxCoverController.ItemsPerBox,
                collectedItemCount + 1);
            onItemCollected?.Invoke(this);
        }

        void PlayUiItemFlight(
            RectTransform flyUi,
            Vector3 startWorld,
            float delay,
            Action onImpact,
            Action onFlightComplete)
        {
            flyingUiItems.Add(flyUi);
            Vector2 start = flyUi.anchoredPosition;
            Vector2 startSize = flyUi.sizeDelta;
            Vector2 target = start;
            Vector2 targetSize = startSize * 0.35f;
            float targetRotationZ = flyUi.localEulerAngles.z;
            BoxCoverManager.TryGetHudFlyTarget(out target, out targetSize, out targetRotationZ);

            BoxCoverFlySettings flySettings = BoxCoverManager.GetFlySettings();
            float jumpPower = flySettings.arcHeight;
            if (jumpPower <= 0.01f)
            {
                float jumpWorld = Mathf.Max(0.35f, localVisualSize.y * 0.35f);
                if (BoxCoverManager.TryWorldToHudFlyLocal(startWorld + Vector3.up * jumpWorld, out Vector2 peakLocal))
                    jumpPower = Mathf.Max(40f, Mathf.Abs(peakLocal.y - start.y));
                else
                    jumpPower = 80f;
            }

            float flyDuration = flySettings.SampleDuration();
            Image flyImage = flyUi.GetComponent<Image>();
            Sequence flight = DOTween.Sequence().SetTarget(this);
            flight.AppendInterval(flySettings.startDelay + delay);
            flight.Append(DOTween.To(
                    () => 0f,
                    t =>
                    {
                        Vector2 liveTarget = target;
                        Vector2 liveSize = targetSize;
                        if (BoxCoverManager.TryGetHudFlyTarget(out Vector2 sampledTarget, out Vector2 sampledSize, out _))
                        {
                            liveTarget = sampledTarget;
                            liveSize = sampledSize;
                        }

                        float progress = flySettings.EvaluateMove(t);
                        float height = flySettings.EvaluateHeight(t) * jumpPower;
                        flyUi.anchoredPosition = new Vector2(
                            Mathf.Lerp(start.x, liveTarget.x, progress),
                            Mathf.Lerp(start.y, liveTarget.y, progress) + height);
                        flyUi.sizeDelta = Vector2.Lerp(startSize, liveSize, t * t);
                    },
                    1f,
                    flyDuration));
            flight.Join(flyUi
                .DOLocalRotate(new Vector3(0f, 0f, targetRotationZ), flyDuration, RotateMode.Fast)
                .SetEase(Ease.InOutSine));
            if (flyImage != null)
            {
                flight.Join(flyImage
                    .DOFade(0.15f, 0.18f)
                    .SetDelay(Mathf.Max(0f, flyDuration - CollectFeedbackLead)));
            }

            bool impactNotified = false;
            void NotifyImpact()
            {
                if (impactNotified)
                    return;
                impactNotified = true;
                onImpact?.Invoke();
            }

            flight.InsertCallback(
                flySettings.startDelay + delay + Mathf.Max(0f, flyDuration - CollectFeedbackLead),
                NotifyImpact);
            flight.OnComplete(() =>
            {
                NotifyImpact();
                flyingUiItems.Remove(flyUi);
                DestroyRuntimeObject(flyUi != null ? flyUi.gameObject : null);
                onFlightComplete?.Invoke();
            });
        }

        void FinishCollection(Action<BoxCoverBehavior> onCompleted)
        {
            isCollected = true;
            collectedItemCount = BoxCoverController.ItemsPerBox;
            SetIceSmokeActive(false);

            const float hideDuration = 0.21f;
            mainSequence = DOTween.Sequence().SetTarget(this);
            if (visuals != null)
            {
                SpriteRenderer[] renderers = visuals.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    SpriteRenderer renderer = renderers[i];
                    if (renderer == null || !renderer.gameObject.activeInHierarchy)
                        continue;

                    renderer.DOKill();
                    mainSequence.Join(renderer.DOFade(0f, hideDuration).SetEase(Ease.InQuad));
                }

                mainSequence.Join(visuals
                    .DOScale(visuals.localScale * 0.9f, hideDuration)
                    .SetEase(Ease.InSine));
            }

            mainSequence.OnComplete(() =>
            {
                mainSequence = null;
                isAnimating = false;
                gameObject.SetActive(false);
                onCompleted?.Invoke(this);
            });
        }

        public void PlayShake()
        {
            if (!gameObject.activeInHierarchy || isCollected)
                return;

            transform.DOKill();
            transform.localPosition = stableLocalPosition;
            transform.localRotation = Quaternion.identity;
            transform
                .DOShakePosition(0.36f, new Vector3(Mathf.Max(0.08f, localVisualSize.x * 0.035f), 0f, 0f), 12, 65f, false, true)
                .SetTarget(this)
                .OnComplete(() => transform.localPosition = stableLocalPosition);
        }

        public void OnObjectClicked(out bool isValid)
        {
            isValid = false;
            if (isCollected)
                return;

            GameDirector.RecordLastPlayTime(true);
            controller?.HandleBoxClicked(this);
            PlayShake();
            PlayClickSound();
        }

        void PlayClickSound()
        {
            PlayClip(
                AudioController.AudioClips != null ? AudioController.AudioClips.boxCoverClick : null,
                0.8f,
                UnityEngine.Random.Range(0.95f, 1.05f));
        }

        static void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null)
                return;

            AudioController.PlaySound(clip, volume, pitch);
        }

        public void OnObjectReleased()
        {
        }

        public void PlayLongPresssAnim()
        {
            PlayShake();
        }

        public void RecoverLongPressAnim(bool bResetTran = true)
        {
            if (!bResetTran)
                return;

            transform.DOKill();
            transform.localPosition = stableLocalPosition;
            transform.localRotation = Quaternion.identity;
        }

        public void Dispose()
        {
            KillTweens();
            // 立刻关掉碰撞，避免 Destroy 延后一帧时次关射线仍打到隐形盖子。
            if (boxCollider != null)
                boxCollider.enabled = false;
            if (gameObject != null)
            {
                gameObject.SetActive(false);
                DestroyRuntimeObject(gameObject);
            }
        }

        static void DestroyRuntimeObject(GameObject target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        void KillTweens()
        {
            mainSequence?.Kill();
            mainSequence = null;
            DOTween.Kill(this);
            transform.DOKill();
            if (visuals != null)
                visuals.DOKill();
            if (bottomRenderer != null)
            {
                bottomRenderer.DOKill();
                bottomRenderer.transform.DOKill();
            }
            if (lidRenderer != null)
            {
                lidRenderer.DOKill();
                lidRenderer.transform.DOKill();
            }
            for (int i = 0; i < flyingItems.Count; i++)
            {
                if (flyingItems[i] == null)
                    continue;
                flyingItems[i].DOKill();
                flyingItems[i].transform.DOKill();
            }
            for (int i = 0; i < flyingUiItems.Count; i++)
            {
                if (flyingUiItems[i] == null)
                    continue;
                flyingUiItems[i].DOKill();
                Image flyImage = flyingUiItems[i].GetComponent<Image>();
                flyImage?.DOKill();
                DestroyRuntimeObject(flyingUiItems[i].gameObject);
            }
            flyingUiItems.Clear();
            for (int i = 0; i < contentItems.Count; i++)
            {
                if (contentItems[i] == null)
                    continue;
                contentItems[i].DOKill();
                contentItems[i].transform.DOKill();
            }
        }

        void OnDestroy()
        {
            KillTweens();
            flyingItems.Clear();
            flyingUiItems.Clear();
            contentItems.Clear();
        }
    }
}
