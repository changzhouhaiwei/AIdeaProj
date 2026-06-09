using UnityEngine;

public class BaseChess : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int PieceType { get; private set; }
    public int Color { get; private set; }

    public void Init(int x, int y, int pieceType, int color)
    {
        X = x;
        Y = y;
        PieceType = pieceType;
        Color = color;

        gameObject.name = "Chess_" + pieceType + "_" + x + "_" + y;
    }
}
