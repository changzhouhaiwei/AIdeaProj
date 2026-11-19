using System;
using FishFramework;
using UnityEngine;
using UnityEngine.UI;

public class UIStoryBgPassDlg : UIStoryBgPassDlgBase
{
    public Action EndAction;

    private bool _isManualOpen;
    private GameObject _tapContinue;
    private Animator _panelAnimator;
    private Timer _appearReleaseTimer;

    private const string AppearClipName = "UIStoryBgPass_In";
    private const float AppearFallbackDuration = 1.25f;

    protected override void OnClicked(Button button)
    {
        if (button == okBtn || button == tapokBtn)
        {
            // Keep Animator off so clickDoTween isn't overwritten mid-press.
            ReleaseOkBtnPressScale();

            if (XSortGameCore.AudioController.AudioClips != null)
                XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.buttonSound);

            EndAction?.Invoke();
            DestroySelf();
        }
    }

    public void RefreshUI(cfg.Game.GameBgCfg oneCfg, bool isManualOpen = false)
    {
        _isManualOpen = isManualOpen;
        ApplyOkButtons();

        int startLevel = oneCfg.StartLevel;
        int endLevel = oneCfg.EndLevel;

        string levelStr = I2LocalizeMgr.GetString("LevelName");
        titleText.text = I2LocalizeMgr.GetString(oneCfg.Name);
        descLb.text = I2LocalizeMgr.GetString(oneCfg.Desc);
        levelRangeLb.text = levelStr + " " + startLevel + "-" + endLevel;
        bg.sprite = LoadSpriteBg("SceneBg", "bg_" + oneCfg.Path);
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        ApplyOkButtons();
        ScheduleReleaseOkBtnPressScale();
    }

    private void ApplyOkButtons()
    {
        okBtn.gameObject.SetActive(!_isManualOpen);
        tapokBtn.gameObject.SetActive(_isManualOpen);

        if (_tapContinue == null && gameObject != null)
            _tapContinue = gameObject.transform.Find("TapContinue")?.gameObject;
        if (_tapContinue != null)
            _tapContinue.SetActive(_isManualOpen);
    }

    /// <summary>
    /// Appear clip (UIStoryBgPass_In) drives StoryNineBg/okBtn scale; keep Animator off after so TButton press works.
    /// </summary>
    private void ScheduleReleaseOkBtnPressScale()
    {
        CancelAppearReleaseTimer();
        _appearReleaseTimer = Delay(GetAppearDuration(), () =>
        {
            _appearReleaseTimer = null;
            ReleaseOkBtnPressScale();
        }, useRealTime: true);
    }

    private float GetAppearDuration()
    {
        var animator = EnsurePanelAnimator();
        var controller = animator != null ? animator.runtimeAnimatorController : null;
        if (controller == null)
            return AppearFallbackDuration;

        foreach (var clip in controller.animationClips)
        {
            if (clip != null && clip.name == AppearClipName)
                return clip.length;
        }

        return AppearFallbackDuration;
    }

    private void ReleaseOkBtnPressScale()
    {
        if (gameObject == null)
            return;

        var animator = EnsurePanelAnimator();
        if (animator != null)
            animator.enabled = false;

        ConfigureButtonPressScale(okBtn);
    }

    private static void ConfigureButtonPressScale(TButton button)
    {
        if (button == null)
            return;

        button.clickDoTween = true;
        button.transition = Selectable.Transition.None;
        button.SetRestingScale(Vector3.one);
    }

    private Animator EnsurePanelAnimator()
    {
        if (_panelAnimator == null && gameObject != null)
            _panelAnimator = gameObject.GetComponent<Animator>();
        return _panelAnimator;
    }

    private void CancelAppearReleaseTimer()
    {
        if (_appearReleaseTimer == null)
            return;

        Unschedule(_appearReleaseTimer);
        _appearReleaseTimer = null;
    }

    protected override void PlayOpenSound()
    {
        var clip = XSortGameCore.AudioController.AudioClips?.bgChange;
        if (clip != null)
            XSortGameCore.AudioController.PlaySound(clip);
    }

    protected override bool OnEscButtonPressed()
    {
        return true;
    }
}
