using UnityEngine;
using UnityEngine.UI;

namespace RopeSort
{
    public class RopeDeadLockTip : MonoBehaviour
    {
        void Awake()
        {
            Bind("Close", Close);
            Bind("Restart", OnRestart);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PlaceAboveTools();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void OnRestart()
        {
            Close();
            RopeGamePage page = GetComponentInParent<RopeGamePage>();
            if (page != null)
                page.RestartFromDeadlock();
        }

        void PlaceAboveTools()
        {
            RectTransform card = transform.Find("Card") as RectTransform;
            if (card == null)
                return;
            RectTransform bottom = transform.parent != null ? transform.parent.Find("BottomBar") as RectTransform : null;
            float y = bottom != null ? bottom.sizeDelta.y + 18f : 248f;
            card.anchorMin = new Vector2(0.5f, 0f);
            card.anchorMax = new Vector2(0.5f, 0f);
            card.pivot = new Vector2(0.5f, 0f);
            card.anchoredPosition = new Vector2(0f, y);
            RectTransform parent = transform.parent as RectTransform;
            if (parent != null && parent.rect.width > 1f)
                card.sizeDelta = new Vector2(Mathf.Min(665f, parent.rect.width - 36f), 157f);
        }

        void Bind(string childName, UnityEngine.Events.UnityAction action)
        {
            Button button = null;
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == childName)
                {
                    button = buttons[i];
                    break;
                }
            }
            if (button != null)
                button.onClick.AddListener(action);
        }
    }
}
