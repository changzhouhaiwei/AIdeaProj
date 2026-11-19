using System;
using System.Collections;
using System.Collections.Generic;
using I2.Loc;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;
using XSortGameCore;

public class UILanguageDlg : MonoBehaviour
{
    [Serializable]
    private struct LanguageItemBinding
    {
        public string i2LanguageName;
        public TToggle toggle;
    }

    [SerializeField] CanvasGroup canvasGroupDlg;
    [SerializeField] private Transform languageContentTransform;
    [SerializeField] private ScrollRect languageScrollRect;
    [SerializeField] private TButton closeButton;
    [SerializeField] private TButton confirmButton;
    [SerializeField] private LanguageItemBinding[] languageItems;

    private Coroutine openPanelCoroutine;

    static bool s_languageListBuilt;
    static bool s_hasScrolledToTopOnFirstLanguageOpen;

    static float s_savedVerticalNormalizedPosition = 1f;
    static float s_savedContentAnchoredPosY;
    static bool s_hasSavedScrollPosition;

    static readonly Dictionary<string, TToggle> s_languageCodeToToggle = new();
    static bool s_isApplyingSelection;
    static bool s_languagePanelWasVisible;
    static string s_pendingLanguage;

    private void Awake()
    {
        if (languageScrollRect == null && languageContentTransform != null)
            languageScrollRect = languageContentTransform.GetComponentInParent<ScrollRect>();

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
    }

    public void BuildLanguageListOncePerSession()
    {
        BuildLanguageListInternal();
    }

    public void SaveScrollPositionBeforeSettingsHide()
    {
        if (IsLanguagePanelVisible())
            SaveScrollPosition();
    }

    /// <summary>打开语言面板：先隐藏 → 恢复滚动 → 再显示，避免滑动残影。</summary>
    public void OpenLanguagePanel()
    {
        SetLanguagePanelVisible(false);

        if (openPanelCoroutine != null)
            StopCoroutine(openPanelCoroutine);
        openPanelCoroutine = StartCoroutine(CoOpenLanguagePanel());
    }

    IEnumerator CoOpenLanguagePanel()
    {
        BuildLanguageListInternal();

        SyncLanguageSelection();

        yield return null;
        ApplyScrollPositionWhileHidden();

        yield return new WaitForEndOfFrame();
        ApplyScrollPositionWhileHidden();

        SyncLanguageSelection();
        SetLanguagePanelVisible(true);
        openPanelCoroutine = null;
    }

    void LateUpdate()
    {
        bool visible = IsLanguagePanelVisible();
        if (visible && !s_languagePanelWasVisible)
            SyncLanguageSelection();
        s_languagePanelWasVisible = visible;
    }

    void OnEnable()
    {
        SyncLanguageSelection();
    }

    void ApplyScrollPositionWhileHidden()
    {
        ForceRebuildContentLayout();

        if (!s_hasScrolledToTopOnFirstLanguageOpen)
        {
            ResetScrollToTop();
            s_hasScrolledToTopOnFirstLanguageOpen = true;
            SaveScrollPosition();
        }
        else if (s_hasSavedScrollPosition)
        {
            RestoreScrollPosition();
        }
    }

    void SetLanguagePanelVisible(bool visible)
    {
        if (canvasGroupDlg == null)
            return;

        canvasGroupDlg.alpha = visible ? 1f : 0f;
        canvasGroupDlg.blocksRaycasts = visible;
        canvasGroupDlg.interactable = visible;
    }

    bool IsLanguagePanelVisible()
    {
        return canvasGroupDlg != null && canvasGroupDlg.alpha > 0.01f;
    }

    void BuildLanguageListInternal()
    {
        if (HasLiveLanguageBindings())
            return;

        s_languageListBuilt = false;
        s_languageCodeToToggle.Clear();

        if (languageItems == null || languageItems.Length == 0)
        {
            Debug.LogError("[UILanguageDlg] Static languageItems are not assigned.");
            return;
        }

        var seenCodes = new HashSet<string>();

        foreach (var binding in languageItems)
        {
            var language = binding.i2LanguageName;
            var item = binding.toggle;
            if (string.IsNullOrWhiteSpace(language) || item == null)
            {
                Debug.LogError("[UILanguageDlg] Static language item binding is incomplete.");
                continue;
            }

            var code = LocalizationManager.GetLanguageCode(language);
            if (string.IsNullOrEmpty(code) || !seenCodes.Add(code))
            {
                Debug.LogError($"[UILanguageDlg] Invalid or duplicate language binding: {language}");
                continue;
            }

            item.gameObject.SetActive(true);
            s_languageCodeToToggle[code] = item;

            var captured = language;
            item.onValueChanged.AddListener(isOn =>
            {
                if (s_isApplyingSelection || !isOn)
                    return;

                AudioController.PlaySound(AudioController.AudioClips.buttonSoundSpread);
                SetPendingLanguage(captured);
            });
        }

        s_languageListBuilt = s_languageCodeToToggle.Count > 0;
        SyncLanguageSelection();
    }

