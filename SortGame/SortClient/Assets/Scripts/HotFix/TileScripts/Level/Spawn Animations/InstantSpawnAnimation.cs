using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishTilePlay
{
    [CreateAssetMenu(fileName = "Instant Spawn Animation", menuName = "Content/Level/Instant Spawn Animation")]
    public class InstantSpawnAnimation : LevelSpawnAnimation
    {
        protected override IEnumerator SpawnLevelCoroutine(LevelGenService _levelGenFunc, SimpleCallback onAnimationCompleted)
        {
            List<TileActor> tiles = _levelGenFunc.ActTiles;
            foreach(TileActor tile in tiles)
            {
                tile.transform.localScale = Vector3.one;
                tile.SetSpawnTweenVisualsActive(true);
            }

            BoxCoverManager.CompleteSpawnAnimation();

            yield return null;

            onAnimationCompleted?.Invoke();
        }
    }
}