using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using XSortGameCore;
using FishFramework;

namespace FishXSortPlay
{
    /// <summary>
    /// 道具UI行为组件 - 负责道具按钮的显示、交互和状态管理
    /// </summary>
    /// 
    public class PUUIActor : MonoBehaviour
    {
        private enum enBtnState
        {
            Default = 0,
            NotOpen,
            NotEnough,
            CantRelease,
        }

        [Group("Refs")]
        [SerializeField] Image backgroundImage; // 背景图片

        [Group("Refs")]
        [SerializeField] Image iconImage; // 道具图标

        [Group("Refs")]
        [SerializeField] GameObject amountContainerObject; // 数量显示容器

        [Group("Refs")]
        [SerializeField] TextMeshProUGUI amountText; // 数量文本

        [Group("Refs")]
        [SerializeField] GameObject amountPurchaseObject; // 购买提示对象

        //广告购买还是金币购买
        [SerializeField] GameObject adPurchaseNode;
        [SerializeField] GameObject coinPurchaseNode;
        [SerializeField] TextMeshProUGUI coinLb;

        bool buyOrWatchAd = true;
        int priceCoin = 160;

        //上锁状态
        [SerializeField] GameObject m_lockSp;
        [SerializeField] GameObject m_grayBg;

        // [Space]
        // [SerializeField] SimpleBounce bounce; // 弹跳效果组件

        protected PUActor behavior; // 道具行为组件
        public PUActor Behavior => behavior;

        protected PUConfig settings; // 道具设置
        public PUConfig Settings => settings;

        private Button button; // UI按钮组件

        private bool isActive = false; // 道具是否激活
        public bool IsActive => isActive;
        private enBtnState m_btnState;
        private string m_alertStr;

        [SerializeField]
        private GameObject effectNode;

        [SerializeField]
        private Animator animator;

        private Vector3 initBgScale;
        private Quaternion initIconRot;
        private Vector3 initIconScal;

        /// <summary>
        /// 初始化时设置按钮点击事件
        /// </summary>
        private void Awake()
        {
            button = GetComponent<Button>();
            // 绑定按钮点击事件到OnButtonClicked方法
            button.onClick.AddListener(() => OnButtonClicked());

            initBgScale = Vector2.one * 1.15f; // backgroundImage.transform.localScale;
            initIconScal = Vector2.one; //iconImage.transform.localScale;
            initIconRot = Quaternion.Euler(0f, 0f, 0f);// iconImage.transform.rotation;
        }

        /// <summary>
        /// 初始化道具UI行为
        /// </summary>
        /// <param name="powerUpBehavior">道具行为组件</param>
        public void Init(PUActor powerUpBehavior)
        {
            behavior = powerUpBehavior;
            settings = powerUpBehavior.Settings;

            behavior.bindBehaviour = this;

            InitPrice();
            ApplyVisuals(); // 应用视觉效果
            Redraw(); // 重新绘制UI

            // bounce.Init(transform); // 初始化弹跳效果
            gameObject.SetActive(false); // 初始状态设为隐藏
            isActive = false;

            //显示
            SetBtnState(enBtnState.Default);
            ShowOrHideEffect(false);


        }

        void InitPrice()
        {
            switch (settings.Type)
            {
                case PUType.Hint:
                    // GPUserDataManager.Instance.AddHintItem(-1);
                    priceCoin = 200;
                    break;
                case PUType.Shuffle:
                    priceCoin = 150;
                    // GPUserDataManager.Instance.AddShuffleItem(-1);
                    break;
                case PUType.Undo:
                    priceCoin = 100;
                    // GPUserDataManager.Instance.AddUndoItem(-1);
                    break;
                case PUType.HighLight:
                    priceCoin = 125;
                    break;
            }
        }

        private void SetBtnState(enBtnState state)
        {
            m_btnState = state;
            bool bGray = state == enBtnState.CantRelease || state == enBtnState.NotEnough || state == enBtnState.NotOpen || behavior.IsBusy;
            m_grayBg.SetActive(bGray);
            m_lockSp.SetActive(state == enBtnState.NotOpen);
            amountContainerObject.SetActive((state != enBtnState.NotOpen) && GetAmount() > 0);
            amountPurchaseObject.SetActive(state != enBtnState.NotOpen && GetAmount() <= 0);

            coinLb.text = priceCoin.ToString();

            if (GameData.BFromStartScene)
            {
                int goldCoin = GPUserDataManager.Instance.GetCoin();
                buyOrWatchAd = goldCoin >= priceCoin;
            }
            else
            {
                buyOrWatchAd = false;
            }

            adPurchaseNode.SetActive(!buyOrWatchAd);
            coinPurchaseNode.SetActive(buyOrWatchAd);

        }

        public void UpdateBtnStateShow()
        {
            SetBtnState(m_btnState);
        }

        /// <summary>
        /// 应用道具的视觉效果（图标、背景色等）
        /// </summary>
        protected virtual void ApplyVisuals()
        {
            iconImage.sprite = settings.Icon; // 设置道具图标
            iconImage.color = Color.white; // 设置图标颜色为白色
            backgroundImage.color = settings.BackgroundColor; // 设置背景颜色
        }

