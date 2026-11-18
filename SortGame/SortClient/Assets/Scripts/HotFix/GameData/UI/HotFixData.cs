
using System.Collections.Generic;

public class HotFixData
{
    public List<int> toCloseLevelDic;
    private string jsonStr = "";

    public void SetJsonStr(string _jStr)
    {
        jsonStr = _jStr;
    }
    
    public void SaveToLocal()//保存到本地
    {
        
    }
}

//支持修复类型
//1.关闭的关卡
//4.