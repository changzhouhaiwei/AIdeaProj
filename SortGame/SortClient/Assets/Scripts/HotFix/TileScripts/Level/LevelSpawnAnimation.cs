using System.Collections;
using UnityEngine;
using TileGameCore;

namespace FishTilePlay
{
    public abstract class LevelSpawnAnimation : ScriptableObject
    {
        protected Coroutine animationCoroutine;

        public void Play(LevelGenService _levelGenFunc, SimpleCallback onAnimationCompleted)
        {
            Clear();

            animationCoroutine = Tween.InvokeCoroutine(SpawnLevelCoroutine(_levelGenFunc, onAnimationCompleted));
        }

        public virtual void Clear()
        {
            if (animationCoroutine != null)
                Tween.StopCustomCoroutine(animationCoroutine);
        }

        protected abstract IEnumerator SpawnLevelCoroutine(LevelGenService _levelGenFunc, SimpleCallback onAnimationCompleted);
    }
}