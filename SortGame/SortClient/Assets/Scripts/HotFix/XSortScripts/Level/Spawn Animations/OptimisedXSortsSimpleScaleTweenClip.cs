using System.Collections.Generic;
using UnityEngine;
using XSortGameCore;
using DG.Tweening;

namespace FishXSortPlay
{
    /// <summary>
    /// 使用 DOTween 实现的分层棋子缩放动画
    /// </summary>
    public class OptimisedXSortsSimpleScaleTweenClip : TweenCaseDOTween
    {
        private const float kSpawnVisibleScaleStart = 0.1f;

        public Vector3 targetScale { get; }

        public OptimisedXSortsSimpleScaleTweenClip(LevelGenService _levelGenFunc, float scaleDuration, float elementDelay, float layerDelay, Ease scaleEase)
            : base(BuildSequence(_levelGenFunc, scaleDuration, elementDelay, layerDelay, scaleEase))
        {
            targetScale = Vector3.one;
        }

        static bool IsXSortAlive(XSortActor XSort) =>
            XSort != null && XSort.gameObject != null;

        static bool IsBoxAlive(BoxCoverBehavior box) =>
            box != null && box.gameObject != null;

        private static DG.Tweening.Tween BuildSequence(LevelGenService LevelGenService, float scaleDuration, float elementDelay, float layerDelay, Ease scaleEase)
        {
            if (LevelGenService?.Layers == null)
                return DOTween.Sequence();

            var layers = LevelGenService.Layers;

            var XSortInfos = new List<(XSortActor XSort, float startTime)>();
            var boxInfos = new List<(BoxCoverBehavior box, float startTime)>();
            var eggInfos = new List<(AwardColorEggCoverBehavior egg, float startTime)>();
            var layerSoundTimes = new List<float>();
            var boxesByLayer = new Dictionary<int, List<BoxCoverBehavior>>();
            var eggsByLayer = new Dictionary<int, List<AwardColorEggCoverBehavior>>();
            CollectBoxesByLayer(boxesByLayer);
            CollectEggsByLayer(eggsByLayer);

            float delay = 0;
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                LayerBoard layer = layers[i];
                bool layerHadXSort = false;

                if (boxesByLayer.TryGetValue(i, out List<BoxCoverBehavior> layerBoxes))
                {
                    for (int boxIndex = 0; boxIndex < layerBoxes.Count; boxIndex++)
                    {
                        BoxCoverBehavior box = layerBoxes[boxIndex];
                        if (!IsBoxAlive(box))
                            continue;

                        box.PrepareForSpawnAnimation();
                        boxInfos.Add((box, delay));
                    }

                    boxesByLayer.Remove(i);
                }

                if (eggsByLayer.TryGetValue(i, out List<AwardColorEggCoverBehavior> layerEggs))
                {
                    for (int eggIndex = 0; eggIndex < layerEggs.Count; eggIndex++)
                    {
                        AwardColorEggCoverBehavior egg = layerEggs[eggIndex];
                        if (egg == null)
                            continue;
                        egg.PrepareForSpawnAnimation();
                        eggInfos.Add((egg, delay));
                    }

                    eggsByLayer.Remove(i);
                }

                for (int x = 0; x < layer.Width; x++)
                {
                    bool isPlaced = false;
                    for (int y = 0; y < layer.Height; y++)
                    {
                        if (layer[x, y].State)
                        {
                            XSortActor XSort = layer[x, y].XSort;
                            if (XSort == null) continue;

                            XSort.transform.localScale = Vector3.zero;
                            XSort.SetSpawnTweenVisualsActive(false);
                            XSort.SetIsClickable(true, false);

                            float startTime = delay;
                            XSortInfos.Add((XSort, startTime));
                            isPlaced = true;
                        }
                    }
                    if (isPlaced) { delay += elementDelay; layerHadXSort = true; }
                }
                if (layerHadXSort)
                    layerSoundTimes.Add(delay);
                delay += layerDelay;
            }

            foreach (KeyValuePair<int, List<BoxCoverBehavior>> leftover in boxesByLayer)
            {
                List<BoxCoverBehavior> leftoverBoxes = leftover.Value;
                for (int boxIndex = 0; boxIndex < leftoverBoxes.Count; boxIndex++)
                {
                    BoxCoverBehavior box = leftoverBoxes[boxIndex];
                    if (!IsBoxAlive(box))
                        continue;

                    box.PrepareForSpawnAnimation();
                    boxInfos.Add((box, delay));
                }
            }

            var seq = DOTween.Sequence().SetRecyclable(true);
            float lastSoundTime = 0;
            int soundCnt = 0;

