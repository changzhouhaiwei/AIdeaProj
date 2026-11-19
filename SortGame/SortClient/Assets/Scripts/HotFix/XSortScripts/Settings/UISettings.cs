using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XSortGameCore;
using TMPro;
using FishFramework;

namespace FishXSortPlay
{
    public class UISettings : UIPage, IPopupWindow
    {
        [BoxGroup("References", "References")]
        [SerializeField] Image backgroundImage;
        [BoxGroup("References", "References")]
        [SerializeField] RectTransform panelRectTransform;
        [BoxGroup("References", "References")]
        [SerializeField] RectTransform contentRectTransform;
        public RectTransform ContentRectTransform => contentRectTransform;

        [SerializeField]
        private TextMeshProUGUI abTestLb;

        [SerializeField]
        private TextMeshProUGUI uidTestLb;

        [SerializeField]
        private TextMeshProUGUI deviceLb;

        [SerializeField]
        private TextMeshProUGUI screenReLb;

        [BoxGroup("Buttons", "Buttons")]
        [SerializeField] Button closeButton;

        [BoxGroup("Buttons", "Buttons")]
        [SerializeField] Button gmButton;
        [SerializeField] Button cmpButton;
        [SerializeField] Button restoreButton;
        [SerializeField] Button customerButton;
        [SerializeField] Button languageButton;
        [SerializeField] Button rateusButton;
        [SerializeField] CanvasGroup languageCanvasGroup;
        [BoxGroup("AutoComplete", "AutoComplete")]
        [SerializeField] Button autoCompleteButton;
        TToggle autoCompleteToggle;
        bool pendingAutoCompleteEnabled = true;
        private int m_gmClickCnt = 0;

        /// <summary>从评价弹窗关闭后再次打开设置时为 true，用于先藏面板、下一帧再显示并刷新布局。</summary>
        static bool s_openingFromRateDialogReopen;

        public bool IsOpened => isPageDisplayed;

        public static void MarkOpeningFromRateDialogReopen()
        {
            s_openingFromRateDialogReopen = true;
        }

        public static void RefreshRateUsButtonVisibilityIfExists()
        {
            UIController.GetPage<UISettings>()?.RefreshRateUsButtonVisibility();
        }

        public override void Init()
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            gmButton.onClick.AddListener(OnGMButtonClicked);
            languageButton.onClick.AddListener(OnLanguageButtonClicked);
            rateusButton.onClick.AddListener(OnRateUsButtonClicked);
            if (autoCompleteButton != null)
            {
                autoCompleteButton.onClick.AddListener(OnAutoCompleteButtonClicked);
                autoCompleteToggle = autoCompleteButton.GetComponentInChildren<TToggle>(true);
                if (autoCompleteToggle != null)
                    autoCompleteToggle.interactable = false;
            }
            backgroundImage.AddEvent(EventTriggerType.PointerDown, OnBackgroundClicked);
            ApplyLanguageButtonOrangeFont();
        }

        public override void PlayShowAnimation()
        {
            BringAboveGame();
            gameObject.SetActive(true);
            ApplyShowData();

            // 先更新按钮显隐（评价返回时 ShowPage 前也可能已调用一次）
            RefreshRateUsButtonVisibility();
            RefreshAutoCompleteButtonVisibility();

            if (s_openingFromRateDialogReopen)
            {
                SetPageVisible(false);
                StartCoroutine(CoFinishRateDialogReopen());
                return;
            }

            SetPageVisible(true);

            // 触发打开动效（Animator / OnEnable 等依赖此开关）
            gameObject.SetActive(false);
            gameObject.SetActive(true);

            UIController.OnPageOpened(this);
            UIController.OnPopupWindowOpened(this);

            GetComponent<UILanguageDlg>()?.BuildLanguageListOncePerSession();

            StartCoroutine(CoDeferredLayoutRefresh());
        }

