using UnityEngine;
using TileGameCore;

namespace FishTilePlay
{

    public class UnknownTileEffect : TileEffect, IStackOccludedTileEffect
    {
        [SerializeField] Sprite unknownSprite;
        [SerializeField] SpriteRenderer unknownOverlayRenderer;
        [SerializeField] Particle questionBoxParticle;

        private Sprite storedSprite;
        private SpriteRenderer iconSpriteRenderer;
        private static int particleHash;

        public Renderer OverlayRenderer => unknownOverlayRenderer;

        public override void Init()
        {
            particleHash = ParticlesDirector.RegisterParticle(questionBoxParticle);
        }

        public override void OnCreated(TileActor tileBehavior)
        {
            iconSpriteRenderer = linkedTile.IconSpriteRenderer;

            storedSprite = iconSpriteRenderer.sprite;
            iconSpriteRenderer.enabled = false;

            Sprite tileBaseUnknownSprite = TileSkinCatalog.LoadUnknownTileBaseSprite(LevelDirector.LoadedLevelTheme);
            unknownOverlayRenderer.sprite = tileBaseUnknownSprite != null ? tileBaseUnknownSprite : unknownSprite;
            unknownOverlayRenderer.sortingOrder = iconSpriteRenderer.sortingOrder + 1;
            unknownOverlayRenderer.enabled = false;
            RefreshVisibility();
        }

        public override void OnStateChanged(bool state)
        {
            RefreshVisibility();
        }

        public override void OnSortingOrderChanged(int order)
        {
            unknownOverlayRenderer.sortingOrder = linkedTile.IconSpriteRenderer.sortingOrder + 1;
        }

        public override void ApplyTintColor(Color color)
        {
            if (unknownOverlayRenderer != null)
                unknownOverlayRenderer.color = color;
        }

        public void RefreshVisibility()
        {
            if (unknownOverlayRenderer == null || iconSpriteRenderer == null || linkedTile == null)
            {
                return;
            }

            iconSpriteRenderer.enabled = false;

            LevelGenService levelGenFunc = LevelDirector.LevelGenService;
            if (levelGenFunc == null)
            {
                unknownOverlayRenderer.enabled = true;
                return;
            }

            bool blocked = TileStackUtil.HasStrongStackUpperBlock(linkedTile, levelGenFunc.ActTiles);
            unknownOverlayRenderer.enabled = !blocked;
        }

        public override void OnDisabled(TileActor tileBehavior)
        {
            iconSpriteRenderer.sprite = storedSprite;
            iconSpriteRenderer.enabled = true;
        }

        public override void OnTileSubmitted()
        {
            if (particleHash != -1 && linkedTile != null)
            {
                ParticlesDirector.PlayParticle(particleHash).SetPosition(linkedTile.transform.position);
                AudioController.PlaySound(AudioController.AudioClips.unknowOpenSound, volumePercentage: 0.5f);
            }

            DisableEffect();
        }

        public override void Clear()
        {
            iconSpriteRenderer.sprite = storedSprite;
            iconSpriteRenderer.enabled = true;
        }

        public override void SetEffectData(string data)
        {
            base.SetEffectData(data);
            RefreshVisibility();
        }

        public override string GetEffectData()
        {
            return "unknow";
        }

    }
}
