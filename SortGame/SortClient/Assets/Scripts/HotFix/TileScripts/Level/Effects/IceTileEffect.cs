using UnityEngine;
using TileGameCore;
using System;
using DG.Tweening;

namespace FishTilePlay
{

    public class IceTileEffect : TileEffect
    {
        [SerializeField] SpriteRenderer iceSpriteRenderer;
        [SerializeField] Sprite[] stageSprites;
        [SerializeField] Particle hitParticle;

        private SpriteRenderer iconSpriteRenderer;
        private TweenClip shakeTweenCase;

        private int stage;

        private static int particleHash;

        public override void Init()
        {
            particleHash = ParticlesDirector.RegisterParticle(hitParticle);
        }

        public override void OnCreated(TileActor tileBehavior)
        {
            iconSpriteRenderer = linkedTile.IconSpriteRenderer;

            stage = 0;
            iceSpriteRenderer.sprite = stageSprites[0];
            iceSpriteRenderer.sortingOrder = iconSpriteRenderer.sortingOrder + 1;
        }

        public override void OnSortingOrderChanged(int order)
        {
            iceSpriteRenderer.sortingOrder = linkedTile.IconSpriteRenderer.sortingOrder + 1;
        }

        public override void OnDisabled(TileActor tileBehavior)
        {
            shakeTweenCase.KillActive();
        }

        public override void OnAnyTileSubmitted(bool bFromHintItem = false)
        {
            if (linkedTile.IsClickable)
            {
                stage++;

                if (particleHash != -1)
                    ParticlesDirector.PlayParticle(particleHash).SetPosition(transform.position);

                shakeTweenCase = TileGameCore.Tween.Wrap(linkedTile.transform.DOShakePosition(0.05f, 0.08f));

                if (stageSprites.IsInRange(stage))
                {
                    iceSpriteRenderer.sprite = stageSprites[stage];
                }
                else
                {
                    DisableEffect();
                }

                AudioController.PlaySound(AudioController.AudioClips.iceCrackSound, volumePercentage: 0.5f, minDelay: 0.3f);
            }
            else
            {
                if (!LevelDirector.IsLevelCompletable())
                {
                    hitParticle.Play().SetPosition(transform.position);

                    shakeTweenCase = TileGameCore.Tween.Wrap(linkedTile.transform.DOShakePosition(0.05f, 0.08f));

                    DisableEffect();
                }
            }
        }

        public override void Clear()
        {
            shakeTweenCase.KillActive();

            Destroy(gameObject);
        }

        public override bool IsClickAllowed()
        {
            return false;
        }

        /// <summary>
        /// 获取Ice效果的序列化数据
        /// </summary>
        /// <returns>包含stage信息的JSON数据</returns>
        public override string GetEffectData()
        {
            var data = new EffectModel { stage = stage };
            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// 从序列化数据恢复Ice效果状态
        /// </summary>
        /// <param name="data">包含stage信息的JSON数据</param>
        public override void SetEffectData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;

            try
            {
                var effectData = JsonUtility.FromJson<EffectModel>(data);
                stage = effectData.stage;
                
                // 恢复对应的sprite状态
                if (stageSprites.IsInRange(stage))
                {
                    iceSpriteRenderer.sprite = stageSprites[stage];
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to restore Ice effect data: {e.Message}");
            }
        }

        [System.Serializable]
        private struct EffectModel
        {
            public int stage;
        }
    }
}
