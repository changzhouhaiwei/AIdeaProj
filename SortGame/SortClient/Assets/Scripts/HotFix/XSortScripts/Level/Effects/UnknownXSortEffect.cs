using UnityEngine;
using XSortGameCore;

namespace FishXSortPlay
{

    public class UnknownXSortEffect : XSortEffect, IStackOccludedXSortEffect
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

        public override void OnCreated(XSortActor XSortBehavior)
        {
            iconSpriteRenderer = linkedXSort.IconSpriteRenderer;

            storedSprite = iconSpriteRenderer.sprite;
            iconSpriteRenderer.enabled = false;

            Sprite XSortBaseUnknownSprite = XSortSkinCatalog.LoadUnknownXSortBaseSprite(LevelDirector.LoadedLevelTheme);
            unknownOverlayRenderer.sprite = XSortBaseUnknownSprite != null ? XSortBaseUnknownSprite : unknownSprite;
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
            unknownOverlayRenderer.sortingOrder = linkedXSort.IconSpriteRenderer.sortingOrder + 1;
        }

        public override void ApplyTintColor(Color color)
        {
            if (unknownOverlayRenderer != null)
                unknownOverlayRenderer.color = color;
        }

        public void RefreshVisibility()
        {
            if (unknownOverlayRenderer == null || iconSpriteRenderer == null || linkedXSort == null)
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

            bool blocked = XSortStackUtil.HasStrongStackUpperBlock(linkedXSort, levelGenFunc.ActXSorts);
            unknownOverlayRenderer.enabled = !blocked;
        }

        public override void OnDisabled(XSortActor XSortBehavior)
        {
            iconSpriteRenderer.sprite = storedSprite;
            iconSpriteRenderer.enabled = true;
        }

        public override void OnXSortSubmitted()
        {
            if (particleHash != -1 && linkedXSort != null)
            {
                ParticlesDirector.PlayParticle(particleHash).SetPosition(linkedXSort.transform.position);
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
