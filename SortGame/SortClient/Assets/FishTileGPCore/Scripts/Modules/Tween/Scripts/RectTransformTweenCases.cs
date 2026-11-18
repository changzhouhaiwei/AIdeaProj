using UnityEngine;
using DG.Tweening;

namespace TileGameCore
{
    public static class RectTransformTweenCases
    {
        public static TweenClip DOAnchoredPosition(this RectTransform target, Vector2 endValue, float duration, bool unscaledTime = false)
        {
            var t = DOTweenModuleUI.DOAnchorPos(target, endValue, duration);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        public static TweenClip DOAnchoredPosition(this RectTransform target, Vector3 endValue, float duration, bool unscaledTime = false)
        {
            var t = DOTweenModuleUI.DOAnchorPos3D(target, endValue, duration);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        public static TweenClip DOLocalMove(this RectTransform target, Vector3 endValue, float duration, bool unscaledTime = false)
        {
            var t = ShortcutExtensions.DOLocalMove(target, endValue, duration);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        public static TweenClip DOLocalMoveY(this RectTransform target, float endValue, float duration, bool unscaledTime = false)
        {
            var t = ShortcutExtensions.DOLocalMoveY(target, endValue, duration);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        public static TweenClip DOTScale(this RectTransform target, float endValue, float duration, bool unscaledTime = false)
        {
            var t = ShortcutExtensions.DOScale(target, endValue, duration);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        public static TweenClip DOTScale(this RectTransform target, Vector3 endValue, float duration, bool unscaledTime = false)
        {
            var t = ShortcutExtensions.DOScale(target, endValue, duration);
            if (unscaledTime) t.SetUpdate(true);
            return Wrap(t);
        }

        private static TweenClip Wrap(DG.Tweening.Tween tw)
        {
            var tc = new TweenClip();
            tc.Bind(tw);
            Tween.AddToActiveCollectionIfEnabled(tc);
            return tc;
        }
    }
}
