namespace FishTilePlay
{
    /// <summary>
    /// 关卡预跑期间关掉 UI/动画/存档。整批预跑期间保持 IsRunning。
    /// </summary>
    public static class LevelDifficultySim
    {
        public static bool IsRunning { get; private set; }
        public static bool HasFinished { get; private set; }
        public static bool LastWon { get; private set; }

        /// <summary>预跑或按种子还原：不打开游戏 UI、不播动效。</summary>
        public static bool SkipPresentation => IsRunning || LevelDifficultySeed.IsReplayLoad;

        public static void BeginBatch()
        {
            IsRunning = true;
            HasFinished = false;
            LastWon = false;
        }

        public static void EndBatch()
        {
            IsRunning = false;
            HasFinished = false;
            LastWon = false;
        }

        public static void BeginGame()
        {
            HasFinished = false;
            LastWon = false;
        }

        public static void NotifyFinished(bool won)
        {
            if (HasFinished)
                return;

            HasFinished = true;
            LastWon = won;
        }
    }
}
