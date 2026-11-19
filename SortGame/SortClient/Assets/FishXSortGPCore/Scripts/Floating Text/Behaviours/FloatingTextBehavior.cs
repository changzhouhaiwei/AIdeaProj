#pragma warning disable 0618

using UnityEngine;
using DG.Tweening;

namespace XSortGameCore
{
    public class FloatingTextBehavior : FloatingTextBaseBehavior
    {
        [Space]
        [SerializeField] Vector3 offset;
        [SerializeField] float time;
        [SerializeField] Ease easing;

        [Space]
        [SerializeField] float scaleTime;
        [SerializeField] AnimationCurve scaleAnimationCurve;

        private Vector3 defaultScale;

        private TweenClip scaleTween;
        private TweenClip moveTween;

        private void Awake()
        {
            defaultScale = transform.localScale;
        }

        public override void Activate(string text, float scaleMultiplier, Color color)
        {
            textRef.text = text;
            textRef.color = color;

            transform.localScale = Vector3.zero;
            scaleTween = Tween.Wrap(transform.DOScale(defaultScale * scaleMultiplier, scaleTime).SetEase(scaleAnimationCurve));
            moveTween = Tween.Wrap(transform.DOMove(transform.position + offset, time).SetEase(easing));
            moveTween.OnComplete(delegate
             {
                 gameObject.SetActive(false);

                 InvokeCompleteEvent();
             });
        }

        public void AddOnTimeReached(float time, SimpleCallback callback)
        {
            if (moveTween.ExistsAndActive())
            {
                moveTween.OnTimeReached(time, callback);
            }
        }

        public void SetText(string text)
        {
            textRef.text = text;
        }

        public void Reset()
        {
            scaleTween.KillActive();
            moveTween.KillActive();
        }
    }
}