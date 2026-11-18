using System;
using System.Collections.Generic;

namespace FishTilePlay
{
    /// <summary>
    /// HighLight 道具：将不可点击棋子置灰。
    /// 效果持续到 UnloadLevel（死亡重开）；中途退出再进由 RecoverLevelData 恢复；复活保持。
    /// 洗牌动画期间通过 IsTintSuppressed 暂时隐藏置灰，结束后恢复。
    /// </summary>
    public class PUHighLightBehavior : PUActor
    {
        public static bool IsActive { get; private set; }

        /// <summary>洗牌等动画期间为 true，暂时不显示置灰（IsActive 仍保持）。</summary>
        public static bool IsTintSuppressed { get; private set; }

        /// <summary>当前是否应对不可点棋子应用置灰。</summary>
        public static bool ShouldApplyBlockedTint => IsActive && !IsTintSuppressed;

        public override void Init()
        {
        }

        public override bool GetCanRelease()
        {
            if (IsActive)
                return false;

            List<TileActor> tiles = LevelDirector.LevelGenService?.ActTiles;
            return tiles != null && tiles.Count > 0;
        }

        public override bool Activate(Action beforeUseFunc = null)
        {
            if (LevelDirector.IsBusy || IsActive)
                return false;

            List<TileActor> tiles = LevelDirector.LevelGenService?.ActTiles;
            if (tiles == null || tiles.Count == 0)
                return false;

            beforeUseFunc?.Invoke();
            IsActive = true;
            IsTintSuppressed = false;
            LevelDirector.LevelGenService.UpdateClickableStates(true);
            RecoverLevelData.MarkUserProgress();
            RecoverLevelData.SaveCurrentLevelData();
            IsBusy = false;
            return true;
        }

        /// <summary>
        /// 洗牌等过程临时压制/恢复置灰显示，不改变 IsActive。
        /// </summary>
        public static void SetTintSuppressed(bool suppressed)
        {
            if (IsTintSuppressed == suppressed)
                return;

            IsTintSuppressed = suppressed;
            if (LevelDirector.LevelGenService != null)
            {
                LevelDirector.LevelGenService.UpdateClickableStates(false);
            }
        }

        /// <summary>
        /// 读档恢复激活态，不走 Activate，避免二次扣费。
        /// </summary>
        public static void RestoreFromSave(bool active)
        {
            IsActive = active;
            IsTintSuppressed = false;
            if (active && LevelDirector.LevelGenService != null)
            {
                LevelDirector.LevelGenService.UpdateClickableStates(true);
            }
        }

        public override void ResetBehavior()
        {
            IsBusy = false;
            IsTintSuppressed = false;
            if (!IsActive)
                return;

            IsActive = false;
            if (LevelDirector.LevelGenService != null)
            {
                LevelDirector.LevelGenService.UpdateClickableStates(false);
            }
        }
    }
}
