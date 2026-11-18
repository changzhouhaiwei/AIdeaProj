using System.Collections.Generic;
using System.Diagnostics;
using cfg.Game;

public class LevelPassData : BaseData
{
    List<LevelPassCfg> cfgList = new();

    public override void OnInit()
    {
        var dataList = ConfigHelper.Tables.TbLevelPassCfg.DataList;
        dataList.Sort((a, b) => a.Id.CompareTo(b.Id));
        cfgList = dataList;
    }

    public override void OnRemove()
    {

    }

    public override void OnReset()
    {

    }

    //当前阶段数据
    public LevelPassCfg GetCurrentStageLevelCfg(int levelIdx = -1)
    {
        if (cfgList == null || cfgList.Count == 0)
        {
            return null;
        }

        int levelNum = GPUserDataManager.Instance.GetLevelNum();
        if (levelIdx != -1)
        {
            levelNum = levelIdx;
        }

        int valueAdd = 0;
        foreach (var item in cfgList)
        {
            if (levelNum >= valueAdd && levelNum < valueAdd + item.ProgressValue)
            {
                return item;
            }

            valueAdd += item.ProgressValue;
        }
        // 超过配表总进度后，固定使用最后一条配置继续循环
        return cfgList[cfgList.Count - 1];
    }

    public int GetCurrentStagePoint(int levelIdx = -1)
    {
        if (cfgList == null || cfgList.Count == 0)
        {
            return 0;
        }

        int levelNum = GPUserDataManager.Instance.GetLevelNum();
        if (levelIdx != -1)
        {
            levelNum = levelIdx;
        }
        
        int valueAdd = 0;
        foreach (var item in cfgList)
        {
            if (levelNum >= valueAdd && levelNum < valueAdd + item.ProgressValue)
            {
                return levelNum - valueAdd + 1;
            }
            valueAdd += item.ProgressValue;
        }

        // 超过配表总进度后，按最后一条配置的进度值循环
        int lastProgressValue = cfgList[cfgList.Count - 1].ProgressValue;
        if (lastProgressValue <= 0)
        {
            return 0;
        }

        int overflowLevel = levelNum - valueAdd;
        return (overflowLevel % lastProgressValue) + 1;
    }


    //获得最大ID
    public int GetMaxStage()
    {
        return cfgList[cfgList.Count - 1].Id;
    }
}