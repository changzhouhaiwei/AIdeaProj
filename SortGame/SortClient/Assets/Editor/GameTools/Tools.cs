using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class Tools : MonoBehaviour
{
    [MenuItem("游戏工具/工程目录")]
    private static void OpenProject()
    {
        System.Diagnostics.Process.Start(Environment.CurrentDirectory);
    }

    
    // [MenuItem("GoToScene/InitScene", false, 0)]
    // private static void GoToMark3DemoScene()
    // {
    //     if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
    //     {
    //         EditorSceneManager.OpenScene("Assets/TileExplorer/Game/Scenes/Init.unity");
    //     }
    // }


    [MenuItem("GoToScene/GameScene", false, 0)]
    private static void GoToMyGameScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/CABRes/Scenes/GameScene.unity");
        }
    }


    [MenuItem("GoToScene/StartScene %#g", false, 2)]
    private static void GoToStartScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/CABRes/ScenesInBuild/StartScene.unity");
        }
    }

    [MenuItem("GoToScene/GameSceneOri(Deprecated)", false, 4)]
    public static void GoToGameScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/TileExplorer/Game/Scenes/GameSceneOri.unity");
        }
    }


    // [MenuItem("GoToScene/FightScene", false, 500)]
    // private static void GoToFightScene()
    // {
    //     if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
    //     {
    //         EditorSceneManager.OpenScene("Assets/CABRes/Scenes/FightScene.unity");
    //     }
    // }
}