using Unity.Mathematics;
using UnityEngine;

public class UIRankChangeDlgScrollSettings : MonoBehaviour
{
    const float DefaultScrollSpeed = 10f;
    const float MinScrollSpeed = 0.1f;

    [Tooltip("X=上升名次，Y=滚动速度（名次/秒）。滚动时长 = 上升名次 / 速度")]
    [SerializeField] AnimationCurve rankUpScrollSpeedCurve;

    public float GetScrollSpeed(int rankUpCount)
    {
        rankUpCount = math.max(1, rankUpCount);
        if (!IsValidCurve(rankUpScrollSpeedCurve))
            return DefaultScrollSpeed;

        float speed = rankUpScrollSpeedCurve.Evaluate(rankUpCount);
        return math.max(MinScrollSpeed, speed);
    }

    public float GetScrollDuration(int rankUpCount)
    {
        rankUpCount = math.max(1, rankUpCount);
        return rankUpCount / GetScrollSpeed(rankUpCount);
    }

    static bool IsValidCurve(AnimationCurve curve)
    {
        return curve != null && curve.length >= 2;
    }

    void Reset()
    {
        rankUpScrollSpeedCurve = CreateDefaultCurve();
    }

    static AnimationCurve CreateDefaultCurve()
    {
        return new AnimationCurve(
            new Keyframe(1f, 6f, 0f, 0f),
            new Keyframe(10f, 10f, 0f, 0f),
            new Keyframe(20f, 10f, 0f, 0f),
            new Keyframe(50f, 18f, 0f, 0f));
    }
}
