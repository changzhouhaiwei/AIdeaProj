using System.Collections.Generic;
using UnityEngine;
using TileGameCore;
using System;
using DG.Tweening;

namespace FishTilePlay
{
    /// <summary>
    /// 道具控制器 - 管理所有道具的初始化、使用、购买等核心逻辑
    /// </summary>
    public class PUDirector : MonoBehaviour
    {
        private static PUDirector instance; // 单例实例

        [DrawReference]
        [SerializeField] PUDatabase database; // 道具数据库

        [LineSpacer("Sounds")]
        [SerializeField] AudioClip activateSound; // 道具激活音效

        private static PUActor[] activePowerUps; // 所有活跃的道具行为数组
        public static PUActor[] ActivePowerUps => activePowerUps;

        private static Dictionary<PUType, PUActor> powerUpsLink; // 道具类型到行为组件的映射字典

        private static PUUIDirector powerUpsUIController; // 道具UI控制器
        public static PUUIDirector PowerUpsUIController => powerUpsUIController;

        private Transform behaviorsContainer; // 道具行为组件的容器

        /// <summary>
        /// 道具使用事件回调
        /// </summary>
        public static event OnPowerUpUsedCallback OnPowerUpUsed;

        /// <summary>
        /// 初始化道具系统
        /// </summary>
        public void Init()
        {
#if MODULE_POWERUPS
            instance = this;

            // 创建道具行为组件的容器
            behaviorsContainer = new GameObject("[POWER UPS]").transform;
            behaviorsContainer.gameObject.isStatic = true;

            PUConfig[] powerUpSettings = database.PowerUps; // 获取所有道具设置
            activePowerUps = new PUActor[powerUpSettings.Length]; // 初始化道具行为数组
            powerUpsLink = new Dictionary<PUType, PUActor>(); // 初始化映射字典


            // 遍历所有道具设置，创建对应的行为组件
            for (int i = 0; i < activePowerUps.Length; i++)
            {
                // 初始化道具保存数据
                powerUpSettings[i].InitialiseSave();
                powerUpSettings[i].Init();

                // 实例化道具行为对象
                GameObject powerUpBehaviorObject = Instantiate(powerUpSettings[i].BehaviorPrefab, behaviorsContainer);
                powerUpBehaviorObject.transform.ResetLocal();

                // 获取并初始化道具行为组件
                PUActor powerUpBehavior = powerUpBehaviorObject.GetComponent<PUActor>();
                powerUpBehavior.InitialiseSettings(powerUpSettings[i]);
                powerUpBehavior.Init();

                activePowerUps[i] = powerUpBehavior;

                // 将道具添加到映射字典中
                powerUpsLink.Add(activePowerUps[i].Settings.Type, activePowerUps[i]);
            }

            // 获取游戏UI并初始化道具UI控制器
            UIGame gameUI = UIController.GetPage<UIGame>();
            powerUpsUIController = gameUI.PowerUpsUIController;
            powerUpsUIController.Init(this);
            
#else
            Debug.LogWarning("[PU Controller]: Module Define isn't active!");
#endif
        }

