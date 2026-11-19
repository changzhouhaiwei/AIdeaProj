using UnityEngine;
using TMPro;
using FishFramework;
using DG.Tweening;
using System;
using XSortGameCore;
using UnityEngine.UI;

namespace FishXSortPlay
{
    public class CoinPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_text;
        [SerializeField] private GameObject redObj;
        [SerializeField] private CoinFlyAnim coinFly;
        [SerializeField] private GameObject flyStartNode;
        [SerializeField] private GameObject flyTargetNode;

        [SerializeField] private Button button;
        [SerializeField] private GameObject shopIcon;
        [SerializeField] bool inGame;
        [SerializeField] private bool autoPlayGainAnim = true;

        private int lastCoinValue = -1;
        private Vector3 panelBaseScale = Vector3.one;
        private DG.Tweening.Tween gainScaleTween;

        private const float GainScaleFactor = 1.05f;
        private const float GainScaleUpDuration = 0.1f;
        private const float GainScaleDownDuration = 0.05f;
        private static readonly float[] BatchedCoinDelays = { 0.35f, 0.55f, 0.75f, 0.95f };

        private int GetCurrentCoin()
        {
            if (!GameData.BFromStartScene)
            {
                return 0;
            }

            return GPUserDataManager.Instance.UserData.coin;
        }

        public void SetAutoGainAnim(bool enable)
        {
            autoPlayGainAnim = enable;
        }

        public void UpdateCoins()
        {
            if (m_text == null || m_text.IsNull())
                return;

            m_text.text = GetCurrentCoin().ToString();
        }

        public void SetupPanel()
        {
            panelBaseScale = transform.localScale;
            lastCoinValue = GetCurrentCoin();
            UpdateCoins();
            RefreshShopEntryVisible();
        }


        public void Activate()
        {
            gameObject.SetActive(true);
            redObj.SetActive(GPUserDataManager.Instance.BShopRed);
            RefreshShopEntryVisible();
        }


        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Start()
        {
            if (GameData.BFromStartScene)
            {
                if (coinFly != null)
                    coinFly.Hide();
                GPUserDataManager.Instance.OnDataChanged += CoinChangeFunc;
                AddEvent();
            }
        }

        void AddEvent()
        {
            GameEvent.AddEventListener((int)EventNameDef.ShopFreeCoinPanelAnimPlay, AddFreeCoinAnim);
        }

        void RemoveEvent()
        {
            GameEvent.RemoveEventListener((int)EventNameDef.ShopFreeCoinPanelAnimPlay, AddFreeCoinAnim);
        }

        public void SetShopBtnShow(bool b)
        {
            if (button != null)
                button.interactable = b;

            RefreshShopIconVisibility();
        }

        void RefreshShopEntryVisible()
        {
            SetShopBtnShow(true);
        }

        bool IsShopButtonClickable()
        {
            if (button == null || !button.isActiveAndEnabled || !button.interactable)
                return false;

            var graphic = button.targetGraphic;
            return graphic != null && graphic.raycastTarget;
        }

        void RefreshShopIconVisibility()
        {
            if (shopIcon != null)
                shopIcon.SetActive(IsShopButtonClickable());
        }


        public void OnDestroy()
        {
            gainScaleTween?.Kill();
            gainScaleTween = null;
            coinFly?.Stop();

            RemoveEvent();
            if (GPUserDataManager.HasInit())
                GPUserDataManager.Instance.OnDataChanged -= CoinChangeFunc;
        }

        bool IsPanelAlive()
        {
            return this != null && gameObject != null && !gameObject.IsNull();
        }

        public void CoinChangeFunc(string param1)
        {
            if (!IsPanelAlive() || m_text == null || m_text.IsNull())
                return;

            int currentCoin = GetCurrentCoin();
            bool hasCoinIncreased = currentCoin > lastCoinValue;

            UpdateCoins();
            if (redObj != null && !redObj.IsNull())
                redObj.SetActive(GPUserDataManager.Instance.BShopRed);
            if (autoPlayGainAnim && hasCoinIncreased && gameObject.activeInHierarchy)
            {
                PlayGainAnim();
            }
            lastCoinValue = currentCoin;
        }

        public Transform ShakeTarget => button != null ? button.transform : transform;

        /// <summary>
        /// 到达特效播放时抖一下货币按钮（所有飞金币入口共用）。
        /// </summary>
        public void PlayArriveShake()
        {
            var shakeTf = ShakeTarget;
            if (shakeTf == null) return;

            shakeTf.DOKill();
            shakeTf.localScale = Vector3.one;
            var tButton = shakeTf.GetComponent<TButton>();
            shakeTf.DOScale(1.05f, 0.1f).SetUpdate(true).OnComplete(() =>
            {
                if (shakeTf == null) return;
                shakeTf.DOScale(1f, 0.05f).SetUpdate(true).OnComplete(() =>
                {
                    tButton?.SetRestingScale(Vector3.one);
                });
            });
        }

        private void PlayGainAnim()
        {
            // 只抖按钮，避免带着 CoinFlyAnim 一起缩放
            PlayArriveShake();
        }

