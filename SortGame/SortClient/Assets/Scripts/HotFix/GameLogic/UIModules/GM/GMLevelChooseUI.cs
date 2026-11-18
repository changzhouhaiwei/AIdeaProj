using System;
using System.Collections.Generic;
using Core.Tools.MethodExtension;
using FishTilePlay;
using FishFramework;
using TileGameCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GMLevelChooseUI : GPLevelChooseDlgBase
{
    private const string DefaultTimeInput = "2026.x.x.x.x.x";

    TMP_InputField puppyInputField;
    TButton puppyBtn;
    TMP_InputField piggyStageFiled;
    TButton setPiggyBtn;
    Button infoClickBtn;
    GameObject fullInfoPopup;
    TextMeshProUGUI fullInfoText;
    static Texture2D fullInfoWhiteTex;
    static Sprite fullInfoWhiteSprite;

    protected override void OnClicked(Button _button)
    {
        if (_button == jumpLevelBtn)//选择开始
        {
            int levelNum = levelInputFieldTMP.text.ToInt();
            levelNum--;
            
            DestroySelf();
            if (levelNum >= 0)
            {
                GPUserDataManager.Instance.SetLevelNum(levelNum);
                GameDirector.LoadLevel(levelNum);
            }
        }
        else if (_button == jumpInfinitLevelBtn)//跳转无尽关卡
        {
            int infiniteLevelNum = infinitLevelInputFieldTMP.text.ToInt();
            infiniteLevelNum--;

            DestroySelf();
            if (infiniteLevelNum >= 0)
            {
                int infiniteModeStartLevelNum = GPUserDataManager.Instance.GetInfiniteModeStartLevelNum();
                GPUserDataManager.Instance.SetLevelNum(infiniteModeStartLevelNum);
                GPUserDataManager.Instance.SetInfinitLevelNum(infiniteLevelNum);
                int realLevelNum = GPUserDataManager.Instance.GetFinalLevelNum();
                GameDirector.LoadLevel(realLevelNum);
            }
        }
        else if (_button == addItemsBtn)
        {
            GPUserDataManager.Instance.AddHintItem(5);
            GPUserDataManager.Instance.AddShuffleItem(5);
            GPUserDataManager.Instance.AddUndoItem(5);
            GPUserDataManager.Instance.AddHighLightItem(5);
            // DestroySelf();
        }
        else if (_button == deleteBtn)
        {
            GPUserDataManager.Instance.ResetData();
            InterADABTestConfig.TryAssignGroup();
            RefreshGoldTreeInput();
            RefreshLeagueInput();
            RefreshGoldTreeLeagueRelatedData();
            RefreshUidAndAbInfo();
            RefreshPuppyInput();
            RefreshPiggyInput();
        }
        else if (_button == setPiggyBtn)
        {
            ApplyPiggyStageFromInput();
        }
        else if (_button == puppyBtn)
        {
            ApplyPuppyProgressFromInput();
        }
        else if (_button == goldTreeBtn)
        {
            ApplyGoldTreeActPointFromInput();
        }
        else if (_button == leagueBtn)
        {
            ApplyGoldTreeLeagueTierFromInput();
        }
        else if (_button == addCoinBtn)
        {
            GPUserDataManager.Instance.AddCoin(5000);
        }
        else if (_button == winStreakBtn)
        {
            int streakDay = winStreakInputField.text.ToInt();
            GPUserDataManager.GPUserData.GmSetDailyVictoryStreak(streakDay);
            GPUserDataManager.Instance.SetPendingDailyVictorySettlementDlg(false);
            GPUserDataManager.Instance.SetHadWinYesterdayBeforeTodayFirstWin(streakDay > 0);
            SaveController.Save();
            RefreshWinStreakInput();
            GameEvent.Send((int)EventNameDef.DailyVictoryWinRecorded);
        }
        else if (_button == setTimeBtn)
        {
            if (TryParseTimeInput(timeImput.text, out long unixSeconds))
            {
                GameData.SetGmServerTime(unixSeconds);
                ReEnterGameAfterTimeChange();
            }
            else
            {
                Debug.LogWarning($"[GMLevelChooseUI] Invalid time input: {timeImput.text}");
            }
        }
        else if (_button == resetTimebySystem)
        {
            GameData.ResetGmServerTimeToSystem();
            ReEnterGameAfterTimeChange();
        }
#if UNITY_EDITOR
        else if (_button == playBotToggleBtn)
        {
            PlayBotController.SetHudEnabled(!PlayBotController.IsHudEnabled);
            RefreshPlayBotToggleLabel();
        }
#endif
        else
        {
            DestroySelf();
        }
    }

    private void ReEnterGameAfterTimeChange()
    {
        GameApp.Instance.ReLogin();
    }

    private void RefreshWinStreakInput()
    {
        if (winStreakInputField != null)
        {
            winStreakInputField.text = GPUserDataManager.GPUserData.dailyVictoryTotalWinDays.ToString();
        }
    }

    private void RefreshGoldTreeInput()
    {
        if (goldtreeNumInputField != null)
        {
            goldtreeNumInputField.text = GPUserDataManager.GPUserData.goldenTreeActPoint.ToString();
        }
    }

    private void RefreshLeagueInput()
    {
        if (leagueInputField != null)
        {
            int tier = GPUserDataManager.GPUserData.goldenTreeLeagueTier;
            leagueInputField.text = tier > 0 ? tier.ToString() : GoldTreeRobotRankRewardHelper.MinTierId.ToString();
        }
    }

    private void ApplyGoldTreeActPointFromInput()
    {
        if (goldtreeNumInputField == null)
        {
            return;
        }

        if (GameData.GoldTreeActData == null || !GameData.GoldTreeActData.GetBActOpen())
        {
            Debug.LogWarning("[GMLevelChooseUI] Gold tree activity is not open, cannot set act point.");
            RefreshGoldTreeInput();
            return;
        }

        int point = goldtreeNumInputField.text.ToInt();
        GPUserDataManager.GPUserData.GmSetGoldTreeActPoint(point);
        GPUserDataManager.Instance.SaveData();
        RefreshGoldTreeInput();
    }

    private void ApplyGoldTreeLeagueTierFromInput()
    {
        if (leagueInputField == null)
        {
            return;
        }

        int tier = leagueInputField.text.ToInt();
        if (!GoldTreeRobotRankRewardHelper.IsValidTierId(tier))
        {
            Debug.LogWarning($"[GMLevelChooseUI] Invalid league tier: {leagueInputField.text}. Valid range: {GoldTreeRobotRankRewardHelper.MinTierId}-{GoldTreeRobotRankRewardHelper.MaxTierId}.");
            RefreshLeagueInput();
            return;
        }

        GPUserDataManager.GPUserData.GmSetGoldTreeLeagueTier(tier);
        GPUserDataManager.Instance.SaveData();
        RefreshLeagueInput();
        RefreshGoldTreeLeagueRelatedData();
    }

    private void RefreshGoldTreeLeagueRelatedData()
    {
        if (GameData.Rank != null)
        {
            GameData.Rank.InvalidateRobotRankCacheForLeagueTierChange();
            GameData.Rank.GetRankList();
        }

        if (UIRankView.Instance != null)
        {
            UIRankView.Instance.RefreshAfterLeagueTierChanged();
        }

        if (UIRankChangeDlg.Instance != null)
        {
            UIRankChangeDlg.Instance.RefreshAfterLeagueTierChanged();
        }

        UIController.GetPage<UIMainMenu>()?.RefreshGoldTreeIcon();
    }

    private void RefreshCurDateTime()
    {
        if (curDateTime == null)
        {
            return;
        }

        var localDateTime = DateTimeOffset.FromUnixTimeSeconds(GameData.GetServerTime()).LocalDateTime;
        curDateTime.text = FormatLocalDateTime(localDateTime);
    }

    private void RefreshUidAndAbInfo()
    {
        EnsureUidAbTexts();

        if (uidInfo != null)
        {
            uidInfo.gameObject.SetActive(true);
            uidInfo.transform.SetAsLastSibling();
            string uid = GPUserDataManager.HasInit() ? GPUserDataManager.Instance.GetUID() : "";
            bool remoteReady = InterADABTestConfig.IsRemoteConfigReady();
            string config = remoteReady ? InterADABTestConfig.GetConfigForAnalytics() : "Not loaded";
            uidInfo.text =
                $"UID: {(string.IsNullOrEmpty(uid) ? "Unavailable" : uid)}\n" +
                $"RC: {(remoteReady ? "Ready" : "Not ready")}\n" +
                $"Config: {config}";
        }

        if (abInfo != null)
        {
            abInfo.gameObject.SetActive(true);
            abInfo.transform.SetAsLastSibling();
            string interGroup = InterADABTestConfig.GetTestGroupForAnalytics();
            if (string.IsNullOrWhiteSpace(interGroup))
            {
                string media = GPSDKMgr.Instance != null ? GPSDKMgr.Instance.GetMediaSource() : "";
                interGroup = string.IsNullOrWhiteSpace(media) || string.Equals(media, "unknown", System.StringComparison.OrdinalIgnoreCase)
                    ? "unassigned(waitAttr)"
                    : "unassigned";
            }
            int baseCd = InterADABTestConfig.GetIntervalSeconds();
            int effCd = InterADABTestConfig.GetEffectiveIntervalSeconds();
            int dayIndex = InterADABTestConfig.GetDayIndex();
            int level = GPUserDataManager.HasInit() ? GPUserDataManager.Instance.GetLevelNum() : -1;
            string mediaShow = GPSDKMgr.Instance != null ? GPSDKMgr.Instance.GetMediaSource() : "";
            if (string.IsNullOrWhiteSpace(mediaShow) || string.Equals(mediaShow, "unknown", System.StringComparison.OrdinalIgnoreCase))
                mediaShow = "-";
            abInfo.text =
                $"interAD: {interGroup}\nmedia: {mediaShow}\n" +
                $"cd={baseCd}->{effCd}s  d{dayIndex} lv={level} unlock={InterADABTestConfig.GetUnlockCompletedLevel()} load={InterADABTestConfig.GetLoadDisplayLevel()}\n" +
                $"eligible={InterADABTestConfig.IsEligible()}";
        }

        EnsureInfoClickHotspot();
        if (fullInfoPopup != null && fullInfoPopup.activeSelf)
        {
            LayoutFullInfoPopup();
            fullInfoPopup.transform.SetAsLastSibling();
        }
    }

    void EnsureInfoClickHotspot()
    {
        TextMeshProUGUI anchor = uidInfo != null ? uidInfo : abInfo;
        if (anchor == null)
            return;

        var parent = anchor.rectTransform.parent as RectTransform;
        if (parent == null)
            return;

        if (infoClickBtn == null)
        {
            var existing = parent.Find("uidAbInfoClick");
            if (existing != null)
                infoClickBtn = existing.GetComponent<Button>();
        }

        if (infoClickBtn == null)
        {
            var go = new GameObject("uidAbInfoClick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.raycastTarget = true;
            image.color = new Color(1f, 1f, 1f, 0.01f);

            infoClickBtn = go.GetComponent<Button>();
            infoClickBtn.transition = Selectable.Transition.None;
            infoClickBtn.targetGraphic = image;
            image.color = new Color(1f, 1f, 1f, 0.01f);
            var nav = infoClickBtn.navigation;
            nav.mode = Navigation.Mode.None;
            infoClickBtn.navigation = nav;
            infoClickBtn.onClick.AddListener(ToggleFullInfoPopup);
        }

        float top = float.NegativeInfinity;
        float bottom = float.PositiveInfinity;
        ExpandInfoBounds(uidInfo, ref top, ref bottom);
        ExpandInfoBounds(abInfo, ref top, ref bottom);
        if (top == float.NegativeInfinity)
            return;

        var rt = (RectTransform)infoClickBtn.transform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, top);
        rt.sizeDelta = new Vector2(560f, Mathf.Max(40f, top - bottom));
        infoClickBtn.transform.SetAsLastSibling();
        if (uidInfo != null)
            uidInfo.transform.SetAsLastSibling();
        if (abInfo != null)
            abInfo.transform.SetAsLastSibling();
    }

    static void ExpandInfoBounds(TextMeshProUGUI text, ref float top, ref float bottom)
    {
        if (text == null)
            return;

        var rt = text.rectTransform;
        float yTop = rt.anchoredPosition.y + (1f - rt.pivot.y) * rt.rect.height;
        float yBottom = yTop - rt.rect.height;
        if (yTop > top)
            top = yTop;
        if (yBottom < bottom)
            bottom = yBottom;
    }

    void ToggleFullInfoPopup()
    {
        if (fullInfoPopup != null && fullInfoPopup.activeSelf)
            HideFullInfoPopup();
        else
            ShowFullInfoPopup();
    }

    void ShowFullInfoPopup()
    {
        EnsureFullInfoPopup();
        if (fullInfoPopup == null)
            return;

        fullInfoPopup.SetActive(true);
        LayoutFullInfoPopup();
        fullInfoPopup.transform.SetAsLastSibling();
        var popupCanvas = fullInfoPopup.GetComponent<Canvas>();
        var parentCanvas = fullInfoPopup.transform.parent != null
            ? fullInfoPopup.transform.parent.GetComponentInParent<Canvas>()
            : null;
        if (popupCanvas != null && parentCanvas != null)
        {
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingLayerID = parentCanvas.sortingLayerID;
            popupCanvas.sortingOrder = parentCanvas.sortingOrder + 5;
        }
    }

    void HideFullInfoPopup()
    {
        if (fullInfoPopup != null)
            fullInfoPopup.SetActive(false);
    }

    void EnsureFullInfoPopup()
    {
        if (fullInfoPopup != null)
            return;

        TextMeshProUGUI anchor = uidInfo != null ? uidInfo : abInfo;
        if (anchor == null)
            return;

        var parent = anchor.rectTransform.parent as RectTransform;
        if (parent == null)
            return;

        var source = anchor;
        fullInfoPopup = new GameObject("fullInfoPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        fullInfoPopup.layer = parent.gameObject.layer;
        fullInfoPopup.transform.SetParent(parent, false);

        var popupCanvas = fullInfoPopup.AddComponent<Canvas>();
        popupCanvas.overrideSorting = true;
        var parentCanvas = parent.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            popupCanvas.sortingLayerID = parentCanvas.sortingLayerID;
            popupCanvas.additionalShaderChannels = parentCanvas.additionalShaderChannels
                | AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            popupCanvas.sortingOrder = parentCanvas.sortingOrder + 5;
        }
        fullInfoPopup.AddComponent<GraphicRaycaster>();

        var popupRt = (RectTransform)fullInfoPopup.transform;
        popupRt.anchorMin = new Vector2(0.5f, 1f);
        popupRt.anchorMax = new Vector2(0.5f, 1f);
        popupRt.pivot = new Vector2(0.5f, 1f);
        popupRt.anchoredPosition = new Vector2(0f, -64f);
        popupRt.sizeDelta = new Vector2(560f, 240f);

        var image = fullInfoPopup.GetComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.raycastTarget = true;
        image.color = new Color(0.16f, 0.2f, 0.38f, 0.98f);

        var btn = fullInfoPopup.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = image;
        image.color = new Color(0.16f, 0.2f, 0.38f, 0.98f);
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
        btn.onClick.AddListener(HideFullInfoPopup);

        var fillGo = new GameObject("fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.layer = parent.gameObject.layer;
        fillGo.transform.SetParent(fullInfoPopup.transform, false);
        var fillRt = (RectTransform)fillGo.transform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(3f, 3f);
        fillRt.offsetMax = new Vector2(-3f, -3f);
        var fill = fillGo.GetComponent<Image>();
        fill.sprite = GetWhiteSprite();
        fill.raycastTarget = false;
        fill.color = new Color(0.97f, 0.98f, 1f, 1f);

        var textGo = new GameObject("text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.layer = parent.gameObject.layer;
        textGo.transform.SetParent(fullInfoPopup.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(16f, 14f);
        textRt.offsetMax = new Vector2(-16f, -14f);

        fullInfoText = textGo.GetComponent<TextMeshProUGUI>();
        fullInfoText.font = source.font;
        if (source.fontSharedMaterial != null)
            fullInfoText.fontSharedMaterial = source.fontSharedMaterial;
        fullInfoText.fontSize = 22f;
        fullInfoText.color = new Color(0.12f, 0.1f, 0.22f, 1f);
        fullInfoText.alignment = TextAlignmentOptions.TopLeft;
        fullInfoText.enableWordWrapping = true;
        fullInfoText.overflowMode = TextOverflowModes.Overflow;
        fullInfoText.raycastTarget = false;
        fullInfoText.richText = false;
        fullInfoText.lineSpacing = 4f;

        fullInfoPopup.SetActive(false);
    }

    void LayoutFullInfoPopup()
    {
        if (fullInfoPopup == null || fullInfoText == null)
            return;

        string full = BuildFullInfoText();
        const float width = 560f;
        const float padX = 16f;
        const float padY = 14f;
        float textWidth = width - padX * 2f;

        var rt = (RectTransform)fullInfoPopup.transform;
        rt.anchoredPosition = new Vector2(0f, -64f);
        rt.sizeDelta = new Vector2(width, 120f);

        fullInfoText.fontSize = 22f;
        fullInfoText.text = full;
        fullInfoText.ForceMeshUpdate();
        float textHeight = MeasureFullInfoHeight(full, textWidth);

        var parentRt = fullInfoPopup.transform.parent as RectTransform;
        float maxHeight = parentRt != null ? parentRt.rect.height - 80f : 900f;
        if (maxHeight < 160f)
            maxHeight = 900f;

        while (textHeight + padY * 2f > maxHeight && fullInfoText.fontSize > 16f)
        {
            fullInfoText.fontSize -= 1f;
            fullInfoText.ForceMeshUpdate();
            textHeight = MeasureFullInfoHeight(full, textWidth);
        }

        float height = Mathf.Clamp(textHeight + padY * 2f, 96f, maxHeight);
        rt.sizeDelta = new Vector2(width, height);
    }

    float MeasureFullInfoHeight(string full, float textWidth)
    {
        float preferred = 0f;
        if (fullInfoText != null)
        {
            preferred = Mathf.Max(fullInfoText.preferredHeight, fullInfoText.GetPreferredValues(full, textWidth, 10000f).y);
        }

        if (preferred > 1f)
            return preferred;

        int lines = 1;
        if (!string.IsNullOrEmpty(full))
        {
            for (int i = 0; i < full.Length; i++)
            {
                if (full[i] == '\n')
                    lines++;
            }
        }

        float fontSize = fullInfoText != null ? fullInfoText.fontSize : 22f;
        return lines * (fontSize + 8f);
    }

    string BuildFullInfoText()
    {
        string uid = uidInfo != null ? uidInfo.text : "";
        string ab = abInfo != null ? abInfo.text : "";
        if (string.IsNullOrEmpty(uid))
            return ab ?? "";
        if (string.IsNullOrEmpty(ab))
            return uid;
        return uid + "\n" + ab;
    }

    static Sprite GetWhiteSprite()
    {
        if (fullInfoWhiteSprite != null)
            return fullInfoWhiteSprite;

        fullInfoWhiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        fullInfoWhiteTex.SetPixel(0, 0, Color.white);
        fullInfoWhiteTex.Apply();
        fullInfoWhiteTex.hideFlags = HideFlags.HideAndDontSave;
        fullInfoWhiteSprite = Sprite.Create(fullInfoWhiteTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        fullInfoWhiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return fullInfoWhiteSprite;
    }

    private void EnsureUidAbTexts()
    {
        if (viewBehaviour == null)
        {
            return;
        }

        var texts = viewBehaviour.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        if (uidInfo == null)
        {
            foreach (var text in texts)
            {
                if (text != null && text.gameObject.name == "uidInfo")
                {
                    uidInfo = text;
                    break;
                }
            }
        }

        if (abInfo == null)
        {
            foreach (var text in texts)
            {
                if (text != null && text.gameObject.name == "abInfo")
                {
                    abInfo = text;
                    break;
                }
            }
        }
    }

    private static string FormatLocalDateTime(DateTime dateTime)
    {
        return $"{dateTime.Year}.{dateTime.Month}.{dateTime.Day}.{dateTime.Hour}.{dateTime.Minute}.{dateTime.Second}";
    }

    private static bool TryParseTimeInput(string input, out long unixSeconds)
    {
        unixSeconds = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string[] parts = input.Trim().Split('.');
        if (parts.Length < 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out int year)
            || !int.TryParse(parts[1], out int month)
            || !int.TryParse(parts[2], out int day))
        {
            return false;
        }

        int hour = 12;
        int minute = 0;
        int second = 0;

        if (parts.Length >= 4 && !int.TryParse(parts[3], out hour))
        {
            return false;
        }

        if (parts.Length >= 5 && !int.TryParse(parts[4], out minute))
        {
            return false;
        }

        if (parts.Length >= 6 && !int.TryParse(parts[5], out second))
        {
            return false;
        }

        if (parts.Length > 6)
        {
            return false;
        }

        try
        {
            var localDateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
            unixSeconds = new DateTimeOffset(localDateTime).ToUnixTimeSeconds();
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        HideFullInfoPopup();
        GameModule.Setting.EnableGmLevelPassGlobalSwitch();
        UIController.GetPage<UIGame>()?.RefreshGmDebugUI();
        UITestDynamicLevel.RefreshActive();
        RefreshWinStreakInput();
        RefreshGoldTreeInput();
        RefreshLeagueInput();
        RefreshUidAndAbInfo();
        EnsurePuppyGmControls();
        RefreshPuppyInput();
        EnsurePiggyGmControls();
        RefreshPiggyInput();
        BindPlayBotToggle();
        GameBAData.ExcuteGMMode();

        if (timeImput != null)
        {
            timeImput.text = DefaultTimeInput;
        }

        RefreshCurDateTime();
        Interval(1f, _ => RefreshCurDateTime(), useRealTime: true);
    }

    void EnsurePuppyGmControls()
    {
        if (viewBehaviour == null)
            return;

        if (puppyInputField == null)
        {
            var inputs = viewBehaviour.GetComponentsInChildren<TMP_InputField>(true);
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] != null && inputs[i].gameObject.name == "PuppyInputField (1)")
                {
                    puppyInputField = inputs[i];
                    puppyInputField.onSelect.AddListener(_ => RefreshPuppyInput());
                    break;
                }
            }
        }

        if (puppyBtn == null)
        {
            var buttons = viewBehaviour.GetComponentsInChildren<TButton>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "PuppyBtn (1)")
                {
                    puppyBtn = buttons[i];
                    BindEvent(puppyBtn);
                    break;
                }
            }
        }
    }

    void RefreshPuppyInput()
    {
        if (puppyInputField == null)
            return;
        puppyInputField.text = $"{PuppyAdventureDemoState.CurWin}/{PuppyAdventureDemoState.WinTarget}";
    }

    void ApplyPuppyProgressFromInput()
    {
        if (puppyInputField == null)
            return;

        string raw = puppyInputField.text != null ? puppyInputField.text.Trim() : "";
        string[] parts = raw.Split('/');
        if (parts.Length != 2
            || !int.TryParse(parts[0].Trim(), out int wins)
            || !int.TryParse(parts[1].Trim(), out int winTarget))
        {
            Debug.LogWarning($"[GMLevelChooseUI] Invalid puppy progress input: {raw}. Use wins/target e.g. 2/5.");
            RefreshPuppyInput();
            return;
        }

        if (!PuppyAdventureDemoState.GmSetProgress(wins, winTarget))
        {
            RefreshPuppyInput();
            return;
        }

        RefreshPuppyInput();
    }

    void EnsurePiggyGmControls()
    {
        if (viewBehaviour == null)
            return;

        if (piggyStageFiled == null)
        {
            var inputs = viewBehaviour.GetComponentsInChildren<TMP_InputField>(true);
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] != null && inputs[i].gameObject.name == "piggyStageFiled")
                {
                    piggyStageFiled = inputs[i];
                    break;
                }
            }
        }

        if (setPiggyBtn == null)
        {
            var buttons = viewBehaviour.GetComponentsInChildren<TButton>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "setPiggyBtn")
                {
                    setPiggyBtn = buttons[i];
                    BindEvent(setPiggyBtn);
                    break;
                }
            }
        }
    }

    void RefreshPiggyInput()
    {
        if (piggyStageFiled == null)
            return;
        piggyStageFiled.text = PiggyBankState.GmProgressText();
    }

    void ApplyPiggyStageFromInput()
    {
        if (piggyStageFiled == null)
            return;

        string raw = piggyStageFiled.text != null ? piggyStageFiled.text.Trim() : "";
        string[] parts = raw.Split('/');
        if (parts.Length != 2
            || !int.TryParse(parts[0].Trim(), out int progress)
            || !int.TryParse(parts[1].Trim(), out int stageLevel)
            || !PiggyBankState.GmSetStage(progress, stageLevel))
        {
            Debug.LogWarning($"[GMLevelChooseUI] Invalid piggy stage input: {raw}. Use progress/target e.g. 5/5, 2/2, 7/3.");
            RefreshPiggyInput();
            return;
        }

        RefreshPiggyInput();
        PiggyBankBarView[] bars = UnityEngine.Object.FindObjectsOfType<PiggyBankBarView>(true);
        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] != null)
                bars[i].RefreshAfterExternalChange();
        }
    }

    TButton playBotToggleBtn;
    TextMeshProUGUI playBotToggleLabel;

    void BindPlayBotToggle()
    {
        if (playBotToggleBtn == null && viewBehaviour != null)
        {
            var buttons = viewBehaviour.GetComponentsInChildren<TButton>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.name == "playBotToggleBtn")
                {
                    playBotToggleBtn = buttons[i];
                    playBotToggleLabel = buttons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    BindEvent(playBotToggleBtn);
                    break;
                }
            }
        }

#if UNITY_EDITOR
        RefreshPlayBotToggleLabel();
#else
        if (playBotToggleBtn != null)
            playBotToggleBtn.gameObject.SetActive(false);
#endif
    }

#if UNITY_EDITOR
    void RefreshPlayBotToggleLabel()
    {
        if (playBotToggleLabel != null)
            playBotToggleLabel.text = PlayBotController.IsHudEnabled ? "PlayBot ON" : "PlayBot OFF";
    }
#endif
}
