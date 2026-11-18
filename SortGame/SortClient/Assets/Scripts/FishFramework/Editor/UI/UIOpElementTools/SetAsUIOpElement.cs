using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FishFramework
{
    /// <summary>
    /// 将选中的游戏物体加入加入最近一层父 UIViewBehaviour 中。
    /// 同时推测出该游戏物体“最可能要操作的”的一个组件加入 组件列表。
    /// 推测优先规则如下：
    /// 1、如果存在【可交互组件】，则加入。
    /// 2、否则，如果存在【自定义需要添加的组件】，则加入。
    /// 3、否则，如果存在【布局相关组件】，则加入（优先级相对较低，但高于图形）。
    /// 4、否则，如果存在【图形组件】，则加入。
    /// 5、否则，将 【RectTransform 或 Transform】 加入（保底）。
    /// (若要操作多个组件，请手动添加)
    /// (若要操作自定义的脚本，请手动添加)
    /// (嵌套的子 UIViewBehaviour 是否加入父 UIViewBehaviour 中？ 否，如有需要就手动添加。)
    /// </summary>
    public class SetAsUIOpElement : Editor
    {
        private static readonly HashSet<string> sm_CustomSet = new HashSet<string>()
        {
            // "ScrollRect",
            "Canvas", "Camera",
            "EventTrigger",
            "AudioListener", "AudioSource",
            "VideoPlayer",
            "Animator", "Animation",
            "PlayableDirector",
            "LoopListView2",
            "LoopGridView",
            "UIWidgetBehaviour"
            //...
        };

        [MenuItem("GameObject/UITools/SetAsUIOpElement &s", false, 0)]
        public static void DoSet()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length <= 0)
            {
                return;
            }

            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                GameObject go = Selection.gameObjects[i];
                // 获取当前对象的父对象,跳过自身检查
                Transform parentTransform = go.transform.parent;
                if (parentTransform == null)
                {
                    return;
                }

                UIViewBehaviour behaviour = parentTransform.GetComponentInParent<UIViewBehaviour>(true);

                if (behaviour == null)
                {
                    return;
                }

                //找到第一个已保存的element或创建
                UIOpElement opElement = behaviour.opElementList.Find((x) => { return go.Equals(x.target); });
                if (opElement == null)
                {
                    opElement = new UIOpElement();
                    opElement.target = go;
                    behaviour.opElementList.Add(opElement);
                }

                Component[] comps = go.GetComponents<Component>();

                Component selectableComp = null;
                Component layoutComp = null;
                Component customComp = null;
                Component graphicComp = null;
                Component transformComp = null;

                for (int j = 0; j < comps.Length; j++)
                {
                    Component comp = comps[j];

                    //Debug.Log("comp.GetType().Name: " + comp.GetType().Name);

                    if (comp is Selectable && selectableComp == null)
                    {
                        selectableComp = comp;
                    }

                    if (sm_CustomSet.Contains(comp.GetType().Name) && customComp == null)
                    {
                        customComp = comp;
                    }

                    if (comp is LayoutGroup or ContentSizeFitter or AspectRatioFitter or LayoutElement && layoutComp == null)
                    {
                        layoutComp = comp;
                    }

                    if (comp is Graphic && graphicComp == null)
                    {
                        graphicComp = comp;
                    }

                    if (comp is Transform && transformComp == null)
                    {
                        transformComp = comp;
                    }
                }

                //Debug.Log(go + "------------------------");
                //Debug.Log(selectableComp);
                //Debug.Log(layoutComp);
                //Debug.Log(customComp);
                //Debug.Log(graphicComp);
                //Debug.Log(transformComp);

                Component theComp = selectableComp ?? customComp ?? layoutComp ?? graphicComp ?? transformComp;

                if (!behaviour.HasSavedComponent(go, theComp))
                {
                    opElement.componentList.Add(theComp);
                }

                EditorUtility.SetDirty(behaviour);
            }

            AssetDatabase.Refresh();
        }
    }
}