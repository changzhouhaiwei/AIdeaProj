using System.Collections;
using DG.Tweening;
using UnityEngine;
using TileGameCore;

namespace FishTilePlay
{
    [CreateAssetMenu(fileName = "Simple Spawn Animation", menuName = "Content/Level/Simple Spawn Animation")]
    public class SimpleSpawnAnimation : LevelSpawnAnimation
    {
        [SerializeField] float scaleTime = 0.35f;
        [SerializeField] Ease scaleEasing = Ease.OutBack;

        [Space]
        [SerializeField] float elementDelay = 0f;
        [SerializeField] float layerDelay = 0.1f;
        
        private OptimisedTilesSimpleScaleTweenClip optimisedScaleTweenCase;

        protected override IEnumerator SpawnLevelCoroutine(LevelGenService LevelGenService, SimpleCallback onAnimationCompleted)
        {
            optimisedScaleTweenCase = new OptimisedTilesSimpleScaleTweenClip(LevelGenService, scaleTime, elementDelay, layerDelay, scaleEasing);
            optimisedScaleTweenCase.OnComplete(() => onAnimationCompleted?.Invoke());
            optimisedScaleTweenCase.StartTween();

            yield return null;
        }

        public override void Clear()
        {
            base.Clear();

            if (optimisedScaleTweenCase != null)
                optimisedScaleTweenCase.Kill();
        }
    }
}