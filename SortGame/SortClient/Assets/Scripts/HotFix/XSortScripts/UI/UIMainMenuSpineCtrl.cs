using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 主菜单 Spine：Inspector 配置进场/循环；可选仅布局节点（只参与激活与位置恢复）；支持运行时 AddSpine。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIMainMenuSpineCtrl : MonoBehaviour
    {
        [Serializable]
        public class SpineAnimEntry
        {
            public SkeletonGraphic graphic;
            public string enterAnimationName;
            public string loopAnimationName;
            /// <summary>显示前短暂 SetActive(false) 再打开，用于刷新 SkeletonGraphic（如标题）。</summary>
            public bool refreshBeforePlay;
        }

        [SerializeField] private List<SpineAnimEntry> _entries = new List<SpineAnimEntry>();
        [SerializeField] private List<SkeletonGraphic> _layoutOnlyGraphics = new List<SkeletonGraphic>();

        private readonly List<SpineAnimEntry> _runtimeEntries = new List<SpineAnimEntry>();

        private readonly List<SkeletonGraphic> _posCacheGraphics = new List<SkeletonGraphic>();
        private readonly List<Vector3> _posCacheWorld = new List<Vector3>();

        /// <summary>运行时追加一条 Spine（进场 → 循环）。</summary>
        public void AddSpine(SkeletonGraphic graphic, string enterAnimationName, string loopAnimationName, bool refreshBeforePlay = false)
        {
            if (graphic == null) return;
            _runtimeEntries.Add(new SpineAnimEntry
            {
                graphic = graphic,
                enterAnimationName = enterAnimationName,
                loopAnimationName = loopAnimationName,
                refreshBeforePlay = refreshBeforePlay
            });
        }

        public void ClearRuntimeEntries()
        {
            _runtimeEntries.Clear();
        }

        public IEnumerable<SpineAnimEntry> EnumerateAllEntries()
        {
            for (int i = 0; i < _entries.Count; i++)
                yield return _entries[i];
            for (int i = 0; i < _runtimeEntries.Count; i++)
                yield return _runtimeEntries[i];
        }

        /// <summary>统一设置本控制器管理的 SkeletonGraphic 组件的 enabled（PlayShow 为 true，隐藏界面为 false）。</summary>
        public void SetSpineGraphicsEnabled(bool enabled)
        {
            foreach (var entry in EnumerateAllEntries())
            {
                if (entry.graphic != null)
                    entry.graphic.enabled = enabled;
            }
            for (int i = 0; i < _layoutOnlyGraphics.Count; i++)
            {
                var g = _layoutOnlyGraphics[i];
                if (g != null)
                    g.enabled = enabled;
            }
        }

        /// <summary>显示前：先关闭渲染避免首帧闪现；缓存世界坐标、全部 SetActive(true)、对需刷新的条目先关再开前的关。</summary>
        public void OnShowPrepare()
        {
            SetSpineGraphicsEnabled(false);
            CachePositions();
            SetAllGraphicsActive(true);
            foreach (var entry in EnumerateAllEntries())
            {
                if (entry.graphic == null || !entry.refreshBeforePlay) continue;
                entry.graphic.SetActiveEx(false);
            }
        }

        /// <summary>延迟后：恢复坐标、打开需刷新的条目、播放全部动画（首帧就绪后再开启渲染）。</summary>
        public void OnShowDelayedRestoreAndPlay()
        {
            RestorePositions();
            foreach (var entry in EnumerateAllEntries())
            {
                if (entry.graphic == null || !entry.refreshBeforePlay) continue;
                entry.graphic.SetActiveEx(true);
            }
            PlaySpineSequence();
            SetSpineGraphicsEnabled(true);
        }

        private void CachePositions()
        {
            _posCacheGraphics.Clear();
            _posCacheWorld.Clear();
            foreach (var entry in EnumerateAllEntries())
            {
                if (entry.graphic == null) continue;
                _posCacheGraphics.Add(entry.graphic);
                _posCacheWorld.Add(entry.graphic.transform.position);
            }
            for (int i = 0; i < _layoutOnlyGraphics.Count; i++)
            {
                var g = _layoutOnlyGraphics[i];
                if (g == null) continue;
                _posCacheGraphics.Add(g);
                _posCacheWorld.Add(g.transform.position);
            }
        }

        private void RestorePositions()
        {
            for (int i = 0; i < _posCacheGraphics.Count; i++)
            {
                var g = _posCacheGraphics[i];
                if (g == null) continue;
                g.transform.position = _posCacheWorld[i];
            }
        }

        private void SetAllGraphicsActive(bool active)
        {
            foreach (var entry in EnumerateAllEntries())
            {
                if (entry.graphic != null)
                    entry.graphic.SetActiveEx(active);
            }
            for (int i = 0; i < _layoutOnlyGraphics.Count; i++)
            {
                var g = _layoutOnlyGraphics[i];
                if (g != null)
                    g.SetActiveEx(active);
            }
        }

        public void PlaySpineSequence()
        {
            foreach (var entry in EnumerateAllEntries())
                PlayEntry(entry);
        }

        private static void PlayEntry(SpineAnimEntry entry)
        {
            if (entry.graphic == null) return;
            if (string.IsNullOrEmpty(entry.enterAnimationName))
                return;

            var g = entry.graphic;
            g.AnimationState.ClearTrack(0);
            g.AnimationState.SetAnimation(0, entry.enterAnimationName, false);
            if (!string.IsNullOrEmpty(entry.loopAnimationName))
                g.AnimationState.AddAnimation(0, entry.loopAnimationName, true, 0f);
            // 立即应用第 0 帧，避免先显示 setup pose 再进动画造成闪一下（与 SpineSequencePlayer.RefreshFrameZero 一致）
            g.Update(0f);
        }

        public void StopAll()
        {
            foreach (var entry in EnumerateAllEntries())
            {
                if (entry.graphic == null) continue;
                var g = entry.graphic;
                g.AnimationState.ClearTrack(0);
                g.Skeleton.SetToSetupPose();
                g.Update(0);
            }
        }

        private void OnDestroy()
        {
            StopAll();
        }
    }
}
