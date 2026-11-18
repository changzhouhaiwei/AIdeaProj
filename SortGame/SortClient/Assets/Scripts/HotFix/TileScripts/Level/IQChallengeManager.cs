using System.Collections.Generic;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
using TileGameCore;
using FishFramework;

namespace FishTilePlay
{
    /// <summary>
    /// 裂变棋：初始安置、三消进度、感染与 HUD。
    /// 第 4 次消除后走可乐箱同款快速结算。
    /// </summary>
    public static class IQChallengeManager
    {
        public const int ClearCountToWin = 4;
        const string IqPanelPrefabPath = "Assets/CABRes/Prefabs/IQChallenge/IQChallengePanel.prefab";
        const float IqPanelDuration = 2f;
        const float IqBrokenDuration = 1f;

        static bool active;
        static int cleared;
        static bool boardReadyApplied;
        static bool victorySequenceStarted;
        static bool victoryReady;
        static bool victoryCommitted;
        static DG.Tweening.Tween victoryDelay;
        static DG.Tweening.Tween pandaSwitchDelay;
        static bool boltAfterMerge;
        static bool deferVictoryPanel;
        static Vector3 boltOrigin;
        static GameObject iqPanelInstance;
        static DG.Tweening.Tween iqPanelDelay;

        public static bool HasActiveLevel => active;
        public static int ClearedCount => cleared;
        public static int TotalCount => ClearCountToWin;
        public static bool IsReadyToWin => active && cleared >= ClearCountToWin;
        public static bool IsVictorySequenceStarted => victorySequenceStarted;

        public static void Load(LevelModel level, RecoverLevelData recoveryData = null)
        {
            ClearInternal(hideHud: false);
            if (level == null || !LevelDirector.IsIQChallengeLevelIndex(LevelDirector.LoadedLevelIndex))
            {
                DockActor.HideIqPanda();
                // ColorEgg 与 BoxCover 共用目标栏；只认 BoxCover 会把蛋关刚起的淡入掐掉。
                if (!BoxCoverManager.HasActiveLevel && !AwardColorEggCoverManager.HasActiveLevel)
                    HideHud();
                return;
            }

            active = true;
            cleared = 0;
            boardReadyApplied = false;
            if (recoveryData != null
                && recoveryData.LevelIndex == LevelDirector.LoadedLevelIndex)
            {
                cleared = Mathf.Clamp(recoveryData.iqChallengeClearedCount, 0, ClearCountToWin);
            }

            RefreshHud(false);
        }

        public static void NotifyBoardReady()
        {
            if (!active || boardReadyApplied)
                return;

            boardReadyApplied = true;
            if (cleared >= ClearCountToWin)
            {
                BeginVictorySequence();
                return;
            }

            if (HasAnyFission())
            {
                RefreshHud(false);
                return;
            }

            PlaceInitial();
            RefreshHud(false);
        }

        public static void NotifyFissionCleared()
        {
            if (!active)
                return;

            cleared = Mathf.Min(cleared + 1, ClearCountToWin);
        }

        public static void OnFissionMergeShattered()
        {
            if (!active)
                return;

            RefreshHud(true);
            if (!deferVictoryPanel)
                return;

            float wait = LevelDifficultySim.SkipPresentation ? 0f : IqBrokenDuration;
            victoryDelay?.Kill();
            victoryDelay = DOVirtual.DelayedCall(wait, () =>
            {
                victoryDelay = null;
                if (!deferVictoryPanel || !active)
                    return;
                deferVictoryPanel = false;
                BeginVictorySequence();
            }).SetUpdate(true).SetTarget(typeof(IQChallengeManager));
        }

        public static void OnFissionMatched(Vector3 origin)
        {
            if (!active)
                return;

            NotifyFissionCleared();
            IQChallengeVisual.ShowIqUp(origin, genius: cleared >= ClearCountToWin);

            if (cleared < ClearCountToWin)
            {
                boltAfterMerge = true;
                boltOrigin = origin;
                return;
            }

            deferVictoryPanel = true;
        }

