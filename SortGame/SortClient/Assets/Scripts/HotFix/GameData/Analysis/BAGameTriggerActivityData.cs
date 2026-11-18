public class BAGameTriggerActivityData : BaseAnaData
{
    public string activity_id;
    public string activity_name;
    public string unique_id;
    public int status;

    public override void ClearData()
    {
        activity_id = "";
        activity_name = "";
        unique_id = "";
        status = 0;
    }
}