        public void PlayCoinGainBatched(int totalCoin, Action<int> onEachBatch, Action onComplete = null, float completeDelay = 0.7f)
        {
            if (totalCoin <= 0)
            {
                onComplete?.Invoke();
                return;
            }

            int[] batches = SplitToFourBatches(totalCoin);
            for (int i = 0; i < BatchedCoinDelays.Length; i++)
            {
                int batchAward = batches[i];
                if (batchAward <= 0)
                {
                    continue;
                }

                Utility.DGDelayedCall(BatchedCoinDelays[i], () =>
                {
                    if (!IsPanelAlive())
                        return;

                    onEachBatch?.Invoke(batchAward);
                    UpdateCoins();
                });
            }

            Utility.DGDelayedCall(BatchedCoinDelays[BatchedCoinDelays.Length - 1] + completeDelay, () =>
            {
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// 金币已入账：把显示回拨 amount，供飞币前展示「领取前」数值。
        /// </summary>
        public void RewindDisplayedCoin(int amount)
        {
            if (!IsPanelAlive() || m_text == null || m_text.IsNull() || amount <= 0)
                return;

            int end = GetCurrentCoin();
            lastCoinValue = Mathf.Max(0, end - amount);
            m_text.text = lastCoinValue.ToString();
        }

        /// <summary>
        /// 金币已入账：分批把显示涨到真实值（不二次 AddCoin）。
        /// </summary>
        public void PlayCoinGainDisplayOnly(int amount, Action onComplete = null, float completeDelay = 0.7f)
        {
            if (amount <= 0 || !IsPanelAlive())
            {
                onComplete?.Invoke();
                return;
            }

            int end = GetCurrentCoin();
            int shown = lastCoinValue >= 0 && lastCoinValue <= end
                ? lastCoinValue
                : Mathf.Max(0, end - amount);
            if (shown > end - amount)
                shown = Mathf.Max(0, end - amount);
            lastCoinValue = shown;
            if (m_text != null && !m_text.IsNull())
                m_text.text = shown.ToString();

            int[] batches = SplitToFourBatches(amount);
            for (int i = 0; i < BatchedCoinDelays.Length; i++)
            {
                int batchAward = batches[i];
                if (batchAward <= 0)
                    continue;

                Utility.DGDelayedCall(BatchedCoinDelays[i], () =>
                {
                    if (!IsPanelAlive())
                        return;

                    shown = Mathf.Min(end, shown + batchAward);
                    lastCoinValue = shown;
                    if (m_text != null && !m_text.IsNull())
                        m_text.text = shown.ToString();
                });
            }

            Utility.DGDelayedCall(BatchedCoinDelays[BatchedCoinDelays.Length - 1] + completeDelay, () =>
            {
                if (IsPanelAlive())
                {
                    UpdateCoins();
                    lastCoinValue = GetCurrentCoin();
                }
                onComplete?.Invoke();
            });
        }

        private int[] SplitToFourBatches(int totalCoin)
        {
            int v1 = (int)(totalCoin * 0.25f);
            int v2 = (int)(totalCoin * 0.25f);
            int v3 = (int)(totalCoin * 0.25f);
            int v4 = totalCoin - v1 - v2 - v3;
            return new[] { v1, v2, v3, v4 };
        }

        public void OpenShop()
        {
            if (redObj != null)
            {
                redObj.SetActive(false);
            }

            UIBottomNav.OpenShopFromNav(inGame);
        }

        /// <summary>
        /// 统一飞金币入口（结算/广告等）。
        /// startFrom：初始散开中心，null 则用屏幕中心。
        /// </summary>
        public void PlayCoinFly(Action onComplete = null, Transform startFrom = null)
        {
            if (gameObject == null || gameObject.IsNull() || !gameObject.activeInHierarchy)
            {
                onComplete?.Invoke();
                return;
            }

            if (coinFly == null)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureCoinFlyTarget();
            coinFly.StartFly(onComplete, null, startFrom);
        }

        public void StopCoinFly()
        {
            coinFly?.Stop();
        }

        public void AddFreeCoinAnim()
        {
            if (gameObject == null || gameObject.IsNull() || !gameObject.activeInHierarchy) return;

            EnsureCoinFlyTarget();
            coinFly?.StartFly();
        }

        public void AddCoinCommonAnim(int coinNum, GameObject coinSp, Action action = null)
        {
            if (gameObject.IsNull() || !gameObject.activeInHierarchy) return;

            coinSp.transform.DOMove(flyStartNode.transform.position, 0.5f);
            coinSp.transform.DOScale(0.4f, 0.5f);
            coinSp.GetComponent<Image>().DOFade(0, 0.5f);

            Utility.DGDelayedCall(0.5f, () =>
            {
                coinSp.SetActive(false);
            }
            );

            Utility.DGDelayedCall(0.1f, () =>
            {
                EnsureCoinFlyTarget();
                coinFly?.StartFly(() =>
                {
                    action?.Invoke();
                });

                PlayCoinGainBatched(coinNum, (batch) =>
                {
                    GPUserDataManager.Instance.AddCoin(batch);
                });
            });
        }

        void EnsureCoinFlyTarget()
        {
            if (coinFly == null) return;
            if (flyTargetNode != null)
                coinFly.SetTarget(flyTargetNode.transform);
            coinFly.OnArrivePlay = PlayArriveShake;
        }
    }
}
