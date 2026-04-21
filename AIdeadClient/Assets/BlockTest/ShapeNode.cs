using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ShapeNode : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    ShapeKind _kind;
    int _rotation;
    Color _color;
    Vector2 _homeAnchored;
    RectTransform _parentRect;
    WaitNode _wait;
    Vector2 _dragGrabOffset;
    bool _dragging;
    float _cellSize;
    Vector2 _boundsCenter;

    readonly List<MinCell> _parts = new List<MinCell>();

    RectTransform _rt;
    Image _hitGraphic;

    public ShapeKind Kind => _kind;
    public int Rot => _rotation;

    void Awake()
    {
        _rt = transform as RectTransform;
        _hitGraphic = GetComponent<Image>();
        if (_hitGraphic == null)
            _hitGraphic = gameObject.AddComponent<Image>();
        _hitGraphic.color = new Color(1f, 1f, 1f, 0.01f);
        _hitGraphic.raycastTarget = true;
    }

    public void Init(ShapeKind kind, int rotation, Color color, RectTransform slotParent, WaitNode wait)
    {
        _kind = kind;
        _rotation = rotation;
        _color = color;
        _wait = wait;
        _parentRect = slotParent;
        _homeAnchored = _rt.anchoredPosition;
        _cellSize = BlockGameManager.Instance != null && BlockGameManager.Instance.Board != null
            ? BlockGameManager.Instance.Board.CellSize
            : 100f;
        RebuildVisual();
    }

    void RebuildVisual()
    {
        foreach (var p in _parts)
        {
            if (p) Destroy(p.gameObject);
        }

        _parts.Clear();
        var pref = BlockGameManager.Instance != null ? BlockGameManager.Instance.CellPrefab : null;
        if (pref == null) return;

        var cells = ShapeDefine.GetCells(_kind, _rotation);
        if (cells.Length == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        foreach (var cc in cells)
        {
            float x = cc.x * _cellSize;
            float y = cc.y * _cellSize;
            minX = Mathf.Min(minX, x - _cellSize * 0.5f);
            maxX = Mathf.Max(maxX, x + _cellSize * 0.5f);
            minY = Mathf.Min(minY, y - _cellSize * 0.5f);
            maxY = Mathf.Max(maxY, y + _cellSize * 0.5f);
        }

        _boundsCenter = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        float w = Mathf.Max(maxX - minX, _cellSize * 0.5f);
        float h = Mathf.Max(maxY - minY, _cellSize * 0.5f);
        _rt.sizeDelta = new Vector2(w, h);

        foreach (var cc in cells)
        {
            var cell = Instantiate(pref, transform);
            var crt = cell.transform as RectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(_cellSize * 0.92f, _cellSize * 0.92f);
            crt.anchoredPosition = new Vector2(cc.x * _cellSize, cc.y * _cellSize) - _boundsCenter;
            crt.localScale = Vector3.one;
            cell.SetVisual(_color, true);
            _parts.Add(cell);
        }
    }

    Vector3 GridAnchorWorld()
    {
        var local = new Vector3(-_boundsCenter.x, -_boundsCenter.y, 0f);
        return transform.TransformPoint(local);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (BlockGameManager.Instance != null && BlockGameManager.Instance.IsGameOver) return;
        if (_parentRect == null) return;
        _dragging = true;
        transform.SetAsLastSibling();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, eventData.position, eventData.pressEventCamera, out var local))
            _dragGrabOffset = _rt.anchoredPosition - local;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || _parentRect == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, eventData.position, eventData.pressEventCamera, out var local))
            _rt.anchoredPosition = local + _dragGrabOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging) return;
        _dragging = false;

        var board = BlockGameManager.Instance != null ? BlockGameManager.Instance.Board : null;
        if (board == null)
        {
            _rt.anchoredPosition = _homeAnchored;
            return;
        }

        if (board.TryPlaceFromWorldAnchor(_kind, _rotation, GridAnchorWorld(), _color))
        {
            _wait.NotifyPlaced(this);
            return;
        }

        _rt.anchoredPosition = _homeAnchored;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BlockGameManager.Instance != null && BlockGameManager.Instance.IsGameOver) return;
        if (_dragging) return;
        if (eventData.button != PointerEventData.InputButton.Right) return;
        _rotation = (_rotation + 1) & 3;
        RebuildVisual();
    }
}
