using UnityEngine;
using FishFramework;

public static class PlatformMgr
{
    public static bool GetUseSDK()
    {
        var settings = GameModule.Setting.GetGameSettings();
        return settings.platform == ChannelPlatform.FishSDK;
    }

    public static void ExcuteLogin()
    {
        if (GetUseSDK())
        {
            // sdk登录成功后调用[CheckLoginServer]函数
        }
        else
        {
            ServerLoginReqData serverLoginReqData = new ServerLoginReqData
            {
                plat = "native",
                account = AccountData.GetFirstAccountId(),
                token = "",
                ver = 0
            };
            CheckLoginServer(serverLoginReqData);
        }
    }

    private static void CheckLoginServer(ServerLoginReqData serverLoginReqData)
    {
        string url = GameModule.Setting.GetServerLoginURL();
        Utility.RequestByJsonBodyPost(url,
            JsonUtility.ToJson(serverLoginReqData),
            (isOk, str) =>
            {
                if (isOk)
                {
                    JsonData jsonData = JsonUtility.FromJson<JsonData>(str);
                    if (int.Parse(jsonData.code) == 0)
                    {
                        GameData.ServerArea.SaveServerAddressData(jsonData.data);
                        GameEvent.Send((int)EventNameDef.LoginServer, true, jsonData.msg);
                    }
                    else
                    {
                        GameEvent.Send((int)EventNameDef.LoginServer, false, jsonData.msg);
                    }
                }
                else
                {
                    GameEvent.Send((int)EventNameDef.LoginServer, false, str);
                }
            });
    }
}