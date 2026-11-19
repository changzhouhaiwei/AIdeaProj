using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using XSortGameCore;


namespace FishXSortPlay
{
    [CreateAssetMenu(fileName = "Shuffle Spawn Animation", menuName = "Content/Level/Shuffle Spawn Animation")]
    public class ShuffleSpawnAnimation : LevelSpawnAnimation
    {
        [SerializeField] float scaleTime = 0.2f;
        [SerializeField] Ease scaleEasing = Ease.OutBack;

        [Space]
        [SerializeField] float minDelay = 0.05f;
        [SerializeField] float maxDelay = 0.2f;

        private OptimisedXSortsScaleTweenClip optimisedShuffleTweenCase;

        protected override IEnumerator SpawnLevelCoroutine(LevelGenService _levelGenFunc, SimpleCallback onAnimationCompleted)
        {
            // Reset objects
            List<XSortActor> XSorts = _levelGenFunc.GetSortedXSorts();

            optimisedShuffleTweenCase = new OptimisedXSortsScaleTweenClip(XSorts, scaleTime, minDelay, maxDelay, scaleEasing);
            optimisedShuffleTweenCase.OnComplete(() =>
            {
                BoxCoverManager.CompleteSpawnAnimation();
                onAnimationCompleted?.Invoke();
            });
            optimisedShuffleTweenCase.StartTween();

            foreach (XSortActor XSort in XSorts)
            {
                XSort.SetIsClickable(_levelGenFunc.IsXSortUnconcealed(XSort), true);

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