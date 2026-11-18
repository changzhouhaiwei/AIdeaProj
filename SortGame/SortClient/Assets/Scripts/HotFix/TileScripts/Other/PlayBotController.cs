#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using TileGameCore;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using FishFramework;

namespace FishTilePlay
{
    public enum PlayBotStyle
    {
        Perfect = 0,
        Live = 1,
        Casual = 2,
        Skilled = 3
    }

        /// <summary>
        /// Editor demo: plays levels by calling the same click APIs as a player.
        /// F9 start/stop, F8 cycle time scale (1 / 3 / 8), F7 cycle Perfect / Skilled / Live / Casual.
        /// Perfect/Skilled 选子见 PlayBotPlanner（Need / 空格 / 棋盘分布 / 暂存分布）。
        /// Overlay: F9 start/stop. GM panel PlayBot switch can hide the box; F9 still turns it back on.
        /// Long-press the black box to drag its top-right corner. +/− there collapses toward that corner.
        /// </summary>
        public class PlayBotController : MonoBehaviour
        {
            static readonly float[] TimeScales = { 1f, 3f, 8f };
            const string DebugLabelName = "PlayBotDebugLabel";
            const int DebugLabelSortingOrder = 32000;
            const float HudLongPressSeconds = 0.35f;
            const float HudDefaultX = 12f;
            const float HudDefaultY = 12f;
            const float HudWidth = 320f;
            const float HudToggleW = 28f;
            const float HudToggleH = 22f;

            static bool s_hudEnabled = true;

        static readonly PlayBotStyle[] StyleCycle =
        {
            PlayBotStyle.Perfect,
            PlayBotStyle.Skilled,
            PlayBotStyle.Live,
            PlayBotStyle.Casual
        };

        [SerializeField] PlayBotStyle _style = PlayBotStyle.Skilled;
        readonly PlayBotPlanner _planner = new PlayBotPlanner();
        Vector3 _lastClickWorld;
        bool _hasLastClick;

        bool _running;
        bool _ownsTimeScale;
        float _savedTimeScale = 1f;
        int _timeScaleIndex;

        string _status = "Idle  F9 start  F7 style";
        int _clicks;
        int _hints;
        int _shuffles;
        int _undos;
        int _wins;
        int _fails;
        int _stuckTries;
        int _lastRestTiles = -1;
        int _noProgressClicks;

        Coroutine _loop;
        TileActor _heldTile;
        int _commitColorId = -1;
        bool _commitHadInDock;

        Vector2 _hudPos = new Vector2(HudDefaultX + HudWidth, HudDefaultY);
        bool _hudCollapsed;
        Rect _hudRect;
        bool _hudRectValid;
        bool _hudPressing;
        bool _hudDragging;
        bool _hudBlockButtons;
        bool _hudTogglePress;
        float _hudPressRealtime;
        Vector2 _hudDragOffset;
        GUIStyle _gmInfoStyle;

        public static PlayBotController Instance { get; private set; }

        public static void NotifyGmOpened()
        {
        }

        public static bool IsHudEnabled => s_hudEnabled;

        public static void SetHudEnabled(bool enabled)
        {
            s_hudEnabled = enabled;
            if (!enabled && Instance != null && Instance._running)
                Instance.StopBot(restoreTimeScale: true);
        }

        public static void Bind(GameDirector host)
        {
            if (host == null)
                return;
            if (host.GetComponent<PlayBotController>() == null)
                host.gameObject.AddComponent<PlayBotController>();
        }

        void Awake()
        {
            Instance = this;
            _hudPos = new Vector2(HudDefaultX + HudWidth, HudDefaultY);
        }

        void OnEnable()
        {
            GameEvent.AddEventListener((int)EventNameDef.LoadLevelAnimFinish, OnLevelBecamePlayable);
            GameEvent.AddEventListener((int)EventNameDef.LoadSaveLevelAnimFinish, OnLevelBecamePlayable);
        }

        void OnDisable()
        {
            GameEvent.RemoveEventListener((int)EventNameDef.LoadLevelAnimFinish, OnLevelBecamePlayable);
            GameEvent.RemoveEventListener((int)EventNameDef.LoadSaveLevelAnimFinish, OnLevelBecamePlayable);
            StopBot(restoreTimeScale: true);
        }

        void OnLevelBecamePlayable()
        {
            ResetLevelTracking();
            if (_running)
            {
                _status = $"Lv {DisplayLevel()} ready";
                PlayBotStepLog.LevelStart(DisplayLevel(), _style.ToString());
            }
        }

        void ResetLevelTracking()
        {
            _stuckTries = 0;
            _noProgressClicks = 0;
            _lastRestTiles = -1;
            _hasLastClick = false;
            ClearCommit();
        }

        void ClearCommit()
        {
            _commitColorId = -1;
            _commitHadInDock = false;
            _planner.Reset();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f9Key.wasPressedThisFrame)
                {
                    if (!s_hudEnabled)
                        SetHudEnabled(true);
                    Toggle();
                }

                if (keyboard.f7Key.wasPressedThisFrame)
                    CycleStyle();

                if (_running && keyboard.f8Key.wasPressedThisFrame)
                    CycleTimeScale();
            }

