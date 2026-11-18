public class BAADNoShowData : BaseAnaData
{
    public string ad_type;
    public string ad_place;
    public string reason;

    public override void ClearData()
    {
        ad_type = "";
        ad_place = "";
        reason = "";
    }
}