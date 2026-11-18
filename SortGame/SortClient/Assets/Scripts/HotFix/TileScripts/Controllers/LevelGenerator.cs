using System;
using System.Collections.Generic;
// using log4net.Core;

public class LevelGenerator<T>
{
    private readonly List<T> pool;
    private readonly int baseSeed;

    public LevelGenerator(List<T> pool, int baseSeed = 12345)
    {
        this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
        this.baseSeed = baseSeed;
    }

    public T GetValue(int level) //35-85 取到的数字是从35-85之前
    {
        if (level <= 0) throw new ArgumentException("关卡号必须 >= 1");

        int segmentSize = pool.Count;
        int segmentIndex = (level - 1) / segmentSize; // 属于第几段
        int indexInSegment = (level - 1) % segmentSize; // 段内第几个

        // 用 (baseSeed + segmentIndex) 生成随机序列
        var rnd = new Random(baseSeed + segmentIndex);

        // 复制一份 pool
        var shuffled = new List<T>(pool);

        // Fisher-Yates 洗牌
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled[indexInSegment];
    }


}
