using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TileGameCore;
using FishFramework;
using Unity.VisualScripting;

namespace FishTilePlay
{
    /// <summary>
    /// 道具UI控制器 - 管理所有道具UI的显示、隐藏和交互
    /// </summary>
    public class PUUIDirector : MonoBehaviour
    {
        [SerializeField] Transform containerTransform; // 道具UI容器
        [SerializeField] GameObject itemPrefab; // 道具UI预制体
        [SerializeField] RectTransform boxCoverBoardBottomBoundary;

        [Space]
        [SerializeField] RectTransform floatingTextRectTransform; // 浮动文本的RectTransform
        [SerializeField] TextMeshProUGUI floatingText; // 浮动文本组件
        // [SerializeField] float floatingTextDelay = 1.0f; // 浮动文本显示延迟
        [SerializeField] Ease floatingTextEasing = Ease.OutQuart; // 浮动文本缓动类型

        [Space]
        // [SerializeField] PUUIPurchasePanel powerUpPurchasePanel; // 道具购买面板
        // public PUUIPurchasePanel PowerUpPurchasePanel => powerUpPurchasePanel;

        private PUDirector powerUpController; // 道具控制器引用

        private PUUIActor[] uiBehaviors; // 所有道具UI行为数组
        public PUUIActor[] UIBehaviors => uiBehaviors;

        private float defaultFloatingTextWidth; // 浮动文本默认宽度
        private Vector2 defaultFloatingTextPosition; // 浮动文本默认位置
        private TweenClip floatingTextTweenCase; // 浮动文本动画案例
        private RectTransform rectTransform; // 当前对象的RectTransform
        public RectTransform RectTransform => rectTransform;

        /// <summary>
        /// 底部技能按钮图标世界坐标 Y 上沿（只用按钮本身，不含 Container 上边距）。
        /// </summary>
        public bool TryGetButtonsTopWorldY(Camera worldCamera, out float topWorldY)
        {
            topWorldY = 0f;
            Canvas.ForceUpdateCanvases();
            RectTransform coordinateRoot = rectTransform != null
                ? rectTransform
                : transform as RectTransform;

            float maxY = float.MinValue;
            bool any = false;

            if (uiBehaviors != null)
            {
                for (int i = 0; i < uiBehaviors.Length; i++)
                {
                    if (uiBehaviors[i] == null)
                    {
                        continue;
                    }

                    // 用按钮根节点（固定 175 布局框）测顶边：背景图会被 Animator 持续缩放，
                    // 每次进关测出的值都不同，会造成适配漂移。根节点不参与动画，值恒定。
                    // 不过滤 activeInHierarchy：进关瞬间按钮可能尚未激活，布局位置依然有效。
                    var rt = uiBehaviors[i].transform as RectTransform;
                    if (rt == null)
                    {
                        continue;
                    }

                    if (!TryGetRectTopWorldY(rt, coordinateRoot, worldCamera, out float y))
                    {
                        continue;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }

                    any = true;
                }
            }

            // 行为数组未就绪时，尝试 Container 下已存在的按钮节点（含未激活）
            if (!any && containerTransform != null)
            {
                for (int i = 0; i < containerTransform.childCount; i++)
                {
                    var child = containerTransform.GetChild(i) as RectTransform;
                    if (child == null || child.GetComponent<PUUIActor>() == null)
                    {
                        continue;
                    }

                    if (!TryGetRectTopWorldY(child, coordinateRoot, worldCamera, out float y))
                    {
                        continue;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }

                    any = true;
                }
            }

            if (!any)
            {
                return false;
            }

            topWorldY = maxY;
            return true;
        }

        public float GetRequiredBoxCoverBoardBottomWorldY(Camera worldCamera)
        {
            if (boxCoverBoardBottomBoundary == null)
                throw new MissingReferenceException(
                    "PUUIDirector.boxCoverBoardBottomBoundary must be wired on the Power Up Panel prefab.");
            if (worldCamera == null)
                throw new MissingReferenceException(
                    "Gameplay camera is required for the BoxCover board bottom boundary.");

            Canvas.ForceUpdateCanvases();
            RectTransform coordinateRoot = rectTransform != null
                ? rectTransform
                : transform as RectTransform;
            if (!TryGetRectLayoutTopWorldY(
                    boxCoverBoardBottomBoundary,
                    coordinateRoot,
                    worldCamera,
                    out float worldY))
            {
                throw new InvalidOperationException(
                    "PUUIDirector.boxCoverBoardBottomBoundary could not be converted to gameplay world space.");
            }

            return worldY;
        }

