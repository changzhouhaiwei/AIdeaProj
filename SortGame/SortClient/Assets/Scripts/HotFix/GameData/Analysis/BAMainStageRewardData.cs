public class BAMainStageRewardData : BaseAnaData
{
    public readonly string key = "main_stage_reward";
    public int status; // 0:按钮出现 1:点击按钮 2:收到奖励
    public string id;  // 抽中的奖励串，如 4,50

    public override void ClearData()
    {
        status = 0;
        id = "";
    }
}
