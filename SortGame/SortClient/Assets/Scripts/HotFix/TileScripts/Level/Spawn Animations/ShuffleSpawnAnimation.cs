using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TileGameCore;


namespace FishTilePlay
{
    [CreateAssetMenu(fileName = "Shuffle Spawn Animation", menuName = "Content/Level/Shuffle Spawn Animation")]
    public class ShuffleSpawnAnimation : LevelSpawnAnimation
    {
        [SerializeField] float scaleTime = 0.2f;
        [SerializeField] Ease scaleEasing = Ease.OutBack;

        [Space]
        [SerializeField] float minDelay = 0.05f;
        [SerializeField] float maxDelay = 0.2f;

        private OptimisedTilesScaleTweenClip optimisedShuffleTweenCase;

        protected override IEnumerator SpawnLevelCoroutine(LevelGenService _levelGenFunc, SimpleCallback onAnimationCompleted)
        {
            // Reset objects
            List<TileActor> tiles = _levelGenFunc.GetSortedTiles();

            optimisedShuffleTweenCase = new OptimisedTilesScaleTweenClip(tiles, scaleTime, minDelay, maxDelay, scaleEasing);
            optimisedShuffleTweenCase.OnComplete(() =>
            {
                BoxCoverManager.CompleteSpawnAnimation();
                onAnimationCompleted?.Invoke();
            });
            optimisedShuffleTweenCase.StartTween();

            foreach (TileActor tile in tiles)
            {
                tile.SetIsClickable(_levelGenFunc.IsTileUnconcealed(tile), true);

                yield return null;
            }
        }

        public override void Clear()
        {
            base.Clear();

            optimisedShuffleTweenCase.KillActive();
        }
    }
}