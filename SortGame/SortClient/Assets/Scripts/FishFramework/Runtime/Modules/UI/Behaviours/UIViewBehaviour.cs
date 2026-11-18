using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishFramework
{
    [Serializable]
    public class UIOpElement
    {
        [SerializeField] private GameObject m_Target; //目标物体，可能为null（未设置 或 引用的丢失但未刷新）。

        [SerializeField] private List<Component> m_ComponentList; //组件列表，可能为null（引用的丢失但未刷新）。

        public GameObject target
        {
            set => m_Target = value;
            get => m_Target;
        }

        public List<Component> componentList => m_ComponentList;

        public UIOpElement()
        {
            m_Target = null;
            m_ComponentList = new List<Component>();
        }

        public T GetComponentByIndex<T>(int index) where T : Component
        {
            if (m_ComponentList == null || index < 0 || index >= m_ComponentList.Count)
            {
                return null;
            }

            return m_ComponentList[index] as T;
        }
    }

    [DisallowMultipleComponent]
    public abstract class UIViewBehaviour : MonoBehaviour
    {
        [SerializeField] protected List<UIOpElement> m_OpElementList;
        public List<UIOpElement> opElementList => m_OpElementList;

        public bool HasSavedGameObject(GameObject go)
        {
            for (int i = 0; i < opElementList.Count; i++)
            {
                UIOpElement opElement = opElementList[i];
                if (go.Equals(opElement.target)) //go必定不为null, element.target可能为null
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasSavedComponent(GameObject go, Component comp)
        {
            for (int i = 0; i < opElementList.Count; i++)
            {
                UIOpElement element = opElementList[i];
                if (!go.Equals(element.target)) //go必定不为null, element.target可能为null
                {
                    continue; //target不同时，无需继续对组件列表进行遍历
                }

                for (int j = 0; j < element.componentList.Count; j++)
                {
                    Component savedComp = element.componentList[j];
                    if (comp.Equals(savedComp)) //comp必定不为null, savedComp可能为null
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public GameObject GetGameObjectByIndex(int index)
        {
            if (m_OpElementList == null || index < 0 || index >= m_OpElementList.Count)
            {
                return null;
            }

            return m_OpElementList[index].target;
        }

        public T GetComponentByIndexs<T>(int index1, int index2) where T : Component
        {
            if (m_OpElementList == null || index1 < 0 || index1 >= m_OpElementList.Count)
            {
                return null;
            }

            return m_OpElementList[index1].GetComponentByIndex<T>(index2);
        }


#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
        }

        protected virtual void Reset()
        {
            m_OpElementList = new List<UIOpElement>();
        }
#endif
        private void Awake()
        {
            hideFlags = HideFlags.NotEditable;
        }

    }
}