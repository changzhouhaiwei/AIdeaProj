using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TileGameCore;
using DG.Tweening;

namespace FishTilePlay
{
    [CreateAssetMenu(fileName = "Layer Spawn Animation", menuName = "Content/Level/Layer Spawn Animation")]
    public class LayerSpawnAnimation : LevelSpawnAnimation
    {
        private readonly Vector3[] OFFSET = new Vector3[] { new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(-1, 0, 0), new Vector3(0, -1, 0) };

        [SerializeField] float offsetMultiplier = 5;
        [SerializeField] float moveTime = 0.1f;
        [SerializeField] DG.Tweening.Ease moveEasing = DG.Tweening.Ease.OutBack;

        [Space]
        [SerializeField] float layerDelay = 0.2f;

        private TweenClip[] layerTweenCase;

        protected override IEnumerator SpawnLevelCoroutine(LevelGenService _levelGenFunc, SimpleCallback onAnimationCompleted)
        {
            int offsetDirectionIndex = 0;

            // Reset objects
            List<TileActor> tileBehaviors = _levelGenFunc.ActTiles;
            foreach (TileActor tileBehavior in tileBehaviors)
            {
                tileBehavior.transform.localScale = Vector3.one;
                tileBehavior.SetIsClickable(false, false);
            }

            LayersMap layers = _levelGenFunc.Layers;

            // Reset positions
            Vector3[] storedPositions = new Vector3[layers.Count];
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                Transform layerTransform = layers[i].LayerObject.transform;

                storedPositions[i] = layerTransform.localPosition;

                layerTransform.localPosition = OFFSET[offsetDirectionIndex % OFFSET.Length] * offsetMultiplier;

                offsetDirectionIndex++;
            }

            layerTweenCase = new TweenClip[storedPositions.Length];

            WaitForSeconds layerDelayYieldInstruction = new WaitForSeconds(layerDelay);

            // Reset offset index
            offsetDirectionIndex = 0;

            // Start animation
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                LayerBoard layer = layers[i];
                List<TileActor> layerTiles = new List<TileActor>();

                for (int x = 0; x < layer.Width; x++)
                {
                    for (int y = 0; y < layer.Height; y++)
                    {
                        if (layer[x, y].State)
                        {
                            layerTiles.Add(layer[x, y].Tile);
                        }
                    }
                }

                Transform layerTransform = layers[i].LayerObject.transform;
                layerTweenCase[i] = TileGameCore.Tween.Wrap(layerTransform.DOLocalMove(storedPositions[i], moveTime).SetEase(moveEasing));
                yield return layerDelayYieldInstruction;

                offsetDirectionIndex++;
            }

            BoxCoverManager.CompleteSpawnAnimation();
            onAnimationCompleted?.Invoke();
        }

        public override void Clear()
        {
            base.Clear();

            layerTweenCase.KillActive();
        }
    }
}