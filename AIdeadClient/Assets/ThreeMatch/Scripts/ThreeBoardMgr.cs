using System.Collections.Generic;
using UnityEngine;

public class ThreeBoardMgr : MonoBehaviour
{
    [SerializeField] private GameObject boardCellPrefab;
    [SerializeField] private Transform boardRoot;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool yZeroAtTop = true;

    private readonly Dictionary<Vector2Int, Transform> boardCellMap = new Dictionary<Vector2Int, Transform>();

    public float CellSize => cellSize;
    public bool YZeroAtTop => yZeroAtTop;

    private void Awake()
    {
        if (boardRoot == null)
        {
            boardRoot = transform;
        }
    }

    public void Init(ThreeLevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("[ThreeBoardMgr] levelData 为空。");
            return;
        }

        if (boardCellPrefab == null)
        {
            Debug.LogError("[ThreeBoardMgr] boardCellPrefab 未设置。");
            return;
        }

        if (boardRoot == null)
        {
            boardRoot = transform;
        }

        ClearBoard();
        boardCellMap.Clear();

        for (int i = 0; i < levelData.slotInfo.Count; i++)
        {
            SlotInfo slot = levelData.slotInfo[i];
            if (slot.slotType <= 0)
            {
                continue;
            }

            Vector3 localPos = GridToLocalPosition(slot.x, slot.y, levelData.xMax, levelData.yMax, cellSize, yZeroAtTop);
            GameObject cellObj = Instantiate(boardCellPrefab, boardRoot);
            cellObj.name = "BoardCell_" + slot.x + "_" + slot.y;
            cellObj.transform.localPosition = localPos;
            cellObj.transform.localRotation = Quaternion.identity;
            cellObj.transform.localScale = Vector3.one;

            Vector2Int key = new Vector2Int(slot.x, slot.y);
            boardCellMap[key] = cellObj.transform;
        }
    }

    public static Vector3 GridToLocalPosition(int x, int y, int xMax, int yMax, float cellSize, bool yZeroAtTop)
    {
        int visualY = yZeroAtTop ? (yMax - 1 - y) : y;
        float worldX = (x - (xMax - 1) / 2f) * cellSize;
        float worldY = (visualY - (yMax - 1) / 2f) * cellSize;
        return new Vector3(worldX, worldY, 0f);
    }

    public bool TryGetCellTransform(int x, int y, out Transform cellTransform)
    {
        return boardCellMap.TryGetValue(new Vector2Int(x, y), out cellTransform);
    }

    private void ClearBoard()
    {
        if (boardRoot == null)
        {
            return;
        }

        for (int i = boardRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = boardRoot.GetChild(i);
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
