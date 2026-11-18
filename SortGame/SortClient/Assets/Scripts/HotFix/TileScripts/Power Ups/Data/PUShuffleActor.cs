using UnityEngine;
using System.Collections.Generic;
using TileGameCore;
using System;

namespace FishTilePlay
{
    /// <summary>
    /// 打乱道具行为组件 - 实现棋盘打乱功能
    /// </summary>
    // Attempt 1 - https://pastebin.com/SKsuyMnk
    public class PUShuffleActor : PUActor
    {
        private PUShuffleSettings customSettings; // 打乱道具的自定义设置

        private TweenClip delayTweenCase; // 延迟动画案例

        /// <summary>
        /// 初始化打乱道具
        /// </summary>
        public override void Init()
        {
            // 将通用设置转换为打乱专用设置
            customSettings = (PUShuffleSettings)settings;
        }

        /// <summary>
        /// 激活打乱道具 - 执行棋盘打乱逻辑
        /// </summary>
        /// <returns>打乱是否成功执行</returns>
        public override bool Activate(Action beforeUseFunc)
        {
            // 检查关卡控制器是否忙碌（防止重复操作）
            if (!LevelDirector.IsBusy)
            {
                LevelGenService LevelGenService = LevelDirector.LevelGenService;

                // 获取所有活跃的棋子
                List<TileActor> activeTiles = LevelGenService.ActTiles;
                if (activeTiles != null)
                {
                    // 确保至少有2个棋子才能打乱
                    if (activeTiles.Count > 1)
                    {
                        // 创建可打乱棋子的列表（复制所有活跃棋子）
                        List<TileActor> allowedToShuffleTiles = new (activeTiles);

                        // 从后往前遍历，移除不允许打乱的棋子
                        for (int i = allowedToShuffleTiles.Count - 1; i >= 0; i--)
                        {
                            // 检查棋子是否允许打乱（比如某些特殊棋子可能不允许打乱）
                            if (!allowedToShuffleTiles[i].IsShuffleAllowed())
                            {
                                allowedToShuffleTiles.RemoveAt(i);
                            }
                        }

                        // 确保过滤后仍有至少2个棋子可以打乱
                        if (allowedToShuffleTiles.Count > 1)
                        {
                            beforeUseFunc?.Invoke();
                            // 设置道具为忙碌状态
                            IsBusy = true;

                            // 设置关卡为忙碌状态（防止其他操作干扰）
                            LevelDirector.SetBusyState(true);

                            // 禁用射线检测（防止点击干扰）
                            RaycastController.Disable();

                            // 洗牌动画期间暂时隐藏 HighLight 置灰，结束后再恢复
                            PUHighLightBehavior.SetTintSuppressed(true);

                            // 创建位置数组，用于存储打乱后的位置
                            ElementPosition[] shuffleElements = new ElementPosition[allowedToShuffleTiles.Count];
                            for (int i = 0; i < shuffleElements.Length; i++)
                            {
                                // 收集所有可打乱棋子的当前位置
                                shuffleElements[i] = allowedToShuffleTiles[i].ElementPosition;
                            }

                            // 随机打乱位置数组
                            shuffleElements.RandomizeOrder();

                            // 重置所有棋子的缩放为0（准备播放消失动画）
                            for (int i = 0; i < activeTiles.Count; i++)
                            {
                                activeTiles[i].transform.localScale = Vector3.zero;
                                activeTiles[i].SetSpawnTweenVisualsActive(false);
                            }

                            // 重新定位打乱后的棋子
                            var layersShuffle = LevelGenService.Layers;
                            for (int i = 0; i < shuffleElements.Length; i++)
                            {
                                ElementPosition elementPosition = shuffleElements[i];
                                Transform layerT = layersShuffle[elementPosition.LayerId].LayerObject.transform;
                                TileActor tb = allowedToShuffleTiles[i];

                                tb.transform.SetParent(layerT);
                                tb.transform.localPosition = LevelFitter.GetPosition(shuffleElements[i]);
                                tb.transform.localScale = Vector3.zero;
                                tb.SetPosition(shuffleElements[i]);
                                tb.transform.name = $"after_l:{elementPosition.LayerId}_x:{elementPosition.X}_y:{elementPosition.Y}";
                            }

                            // 重新链接棋子之间的关系（更新邻居引用等）
                            // 必须在动画开始前调用，确保层级信息正确更新
                            for (int i = 0; i < activeTiles.Count; i++)
                            {
                                activeTiles[i].InvalidateSpawnTweenVisualCache();
                            }

                            LevelGenService.RelinkTiles(false);
                            LevelGenService.BeginGoldenTreeCornerRecalculation();

                            // 创建优化的棋子缩放动画案例（使用 DOTween）
                            OptimisedTilesScaleTweenClip optimisedShuffleTweenCase = new OptimisedTilesScaleTweenClip(
                                activeTiles, 
                                customSettings.ScaleTime, 
                                customSettings.ScaleMinDelay, 
                                customSettings.ScaleMaxDelay,
                                customSettings.ScaleEasingType
                            );
                            
                            // 设置动画完成后的回调
                            optimisedShuffleTweenCase.OnComplete(() =>
                            {
                                // 重新启用射线检测
                                RaycastController.Enable();

                                // 设置道具为非忙碌状态
                                IsBusy = false;

                                // 设置关卡为非忙碌状态
                                LevelDirector.SetBusyState(false);

                                LevelGenService.FinalizeGoldenTreeCornerDecisions();
                                // 恢复 HighLight 置灰显示，并确保洗牌动画关掉的 Renderer 全部还原
                                PUHighLightBehavior.SetTintSuppressed(false);
                                for (int i = 0; i < activeTiles.Count; i++)
                                {
                                    if (activeTiles[i] != null)
                                        activeTiles[i].SetSpawnTweenVisualsActive(true);
                                }
                                LevelGenService.UpdateClickableStates();
                            });

                            // 开始播放打乱动画
                            optimisedShuffleTweenCase.StartTween();

                            return true; // 打乱成功
                        }
                        // 如果可打乱的棋子不足2个，打乱失败
                    }
                    // 如果总棋子数不足2个，打乱失败
                }
                // 如果没有活跃棋子，打乱失败
            }
            // 如果关卡正在忙碌，打乱失败

            return false; // 打乱失败
        }

        /// <summary>
        /// 重置打乱道具的行为状态
        /// </summary>
        public override void ResetBehavior()
        {
            // 设置道具为非忙碌状态
            IsBusy = false;

            // 停止延迟动画
            delayTweenCase.KillActive();
        }
    }
}
