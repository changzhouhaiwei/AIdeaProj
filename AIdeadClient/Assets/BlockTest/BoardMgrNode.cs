using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class BoardMgrNode : MonoBehaviour
{
    [SerializeField] int width = 8;
    [SerializeField] int height = 8;
    [SerializeField] float cellSize = 1f;
    [SerializeField] Color emptyColor = new Color(0.2f, 0.2f, 0.22f, 1f);
    [SerializeField] MinCell cellPrefab;

    RectTransform _rect;
    MinCell[,] _visuals;
    bool[,] _filled;
    Color[,] _colors;

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;
    public RectTransform Rect => _rect;

    void Awake()
    {
        _rect = transform as RectTransform;
    }

    public void BuildGrid()
    {
        if (cellPrefab == null || _rect == null) return;
        if (_visuals != null) return;

        _filled = new bool[width, height];
        _colors = new Color[width, height];
        _visuals = new MinCell[width, height];

        float pad = cellSize * 0.92f;
        for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++)
        {
            var inst = Instantiate(cellPrefab, _rect);
            var rt = inst.transform as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(pad, pad);
            rt.anchoredPosition = CellAnchoredPosition(r, c);
            rt.localScale = Vector3.one;
            inst.SetVisual(emptyColor, false);
            _visuals[c, r] = inst;
            _colors[c, r] = emptyColor;
        }
    }

    Vector2 GridOriginOffset()
    {
        return new Vector2(-(width - 1) * 0.5f * cellSize, -(height - 1) * 0.5f * cellSize);
    }

    Vector2 CellAnchoredPosition(int row, int col)
    {
        var o = GridOriginOffset();
        return new Vector2(col * cellSize + o.x, row * cellSize + o.y);
    }

    Camera GetUICamera()
    {
        var canvas = _rect.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }

    public Vector2Int WorldPointToAnchorCell(Vector3 worldPoint)
    {
        var cam = GetUICamera();
        var screen = RectTransformUtility.WorldToScreenPoint(cam, worldPoint);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, cam, out var local))
            return new Vector2Int(int.MinValue, int.MinValue);

        var o = GridOriginOffset();
        int col = Mathf.RoundToInt((local.x - o.x) / cellSize);
        int row = Mathf.RoundToInt((local.y - o.y) / cellSize);
        return new Vector2Int(row, col);
    }

    public bool CanPlace(ShapeKind kind, int rot, Vector2Int anchorRowCol, out Vector2Int[] occupiedCells)
    {
        occupiedCells = null;
        if (_filled == null) return false;

        var cells = ShapeDefine.GetCells(kind, rot);
        occupiedCells = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            int r = anchorRowCol.x + cells[i].y;
            int c = anchorRowCol.y + cells[i].x;
            occupiedCells[i] = new Vector2Int(r, c);
            if (r < 0 || r >= height || c < 0 || c >= width) return false;
            if (_filled[c, r]) return false;
        }

        return true;
    }

    public bool TryPlace(ShapeKind kind, int rot, Vector2Int anchorRowCol, Color fillColor)
    {
        if (_filled == null) return false;
        if (!CanPlace(kind, rot, anchorRowCol, out var occ)) return false;
        foreach (var p in occ)
        {
            int c = p.y;
            int r = p.x;
            _filled[c, r] = true;
            _colors[c, r] = fillColor;
            _visuals[c, r].SetVisual(fillColor, true);
        }

        ClearFullLines();
        return true;
    }

    public bool TryPlaceFromWorldAnchor(ShapeKind kind, int rot, Vector3 gridAnchorWorld, Color fillColor)
    {
        var anchor = WorldPointToAnchorCell(gridAnchorWorld);
        if (anchor.x == int.MinValue) return false;
        return TryPlace(kind, rot, anchor, fillColor);
    }

    void ClearFullLines()
    {
        var fullRows = new List<int>();
        var fullCols = new List<int>();

        for (int r = 0; r < height; r++)
        {
            bool full = true;
            for (int c = 0; c < width; c++)
            {
                if (!_filled[c, r]) { full = false; break; }
            }

            if (full) fullRows.Add(r);
        }

        for (int c = 0; c < width; c++)
        {
            bool full = true;
            for (int r = 0; r < height; r++)
            {
                if (!_filled[c, r]) { full = false; break; }
            }

            if (full) fullCols.Add(c);
        }

        if (fullRows.Count == 0 && fullCols.Count == 0) return;

        var toClear = new HashSet<Vector2Int>();
        foreach (var r in fullRows)
        for (int c = 0; c < width; c++)
            toClear.Add(new Vector2Int(r, c));
        foreach (var c in fullCols)
        for (int r = 0; r < height; r++)
            toClear.Add(new Vector2Int(r, c));

        foreach (var p in toClear)
        {
            int c = p.y;
            int r = p.x;
            _filled[c, r] = false;
            _colors[c, r] = emptyColor;
            if (_visuals != null && _visuals[c, r] != null)
                _visuals[c, r].SetVisual(emptyColor, false);
        }
    }

    public bool HasPlacementFor(ShapeKind kind, int rot)
    {
        if (_filled == null) return false;

        for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++)
        {
            if (CanPlace(kind, rot, new Vector2Int(r, c), out _)) return true;
        }

        return false;
    }

    public void ClearBoard()
    {
        if (_filled == null) return;

        for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++)
        {
            _filled[c, r] = false;
            _colors[c, r] = emptyColor;
            if (_visuals != null && _visuals[c, r] != null)
                _visuals[c, r].SetVisual(emptyColor, false);
        }
    }
}
