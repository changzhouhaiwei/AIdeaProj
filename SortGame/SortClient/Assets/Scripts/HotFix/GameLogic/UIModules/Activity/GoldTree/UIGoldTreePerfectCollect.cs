using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIGoldTreePerfectCollect : UIGoldTreePerfectCollectBase
{
    public static UIGoldTreePerfectCollect Instance;

    public Action AfterAction;

    public static bool IsShowing => Instance != null;

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == backBtn)
        {
            CloseSelf();
            AfterAction?.Invoke();
        }
    }

    protected override void OnOpen()
    {
        Instance = this;
        base.OnOpen();
    }

    protected override void OnDestroyed()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        AfterAction = null;
        base.OnDestroyed();
    }

    protected override void PlayOpenSound()
    {
        if (!FishTilePlay.CvVoiceSettings.IsEnabled)
            return;

        var clips = TileGameCore.AudioController.AudioClips;
        if (clips.IsNull())
            return;

        var clip = clips.perfectCollectPanel != null ? clips.perfectCollectPanel : clips.CVPerfect;
        if (clip == null)
        {
            Debug.LogError("[GoldTree] perfectCollectPanel audio clip is not assigned on Audio Settings.");
            return;
        }

        TileGameCore.AudioController.PlaySound(clip);
    }

}
