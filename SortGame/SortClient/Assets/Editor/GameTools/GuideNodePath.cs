using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using FishFramework;

public static class GuideNodePath
{
    private static string m_FullPath = string.Empty;
    private static string m_PathNotIncludeRoot = string.Empty;

    private static GameObject prefabRoot;

    private static string nodePath;

    [MenuItem("新手引导/输出新手引导节点路径", priority = 200)]
    public static void OutputNodePath()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogError("请选择一个prefab实例");
            return;
        }

        GameObject nodeGo = Selection.activeGameObject;
        Transform tempTf = nodeGo.transform.parent;
        if (tempTf)
        {
            prefabRoot = tempTf.gameObject;
            do
            {
                tempTf = tempTf.parent;
                if (tempTf && !tempTf.name.Equals("Canvas (Environment)") && !tempTf.name.Equals(UIModule.UILayerRootName))
                {
                    prefabRoot = tempTf.gameObject;
                }
                else
                {
                    tempTf = null;
                }
            } while (tempTf);
        }
        else
        {
            prefabRoot = nodeGo;
        }

        var PrefabNodePath = Path.Combine(Application.dataPath, "PrefabNodePath/");
        if (!Directory.Exists(PrefabNodePath)) Directory.CreateDirectory(PrefabNodePath);

        nodePath = Path.Combine(Application.dataPath, "PrefabNodePath/NodePath.txt");
        if (File.Exists(nodePath))
            File.Delete(nodePath);

        Deal(nodeGo);
        FileWriteReadText();
    }

    private static void Deal(GameObject nodeGo)
    {
        if (GameObject.Equals(nodeGo, prefabRoot))
            return;

        m_FullPath = "";
        m_PathNotIncludeRoot = "";

        Stack<string> nodeNameStack = new Stack<string>();
        nodeNameStack.Push(nodeGo.name);
        do
        {
            nodeGo = nodeGo.transform.parent.gameObject;
            nodeNameStack.Push(nodeGo.name);
        } while (!GameObject.Equals(nodeGo, prefabRoot));

        nodeNameStack.Push(prefabRoot.name);

        int i = 0;
        foreach (var nodeName in nodeNameStack)
        {
            if (i > 0)
            {
                if (i > 1)
                {
                    m_PathNotIncludeRoot += "/";
                }

                m_FullPath += "/";
                m_PathNotIncludeRoot += nodeName;
            }

            m_FullPath += nodeName;
            i++;
        }
    }

    private static void FileWriteReadText()
    {
        File.WriteAllText(nodePath, m_PathNotIncludeRoot);
        AssetDatabase.Refresh();
        //用记事本
        System.Diagnostics.Process.Start("notepad.exe", nodePath);
    }
}