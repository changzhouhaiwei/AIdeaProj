using DG.Tweening;
using UnityEngine;

namespace XSortGameCore
{
    /// <summary>
    /// TweenClip 包装 DG.Tweening.Tween，提供兼容 API
    /// </summary>
    public class TweenClip
    {
        protected DG.Tweening.Tween _tween;
        public bool IsActive { get; protected set; }
        public bool IsCompleted { get; protected set; }
        public float State => _tween != null ? _tween.Elapsed() / Mathf.Max(0.001f, _tween.Duration()) : 0;
        public GameObject ParentObject { get; protected set; }
        protected event SimpleCallback _onComplete;

        public TweenClip OnComplete(SimpleCallback callback)
        {
            _onComplete += callback;
            return this;
        }

        public virtual TweenClip Kill()
        {
            if (IsActive && _tween != null)
            {
                _tween.Kill();
                IsActive = false;
            }
            return this;
        }

        public virtual TweenClip Pause()
        {
            _tween?.Pause();
            return this;
        }

        public virtual TweenClip Resume()
        {
            _tween?.Play();
            return this;
        }

        public virtual TweenClip SetEasing(DG.Tweening.Ease ease)
        {
            _tween?.SetEase(ease);
            return this;
        }

        public virtual TweenClip SetCurveEasing(AnimationCurve curve)
        {
            _tween?.SetEase(curve);
            return this;
        }

        public virtual void Reset() { _tween?.Restart(); IsCompleted = false; }
        public void InvokeCompleteEvent() { _onComplete?.Invoke(); }
        public TweenClip StartTween() { _tween?.Play(); return this; }
        public TweenClip OnTimeReached(float time, SimpleCallback callback)
        {
            if (_tween == null) return this;
            bool fired = false;
            _tween.OnUpdate(() =>
            {
                if (fired) return;
                if (_tween.Elapsed() >= time) { fired = true; callback?.Invoke(); }
            });
            return this;
        }
        internal void Bind(DG.Tweening.Tween tween)
        {
            _tween = tween;
            IsActive = true;
            _tween.OnComplete(() => { IsCompleted = true; _onComplete?.Invoke(); });
            _tween.OnKill(() => IsActive = false);
        }
    }
}
