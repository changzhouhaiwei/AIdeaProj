using UnityEngine;

/// <summary>转盘 Wheel Spine 播放参数，与转动节奏（SpinSettings）分离。</summary>
public class CircleDailyAwardWheelSpineSettings : MonoBehaviour
{
    public const float DefaultSpineIntenseDelay = 0.5f;

    [Tooltip("开始转动后，延迟多久播放 DJ_Intense（秒）")]
    [SerializeField, Min(0f)] float spineIntenseDelay = DefaultSpineIntenseDelay;

    public float SpineIntenseDelay => Mathf.Max(0f, spineIntenseDelay);

    void Reset()
    {
        spineIntenseDelay = DefaultSpineIntenseDelay;
    }
}
