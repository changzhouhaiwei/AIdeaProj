using UnityEngine;
using UnityEngine.EventSystems;

public class EventTriggerListener : EventTrigger
{
    public delegate void UIEventHandle<in T>(GameObject go, T eventData) where T : BaseEventData;

    public class UIEvent<T> where T : BaseEventData
    {
        public UIEvent()
        {
        }

        public void AddListener(UIEventHandle<T> handle)
        {
            m_UIEventHandle += handle;
        }

        public void RemoveListener(UIEventHandle<T> handle)
        {
            m_UIEventHandle -= handle;
        }

        public void RemoveAllListeners()
        {
            m_UIEventHandle -= m_UIEventHandle;
            m_UIEventHandle = null;
        }

        public void Invoke(GameObject go, T eventData)
        {
            m_UIEventHandle?.Invoke(go, eventData);
        }

        private event UIEventHandle<T> m_UIEventHandle = null;
    }

    public readonly UIEvent<PointerEventData> onPointerEnter = new();
    public readonly UIEvent<PointerEventData> onPointerExit = new();
    public readonly UIEvent<PointerEventData> onPointerDown = new();
    public readonly UIEvent<PointerEventData> onPointerUp = new();
    public readonly UIEvent<PointerEventData> onClick = new();
    public readonly UIEvent<PointerEventData> onLongPress = new();
    public readonly UIEvent<PointerEventData> onInitializePotentialDrag = new();
    public readonly UIEvent<PointerEventData> onBeginDrag = new();
    public readonly UIEvent<PointerEventData> onDrag = new();
    public readonly UIEvent<PointerEventData> onEndDrag = new();
    public readonly UIEvent<PointerEventData> onDrop = new();
    public readonly UIEvent<PointerEventData> onScroll = new();
    public readonly UIEvent<BaseEventData> onUpdateSelected = new();
    public readonly UIEvent<BaseEventData> onSelect = new();
    public readonly UIEvent<BaseEventData> onDeselect = new();
    public readonly UIEvent<AxisEventData> onMove = new();
    public readonly UIEvent<BaseEventData> onSubmit = new();
    public readonly UIEvent<BaseEventData> onCancel = new();


    public static EventTriggerListener Get(GameObject go)
    {
        if (go == null)
        {
            return null;
        }

        EventTriggerListener eventTrigger = go.GetComponent<EventTriggerListener>();
        if (eventTrigger == null) eventTrigger = go.AddComponent<EventTriggerListener>();
        return eventTrigger;
    }

    private void OnDestroy()
    {
        RemoveAllListeners();
    }

    public void RemoveAllListeners()
    {
        onPointerEnter.RemoveAllListeners();
        onPointerExit.RemoveAllListeners();
        onPointerDown.RemoveAllListeners();
        onPointerUp.RemoveAllListeners();
        onClick.RemoveAllListeners();
        onLongPress.RemoveAllListeners();
        onInitializePotentialDrag.RemoveAllListeners();
        onBeginDrag.RemoveAllListeners();
        onDrag.RemoveAllListeners();
        onEndDrag.RemoveAllListeners();
        onDrop.RemoveAllListeners();
        onScroll.RemoveAllListeners();
        onUpdateSelected.RemoveAllListeners();
        onSelect.RemoveAllListeners();
        onDeselect.RemoveAllListeners();
        onMove.RemoveAllListeners();
        onSubmit.RemoveAllListeners();
        onCancel.RemoveAllListeners();
    }

    private void Update()
    {
        if (isPointDown)
        {
            if (Time.unscaledTime - curDonwTime >= LONGPRESS_TIME)
            {
                isLongPress = true;
                isPointDown = false;
                curDonwTime = 0f;
                onLongPress.Invoke(gameObject, null);
            }
        }
    }

    #region 方法

    public override void OnPointerEnter(PointerEventData eventData)
    {
        onPointerEnter.Invoke(gameObject, eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        onPointerExit.Invoke(gameObject, eventData);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        isPointDown = true;
        isLongPress = false;
        curDonwTime = Time.unscaledTime;
        onPointerDown.Invoke(gameObject, eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        isPointDown = false;
        onPointerUp.Invoke(gameObject, eventData);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (isLongPress)
        {
            return;
        }

        onClick.Invoke(gameObject, eventData);
    }

    public override void OnInitializePotentialDrag(PointerEventData eventData)
    {
        onInitializePotentialDrag.Invoke(gameObject, eventData);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        onBeginDrag.Invoke(gameObject, eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        onDrag.Invoke(gameObject, eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        onEndDrag.Invoke(gameObject, eventData);
    }

    public override void OnDrop(PointerEventData eventData)
    {
        onDrop.Invoke(gameObject, eventData);
    }

    public override void OnScroll(PointerEventData eventData)
    {
        onScroll.Invoke(gameObject, eventData);
    }

    public override void OnUpdateSelected(BaseEventData eventData)
    {
        onUpdateSelected.Invoke(gameObject, eventData);
    }

    public override void OnSelect(BaseEventData eventData)
    {
        onSelect.Invoke(gameObject, eventData);
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        onDeselect.Invoke(gameObject, eventData);
    }

    public override void OnMove(AxisEventData eventData)
    {
        onMove.Invoke(gameObject, eventData);
    }

    public override void OnSubmit(BaseEventData eventData)
    {
        onSubmit.Invoke(gameObject, eventData);
    }

    public override void OnCancel(BaseEventData eventData)
    {
        onCancel.Invoke(gameObject, eventData);
    }

    #endregion

    private const float LONGPRESS_TIME = 0.5f;
    private float curDonwTime = 0f;
    private bool isPointDown = false;
    private bool isLongPress = false;
}