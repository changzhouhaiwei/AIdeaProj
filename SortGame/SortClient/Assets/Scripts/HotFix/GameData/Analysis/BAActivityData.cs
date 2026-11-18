public class BAActivityData : BaseAnaData
{
    public int activity_id;
    public string activity_name;
    public int status;
    public int rank;
    public int rank2;
    public string unique_id;
    public int key;
    /// <summary>仅 status=2 时有意义；排位不报（保持 0），小狗和金猪报获得金币数。</summary>
    public int reward_id;

    public override void ClearData()
    {
        activity_id = 0;
        activity_name = "";
        status = 0;
        rank = 0;
        rank2 = 0;
        unique_id = "";
        key = 0;
        reward_id = 0;
    }
}
