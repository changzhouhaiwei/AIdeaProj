using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XSortGameCore;

namespace FishXSortPlay
{
    public class SlotCase
    {
        public IDockable Behavior { get; private set; }

        public TrailRenderer TrailRenderer;

        public bool IsBeingRemoved { get; set; }

        public bool IsMoving { get; set; }
        public DockMoveType MoveType { get; set; }

        private TweenCaseCollection elementMoveCases;

        public void AddTrail(GameObject trailObject)
        {
            if (trailObject == null || Behavior?.VisualsTransform == null)
                return;

            trailObject.transform.SetParent(Behavior.VisualsTransform);
            trailObject.transform.ResetLocal();
            trailObject.transform.localPosition = new Vector3(0, 0, 0.4f);

            TrailRenderer trailRenderer = trailObject.GetComponent<TrailRenderer>();
            if (trailRenderer == null)
                return;

            trailRenderer.Clear();
            trailRenderer.enabled = true;
            TrailRenderer = trailRenderer;
        }

        public SlotCase(IDockable behavior)
        {
            Behavior = behavior;
        }

        public void SubmitMove(Transform position, Vector3 scale, Vector3 rotation, int spriteSortOrder, bool instant = false)
        {
            IsMoving = true;
            MoveType = DockMoveType.Submit;

            if (LevelDifficultySim.IsRunning)
            {
                Behavior.Transform.position = position.position;
                OnMoveEnded(true, spriteSortOrder, true);
                return;
            }

            var distance = Vector3.Distance(position.position, Behavior.Transform.position);
            var duration = Mathf.Clamp(distance / 5f, 0.5f, 5f);

            if (elementMoveCases != null && !elementMoveCases.IsComplete()) elementMoveCases.Kill();
            elementMoveCases = Tween.BeginTweenCaseCollection();

            if (!instant)
            {
                // 使用传入的scale参数而不是LevelScaler.SlotSize，确保大小一致性
                Behavior.SubmitMove(position, scale, () => { OnMoveEnded(true, spriteSortOrder, false); });
                if (TrailRenderer != null)
                    TrailRenderer.sortingOrder = spriteSortOrder - 1;
            }
            else
            {
                Behavior.Transform.position = position.position;

                Tween.NextFrame(() => OnMoveEnded(true, spriteSortOrder, true));
            }

            Tween.EndTweenCaseCollection();
        }

        public void SubmitMove(Vector3 position, Vector3 scale, Vector3 rotation, int spriteSortOrder, bool instant = false)
        {
            IsMoving = true;
            MoveType = DockMoveType.Submit;

            if (LevelDifficultySim.IsRunning)
            {
                Behavior.Transform.position = position;
                OnMoveEnded(true, spriteSortOrder, true);
                return;
            }

            var distance = Vector3.Distance(position, Behavior.Transform.position);
            var duration = Mathf.Clamp(distance / 5f, 0.5f, 5f);

            if (elementMoveCases != null && !elementMoveCases.IsComplete()) elementMoveCases.Kill();
            elementMoveCases = Tween.BeginTweenCaseCollection();

            if (!instant)
            {
                // 使用传入的scale参数而不是LevelScaler.SlotSize，确保大小一致性
                Behavior.SubmitMove(position, scale, () => { OnMoveEnded(true, spriteSortOrder, false); });
                if (TrailRenderer != null)
                    TrailRenderer.sortingOrder = spriteSortOrder - 1;
            }
            else
            {
                Behavior.Transform.position = position;

                Tween.NextFrame(() => OnMoveEnded(true, spriteSortOrder, true));
            }

            Tween.EndTweenCaseCollection();
        }

        /// <param name="isInstantSubmit"> true 表示无飞行动画直接落槽（如存档恢复），不播撞击特效。 </param>
        private void OnMoveEnded(bool rotate, int spriteSortOrder = -1, bool isInstantSubmit = false)
        {
            if (rotate && TrailRenderer != null)
            {
                TrailRenderer.gameObject.SetActive(false);
                TrailRenderer.transform.SetParent(null);
            }

            if (MoveType == DockMoveType.Submit && Behavior != null)
            {
                DockActor.ParentSubmitPieceToDockVisualRoot(Behavior.Transform);
                if (!isInstantSubmit)
                    DockActor.PlaySubmitImpactAt(Behavior.Transform.position);
            }

            IsMoving = false;

            // 先压回槽位排序，再通知 Dock（可能立刻打开 Game Over），避免飞入高排序盖住失败 UI
            if (spriteSortOrder >= 0 && Behavior != null)
                Behavior.SetSortingOrder(spriteSortOrder);

            DockActor.OnMovementEnded(this, MoveType);
        }

        public void ShiftMove(Transform position)
        {
            IsMoving = true;
            MoveType = DockMoveType.Shift;

            if (LevelDifficultySim.IsRunning)
            {
                Behavior.Transform.position = position.position;
                OnMoveEnded(false);
                return;
            }

            if (elementMoveCases != null && !elementMoveCases.IsComplete()) elementMoveCases.Kill();

            elementMoveCases = Tween.BeginTweenCaseCollection();

            Behavior.DockShuffleMove(position, () => { OnMoveEnded(false); });

            Tween.EndTweenCaseCollection();
        }

        public void Clear(bool disableBlock = true)
        {
            if (elementMoveCases != null && !elementMoveCases.IsComplete()) elementMoveCases.Kill();

            if (disableBlock)
            {
                Behavior.Clear();
            }

            IsMoving = false;
            IsBeingRemoved = false;
        }
    }
}
