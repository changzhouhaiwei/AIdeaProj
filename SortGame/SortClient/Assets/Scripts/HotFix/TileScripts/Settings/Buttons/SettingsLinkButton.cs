#pragma warning disable 0649 
using TileGameCore;
using UnityEngine;

namespace FishTilePlay
{
    public class SettingsLinkButton : SettingsButtonBase
    {
        [SerializeField] string url;

        public override void Init()
        {

        }

        public override void OnClick()
        {
            Application.OpenURL(url);

            // Play button sound
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }
    }
}