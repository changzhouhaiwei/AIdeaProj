using System.Globalization;
using Unity.Mathematics;
using UnityEngine;
using FishFramework;

public static class GameUtility
{
    public static GameSettings LoadGameSettings()
    {
        var settings = Resources.Load<GameSettings>(nameof(GameSettings));
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<GameSettings>();
        }

        return settings;
    }

    /// <summary>
    /// 世界坐标转换为画布坐标
    /// </summary>
    /// <param name="canvas">画布</param>
    /// <param name="world">世界坐标</param>
    /// <returns></returns>
    public static Vector2 WorldToCanvasPos(Canvas canvas, Vector3 worldPos)
    {
        //转换成屏幕坐标
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldPos);

        //转换成局部坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.GetComponent<RectTransform>(), screenPos, canvas.worldCamera, out Vector2 localpoint);
        return localpoint;
    }

    const int UidBucketCount = 4096;

    /// <summary>
    /// 将稳定 ID 的完整字符串哈希到 4096 个桶，按各组自定义占比切组，返回组名 A/B/C/...
    /// ID 可为 UUID、数字或其他格式，不要求是十六进制。
    /// </summary>
    public static string GetUserGroupByStableId(string stableId, float[] ratios)
    {
        if (string.IsNullOrWhiteSpace(stableId) || ratios == null || ratios.Length == 0)
            return "A";

        return GetUserGroupByBucket(GetStableIdBucket(stableId), ratios);
    }

    /// <summary>兼容旧版 UID 末三位十六进制分桶。</summary>
    public static string GetUserGroupByUid(string uid, float[] ratios)
    {
        if (string.IsNullOrWhiteSpace(uid) || ratios == null || ratios.Length == 0)
            return "A";

        return GetUserGroupByBucket(GetUidLast3HexBucket(uid), ratios);
    }

    static string GetUserGroupByBucket(int bucket, float[] ratios)
    {
        int groupCount = math.min(ratios.Length, 26);
        float cumulative = 0f;
        for (int i = 0; i < groupCount; i++)
        {
            float r = ratios[i];
            if (r < 0f)
                r = 0f;
            cumulative += r;
            int endExclusive = math.min(UidBucketCount, (int)(cumulative * UidBucketCount + 0.5f));
            if (i == groupCount - 1)
                endExclusive = UidBucketCount;
            if (bucket < endExclusive)
                return ((char)('A' + i)).ToString();
        }

        return ((char)('A' + groupCount - 1)).ToString();
    }

    public static int GetStableIdBucket(string stableId)
    {
        if (string.IsNullOrWhiteSpace(stableId))
            return 0;

        // FNV-1a 后追加 avalanche，确保数字串、UUID 等不同格式都能均匀使用低 12 位。
        unchecked
        {
            uint hash = 2166136261u;
            for (int i = 0; i < stableId.Length; i++)
            {
                hash ^= stableId[i];
                hash *= 16777619u;
            }

            hash ^= hash >> 16;
            hash *= 0x7feb352du;
            hash ^= hash >> 15;
            hash *= 0x846ca68bu;
            hash ^= hash >> 16;
            return (int)(hash & (UidBucketCount - 1));
        }
    }

    /// <summary>旧版末三位十六进制桶算法，仅保留给历史工具或诊断使用。</summary>
    public static int GetUidLast3HexBucket(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid) || uid.Length < 3)
            return 0;

        string last3 = uid.Substring(uid.Length - 3);
        if (!int.TryParse(last3, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int bucket))
            return 0;

        return math.clamp(bucket, 0, UidBucketCount - 1);
    }
}