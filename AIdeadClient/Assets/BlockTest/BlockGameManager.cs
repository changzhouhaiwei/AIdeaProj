using UnityEngine;
using UnityEngine.Serialization;

public class BlockGameManager : MonoBehaviour
{
    public static BlockGameManager Instance;

    [FormerlySerializedAs("mgrNode")]
    [SerializeField] BoardMgrNode boardNode;
    [SerializeField] WaitNode waitNode;
    [FormerlySerializedAs("oneCell")]
    [SerializeField] MinCell cellPrefab;

    public BoardMgrNode Board => boardNode;
    public MinCell CellPrefab => cellPrefab;
    public bool IsGameOver { get; private set; }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (boardNode != null) boardNode.BuildGrid();
        if (waitNode != null) waitNode.BeginRound();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) Restart();
    }

    public void EndGame()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Debug.Log("[BlockGame] Game Over: 当前候选形状已无法放置。按 R 可重开。");
    }

    public void Restart()
    {
        IsGameOver = false;
        if (boardNode != null) boardNode.ClearBoard();
        if (waitNode != null) waitNode.BeginRound();
    }
}
