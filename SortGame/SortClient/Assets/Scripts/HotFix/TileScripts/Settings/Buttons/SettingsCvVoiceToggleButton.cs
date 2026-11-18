#pragma warning disable 649

using UnityEngine;
using UnityEngine.UI;
using TileGameCore;

namespace FishTilePlay
{
    public class SettingsCvVoiceToggleButton : SettingsButtonBase
    {
        [SerializeField] Image imageRef;
        [SerializeField] Image selectionImage;

        [Space]
        [SerializeField] Sprite activeSprite;
        [SerializeField] Sprite disableSprite;

        private bool isActive = true;
        private TweenClip selectionFadeCase;

        public override void Init()
        {
        }

        private void OnEnable()
        {
            isActive = CvVoiceSettings.IsEnabled;
            Redraw();
        }

        private void Redraw()
        {
            if (imageRef != null)
                imageRef.sprite = isActive ? activeSprite : disableSprite;
        }

        public override void OnClick()
        {
            isActive = !isActive;
            CvVoiceSettings.IsEnabled = isActive;
            Redraw();
            AudioController.PlaySound(AudioController.AudioClips.buttonSoundSpread);
        }

        public override void Select()
        {
            IsSelected = true;
            selectionFadeCase.KillActive();
            selectionImage.gameObject.SetActive(true);
            selectionImage.color = selectionImage.color.SetAlpha(0.0f);
            selectionFadeCase = selectionImage.DOFade(0.2f, 0.2f);
        }

        public override void Deselect()
        {
            IsSelected = false;
            selectionFadeCase.KillActive();
            selectionImage.gameObject.SetActive(false);
            selectionImage.color = selectionImage.color.SetAlpha(0.0f);
        }
    }
}
