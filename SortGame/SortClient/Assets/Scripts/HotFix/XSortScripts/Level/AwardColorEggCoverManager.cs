using System;
using System.Collections.Generic;
using DG.Tweening;
using XSortGameCore;
using UnityEngine;

namespace FishXSortPlay
{
    public static class AwardColorEggCoverManager
    {
        static AwardColorEggCoverController controller;
        static DG.Tweening.Tween victoryDelay;
        static bool spawnReady;
        static bool acceptRestoredBlockersWithoutHint;
        static bool victorySequenceStarted;
        static bool victoryReady;
        static bool victoryCommitted;
        static bool lotteryOpen;

        public static bool HasActiveLevel => controller != null && controller.TotalCount > 0;
        public static bool IsSpawnReady => spawnReady;
        public static bool AreAllCollected => HasActiveLevel && controller.AllCollected;
        public static bool IsVictorySequenceStarted => victorySequenceStarted;
        public static int CollectedCount => controller?.CollectedCount ?? 0;
        public static int TotalCount => controller?.TotalCount ?? 0;

        public static void Load(
            LevelModel level,
            GameObject layersParent,
            RecoverLevelData recoveryData = null)
        {
            ClearRuntime();
            spawnReady = false;
            if (level == null
                || layersParent == null
                || LevelDirector.BInCoinRewardLevel
                || !AwardColorEggCoverConfig.IsEnabledForLoadedLevel())
            {
                return;
            }

            bool restoreExact = recoveryData != null
                && recoveryData.LevelIndex == LevelDirector.LoadedLevelIndex
                && RecoverLevelData.ShouldRestoreBoxCoverLayout(recoveryData)
                && recoveryData.AwardColorEggCoverStates != null
                && recoveryData.AwardColorEggCoverStates.Count > 0;
            bool reroll = !restoreExact
                && recoveryData != null
                && recoveryData.LevelIndex == LevelDirector.LoadedLevelIndex
                && recoveryData.AwardColorEggCoverStates != null
                && recoveryData.AwardColorEggCoverStates.Count > 0;

            int seed = CreateSeed();
            List<AwardColorEggCoverPlacement> placements;
            if (restoreExact)
                placements = SavedPlacements(recoveryData.AwardColorEggCoverStates);
            else if (reroll)
                placements = AwardColorEggCoverResolver.RerollOnLayers(
                    level,
                    seed,
                    SavedPlacements(recoveryData.AwardColorEggCoverStates));
            else
                placements = AwardColorEggCoverResolver.Resolve(level, seed);

            if (placements == null || placements.Count != AwardColorEggCoverConfig.EggCount)
            {
                Debug.LogWarning("[AwardColorEggCover] Cannot place 3 eggs. This level is not treated as an egg level.");
                return;
            }

            controller = new AwardColorEggCoverController(
                level,
                layersParent.transform,
                placements,
                restoreExact ? recoveryData.AwardColorEggCoverStates : null);
            acceptRestoredBlockersWithoutHint = restoreExact;
            RefreshHud(false);
        }

        public static void NotifyLevelSpawnCompleted()
        {
            if (!HasActiveLevel)
                return;
            AwardColorEggCoverPending.ResetMainStageSent();
            spawnReady = true;
            controller.CompleteSpawnAnimation();
            RefreshHud(false);
            UpdateStates();
        }

        public static void UpdateStates()
        {
            if (!HasActiveLevel || !spawnReady || victorySequenceStarted)
                return;

            bool acceptWithoutHint = acceptRestoredBlockersWithoutHint;
            acceptRestoredBlockersWithoutHint = false;
            controller.UpdateStates(acceptWithoutHint);
            if (controller.AllCollected)
                BeginVictorySequence();
        }

        public static bool IsXSortBlocked(XSortActor XSort)
        {
            return HasActiveLevel && controller.IsXSortBlocked(XSort);
        }

