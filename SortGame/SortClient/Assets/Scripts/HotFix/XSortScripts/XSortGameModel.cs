using UnityEngine;

namespace FishXSortPlay
{
    [CreateAssetMenu(fileName = "Game Data", menuName = "Data/Game Data")]
    public class XSortGameModel : ScriptableObject
    {
        [SerializeField] bool showTutorial = true;
        public bool ShowTutorial => showTutorial;

        [SerializeField] float maxXSortSize = 1.5f;
        public float MaxXSortSize => maxXSortSize;

        [SerializeField, Tooltip("Cannot be lower than 1 and higher than MaxXSortSize")] Vector2 xSortSize = Vector2.one;
        public Vector2 XSortSize => xSortSize / (xSortSize.x > xSortSize.y ? xSortSize.y : xSortSize.x);

        [SerializeField] bool infiniteLevels;
        public bool InfiniteLevels => infiniteLevels;

        private void OnValidate()
        {
            xSortSize.x = Mathf.Clamp(xSortSize.x, 1, maxXSortSize);
            xSortSize.y = Mathf.Clamp(xSortSize.y, 1, maxXSortSize);
        }
    }
}
