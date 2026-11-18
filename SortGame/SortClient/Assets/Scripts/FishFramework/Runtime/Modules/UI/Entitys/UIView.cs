using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using xasset;
using Object = UnityEngine.Object;

namespace FishFramework
{
    public abstract partial class UIView
    {
        public virtual PanelLayer Layer { set; get; }
        protected virtual string PrefabPath => string.Empty;
        protected virtual string PoolObjPath => string.Empty;

        protected bool isPrefabPool = false;

        protected string viewId;
        private Transform parentTransform;
        protected UIViewBehaviour viewBehaviour;
        protected RectTransform rectTransform;
        protected GameObject gameObject;

        private Dictionary<string, UIWidget> widgetDict { set; get; }

        private static readonly Dictionary<string, Asset> cacheAssets = new();

        #region 创建关闭接口

        protected void Create(string view_Id, Transform parent_Tf)
        {
            GameObject go = GameObject.Find(PoolObjPath);
            isPrefabPool = (go != null);
            
            if (go == null)
            {
                GameObject prefab = LoadAsset<GameObject>(PrefabPath);
                go = Object.Instantiate(prefab);
            }

            UIViewBehaviour view_Behaviour = go.GetComponent<UIViewBehaviour>();
            Debug.Assert(view_Behaviour != null, "UIViewBehaviour组件不存在");

            Create(view_Id, parent_Tf, view_Behaviour);
        }

        protected void Create(string view_Id, Transform parent_Tf, UIViewBehaviour view_Behaviour)
        {
            viewId = view_Id;
            parentTransform = parent_Tf;
            viewBehaviour = view_Behaviour;

            OnInternalCreating();
            OnBindCompsAndEvents();
            OnCreating();

            OnInternalCreated();
        }

        protected void Destroy()
        {
            //先递归销毁子
            DestroyAllWidgets();

            OnDestroying();
            OnUnbindCompsAndEvents();
            OnInternalDestroying();
            OnInternalDestroyed();
            OnDestroyed();
            Clear();
        }

        #endregion

        #region Widget操作相关接口

        public T CreateWidget<T>(string widgetId, Transform parentTf) where T : UIWidget, new()
        {
            UIViewBehaviour parentViewBehaviour = Utility.FindInParents<UIViewBehaviour>(parentTf.gameObject);
            Debug.Assert(viewBehaviour.Equals(parentViewBehaviour)); //必须以当前最近UIView的元素作为UIWidget的父节点

            T widget = Activator.CreateInstance<T>();
            widget.Create(widgetId, this, parentTf);
            if (widgetDict == null)
            {
                widgetDict = new Dictionary<string, UIWidget>();
            }

            widgetDict.Add(widgetId, widget);
            return widget;
        }

        public T CreateWidget<T>(string widgetId, Transform parentTf, UIWidgetBehaviour widgetBehaviour) where T : UIWidget, new()
        {
            T widget = Activator.CreateInstance<T>();
            widget.Create(widgetId, this, parentTf, widgetBehaviour);
            if (widgetDict == null)
            {
                widgetDict = new Dictionary<string, UIWidget>();
            }

            widgetDict.Add(widgetId, widget);
            return widget;
        }

        public T CreateWidget<T>(Transform parentTf) where T : UIWidget, new()
        {
            return CreateWidget<T>(typeof(T).Name, parentTf);
        }

        public T CreateWidget<T>(Transform parentTf, UIWidgetBehaviour widgetBehaviour)
            where T : UIWidget, new()
        {
            return CreateWidget<T>(typeof(T).Name, parentTf, widgetBehaviour);
        }

        public T CreateWidget<T>(UIWidgetBehaviour widgetBehaviour) where T : UIWidget, new()
        {
            return CreateWidget<T>(typeof(T).Name, widgetBehaviour.transform.parent, widgetBehaviour);
        }

        public T CreateWidget<T>(string widgetId, UIWidgetBehaviour widgetBehaviour) where T : UIWidget, new()
        {
            return CreateWidget<T>(widgetId, widgetBehaviour.transform.parent, widgetBehaviour);
        }

        public void DestroyWidget(string widgetId)
        {
            // widgetDict未创建
            if (widgetDict == null) return;

            // widget不存在
            if (!widgetDict.ContainsKey(widgetId)) return;

            //  解除引用
            UIWidget widget = widgetDict[widgetId];
            widgetDict.Remove(widgetId);

            //  开始销毁
            widget.Destroy();
        }

        public void DestroyWidget<T>() where T : UIWidget
        {
            DestroyWidget(typeof(T).Name);
        }

        public void DestroyAllWidgets()
        {
            if (widgetDict == null || widgetDict.Count <= 0)
            {
                return;
            }

            List<string> widgetIds = new List<string>();
            foreach (KeyValuePair<string, UIWidget> kvPair in widgetDict)
            {
                widgetIds.Add(kvPair.Key);
            }

            foreach (string widgetId in widgetIds)
            {
                DestroyWidget(widgetId);
            }

            widgetDict = null;
        }

        public UIWidget GetWidget(string widgetId)
        {
            return widgetDict[widgetId];
        }

        public T GetWidget<T>(string widgetId) where T : UIWidget
        {
            return widgetDict[widgetId] as T;
        }

        public T GetWidget<T>() where T : UIWidget
        {
            return GetWidget(typeof(T).Name) as T;
        }

        public bool ExistWidget(string widgetId)
        {
            if (widgetDict == null)
            {
                return false;
            }

            return widgetDict.ContainsKey(widgetId);
        }

        #endregion

        #region 反射获取组件相关接口

