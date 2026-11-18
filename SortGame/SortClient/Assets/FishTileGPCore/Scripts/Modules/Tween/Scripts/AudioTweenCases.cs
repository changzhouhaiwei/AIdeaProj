using UnityEngine;
using DG.Tweening;

namespace TileGameCore
{
    public static class AudioTweenCases
    {
        public static TweenClip DOVolume(this AudioSource target, float endValue, float duration, bool unscaledTime = false)
        {
            var t = DOTweenModuleAudio.DOFade(target, endValue, duration);
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
