

public class BALoadingData : BaseAnaData
{
    public readonly string key = "loading_event";
    public string client_event_fail_code;
    public string client_event_fail_msg;
    public string client_event_id;
    public string client_event_name;
    public bool is_key_event_step;
    public string app_version;


    public override void ClearData()
    {
        
    }
}