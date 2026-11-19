using System;
using UnityEngine;

namespace FishXSortPlay
{
    [Serializable]
    public sealed class BoxCoverFlySettings
    {
        [Min(0.05f)]
        [Tooltip("飞行时长下限")]
        public float durationMin = 0.4f;

        [Min(0.05f)]
        [Tooltip("飞行时长上限，每次起飞在此区间随机")]
        public float durationMax = 0.65f;

        [Min(0f)]
        [Tooltip("起飞前额外等待")]
        public float startDelay = 0.12f;

        [Tooltip("位移进度。横轴时间 0-1，纵轴 0-1 为从起点到终点。")]
        public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("抛物线高度。横轴时间 0-1，纵轴 1 为最高点。")]
        public AnimationCurve heightCurve;

        [Min(0f)]
        [Tooltip("最高点高度（UI 像素）")]
        public float arcHeight = 80f;

        [Min(0f)]
        [Tooltip("旋转角度下限（度）")]
        public float rotationMin = 270f;

        [Min(0f)]
        [Tooltip("旋转角度上限（度），每次起飞随机，方向也随机")]
        public float rotationMax = 450f;

        public static readonly BoxCoverFlySettings Default = new BoxCoverFlySettings();

        public BoxCoverFlySettings()
        {
            heightCurve = CreateDefaultHeightCurve();
        }

        public void EnsureDefaults()
        {
            if (durationMin < 0.05f)
                durationMin = 0.4f;
            if (durationMax < 0.05f)
                durationMax = 0.65f;
            if (durationMax < durationMin)
                durationMax = durationMin;
            if (startDelay < 0f)
                startDelay = 0f;
            if (arcHeight < 0f)
                arcHeight = 80f;
            if (rotationMin < 0f)
                rotationMin = 0f;
            if (rotationMax < 0f)
                rotationMax = 0f;
            if (rotationMax < rotationMin)
                rotationMax = rotationMin;
            if (moveCurve == null || moveCurve.length == 0)
                moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            if (heightCurve == null || heightCurve.length == 0)
                heightCurve = CreateDefaultHeightCurve();
        }

        public float SampleDuration()
        {
            EnsureDefaults();
            return UnityEngine.Random.Range(durationMin, durationMax);
        }

        public float SampleRotationZ()
        {
            EnsureDefaults();
            float angle = UnityEngine.Random.Range(rotationMin, rotationMax);
            return UnityEngine.Random.value < 0.5f ? -angle : angle;
        }

        public float EvaluateMove(float t)
        {
            EnsureDefaults();
            return Mathf.Clamp01(moveCurve.Evaluate(Mathf.Clamp01(t)));
        }

        public float EvaluateHeight(float t)
        {
            EnsureDefaults();
            return heightCurve.Evaluate(Mathf.Clamp01(t));
        }

        static AnimationCurve CreateDefaultHeightCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 4f),
                new Keyframe(0.5f, 1f, 0f, 0f),
                new Keyframe(1f, 0f, -4f, 0f));
        }
    }
}
