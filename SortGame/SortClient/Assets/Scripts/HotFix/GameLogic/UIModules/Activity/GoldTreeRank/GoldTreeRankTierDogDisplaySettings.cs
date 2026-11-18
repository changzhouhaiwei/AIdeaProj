using UnityEngine;

/// <summary>
/// 挂在 UIRankView / UIGoldTreeLeagueEnd 根节点，单独配置狗段位轮播切换时长。
/// </summary>
public class GoldTreeRankTierDogDisplaySettings : MonoBehaviour
{
    const float MinShiftDuration = 0.05f;

    [SerializeField]
    [Tooltip("狗段位切换一次（移动+缩放）的时长，单位秒")]
    float shiftDuration = GoldTreeRankTierDogDisplay.DefaultShiftDuration;

    public float ShiftDuration => Mathf.Max(MinShiftDuration, shiftDuration);

    void Reset()
    {
        shiftDuration = GoldTreeRankTierDogDisplay.DefaultShiftDuration;
    }
}
