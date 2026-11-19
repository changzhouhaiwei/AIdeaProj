using UnityEngine;
using XSortGameCore;

namespace FishXSortPlay
{
    public class CrateXSortEffect : XSortEffect
    {
        [SerializeField] Sprite crateXSortSprite;
        [SerializeField] Particle crateParticle;

        private Sprite storedSprite;
        private SpriteRenderer XSortSpriteRenderer;
        private SpriteRenderer iconSpriteRenderer;

        private static int particleHash;

        public override void Init()
        {
            particleHash = ParticlesDirector.RegisterParticle(crateParticle);
        }

        public override void OnCreated(XSortActor XSortBehavior)
        {
            iconSpriteRenderer = linkedXSort.IconSpriteRenderer;
            iconSpriteRenderer.enabled = false;

            XSortSpriteRenderer = linkedXSort.BackgroundSpriteRenderer;

            storedSprite = XSortSpriteRenderer.sprite;
            XSortSpriteRenderer.sprite = crateXSortSprite;
            XSortSpriteRenderer.SetActiveEx(true);
        }

        public override void OnDisabled(XSortActor XSortBehavior)
        {
            iconSpriteRenderer.enabled = true;
            XSortSpriteRenderer.sprite = storedSprite;
            XSortSpriteRenderer.SetActiveEx(false);
        }

        public override void OnAnyXSortSubmitted(bool bFromHintItem = false)
        {
            if(linkedXSort.IsClickable || !LevelDirector.IsLevelCompletable())
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
            XSortSpriteRenderer.sprite = storedSprite;
        }

        public override bool IsClickAllowed()
        {
            return false;
        }
    }
}
