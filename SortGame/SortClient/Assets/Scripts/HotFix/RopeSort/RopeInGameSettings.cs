using FishXSortPlay;
using XSortGameCore;
using UnityEngine;
using UnityEngine.UI;

namespace RopeSort
{
    public class RopeInGameSettings : MonoBehaviour
    {
        [SerializeField] Image musicToggle;
        [SerializeField] Image soundToggle;
        [SerializeField] Image vibrationToggle;

        static readonly Color OnColor = new Color(0.29f, 0.73f, 0.46f);
        static readonly Color OffColor = new Color(0.72f, 0.72f, 0.72f);

        void Awake()
        {
            Bind("Music", ToggleMusic);
            Bind("Sound", ToggleSound);
            Bind("Vibration", ToggleVibration);
            Bind("Restart", OnRestart);
            Bind("Home", OnHome);
            Bind("Close", Close);
        }

        void OnEnable()
        {
            Refresh();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void Bind(string childName, UnityEngine.Events.UnityAction action)
        {
            Transform node = transform.Find(childName);
            if (node == null)
            {
                foreach (var child in GetComponentsInChildren<Button>(true))
                {
                    if (child.name == childName)
                    {
                        node = child.transform;
                        break;
                    }
                }
            }

            if (node == null)
                return;
            Button button = node.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(action);
        }

        void ToggleMusic()
        {
            SetAudio(XSortGameCore.AudioType.Music, !AudioOn(XSortGameCore.AudioType.Music));
            Refresh();
        }

        void ToggleSound()
        {
            SetAudio(XSortGameCore.AudioType.Sound, !AudioOn(XSortGameCore.AudioType.Sound));
            Refresh();
        }

        void ToggleVibration()
        {
            if (!Haptic.IsInitialized)
                Haptic.Init();
            Haptic.IsActive = !Haptic.IsActive;
            Refresh();
        }

        void OnRestart()
        {
            Close();
            GameDirector.ReplayLevel();
        }

        void OnHome()
        {
            Close();
            GameDirector.ReturnToMenu();
        }

        void Refresh()
        {
            Paint(musicToggle, AudioOn(XSortGameCore.AudioType.Music));
            Paint(soundToggle, AudioOn(XSortGameCore.AudioType.Sound));
            if (!Haptic.IsInitialized)
                Haptic.Init();
            Paint(vibrationToggle, Haptic.IsActive);
        }

        static void Paint(Image image, bool on)
        {
            if (image != null)
                image.color = on ? OnColor : OffColor;
        }

        static bool AudioOn(XSortGameCore.AudioType type)
        {
            try
            {
                return AudioController.GetVolume(type) > 0.01f;
            }
            catch (System.NullReferenceException)
            {
                return true;
            }
        }

        static void SetAudio(XSortGameCore.AudioType type, bool on)
        {
            AudioController.SetVolume(type, on ? 1f : 0f);
        }
    }
}
