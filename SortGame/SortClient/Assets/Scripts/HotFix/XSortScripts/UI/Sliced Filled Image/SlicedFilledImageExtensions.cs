using UnityEngine;
using XSortGameCore;
using DG.Tweening;

namespace FishXSortPlay
{
    public static class SlicedFilledImageExtensions
    {
        public static TweenClip DOFillAmount(this SlicedFilledImage tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false)
        {
            var t = DOTween.To(() => tweenObject.fillAmount, x => tweenObject.fillAmount = x, resultValue, time).SetDelay(delay);
            if (unscaledTime) t.SetUpdate(true);
            return XSortGameCore.Tween.Wrap(t);
        }
    }
}
