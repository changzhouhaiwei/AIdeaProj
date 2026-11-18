#pragma warning disable 649

using System;
using UnityEngine;
using UnityEngine.UI;
using TileGameCore;

namespace FishTilePlay
{
    public class SettingsSoundToggleButton : SettingsButtonBase
    {
        [SerializeField] bool universal;

        [HideIf("universal")]
        [SerializeField] TileGameCore.AudioType type;

        [Space]
        [SerializeField] Image imageRef;
        [SerializeField] Image selectionImage;

        [Space]
        [SerializeField] Sprite activeSprite;
        [SerializeField] Sprite disableSprite;

        private bool isActive = true;

        private TileGameCore.AudioType[] availableAudioTypes;
        private TweenClip selectionFadeCase;

        public override void Init()
        {
            if(universal)
            {
                availableAudioTypes = EnumUtils.GetEnumArray<TileGameCore.AudioType>();
            }
        }

        private void OnEnable()
        {
            isActive = GetState();

            Redraw();

            TileGameCore.AudioController.VolumeChanged += OnVolumeChanged;
        }

        private void OnDisable()
        {
            TileGameCore.AudioController.VolumeChanged -= OnVolumeChanged;
            // TileGameCore.AudioController.VolumeChanged -= OnVolumeChanged;
        }

        private void Redraw()
        {
            imageRef.sprite = isActive ? activeSprite : disableSprite;
        }

        public override void OnClick()
        {
            isActive = !isActive;

            SetState(isActive);

            Redraw();

            // 立即保存音效设置，确保下次启动时生效
            TileGameCore.SaveController.Save(true);

            if (AudioController.IsAudioTypeActive(TileGameCore.AudioType.Sound))
                AudioController.PlaySound(AudioController.AudioClips.buttonSoundSpread);
        }

        private void OnVolumeChanged(TileGameCore.AudioType audioType, float volume)
        {
            if (universal || audioType == type)
            {
                isActive = GetState();

                Redraw();
            }
        }

        private bool GetState()
        {
            if(universal)
            {
                foreach(TileGameCore.AudioType audioType in availableAudioTypes)
                {
                    if (!AudioController.IsAudioTypeActive(audioType))
                        return false;
                }

                return true;
            }

            return AudioController.IsAudioTypeActive(type);
        }

        private void SetState(bool state)
        {
            float volume = state ? 1.0f : 0.0f;

            if (universal)
            {
                foreach (TileGameCore.AudioType audioType in availableAudioTypes)
                {
                    AudioController.SetVolume(audioType, volume);
                }

                return;
            }

            AudioController.SetVolume(type, volume);
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