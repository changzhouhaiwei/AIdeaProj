using System;
using DG.Tweening;
using XSortGameCore;
using UnityEngine;

namespace FishXSortPlay
{
    public sealed class AwardColorEggCoverBehavior : MonoBehaviour, ITappable
    {
        SpriteRenderer body;
        Transform visuals;
        BoxCollider boxCollider;
        AwardColorEggCoverPlacement placement;
        Vector2 footprint;
        int blockerCount = -1;
        int previousBlockerCount = -1;
        bool lastBlockerHintPlayed;
        const int CollectSortingOrder = 5000;
        const float CollectRevealDelay = 0.4f;
        const float CollectFlightDuration = 0.72f;
        const float CollectScaleDuration = 0.46f;
        bool collecting;
        bool collected;
        Sequence motion;
        Sequence wobble;

        public AwardColorEggCoverPlacement Placement => placement;
        public bool IsCollecting => collecting;
        public bool IsCollected => collected;
        public Vector2 Footprint => footprint;
        public BoxCollider BoxCollider => boxCollider;

        public bool ImageCovers(Vector3 worldPoint)
        {
            if (body == null || !body.enabled || collected || collecting)
                return false;
            return XSortStackUtil.SpriteCoversPoint(body, worldPoint);
        }

        public void Initialize(
            AwardColorEggCoverPlacement resolved,
            Vector3 localPosition,
            Vector2 localSize,
            int sortingOrder)
        {
            placement = resolved;
            footprint = localSize;
            collected = false;
            collecting = false;
            previousBlockerCount = -1;
            lastBlockerHintPlayed = false;
            EnsureParts();
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            FitBodyInstance(body.sprite, localSize);
            AlignWobblePivotToBottom();
            body.sortingOrder = sortingOrder;
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(
                Mathf.Max(0.01f, localSize.x * 0.95f),
                Mathf.Max(0.01f, localSize.y * 0.95f),
                0.18f);
            boxCollider.enabled = true;
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

        public void SetSpawnTweenVisualsActive(bool active)
        {
            if (body != null)
                body.enabled = active;
        }

        public void RefreshBlockers(int count, bool acceptCurrentWithoutHint = false)
        {
            if (collected || collecting)
                return;

            blockerCount = count;
            if (acceptCurrentWithoutHint)
                previousBlockerCount = count;

            bool transitionedToLastBlocker = count == 1 && previousBlockerCount != 1;
            previousBlockerCount = count;
            if (transitionedToLastBlocker && !lastBlockerHintPlayed)
            {
                lastBlockerHintPlayed = true;
                PlayTapWobble();
            }

            if (count <= 0 && AwardColorEggCoverManager.IsSpawnReady)
                AwardColorEggCoverManager.RequestCollect(this);
        }

        public void BeginCollect(Vector3 target, Action onDone)
        {
            if (collecting || collected)
                return;

            collecting = true;
            if (boxCollider != null)
                boxCollider.enabled = false;
            StopWobble();
            motion?.Kill();
            Vector3 start = transform.position;
            Vector3 end = target;
            float span = Vector3.Distance(start, end);
            float arc = Mathf.Max(1.15f, span * 0.32f);
            float side = end.x >= start.x ? Mathf.Max(0.7f, span * 0.18f) : -Mathf.Max(0.7f, span * 0.18f);
            Vector3 control = new Vector3(
                (start.x + end.x) * 0.5f + side,
                Mathf.Max(start.y, end.y) + arc,
                (start.z + end.z) * 0.5f);
            motion = DOTween.Sequence();
            motion.AppendInterval(CollectRevealDelay);
            motion.AppendCallback(RaiseForCollect);
            motion.Append(DOTween.To(
                () => 0f,
                value =>
                {
                    if (this == null)
                        return;
                    float remain = 1f - value;
                    transform.position = remain * remain * start
                        + 2f * remain * value * control
                        + value * value * end;
                },
                1f,
                CollectFlightDuration).SetEase(Ease.InOutSine));
            motion.Join(transform.DOScale(0.28f, CollectScaleDuration).SetEase(Ease.InQuad));
            motion.OnComplete(() =>
            {
                collected = true;
                collecting = false;
                onDone?.Invoke();
                Dispose();
            });
        }

        public void OnObjectClicked(out bool isValid)
        {
            isValid = false;
            if (collected || collecting || blockerCount <= 0)
                return;

            PlayTapWobble();
        }

        void PlayTapWobble()
        {
            Transform pivot = visuals != null ? visuals : transform;
            wobble?.Kill();
            pivot.localRotation = Quaternion.identity;
            const float tilt = 7.5f;
            wobble = DOTween.Sequence();
            wobble.Append(pivot.DOLocalRotate(new Vector3(0f, 0f, tilt), 0.09f).SetEase(Ease.OutSine));
            wobble.Append(pivot.DOLocalRotate(new Vector3(0f, 0f, -tilt), 0.12f).SetEase(Ease.InOutSine));
            wobble.Append(pivot.DOLocalRotate(new Vector3(0f, 0f, tilt * 0.55f), 0.1f).SetEase(Ease.InOutSine));
            wobble.Append(pivot.DOLocalRotate(new Vector3(0f, 0f, -tilt * 0.3f), 0.1f).SetEase(Ease.InOutSine));
            wobble.Append(pivot.DOLocalRotate(Vector3.zero, 0.1f).SetEase(Ease.OutSine));
            PlayShakeSound();
        }

        static void PlayShakeSound()
        {
            AudioClip clip = AudioController.AudioClips != null
                ? AudioController.AudioClips.colorEggShake
                : null;
            if (clip != null)
                AudioController.PlaySound(clip, 0.8f, UnityEngine.Random.Range(0.95f, 1.05f));
        }

        void StopWobble()
        {
            wobble?.Kill();
            wobble = null;
            if (visuals != null)
                visuals.localRotation = Quaternion.identity;
        }

        void AlignWobblePivotToBottom()
        {
            if (visuals == null || body == null || body.sprite == null)
                return;

            visuals.localRotation = Quaternion.identity;
            visuals.localPosition = Vector3.zero;
            body.transform.localPosition = Vector3.zero;
            Bounds spriteBounds = body.sprite.bounds;
            Vector3 bottomWorld = body.transform.TransformPoint(
                new Vector3(spriteBounds.center.x, spriteBounds.min.y, 0f));
            Vector3 bottomLocal = transform.InverseTransformPoint(bottomWorld);
            visuals.localPosition = new Vector3(bottomLocal.x, bottomLocal.y, 0f);
            body.transform.localPosition = new Vector3(-bottomLocal.x, -bottomLocal.y, 0f);
        }

        void RaiseForCollect()
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].sortingOrder = CollectSortingOrder + i;
            }
        }

        public void OnObjectReleased()
        {
        }

        public void PlayLongPresssAnim()
        {
        }

        public void RecoverLongPressAnim(bool bResetTran = true)
        {
        }

        public void Dispose()
        {
            motion?.Kill();
            wobble?.Kill();
            if (this != null)
                Destroy(gameObject);
        }

        void EnsureParts()
        {
            visuals = transform.Find("Visuals");
            if (visuals == null)
            {
                GameObject visualsObject = new GameObject("Visuals");
                visualsObject.transform.SetParent(transform, false);
                visuals = visualsObject.transform;
            }

            Transform bodyTransform = visuals.Find("Body");
            if (bodyTransform == null)
            {
                GameObject bodyObject = new GameObject("Body");
                bodyObject.transform.SetParent(visuals, false);
                bodyTransform = bodyObject.transform;
            }

            body = bodyTransform.GetComponent<SpriteRenderer>();
            if (body == null)
                body = bodyTransform.gameObject.AddComponent<SpriteRenderer>();

            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
                boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        void FitBodyInstance(Sprite sprite, Vector2 localSize)
        {
            if (sprite == null || body == null)
                return;

            Vector2 spriteSize = sprite.bounds.size;
            if (spriteSize.x < 0.001f || spriteSize.y < 0.001f)
                return;

            float width = Mathf.Max(0.01f, localSize.x);
            float height = Mathf.Max(0.01f, localSize.y);
            body.transform.localScale = new Vector3(
                width / spriteSize.x,
                height / spriteSize.y,
                1f);
            body.transform.localPosition = Vector3.zero;
        }

    }
}
