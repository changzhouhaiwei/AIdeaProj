/// <summary>
/// 广告加载埋点数据
/// </summary>
public class BAAdLoadData : BaseAnaData
{
    public string ad_display_type; // 展示广告类型

    public override void ClearData()
    {
        ad_display_type = null;
    }
}
