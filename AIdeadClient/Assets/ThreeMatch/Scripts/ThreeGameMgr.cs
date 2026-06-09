using System.IO;
using UnityEngine;

public class ThreeGameMgr : MonoBehaviour
{
    [SerializeField] private ThreeBoardMgr boardMgr;
    [SerializeField] private ThreeChessMgr chessMgr;

    [Header("测试关卡")]
    [SerializeField] private int level = 1;
    [SerializeField] private bool autoLoadOnStart = true;

    public ThreeLevelData CurrentLevelData { get; private set; }

    private void Awake()
    {
        if (boardMgr == null)
        {
            boardMgr = FindObjectOfType<ThreeBoardMgr>();
        }

        if (chessMgr == null)
        {
            chessMgr = FindObjectOfType<ThreeChessMgr>();
        }
    }

    private void Start()
    {
        if (!autoLoadOnStart)
        {
            return;
        }

        LoadLevel(level);
    }

    [ContextMenu("Reload Current Level")]
    public void ReloadCurrentLevel()
    {
        LoadLevel(level);
    }

    public bool LoadLevel(int targetLevel)
    {
        if (boardMgr == null || chessMgr == null)
        {
            Debug.LogError("[ThreeGameMgr] BoardMgr 或 ChessMgr 未绑定，无法初始化关卡。");
            return false;
        }

        string levelPath = GetLevelFilePath(targetLevel);
        if (!File.Exists(levelPath))
        {
            Debug.LogError("[ThreeGameMgr] 关卡文件不存在: " + levelPath);
            return false;
        }

        string json = File.ReadAllText(levelPath);
        if (!ThreeLevelData.TryFromJson(json, out ThreeLevelData levelData, out string error))
        {
            Debug.LogError("[ThreeGameMgr] 关卡解析失败, level=" + targetLevel + ", error=" + error);
            return false;
        }

        CurrentLevelData = levelData;
        level = targetLevel;

        boardMgr.Init(levelData);
        chessMgr.Init(levelData, boardMgr.CellSize, boardMgr.YZeroAtTop);

        Debug.Log("[ThreeGameMgr] 关卡初始化完成: " + targetLevel);
        return true;
    }

    private string GetLevelFilePath(int targetLevel)
    {
        return Path.Combine(Application.dataPath, "ThreeMatch", "Level", "u_level" + targetLevel + ".json");
    }
}
