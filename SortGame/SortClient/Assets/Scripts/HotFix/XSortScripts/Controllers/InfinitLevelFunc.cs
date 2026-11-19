

using System.Collections.Generic;

public class InfinitLevelFunc
{
    private static InfinitLevelFunc _instance;
    public static InfinitLevelFunc Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new InfinitLevelFunc();
            }
            return _instance;
        }
    }

    private LevelGenerator<int> generator;
    InfinitLevelFunc()
    {
        var pool = new List<int>();
        for (int i = 34; i <= 84; i++)
        {
            pool.Add(i);
        }

        generator = new LevelGenerator<int>(pool, baseSeed: 999);
    }

    //获得实际关卡
    public int GetRealLevelNumByIdx(int index)
    {
        int retVal = generator.GetValue(index);
        return retVal;
    }
}