        static bool TryGetRectLayoutTopWorldY(
            RectTransform rt,
            RectTransform coordinateRoot,
            Camera worldCamera,
            out float topWorldY)
        {
            topWorldY = 0f;
            if (rt == null
                || rt.parent == null
                || coordinateRoot == null
                || coordinateRoot.rect.height <= 0.001f
                || worldCamera == null)
            {
                return false;
            }

            // 只读布局框，不读按钮动画缩放。
            Vector3 topInParent = rt.localPosition + new Vector3(0f, rt.rect.yMax, 0f);
            Vector3 topWorld = rt.parent.TransformPoint(topInParent);
            float topInRootY = coordinateRoot.InverseTransformPoint(topWorld).y;
            float viewportY = Mathf.InverseLerp(
                coordinateRoot.rect.yMin,
                coordinateRoot.rect.yMax,
                topInRootY);
            if (viewportY < 0f || viewportY > 1f)
                return false;

            float depth = Mathf.Abs(worldCamera.transform.position.z);
            if (depth < 0.01f)
                depth = 10f;
            topWorldY = worldCamera.ViewportToWorldPoint(
                new Vector3(0.5f, viewportY, depth)).y;
            return true;
        }

        /// <summary>
        /// UICamera(ortho≈5) 与 MainCamera(ortho≈7.8) 世界坐标不一致。
        /// 统一：UI 角点 → 屏幕像素 → 玩法主相机世界 Y。
        /// </summary>
        private static bool TryGetRectTopWorldY(
            RectTransform rt,
            RectTransform coordinateRoot,
            Camera worldCamera,
            out float topWorldY)
        {
            topWorldY = 0f;
            if (rt == null || worldCamera == null)
            {
                return false;
            }

            // Power Up Panel 本身是全屏 Stretch 节点。按钮和它处于同一预制体层级，
            // 以它为坐标根可避开外层 SafeArea、嵌套 Canvas 及 UICamera 的变换。
            if (coordinateRoot != null && coordinateRoot.rect.height > 0.001f && rt.parent != null)
            {
                // 按“静止布局”计算：Activate() 会把按钮根节点 localScale 从 0 弹到 1，
                // 含缩放的世界 bounds 在进关帧会缩成一个点，导致每次进关适配结果漂移。
                // 这里只用布局数据（localPosition + rect，视缩放恒为 1），与动画时机无关。
                Vector3 topInParent = rt.localPosition + new Vector3(0f, rt.rect.yMax, 0f);
                Vector3 topWorld = rt.parent.TransformPoint(topInParent);
                float topInRootY = coordinateRoot.InverseTransformPoint(topWorld).y;
                float viewportY = Mathf.InverseLerp(
                    coordinateRoot.rect.yMin,
                    coordinateRoot.rect.yMax,
                    topInRootY);
                if (viewportY >= 0f && viewportY <= 1f)
                {
                    float viewportDepth = Mathf.Abs(worldCamera.transform.position.z);
                    if (viewportDepth < 0.01f)
                        viewportDepth = 10f;

                    topWorldY = worldCamera.ViewportToWorldPoint(
                        new Vector3(0.5f, viewportY, viewportDepth)).y;
                    return true;
                }
            }

            // 兜底：没有框架根节点时才按相机做常规转换。
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Camera uiCam = GameModule.UI != null ? GameModule.UI.UICamera : null;
            if (uiCam == null)
            {
                Canvas canvas = rt.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    uiCam = canvas.worldCamera;
                }
            }