        void ApplyShowData()
        {
            uidTestLb.text = GPUserDataManager.Instance.GetUID();

            ShowScreenParams();
            deviceLb.text = SystemInfo.deviceModel;

            cmpButton.SetActiveEx(GPSDKMgr.Instance.GetBCMPShow());
            restoreButton?.SetActiveEx(ShouldShowRestoreButton());
#if UNITY_IOS
            customerButton?.SetActiveEx(false);
#else
            customerButton?.SetActiveEx(true);
#endif
            RefreshAutoCompleteButtonVisibility();
            RefreshAutoCompleteToggle();
            ApplyLanguageButtonOrangeFont();
        }

        void ApplyLanguageButtonOrangeFont()
        {
            if (languageButton == null)
                return;

            var tmp = languageButton.GetComponentInChildren<TextMeshProUGUI>(true);
            // 预制体已挂橙色材质，本地化会把 Mikado orange 映射成 outline_shop；运行时强制回 orange。
            PlayBtnDifficultyFont.Apply(tmp, 0, isCoinLevel: true);
        }

        public void RefreshAutoCompleteButtonVisibility()
        {
            if (autoCompleteButton == null)
                return;

            // 自动完成是三消清盘，绳子玩法不带这行。
            autoCompleteButton.gameObject.SetActive(false);
        }