        public static void OnFissionMergePresentationFinished()
        {
            if (!boltAfterMerge)
                return;

            boltAfterMerge = false;
            if (!active)
                return;

            InfectNext(boltOrigin);
        }

        public static bool HandleDockFilled()
        {
            if (!active)
                return false;

            if (victorySequenceStarted || deferVictoryPanel)
                return true;

            if (IsReadyToWin)
            {
                BeginVictorySequence();
                return true;
            }

            if (!HasRemainingBoardTiles())
            {
                SettleIfBoardExhausted();
                return true;
            }

            return false;
        }

        public static void EnsureVictorySequence()
        {
            if (deferVictoryPanel)
                return;
            if (IsReadyToWin)
                BeginVictorySequence();
        }

        /// <summary>
        /// 场上已经空了，但裂变消除还没到 4 次。无法再感染下一组脑花，直接进入脑花结算。
        /// </summary>
        public static void SettleIfBoardExhausted()
        {
            if (!active || victorySequenceStarted || victoryReady || victoryCommitted)
                return;
            if (HasRemainingBoardTiles())
                return;

            boltAfterMerge = false;
            if (cleared < ClearCountToWin)
                cleared = ClearCountToWin;
            RefreshHud(false);
            BeginVictorySequence();
        }

        public static bool TryConsumeVictory()
        {
            if (!active || !IsReadyToWin || !victoryReady || victoryCommitted)
                return false;

            victoryCommitted = true;
            return true;
        }

        public static void WriteRecoveryData(RecoverLevelData dest)
        {
            if (dest == null)
                return;
            dest.iqChallengeClearedCount = active ? cleared : 0;
        }

        public static void RefreshHud()
        {
            RefreshHud(false);
        }

        public static void RefreshHud(bool animate)
        {
            UIGame game = UIController.GetPage<UIGame>();
            if (game == null)
                return;

            if (!active)
            {
                DockActor.HideIqPanda();
                if (!BoxCoverManager.HasActiveLevel && !AwardColorEggCoverManager.HasActiveLevel)
                {
                    game.HideBoxCoverHud();
                    DockActor.NotifyBoxPanda(false, false);
                }
                return;
            }

            if (BoxCoverManager.HasActiveLevel)
                return;

            game.ShowGoalHud(GoalHudStyle.IQChallenge, cleared, ClearCountToWin, animate);
            DockActor.NotifyBoxPanda(false, false);
            int pandaLevel = PandaLevelForCleared(cleared);
            pandaSwitchDelay?.Kill();
            pandaSwitchDelay = null;
            DockActor.ShowIqPanda(pandaLevel, animate);
        }

        static int PandaLevelForCleared(int count)
        {
            return Mathf.Clamp(count, 0, 4) + 1;
        }

        public static void RelayoutHud()
        {
            if (!active || BoxCoverManager.HasActiveLevel)
                return;
            UIController.GetPage<UIGame>()?.LayoutBoxCoverHud();
        }

        public static void Clear()
        {
            IQChallengeVisual.CancelPresentation();
            ClearInternal(hideHud: true);
        }

        static void PlaceInitial()
        {
            List<TileActor> picked = IQChallengeSelector.PickTargets(0);
            if (!IsCompleteFissionSet(picked))
                return;

            for (int i = 0; i < picked.Count; i++)
            {
                if (picked[i] != null)
                    picked[i].SetFissionIdentity(true);
            }

            PersistIfNeeded();
            LevelDirector.LevelGenService?.UpdateClickableStates(false);

            for (int i = 0; i < picked.Count; i++)
            {
                TileActor tile = picked[i];
                if (tile == null)
                    continue;

                if (LevelDifficultySim.SkipPresentation)
                    IQChallengeVisual.ApplyLook(tile);
                else
                    IQChallengeVisual.PlayIntroConvert(tile);
            }
        }

