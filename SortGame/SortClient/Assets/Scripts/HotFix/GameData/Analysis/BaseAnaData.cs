using UnityEngine;

public abstract class BaseAnaData
{
    [System.Serializable]
    public struct BAMetaData //数组结构,使用
    {
        public string id;
        public int num;
    }

    public abstract void ClearData();

    ///转换成json字符串
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    //将json字符串变成数据
    public BaseAnaData FromJson(string json)
    {
        return JsonUtility.FromJson<BaseAnaData>(json);
    }
    
}