        static bool ShouldShowRestoreButton()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return GameData.Shop.GetCfgBuyPayID(GameData.Pay.GetNoAdsPayId()) != null;
#else
            return false;
#endif
        }

        public void RefreshRateUsButtonVisibility()
        {
            if (rateusButton == null)
                return;
#if UNITY_IOS
            rateusButton.SetActiveEx(false);
#else
            rateusButton.SetActiveEx(PlayerPrefs.GetInt(UIRatesDlg.RateUsStateKey, 0) != UIRatesDlg.RateUsStateCompleted);
#endif
        }

        void BringAboveGame()
        {
            // 绳子页在设置页后面，后画的会盖住设置。打开时拉到主画布之上。
            transform.SetAsLastSibling();
            if (canvas == null)
                return;

            Canvas root = canvas.rootCanvas;
            int order = root != null ? root.sortingOrder + 20 : 520;
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
        }

        void SetPageVisible(bool visible)
        {
            if (panelRectTransform != null)
                panelRectTransform.gameObject.SetActive(visible);
        }

        IEnumerator CoFinishRateDialogReopen()
        {
            yield return null;
            if (!isPageDisplayed)
                yield break;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
            RecalculatePanelSize();

            SetPageVisible(true);
            s_openingFromRateDialogReopen = false;

            // 评价返回后再播一次打开动效
            gameObject.SetActive(false);
            gameObject.SetActive(true);

            UIController.OnPageOpened(this);
            UIController.OnPopupWindowOpened(this);

            GetComponent<UILanguageDlg>()?.BuildLanguageListOncePerSession();
        }

        IEnumerator CoDeferredLayoutRefresh()
        {
            yield return null;
            if (!isPageDisplayed)
                yield break;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
            RecalculatePanelSize();
        }

        void ShowScreenParams()
        {
            if (GameData.BFromStartScene && FishFramework.GameModule.Setting.GmMode)
            {
                uint numerator = Screen.currentResolution.refreshRateRatio.numerator;
                uint denominator = Screen.currentResolution.refreshRateRatio.denominator;
                string text = "interAD: " + InterADABTestConfig.GetTestGroupForAnalytics() + "\n";
                text += "numerator: " + numerator + "\n";
                text += "denominator: " + denominator + "\n";
                text += "framerate: " + Application.targetFrameRate + "\n";
                abTestLb.text = text;
                GameModule.Setting.SetFrameRate();

                float scaleW = ScalableBufferManager.widthScaleFactor;
                float scaleH = ScalableBufferManager.heightScaleFactor;

                int width = Mathf.RoundToInt(Screen.width * scaleW);
                int height = Mathf.RoundToInt(Screen.height * scaleH);

                screenReLb.text = width + "x" + height;
            }
        }

        public override void PlayHideAnimation()
        {
            GetComponent<UILanguageDlg>()?.SaveScrollPositionBeforeSettingsHide();

            isPageDisplayed = false;
            s_openingFromRateDialogReopen = false;
            gameObject.SetActive(false);
            UIController.OnPageClosed(this);
            UIController.OnPopupWindowClosed(this);
        }

        private void RecalculatePanelSize()
        {
            float height = Mathf.Abs(contentRectTransform.sizeDelta.y);

            int childCount = contentRectTransform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = contentRectTransform.GetChild(i);
                if (childTransform != null)
                {
                    SettingsElementsCluster settingsElementsGroup = childTransform.GetComponent<SettingsElementsCluster>();
                    if (settingsElementsGroup != null)
                    {
                        if (settingsElementsGroup.IsGroupActive())
                        {
                            height += ((RectTransform)childTransform).sizeDelta.y;
                        }
                    }
                    else if (childTransform.gameObject.activeSelf)
                    {
                        height += ((RectTransform)childTransform).sizeDelta.y;
                    }
                }
            }

            panelRectTransform.sizeDelta = new Vector2(panelRectTransform.sizeDelta.x, height);
        }

        private void OnLanguageButtonClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            GetComponent<UILanguageDlg>()?.OpenLanguagePanel();
        }

        public void OnCloseButtonClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            UIController.HidePage<UISettings>();
        }

        public void OnGMButtonClicked()
        {
            m_gmClickCnt++;
            int maxCnt = 10;

            if (GameModule.Setting.GmMode)
            {
                maxCnt = 2;
            }

            if (m_gmClickCnt > maxCnt && GameData.BFromStartScene)
            {
                m_gmClickCnt = 0;
                UIController.HidePage<UISettings>();
                if (GameDirector.levelController == null)
                    RopeSort.RopeGmPanel.Open();
                else
                    FishFramework.GameModule.UI.OpenPanel<GMLevelChooseUI>();
            }
        }

        private void OnBackgroundClicked(PointerEventData data)
        {
            UIController.HidePage<UISettings>();
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }

        public void OnClickPrivacyBtn()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSoundSpread);
            Application.OpenURL(GameData.PrivacyUrl);
        }

        public void OnClickServiceBtn()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSoundSpread);
            Application.OpenURL(GameData.ServiceUrl);
        }

        public void OnClickCustomerBtn()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            GPSDKMgr.Instance.OpenServiceCenter();
        }

        void OnRateUsButtonClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            UIController.HidePage<UISettings>();
            GameModule.UI.OpenPanel<UIRatesDlg, bool>(true);
        }

        void RefreshAutoCompleteToggle()
        {
            if (!AutoCompleteSettings.IsFeatureUnlocked())
                return;

            pendingAutoCompleteEnabled = AutoCompleteSettings.IsEnabled;
            if (autoCompleteToggle == null)
                return;

            autoCompleteToggle.SetIsOnWithoutNotify(pendingAutoCompleteEnabled);
            autoCompleteToggle.OnToggleClick();
        }

        void OnAutoCompleteButtonClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSoundSpread);
            pendingAutoCompleteEnabled = !pendingAutoCompleteEnabled;
            if (autoCompleteToggle == null)
                return;

            autoCompleteToggle.SetIsOnWithoutNotify(pendingAutoCompleteEnabled);
            autoCompleteToggle.OnToggleClick();
        }

        public void OnClickCmpBtn()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            GPSDKMgr.Instance.ShowUMP();
        }

        public void OnRestoreClickBtn()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            GameData.Pay.CheckRestorePay();
        }

        protected override bool OnEscButtonPressed()
        {
            if (GetComponent<UILanguageDlg>()?.TryCloseOnBack() == true)
                return true;

            UIController.HidePage<UISettings>();
            return true;
        }
    }
}
