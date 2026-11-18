using DG.Tweening;

namespace TileGameCore
{
    /// <summary>
    /// TweenClip 子类，直接包装 DG.Tweening.Tween
    /// </summary>
    public class TweenCaseDOTween : TweenClip
    {
        public TweenCaseDOTween(DG.Tweening.Tween tween)
        {
            Bind(tween);
        }
    }
}