            for (int i = 0; i < XSortInfos.Count; i++)
            {
                var (XSort, startTime) = XSortInfos[i];
                if (XSort == null || XSort.transform == null) continue;

                var perXSort = DOTween.Sequence().SetRecyclable(true);
                perXSort.AppendCallback(() =>
                {
                    if (!IsXSortAlive(XSort))
                        return;
                    XSort.SetSpawnTweenVisualsActive(true);
                    XSort.transform.localScale = Vector3.one * kSpawnVisibleScaleStart;
                });
                perXSort.Append(XSort.transform.DOScale(1f, scaleDuration)
                    .SetEase(scaleEase)
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

            for (int i = 0; i < boxInfos.Count; i++)
            {
                var (box, startTime) = boxInfos[i];
                if (!IsBoxAlive(box) || box.transform == null)
                    continue;

                var perBox = DOTween.Sequence().SetRecyclable(true);
                perBox.AppendCallback(() =>
                {
                    if (!IsBoxAlive(box))
                        return;
                    box.SetSpawnTweenVisualsActive(true);
                    box.transform.localScale = Vector3.one * kSpawnVisibleScaleStart;
                });
                perBox.Append(box.transform.DOScale(1f, scaleDuration).SetEase(scaleEase));
                perBox.SetLink(box.gameObject, LinkBehaviour.KillOnDestroy);
                seq.Insert(startTime, perBox);
            }

            for (int i = 0; i < eggInfos.Count; i++)
            {
                var (egg, startTime) = eggInfos[i];
                if (egg == null || egg.transform == null)
                    continue;

                var perEgg = DOTween.Sequence().SetRecyclable(true);
                perEgg.AppendCallback(() =>
                {
                    if (egg == null)
                        return;
                    egg.SetSpawnTweenVisualsActive(true);
                    egg.transform.localScale = Vector3.one * kSpawnVisibleScaleStart;
                });
                perEgg.Append(egg.transform.DOScale(1f, scaleDuration).SetEase(scaleEase));
                perEgg.SetLink(egg.gameObject, LinkBehaviour.KillOnDestroy);
                seq.Insert(startTime, perEgg);
            }

            seq.OnKill(() =>
            {
                for (int j = 0; j < XSortInfos.Count; j++)
                {
                    var t = XSortInfos[j].XSort;
                    if (!IsXSortAlive(t))
                        continue;
                    t.SetSpawnTweenVisualsActive(true);
                    // Kill mid-spawn can leave scale at 0; restore so XSorts stay visible/clickable.
                    if (t.transform.localScale.x < 0.5f)
                        t.transform.localScale = Vector3.one;
                }

                for (int j = 0; j < boxInfos.Count; j++)
                {
                    BoxCoverBehavior box = boxInfos[j].box;
                    if (!IsBoxAlive(box))
                        continue;
                    // Must restore scale too — visuals-on + scale 0 = invisible blocker.
                    box.CompleteSpawnAnimation();
                }

                AwardColorEggCoverManager.CompleteSpawnAnimation();
            });

            for (int i = 0; i < layerSoundTimes.Count; i++)
            {
                float callbackTime = layerSoundTimes[i];
                seq.InsertCallback(callbackTime, () =>
                {
                    if (AudioController.AudioClips != null && AudioController.AudioClips.pockerSound != null)
                        AudioController.PlaySound(AudioController.AudioClips.pockerSound);
                });
            }

            return seq;
        }

        static void CollectEggsByLayer(Dictionary<int, List<AwardColorEggCoverBehavior>> eggsByLayer)
        {
            List<AwardColorEggCoverBehavior> eggs = new();
            AwardColorEggCoverManager.CollectActiveBehaviors(eggs);
            for (int i = 0; i < eggs.Count; i++)
            {
                AwardColorEggCoverBehavior egg = eggs[i];
                if (egg == null)
                    continue;
                int layer = egg.Placement.layer;
                if (!eggsByLayer.TryGetValue(layer, out List<AwardColorEggCoverBehavior> layerEggs))
                {
                    layerEggs = new List<AwardColorEggCoverBehavior>();
                    eggsByLayer.Add(layer, layerEggs);
                }

                layerEggs.Add(egg);
            }
        }

        static void CollectBoxesByLayer(Dictionary<int, List<BoxCoverBehavior>> boxesByLayer)
        {
            var boxes = new List<BoxCoverBehavior>();
            BoxCoverManager.CollectActiveBehaviors(boxes);
            for (int i = 0; i < boxes.Count; i++)
            {
                BoxCoverBehavior box = boxes[i];
                if (!IsBoxAlive(box))
                    continue;

                int layer = box.Placement.layer;
                if (!boxesByLayer.TryGetValue(layer, out List<BoxCoverBehavior> layerBoxes))
                {
                    layerBoxes = new List<BoxCoverBehavior>();
                    boxesByLayer.Add(layer, layerBoxes);
                }

                layerBoxes.Add(box);
            }
        }
    }
}
