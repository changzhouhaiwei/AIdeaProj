using UnityEngine;
using TileGameCore;
using DG.Tweening;

namespace FishTilePlay
{
    public static class SlicedFilledImageExtensions
    {
        public static TweenClip DOFillAmount(this SlicedFilledImage tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false)
        {
            var t = DOTween.To(() => tweenObject.fillAmount, x => tweenObject.fillAmount = x, resultValue, time).SetDelay(delay);
            if (unscaledTime) t.SetUpdate(true);
            return TileGameCore.Tween.Wrap(t);
        }
    }
}
