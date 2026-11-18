namespace FishTilePlay
{
    /// <summary>
    /// 道具类型枚举 - 定义游戏中所有可用的道具类型
    /// </summary>
    public enum PUType
    {
        Hint = 0,      // 提示道具 - 显示下一步可操作的提示
        Shuffle = 1,   // 打乱道具 - 随机打乱棋盘上的棋子位置
        Undo = 2,      // 撤销道具 - 撤销上一步操作
        
        ExtraSlot = 3, // 额外槽位道具 - 增加可放置棋子的槽位数量
        HighLight = 4  // 高亮道具 - 将不可点击棋子置灰，奖励/埋点编号 5
    }
}
