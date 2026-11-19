using DG.Tweening;
using XSortGameCore;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 打乱道具设置 - 配置打乱道具的动画参数和视觉效果
    /// </summary>
    [CreateAssetMenu(fileName = "PU Shuffle Settings", menuName = "Content/Power Ups/PU Shuffle Settings")]
    public class PUShuffleSettings : PUCustomSettings
    {
        [LineSpacer("Animation")]
        [SerializeField] float scaleTime = 0.5f; // 缩放动画持续时间
        public float ScaleTime => scaleTime;

        [SerializeField] float scaleMinDelay = 0.05f; // 最小延迟时间（棋子开始缩放的最小间隔）
        public float ScaleMinDelay => scaleMinDelay;

        [SerializeField] float scaleMaxDelay = 0.4f; // 最大延迟时间（棋子开始缩放的最大间隔）
        public float ScaleMaxDelay => scaleMaxDelay;

        [SerializeField] Ease scaleEasingType = Ease.OutBack; // 缩放动画的缓动类型
        public Ease ScaleEasingType => scaleEasingType;

        /// <summary>
        /// 初始化打乱设置
        /// </summary>
        public override void Init()
        {
            // 打乱道具的初始化逻辑（目前为空，可以在这里添加特定的初始化代码）
        }
    }
}
