using TileGameCore;


namespace FishTilePlay
{
    [System.Serializable]
    public class LevelArchive : ISaveObject
    {
        public int MaxReachedLevelIndex = 0;

        public int RealLevelIndex = 0;
        public int DisplayLevelIndex = 0;
        public bool IsPlayingRandomLevel = false;

        public int LastPlayerLevelIndex = -1;

        public void Flush()
        {

        }
    }
}
