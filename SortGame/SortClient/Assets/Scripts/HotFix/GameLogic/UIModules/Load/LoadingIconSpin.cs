using DG.Tweening;
using UnityEngine;

/// <summary>
/// 用 unscaledTime 驱动 loading 图标绝对角度，多实例叠层时相位一致。
/// </summary>
public static class LoadingIconSpin
{
    private const float DegreesPerSecond = 360f;

    public static Tween Start(Transform icon)
    {
        if (icon == null)
        {
            return null;
        }

        ApplyAngle(icon);
        return DOTween.To(() => 0f, _ => ApplyAngle(icon), 1f, 1f)
            .SetEase(Ease.Linear)
            .SetLoops(-1)
            .SetUpdate(true)
            .SetTarget(icon);
    }

    public static void Stop(ref Tween tween)
    {
        tween?.Kill();
        tween = null;
    }

    private static void ApplyAngle(Transform icon)
    {
        float z = -(Time.unscaledTime * DegreesPerSecond) % 360f;
        icon.localEulerAngles = new Vector3(0f, 0f, z);
    }
}
