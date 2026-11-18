using System;
using System.Collections.Generic;
using DG.Tweening;
using TileGameCore;
using UnityEngine;

namespace FishTilePlay
{
    /// <summary>Static lifecycle bridge used by level, dock, save and UI systems.</summary>
    public static class BoxCoverManager
    {
        static BoxCoverController controller;
        static DG.Tweening.Tween dockFailureDelay;
        static bool dockFailureDeferred;
        static bool resolvingDockFailure;
        static bool victorySequenceStarted;
        static bool victoryReady;
        static bool victoryCommitted;
        static bool spawnReady;
        static bool acceptRestoredBlockersWithoutHint;
        static bool waitingForBoxDogExit;
        static bool collectionHudHiding;
        static DG.Tweening.Tween victoryCollectDelay;

        public static bool HasActiveLevel => controller != null && controller.TotalBoxCount > 0;
        public static bool IsSpawnReady => spawnReady;
        public static bool AreAllCollected => HasActiveLevel && controller.AllCollected;
        public static bool HasPendingCollection =>
            HasActiveLevel && controller.HasPendingCollection;
        public static int CollectedCount => controller?.CollectedItemCount ?? 0;
        public static int TotalCount => controller?.TotalItemCount ?? 0;
        public static bool IsVictorySequenceStarted => victorySequenceStarted;

        public static void Load(
            LevelModel level,
            GameObject layersParent,
            RecoverLevelData recoveryData = null)
        {
            Clear();
            spawnReady = false;

            if (level == null
                || layersParent == null
                || LevelDirector.BInCoinRewardLevel
                || AwardColorEggCoverManager.HasActiveLevel
                || !level.HasBoxCovers)
            {
                HideHud();
                return;
            }

            bool restoreLayout = recoveryData != null
                && recoveryData.LevelIndex == LevelDirector.LoadedLevelIndex
                && RecoverLevelData.ShouldRestoreBoxCoverLayout(recoveryData)
                && recoveryData.BoxCoverStates != null
                && recoveryData.BoxCoverStates.Count > 0;

            List<BoxCoverResolvedPlacement> placements = restoreLayout
                ? GetSavedPlacements(recoveryData.BoxCoverStates)
                : BoxCoverLayoutResolver.Resolve(level, GetReduction(), CreateSeed());
            if (placements.Count == 0)
            {
                HideHud();
                return;
            }

            controller = new BoxCoverController(
                level,
                layersParent.transform,
                placements,
                restoreLayout ? recoveryData.BoxCoverStates : null);
            acceptRestoredBlockersWithoutHint = restoreLayout;
            RefreshHud(false);
        }

        static List<BoxCoverResolvedPlacement> GetSavedPlacements(
            List<RecoverLevelData.RLBoxCoverData> states)
        {
            List<BoxCoverResolvedPlacement> result = new();
            HashSet<string> ids = new();
            for (int i = 0; i < states.Count; i++)
            {
                BoxCoverResolvedPlacement placement = states[i].placement;
                if (placement.layer <= 0 || !ids.Add(placement.RuntimeId))
                    continue;
                result.Add(placement);
            }
            return result;
        }

