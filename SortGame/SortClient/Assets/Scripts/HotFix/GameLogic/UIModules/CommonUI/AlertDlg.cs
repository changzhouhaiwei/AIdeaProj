using DG.Tweening;
using UnityEngine;
using FishFramework;

public class AlertDlg : AlertDlgBase
{
    private static int haveCount;
    private const int maxCount = 6;

    private void InitUI(string desc)
    {
        tipText.text = desc;
        DoUITween();
    }

    private void DoUITween()
    {
        nineBg_CanvasGroup.alpha = 0;

        int idx = Mathf.Min(maxCount, haveCount);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(nineBg_RectTransform.DOAnchorPosY(150 + 64 * (idx - 1), 0.5f).SetEase(Ease.OutCirc));
        sequence.Join(nineBg_CanvasGroup.DOFade(1, 0.4f));
        sequence.AppendInterval(0.5f);
        sequence.AppendCallback(DeleteMe);
        sequence.SetUpdate(true);
    }

    private void DeleteMe()
    {
        haveCount--;
        DestroySelf();
    }

    public static void ShowDlg(string desc)
    {
        Debug.Log("AlertDlg  =" + desc);

        haveCount++;
        AlertDlg alertDlg = GameModule.UI.OpenPanel<AlertDlg>(PanelLayer.Top, PanelOpenType.Multiple);
        alertDlg.InitUI(desc);
    }
}