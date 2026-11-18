namespace FishTilePlay
{
    /// <summary>
    /// 可乐箱 / 裂变棋共用：无关卡号底板、目标栏预留。
    /// </summary>
    public static class SpecialPlayDock
    {
        public static bool PreviewNoLevelIdPlate;

        public static bool UsesNoLevelIdPlate =>
            PreviewNoLevelIdPlate
            || BoxCoverManager.HasActiveLevel
            || AwardColorEggCoverManager.HasActiveLevel
            || IQChallengeManager.HasActiveLevel;

        public static bool NeedsHudPlayfield =>
            PreviewNoLevelIdPlate
            || BoxCoverManager.HasActiveLevel
            || AwardColorEggCoverManager.HasActiveLevel
            || IQChallengeManager.HasActiveLevel;

        /// <summary>局内有目标栏：将满呼吸钉住 HUD，警告光走特殊底板那套。</summary>
        public static bool HasGoalHud =>
            BoxCoverManager.HasActiveLevel
            || AwardColorEggCoverManager.HasActiveLevel
            || IQChallengeManager.HasActiveLevel;

        public static void SyncPreview(LevelModel level)
        {
            PreviewNoLevelIdPlate = level != null && level.UsesSpecialDockPlate;
        }
    }
}