        /// <summary>
        /// 激活道具UI（显示道具按钮）
        /// </summary>
        public void Activate()
        {
            isActive = true;

            gameObject.SetActive(true); // 显示道具按钮

            // 播放激活动画：从0缩放到1。完成后同步 TButton 静止缩放，避免 Start/按下时采到 0 导致按反馈缩没。
            transform.DOKill(false);
            transform.localScale = Vector3.zero;
            var appearTarget = Vector3.one;
            transform.DOScale(appearTarget, 0.3f)
                .SetEase(DG.Tweening.Ease.OutBack)
                .OnComplete(() =>
                {
                    var tButton = button as TButton;
                    tButton?.SetRestingScale(appearTarget);
                });
            RefreshBtnState(true);

            Redraw(); // 重新绘制UI
            ReportAdImpress();
            ShowOrHideEffect(false);

        }

        /// <summary>
        /// 禁用道具UI（隐藏道具按钮）
        /// </summary>
        public void Disable()
        {
            isActive = false;

            gameObject.SetActive(false);
        }


        bool m_toolConsumedForEvent;

        private void BeforeUsePowerUpFunc(int useId)
        {
            m_toolConsumedForEvent = false;
            if (GameData.BFromStartScene)
            {
                ChangeTrackedTool(useId, -1);
                m_toolConsumedForEvent = true;
            }

            //先记录增加的tooluse。扣数量后再上报，tool_left 才是用完后的剩余。
            GameBAData.AddToolUseCnt(useId);
            GameBAData.ExcuteStageEvent(2, useId);
        }

        static void ChangeTrackedTool(int useId, int delta)
        {
            switch (useId)
            {
                case 1:
                    GPUserDataManager.Instance.AddHintItem(delta);
                    break;
                case 2:
                    GPUserDataManager.Instance.AddShuffleItem(delta);
                    break;
                case 3:
                    GPUserDataManager.Instance.AddUndoItem(delta);
                    break;
                case 5:
                    GPUserDataManager.Instance.AddHighLightItem(delta);
                    break;
            }
        }

        //播放广告获得道具
        private void CheckNeedAddByAds(int itemId)
        {
            GameBAData.ExcuteAdClick(GetAdKey());
            if (!AdsPlayStrategy.CheckRewardAdsPlay(GetAdKey()))
            {
                return;
            }

#if UNITY_EDITOR

#else
            // 广告加载成功，开始播放广告
            UIController.ShowPage<UIAdsLoading>();
            GPSDKMgr.Instance.PlayRewardedAd(GetAdKey(), 2);
            GPUserDataManager.Instance.SetAdAddItemId(itemId);
#endif

        }

        /// <summary>
        /// 按钮点击事件处理 - 这是道具使用的入口点
        /// </summary>
        public void OnButtonClicked()
        {
            ShowOrHideEffect(false);
            //无法释放
            if (m_btnState == enBtnState.NotOpen)
            {
                if (GameData.BFromStartScene)
                {
                    AlertDlg.ShowDlg(I2LocalizeMgr.GetString("ItemUnlock1"));
                }
                return;
            }
            else if (m_btnState == enBtnState.CantRelease)
            {
                if (GameData.BFromStartScene && !string.IsNullOrEmpty(m_alertStr))
                {
                    AlertDlg.ShowDlg(m_alertStr);
                }
                return;
            }

            //记录使用道具ID
            int useId = GetUseId();

            // 检查道具数量是否足够
            if (GetAmount() > 0)
            {
                // 检查道具是否不在忙碌状态
                if (!behavior.IsBusy)
                {
                    // 尝试使用道具
                    m_toolConsumedForEvent = false;
                    if (PUDirector.UsePowerUp(settings.Type, () => { BeforeUsePowerUpFunc(useId); }))
                    {
                        // 播放弹跳动画效果
                        // bounce.Bounce();

                        GameDirector.RecordLastPlayTime();
                        Redraw();
                        ReportAdImpress();
                    }
                    else if (m_toolConsumedForEvent)
                    {
                        ChangeTrackedTool(useId, 1);
                    }
                    // 如果使用失败（比如条件不满足），不会有任何反馈
                }
                // 如果道具正在忙碌中，也不会有任何反馈
            }
            else
            {
                var ac = AudioController.AudioClips;
                var addOrBtn = ac != null && ac.tool_add != null ? ac.tool_add : ac?.buttonSound;
                if (addOrBtn != null)
                    AudioController.PlaySound(addOrBtn);
                if (GameData.BFromStartScene && !behavior.IsBusy)
                {
                    if (buyOrWatchAd)
                    {
                        if (!behavior.GetCanRelease())
                            return;

                        bool useAfterBuy = false;
                        switch (settings.Type)
                        {
                            case PUType.Hint:
                                GPUserDataManager.Instance.BuyHintItemDirectly(priceCoin);
                                useAfterBuy = true;
                                break;
                            case PUType.Shuffle:
                                GPUserDataManager.Instance.BuyShuffleItemDirectly(priceCoin);
                                useAfterBuy = true;
                                break;
                            case PUType.Undo:
                                GPUserDataManager.Instance.BuyUndoItemDirectly(priceCoin);
                                useAfterBuy = true;
                                break;
                            case PUType.HighLight:
                                GPUserDataManager.Instance.BuyHighLightItemDirectly(priceCoin);
                                useAfterBuy = true;
                                break;
                            default:
                                break;
                        }

                        GameBAData.ExcuteStageEvent(7, useId);//金币购买道具
                        if (useAfterBuy)
                            OnButtonClicked();
                    }
                    else
                    {
                        CheckNeedAddByAds(useId);
                    }
                }
            }
        }