        public static bool IsClickBlocked(XSortActor XSort, Vector3 worldPoint)
        {
            return HasActiveLevel && controller.IsClickBlocked(XSort, worldPoint);
        }

        public static bool TryHandleBlockedClick(Vector3 worldPoint, XSortActor resolvedXSort = null)
        {
            return HasActiveLevel && controller.TryHandleBlockedClick(worldPoint, resolvedXSort);
        }

        public static void RequestCollect(AwardColorEggCoverBehavior behavior)
        {
            if (!HasActiveLevel || !spawnReady || victorySequenceStarted)
                return;
            controller.RequestCollect(behavior);
            if (controller.AreAllSettling)
                LockSettlementInput();
        }

        public static void LockSettlementInput()
        {
            LevelDirector.SetBusyState(true);
            RaycastController.ResetLongPress();
            RaycastController.Disable();
        }

        public static void NotifyCollected()
        {
            AudioClip clip = AudioController.AudioClips != null
                ? AudioController.AudioClips.boxCoverCollect
                : null;
            if (clip != null)
                AudioController.PlaySound(clip, 0.85f, UnityEngine.Random.Range(0.96f, 1.06f));
            RefreshHud(true);
            if (GameData.BFromStartScene && GameDirector.IsGameActive)
                RecoverLevelData.SaveCurrentLevelData();
            if (HasActiveLevel && controller.AllCollected)
                BeginVictorySequence();
        }

        public static void CollectActiveBehaviors(List<AwardColorEggCoverBehavior> dest)
        {
            controller?.CollectActiveBehaviors(dest);
        }

        public static void CompleteSpawnAnimation()
        {
            controller?.CompleteSpawnAnimation();
        }

        public static void WriteRecoveryData(RecoverLevelData target)
        {
            controller?.WriteRecoveryData(target);
        }

        public static bool HandleDockFilled()
        {
            if (!HasActiveLevel)
                return false;
            if (victorySequenceStarted || lotteryOpen)
                return true;
            if (controller.AllCollected)
            {
                BeginVictorySequence();
                return true;
            }

            return controller.HasPendingCollection;
        }

        public static void EnsureVictorySequence()
        {
            if (AreAllCollected)
                BeginVictorySequence();
        }

        public static bool TryConsumeVictory()
        {
            if (!HasActiveLevel || !controller.AllCollected || !victoryReady || victoryCommitted)
                return false;
            victoryCommitted = true;
            AwardColorEggCoverPending.MarkCompleted();
            return true;
        }

        public static void RelayoutHud()
        {
            if (!HasActiveLevel)
                return;
            UIController.GetPage<UIGame>()?.LayoutBoxCoverHud();
        }

        public static Vector3 GetHudTargetWorldPosition(Vector3 source)
        {
            UIGame game = UIController.GetPage<UIGame>();
            return game != null
                ? game.GetBoxCoverHudTargetWorldPosition(source)
                : source + Vector3.up * 3f;
        }

        public static void Clear()
        {
            ClearRuntime();
            UIGame game = UIController.GetPage<UIGame>();
            if (game != null)
            {
                game.SetBoxCoverHudCollectible(null);
                game.HideBoxCoverHud();
            }

            DockActor.NotifyBoxPanda(false, false);
        }

        static void BeginVictorySequence()
        {
            if (!AreAllCollected || victorySequenceStarted || lotteryOpen || !GameDirector.IsGameActive)
                return;

            victorySequenceStarted = true;
            AwardColorEggCoverPending.CommitPassOnConditionMet(LevelDirector.LoadedLevelIndex);
            LevelDirector.SetBusyState(true);
            RaycastController.ResetLongPress();
            RaycastController.Disable();
            if (GameData.BFromStartScene)
                RecoverLevelData.SaveCurrentLevelData();

            victoryDelay?.Kill();
            victoryDelay = DOVirtual.DelayedCall(0.35f, OpenLottery).SetUpdate(true);
        }

