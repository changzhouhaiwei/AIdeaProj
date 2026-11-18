using UnityEngine;
using TileGameCore;

namespace FishTilePlay
{
    public class CrateTileEffect : TileEffect
    {
        [SerializeField] Sprite crateTileSprite;
        [SerializeField] Particle crateParticle;

        private Sprite storedSprite;
        private SpriteRenderer tileSpriteRenderer;
        private SpriteRenderer iconSpriteRenderer;

        private static int particleHash;

        public override void Init()
        {
            particleHash = ParticlesDirector.RegisterParticle(crateParticle);
        }

        public override void OnCreated(TileActor tileBehavior)
        {
            iconSpriteRenderer = linkedTile.IconSpriteRenderer;
            iconSpriteRenderer.enabled = false;

            tileSpriteRenderer = linkedTile.BackgroundSpriteRenderer;

            storedSprite = tileSpriteRenderer.sprite;
            tileSpriteRenderer.sprite = crateTileSprite;
            tileSpriteRenderer.SetActiveEx(true);
        }

        public override void OnDisabled(TileActor tileBehavior)
        {
            iconSpriteRenderer.enabled = true;
            tileSpriteRenderer.sprite = storedSprite;
            tileSpriteRenderer.SetActiveEx(false);
        }

        public override void OnAnyTileSubmitted(bool bFromHintItem = false)
        {
            if(linkedTile.IsClickable || !LevelDirector.IsLevelCompletable())
            {
                if (particleHash != -1)
                    ParticlesDirector.PlayParticle(particleHash).SetPosition(transform.position);
                
                DisableEffect();

                AudioController.PlaySound(AudioController.AudioClips.crateCrackSound, volumePercentage: 0.5f, minDelay: 0.3f);
            }
        }

        public override void Clear()
        {
            iconSpriteRenderer.enabled = true;
            tileSpriteRenderer.sprite = storedSprite;
        }

        public override bool IsClickAllowed()
        {
            return false;
        }
    }
}