        public int GetAmount()
        {
            if (GameData.BFromStartScene)
            {
                switch (settings.Type)
                {
                    case PUType.Hint:
                        return GPUserDataManager.Instance.GetHintItemCount();
                    case PUType.Shuffle:
                        return GPUserDataManager.Instance.GetShuffleItemCount();
                    case PUType.Undo:
                        return GPUserDataManager.Instance.GetUndoItemCount();
                    case PUType.HighLight:
                        return GPUserDataManager.Instance.GetHighLightItemCount();
                    default:
                        Debug.LogWarning("Invalid PUType: " + settings.Type);
                        break;
                }
            }
            else
            {
                return settings.Save.Amount;
            }
            return 0;
        }

        private void ReportAdImpress()
        {
            if (GetAmount() == 0 && GameData.BFromStartScene)
            {
                GameBAData.ExcuteAdImpress(GetAdKey());// 广告显示的埋点                
            }
        }

        private int GetUseId()
        {
            return (int)settings.Type + 1;
        }

        private string GetAdKey()
        {
            return "BuyItem" + GetUseId();
        }

        /// <summary>
        /// 重新绘制道具UI（更新数量显示、计时器等）
        /// </summary>
        public void Redraw()
        {
            int amount = GetAmount(); // 获取当前道具数量
            // amountContainerObject.SetActive(true);
            // amountPurchaseObject.SetActive(amount == 0);
            amountText.text = amount.ToString(); // 显示数量

            SetBtnState(m_btnState);//设置按钮状态
            // 标记道具已重新绘制
            behavior.OnRedrawn();
        }

        public void RefreshBtnState(bool bInit = false)//刷新状态
        {
            SetBtnState(enBtnState.Default);

            //先判定解锁状态
            int unlockLevel = -1;
            var ty = settings.Type;//设定解锁关卡
            if (ty == PUType.Undo)
            {
                unlockLevel = 1;
            }
            else if (ty == PUType.Shuffle)
            {
                unlockLevel = 3;
            }
            else if (ty == PUType.Hint)
            {
                unlockLevel = 2;
            }
            else if (ty == PUType.HighLight)
            {
                unlockLevel = 4; // 第 5 关（LoadedLevelIndex 0-based）
            }

            int levelIndex = LevelDirector.LoadedLevelIndex;
            if (unlockLevel > levelIndex)
            {
                SetBtnState(enBtnState.NotOpen);
                return;
            }

            if (ty == PUType.Undo)
            {
                if (LevelDirector.Dock.IsEmpty || !DockActor.HasUndoablePickedObject())
                {
                    m_alertStr = I2LocalizeMgr.GetString("UndoItem1");
                    SetBtnState(enBtnState.CantRelease);
                }
            }

            // 本局已激活 HighLight：与 Undo 一样仅走 CantRelease（grayBg），不额外改图标色
            if (ty == PUType.HighLight && PUHighLightBehavior.IsActive)
            {
                m_alertStr = null;
                SetBtnState(enBtnState.CantRelease);
            }

            if (!bInit)
            {
                // Undo.GetCanRelease 恒 true，CantRelease 时图标仍为白；
                // HighLight 已激活时 GetCanRelease 为 false，需排除以免多一层图标置灰
                bool iconGray = !behavior.GetCanRelease()
                    && !(ty == PUType.HighLight && PUHighLightBehavior.IsActive);
                SetIconBGray(iconGray);
            }
            else
            {
                SetIconBGray(false);
            }
        }

        public void ShowOrHideEffect(bool b)
        {
            effectNode.SetActive(b);
            animator.enabled = b;

            if (b == false)
            {
                backgroundImage.transform.localScale = initBgScale;
                iconImage.transform.localScale = initIconScal;
                iconImage.transform.rotation = initIconRot;
            }
        }


        private void Start()
        {
            GameEvent.AddEventListener<int>((int)EventNameDef.PUUIAdGetItem, CbAddItemByAd);
        }

        private void CbAddItemByAd(int useId)
        {
            if (useId == GetUseId())
            {
                OnButtonClicked();
            }
        }

        private void OnDestroy()
        {
            GameEvent.RemoveEventListener<int>((int)EventNameDef.PUUIAdGetItem, CbAddItemByAd);
        }

        public void SetIconBGray(bool b)
        {
            Color col = b ? new Color(167f / 255f, 167f / 255f, 167f / 255f, 1f) : Color.white;
            iconImage.color = col;
        }
    }
}
