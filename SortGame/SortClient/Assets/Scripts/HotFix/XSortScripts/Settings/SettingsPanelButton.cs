using UnityEngine;
using UnityEngine.UI;
using XSortGameCore;

namespace FishXSortPlay
{
    [RequireComponent(typeof(Button))]
    public class SettingsPanelButton : MonoBehaviour
    {
        public Button Button { get; private set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
            Button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            UIController.ShowPage<UISettings>();
            // Play button sound
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }
    }
}
