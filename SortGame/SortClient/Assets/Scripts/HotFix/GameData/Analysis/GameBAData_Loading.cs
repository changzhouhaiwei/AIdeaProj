using FishFramework;
using UnityEngine;

/// <summary>
/// 与 Mahjong 分文件一致；TrackEvent 在 Mahjong 为空实现，Tile2 走 GPSDKMgr 以保证加载步骤可上报。
/// </summary>
public static partial class GameBAData
{
    private class PreLoadingData
    {
        public readonly string key = "loading_event";
        public string client_event_fail_code;
        public string client_event_fail_msg;
        public string client_event_id;
        public string client_event_name;
        public bool is_key_event_step;
        public string app_version;
    }

    static void TrackEvent(string key, string json)
    {
        GPSDKMgr.Instance.TrackEvent(key, json);
    }

    public static void ExcuteLoadingEvent10008()
    {
        var oneData = new PreLoadingData
        {
            client_event_id = "10008",
            client_event_name = "加载主程序",
            is_key_event_step = true,
            app_version = GameModule.Setting.GetGameSettings().GetVersion(),
        };
        TrackEvent(oneData.key, JsonUtility.ToJson(oneData));
    }

    public static void ExcuteLoadingEvent10009()
    {
        var oneData = new PreLoadingData
        {
            client_event_id = "10009",
            client_event_name = "加载主程序完成",
            is_key_event_step = true,
            app_version = GameModule.Setting.GetGameSettings().GetVersion(),
        };
        TrackEvent(oneData.key, JsonUtility.ToJson(oneData));
    }

    public static void ExcuteLoadingEvent10010()
    {
        var oneData = new PreLoadingData
        {
            client_event_id = "10010",
            client_event_name = "到达登录界面",
            is_key_event_step = true,
            app_version = GameModule.Setting.GetGameSettings().GetVersion(),
        };
        TrackEvent(oneData.key, JsonUtility.ToJson(oneData));
    }

    public static void ExcuteLoadingEvent10014()
    {
        var oneData = new PreLoadingData
        {
            client_event_id = "10014",
            client_event_name = "开始加载配置表",
            is_key_event_step = true,
            app_version = GameModule.Setting.GetGameSettings().GetVersion(),
        };
        TrackEvent(oneData.key, JsonUtility.ToJson(oneData));
    }

    public static void ExcuteLoadingEvent10015()
    {
        var oneData = new PreLoadingData
        {
            client_event_id = "10015",
            client_event_name = "配置表加载完成",
            is_key_event_step = true,
            app_version = GameModule.Setting.GetGameSettings().GetVersion(),
        };
        TrackEvent(oneData.key, JsonUtility.ToJson(oneData));
    }

    public static void ExcuteLoadingEvent10016()
    {
        var oneData = new PreLoadingData
        {
            client_event_id = "10016",
            client_event_name = "加载global资源",
            is_key_event_step = true,
            app_version = GameModule.Setting.GetGameSettings().GetVersion(),
        };
        TrackEvent(oneData.key, JsonUtility.ToJson(oneData));
    }

    public static void ExcuteLoadingEvent10017()
    {
        var oneData = new PreLoadingData
        {
            client_event_id = "10017",
            client_event_name = "加载global资源完成",
            is_key_event_step = true,
            app_version = GameModule.Setting.GetGameSettings().GetVersion(),
        };
        TrackEvent(oneData.key, JsonUtility.ToJson(oneData));
    }

    public static void ExcuteLoadingEvent10029()
    {
        var oneData = new PreLoadingData
        {
            client_event_id = "10029",
            client_event_name = "到达主界面",
            is_key_event_step = true,
            app_version = GameModule.Setting.GetGameSettings().GetVersion(),
        };
        TrackEvent(oneData.key, JsonUtility.ToJson(oneData));
    }
}
