using DG.Tweening;
using UnityEngine;

public static class ActionHelper
{
    /// <summary>
    /// 翻转
    /// </summary>
    public static void OverTurn(Transform target, float duration, float delay = 0f)
    {
        target.eulerAngles = new Vector3(0, 180, 0);
        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(delay);
        sequence.Append(target.DORotate(new Vector3(0, 0, 0), duration));
        sequence.SetUpdate(true);
    }
}