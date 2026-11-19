using UnityEngine;
using System.Collections.Generic;
using XSortGameCore;
using DG.Tweening;

namespace FishXSortPlay
{
    /// <summary>
    /// 使用 DOTween 实现的批量棋子缩放动画
    /// </summary>
    public class OptimisedXSortsScaleTweenClip : TweenCaseDOTween
    {
        /// <summary>登场可见起始均匀缩放，避免从 0 插值时 Back 等缓动出现负分量。</summary>
        private const float kSpawnVisibleScaleStart = 0.1f;

        public Vector3 targetScale => Vector3.one;

        public OptimisedXSortsScaleTweenClip(List<XSortActor> XSorts, float scaleDuration, float minDelay, float maxDelay, DG.Tweening.Ease easeType = DG.Tweening.Ease.OutSine)
            : base(BuildSequence(XSorts, scaleDuration, minDelay, maxDelay, easeType))
        {
        }

        static bool IsXSortAlive(XSortActor XSort) =>
            XSort != null && XSort.gameObject != null;

        private static DG.Tweening.Tween BuildSequence(List<XSortActor> XSorts, float scaleDuration, float minDelay, float maxDelay, DG.Tweening.Ease easeType)
        {
            var delays = new float[XSorts.Count];

            float longestDelay = 0;
            for (int i = 0; i < delays.Length; i++)
            {
                float delay = Random.Range(minDelay, maxDelay);
                if (delay > longestDelay)
                    longestDelay = delay;
                delays[i] = delay;
            }

            float totalDuration = scaleDuration + longestDelay;
            for (int i = 0; i < delays.Length; i++)
                delays[i] = delays[i] / totalDuration;

            var seq = DOTween.Sequence().SetRecyclable(true);
            float lastSoundTime = 0;
            int soundCnt = 0;

            for (int i = 0; i < XSorts.Count; i++)
            {
                XSortActor XSort = XSorts[i];
                if (XSort == null || XSort.transform == null) continue;

                XSort.transform.localScale = Vector3.zero;
                XSort.SetSpawnTweenVisualsActive(false);
                float startTime = delays[i] * totalDuration;

                var perXSort = DOTween.Sequence().SetRecyclable(true);
                perXSort.AppendCallback(() =>
                {
                    if (!IsXSortAlive(XSort))
                        return;
                    XSort.SetSpawnTweenVisualsActive(true);
                    XSort.transform.localScale = Vector3.one * kSpawnVisibleScaleStart;
                });
                perXSort.Append(XSort.transform.DOScale(1f, scaleDuration)
                    .SetEase(easeType)
                    .OnStart(() =>
                    {
                        if (AudioController.AudioClips != null && AudioController.AudioClips.pockerSound != null)
                        {
                            if (soundCnt < 7 && Time.timeSinceLevelLoad - lastSoundTime >= 0.1f)
                            {
                                soundCnt++;
                                AudioController.PlaySound(AudioController.AudioClips.pockerSound);
                                lastSoundTime = Time.timeSinceLevelLoad;
                            }
                        }
                    }));
                perXSort.SetLink(XSort.gameObject, LinkBehaviour.KillOnDestroy);
                seq.Insert(startTime, perXSort);
            }

            seq.AppendCallback(() =>
            {
                LevelDirector.LevelGenService?.RefreshStackOccludedEffectsAfterBoardChange();
            });

            seq.OnKill(() =>
            {
                for (int j = 0; j < XSorts.Count; j++)
                {
                    var t = XSorts[j];
                    if (!IsXSortAlive(t))
                        continue;
                    t.SetSpawnTweenVisualsActive(true);
                }

                LevelDirector.LevelGenService?.RefreshStackOccludedEffectsAfterBoardChange();
            });

            return seq;
        }
    }
}
