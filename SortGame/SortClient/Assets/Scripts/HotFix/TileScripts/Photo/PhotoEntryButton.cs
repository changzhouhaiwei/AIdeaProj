using UnityEngine;
using UnityEngine.UI;
using TileGameCore;
using FishFramework;

namespace FishTilePlay
{
    [RequireComponent(typeof(Button))]
    public class PhotoEntryButton : MonoBehaviour
    {
        public Button Button { get; private set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
            Button.onClick.AddListener(HandleButtonTap);
        }

        private void HandleButtonTap()
        {
            // UIController.ShowPage<UISettings>();
            GameModule.UI.OpenPanel<UIStoryBookView>();
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }
    }
}