        static void OpenLottery()
        {
            victoryDelay = null;
            if (!HasActiveLevel || victoryReady)
                return;

            lotteryOpen = true;
            int levelIndex = LevelDirector.LoadedLevelIndex;
            AwardColorEggCoverReward[] rewards = AwardColorEggCoverPending.EnsureRolled(levelIndex);
            AwardColorEggCoverPending.CommitPassOnLotteryOpen(levelIndex);
            UIAwardColorEggCoverLottery.Show(rewards, 0, OnLotteryFinished);
        }

        static void OnLotteryFinished()
        {
            lotteryOpen = false;
            if (!HasActiveLevel || victoryReady || !GameDirector.IsGameActive)
                return;

            if (!HasRemainingBoardXSorts())
            {
                CompleteVictoryAfterQuickFinish();
                return;
            }

            DockActor dock = LevelDirector.Dock;
            if (dock == null)
            {
                CompleteVictoryAfterQuickFinish();
                return;
            }

            var autocompleteClip = AudioController.AudioClips?.autocomplete;
            if (autocompleteClip != null)
                AudioController.PlaySound(autocompleteClip);

            UIBoxCoverQuickVictory.Show();
            victoryDelay = DOVirtual.DelayedCall(1f, () =>
            {
                victoryDelay = null;
                if (!HasActiveLevel || victoryReady || !GameDirector.IsGameActive)
                    return;
                dock.StartBoxCoverQuickFinish(CompleteVictoryAfterQuickFinish);
            }).SetUpdate(true);
        }

        static void CompleteVictoryAfterQuickFinish()
        {
            if (!HasActiveLevel || victoryReady || !GameDirector.IsGameActive)
                return;
            victoryReady = true;
            GameDirector.levelController?.OnMatchCompleted();
        }

        static bool HasRemainingBoardXSorts()
        {
            List<XSortActor> XSorts = LevelDirector.LevelGenService?.ActXSorts;
            if (XSorts == null)
                return false;
            for (int i = 0; i < XSorts.Count; i++)
            {
                XSortActor XSort = XSorts[i];
                if (XSort != null && !XSort.IsSubmitted)
                    return true;
            }
            return false;
        }

        static List<AwardColorEggCoverPlacement> SavedPlacements(
            List<RecoverLevelData.RLAwardColorEggCoverData> states)
        {
            List<AwardColorEggCoverPlacement> result = new();
            HashSet<string> ids = new();
            for (int i = 0; i < states.Count; i++)
            {
                AwardColorEggCoverPlacement placement = states[i].placement;
                if (placement.layer <= 0 || !ids.Add(placement.RuntimeId))
                    continue;
                result.Add(placement);
            }
            return result;
        }

        static int CreateSeed()
        {
            unchecked
            {
                int random = UnityEngine.Random.Range(1, int.MaxValue);
                return random
                    ^ (LevelDirector.LoadedLevelIndex + 1) * 486187739
                    ^ Environment.TickCount;
            }
        }

        public static void RefreshHud()
        {
            RefreshHud(false);
        }

        static void RefreshHud(bool animate)
        {
            if (!HasActiveLevel)
                return;
            UIGame game = UIController.GetPage<UIGame>();
            if (game == null)
                return;

            game.SetBoxCoverHudCollectible(controller != null ? controller.CollectibleSprite : null);
            game.ShowBoxCoverHud(CollectedCount, TotalCount, animate);
            bool dogIntro = DockActor.PresentGoalHudDog(animate, !controller.AllCollected);
            DockActor.PlayGoalHudAppearIfDogIntro(dogIntro);
        }

        static void ClearRuntime()
        {
            victoryDelay?.Kill();
            victoryDelay = null;
            controller?.Dispose();
            controller = null;
            spawnReady = false;
            acceptRestoredBlockersWithoutHint = false;
            victorySequenceStarted = false;
            victoryReady = false;
            victoryCommitted = false;
            lotteryOpen = false;
        }
    }
}
