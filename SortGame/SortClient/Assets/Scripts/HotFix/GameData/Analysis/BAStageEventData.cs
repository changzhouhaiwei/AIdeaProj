using System.Collections.Generic;


/// <summary>
/// 局内数据统计
/// </summary>
public class BAStageEventDta : BaseAnaData
{
    public string key = "stage_event";

    public string stage_id;
    public string stage_form_id;
    public string tilebase;
    // 0普通 1boxcover 2彩蛋 3IQ 4颜色挑战。多个同时存在就报多个，例如 ["1","4"]
    public List<string> playtype = new();
    public int round;
    public int straight_win;
    public int type;
    public int tile;//总棋子数
    public int layer;//总层数
    public int tile_left;//剩余棋子数
    public int layer_left;//剩余层数
    public int temp;//暂存区棋子数
    public int count;//操作次数
    public int status;//通关情况
    public int cost_time;//耗时
    public int tool_id;//道具id
    public List<BAMetaData> tool_use = new();//道具使用记录
    public bool is_saved;//是否从存档进入
    public bool is_nonet;//当前是否无网。只记探测结果，不改变难度
    public int ad_num;//局内使用了激励广告.

    public override void ClearData()
    {
        is_saved = false;
        is_nonet = false;
        playtype.Clear();
        tool_use.Clear();
        ad_num = 0;
    }
}