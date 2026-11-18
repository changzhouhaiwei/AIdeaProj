using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FishFramework
{
    [DisallowMultipleComponent]
    public class UIModule : MonoBehaviour
    {
        public Canvas UICanvas { private set; get; }
        public Camera UICamera { private set; get; }
        public GameObject UIRoot { private set; get; }
        public GameObject UICanvasRoot { private set; get; }
        public RectTransform UILayerRoot { private set; get; }

        private const string UIRootName = "UIRoot";
        public const string UILayerRootName = "UILayerRoot";
        private const string UIRootResPath = "Assets/CABRes/Prefabs/Main/UIRoot.prefab";
        private const int DesignScreenWidth = 720;
        private const int DesignScreenHeight = 1560;
        public float DesignScreenWidth_F = 720f;
        public float DesignScreenHeight_F = 1560f;
        private const int LayerDistance = 1000;

        //loading中的字样
        private GameObject loadingObj;

        private GameObject layerBlock; //内部屏蔽对象 显示时之下的所有UI将不可操作
        private GameObject blackBorder;

        private RectTransform panelTransform;

        /// <summary>
        /// UI层级字典
        /// </summary>
        private readonly Dictionary<PanelLayer, UILayerRoot> layerParentMap = new();

        private readonly Dictionary<string, UIPanel> allPanelLayerMap = new();
        private readonly List<UIPanel> panelLayerList = new();

        private const uint MaxValue = uint.MaxValue - 1;
        private uint nameIndex = 0;

        private uint NameIndex
        {
            get
            {
                if (nameIndex >= MaxValue)
                {
                    nameIndex = 0;
                }

                uint currentIndex = nameIndex;
                nameIndex++;

                return currentIndex;
            }
        }

        public bool InitRoot()
        {
            #region UICanvasRoot 查找各种组件

            UIRoot = GameObject.Find(UIRootName);
            if (UIRoot == null)
            {
                UIRoot = Instantiate(UILoadHelper.LoadAsset<GameObject>(UIRootResPath));
            }

            if (UIRoot == null)
            {
                Debug.LogError($"初始化错误 没有找到UIRoot");
                return false;
            }

            UIRoot.name = UIRoot.name.Replace("(Clone)", "");
            DontDestroyOnLoad(UIRoot);

            UICanvas = UIRoot.GetComponentInChildren<Canvas>();
            if (UICanvas == null)
            {
                Debug.LogError($"初始化错误 没有找到Canvas");
                return false;
            }

            UICanvasRoot = UICanvas.gameObject;
            UILayerRoot = UICanvasRoot.transform.Find(UILayerRootName)?.GetComponent<RectTransform>();
            if (UILayerRoot == null)
            {
                Debug.LogError($"初始化错误 没有找到UILayerRoot");
                return false;
            }

            UICamera = UICanvasRoot.GetComponentInChildren<Camera>();
            if (UICamera == null)
            {
                Debug.LogError($"初始化错误 没有找到UICamera");
                return false;
            }

            Canvas canvas = UICanvasRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError($"初始化错误 没有找到UICanvasRoot - Canvas");
                return false;
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = UICamera;

            CanvasScaler canvasScaler = UICanvasRoot.GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                Debug.LogError($"初始化错误 没有找到UICanvasRoot - CanvasScaler");
                return false;
            }

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(DesignScreenWidth, DesignScreenHeight);

            #endregion

            // 分层
            layerParentMap.Clear();
            const int len = (int)PanelLayer.Count;
            int startOrder = 0;
            const int additiveOrder = 999;
            for (int i = len - 1; i >= 0; i--)
            {
                name = $"Layer{i}-{(PanelLayer)i}";

                var layer = UILayerRoot.transform.Find(name).gameObject;

                // var layer = new GameObject($"Layer{i}-{(PanelLayer)i}")
                // {
                //     layer = LayerMask.NameToLayer("UI")
                // };

                RectTransform rect = layer.GetComponent<RectTransform>(); //<RectTransform>();
                rect.SetParent(UILayerRoot);
                rect.localScale = Vector3.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchorMax = Vector2.one;
                rect.anchorMin = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
                rect.localRotation = Quaternion.identity;

                canvas = layer.GetComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = startOrder;
                canvas.sortingLayerName = "UI";
                layer.GetComponent<GraphicRaycaster>();

                int endOrder = startOrder + additiveOrder;
                CreateRoot((PanelLayer)i, rect, startOrder, endOrder);
                startOrder = endOrder + 1;

                if(i == 6)
                {
                    panelTransform = rect;
                }
            }

            InitAddUIBlock(); //所有层级初始化后添加一个终极屏蔽层 可根据API 定时屏蔽UI操作
            // InitBlackBorder();

            UICamera.transform.localPosition = new Vector3(UILayerRoot.localPosition.x, UILayerRoot.localPosition.y, -LayerDistance);
            UICamera.clearFlags = CameraClearFlags.Depth;
            UICamera.orthographic = true;

            return true;
        }

        private void CreateRoot(PanelLayer PanelLayer, RectTransform rect, int startOrder, int endOrder)
        {
            Debug.Assert(!layerParentMap.ContainsKey(PanelLayer)); //uiRoot已存在
            Debug.Assert(startOrder >= 0); //必须使startOrder >= 0
            Debug.Assert(endOrder >= startOrder); //必须使endOrder >= startOrder

            layerParentMap.Add(PanelLayer, new UILayerRoot
            {
                layerRect = rect,
                startOrder = startOrder,
                endOrder = endOrder
            });
        }

        //初始化添加屏蔽模块
        private void InitAddUIBlock()
        {
            layerBlock = new GameObject("LayerBlock");
            var rect = layerBlock.AddComponent<RectTransform>();
            layerBlock.AddComponent<CanvasRenderer>();
            layerBlock.AddComponent<UIBlock>();
            rect.SetParent(layerParentMap[PanelLayer.Block].layerRect);
            rect.SetAsLastSibling();
            rect.ResetToFullScreen();
            SetLayerBlockVisible(false);
        }

        //初始化添加黑边模块
        private void InitBlackBorder()
        {
            blackBorder = new GameObject("BlackBorder")
            {
                layer = LayerMask.NameToLayer("UI")
            };
            var rect = blackBorder.AddComponent<RectTransform>();
            blackBorder.AddComponent<BlackBorder>();
            rect.SetParent(layerParentMap[PanelLayer.BlackBorder].layerRect);
            rect.SetAsLastSibling();
            rect.ResetToFullScreen();
        }

        /// <summary>
        /// 谨慎使用
        /// </summary>
        /// <param name="value"></param>
        private void SetLayerBlockVisible(bool value)
        {
            layerBlock.SetActive(value);
        }

        public RectTransform GetLayerRect(PanelLayer panelLayer)
        {
            return layerParentMap[panelLayer].layerRect;
        }

        public T OpenPanel<T>() where T : UIPanel, new()
        {
            T panel = OpenPanel<T>(PanelLayer.Panel, PanelOpenType.Single);
            panel.Open();
            return panel;
        }

        public T OpenPanel<T, P1>(P1 p1) where T : UIPanel, IUIOpen<P1>, new()
        {
            T panel = OpenPanel<T>(PanelLayer.Panel, PanelOpenType.Single);
            panel.Open(p1);
            return panel;
        }

        public T OpenPanel<T, P1, P2>(P1 p1, P2 p2) where T : UIPanel, IUIOpen<P1, P2>, new()
        {
            T panel = OpenPanel<T>(PanelLayer.Panel, PanelOpenType.Single);
            panel.Open(p1, p2);
            return panel;
        }

        public T OpenPanel<T, P1, P2, P3>(P1 p1, P2 p2, P3 p3) where T : UIPanel, IUIOpen<P1, P2, P3>, new()
        {
            T panel = OpenPanel<T>(PanelLayer.Panel, PanelOpenType.Single);
            panel.Open(p1, p2, p3);
            return panel;
        }

        public T OpenPanel<T, P1, P2, P3, P4>(P1 p1, P2 p2, P3 p3, P4 p4) where T : UIPanel, IUIOpen<P1, P2, P3, P4>, new()
        {
            T panel = OpenPanel<T>(PanelLayer.Panel, PanelOpenType.Single);
            panel.Open(p1, p2, p3, p4);
            return panel;
        }

        public T OpenPanel<T, P1, P2, P3, P4, P5>(P1 p1, P2 p2, P3 p3, P4 p4, P5 p5) where T : UIPanel, IUIOpen<P1, P2, P3, P4, P5>, new()
        {
            T panel = OpenPanel<T>(PanelLayer.Panel, PanelOpenType.Single);
            panel.Open(p1, p2, p3, p4, p5);
            return panel;
        }

        public T OpenPanel<T>(PanelLayer panelLayer, PanelOpenType panelOpenType) where T : UIPanel, new()
        {

            string panelName = typeof(T).Name;
            if (panelOpenType == PanelOpenType.Single)
            {
                if (allPanelLayerMap.TryGetValue(panelName, out UIPanel view))
                {
                    return view as T;
                }
            }
            else
            {
                panelName += NameIndex.ToString();
            }

            T panel = Activator.CreateInstance<T>();
            panel.Layer = panelLayer;
            panel.Create(panelName, layerParentMap[panelLayer].layerRect);
            int sortingOrder = GetIncrementedSortingOrder(panelLayer);
            panel.SetSortingOrder(sortingOrder);
            allPanelLayerMap.Add(panelName, panel);
            OpenPanelBefore(panel);
            SetBackgroundAndFocus();


            return panel;
        }

        public T OpenPanel<T, P1>(PanelLayer panelLayer, PanelOpenType panelOpenType, P1 p1) where T : UIPanel, IUIOpen<P1>, new()
        {
            T panel = OpenPanel<T>(panelLayer, panelOpenType);
            panel.Open(p1);
            return panel;
        }

        public void ClosePanel(string panelId, Action onFinish = null)
        {
            Debug.Assert(allPanelLayerMap.ContainsKey(panelId));

            UIPanel panel = allPanelLayerMap[panelId];
            allPanelLayerMap.Remove(panelId);
            RemoveUIElse(panel);
            panel.Close(onFinish);
            SetBackgroundAndFocus();
        }

        public void ClosePanel<T>(Action onFinish = null) where T : UIPanel
        {
            ClosePanel(typeof(T).Name, onFinish);
        }

        public void DestroyPanel(string panelId)
        {
            Debug.Assert(allPanelLayerMap.ContainsKey(panelId));

            UIPanel panel = allPanelLayerMap[panelId];
            allPanelLayerMap.Remove(panelId);
            RemoveUIElse(panel);
            panel.Destroy();
            SetBackgroundAndFocus();
        }

        public void DestroyPanel<T>() where T : UIPanel
        {
            DestroyPanel(typeof(T).Name);
        }

        public void DestroyAllPanel()
        {
            foreach (UIPanel panel in allPanelLayerMap.Values)
            {
                panel.Destroy();
            }

            allPanelLayerMap.Clear();
            panelLayerList.Clear();
        }

        public void SetPanelVisible(string panelId, bool visible)
        {
            Debug.Assert(allPanelLayerMap.ContainsKey(panelId));
            UIPanel panel = allPanelLayerMap[panelId];
            panel.SetVisible(visible);
            SetBackgroundAndFocus();
        }

        public UIPanel GetPanel(string panelId)
        {
            return allPanelLayerMap[panelId];
        }

        public T GetPanel<T>(string panelId) where T : UIPanel
        {
            return allPanelLayerMap[panelId] as T;
        }

        public T GetPanel<T>() where T : UIPanel
        {
            return GetPanel(typeof(T).Name) as T;
        }

        public bool ExistPanel(string panelId)
        {
            return allPanelLayerMap.ContainsKey(panelId);
        }

        public bool ExistPanel<T>()
        {
            return ExistPanel(typeof(T).Name);
        }

        private void OpenPanelBefore(UIPanel panel)
        {
            if (panel is not { Layer: PanelLayer.Panel })
            {
                return;
            }

            if (panel.panelBehaviour.PanelType != UIPanelType.Popup)
            {
                for (int i = panelLayerList.Count - 1; i >= 0; i--)
                {
                    UIPanel uiPanel = panelLayerList[i];
                    if (uiPanel == panel)
                    {
                        continue;
                    }

                    uiPanel.SetVisible(false);
                }
            }

            panelLayerList.Add(panel);
        }

        private void RemoveUIElse(UIPanel panel)
        {
            if (panel is not { Layer: PanelLayer.Panel })
            {
                return;
            }

            for (int i = panelLayerList.Count - 1; i >= 0; i--)
            {
                UIPanel uiPanel = panelLayerList[i];
                if (uiPanel == panel)
                {
                    continue;
                }

                if (uiPanel.panelBehaviour.PanelType == UIPanelType.Popup)
                {
                    continue;
                }

                uiPanel.SetVisible(true);
                break;
            }

            panelLayerList.RemoveAt(panelLayerList.Count - 1);
        }

        public int GetIncrementedSortingOrder(PanelLayer panelLayer)
        {
            if (panelLayer == PanelLayer.Panel)
            {
                if (panelLayerList.Count > 0)
                {
                    return panelLayerList[^1].canvas.sortingOrder + panelLayerList[^1].panelBehaviour.thickness + 1;
                }

                return layerParentMap[panelLayer].startOrder;
            }

            UIPanel topestPanel = null;
            foreach (var panel in allPanelLayerMap.Values)
            {
                if (panel.Layer != panelLayer) continue;
                if (topestPanel == null || panel.canvas.sortingOrder > topestPanel.canvas.sortingOrder)
                {
                    topestPanel = panel;
                }
            }

            return topestPanel != null ? topestPanel.canvas.sortingOrder + topestPanel.panelBehaviour.thickness + 1 : layerParentMap[panelLayer].startOrder;
        }

        public void BringPanelToFront(UIPanel panel)
        {
            if (panel == null || !ExistPanel(panel.panelId))
            {
                return;
            }

            if (panel.Layer == PanelLayer.Panel && panelLayerList.Contains(panel))
            {
                panelLayerList.Remove(panel);
            }

            panel.SetSortingOrder(GetIncrementedSortingOrder(panel.Layer));

            if (panel.Layer == PanelLayer.Panel)
            {
                panelLayerList.Add(panel);
            }

            panel.GetOwnerRectTransform().SetAsLastSibling();
            SetBackgroundAndFocus();
        }

        private void SetBackgroundAndFocus()
        {
            UIPanel needBgPanel = null;

            List<UIPanel> panels = new List<UIPanel>();
            foreach (var panel in allPanelLayerMap.Values)
            {
                if (panel.Layer == PanelLayer.Tips)
                {
                    panels.Add(panel);
                }
            }

            panels.Sort((a, b) => b.canvas.sortingOrder - a.canvas.sortingOrder);

            for (int i = panelLayerList.Count - 1; i >= 0; i--)
            {
                panels.Add(panelLayerList[i]);
            }

            for (int i = 0; i < panels.Count; i++)
            {
                UIPanel panel = panels[i];
                if (!panel.panelBehaviour.hasBg) continue;
                needBgPanel = panel;
                break;
            }

            //设置/移除背景
            if (needBgPanel != null)
            {
                needBgPanel.SetBackground();
            }
            else
            {
                UIBlocker.Instance.Unbind();
            }
        }

        //loading
        public void SetLoadingObj(GameObject obj)
        {
            loadingObj = obj;
            SetBLoading(false);
        }

        public void SetBLoading(bool b)
        {
            loadingObj?.SetActive(b);
        }


        public void SetPoolObectToPanel(RectTransform tra)
        {
            tra.SetParent(panelTransform);
            tra.InitLocalTransform();
            tra.anchoredPosition = new Vector2(-3500,0);
        }

        /// <summary>
        /// 获取最上层的UI面板
        /// 根据sortingOrder找到最上层的可见面板
        /// </summary>
        /// <returns>最上层的面板，如果没有则返回null</returns>
        public UIPanel GetTopPanel()
        {
            UIPanel topPanel = null;
            int highestOrder = int.MinValue;

            // 遍历所有面板，找到sortingOrder最高的可见面板
            foreach (var panel in allPanelLayerMap.Values)
            {
                if (!panel.IsEligibleForEscRouting())
                {
                    continue;
                }

                // 找到sortingOrder最高的面板
                if (panel.canvas.sortingOrder > highestOrder)
                {
                    highestOrder = panel.canvas.sortingOrder;
                    topPanel = panel;
                }
            }

            return topPanel;
        }

        /// <summary>
        /// 处理最上层UIPanel的Esc按键
        /// </summary>
        /// <returns>返回true表示已处理，false表示未处理</returns>
        public bool HandleTopPanelEsc()
        {
            List<UIPanel> eligiblePanels = null;
            foreach (var panel in allPanelLayerMap.Values)
            {
                if (!panel.IsEligibleForEscRouting())
                {
                    continue;
                }

                eligiblePanels ??= new List<UIPanel>();
                eligiblePanels.Add(panel);
            }

            if (eligiblePanels == null || eligiblePanels.Count == 0)
            {
                return false;
            }

            eligiblePanels.Sort((a, b) => b.canvas.sortingOrder.CompareTo(a.canvas.sortingOrder));
            for (int i = 0; i < eligiblePanels.Count; i++)
            {
                if (eligiblePanels[i].HandleEscButton())
                {
                    return true;
                }
            }

            return false;
        }


    }
}