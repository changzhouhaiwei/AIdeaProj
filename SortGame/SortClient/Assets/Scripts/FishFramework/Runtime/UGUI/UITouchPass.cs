using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UITouchPass : MonoBehaviour, IPointerClickHandler,
    IMoveHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, ISelectHandler, IDeselectHandler
    , ISubmitHandler, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler
{
    private bool _isPicking = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isPicking)
            return;
        PassEvent(eventData, ExecuteEvents.pointerClickHandler);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PassEvent(eventData, ExecuteEvents.pointerDownHandler);
        if (Input.GetButtonDown("Submit"))
            ExecuteEvents.Execute(eventData.pointerCurrentRaycast.gameObject, eventData, ExecuteEvents.submitHandler);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        PassEvent(eventData, ExecuteEvents.pointerUpHandler);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PassEvent(eventData, ExecuteEvents.pointerEnterHandler);
    }

    public void OnSelect(BaseEventData eventData)
    {
        PassEvent(eventData, ExecuteEvents.selectHandler);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        PassEvent(eventData, ExecuteEvents.deselectHandler);
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PassEvent(eventData, ExecuteEvents.submitHandler);
    }

    public void OnMove(AxisEventData eventData)
    {
        PassEvent(eventData, ExecuteEvents.moveHandler);
    }

    GameObject CacheGameObject;

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        CacheGameObject = PassEvent(eventData, ExecuteEvents.initializePotentialDrag);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isPicking = true;
        PassEvent(eventData, ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ExecuteEvents.Execute(CacheGameObject, eventData, ExecuteEvents.dragHandler);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isPicking = false;
        ExecuteEvents.Execute(CacheGameObject, eventData, ExecuteEvents.endDragHandler);
        CacheGameObject = null;
    }

    public void OnScroll(PointerEventData eventData)
    {
        ExecuteEvents.Execute(CacheGameObject, eventData, ExecuteEvents.scrollHandler);
    }

    List<RaycastResult> result = new List<RaycastResult>();

    private GameObject PassEvent<T>(BaseEventData data, ExecuteEvents.EventFunction<T> function) where T : IEventSystemHandler
    {
        PointerEventData eventData = data as PointerEventData;
        if (eventData == null)
        {
            return null;
        }

        var pointerGo = eventData.pointerCurrentRaycast.gameObject ?? eventData.pointerDrag;
        EventSystem.current.RaycastAll(eventData, result);
        foreach (var item in result)
        {
            var go = item.gameObject;
            if (go != null && go != pointerGo)
            {
                var excuteGo = ExecuteEvents.GetEventHandler<T>(go);
                if (excuteGo)
                {
                    if (excuteGo.TryGetComponent<UITouchPass>(out var __))
                        continue;
                    ExecuteEvents.Execute(excuteGo, data, function);
                    return excuteGo;
                }
                else
                {
                    if (go.TryGetComponent<UnityEngine.UI.Graphic>(out var com))
                    {
                        if (com.raycastTarget) return null;
                    }
                }
            }
        }

        return null;
    }
}