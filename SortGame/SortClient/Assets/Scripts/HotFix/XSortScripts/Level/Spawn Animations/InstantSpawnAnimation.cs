using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    [CreateAssetMenu(fileName = "Instant Spawn Animation", menuName = "Content/Level/Instant Spawn Animation")]
    public class InstantSpawnAnimation : LevelSpawnAnimation
    {
        protected override IEnumerator SpawnLevelCoroutine(LevelGenService _levelGenFunc, SimpleCallback onAnimationCompleted)
        {
            List<XSortActor> XSorts = _levelGenFunc.ActXSorts;
            foreach(XSortActor XSort in XSorts)
            {
                XSort.transform.localScale = Vector3.one;
                XSort.SetSpawnTweenVisualsActive(true);
            }

            BoxCoverManager.CompleteSpawnAnimation();

            yield return null;

            onAnimationCompleted?.Invoke();
        }
    }
}