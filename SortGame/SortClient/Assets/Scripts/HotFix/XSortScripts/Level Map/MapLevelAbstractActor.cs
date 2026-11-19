using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XSortGameCore;

namespace FishXSortPlay.Map
{
    [StaticUnload]
    public abstract class MapLevelAbstractActor : MonoBehaviour
    {
        [SerializeField] protected Button button;
        [SerializeField] protected TMP_Text levelNumber;

        public int LevelId { get; protected set; }

        public static event SimpleIntCallback OnLevelClicked;

        protected virtual void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        public virtual void Init(int id)
        {
            LevelId = id;
            levelNumber.text = $"{id + 1}";

            if (id < MapActor.MaxLevelReached)
            {
                InitOpen();
            }
            else if (id == MapActor.MaxLevelReached)
            {
                InitCurrent();
            }
            else
            {
                InitClose();
            }
        }

        protected virtual void OnButtonClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            OnLevelClicked?.Invoke(LevelId);
        }

        protected abstract void InitOpen();
        protected abstract void InitClose();
        protected abstract void InitCurrent();

        private static void UnloadStatic()
        {
            OnLevelClicked = null;
        }
    }
}