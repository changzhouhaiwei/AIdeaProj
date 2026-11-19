using System;
using System.Collections.Generic;
using DG.Tweening;
using XSortGameCore;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 统一管理 Dock 的展示根节点与背景引用，并对「背景 + 槽位」做整体位移动画。
    /// 请将 <see cref="VisualRoot"/> 指定为背景与所有 Slot 的共同父节点；棋子仍在其它父节点下时，整体平移会通过世界位移增量同步到棋子。
    /// </summary>
    public sealed class DockVisualCluster
    {
        Transform visualRoot;
        Transform background;
        Func<IReadOnlyList<SlotActor>> getSlots;

        DG.Tweening.Tween activeTween;
        Vector3 syncLastWorldPos;

        /// <summary> 呼吸：仅用于 VisualRoot 相对基准的系数（供 <see cref="GetBreathingUniformFactor"/>）。 </summary>
        Vector3 breathingRootScaleBaseline = Vector3.one;

        public Transform VisualRoot => visualRoot;
        public Transform Background => background;

        public void Bind(Transform visualRoot, Transform background, Func<IReadOnlyList<SlotActor>> getSlots)
        {
            this.visualRoot = visualRoot != null ? visualRoot : background;
            this.background = background;
            this.getSlots = getSlots;
        }

        public void KillWholeTween()
        {
            if (activeTween != null && activeTween.IsActive())
                activeTween.Kill();
            activeTween = null;
        }

        public bool IsWholeTweenActive => activeTween != null && activeTween.IsActive();

        public void PauseWholeTween()
        {
            activeTween?.Pause();
        }

        public void ResumeWholeTween()
        {
            activeTween?.Play();
        }

        public IEnumerable<Transform> EnumeratePieceTransforms()
        {
            var list = getSlots?.Invoke();
            if (list == null) yield break;
            for (int i = 0; i < list.Count; i++)
            {
                var slot = list[i];
                if (slot == null || !slot.IsOccupied || slot.SlotCase?.Behavior == null) continue;
                var tr = slot.SlotCase.Behavior.Transform;
                if (tr != null) yield return tr;
            }
        }

        /// <summary>
        /// 对 VisualRoot 做 DOLocalMove；槽位为背景子级时会一并移动。已提交棋子若不在 VisualRoot 子层级下则按根节点世界位移同步。
        /// </summary>
        public TweenClip TweenWholeLocalPosition(Vector3 endLocal, float duration, DG.Tweening.Ease ease = DG.Tweening.Ease.OutQuad)
        {
            KillWholeTween();
            if (visualRoot == null) return null;

            syncLastWorldPos = visualRoot.position;
            var tw = visualRoot.DOLocalMove(endLocal, duration).SetEase(ease);
            tw.OnUpdate(SyncPiecesToRootWorldDelta);
            tw.OnKill(() => { if (activeTween == tw) activeTween = null; });
            tw.OnComplete(() => { if (activeTween == tw) activeTween = null; });
            activeTween = tw;
            return XSortGameCore.Tween.Wrap(tw);
        }

        /// <summary>
        /// 在当前局部坐标基础上平移 VisualRoot。
        /// </summary>
        public TweenClip TweenWholeLocalMove(Vector3 localDelta, float duration, DG.Tweening.Ease ease = DG.Tweening.Ease.OutQuad)
        {
            if (visualRoot == null) return null;
            return TweenWholeLocalPosition(visualRoot.localPosition + localDelta, duration, ease);
        }

        /// <summary>
        /// 对 VisualRoot 做局部缩放，子物体（含槽位）跟随。已挂在 VisualRoot 下的棋子会随父级缩放；否则仅槽位缩放，棋子需改层级或单独处理。
        /// </summary>
        public TweenClip TweenWholeLocalScale(Vector3 endLocalScale, float duration, DG.Tweening.Ease ease = DG.Tweening.Ease.OutQuad)
        {
            KillWholeTween();
            if (visualRoot == null) return null;

            var tw = visualRoot.DOScale(endLocalScale, duration).SetEase(ease);
            tw.OnKill(() => { if (activeTween == tw) activeTween = null; });
            tw.OnComplete(() => { if (activeTween == tw) activeTween = null; });
            activeTween = tw;
            return XSortGameCore.Tween.Wrap(tw);
        }

        /// <summary>
        /// 以当前 <see cref="VisualRoot"/> 的 localScale 为基准，在基准与 <paramref name="uniformScaleMultiplier"/> 倍之间无限 Yoyo 呼吸。
        /// 会终止同组内其它 whole tween（与 <see cref="TweenWholeLocalScale"/> 互斥）。
        /// </summary>
        public TweenClip TweenBreathingLocalScale(float uniformScaleMultiplier, float halfDuration, DG.Tweening.Ease ease = DG.Tweening.Ease.InOutSine)
        {
            KillWholeTween();
            if (visualRoot == null) return null;

            Vector3 baseline = visualRoot.localScale;
            breathingRootScaleBaseline = baseline;

            Vector3 peak = baseline * uniformScaleMultiplier;
            float half = Mathf.Max(0.05f, halfDuration);

            var tw = visualRoot
                .DOScale(peak, half)
                .SetEase(ease)
                .SetLoops(-1, LoopType.Yoyo);
            tw.OnKill(() => { if (activeTween == tw) activeTween = null; });
            tw.OnComplete(() => { if (activeTween == tw) activeTween = null; });
            activeTween = tw;
            return XSortGameCore.Tween.Wrap(tw);
        }

        float BreathingScaleFactor()
        {
            float bx = Mathf.Abs(breathingRootScaleBaseline.x) > 1e-6f ? breathingRootScaleBaseline.x : 1f;
            return visualRoot != null ? visualRoot.localScale.x / bx : 1f;
        }

        /// <summary> 当前 VisualRoot 相对呼吸基准的均匀缩放系数（仅在与警告呼吸 Tween 配合使用时读取）。 </summary>
        public float GetBreathingUniformFactor()
        {
            return BreathingScaleFactor();
        }

        void SyncPiecesToRootWorldDelta()
        {
            if (visualRoot == null) return;
            Vector3 now = visualRoot.position;
            Vector3 dw = now - syncLastWorldPos;
            syncLastWorldPos = now;
            if (dw.sqrMagnitude < 1e-16f) return;

            var list = getSlots?.Invoke();
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var slot = list[i];
                if (slot == null || !slot.IsOccupied || slot.SlotCase?.Behavior == null) continue;
                var tr = slot.SlotCase.Behavior.Transform;
                if (tr == null) continue;
                if (tr.IsChildOf(visualRoot)) continue;
                tr.position += dw;
            }
        }
    }
}
