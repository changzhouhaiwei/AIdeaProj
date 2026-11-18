public class BASignData : BaseAnaData
{
    public readonly string key = "sign";
    public int status; // 0:结算弹出 4:主城进入
    public int day;    // 当前连胜天数
    public int type;   // 默认 1

    public override void ClearData()
    {
        status = 0;
        day = 0;
        type = 1;
    }
}
