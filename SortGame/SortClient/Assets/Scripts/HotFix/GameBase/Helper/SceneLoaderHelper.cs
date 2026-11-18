using System;
using System.Collections.Generic;
using xasset;
using FishFramework;

public static class SceneLoaderHelper
{
    public static GameSceneName RunningSceneName { get; private set; }

    private static readonly Dictionary<GameSceneName, ISceneEnter> sceneEnterMap = new()
    {
    };

    private static readonly List<GameSceneName> sceneNameList = new();


    public static void OpenScene(GameSceneName sceneName, Action completed = null, bool needBack = false, bool needLoading = true)
    {
        GameModule.UI.DestroyAllPanel();
        if (needBack)
        {
            sceneNameList.Add(sceneName);
        }

        SwitchScene(sceneName, completed, needLoading);
    }

    public static void OpenSceneNew(GameSceneName sceneName, Action completed = null, bool needBack = false, bool needLoading = true)
    {
        // GameModule.UI.DestroyAllPanel();
        if (needBack)
        {
            sceneNameList.Add(sceneName);
        }

        SwitchScene(sceneName, completed, needLoading);
    }

    public static void BackScene(Action completed = null, bool needLoading = true)
    {
        GameModule.UI.DestroyAllPanel();
        GameSceneName sceneName = sceneNameList.Count > 0 ? sceneNameList[^1] : GameSceneName.HomeScene;
        SwitchScene(sceneName, completed, needLoading);
    }

    private static void SwitchScene(GameSceneName sceneName, Action completed, bool needLoading)
    {
        RunningSceneName = sceneName;

        LoadingUI loadingUI = null;
        if (needLoading)
        {
            loadingUI = GameModule.UI.OpenPanel<LoadingUI>(PanelLayer.Tips, PanelOpenType.Single);
        }

        Scene loading = Scene.LoadAsync($"Assets/CABRes/Scenes/{sceneName.ToString()}.unity");
        loading.completed += scene =>
        {
            if (loadingUI != null)
            {
                GameModule.UI.DestroyPanel<LoadingUI>();
                loadingUI = null;
            }

            completed?.Invoke();
            if (sceneEnterMap.TryGetValue(sceneName, out ISceneEnter sceneEnter))
            {
                sceneEnter.OnInit();
            }
        };
        loading.updated += scene => { loadingUI?.SetMessage(scene.progress); };
    }
}