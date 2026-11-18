using System;
using System.Collections.Generic;
using FishFramework;
using Unity.VisualScripting;
using UnityEngine;

public static partial class GameData
{
    public static ServerAreaData ServerArea { get; private set; }
    public static PlayerData Player { get; private set; }
    public static PayData Pay { get; private set; }
    public static StoryBookData StoryBook { get; private set; }
    public static LevelPassData LevelPass { get; private set; }
    public static ShopData Shop { get; private set; }
    public static ActivityData Activity { get; private set; }
    public static RankData Rank { get; private set; }
    public static GoldTreeActData GoldTreeActData { get; private set; }
    public static HotFixData HotFix { get; private set; }
    public static UnlockData UnlockData { get; private set; }

    public static string PrivacyUrl = "https://www.sofish.com/privacy-policy.html";
    public static string ServiceUrl = "https://www.sofish.com/terms-of-service.html";
    public static string AppUrl = "https://play.google.com/store/apps/details?id=com.sofish.soeasytile.gp";

    private static bool _bFromStartScene;
    public static bool BFromStartScene
    {
        set { _bFromStartScene = value; }
        get { return _bFromStartScene; }
    }

    private static readonly Dictionary<Type, BaseData> dataMaps = new();

    /// <summary>
    /// 游戏基础数据注册
    /// </summary>
    public static void RegisterData()
    {
        ServerArea = AddData(new ServerAreaData()) as ServerAreaData;
        AddData(new LoginData());
        Player = AddData(new PlayerData()) as PlayerData;
        Pay = AddData(new PayData()) as PayData;
        StoryBook = AddData(new StoryBookData()) as StoryBookData;
        LevelPass = AddData(new LevelPassData()) as LevelPassData;
        Shop = AddData(new ShopData()) as ShopData;
        Activity = AddData(new ActivityData()) as ActivityData;
        Rank = AddData(new RankData()) as RankData;
        GoldTreeActData = AddData(new GoldTreeActData()) as GoldTreeActData;
        UnlockData = AddData(new UnlockData()) as UnlockData;

        HotFix = new HotFixData();
    }

    /// <summary>
    /// 游戏基础数据清空
    /// </summary>
    public static void Clear()
    {
        ServerArea = null;
        Player = null;
        Pay = null;
        StoryBook = null;
        LevelPass = null;
        Shop = null;
        Activity = null;
        Rank = null;
        GoldTreeActData = null;
        HotFix = null;
        UnlockData = null;

        foreach (BaseData data in dataMaps.Values)
        {
            data.OnClear();
            data.OnRemove();
        }

        dataMaps.Clear();
        ResetServerTimeState();
    }

    private static BaseData AddData(BaseData data)
    {
        if (data == null)
        {
            Debug.Log("Data is invalid.");
            return null;
        }

        Type type = data.GetType();

        if (dataMaps.TryGetValue(type, out BaseData ret))
        {
            Debug.Log($"game data type '{type.FullName}' is already exist.");
            return ret;
        }

        data.OnInit();
        dataMaps[type] = data;
        return data;
    }

    public static T Get<T>() where T : BaseData
    {
        Type type = typeof(T);

        if (dataMaps.TryGetValue(type, out BaseData ret))
        {
            return ret as T;
        }

        return null;
    }

}

