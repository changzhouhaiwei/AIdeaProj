using System.Collections.Generic;
using UnityEngine;
using TileGameCore;
using System;

namespace FishTilePlay
{
    public class PUHintActor : PUActor
    {
        private PUHintSettings customSettings;

        private TweenClip[] scaleTweenCases;
        private TweenClip disableTweenCase;

        public override void Init()
        {
            customSettings = (PUHintSettings)settings;
        }

        public override bool GetCanRelease()
        {
            return TryCollectHintTargets(out _);
        }

        public override bool Activate(Action beforeUseFunc = null)
        {
            if (LevelDirector.IsBusy)
                return false;

            if (!TryCollectHintTargets(out List<TileActor> targetTiles))
                return false;

            IsBusy = true;

            beforeUseFunc?.Invoke();
            LevelDirector.SetBusyState(true);

            scaleTweenCases = new TweenClip[targetTiles.Count];
            for (int i = 0; i < scaleTweenCases.Length; i++)
            {
                TileActor targetTile = targetTiles[i];

                targetTile.MarkAsSubmitted(true);
                targetTile.DisableEffect();
                targetTile.SetIsClickable(true, false);
            }

            LevelDirector.SubmitElements(targetTiles, true);

            disableTweenCase = Tween.DelayedCall(1.0f, () =>
            {
                IsBusy = false;

                LevelDirector.SetBusyState(false);
            });

            return true;
        }

        static bool TryCollectHintTargets(out List<TileActor> targets)
        {
            targets = null;
            if (LevelDirector.LevelGenService == null)
                return false;

            int requiredElementsCount = 3;
            TileModel tileData = null;

            List<IDockable> dockTiles = DockActor.GetHintSlots();
            if (!dockTiles.IsNullOrEmpty())
            {
                TileActor dockSample = dockTiles[0] as TileActor;
                tileData = dockSample != null ? dockSample.TileModel : null;
                requiredElementsCount -= dockTiles.Count;
                if (tileData == null || DockActor.GetSlotsAvailable() < requiredElementsCount)
                    return false;

                List<TileActor> found = LevelDirector.GetTilesMatching(dockSample, requiredElementsCount, true);
                if (found.Count < requiredElementsCount)
                    return false;

                targets = new List<TileActor>(found);
                return true;
            }

            if (DockActor.GetSlotsAvailable() < requiredElementsCount)
                return false;

            List<TileActor> activeTiles = LevelDirector.GetActiveTiles(true, true);
            if (activeTiles.IsNullOrEmpty())
                return false;

            HashSet<int> triedTypes = new HashSet<int>();
            for (int n = activeTiles.Count - 1; n > 0; n--)
            {
                int k = UnityEngine.Random.Range(0, n + 1);
                TileActor swap = activeTiles[k];
                activeTiles[k] = activeTiles[n];
                activeTiles[n] = swap;
            }

            for (int i = 0; i < activeTiles.Count; i++)
            {
                TileActor sample = activeTiles[i];
                tileData = sample != null ? sample.TileModel : null;
                if (tileData == null || !triedTypes.Add(sample.MatchTypeId))
                    continue;

                List<TileActor> found = LevelDirector.GetTilesMatching(sample, requiredElementsCount, true);
                if (found.Count < requiredElementsCount)
                    continue;

                targets = new List<TileActor>(found);
                return true;
            }

            return false;
        }

        public override void ResetBehavior()
        {
            if (!scaleTweenCases.IsNullOrEmpty())
            {
                for (int i = 0; i < scaleTweenCases.Length; i++)
                {
                    scaleTweenCases[i].KillActive();
                }
            }

            disableTweenCase.KillActive();

            IsBusy = false;
        }
    }
}
