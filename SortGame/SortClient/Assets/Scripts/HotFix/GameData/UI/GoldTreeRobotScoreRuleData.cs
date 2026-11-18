using System.Collections.Generic;
using cfg.Game;

public static class GoldTreeRobotScoreRuleData
{
    public static List<GoldTreeRobotScoreRuleCfg> GetRules()
    {
        return ConfigHelper.Tables.TbGoldTreeRobotScoreRuleCfg.DataList;
    }
}
