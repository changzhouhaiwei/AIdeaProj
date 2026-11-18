using System;

public class BATkCommonData : BaseAnaData
{
    [Serializable]
    public struct BACfgData
    {
        public string name;
        public string version;
    }

    public int stage;
    public int coin;
    public string test_group;
    /// <summary>用数组而非 List，保证 JsonUtility 能带上 cfg.name / cfg.version。</summary>
    public BACfgData[] cfg = Array.Empty<BACfgData>();
    /// <summary>事件发生时的有效插屏 CD（秒）。E 组按生命周期/国家；其余为 0。</summary>
    public int ad_strategy_Cooldown;
    /// <summary>插屏开始计 CD 的 Unix 秒：冷启动首次进关重写，或上一条插屏关播。</summary>
    public long ad_strategy_timestamp;
    /// <summary>最近一次进入的关卡号（status=0）。</summary>
    public string last_stage = "";
    /// <summary>最近一次进入的关卡形态，与 main_stage.stage_form_id 相同。</summary>
    public string last_stage_form = "";
    /// <summary>剩余道具。字符串列表，从左到右：提示、洗牌、撤回、高亮。</summary>
    public string[] tool_left = Array.Empty<string>();

    public override void ClearData()
    {
        test_group = "";
        cfg = Array.Empty<BACfgData>();
        ad_strategy_Cooldown = 0;
        ad_strategy_timestamp = 0;
        last_stage = "";
        last_stage_form = "";
        tool_left = Array.Empty<string>();
    }
}