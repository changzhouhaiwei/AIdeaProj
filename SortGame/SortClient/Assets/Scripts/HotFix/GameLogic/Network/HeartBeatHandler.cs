using System;
using FishFramework;

public class HeartBeatHandler
{
    public bool IsConnect { get; set; }

    private const float heartBeatTime = 3f;
    private int failCount;
    private Timer timer;

    public void DoHeartBeat()
    {
        failCount = 0;
        StopHeartBeat();
        timer = Timer.PersistenceLoopAction(heartBeatTime, SendHeatBeatReq, executeOnStart: true);
    }

    private void StopHeartBeat()
    {
        if (timer == null) return;
        Timer.Cancel(timer);
        timer = null;
    }

    private void SendHeatBeatReq()
    {
        if (IsConnect)
        {

        }
        else
        {

        }

        IsConnect = false;
    }
}