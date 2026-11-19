using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace XSortGameCore
{
    public static class GraphicTweenCases
    {
        public static TweenClip DOFade(this CanvasGroup target, float endValue, float duration, bool unscaledTime = false)
        {
            var t = DOTweenModuleUI.DOFade(target, endValue, duration);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        public static TweenClip DOFade(this CanvasGroup target, float endValue, float duration, float delay, bool unscaledTime = false)
        {
            var t = DOTweenModuleUI.DOFade(target, endValue, duration).SetDelay(delay);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        public static TweenClip DOFade(this Graphic target, float endValue, float duration, bool unscaledTime = false)
        {
            var t = DOTweenModuleUI.DOFade(target, endValue, duration);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        public static TweenClip DOColor(this Graphic target, Color endValue, float duration, bool unscaledTime = false)
        {
            var t = DOTweenModuleUI.DOColor(target, endValue, duration);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        public static TweenClip DOPreferredHeight(this LayoutElement target, float endValue, float duration, bool unscaledTime = false)
        {
            var t = DOTween.To(() => target.preferredHeight, x => target.preferredHeight = x, endValue, duration);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        private static TweenClip Wrap(DG.Tweening.Tween t)
        {
            var tc = new TweenClip();
            tc.Bind(t);
            Tween.AddToActiveCollectionIfEnabled(tc);
            return tc;
        }
    }
}
