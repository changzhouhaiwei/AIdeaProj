using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ShapeKind
{
    R1x1,
    R1x2,
    R1x3,
    R1x4,
    R1x5,
    R2x2,
    R2x1,
    R2x3,
    R3x2,
    R4x1,
    R5x1,
    Z,
    L2,
    L3,
    LBig
}

public static class ShapeDefine
{
    static readonly Dictionary<ShapeKind, Vector2Int[]> Base = BuildBase();

    static Dictionary<ShapeKind, Vector2Int[]> BuildBase()
    {
        var d = new Dictionary<ShapeKind, Vector2Int[]>
        {
            { ShapeKind.R1x1, Rect(1, 1) },
            { ShapeKind.R1x2, Rect(2, 1) },
            { ShapeKind.R1x3, Rect(3, 1) },
            { ShapeKind.R1x4, Rect(4, 1) },
            { ShapeKind.R1x5, Rect(5, 1) },
            { ShapeKind.R2x2, Rect(2, 2) },
            { ShapeKind.R2x1, Rect(1, 2) },
            { ShapeKind.R2x3, Rect(3, 2) },
            { ShapeKind.R3x2, Rect(3, 2) },
            { ShapeKind.R4x1, Rect(4, 1) },
            { ShapeKind.R5x1, Rect(5, 1) },
            { ShapeKind.Z, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) } },
            { ShapeKind.L2, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) } },
            { ShapeKind.L3, new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0) } },
            {
                ShapeKind.LBig,
                new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                    new Vector2Int(0, 1), new Vector2Int(0, 2)
                }
            }
        };
        return d;
    }

    static Vector2Int[] Rect(int w, int h)
    {
        var list = new List<Vector2Int>(w * h);
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
            list.Add(new Vector2Int(x, y));
        return list.ToArray();
    }

    public static Vector2Int[] GetCells(ShapeKind kind, int rotationQuarter)
    {
        if (!Base.TryGetValue(kind, out var cells))
            return new[] { Vector2Int.zero };
        return RotateNormalize(cells, rotationQuarter & 3);
    }

    static Vector2Int[] RotateNormalize(Vector2Int[] cells, int quarterTurns)
    {
        if (quarterTurns == 0)
            return cells.ToArray();

        var list = cells.ToList();
        for (int t = 0; t < quarterTurns; t++)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                list[i] = new Vector2Int(-c.y, c.x);
            }
        }

        int minX = list.Min(c => c.x);
        int minY = list.Min(c => c.y);
        return list.Select(c => new Vector2Int(c.x - minX, c.y - minY)).ToArray();
    }

    public static ShapeKind RandomKind()
    {
        var values = (ShapeKind[])System.Enum.GetValues(typeof(ShapeKind));
        return values[Random.Range(0, values.Length)];
    }

    public static int RandomRotation() => Random.Range(0, 4);
}
