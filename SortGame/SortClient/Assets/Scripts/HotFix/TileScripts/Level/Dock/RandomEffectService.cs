using UnityEngine;

public class RandomEffectService
{
    private int[] values = { 0, 1, 2, 3, 4 };
    private float[] weights;
    private float baseWeight = 1f;
    private float penaltyFactor = 0.3f; // 惩罚系数，越小越难连出

    public RandomEffectService()
    {
        weights = new float[values.Length];
        for (int i = 0; i < weights.Length; i++)
            weights[i] = baseWeight;
    }

    public int Next()
    {
        // 按权重随机
        float total = 0f;
        foreach (var w in weights) total += w;
        float r = Random.value * total;

        int chosen = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (r < weights[i])
            {
                chosen = i;
                break;
            }
            r -= weights[i];
        }

        // 更新权重：降低选中的，恢复其他的
        for (int i = 0; i < weights.Length; i++)
        {
            if (i == chosen)
                weights[i] *= penaltyFactor;  // 惩罚，降低概率
            else
                weights[i] = Mathf.Min(baseWeight, weights[i] + 0.1f); // 慢慢恢复
        }

        return values[chosen];
    }
}