        static int GetReduction()
        {
            if (!GPUserDataManager.HasInit())
                return 0;

            int failCount = GPUserDataManager.Instance.UserData.continusFailCnt;
            if (failCount >= 3)
                return 2;
            if (failCount >= 2)
                return 1;
            return 0;
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

        public static void CollectActiveBehaviors(List<BoxCoverBehavior> dest)
        {
            controller?.CollectActiveBehaviors(dest);
        }

        public static void CompleteSpawnAnimation()
        {
            controller?.CompleteSpawnAnimation();
        }

        public static void NotifyLevelSpawnCompleted()
        {
            spawnReady = true;
            CompleteSpawnAnimation();
            UpdateStates();
        }

        public static void UpdateStates()
        {
            if (!HasActiveLevel || !spawnReady)
                return;

            bool acceptWithoutHint = acceptRestoredBlockersWithoutHint;
            acceptRestoredBlockersWithoutHint = false;
            controller.UpdateStates(acceptWithoutHint);
            if (controller.AllCollected)
                BeginVictorySequence();
        }

        public static bool IsTileBlocked(TileActor tile)
        {
            return HasActiveLevel && controller.IsTileBlocked(tile);
        }

        public static bool IsClickBlocked(TileActor tile, Vector3 worldPoint)
        {
            return HasActiveLevel && controller.IsClickBlocked(tile, worldPoint);
        }

        public static bool TryHandleBlockedClick(Vector3 worldPoint, TileActor resolvedTile = null)
        {
            return HasActiveLevel && controller.TryHandleBlockedClick(worldPoint, resolvedTile);
        }

        public static void WriteRecoveryData(RecoverLevelData target)
        {
            controller?.WriteRecoveryData(target);
        }

        public static void NotifyItemCollected(int collected, int total)
        {
            AudioClip clip = AudioController.AudioClips != null
                ? AudioController.AudioClips.boxCoverCollect
                : null;
            if (clip != null)
                AudioController.PlaySound(clip, 0.85f, UnityEngine.Random.Range(0.96f, 1.06f));
            RefreshHud(true, collected, total, true);
        }

        public static void NotifyBoxCollectionCompleted()
        {
            if (!HasActiveLevel)
                return;

            RefreshHud(false);
            if (controller.AllCollected)
            {
                AudioClip clip = AudioController.AudioClips != null
                    ? AudioController.AudioClips.allClear
                    : null;
                if (clip != null)
                    AudioController.PlaySound(clip, 1f);
                LockSettlementInput();
                waitingForBoxDogExit = true;
                DockActor.PlayBoxPandaExitThen(FadeCollectionHudWithOut, OnBoxDogExitFinished);
                return;
            }

            if (dockFailureDeferred && !controller.HasPendingCollection)
            {
                dockFailureDeferred = false;
                ScheduleDockFailureEvaluation();
            }
        }

        public static bool HandleDockFilled()
        {
            if (!HasActiveLevel)
                return false;

            if (victorySequenceStarted)
                return true;

            if (controller.AllCollected)
            {
                BeginVictorySequence();
                return true;
            }

            if (controller.HasPendingCollection)
            {
                dockFailureDeferred = true;
                return true;
            }

            // SubmitToSlot can report a full dock before SubmitElement has removed the tile from
            // the board. Defer one tick so a newly exposed final box wins the same-frame race.
            if (!resolvingDockFailure)
            {
                ScheduleDockFailureEvaluation();
                return true;
            }

            return false;
        }

        static void ScheduleDockFailureEvaluation()
        {
            if (dockFailureDelay != null && dockFailureDelay.IsActive())
                return;

            dockFailureDelay = DOVirtual.DelayedCall(0.05f, EvaluateDeferredDockFailure)
                .SetTarget(typeof(BoxCoverManager));
        }

        static void EvaluateDeferredDockFailure()
        {
            dockFailureDelay = null;
            if (!HasActiveLevel || !GameDirector.IsGameActive)
                return;

            if (controller.AllCollected)
            {
                BeginVictorySequence();
                return;
            }

            if (controller.HasPendingCollection)
            {
                dockFailureDeferred = true;
                return;
            }

            dockFailureDeferred = false;
            resolvingDockFailure = true;
            try
            {
                GameDirector.levelController?.OnSlotsFilled();
            }
            finally
            {
                resolvingDockFailure = false;
            }
        }

        static void OnBoxDogExitFinished()
        {
            waitingForBoxDogExit = false;
            BeginVictorySequence(0f);
        }

        public static void LockSettlementInput()
        {
            LevelDirector.SetBusyState(true);
            RaycastController.ResetLongPress();
            RaycastController.Disable();
        }

        public static void BeginWaitForBoxDogExit()
        {
            waitingForBoxDogExit = true;
            LockSettlementInput();
        }

        static void FadeCollectionHudWithOut()
        {
            FadeCollectionHud(DockActor.GetBoxPandaOutDuration());
        }

        static void FadeCollectionHud(float duration)
        {
            collectionHudHiding = true;
            UIController.GetPage<UIGame>()?.FadeBoxCoverHud(duration);
        }

        public static void EnsureVictorySequence()
        {
            if (AreAllCollected)
                BeginVictorySequence();
        }

        static void BeginVictorySequence(float waitOverride = -1f)
        {
            if (!AreAllCollected || victorySequenceStarted || !GameDirector.IsGameActive)
                return;
            if (waitingForBoxDogExit)
                return;

            victorySequenceStarted = true;
            LockSettlementInput();
            if (GameData.BFromStartScene)
                RecoverLevelData.SaveCurrentLevelData();

            float wait = waitOverride >= 0f ? waitOverride : CollectRemainingGoldenTrees();
            victoryCollectDelay?.Kill();
            victoryCollectDelay = DOVirtual.DelayedCall(wait, OnVictoryReady)
                .SetTarget(typeof(BoxCoverManager));
        }

        static float CollectRemainingGoldenTrees()
        {
            return 0.05f;
        }

        static void OnVictoryReady()
        {
            victoryCollectDelay = null;
            if (!HasActiveLevel || victoryReady)
                return;

            if (!HasRemainingBoardTiles())
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
            victoryCollectDelay = DOVirtual.DelayedCall(1.0f, () =>
            {
                victoryCollectDelay = null;
                if (!HasActiveLevel || victoryReady || !GameDirector.IsGameActive)
                    return;

                dock.StartBoxCoverQuickFinish(CompleteVictoryAfterQuickFinish);
            }).SetUpdate(true).SetTarget(typeof(BoxCoverManager));
        }

        static bool HasRemainingBoardTiles()
        {
            List<TileActor> tiles = LevelDirector.LevelGenService?.ActTiles;
            if (tiles == null)
                return false;

            for (int i = 0; i < tiles.Count; i++)
            {
                TileActor tile = tiles[i];
                if (tile != null && !tile.IsSubmitted)
                    return true;
            }

            return false;
        }

        static void CompleteVictoryAfterQuickFinish()
        {
            if (!HasActiveLevel || victoryReady || !GameDirector.IsGameActive)
                return;

            victoryReady = true;
            GameDirector.levelController?.OnMatchCompleted();
        }

        public static bool TryConsumeVictory()
        {
            if (!HasActiveLevel || !controller.AllCollected || !victoryReady || victoryCommitted)
                return false;

            victoryCommitted = true;
            return true;
        }

        public static Vector3 GetHudTargetWorldPosition(Vector3 sourceWorldPosition)
        {
            UIGame game = UIController.GetPage<UIGame>();
            return game != null
                ? game.GetBoxCoverHudTargetWorldPosition(sourceWorldPosition)
                : sourceWorldPosition + Vector3.up * 3f;
        }

        public static RectTransform CreateHudFlyItem(SpriteRenderer source)
        {
            UIGame game = UIController.GetPage<UIGame>();
            return game != null ? game.CreateBoxCoverFlyItem(source) : null;
        }

        public static bool TryWorldToHudFlyLocal(Vector3 worldPosition, out Vector2 local)
        {
            local = default;
            UIGame game = UIController.GetPage<UIGame>();
            return game != null && game.TryWorldToFlyLocal(worldPosition, out local);
        }

        public static bool TryGetHudFlyTarget(out Vector2 local, out Vector2 size)
        {
            return TryGetHudFlyTarget(out local, out size, out _);
        }

        public static bool TryGetHudFlyTarget(out Vector2 local, out Vector2 size, out float zAngle)
        {
            local = default;
            size = new Vector2(34f, 34f);
            zAngle = 0f;
            UIGame game = UIController.GetPage<UIGame>();
            return game != null && game.TryGetBoxCoverFlyTarget(out local, out size, out zAngle);
        }

        public static BoxCoverFlySettings GetFlySettings()
        {
            UIGame game = UIController.GetPage<UIGame>();
            BoxCoverFlySettings settings = game != null ? game.GetBoxCoverFlySettings() : null;
            if (settings == null)
                settings = BoxCoverFlySettings.Default;
            settings.EnsureDefaults();
            return settings;
        }

        public static void RelayoutHud()
        {
            UIController.GetPage<UIGame>()?.LayoutBoxCoverHud();
            AwardColorEggCoverManager.RelayoutHud();
            IQChallengeManager.RelayoutHud();
        }

        public static void SyncHudToDockBreathing()
        {
            UIController.GetPage<UIGame>()?.SyncBoxCoverHudToDockBreathing();
        }

        public static void RestoreHudFromDockBreathing()
        {
            UIController.GetPage<UIGame>()?.RestoreBoxCoverHudFromDockBreathing();
        }

        public static void RefreshHud()
        {
            RefreshHud(false);
        }

        static void RefreshHud(
            bool animate,
            int collectedOverride = -1,
            int totalOverride = -1,
            bool pandaCollect = false)
        {
            UIGame game = UIController.GetPage<UIGame>();
            if (game == null)
                return;

            if (!HasActiveLevel)
            {
                if (IQChallengeManager.HasActiveLevel)
                {
                    IQChallengeManager.RefreshHud();
                    DockActor.NotifyBoxPanda(false, false);
                    return;
                }

                if (AwardColorEggCoverManager.HasActiveLevel)
                {
                    AwardColorEggCoverManager.RefreshHud();
                    return;
                }

                game.HideBoxCoverHud();
                DockActor.NotifyBoxPanda(false, false);
                return;
            }

            if (collectionHudHiding)
                return;

            int collected = collectedOverride >= 0
                ? collectedOverride
                : controller.CollectedItemCount;
            int total = totalOverride >= 0
                ? totalOverride
                : controller.TotalItemCount;
            game.ShowBoxCoverHud(collected, total, animate);
            bool dogIntro = DockActor.PresentGoalHudDog(pandaCollect, !controller.AllCollected);
            DockActor.PlayGoalHudAppearIfDogIntro(dogIntro);
        }

        static void HideHud()
        {
            if (IQChallengeManager.HasActiveLevel)
            {
                IQChallengeManager.RefreshHud();
                DockActor.NotifyBoxPanda(false, false);
                return;
            }

            if (AwardColorEggCoverManager.HasActiveLevel)
            {
                AwardColorEggCoverManager.RefreshHud();
                return;
            }

            UIController.GetPage<UIGame>()?.HideBoxCoverHud();
            DockActor.NotifyBoxPanda(false, false);
        }

        public static void Clear()
        {
            dockFailureDelay?.Kill();
            dockFailureDelay = null;
            victoryCollectDelay?.Kill();
            victoryCollectDelay = null;
            dockFailureDeferred = false;
            resolvingDockFailure = false;
            victorySequenceStarted = false;
            victoryReady = false;
            victoryCommitted = false;
            spawnReady = false;
            acceptRestoredBlockersWithoutHint = false;
            waitingForBoxDogExit = false;
            collectionHudHiding = false;

            controller?.Dispose();
            controller = null;
            HideHud();
        }
    }
}