        /// <summary>
        /// 购买道具
        /// </summary>
        /// <param name="powerUpType">道具类型</param>
        /// <returns>购买是否成功</returns>
        public static bool PurchasePowerUp(PUType powerUpType)
        {
            if (powerUpsLink.ContainsKey(powerUpType))
            {
                PUActor powerUpBehavior = powerUpsLink[powerUpType];
                // 检查是否有足够的货币
                if (powerUpBehavior.Settings.HasEnoughCurrency())
                {
                    // 扣除货币
                    CurrencyController.Substract(powerUpBehavior.Settings.CurrencyType, powerUpBehavior.Settings.Price);

                    // 增加道具数量
                    powerUpBehavior.Settings.Save.Amount += powerUpBehavior.Settings.PurchaseAmount;

                    // 重新绘制道具UI
                    powerUpsUIController.RedrawPanels();

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
            }

            return false;
        }

        /// <summary>
        /// 添加道具数量
        /// </summary>
        /// <param name="powerUpType">道具类型</param>
        /// <param name="amount">添加的数量</param>
        public static void AddPowerUp(PUType powerUpType, int amount)
        {
            if (powerUpsLink.ContainsKey(powerUpType))
            {
                PUActor powerUpBehavior = powerUpsLink[powerUpType];

                // 增加道具数量
                powerUpBehavior.Settings.Save.Amount += amount;

                // 重新绘制道具UI
                powerUpsUIController.RedrawPanels();
            }
            else
            {
                Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
            }
        }

        /// <summary>
        /// 设置道具数量
        /// </summary>
        /// <param name="powerUpType">道具类型</param>
        /// <param name="amount">设置的数量</param>
        public static void SetPowerUpAmount(PUType powerUpType, int amount)
        {
            if (powerUpsLink.ContainsKey(powerUpType))
            {
                PUActor powerUpBehavior = powerUpsLink[powerUpType];

                // 设置道具数量
                powerUpBehavior.Settings.Save.Amount = amount;

                // 重新绘制道具UI
                powerUpsUIController.RedrawPanels();
            }
            else
            {
                Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
            }
        }

        /// <summary>
        /// 使用道具 - 这是道具使用的核心方法
        /// </summary>
        /// <param name="powerUpType">要使用的道具类型</param>
        /// <returns>使用是否成功</returns>
        public static bool UsePowerUp(PUType powerUpType, Action beforeUseFunc = null)
        {
            if (powerUpType == PUType.ExtraSlot)
                return false;

            if (LevelDirector.IsBusy)
                return false;

            // 检查道具类型是否已注册
            if (powerUpsLink.ContainsKey(powerUpType))
            {
                PUActor powerUpBehavior = powerUpsLink[powerUpType];

                // 检查道具是否不在忙碌状态
                if (!powerUpBehavior.IsBusy)
                {
                    // 尝试激活道具
                    if (powerUpBehavior.Activate(beforeUseFunc))
                    {
                        // 激活成功：按道具类型播放音效（无配置时退回 activateSound）
                        AudioClip useClip = null;
                        var ac = AudioController.AudioClips;
                        if (ac != null)
                        {
                            switch (powerUpType)
                            {
                                case PUType.Hint:
                                    useClip = ac.tool_magic;
                                    break;
                                case PUType.Shuffle:
                                    useClip = ac.tool_random;
                                    break;
                                case PUType.Undo:
                                    useClip = ac.tool_return;
                                    break;
                                case PUType.HighLight:
                                    useClip = ac.tool_highlight;
                                    break;
                            }
                        }
                        if (useClip == null)
                            useClip = instance.activateSound;
                        if (useClip != null)
                            AudioController.PlaySound(useClip, 0.45f);

                        // 减少道具数量
                        powerUpBehavior.Settings.Save.Amount--;

                        // 通知UI控制器道具已使用
                        powerUpsUIController.OnPowerUpUsed(powerUpBehavior);

                        // 触发道具使用事件
                        OnPowerUpUsed?.Invoke(powerUpType);

                        return true;
                    }
                    // 如果激活失败（比如条件不满足），返回false
                }
                // 如果道具正在忙碌中，返回false
            }
            else
            {
                Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
            }

            return false;
        }

        /// <summary>
        /// 重置指定道具的数量为0
        /// </summary>
        /// <param name="powerUpType">道具类型</param>
        public static void ResetPowerUp(PUType powerUpType)
        {
            if (powerUpsLink.ContainsKey(powerUpType))
            {
                PUActor powerUpBehavior = powerUpsLink[powerUpType];

                // 重置道具数量为0
                powerUpBehavior.Settings.Save.Amount = 0;

                // 重新绘制道具UI
                powerUpsUIController.RedrawPanels();
            }
            else
            {
                Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));
            }
        }

        /// <summary>
        /// 重置所有道具的数量为0
        /// </summary>
        public static void ResetPowerUps()
        {
            // 遍历所有道具，重置数量为0
            foreach (PUActor powerUp in activePowerUps)
            {
                powerUp.Settings.Save.Amount = 0;
            }

            // 重新绘制道具UI
            powerUpsUIController.RedrawPanels();
        }

        /// <summary>
        /// 获取指定类型的道具行为组件
        /// </summary>
        /// <param name="powerUpType">道具类型</param>
        /// <returns>道具行为组件，如果不存在则返回null</returns>
        public static PUActor GetPowerUpBehavior(PUType powerUpType)
        {
            if (powerUpsLink.ContainsKey(powerUpType))
            {
                return powerUpsLink[powerUpType];
            }

            Debug.LogWarning(string.Format("[Power Ups]: Power up with type {0} isn't registered.", powerUpType));

            return null;
        }

        /// <summary>
        /// 与打乱道具（PU Shuffle Settings）相同的棋盘棋子缩放 tween 参数，供复活打乱等复用。
        /// </summary>
        public static void GetShuffleBoardScaleTweenParams(out float scaleTime, out float minDelay, out float maxDelay, out Ease scaleEasing)
        {
#if MODULE_POWERUPS
            if (powerUpsLink != null && powerUpsLink.TryGetValue(PUType.Shuffle, out PUActor behavior) && behavior != null && behavior.Settings is PUShuffleSettings shuffleSettings)
            {
                scaleTime = shuffleSettings.ScaleTime;
                minDelay = shuffleSettings.ScaleMinDelay;
                maxDelay = shuffleSettings.ScaleMaxDelay;
                scaleEasing = shuffleSettings.ScaleEasingType;
                return;
            }
#endif
            scaleTime = 0.5f;
            minDelay = 0.05f;
            maxDelay = 0.4f;
            scaleEasing = Ease.OutBack;
        }

        /// <summary>
        /// 重置所有道具的行为状态
        /// </summary>
        public static void ResetBehaviors()
        {
            // 遍历所有道具，重置其行为状态
            for (int i = 0; i < activePowerUps.Length; i++)
            {
                activePowerUps[i].ResetBehavior();
            }
        }

        /// <summary>
        /// 道具使用事件回调委托
        /// </summary>
        /// <param name="powerUpType">使用的道具类型</param>
        public delegate void OnPowerUpUsedCallback(PUType powerUpType);



    }
}

