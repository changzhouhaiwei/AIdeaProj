using System;

public static partial class GameData
{
    private static long deltaTime;
    private static bool gmServerTimeOverride;
    private static long gmServerTimeSnapshotDelta;
    private static bool gmServerTimeSnapshotActive;

    public static bool IsGmServerTimeOverride => gmServerTimeOverride;

    public static long GetNowTime()
    {
        long timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        return timestamp;
    }

    public static void SetServerTime(long t)
    {
        deltaTime = GetNowTime() - t;
    }

    public static void SetGmServerTime(long targetUnixSeconds)
    {
        deltaTime = GetNowTime() - targetUnixSeconds;
        gmServerTimeOverride = true;
    }

    public static void ResetGmServerTimeToSystem()
    {
        deltaTime = 0;
        gmServerTimeOverride = false;
    }

    internal static void ResetServerTimeState()
    {
        deltaTime = 0;
        gmServerTimeOverride = false;
    }

    internal static void SnapshotGmServerTimeForRelogin()
    {
        if (gmServerTimeOverride)
        {
            gmServerTimeSnapshotDelta = deltaTime;
            gmServerTimeSnapshotActive = true;
        }
        else
        {
            gmServerTimeSnapshotActive = false;
        }
    }

    internal static void ApplyGmServerTimeSnapshotAfterClear()
    {
        if (!gmServerTimeSnapshotActive)
        {
            return;
        }

        deltaTime = gmServerTimeSnapshotDelta;
        gmServerTimeOverride = true;
        gmServerTimeSnapshotActive = false;
    }

    public static long GetServerTime()
    {
        return GetNowTime() - deltaTime;
    }

    public static DateTime GetGameLocalDateTime()
    {
        return DateTimeOffset.FromUnixTimeSeconds(GetServerTime()).LocalDateTime;
    }
}
