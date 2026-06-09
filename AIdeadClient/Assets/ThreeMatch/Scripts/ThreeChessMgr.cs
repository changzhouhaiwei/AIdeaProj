using System.Collections.Generic;
using UnityEngine;

public class ThreeChessMgr : MonoBehaviour
{
    [SerializeField] private List<BaseChess> chessOriList = new List<BaseChess>();
    [SerializeField] private Transform chessRoot;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool spawnSpecialAsPlaceholder = false;
    [SerializeField] private BaseChess specialPiecePlaceholderPrefab;

    private BaseChess[,] chessGrid;

    private void Awake()
    {
        if (chessRoot == null)
        {
            chessRoot = transform;
        }
    }

    public void Init(ThreeLevelData levelData, float boardCellSize, bool yZeroAtTop)
    {
        if (levelData == null)
        {
            Debug.LogError("[ThreeChessMgr] levelData 为空。");
            return;
        }

        if (boardCellSize > 0f)
        {
            cellSize = boardCellSize;
        }

        if (chessRoot == null)
        {
            chessRoot = transform;
        }

        ClearChess();
        chessGrid = new BaseChess[levelData.xMax, levelData.yMax];

        for (int i = 0; i < levelData.pieceInfo.Count; i++)
        {
            PieceInfo piece = levelData.pieceInfo[i];
            bool isNormalPiece = IsNormalPiece(piece.pieceType);
            if (!isNormalPiece && !spawnSpecialAsPlaceholder)
            {
                continue;
            }

            BaseChess prefab = isNormalPiece ? GetPiecePrefab(piece.pieceType) : specialPiecePlaceholderPrefab;
            if (prefab == null)
            {
                if (isNormalPiece)
                {
                    Debug.LogWarning("[ThreeChessMgr] 缺少 pieceType 对应预制体: " + piece.pieceType);
                }
                else
                {
                    Debug.LogWarning("[ThreeChessMgr] 特殊棋子占位预制体未设置, pieceType: " + piece.pieceType);
                }
                continue;
            }

            Vector3 localPos = ThreeBoardMgr.GridToLocalPosition(piece.x, piece.y, levelData.xMax, levelData.yMax, cellSize, yZeroAtTop);
            BaseChess chess = Instantiate(prefab, chessRoot);
            chess.transform.localPosition = localPos;
            chess.transform.localRotation = Quaternion.identity;
            chess.transform.localScale = Vector3.one;
            chess.Init(piece.x, piece.y, piece.pieceType, piece.color);

            if (IsInsideBoard(piece.x, piece.y, levelData.xMax, levelData.yMax))
            {
                chessGrid[piece.x, piece.y] = chess;
            }
        }
    }

    public BaseChess GetChess(int x, int y)
    {
        if (chessGrid == null)
        {
            return null;
        }

        if (x < 0 || x >= chessGrid.GetLength(0) || y < 0 || y >= chessGrid.GetLength(1))
        {
            return null;
        }

        return chessGrid[x, y];
    }

    private bool IsNormalPiece(int pieceType)
    {
        return pieceType >= 1 && pieceType <= 5;
    }

    private BaseChess GetPiecePrefab(int pieceType)
    {
        int index = pieceType - 1;
        if (index < 0 || index >= chessOriList.Count)
        {
            return null;
        }

        return chessOriList[index];
    }

    private bool IsInsideBoard(int x, int y, int xMax, int yMax)
    {
        return x >= 0 && x < xMax && y >= 0 && y < yMax;
    }

    private void ClearChess()
    {
        if (chessRoot == null)
        {
            return;
        }

        for (int i = chessRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = chessRoot.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}