            float maxScreenY = float.MinValue;
            bool any = false;
            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCam, corners[i]);
                if (screen.y > maxScreenY)
                    maxScreenY = screen.y;
                any = true;
            }

            if (!any || maxScreenY <= float.MinValue + 1f)
            {
                return false;
            }

            float zDist = Mathf.Abs(worldCamera.transform.position.z);
            if (zDist < 0.01f)
                zDist = 10f;

            // 用屏幕水平中线 + 按钮最高屏幕 Y，投到玩法相机世界坐标
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, maxScreenY, zDist));
            topWorldY = world.y;
            return true;
        }

        /// <summary>
        /// 初始化道具UI控制器
        /// </summary>
        /// <param name="powerUpController">道具控制器</param>
        public void Init(PUDirector powerUpController)
        {
            this.powerUpController = powerUpController;

            rectTransform = (RectTransform)transform;

            // 保存浮动文本的默认位置和宽度
            defaultFloatingTextPosition = floatingTextRectTransform.anchoredPosition;
            defaultFloatingTextWidth = floatingTextRectTransform.sizeDelta.x;

            // 创建道具UI元素
            PUActor[] activePowerUps = PUDirector.ActivePowerUps; // 获取所有活跃道具
            uiBehaviors = new PUUIActor[activePowerUps.Length]; // 初始化UI行为数组

            // 为每个道具创建对应的UI
            for (int i = 0; i < activePowerUps.Length; i++)
            {
                GameObject itemObject = containerTransform.Find("PU Behavior UI" + (i + 1)).gameObject;

                // 获取并初始化UI行为组件
                uiBehaviors[i] = itemObject.GetComponent<PUUIActor>();
                uiBehaviors[i].Init(activePowerUps[i]);
            }


        }

        /// <summary>
        /// 每帧更新 - 检查并重新绘制需要更新的道具UI
        /// </summary>
        private void Update()
        {
            // 遍历所有道具UI，检查是否需要重新绘制
            foreach (var uiBehavior in uiBehaviors)
            {
                if (uiBehavior.Behavior.IsDirty) // 如果道具标记为需要重新绘制
                {
                    uiBehavior.Redraw(); // 重新绘制UI
                }
            }

        }

        public void RefreshStates()
        {
            foreach (var uiBehavior in uiBehaviors)
            {
                uiBehavior.RefreshBtnState(); // 重新绘制UI
            }
        }

        /// <summary>
        /// 关卡开始时调用 - 根据关卡等级激活/禁用道具
        /// </summary>
        /// <param name="levelIndex">关卡索引</param>
        public void OnLevelStarted(int levelIndex, bool bFromSave = false)
        {
            for (int i = 0; i < uiBehaviors.Length; i++)
            {
                if (uiBehaviors[i].Settings.Type == PUType.ExtraSlot)
                {
                    uiBehaviors[i].Disable();
                    continue;
                }

                // 检查道具是否在当前关卡可用
                if (uiBehaviors[i].Settings.RequiredLevel <= levelIndex)
                {
                    uiBehaviors[i].Activate(); // 激活道具UI
                }
                else
                {
                    uiBehaviors[i].Disable(); // 禁用道具UI
                }
            }

            if(!GameData.BFromStartScene)//编辑器处理
            {
                return;
            }
        }


        /// <summary>
        /// 道具使用后调用 - 更新UI并显示浮动消息
        /// </summary>
        /// <param name="powerUpBehavior">使用的道具行为</param>
        public void OnPowerUpUsed(PUActor powerUpBehavior)
        {
            // 重新绘制所有道具面板
            RedrawPanels();
            // 刷新可释放状态（如 HighLight 本局已生效需置灰）
            RefreshStates();

            // 获取并显示浮动消息
            string floatingMessageText = powerUpBehavior.GetFloatingMessage();
            if (!string.IsNullOrEmpty(floatingMessageText))
                SpawnFloatingText(floatingMessageText);
        }

        /// <summary>
        /// 生成浮动文本 - 显示道具使用消息
        /// </summary>
        /// <param name="text">要显示的文本</param>
        public void SpawnFloatingText(string text)
        {
            // 停止之前的浮动文本动画
            floatingTextTweenCase.KillActive();

            // 显示浮动文本对象
            floatingTextRectTransform.gameObject.SetActive(true);

            // 设置文本内容
            floatingText.text = text;
            floatingText.ForceMeshUpdate(); // 强制更新网格

            // 计算文本的推荐高度
            float prefferedHeight = LayoutUtility.GetPreferredHeight(floatingText.rectTransform);

            // 设置浮动文本的大小和位置
            floatingTextRectTransform.sizeDelta = new Vector2(defaultFloatingTextWidth, prefferedHeight);
            floatingTextRectTransform.anchoredPosition = defaultFloatingTextPosition;

            // 注释掉的动画代码 - 原本应该有向上移动的动画
            // floatingTextTweenCase = floatingTextRectTransform.DOAnchoredPosition(new Vector2(defaultFloatingTextPosition.x, defaultFloatingTextPosition.y + 50), floatingTextDelay).SetEasing(floatingTextEasing).OnComplete(() =>
            // {
            floatingTextRectTransform.gameObject.SetActive(false); // 隐藏浮动文本
            // });
        }

        /// <summary>
        /// 重新绘制所有道具面板
        /// </summary>
        public void RedrawPanels()
        {
            for (int i = 0; i < uiBehaviors.Length; i++)
            {
                uiBehaviors[i].Redraw(); // 重新绘制每个道具UI
            }
        }

        /// <summary>
        /// 隐藏所有道具面板
        /// </summary>
        public void HidePanels()
        {
            foreach (var uiBehavior in uiBehaviors)
            {
                uiBehavior.gameObject.SetActive(false); // 隐藏所有道具UI
            }
        }

        /// <summary>
        /// 隐藏指定类型的道具面板
        /// </summary>
        /// <param name="puType">道具类型</param>
        public void HidePanel(PUType puType)
        {
            foreach (var uiBehavior in uiBehaviors)
            {
                if (uiBehavior.Settings.Type == puType) // 找到指定类型的道具
                {
                    uiBehavior.gameObject.SetActive(false); // 隐藏该道具UI
                    break; // 找到后退出循环
                }
            }
        }

        /// <summary>
        /// 显示所有激活的道具面板
        /// </summary>
        public void ShowPanels()
        {
            foreach (var uiBehavior in uiBehaviors)
            {
                if (uiBehavior.IsActive) // 只显示激活的道具
                {
                    uiBehavior.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 显示指定类型的道具面板
        /// </summary>
        /// <param name="puType">道具类型</param>
        public void ShowPanel(PUType puType)
        {
            foreach (var uiBehavior in uiBehaviors)
            {
                if (uiBehavior.IsActive && uiBehavior.Settings.Type == puType) // 找到指定类型且激活的道具
                {
                    uiBehavior.gameObject.SetActive(true); // 显示该道具UI
                    break; // 找到后退出循环
                }
            }
        }

        /// <summary>
        /// 获取指定类型的道具UI行为
        /// </summary>
        /// <param name="puType">道具类型</param>
        /// <returns>道具UI行为，如果不存在则返回null</returns>
        public PUUIActor GetPanel(PUType puType)
        {
            foreach (var uiBehavior in uiBehaviors)
            {
                if (uiBehavior.Settings.Type == puType) // 找到指定类型的道具
                {
                    return uiBehavior; // 返回该道具UI行为
                }
            }

            return null; // 未找到指定类型的道具
        }

        private void Start()
        {
            GPUserDataManager.Instance.OnDataChanged += RefreshItems;
            GameEvent.AddEventListener((int)EventNameDef.SubmitOver, CbSubmitOver);
            GameEvent.AddEventListener((int)EventNameDef.LoadSaveLevelAnimFinish, CbShuffleOver);
        }

        private void RefreshItems(string info)
        {
            RedrawPanels();
        }

        private void OnDestroy()
        {
            GPUserDataManager.Instance.OnDataChanged -= RefreshItems;
            GameEvent.RemoveEventListener((int)EventNameDef.SubmitOver, CbSubmitOver);
            GameEvent.RemoveEventListener((int)EventNameDef.LoadSaveLevelAnimFinish, CbShuffleOver);
        }


        private void CbSubmitOver()
        {
            if (LevelDirector.LevelGenService == null)
            {
                return;
            }

            RefreshStates();
        }

        private void CbShuffleOver()
        {
            RefreshStates();
        }


        public void HideAllOperEffect()
        {
            foreach (var uiBehavior in uiBehaviors)
            {
                uiBehavior.ShowOrHideEffect(false);
            }
        }


        //玩家无操作超过15S时，提示使用道具，优先级：洗牌》魔法棒》撤回。先提示有存量的。美术效果同竞品
        public bool CheckShowEffect()
        {
            PUUIActor shuffle = null;
            PUUIActor hint = null;
            PUUIActor undo = null;
            foreach (var uiBehavior in uiBehaviors)
            {
                if (uiBehavior.Settings.Type == PUType.Shuffle)
                {
                    shuffle = uiBehavior;
                }
                else if (uiBehavior.Settings.Type == PUType.Hint)
                {
                    hint = uiBehavior;
                }
                else if (uiBehavior.Settings.Type == PUType.Undo)
                {
                    undo = uiBehavior;
                }

                uiBehavior.ShowOrHideEffect(false);
            }

            if (shuffle != null && shuffle.GetAmount() > 0)
            {
                shuffle.ShowOrHideEffect(true);
                return true;
            }

            if (hint != null && hint.GetAmount() > 0)
            {
                hint.ShowOrHideEffect(true);
                return true;
            }

            if (undo != null && undo.GetAmount() > 0)
            {
                // 有可撤销的最后拾取棋子时才提示
                if (DockActor.HasUndoablePickedObject())
                {
                    undo.ShowOrHideEffect(true);
                }
                return true;
            }

            return false;
        }
    }


}