            UpdateHudDrag();
        }

        void LateUpdate()
        {
            if (_running)
                SyncDebugLabels();
        }

        public void Toggle()
        {
            if (_running)
                StopBot(restoreTimeScale: true);
            else
                StartBot();
        }

        public void StartBot()
        {
            if (_running)
                return;

            _running = true;
            ResetLevelTracking();
            _status = GameDirector.IsGameActive
                ? $"Lv {DisplayLevel()}  {_style}"
                : $"Waiting for level  {_style}";

            RaycastController.ResetLongPress();
            RaycastController.Disable();

            if (_loop != null)
                StopCoroutine(_loop);
            _loop = StartCoroutine(RunLoop());
            Debug.Log($"[PlayBot] started  style={_style}  F9 stop  F8 speed  F7 style");
            PlayBotStepLog.Console = true;
            PlayBotStepLog.File = true;
            PlayBotStepLog.BeginSession(_style.ToString());
        }

        public void StopBot(bool restoreTimeScale)
        {
            _running = false;
            ReleaseHeldTile();
            ClearCommit();

            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }

            if (restoreTimeScale && _ownsTimeScale)
            {
                Time.timeScale = _savedTimeScale;
                _ownsTimeScale = false;
                _timeScaleIndex = 0;
            }

            RaycastController.Enable();
            _status = "Idle  F9 start  F7 style";
            DestroyAllDebugLabels();
            Debug.Log($"[PlayBot] stopped  clicks={_clicks} hint={_hints} shuffle={_shuffles} undo={_undos} win={_wins} fail={_fails}");
        }

        void CycleTimeScale()
        {
            if (!_ownsTimeScale)
            {
                _savedTimeScale = Time.timeScale;
                _ownsTimeScale = true;
                _timeScaleIndex = 0;
            }

            _timeScaleIndex = (_timeScaleIndex + 1) % TimeScales.Length;
            Time.timeScale = TimeScales[_timeScaleIndex];
            _status = $"speed x{Time.timeScale:0.##}  {_style}";
        }

        void CycleStyle()
        {
            int index = System.Array.IndexOf(StyleCycle, _style);
            if (index < 0)
                index = 0;
            _style = StyleCycle[(index + 1) % StyleCycle.Length];
            ClearCommit();
            _status = $"style {_style}";
            Debug.Log($"[PlayBot] style {_style}");
        }

        IEnumerator RunLoop()
        {
            var wait = new WaitForSeconds(0.05f);

            while (_running)
            {
                if (UIController.IsDisplayed<UIComplete>())
                {
                    yield return HandleVictory();
                    continue;
                }

                if (UIController.IsDisplayed<UIGameOver>())
                {
                    yield return HandleFail();
                    continue;
                }

                if (!GameDirector.IsGameActive || !LevelDirector.IsLevelLoaded || LevelDirector.LevelGenService == null)
                {
                    _status = "Waiting for level";
                    TryDismissBlockingPanel();
                    yield return wait;
                    continue;
                }

                if (!CanActOnBoard())
                {
                    if (_running && RaycastController.IsActive)
                        RaycastController.Disable();
                    TryDismissBlockingPanel();
                    yield return wait;
                    continue;
                }

                if (LevelDirector.Dock != null && !LevelDirector.Dock.CanAcceptMoreSubmits())
                {
                    _status = "Dock full, waiting";
                    _stuckTries++;
                    if (_stuckTries >= DockFullUndoAt() && DockActor.HasUndoablePickedObject())
                        yield return UseItem(PUType.Undo);
                    yield return wait;
                    continue;
                }

                TileActor tile = PickNextTile();
                if (tile != null)
                {
                    _stuckTries = 0;
                    yield return ClickTileLikePlayer(tile);
                    TrackProgress();
                    yield return new WaitForSeconds(ThinkDelay());
                    continue;
                }

                _stuckTries++;
                _status = $"Stuck {_stuckTries}  looking for item";

                if (_stuckTries == UndoStuckAt() && DockActor.HasUndoablePickedObject())
                {
                    yield return UseItem(PUType.Undo);
                    continue;
                }

                if (_stuckTries == HintStuckAt())
                {
                    yield return UseItem(PUType.Hint);
                    continue;
                }

                if (_stuckTries >= ShuffleStuckAt())
                {
                    yield return UseItem(PUType.Shuffle);
                    _stuckTries = 0;
                    continue;
                }

                yield return wait;
            }
        }

        bool CanActOnBoard()
        {
            if (LevelDirector.IsBusy)
                return false;
            if (!LevelDirector.IsRaycastEnabled)
                return false;
            if (UIController.IsPopupOpened)
                return false;
            if (UIQuickVictory.IsAnyDisplayed())
                return false;
            if (UIController.IsDisplayed<UIGame>())
            {
                var dock = LevelDirector.Dock;
                if (dock != null && dock.HasActiveSlotMotion())
                    return false;
            }

            return true;
        }

        IEnumerator ClickTileLikePlayer(TileActor tile)
        {
            if (tile == null || tile.IsSubmitted)
                yield break;

            _heldTile = tile;
            tile.OnObjectClicked(out bool isValid);
            if (!isValid)
            {
                _status = "Click blocked";
                _heldTile = null;
                yield break;
            }

            yield return new WaitForSeconds(0.07f);

            if (tile == null || tile.IsSubmitted)
            {
                _heldTile = null;
                yield break;
            }

            tile.OnObjectReleased();
            _heldTile = null;
            _clicks++;
            _lastClickWorld = tile.transform.position;
            _hasLastClick = true;
            int typeId = tile.TileModel != null ? tile.MatchTypeId : -1;
            PlayBotStepLog.Emit(DisplayLevel(), _clicks, _style.ToString(), _planner.LastStep);
            _status = _commitColorId >= 0
                ? $"Lv {DisplayLevel()}  click #{_clicks}  type {typeId}  lock {_commitColorId}{SkilledPlanSuffix()}"
                : $"Lv {DisplayLevel()}  click #{_clicks}  type {typeId}{SkilledPlanSuffix()}";
        }

        string SkilledPlanSuffix()
        {
            if (_style != PlayBotStyle.Perfect && _style != PlayBotStyle.Skilled)
                return string.Empty;
            int cat = _planner.LastCategory;
            return cat <= 0 ? "  R" : $"  n{_planner.LastNeed} e{_planner.LastExtra}";
        }

        void ReleaseHeldTile()
        {
            if (_heldTile == null)
                return;
            if (!_heldTile.IsSubmitted)
                _heldTile.RecoverLongPressAnim(true);
            _heldTile = null;
        }

        float ThinkDelay()
        {
            if (_style == PlayBotStyle.Perfect)
                return 0.18f;
            if (_style == PlayBotStyle.Skilled)
            {
                float skilled = Random.Range(0.22f, 0.42f);
                if (Random.value < 0.04f)
                    skilled += Random.Range(0.25f, 0.7f);
                return skilled;
            }

            float min = _style == PlayBotStyle.Casual ? 0.38f : 0.28f;
            float max = _style == PlayBotStyle.Casual ? 1.05f : 0.72f;
            float delay = Random.Range(min, max);
            if (Random.value < (_style == PlayBotStyle.Casual ? 0.12f : 0.07f))
                delay += Random.Range(0.45f, 1.35f);
            return delay;
        }

        int DockFullUndoAt()
        {
            if (_style == PlayBotStyle.Perfect)
                return 8;
            if (_style == PlayBotStyle.Skilled)
                return 9;
            return _style == PlayBotStyle.Casual ? 16 : 12;
        }

        int UndoStuckAt()
        {
            if (_style == PlayBotStyle.Perfect)
                return 3;
            if (_style == PlayBotStyle.Skilled)
                return 4;
            return _style == PlayBotStyle.Casual ? 9 : 6;
        }

        int HintStuckAt()
        {
            if (_style == PlayBotStyle.Perfect)
                return 5;
            if (_style == PlayBotStyle.Skilled)
                return 7;
            return _style == PlayBotStyle.Casual ? 15 : 11;
        }

        int ShuffleStuckAt()
        {
            if (_style == PlayBotStyle.Perfect)
                return 8;
            if (_style == PlayBotStyle.Skilled)
                return 11;
            return _style == PlayBotStyle.Casual ? 22 : 16;
        }

        TileActor PickNextTile()
        {
            if (_style == PlayBotStyle.Skilled)
            {
                TileActor skilled = _planner.PickNext(PlayBotNoticeMode.Occlusion);
                _commitColorId = _planner.CommitColorId;
                return skilled;
            }

            if (_style == PlayBotStyle.Perfect)
            {
                TileActor perfect = _planner.PickNext(PlayBotNoticeMode.All);
                _commitColorId = _planner.CommitColorId;
                return perfect;
            }

            if (!LevelDirector.SubmitIsAllowed())
                return null;
            if (LevelDirector.LevelGenService == null || LevelDirector.LevelGenService.ActTiles == null)
                return null;

            List<TileActor> clickable = PlayBotPlanner.CollectClickableTiles();
            if (clickable.Count == 0)
            {
                ClearCommit();
                return null;
            }

            Dictionary<int, int> dockCounts = PlayBotPlanner.CollectDockColorCounts();
            List<TileActor> actTiles = LevelDirector.LevelGenService.ActTiles;
            Dictionary<int, int> boardVisible = PlayBotPlanner.CountByColor(clickable);
            Dictionary<int, int> boardTotal = PlayBotPlanner.CountBoardRemaining(actTiles);
            int slotsAvailable = DockActor.GetSlotsAvailable();
            List<PlayBotPlanner.ColorPlan> plans = PlayBotPlanner.CollectPlans(
                clickable, dockCounts, boardVisible, slotsAvailable, actTiles);
            return PickHumanTile(clickable, dockCounts, boardVisible, boardTotal, actTiles, slotsAvailable, plans);
        }

        TileActor PickHumanTile(
            List<TileActor> clickable,
            Dictionary<int, int> dockCounts,
            Dictionary<int, int> boardVisible,
            Dictionary<int, int> boardTotal,
            List<TileActor> actTiles,
            int slotsAvailable,
            List<PlayBotPlanner.ColorPlan> plans)
        {
            List<PlayBotPlanner.ColorPlan> noticed = FilterNoticedPlans(plans);
            int bestColor = noticed.Count > 0 ? PlayBotPlanner.BestPlan(noticed).ColorId : -1;
            int bestNeed = noticed.Count > 0 ? PlayBotPlanner.BestPlan(noticed).Need : 0;
            string afterClear = "";

            if (_commitColorId >= 0)
            {
                dockCounts.TryGetValue(_commitColorId, out int inDock);
                if (inDock > 0)
                    _commitHadInDock = true;

                if (_commitHadInDock && inDock == 0)
                {
                    afterClear = $"clear-{_commitColorId}";
                    ClearCommit();
                }
                else if (!CommitPlanExists(_commitColorId, plans))
                {
                    afterClear = "lock-invalid";
                    ClearCommit();
                }
                else if (ShouldBreakHumanCommit(_commitColorId, noticed, slotsAvailable, dockCounts))
                {
                    afterClear = "break-human";
                    ClearCommit();
                }
                else
                {
                    TileActor committed = PickCommittedHumanTile(_commitColorId, clickable, actTiles);
                    if (committed != null)
                    {
                        return PublishHuman(
                            committed.TileModel != null && committed.MatchTypeId == _commitColorId
                                ? "KEEP"
                                : "UNCOVER",
                            "lock",
                            committed,
                            _commitColorId,
                            plans,
                            bestColor,
                            bestNeed,
                            plans.Count,
                            noticed.Count,
                            dockCounts,
                            boardVisible,
                            boardTotal,
                            slotsAvailable);
                    }

                    afterClear = "lock-no-tile";
                    ClearCommit();
                }
            }

            if (!HasObviousVisibleMatch(noticed) && Random.value < WanderChance(slotsAvailable))
            {
                TileActor wander = PickWanderTile(clickable, dockCounts, slotsAvailable);
                int wanderColor = wander != null && wander.TileModel != null ? wander.MatchTypeId : -1;
                return PublishHuman(
                    "WANDER",
                    string.IsNullOrEmpty(afterClear) ? "idle" : afterClear,
                    wander,
                    wanderColor,
                    plans,
                    bestColor,
                    bestNeed,
                    plans.Count,
                    noticed.Count,
                    dockCounts,
                    boardVisible,
                    boardTotal,
                    slotsAvailable);
            }

            if (!TryChooseHumanPlan(noticed, out PlayBotPlanner.ColorPlan chosen))
            {
                TileActor wander = PickWanderTile(clickable, dockCounts, slotsAvailable);
                int wanderColor = wander != null && wander.TileModel != null ? wander.MatchTypeId : -1;
                return PublishHuman(
                    "WANDER",
                    string.IsNullOrEmpty(afterClear) ? "no-noticed" : afterClear,
                    wander,
                    wanderColor,
                    plans,
                    bestColor,
                    bestNeed,
                    plans.Count,
                    noticed.Count,
                    dockCounts,
                    boardVisible,
                    boardTotal,
                    slotsAvailable);
            }

            TileActor tile = PickCommittedHumanTile(chosen.ColorId, clickable, actTiles);
            if (tile != null)
            {
                CommitColor(chosen.ColorId, dockCounts, tile);
                return PublishHuman(
                    tile.TileModel != null && tile.MatchTypeId == chosen.ColorId ? "NEW" : "NEW-UNCOVER",
                    string.IsNullOrEmpty(afterClear) ? "best" : afterClear,
                    tile,
                    chosen.ColorId,
                    plans,
                    bestColor,
                    bestNeed,
                    plans.Count,
                    noticed.Count,
                    dockCounts,
                    boardVisible,
                    boardTotal,
                    slotsAvailable);
            }

            ClearCommit();
            TileActor fallback = PickWanderTile(clickable, dockCounts, slotsAvailable);
            int fallbackColor = fallback != null && fallback.TileModel != null ? fallback.MatchTypeId : -1;
            return PublishHuman(
                "WANDER",
                "pick-failed",
                fallback,
                fallbackColor,
                plans,
                bestColor,
                bestNeed,
                plans.Count,
                noticed.Count,
                dockCounts,
                boardVisible,
                boardTotal,
                slotsAvailable);
        }

        TileActor PublishHuman(
            string action,
            string why,
            TileActor tile,
            int targetColor,
            List<PlayBotPlanner.ColorPlan> plans,
            int bestColor,
            int bestNeed,
            int planCount,
            int noticedCount,
            Dictionary<int, int> dockCounts,
            Dictionary<int, int> boardVisible,
            Dictionary<int, int> boardTotal,
            int slotsAvailable)
        {
            _planner.PublishStep(PlayBotPlanner.MakeStep(
                action, why, tile, targetColor, plans, bestColor, bestNeed,
                planCount, noticedCount, dockCounts, boardVisible, boardTotal, slotsAvailable));
            return tile;
        }

        TileActor CommitColor(int colorId, Dictionary<int, int> dockCounts, TileActor tile)
        {
            _commitColorId = colorId;
            dockCounts.TryGetValue(colorId, out int inDock);
            _commitHadInDock = inDock > 0;
            return tile;
        }

        bool ShouldBreakHumanCommit(
            int commitColorId,
            List<PlayBotPlanner.ColorPlan> noticed,
            int slotsAvailable,
            Dictionary<int, int> dockCounts)
        {
            dockCounts.TryGetValue(commitColorId, out int inDock);
            if (inDock >= 2)
                return false;
            if (inDock <= 0 && slotsAvailable >= 4
                && Random.value < (_style == PlayBotStyle.Casual ? 0.10f : 0.04f))
                return true;
            if (noticed.Count == 0)
                return false;

            PlayBotPlanner.ColorPlan best = PlayBotPlanner.BestPlan(noticed);
            if (best.ColorId == commitColorId)
                return false;
            if (inDock < 2 && best.IsLocalTriple)
                return true;
            if (inDock >= 1 && (best.Need > 1 || best.Extra > 0 || best.BoardVisible <= 0))
                return false;
            if (best.Need <= 1 && best.BoardVisible > 0 && best.Extra <= 0)
                return Random.value < (_style == PlayBotStyle.Casual ? 0.35f : 0.55f);
            return false;
        }

        static bool CommitPlanExists(int colorId, List<PlayBotPlanner.ColorPlan> plans)
        {
            return PlayBotPlanner.TryFindPlan(plans, colorId, out _);
        }

        TileActor PickCommittedHumanTile(int colorId, List<TileActor> clickable, List<TileActor> actTiles)
        {
            TileActor sameColor = FindHumanClickableOfColor(clickable, colorId);
            if (sameColor != null)
                return sameColor;
            return PlayBotPlanner.FindCoverToUncoverColor(colorId, clickable, actTiles);
        }

        List<PlayBotPlanner.ColorPlan> FilterNoticedPlans(List<PlayBotPlanner.ColorPlan> plans)
        {
            var noticed = new List<PlayBotPlanner.ColorPlan>();
            for (int i = 0; i < plans.Count; i++)
            {
                if (Random.value < NoticeChance(plans[i]))
                    noticed.Add(plans[i]);
            }

            return noticed;
        }

        float NoticeChance(PlayBotPlanner.ColorPlan plan)
        {
            float live;
            float casual;
            if (plan.Need <= 1 && plan.Extra <= 0)
            {
                live = 0.94f;
                casual = 0.82f;
            }
            else if (plan.Need <= 1)
            {
                live = 0.20f;
                casual = 0.07f;
            }
            else if (plan.Need == 2 && plan.Extra <= 0)
            {
                live = 0.84f;
                casual = 0.64f;
            }
            else if (plan.Need == 2)
            {
                live = 0.24f;
                casual = 0.08f;
            }
            else if (plan.IsLocalTriple || (plan.Extra <= 0 && plan.BoardVisible >= 2))
            {
                live = 1f;
                casual = 0.85f;
            }
            else if (plan.Extra <= 0)
            {
                live = 0.80f;
                casual = 0.56f;
            }
            else if (plan.BoardVisible >= 2)
            {
                live = 0.18f;
                casual = 0.06f;
            }
            else
            {
                live = 0.12f;
                casual = 0.04f;
            }

            return _style == PlayBotStyle.Casual ? casual : live;
        }

        static bool HasObviousVisibleMatch(List<PlayBotPlanner.ColorPlan> noticed)
        {
            for (int i = 0; i < noticed.Count; i++)
            {
                PlayBotPlanner.ColorPlan plan = noticed[i];
                if (plan.IsLocalTriple)
                    return true;
                if (plan.Extra > 0)
                    continue;
                if (plan.BoardVisible >= 2)
                    return true;
                if (plan.BoardVisible >= plan.Need)
                    return true;
            }

            return false;
        }

        float WanderChance(int slotsAvailable)
        {
            if (_style == PlayBotStyle.Casual)
                return slotsAvailable >= 5 ? 0.42f : slotsAvailable >= 3 ? 0.24f : 0.10f;
            return slotsAvailable >= 5 ? 0.28f : slotsAvailable >= 3 ? 0.14f : 0.05f;
        }

        bool TryChooseHumanPlan(List<PlayBotPlanner.ColorPlan> noticed, out PlayBotPlanner.ColorPlan chosen)
        {
            chosen = default;
            if (noticed.Count == 0)
                return false;

            if (TryPickSalientBoardTriple(noticed, out chosen))
                return true;

            float takeBest = _style == PlayBotStyle.Casual ? 0.38f : 0.58f;
            if (Random.value < takeBest)
            {
                chosen = PlayBotPlanner.BestPlan(noticed);
                return true;
            }

            chosen = WeightedPlan(noticed);
            return true;
        }

        static bool TryPickSalientBoardTriple(
            List<PlayBotPlanner.ColorPlan> noticed,
            out PlayBotPlanner.ColorPlan chosen)
        {
            chosen = default;
            var triples = new List<PlayBotPlanner.ColorPlan>();
            for (int i = 0; i < noticed.Count; i++)
            {
                PlayBotPlanner.ColorPlan plan = noticed[i];
                if (plan.IsLocalTriple || (plan.Extra <= 0 && plan.BoardVisible >= PlayBotPlanner.MatchSize))
                    triples.Add(plan);
            }

            if (triples.Count == 0)
                return false;
            chosen = PlayBotPlanner.BestPlan(triples);
            return true;
        }

        static PlayBotPlanner.ColorPlan WeightedPlan(List<PlayBotPlanner.ColorPlan> plans)
        {
            float sum = 0f;
            var weights = new float[plans.Count];
            for (int i = 0; i < plans.Count; i++)
            {
                weights[i] = HumanPlanWeight(plans[i]);
                sum += weights[i];
            }

            if (sum <= 0f)
                return plans[Random.Range(0, plans.Count)];

            float roll = Random.Range(0f, sum);
            for (int i = 0; i < plans.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                    return plans[i];
            }

            return plans[plans.Count - 1];
        }

        static float HumanPlanWeight(PlayBotPlanner.ColorPlan plan)
        {
            float weight = 4 - plan.Need;
            if (plan.IsLocalTriple)
                weight += 8f;
            else if (plan.BoardVisible >= 3)
                weight += 2f;
            if (plan.InDock >= 2 && plan.BoardVisible > 0)
                weight += 3f;
            if (plan.Need <= 1 && plan.Extra > 0)
                weight *= 0.4f;
            else if (plan.Extra > 0 && plan.Need >= 3)
                weight *= 0.45f;
            return Mathf.Max(0.15f, weight);
        }

        TileActor PickWanderTile(
            List<TileActor> clickable,
            Dictionary<int, int> dockCounts,
            int slotsAvailable)
        {
            var dockMatches = new List<TileActor>();
            for (int i = 0; i < clickable.Count; i++)
            {
                TileActor tile = clickable[i];
                if (tile.TileModel == null)
                    continue;
                dockCounts.TryGetValue(tile.MatchTypeId, out int inDock);
                if (inDock >= 1 && (inDock >= 2 || slotsAvailable >= 2))
                    dockMatches.Add(tile);
            }

            TileActor picked = null;
            if (dockMatches.Count > 0 && Random.value < 0.55f)
                picked = WeightedHumanTile(dockMatches);
            if (picked == null)
                picked = WeightedHumanTile(CollectSafeClickable(clickable, dockCounts, slotsAvailable));
            if (picked == null)
                return null;

            return CommitColor(picked.MatchTypeId, dockCounts, picked);
        }

        static List<TileActor> CollectSafeClickable(
            List<TileActor> clickable,
            Dictionary<int, int> dockCounts,
            int slotsAvailable)
        {
            var pool = new List<TileActor>();
            for (int i = 0; i < clickable.Count; i++)
            {
                TileActor tile = clickable[i];
                dockCounts.TryGetValue(tile.MatchTypeId, out int inDock);
                int need = PlayBotPlanner.MatchSize - inDock;
                if (need <= 1 || (need == 2 && slotsAvailable >= 2) || slotsAvailable >= PlayBotPlanner.MatchSize)
                    pool.Add(tile);
            }

            return pool.Count > 0 ? pool : clickable;
        }

        TileActor FindHumanClickableOfColor(List<TileActor> clickable, int colorId)
        {
            var pool = new List<TileActor>();
            for (int i = 0; i < clickable.Count; i++)
            {
                if (clickable[i].TileModel != null && clickable[i].MatchTypeId == colorId)
                    pool.Add(clickable[i]);
            }

            if (pool.Count == 0)
                return null;
            return WeightedHumanTile(pool);
        }

        TileActor WeightedHumanTile(List<TileActor> pool)
        {
            if (pool == null || pool.Count == 0)
                return null;
            if (pool.Count == 1)
                return pool[0];

            float sum = 0f;
            var weights = new float[pool.Count];
            for (int i = 0; i < pool.Count; i++)
            {
                weights[i] = HumanTileScore(pool[i]);
                sum += weights[i];
            }

            float roll = Random.Range(0f, sum);
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                    return pool[i];
            }

            return pool[pool.Count - 1];
        }

        float HumanTileScore(TileActor tile)
        {
            float score = 1f;
            if (_hasLastClick)
            {
                float distance = Vector3.Distance(tile.transform.position, _lastClickWorld);
                score += 2.4f / (0.35f + distance);
            }

            score += Mathf.Max(0, 8 - tile.LayerID) * 0.08f;
            return score;
        }

        static bool CanPlayerClick(TileActor tile)
        {
            return PlayBotPlanner.CanPlayerClick(tile);
        }

        void TrackProgress()
        {
            LevelGenService gen = LevelDirector.LevelGenService;
            int rest = gen != null ? gen.GetRestTileCount() : -1;
            if (rest >= 0 && _lastRestTiles >= 0 && rest >= _lastRestTiles)
                _noProgressClicks++;
            else
                _noProgressClicks = 0;

            _lastRestTiles = rest;
            if (_noProgressClicks >= 12)
            {
                _stuckTries = Mathf.Max(_stuckTries, 7);
                _noProgressClicks = 0;
            }
        }

        IEnumerator UseItem(PUType type)
        {
            if (PUDirector.ActivePowerUps == null)
                yield break;

            PUActor behavior = PUDirector.GetPowerUpBehavior(type);
            if (behavior == null)
                yield break;

            if (behavior.Settings.Save.Amount <= 0)
                PUDirector.AddPowerUp(type, 1);

            if (!PUDirector.UsePowerUp(type))
            {
                _status = $"{type} failed";
                yield return new WaitForSeconds(0.2f);
                yield break;
            }

            switch (type)
            {
                case PUType.Hint:
                    _hints++;
                    break;
                case PUType.Shuffle:
                    _shuffles++;
                    break;
                case PUType.Undo:
                    _undos++;
                    break;
            }

            ClearCommit();
            _status = $"Used {type}";
            PlayBotStepLog.Note($"Lv {DisplayLevel()} used {type}");
            Debug.Log($"[PlayBot] used {type} on lv {DisplayLevel()}");
            yield return new WaitForSeconds(type == PUType.Shuffle ? 1.4f : 1.05f);
        }

        IEnumerator HandleVictory()
        {
            _wins++;
            _status = $"Win {_wins}  next level";
            Debug.Log($"[PlayBot] win lv {DisplayLevel()}  clicks={_clicks}");
            PlayBotStepLog.Note($"Lv {DisplayLevel()} WIN clicks={_clicks}");
            yield return new WaitForSeconds(2.2f);

            const int retries = 6;
            for (int i = 0; i < retries && _running; i++)
            {
                if (!UIController.IsDisplayed<UIComplete>())
                    break;

                TryDismissBlockingPanel();

                UIComplete page = UIController.GetPage<UIComplete>();
                if (page != null)
                    page.NextLevelButton();

                yield return new WaitForSeconds(0.8f);
            }

            if (_running && UIController.IsDisplayed<UIComplete>())
            {
                TryDismissBlockingPanel();
                UIController.HidePage<UIComplete>(() => GameDirector.LoadNextLevel());
            }

            float elapsed = 0f;
            while (_running && UIController.IsDisplayed<UIComplete>() && elapsed < 3f)
            {
                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            ResetLevelTracking();
        }

        IEnumerator HandleFail()
        {
            _fails++;
            _status = $"Fail {_fails}  revive";
            Debug.Log($"[PlayBot] fail lv {DisplayLevel()}");
            PlayBotStepLog.Note($"Lv {DisplayLevel()} FAIL");
            yield return new WaitForSeconds(1.4f);

            if (!_running || !UIController.IsDisplayed<UIGameOver>())
                yield break;

            GameDirector.Revive();
            UIController.HidePage<UIGameOver>();
            UIController.ShowPage<UIGame>();
            ResetLevelTracking();
            yield return new WaitForSeconds(0.6f);
        }

        static int DisplayLevel()
        {
            return LevelDirector.DisplayedLevelIndex + 1;
        }

        static void TryDismissBlockingPanel()
        {
            if (GameModule.UI == null)
                return;
            GameModule.UI.HandleTopPanelEsc();
        }

        void SyncDebugLabels()
        {
            LevelGenService gen = LevelDirector.LevelGenService;
            List<TileActor> actTiles = gen != null ? gen.ActTiles : null;
            if (actTiles == null)
                return;

            Dictionary<int, int> dockCounts = PlayBotPlanner.CollectDockColorCounts();
            Dictionary<int, int> boardVisible = PlayBotPlanner.CountByColor(PlayBotPlanner.CollectClickableTiles());
            int slotsAvailable = DockActor.GetSlotsAvailable();

            for (int i = 0; i < actTiles.Count; i++)
            {
                TileActor tile = actTiles[i];
                if (tile == null || !tile.gameObject.activeInHierarchy)
                    continue;
                RefreshDebugLabel(tile, dockCounts, boardVisible, slotsAvailable, actTiles);
            }
        }

        void RefreshDebugLabel(
            TileActor tile,
            Dictionary<int, int> dockCounts,
            Dictionary<int, int> boardVisible,
            int slotsAvailable,
            List<TileActor> actTiles)
        {
            TextMeshPro tmp = GetOrCreateDebugLabel(tile);
            if (tmp == null)
                return;

            int code = tile.TileModel != null ? tile.MatchTypeId : -1;
            ElementPosition pos = tile.ElementPosition;
            bool planned = false;
            PlayBotPlanner.ColorPlan plan = default;
            if (CanPlayerClick(tile) && tile.TileModel != null)
            {
                planned = PlayBotPlanner.TryBuildPlan(
                    tile.MatchTypeId,
                    dockCounts,
                    boardVisible,
                    slotsAvailable,
                    actTiles,
                    out plan);
            }

            bool locked = _commitColorId >= 0 && code == _commitColorId;
            tmp.text = locked && CanPlayerClick(tile)
                ? $"{code} L\n{pos.X},{pos.Y},{pos.LayerId}"
                : planned && plan.IsLocalTriple
                    ? $"{code} 堆{plan.Pile}\n{pos.X},{pos.Y},{pos.LayerId}"
                    : planned
                    ? $"{code} n{plan.Need} b{plan.BoardVisible} d{plan.InDock}\n{pos.X},{pos.Y},{pos.LayerId}"
                    : CanPlayerClick(tile)
                        ? $"{code} R\n{pos.X},{pos.Y},{pos.LayerId}"
                        : $"{code}\n{pos.X},{pos.Y},{pos.LayerId}";

            if (tile.IsSubmitted)
                tmp.color = new Color(0.45f, 0.95f, 1f);
            else if (locked)
                tmp.color = new Color(0.2f, 1f, 0.55f);
            else if (planned && plan.IsLocalTriple)
                tmp.color = new Color(1f, 0.75f, 0.2f);
            else if (planned && plan.Need <= 1)
                tmp.color = new Color(0.35f, 0.95f, 0.45f);
            else if (planned && plan.Need == 2)
                tmp.color = new Color(0.45f, 0.85f, 1f);
            else if (planned && plan.Extra <= 0)
                tmp.color = new Color(1f, 0.92f, 0.15f);
            else if (planned && plan.BoardVisible >= 2)
                tmp.color = new Color(1f, 0.55f, 0.2f);
            else if (planned)
                tmp.color = new Color(0.85f, 0.55f, 1f);
            else if (CanPlayerClick(tile))
                tmp.color = new Color(0.85f, 0.85f, 0.85f);
            else
                tmp.color = Color.white;

            tmp.outlineWidth = 0.28f;
            tmp.outlineColor = Color.black;
        }

        static TextMeshPro GetOrCreateDebugLabel(TileActor tile)
        {
            Transform existing = tile.transform.Find(DebugLabelName);
            if (existing != null)
                return existing.GetComponent<TextMeshPro>();

            var go = new GameObject(DebugLabelName);
            go.transform.SetParent(tile.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.02f, -0.08f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.hideFlags = HideFlags.DontSave;

            var tmp = go.AddComponent<TextMeshPro>();
            if (TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = 2.4f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.rectTransform.sizeDelta = new Vector2(1.5f, 1f);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sortingOrder = DebugLabelSortingOrder;

            return tmp;
        }

        static void DestroyAllDebugLabels()
        {
            TextMeshPro[] labels = FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMeshPro tmp = labels[i];
                if (tmp == null || tmp.gameObject.name != DebugLabelName)
                    continue;
                Destroy(tmp.gameObject);
            }
        }

        bool ShouldShowHud()
        {
            if (!IsOnLevelPage())
                return false;
            return s_hudEnabled || _running;
        }

        static bool ShouldShowGmInfoHud()
        {
            if (!IsOnLevelPage())
                return false;
            // Independent of PlayBot HUD. Follow GM: default gmMode, or F1 opened GM this session.
            if (GameModule.Setting == null)
                return true;
            return GameModule.Setting.GmLevelPassGlobalSwitch;
        }

        static bool IsOnLevelPage()
        {
            UIGame page = UIController.GetPage<UIGame>();
            return page != null && page.IsPageDisplayed;
        }

        Rect ToggleRect()
        {
            return new Rect(_hudPos.x - HudToggleW, _hudPos.y, HudToggleW, HudToggleH);
        }

        // Device Simulator + Input System only: OnGUI never receives clicks, so GUI.Button is dead there.
        static bool TryReadPointer(out Vector2 gui, out bool down, out bool held, out bool up)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var press = touchscreen.primaryTouch.press;
                if (press.isPressed || press.wasPressedThisFrame || press.wasReleasedThisFrame)
                {
                    Vector2 p = touchscreen.primaryTouch.position.ReadValue();
                    gui = new Vector2(p.x, Screen.height - p.y);
                    down = press.wasPressedThisFrame;
                    held = press.isPressed;
                    up = press.wasReleasedThisFrame;
                    return true;
                }
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                gui = default;
                down = held = up = false;
                return false;
            }

            Vector2 m = mouse.position.ReadValue();
            gui = new Vector2(m.x, Screen.height - m.y);
            down = mouse.leftButton.wasPressedThisFrame;
            held = mouse.leftButton.isPressed;
            up = mouse.leftButton.wasReleasedThisFrame;
            return true;
        }

        float HudWidthNow()
        {
            return _hudCollapsed ? HudToggleW : HudWidth;
        }

        float HudHeightNow()
        {
            if (_hudCollapsed)
                return HudToggleH;
            return _running ? 278f : 128f;
        }

        void ResetHudDrag()
        {
            _hudPressing = false;
            _hudDragging = false;
        }

        void UpdateHudDrag()
        {
            if (!ShouldShowHud())
            {
                _hudTogglePress = false;
                ResetHudDrag();
                return;
            }

            if (!TryReadPointer(out Vector2 gui, out bool down, out bool held, out bool up))
                return;

            if (down && _hudRectValid && _hudRect.Contains(gui))
            {
                _hudPressing = true;
                _hudDragging = false;
                _hudPressRealtime = Time.realtimeSinceStartup;
                _hudDragOffset = gui - _hudPos;
                _hudTogglePress = ToggleRect().Contains(gui);
            }
            else if (down)
            {
                _hudTogglePress = false;
            }

            if (_hudPressing && held && !_hudDragging
                && Time.realtimeSinceStartup - _hudPressRealtime >= HudLongPressSeconds)
            {
                _hudDragging = true;
            }

            if (_hudDragging && held)
                _hudPos = gui - _hudDragOffset;

            if (up)
            {
                if (_hudTogglePress && !_hudDragging)
                    _hudCollapsed = !_hudCollapsed;
                _hudTogglePress = false;
                _hudBlockButtons = _hudDragging;
                ResetHudDrag();
            }
            else
            {
                _hudBlockButtons = _hudDragging;
            }
        }

        void DrawDifficultyLabel()
        {
            if (!ShouldShowGmInfoHud())
                return;

            var gen = LevelDirector.LevelGenService;
            if (gen == null)
                return;

            string text = gen.GetActiveDifficultyDebugLabel();
            if (string.IsNullOrEmpty(text))
                return;

            if (_gmInfoStyle == null)
            {
                _gmInfoStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.LowerRight,
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };
                _gmInfoStyle.normal.textColor = Color.black;
                _gmInfoStyle.hover.textColor = Color.black;
                _gmInfoStyle.active.textColor = Color.black;
            }

            const float margin = 18f;
            const float height = 36f;
            var rect = new Rect(margin, Screen.height - height - margin, Screen.width - margin * 2f, height);
            GUI.Label(rect, text, _gmInfoStyle);
        }

        void OnGUI()
        {
            DrawDifficultyLabel();

            if (!ShouldShowHud())
            {
                _hudRectValid = false;
                return;
            }

            float width = HudWidthNow();
            var rect = new Rect(_hudPos.x - width, _hudPos.y, width, HudHeightNow());
            _hudRect = rect;
            _hudRectValid = true;

            bool wasEnabled = GUI.enabled;
            if (_hudDragging || _hudBlockButtons)
            {
                GUI.enabled = false;
                GUIUtility.hotControl = 0;
            }

            GUI.Box(rect, GUIContent.none);

            if (!_hudCollapsed)
            {
                var inner = new Rect(rect.x + 8f, rect.y + 6f, HudWidth - 16f, rect.height - 12f);
                GUILayout.BeginArea(inner);
                GUILayout.Label(_running ? "PlayBot  ON" : "PlayBot  OFF");
                GUILayout.Label(_status);
                if (GUILayout.Button(_running ? "Stop (F9)" : "Start (F9)", GUILayout.Height(22f)))
                    Toggle();
                if (GUILayout.Button($"Style {_style}  (F7)", GUILayout.Height(22f)))
                    CycleStyle();
                if (_running)
                {
                    GUILayout.Label($"click {_clicks}  hint {_hints}  shuffle {_shuffles}  undo {_undos}");
                    GUILayout.Label($"win {_wins}  fail {_fails}  x{Time.timeScale:0.##}  (F8 speed)");
                    GUILayout.Label(StyleHint(_style));
                    GUILayout.Label("Console 过滤 PlayBotStep");
                }
                else
                {
                    GUILayout.Label("Enter a level, then start. Clicks real tiles.");
                }

                GUILayout.EndArea();
            }

            GUI.Label(ToggleRect(), _hudCollapsed ? "+" : "−", GUI.skin.button);

            GUI.enabled = wasEnabled;

            if (Event.current.type == EventType.Repaint)
                _hudBlockButtons = false;
        }

        static string StyleHint(PlayBotStyle style)
        {
            switch (style)
            {
                case PlayBotStyle.Perfect:
                    return "Perfect: min Need, then Extra; slots must fit";
                case PlayBotStyle.Skilled:
                    return "Skilled: same 4-axis pick, miss buried if occluded";
                case PlayBotStyle.Live:
                    return "Live: mostly visible Need, some wander";
                default:
                    return "Casual: weaker vision, more random clicks";
            }
        }
    }
}
#endif
