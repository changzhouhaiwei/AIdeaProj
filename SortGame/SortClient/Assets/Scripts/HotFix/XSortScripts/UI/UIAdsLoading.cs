using UnityEngine;
using System;
using FishFramework;
using XSortGameCore;

namespace FishXSortPlay
{
    public class UIAdsLoading : UIPage
    {
        [Header("UI Components")]
        [SerializeField] RectTransform safeAreaTransform;

        public override void Init()
        {
            RegisterEvent();
        }

        public override void PlayShowAnimation()
        {

        }

        public override void PlayHideAnimation()
        {
            UIController.OnPageClosed(this);
        }

        public static void Show(Action onComplete = null, Action onCancel = null)
        {
            UIController.ShowPage<UIAdsLoading>();
        }

        public static void Hide()
        {
            UIController.HidePage<UIAdsLoading>();
        }

        public static bool IsShowing()
        {
            return UIController.IsDisplayed<UIAdsLoading>();
        }

        public void SetCancelButtonVisible(bool show)
        {

        }

        void OnDestroy()
        {
            RemoveEvent();
        }

        void RegisterEvent()
        {
            if (GPSDKMgr.Instance != null && GPSDKMgr.Instance.GetCallbackHandler() != null)
            {
                GPSDKMgr.Instance.GetCallbackHandler().OnHideAdsLoading += OnHideAdsLoading;
                GPSDKMgr.Instance.GetCallbackHandler().OnShowGiftBagDlg += OnShowGiftBagDlg;
            }
        }

        void RemoveEvent()
        {
            if (GPSDKMgr.Instance != null && GPSDKMgr.Instance.GetCallbackHandler() != null)
            {
                GPSDKMgr.Instance.GetCallbackHandler().OnHideAdsLoading -= OnHideAdsLoading;
                GPSDKMgr.Instance.GetCallbackHandler().OnShowGiftBagDlg -= OnShowGiftBagDlg;
            }
        }

        private void OnHideAdsLoading()
        {
            UIController.HidePage<UIAdsLoading>();
        }

        private void OnShowGiftBagDlg()
        {
        }
    }
}
