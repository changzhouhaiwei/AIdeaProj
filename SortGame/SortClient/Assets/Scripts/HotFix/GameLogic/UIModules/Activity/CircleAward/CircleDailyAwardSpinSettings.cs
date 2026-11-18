using UnityEngine;

public class CircleDailyAwardSpinSettings : MonoBehaviour
{
    public const int DefaultExtraLaps = 3;
    public const float DefaultTrailPrevAlpha = 0.7f;
    public const float DefaultTrailPrevPrevAlpha = 0.4f;
    public const float DefaultWinHoldTime = 0.2f;
    public const float DefaultFallbackInterval = 0.05f;
    const float MinStepInterval = 0.01f;

    [Tooltip("额外整圈数，最终步数 = ExtraLaps * 格子数 + 到目标的最短步数")]
    [SerializeField, Min(0)] int extraLaps = DefaultExtraLaps;

    [Tooltip("X=总步长，Y=每步间隔（秒）。可在同一条曲线上画出加速、匀速、减速")]
    [SerializeField] AnimationCurve spinCurve = CreateDefaultSpinCurve();

    [Tooltip("上一格残影透明度（相对 Selected 原 alpha）")]
    [SerializeField, Range(0f, 1f)] float trailPrevAlpha = DefaultTrailPrevAlpha;

    [Tooltip("上上格残影透明度（相对 Selected 原 alpha）")]
    [SerializeField, Range(0f, 1f)] float trailPrevPrevAlpha = DefaultTrailPrevPrevAlpha;

    [Tooltip("停在中奖格后的停留时间（秒）")]
    [SerializeField, Min(0f)] float winHoldTime = DefaultWinHoldTime;

    static AnimationCurve sDefaultSpinCurve;

    public int ExtraLaps => Mathf.Max(0, extraLaps);
    public float TrailPrevAlpha => Mathf.Clamp01(trailPrevAlpha);
    public float TrailPrevPrevAlpha => Mathf.Clamp01(trailPrevPrevAlpha);
    public float WinHoldTime => Mathf.Max(0f, winHoldTime);

    public static AnimationCurve DefaultSpinCurve => sDefaultSpinCurve ??= CreateDefaultSpinCurve();

    public float GetStepInterval(int stepIndex, int steps)
    {
        return EvaluateStepInterval(spinCurve, stepIndex, steps);
    }

    public static float EvaluateDefaultStepInterval(int stepIndex, int steps)
    {
        return EvaluateStepInterval(DefaultSpinCurve, stepIndex, steps);
    }

    public static float EvaluateStepInterval(AnimationCurve curve, int stepIndex, int steps)
    {
        float length = GetCurveLength(curve);
        if (length <= 0f)
        {
            return DefaultFallbackInterval;
        }

        float x = steps <= 1 ? length : stepIndex / (float)(steps - 1) * length;
        x = Mathf.Clamp(x, 0f, length);
        float y = curve.Evaluate(curve[0].time + x);
        return Mathf.Max(MinStepInterval, y);
    }

    void Reset()
    {
        extraLaps = DefaultExtraLaps;
        spinCurve = CreateDefaultSpinCurve();
        trailPrevAlpha = DefaultTrailPrevAlpha;
        trailPrevPrevAlpha = DefaultTrailPrevPrevAlpha;
        winHoldTime = DefaultWinHoldTime;
    }

    static AnimationCurve CreateDefaultSpinCurve()
    {
        // 点击后立即匀速，后段再减速停稳
        var curve = new AnimationCurve(
            new Keyframe(0f, 0.05f, 0f, 0f),
            new Keyframe(18f, 0.05f, 0f, 0f),
            new Keyframe(26f, 0.28f, 0f, 0f));
        curve.preWrapMode = WrapMode.ClampForever;
        curve.postWrapMode = WrapMode.ClampForever;
        return curve;
    }

    static float GetCurveLength(AnimationCurve curve)
    {
        if (curve == null || curve.length < 2)
        {
            return 0f;
        }

        return Mathf.Max(0f, curve[curve.length - 1].time - curve[0].time);
    }
}
