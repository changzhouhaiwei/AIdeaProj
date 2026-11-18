#pragma warning disable 0618

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace TileGameCore
{
    public class FloatingTextUpgradesBehavior : FloatingTextBaseBehavior
    {
        [SerializeField] Transform containerTransform;
        [SerializeField] CanvasGroup containerCanvasGroup;
        [SerializeField] Image iconImage;
        [SerializeField] Text floatingText;

        [Space]
        [SerializeField] Vector3 offset;
        [SerializeField] float time;
        [SerializeField] Ease easing;

        [Space]
        [SerializeField] float scaleTime;
        [SerializeField] AnimationCurve scaleAnimationCurve;

        [Space]
        [SerializeField] float fadeTime;
        [SerializeField] Ease fadeEasing;

        private Transform targetTransform;
        private Vector3 targetOffset;
        private bool fixToTarget;

        private void LateUpdate()
        {
            if (fixToTarget)
                transform.position = targetTransform.position + targetOffset;
        }

        public void SetIconAndColor(Sprite icon, Color color)
        {
            iconImage.sprite = icon;
            iconImage.color = color;

            floatingText.color = color;
        }

        public override void Activate(string text, float scaleMultiplier, Color color)
        {
            fixToTarget = false;

            floatingText.text = text;
            floatingText.color = color;

            containerCanvasGroup.alpha = 1.0f;

            containerTransform.localScale = Vector3.zero;
            containerTransform.DOScale(Vector3.one * scaleMultiplier, scaleTime).SetEase(scaleAnimationCurve);

            containerCanvasGroup.DOFade(0.0f, fadeTime).SetEasing(fadeEasing);

            containerTransform.localPosition = Vector3.zero;
            var moveTc = Tween.Wrap(containerTransform.DOLocalMove(offset, time).SetEase(easing));
            moveTc.OnComplete(delegate
            {
                gameObject.SetActive(false);
                transform.SetParent(null);

                InvokeCompleteEvent();
            });
        }

        public void FixToTarget(Transform target, Vector3 offset)
        {
            fixToTarget = true;

            targetOffset = offset;
            targetTransform = target;
        }
    }
}