        static void InfectNext(Vector3 origin)
        {
            List<TileActor> picked = IQChallengeSelector.PickTargets(cleared);
            ApplyFission(picked, animate: true, origin: origin, playBolt: true);
        }

        static void ApplyFission(List<TileActor> picked, bool animate, Vector3 origin, bool playBolt)
        {
            if (!IsCompleteFissionSet(picked))
            {
                LevelDirector.LevelGenService?.UpdateClickableStates(false);
                return;
            }

            for (int i = 0; i < picked.Count; i++)
            {
                if (picked[i] != null)
                    picked[i].SetFissionIdentity(true);
            }

            if (playBolt)
            {
                IQChallengeVisual.PlayInfect(origin, picked, () =>
                {
                    LevelDirector.LevelGenService?.UpdateClickableStates(true);
                    PersistIfNeeded();
                });
                return;
            }

            for (int i = 0; i < picked.Count; i++)
            {
                TileActor tile = picked[i];
                if (tile == null)
                    continue;
                if (animate)
                    IQChallengeVisual.PlayConvert(tile);
                else
                    IQChallengeVisual.ApplyLook(tile);
            }

            LevelDirector.LevelGenService?.UpdateClickableStates(false);
        }

        static bool IsCompleteFissionSet(List<TileActor> picked)
        {
            if (picked == null || picked.Count != IQChallengeSelector.NeedCount)
                return false;

            for (int i = 0; i < picked.Count; i++)
            {
                if (picked[i] == null)
                    return false;
            }

            return true;
        }

        static bool HasAnyFission()
        {
            List<TileActor> actTiles = LevelDirector.LevelGenService?.ActTiles;
            if (actTiles != null)
            {
                for (int i = 0; i < actTiles.Count; i++)
                {
                    TileActor tile = actTiles[i];
                    if (tile != null && tile.IsFission)
                        return true;
                }
            }

            DockActor dock = LevelDirector.Dock;
            if (dock == null)
                return false;

            List<TileActor> dockTiles = dock.GetOccupiedTileIndices();
            if (dockTiles == null)
                return false;

            for (int i = 0; i < dockTiles.Count; i++)
            {
                if (dockTiles[i] != null && dockTiles[i].IsFission)
                    return true;
            }

            return false;
        }

        static void PersistIfNeeded()
        {
            if (GameData.BFromStartScene && !LevelDifficultySim.IsRunning)
                RecoverLevelData.SaveCurrentLevelData();
        }

        static void BeginVictorySequence(float waitOverride = -1f)
        {
            if (!active || !IsReadyToWin || victorySequenceStarted || !GameDirector.IsGameActive)
                return;

            victorySequenceStarted = true;
            BoxCoverManager.LockSettlementInput();
            if (GameData.BFromStartScene)
                RecoverLevelData.SaveCurrentLevelData();

            if (LevelDifficultySim.SkipPresentation)
            {
                CompleteVictoryAfterQuickFinish();
                return;
            }

            float wait = waitOverride >= 0f ? waitOverride : 0.05f;
            victoryDelay?.Kill();
            victoryDelay = DOVirtual.DelayedCall(wait, OnVictoryReady)
                .SetTarget(typeof(IQChallengeManager));
        }

        static void OnVictoryReady()
        {
            victoryDelay = null;
            if (!active || victoryReady)
                return;

            PlayIqPanelThen(BeginQuickVictory);
        }

        static void BeginQuickVictory()
        {
            if (!active || victoryReady)
                return;

            DockActor dock = LevelDirector.Dock;
            bool dockLeft = dock != null && !dock.IsEmpty;
            if (!HasRemainingBoardTiles() && !dockLeft)
            {
                CompleteVictoryAfterQuickFinish();
                return;
            }

            if (dock == null)
            {
                CompleteVictoryAfterQuickFinish();
                return;
            }

            var autocompleteClip = AudioController.AudioClips?.autocomplete;
            if (autocompleteClip != null)
                AudioController.PlaySound(autocompleteClip);

            UIIQChallengeQuickVictory.Show();
            victoryDelay = DOVirtual.DelayedCall(1.0f, () =>
            {
                victoryDelay = null;
                if (!active || victoryReady || !GameDirector.IsGameActive)
                    return;

                dock.StartBoxCoverQuickFinish(CompleteVictoryAfterQuickFinish);
            }).SetUpdate(true).SetTarget(typeof(IQChallengeManager));
        }

