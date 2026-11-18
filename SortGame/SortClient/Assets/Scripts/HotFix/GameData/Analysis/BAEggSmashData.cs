public class BAEggSmashData : BaseAnaData
{
    public readonly string key = "eggSmash";
    /// <summary>0 界面弹出 1 砸蛋 2 奖品面板关闭</summary>
    public int status;
    /// <summary>抽中的奖励组，配置 id</summary>
    public int rewardgroup;
    /// <summary>道具id_数量，多个用逗号</summary>
    public string rewardid;
    /// <summary>0 正常收集完成弹出 1 主城补领弹出</summary>
    public int type;
    /// <summary>0 Claim 1 ClaimAll</summary>
    public int choice;

    public override void ClearData()
    {
        status = 0;
        rewardgroup = 0;
        rewardid = "";
        type = 0;
        choice = 0;
    }
}
