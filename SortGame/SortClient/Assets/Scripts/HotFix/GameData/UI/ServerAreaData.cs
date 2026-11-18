using System;

[Serializable]
public class ServerLoginReqData
{
    public string plat;
    public string account;
    public string token;
    public uint ver;
}

[Serializable]
public class ServerLoginRspData
{
    public string addr;
    public string port;
    public uint uid;
    public string token;
}

[Serializable]
public class JsonData
{
    public string code;
    public string msg;
    public ServerLoginRspData data;
}

public class ServerAreaData : BaseData
{
    private ServerLoginRspData serverLoginRspData;

    public override void OnInit()
    {
    }

    public override void OnReset()
    {
    }

    public override void OnRemove()
    {
    }

    public void SaveServerAddressData(ServerLoginRspData data)
    {
        serverLoginRspData = data;
    }

    public ServerLoginRspData GetServerLoginRspData()
    {
        return serverLoginRspData;
    }

    public string GetServerAddress()
    {
        return $"ws://{serverLoginRspData.addr}:{serverLoginRspData.port}/ws";
    }
}