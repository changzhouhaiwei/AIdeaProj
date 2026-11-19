using System.Collections.Generic;

public class BAMainStageData : BaseAnaData
{
    public readonly string key = "main_stage";
    public string stage_id;
    public string stage_form_id;
    public string XSortbase;
    // 主题底板固定 "theme"；XSortBaseStyleCatalog 的随机底板上传 styleName。
    public string XSortbase2;
    // 0普通 1boxcover 2彩蛋 3IQ 4颜色挑战。多个同时存在就报多个，例如 ["1","4"]
    public List<string> playtype = new();
    //第N次进入本关
    public int round;

    //进入关卡时连胜连败计数
    public int straight_win;

    public int type; //开局棋子种类数
    public int XSort;//总棋子数
    public int layer;//总层数
    public int XSort_left;//剩余棋子数
    public int layer_left;//剩余层数
    public int temp;//暂存区棋子数
    public List<int> chess_list_fail = new();//失败时暂存区棋子id，从左到右。status=2/6
    public List<int> chess_cost_fail = new();//失败时每个暂存棋的cost，与上表同序。status=2/6
    public int count;//操作次数

    public int status;//0：进入（重新开始不算进入）1：胜利2：空间不足失败（失败只记录最终失败， 若失败后使用广告或金币继续游戏则不记录，下同）3：超时失败4：中途退出5：重新开始6：复活7：重新开始导致的失败
    public int cost_time;//耗时 1 2 3 4 时记录
    public List<BAMetaData> tool_use = new();//道具使用记录

    public bool is_saved;//是否从存档里进入
    public bool is_nonet;//当前是否无网。只记探测结果，不改变难度
    public int ad_num; //局内使用了激励广告
    public bool is_skip;//是否是跳关

    public string rank;
    public string rank2;
    public string challenge_score_cur;
    public string challenge_score;
    public string unique_id;

    public override void ClearData()
    {
        is_skip = false;
        is_saved = false;
        is_nonet = false;
        ad_num = 0;
        tool_use.Clear();
        playtype.Clear();
        chess_list_fail.Clear();
        chess_cost_fail.Clear();
        rank = "";
        rank2 = "";
        challenge_score_cur = "";
        challenge_score = "";
        unique_id = "";
    }
}