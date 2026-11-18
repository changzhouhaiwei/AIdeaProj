using UnityEngine;
using UnityEditor;
using System.IO;
using YooAsset.Editor;

using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.SceneManagement;

public  class MaterialPathExtractor : EditorWindow
{
    
    //[MenuItem("Tools/Extract Material Path")]
    public static void ShowWindow()
    {
        //EditorWindow.GetWindow(typeof(MaterialPathExtractor));
    }

    private static Object prefabsFolder;
    private static Object secenFolder;

    void OnGUI()
    {
        GUILayout.Label("Extract Material Path", EditorStyles.boldLabel);
        //GameObject prefab = EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), true) as GameObject;
        // 显示文件夹路径的ObjectField
        prefabsFolder = EditorGUILayout.ObjectField("Prefab的文件夹：", prefabsFolder, typeof(DefaultAsset), false);
        secenFolder = EditorGUILayout.ObjectField("Scene的文件夹：", secenFolder, typeof(DefaultAsset), false);

        if (GUILayout.Button("Extract"))
        {
           
            Extract_MaterialPath();
        }
    }

    public static void ShowVisualElement(VisualElement parent)
    {
        ObjectField scene_ObjectField = new ObjectField("场景文件件");
        //scene_ObjectField.objectType = typeof(UnityEngine.Object); // 设置对象类型
        scene_ObjectField.objectType = typeof(DefaultAsset); // 设置对象类型
        scene_ObjectField.allowSceneObjects = false; // 允许选择场景中的对象
        scene_ObjectField.SetValueWithoutNotify(prefabsFolder);
        scene_ObjectField.RegisterValueChangedCallback(evt =>
        {
            secenFolder = scene_ObjectField.value;
        });

        parent.Add(scene_ObjectField);

        ObjectField prefab_ObjectFiedl = new ObjectField("预制件文件夹");
        prefab_ObjectFiedl.objectType = typeof(DefaultAsset); // 设置对象类型
        prefab_ObjectFiedl.allowSceneObjects = false; // 允许选择场景中的对象
        prefab_ObjectFiedl.SetValueWithoutNotify(prefabsFolder);
        prefab_ObjectFiedl.RegisterValueChangedCallback(evt =>
        {
            prefabsFolder = prefab_ObjectFiedl.value;

        });

        parent.Add(prefab_ObjectFiedl);
    }


    public static List<string> Extract_MaterialPath()
    {
        EditorTools.ClearUnityConsole();
        var secenPathList = Get_Scene_MaterialPath();
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);


        var prePathList = Get_Prefabs_MaterialPath();
        var allMaterial = secenPathList.Concat(prePathList).ToList();

        // 注意：AssetDatabase.FindAssets()可能会获取到重复的资源
        HashSet<string> result = new HashSet<string>();
        foreach (var assetPath in allMaterial)
        {
            if (result.Contains(assetPath) == false)
            {
                result.Add(assetPath);
            }
        }

        //装载
        allMaterial.Clear();
        foreach (var assetPath in result)
        {
            allMaterial.Add(assetPath);
        }
        //---------------------------------------------
        Debug.Log("材质球数量:" + allMaterial.Count);
        foreach (var path in allMaterial)
        {
            //Debug.Log("路径:" + path);
        }

        EditorTools.ClearProgressBar();
        // 返回结果
        return allMaterial;
    }

    public static List<string> Get_Scene_MaterialPath()
    {
        List<string> allMaterial = new List<string>(1000);
        string path = GetFloderPath(secenFolder);
        if (!string.IsNullOrEmpty(path))
        {
            var scenePathList = EditorTools.FindAssets(EAssetSearchType.Scene, path);
            int idx = 0;
            foreach (var sceneFullName in scenePathList)
            {
                ++idx;
                //LoadSceneParameters parameters = new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Single, localPhysicsMode = LocalPhysicsMode.None };
                //Scene scene = EditorSceneManager.LoadSceneInPlayMode(sceneFullName, parameters);

                Scene scene = EditorSceneManager.OpenScene(sceneFullName);
                //var scene = EditorSceneManager.GetSceneByPath(sceneFullName);
                var rootObject = scene.GetRootGameObjects();
                foreach (var gameObject in rootObject)
                {
                    //Debug.Log(gameObject.name);
                    Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        Material[] materials = renderer.sharedMaterials;
                        foreach (Material material in materials)
                        {
                            var m_path = AssetDatabase.GetAssetPath(material);
                            System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(m_path);
                            if (assetType == typeof(UnityEngine.Material))
                            {
                                allMaterial.Add(m_path);
                                //Debug.Log(m_path);
                                EditorTools.DisplayProgressBar(sceneFullName+ "材质球收集",idx, sceneFullName.Length);
                            }
                        }
                    }
                }
            }
        }
        return allMaterial;
    }

    public static List<string> Get_Prefabs_MaterialPath()
    {
        List<string> allMaterial = new List<string>(1000);
        string path = GetFloderPath(prefabsFolder);
        if (!string.IsNullOrEmpty(path))
        {
            int idx = 0;
            var prePathList = EditorTools.FindAssets(EAssetSearchType.Prefab, path);
            foreach (var assetPath in prePathList)
            {
                ++idx;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    Material[] materials = renderer.sharedMaterials;
                    foreach (Material material in materials)
                    {
                        var m_path = AssetDatabase.GetAssetPath(material);
                        System.Type assetType = AssetDatabase.GetMainAssetTypeAtPath(m_path);
                        if (assetType == typeof(UnityEngine.Material))
                        {
                            allMaterial.Add(m_path);
                            //Debug.Log(m_path);
                            EditorTools.DisplayProgressBar(assetPath + "材质球收集", idx, prePathList.Length);
                        }
                    }
                }
            }
        }
        return allMaterial;
    }

    public static string GetFloderPath(Object _selectedFolder)
    {
        if (_selectedFolder != null)
        {
            var path = AssetDatabase.GetAssetPath(_selectedFolder);
            if (Directory.Exists(path))
            {
                return path;
            }
        }
        return string.Empty;
    }
}