        static void PlayIqPanelThen(System.Action next)
        {
            DestroyIqPanel();
            GameObject prefab = UILoadHelper.LoadAsset<GameObject>(IqPanelPrefabPath);
#if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(IqPanelPrefabPath);
#endif
            Canvas main = UIController.MainCanvas;
            Transform parent = main != null ? main.transform : UIController.GetPage<UIGame>()?.transform;
            if (prefab == null || parent == null)
            {
                if (prefab == null)
                    Debug.LogError($"[IQ] panel prefab is missing: {IqPanelPrefabPath}");
                next?.Invoke();
                return;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, parent, false);
            go.name = "IQChallengePanel";
            SetLayerRecursively(go, parent.gameObject.layer);
            RectTransform rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.SetAsLastSibling();
            }

            Canvas canvas = go.GetComponent<Canvas>();
            if (canvas == null)
                canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = (main != null ? main.sortingOrder : 0) + 500;
            if (go.GetComponent<GraphicRaycaster>() == null)
                go.AddComponent<GraphicRaycaster>();

            Animator entrance = go.GetComponent<Animator>();
            if (entrance != null && entrance.runtimeAnimatorController != null)
            {
                entrance.updateMode = AnimatorUpdateMode.UnscaledTime;
                entrance.enabled = true;
                entrance.Rebind();
                entrance.Update(0f);
            }

            SkeletonGraphic spine = go.GetComponentInChildren<SkeletonGraphic>(true);
            if (spine != null)
            {
                spine.UnscaledTime = true;
                spine.gameObject.SetActive(true);
                if (!spine.IsValid)
                    spine.Initialize(false);
                if (spine.AnimationState != null && !string.IsNullOrEmpty(spine.startingAnimation))
                {
                    spine.AnimationState.ClearTrack(0);
                    spine.AnimationState.SetAnimation(0, spine.startingAnimation, true);
                    spine.Update(0f);
                }
            }

            iqPanelInstance = go;
            iqPanelDelay = DOVirtual.DelayedCall(IqPanelDuration, () =>
            {
                iqPanelDelay = null;
                DestroyIqPanel();
                if (!active)
                    return;
                next?.Invoke();
            }).SetUpdate(true).SetTarget(typeof(IQChallengeManager));
        }

        static void DestroyIqPanel()
        {
            iqPanelDelay?.Kill();
            iqPanelDelay = null;
            if (iqPanelInstance != null)
                UnityEngine.Object.Destroy(iqPanelInstance);
            iqPanelInstance = null;
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            if (go == null)
                return;
            go.layer = layer;
            Transform root = go.transform;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i).gameObject, layer);
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
            if (!active || victoryReady || !GameDirector.IsGameActive)
                return;

            victoryReady = true;
            GameDirector.levelController?.OnMatchCompleted();
        }

        static void ClearInternal(bool hideHud)
        {
            victoryDelay?.Kill();
            victoryDelay = null;
            pandaSwitchDelay?.Kill();
            pandaSwitchDelay = null;
            boltAfterMerge = false;
            deferVictoryPanel = false;
            DestroyIqPanel();
            active = false;
            cleared = 0;
            boardReadyApplied = false;
            victorySequenceStarted = false;
            victoryReady = false;
            victoryCommitted = false;
            if (hideHud && !BoxCoverManager.HasActiveLevel && !AwardColorEggCoverManager.HasActiveLevel)
                HideHud();
        }

        static void HideHud()
        {
            UIController.GetPage<UIGame>()?.HideBoxCoverHud();
            DockActor.HideIqPanda();
            DockActor.NotifyBoxPanda(false, false);
        }
    }
}
