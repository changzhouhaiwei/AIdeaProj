using UnityEngine;
using System.Collections.Generic;
using TileGameCore;
using DG.Tweening;

namespace FishTilePlay
{
    /// <summary>
    /// 使用 DOTween 实现的批量棋子缩放动画
    /// </summary>
    public class OptimisedTilesScaleTweenClip : TweenCaseDOTween
    {
        /// <summary>登场可见起始均匀缩放，避免从 0 插值时 Back 等缓动出现负分量。</summary>
        private const float kSpawnVisibleScaleStart = 0.1f;

        public Vector3 targetScale => Vector3.one;

        public OptimisedTilesScaleTweenClip(List<TileActor> tiles, float scaleDuration, float minDelay, float maxDelay, DG.Tweening.Ease easeType = DG.Tweening.Ease.OutSine)
            : base(BuildSequence(tiles, scaleDuration, minDelay, maxDelay, easeType))
        {
        }

        static bool IsTileAlive(TileActor tile) =>
            tile != null && tile.gameObject != null;

        private static DG.Tweening.Tween BuildSequence(List<TileActor> tiles, float scaleDuration, float minDelay, float maxDelay, DG.Tweening.Ease easeType)
        {
            var delays = new float[tiles.Count];

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

            for (int i = 0; i < tiles.Count; i++)
            {
                TileActor tile = tiles[i];
                if (tile == null || tile.transform == null) continue;

                tile.transform.localScale = Vector3.zero;
                tile.SetSpawnTweenVisualsActive(false);
                float startTime = delays[i] * totalDuration;

                var perTile = DOTween.Sequence().SetRecyclable(true);
                perTile.AppendCallback(() =>
                {
                    if (!IsTileAlive(tile))
                        return;
                    tile.SetSpawnTweenVisualsActive(true);
                    tile.transform.localScale = Vector3.one * kSpawnVisibleScaleStart;
                });
                perTile.Append(tile.transform.DOScale(1f, scaleDuration)
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
                perTile.SetLink(tile.gameObject, LinkBehaviour.KillOnDestroy);
                seq.Insert(startTime, perTile);
            }

            seq.AppendCallback(() =>
            {
                LevelDirector.LevelGenService?.RefreshStackOccludedEffectsAfterBoardChange();
            });

            seq.OnKill(() =>
            {
                for (int j = 0; j < tiles.Count; j++)
                {
                    var t = tiles[j];
                    if (!IsTileAlive(t))
                        continue;
                    t.SetSpawnTweenVisualsActive(true);
                }

                LevelDirector.LevelGenService?.RefreshStackOccludedEffectsAfterBoardChange();
            });

            return seq;
        }
    }
}