        public int FindComponent<T>(string compDefine, out T comp) where T : Component
        {
            comp = null;
            if (string.IsNullOrEmpty(compDefine))
            {
                return FindCompErrorCode.COMP_DEFINE_IS_NULL_OR_EMPTY;
            }

            FieldInfo fieldInfo = this.GetType().GetField(compDefine, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                return FindCompErrorCode.NOT_EXIST_THIS_COMPONENT;
            }

            T value = fieldInfo.GetValue(this) as T;
            if (value == null)
            {
                return FindCompErrorCode.ERROR_CAST_TYPE;
            }

            comp = value;
            return FindCompErrorCode.OK;
        }

        public int FindWidgetComponent<T>(string[] widgetIds, string compDefine, out T comp) where T : Component
        {
            comp = null;
            if (widgetIds == null || widgetIds.Length <= 0)
            {
                return FindCompErrorCode.WIDGETS_ID_IS_NULL_OR_EMPTY;
            }

            UIView view = this;
            for (int i = 0; i < widgetIds.Length; i++)
            {
                if (view.widgetDict == null)
                {
                    return FindCompErrorCode.NOT_EXIST_ANY_CHILD_WIDGET;
                }

                string widgetId = widgetIds[i];
                if (!view.ExistWidget(widgetId))
                {
                    return FindCompErrorCode.NOT_EXIST_THIS_CHILD_WIDGET;
                }

                view = view.GetWidget(widgetId);
            }

            return view.FindComponent<T>(compDefine, out comp);
        }

        #endregion

        #region 组件事件绑定

        protected void BindEvent(Button button)
        {
            if (button == null)
                return;
            button.onClick.AddListener(() => { OnClicked(button); });
        }

        protected void BindEvent(Toggle toggle)
        {
            toggle.onValueChanged.AddListener((value) => { OnValueChanged(toggle, value); });
        }

        protected void BindEvent(Dropdown dropdown)
        {
            dropdown.onValueChanged.AddListener((value) => { OnValueChanged(dropdown, value); });
        }

        protected void BindEvent(InputField inputField)
        {
            inputField.onValueChanged.AddListener((value) => { OnValueChanged(inputField, value); });
        }

        protected void BindEvent(Slider slider)
        {
            slider.onValueChanged.AddListener((value) => { OnValueChanged(slider, value); });
        }

        protected void BindEvent(Scrollbar scrollbar)
        {
            scrollbar.onValueChanged.AddListener((value) => { OnValueChanged(scrollbar, value); });
        }

        protected void BindEvent(ScrollRect scrollRect)
        {
            scrollRect.onValueChanged.AddListener((value) => { OnValueChanged(scrollRect, value); });
        }

        protected void UnbindEvent(Button button)
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
        }

        protected void UnbindEvent(Toggle toggle)
        {
            toggle.onValueChanged.RemoveAllListeners();
        }

        protected void UnbindEvent(Dropdown dropdown)
        {
            dropdown.onValueChanged.RemoveAllListeners();
        }

        protected void UnbindEvent(InputField inputField)
        {
            inputField.onValueChanged.RemoveAllListeners();
        }

        protected void UnbindEvent(Slider slider)
        {
            slider.onValueChanged.RemoveAllListeners();
        }

        protected void UnbindEvent(Scrollbar scrollbar)
        {
            scrollbar.onValueChanged.RemoveAllListeners();
        }

        protected void UnbindEvent(ScrollRect scrollRect)
        {
            scrollRect.onValueChanged.RemoveAllListeners();
        }

        #endregion

        #region 内部生命周期

        protected virtual void OnInternalCreating()
        {
            rectTransform = viewBehaviour.gameObject.GetComponent<RectTransform>();
            gameObject = viewBehaviour.gameObject;

            if (rectTransform.parent != parentTransform)
            {
                rectTransform.SetParent(parentTransform, false);
            }
        }

        protected virtual void OnInternalCreated()
        {
        }

        protected virtual void OnInternalDestroying()
        {
            RemoveAllUIEvent();
            UnscheduleAll();
            if (isPrefabPool)
            {
                gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(-2000f, 0f);
            }
            else
            {
                Object.Destroy(gameObject); //这里位置进行了destroy
            }
            gameObject = null;
            rectTransform = null;
            viewBehaviour = null;
            parentTransform = null;
            viewId = null;

            widgetDict = null;

            cacheAssets.Clear();
        }

        protected virtual void OnInternalDestroyed()
        {
        }

        #endregion

        #region 子类生命周期

        /// <summary>
        /// 子类在此完成自身特有创建内容
        /// </summary>
        protected virtual void OnCreating()
        {
        }

        /// <summary>
        /// 绑定组件变量和事件（自动生成）
        /// </summary>
        protected virtual void OnBindCompsAndEvents()
        {
        }

        /// <summary>
        /// 创建完成打开
        /// </summary>
        protected virtual void OnOpen()
        {
        }

        protected virtual void OnClicked(Button button)
        {
        }

        protected virtual void OnValueChanged(Toggle toggle, bool value)
        {
        }

        protected virtual void OnValueChanged(Dropdown dropdown, int value)
        {
        }

        protected virtual void OnValueChanged(InputField inputField, string value)
        {
        }

        protected virtual void OnValueChanged(Slider slider, float value)
        {
        }

        protected virtual void OnValueChanged(Scrollbar scrollbar, float value)
        {
        }

        protected virtual void OnValueChanged(ScrollRect scrollRect, Vector2 value)
        {
        }


        /// <summary>
        /// 子类在此完成自身特有关闭（清理）内容
        /// </summary>
        protected virtual void OnDestroying()
        {
        }

        /// <summary>
        /// 解除组件变量和事件（自动生成）
        /// </summary>
        protected virtual void OnUnbindCompsAndEvents()
        {
        }

        /// <summary>
        /// 关闭完成
        /// </summary>
        protected virtual void OnDestroyed()
        {
        }

        #endregion
    }
}