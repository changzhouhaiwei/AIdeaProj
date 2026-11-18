using System.Collections.Generic;
using cfg.Game;

public class StoryBookData : BaseData
{
    Dictionary<int, List<GameBgCfg>> m_cfgDic = new();

    public override void OnInit()
    {
        m_cfgDic.Clear();
        //初始化,这里构造想要的数据
        var dataList = ConfigHelper.Tables.TbGameBgShow.DataList;
        foreach (var item in dataList)
        {
            if (!m_cfgDic.TryGetValue(item.Category, out List<GameBgCfg> oneCategory))
            {
                oneCategory = new();
                m_cfgDic[item.Category] = oneCategory;
            }
            oneCategory.Add(item);
        }

        foreach (var item in m_cfgDic)
        {
            var list = item.Value;
            list.Sort((a, b) =>
            {
                return a.Id.CompareTo(b.Id);
            });
        }

        UnityEngine.Debug.Log("hello ~");
    }

    public string GetBgPathByLevelIndex(int levelIndex)
    {
        var dataList = ConfigHelper.Tables.TbGameBgShow.DataList;
        int id = levelIndex + 1;
        foreach (var item in dataList)
        {
            if (id <= item.EndLevel && id >= item.StartLevel)
            {
                return item.Path;
            }
        }
        return "1_1";
    }

    public Dictionary<int, List<GameBgCfg>> GetCfgDic()
    {
        return m_cfgDic;
    }

    public override void OnRemove()
    {
        m_cfgDic.Clear();
    }

    public override void OnReset()
    {
        m_cfgDic.Clear();
    }
}