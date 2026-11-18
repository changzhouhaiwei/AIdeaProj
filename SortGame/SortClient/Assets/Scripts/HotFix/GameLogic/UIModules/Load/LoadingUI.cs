using System.Text.RegularExpressions;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;

public class LoadingUI : LoadingLoginUIBase
{
    private const string LoadingZzNodeName = "SkeletonGraphic (Loading_zz)";
    private const string LoadingZzLoopAnim = "Loading_zz";

    private Tween loadingSpinTween;
    private SkeletonGraphic loadingZzSpine;

    public void SetMessage(float progress)
    {
        Debug.Log($"Load the game scene without cell data... {progress * 100:F2}%");
    }

    private void ShowQuate()
    {
        int idx = GameData.Player.QuateId;

        string text = I2LocalizeMgr.GetString("FamousQuotes" + idx);
        // string[] parts = text.Split('—');
        string[] parts = Regex.Split(text, "-+|—+"); // - 连续 或 — 连续 都能切
        
        if (parts.Length > 1)
        {
            quateDescLb.text = parts[0];
            quateNameLb.text = "—" + parts[1];
        }
        else
        {
            quateDescLb.text = "";
            quateNameLb.text = "";
        }
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        PlayLoadingZzLoop();
    }

    protected override void OnCreating()
    {
        base.OnCreating();
        ShowQuate();

        var loadingIcon = viewBehaviour != null ? viewBehaviour.transform.Find("loading") : null;
        loadingSpinTween = LoadingIconSpin.Start(loadingIcon);
    }

    protected override void OnDestroyed()
    {
        LoadingZzSpinePlayback.Release();
        LoadingIconSpin.Stop(ref loadingSpinTween);
        base.OnDestroyed();
    }

    private void PlayLoadingZzLoop()
    {
        if (loadingZzSpine == null && viewBehaviour != null)
        {
            var spineTransform = viewBehaviour.transform.Find(LoadingZzNodeName);
            loadingZzSpine = spineTransform != null ? spineTransform.GetComponent<SkeletonGraphic>() : null;
        }

        if (loadingZzSpine == null)
        {
            return;
        }

        LoadingZzSpinePlayback.Show(loadingZzSpine, LoadingZzLoopAnim);
    }
}
