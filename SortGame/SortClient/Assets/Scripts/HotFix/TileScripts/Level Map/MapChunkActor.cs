using System.Collections.Generic;
using UnityEngine;
using TileGameCore;

namespace FishTilePlay.Map
{
    public class MapChunkActor : MonoBehaviour
    {
        [SerializeField] SpriteRenderer background;
        [SerializeField] GameObject bottom;

        [SerializeField] List<MapLevelActor> levels;
        public int LevelsCount => levels.Count;

        public int ChunkId { get; private set; }

        public MapActor Map { get; private set; }
        public float Height => background.size.y * transform.localScale.y;
        public float AdjustedHeight => Height / Map.MapVisibleRectHeight;

        private MapLevelActor FirstDisabledLevel { get; set; }
        public bool HasDisabledLevels => FirstDisabledLevel != null;
        public float FirstDisabledLevelPostion => Position + (FirstDisabledLevel.transform.localPosition.y + Height / 2) / Map.MapVisibleRectHeight;
        public float FirstDisabledLevelLocalPostion => (FirstDisabledLevel.transform.localPosition.y + Height / 2) / Map.MapVisibleRectHeight;

        public float CurrentLevelPosition { get; private set; }
        public float Position { get; private set; }
        public int StartLevelCount { get; private set; }

        

        public void SetPosition(float y)
        {
            Position = y;
            transform.SetPositionY(y * Map.MapVisibleRectHeight + Height / 2 + Camera.main.transform.position.y - Map.MapVisibleRectHeight / 2);
        }

        public void SetMap(MapActor map)
        {
            Map = map;
        }

        public void Init(int chunkId, int startLevelCount)
        {
            ChunkId = chunkId;
            StartLevelCount = startLevelCount;

            CurrentLevelPosition = -1;

            transform.localScale = Vector3.one * Map.MapVisibleRectWidth / background.size.x;

            bool reachedLastLevel = false;
            FirstDisabledLevel = null;

            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                var levelId = startLevelCount + i;

                if (!GameDirector.Data.InfiniteLevels && levelId >= LevelDirector.Database.AmountOfLevels)
                {
                    level.gameObject.SetActive(false);

                    if (!reachedLastLevel)
                    {
                        reachedLastLevel = true;
                        FirstDisabledLevel = level;
                    }
                } else
                {
                    level.Init(levelId);

                    if (levelId == MapActor.MaxLevelReached)
                    {
                        CurrentLevelPosition = (level.transform.position.y + Height / 2) / Map.MapVisibleRectHeight;
                    }
                }
            }

            background.receiveShadows = true;

            if(bottom != null) bottom.SetActive(startLevelCount == 0);
        }

        private void CalculateNarrowScreenScale()
        {
            transform.localScale = Vector3.one * Map.MapVisibleRectWidth / background.size.x;
        }

        private void CalculateWideScreenScale()
        {

        }
    }
}

