using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace XSortGameCore
{
    public class Tween : MonoBehaviour
    {
        private static Tween _instance;
        private static TweenCaseCollection _activeCollection;
        private static bool _collectionEnabled;

        public void Init() { _instance = this; DontDestroyOnLoad(gameObject); }

        public static TweenClip DelayedCall(float delay, SimpleCallback callback, bool unscaledTime = false)
        {
            if (delay <= 0) { callback?.Invoke(); return null; }
            var tween = DOVirtual.DelayedCall(delay, () => callback?.Invoke(), unscaledTime);
            return Wrap(tween);
        }

        public static TweenClip DoFloat(float start, float end, float time, System.Action<float> callback, float delay = 0, bool unscaledTime = false)
        {
            float v = start;
            var tween = DOTween.To(() => v, x => { v = x; callback?.Invoke(x); }, end, time).SetDelay(delay).SetUpdate(UpdateType.Normal, unscaledTime);
            return Wrap(tween);
        }

        public static TweenClip NextFrame(SimpleCallback callback, int framesOffset = 1, bool unscaledTime = true)
        {
            var delay = (framesOffset + 1) * (1f / 60f);
            var tween = DOVirtual.DelayedCall(delay, () => callback?.Invoke(), unscaledTime);
            return Wrap(tween);
        }

        public static TweenCaseCollection BeginTweenCaseCollection()
        {
            _collectionEnabled = true;
            _activeCollection = new TweenCaseCollection();
            return _activeCollection;
        }

        public static void EndTweenCaseCollection() { _collectionEnabled = false; _activeCollection = null; }

        public static void AddToActiveCollectionIfEnabled(TweenClip tc)
        {
            if (_collectionEnabled && _activeCollection != null) _activeCollection.AddTween(tc);
        }

        public static TweenClip Wrap(DG.Tweening.Tween t)
        {
            var tc = new TweenClip();
            tc.Bind(t);
            AddToActiveCollectionIfEnabled(tc);
            return tc;
        }

        public static TweenClip DOPushScale(Transform target, Vector3 from, Vector3 to, float timeUp, float timeDown, DG.Tweening.Ease upEase, DG.Tweening.Ease downEase, float delay = 0)
        {
            var seq = DOTween.Sequence();
            seq.Append(target.DOScale(from, timeUp).SetEase(upEase).SetDelay(delay));
            seq.Append(target.DOScale(to, timeDown).SetEase(downEase));
            return Wrap(seq);
        }

        public static TweenClip DOPingPongScale(Transform target, float min, float max, float duration, DG.Tweening.Ease easeIn, DG.Tweening.Ease easeOut, bool unscaledTime = false)
        {
            var seq = DOTween.Sequence();
            seq.Append(target.DOScale(max, duration / 2).SetEase(easeIn));
            seq.Append(target.DOScale(min, duration / 2).SetEase(easeOut));
            seq.SetLoops(-1, LoopType.Yoyo);
            if (unscaledTime) seq.SetUpdate(true);
            return Wrap(seq);
        }

        public static Coroutine InvokeCoroutine(IEnumerator e) => _instance != null ? _instance.StartCoroutine(e) : null;
        public static void StopCustomCoroutine(Coroutine c) { if (_instance != null) _instance.StopCoroutine(c); }
    }
}