    static bool HasLiveLanguageBindings()
    {
        if (!s_languageListBuilt || s_languageCodeToToggle.Count == 0)
            return false;

        foreach (var item in s_languageCodeToToggle.Values)
        {
            if (item == null)
                return false;
        }

        return true;
    }

    static void SyncLanguageSelection()
    {
        if (!s_languageListBuilt || s_languageCodeToToggle.Count == 0)
            return;

        var currentCode = LocalizationManager.GetLanguageCode(LocalizationManager.CurrentLanguage);
        if (string.IsNullOrEmpty(currentCode))
            return;

        s_isApplyingSelection = true;
        try
        {
            foreach (var kv in s_languageCodeToToggle)
            {
                var isCurrent = string.Equals(kv.Key, currentCode);
                kv.Value.SetIsOnWithoutNotify(isCurrent);
                kv.Value.OnToggleClick();
            }
        }
        finally
        {
            s_isApplyingSelection = false;
        }

        s_pendingLanguage = LocalizationManager.CurrentLanguage;
    }

    static void SetPendingLanguage(string i2LanguageName)
    {
        s_pendingLanguage = i2LanguageName;
    }

    void SaveScrollPosition()
    {
        if (languageScrollRect == null)
            return;

        s_savedVerticalNormalizedPosition = languageScrollRect.verticalNormalizedPosition;
        var content = languageScrollRect.content;
        s_savedContentAnchoredPosY = content != null ? content.anchoredPosition.y : 0f;
        s_hasSavedScrollPosition = true;
    }

    void ForceRebuildContentLayout()
    {
        Canvas.ForceUpdateCanvases();
        if (languageContentTransform is RectTransform contentRt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
    }

    void ResetScrollToTop()
    {
        if (languageScrollRect == null)
            return;

        languageScrollRect.StopMovement();
        languageScrollRect.velocity = Vector2.zero;
        languageScrollRect.verticalNormalizedPosition = 1f;
        languageScrollRect.horizontalNormalizedPosition = 0f;

        var content = languageScrollRect.content;
        if (content != null)
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
    }

    void RestoreScrollPosition()
    {
        if (languageScrollRect == null)
            return;

        languageScrollRect.StopMovement();
        languageScrollRect.velocity = Vector2.zero;
        languageScrollRect.verticalNormalizedPosition = s_savedVerticalNormalizedPosition;
        languageScrollRect.horizontalNormalizedPosition = 0f;

        var content = languageScrollRect.content;
        if (content != null)
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, s_savedContentAnchoredPosY);
    }

    static void ApplyLanguageSelection(string i2LanguageName)
    {
        LocalizationManager.CurrentLanguage = i2LanguageName;

        var code = LocalizationManager.GetLanguageCode(i2LanguageName);
        if (!string.IsNullOrEmpty(code) && GameModule.Setting != null)
            GameModule.Setting.SetLanguageByCode(code);
    }

    private void OnConfirmButtonClicked()
    {
        AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        if (!string.IsNullOrEmpty(s_pendingLanguage))
            ApplyLanguageSelection(s_pendingLanguage);

        CloseLanguagePanel();
    }

    /// <summary>系统返回键：语言面板打开时仅关闭面板，返回 true。</summary>
    public bool TryCloseOnBack()
    {
        if (!IsLanguagePanelVisible())
            return false;

        OnCloseButtonClicked();
        return true;
    }

    private void OnCloseButtonClicked()
    {
        AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        CloseLanguagePanel();
    }

    void CloseLanguagePanel()
    {
        SyncLanguageSelection();
        SaveScrollPosition();
        SetLanguagePanelVisible(false);
    }
}
