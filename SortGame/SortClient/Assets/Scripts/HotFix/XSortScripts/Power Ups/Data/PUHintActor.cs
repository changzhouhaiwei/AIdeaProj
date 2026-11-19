using System.Collections.Generic;
using UnityEngine;
using XSortGameCore;
using System;

namespace FishXSortPlay
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

            if (!TryCollectHintTargets(out List<XSortActor> targetXSorts))
                return false;

            IsBusy = true;

            beforeUseFunc?.Invoke();
            LevelDirector.SetBusyState(true);

            scaleTweenCases = new TweenClip[targetXSorts.Count];
            for (int i = 0; i < scaleTweenCases.Length; i++)
            {
                XSortActor targetXSort = targetXSorts[i];

                targetXSort.MarkAsSubmitted(true);
                targetXSort.DisableEffect();
                targetXSort.SetIsClickable(true, false);
            }

            LevelDirector.SubmitElements(targetXSorts, true);

            disableTweenCase = Tween.DelayedCall(1.0f, () =>
            {
                IsBusy = false;

                LevelDirector.SetBusyState(false);
            });

            return true;
        }

        static bool TryCollectHintTargets(out List<XSortActor> targets)
        {
            targets = null;
            if (LevelDirector.LevelGenService == null)
                return false;

            int requiredElementsCount = 3;
            XSortModel XSortData = null;

            List<IDockable> dockXSorts = DockActor.GetHintSlots();
            if (!dockXSorts.IsNullOrEmpty())
            {
                XSortActor dockSample = dockXSorts[0] as XSortActor;
                XSortData = dockSample != null ? dockSample.XSortModel : null;
                requiredElementsCount -= dockXSorts.Count;
                if (XSortData == null || DockActor.GetSlotsAvailable() < requiredElementsCount)
                    return false;

                List<XSortActor> found = LevelDirector.GetXSortsMatching(dockSample, requiredElementsCount, true);
                if (found.Count < requiredElementsCount)
                    return false;

                targets = new List<XSortActor>(found);
                return true;
            }

            if (DockActor.GetSlotsAvailable() < requiredElementsCount)
                return false;

            List<XSortActor> activeXSorts = LevelDirector.GetActiveXSorts(true, true);
            if (activeXSorts.IsNullOrEmpty())
                return false;

            HashSet<int> triedTypes = new HashSet<int>();
            for (int n = activeXSorts.Count - 1; n > 0; n--)
            {
                int k = UnityEngine.Random.Range(0, n + 1);
                XSortActor swap = activeXSorts[k];
                activeXSorts[k] = activeXSorts[n];
                activeXSorts[n] = swap;
            }

            for (int i = 0; i < activeXSorts.Count; i++)
            {
                XSortActor sample = activeXSorts[i];
                XSortData = sample != null ? sample.XSortModel : null;
                if (XSortData == null || !triedTypes.Add(sample.MatchTypeId))
                    continue;

                List<XSortActor> found = LevelDirector.GetXSortsMatching(sample, requiredElementsCount, true);
                if (found.Count < requiredElementsCount)
                    continue;

                targets = new List<XSortActor>(found);
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
