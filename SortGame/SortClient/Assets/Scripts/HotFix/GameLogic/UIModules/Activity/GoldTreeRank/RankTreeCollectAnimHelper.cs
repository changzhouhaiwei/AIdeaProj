using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// RankTreeCollect 预制体里 goldtreeNum 文本的收集数量飘字动画。
/// </summary>
public static class RankTreeCollectAnimHelper
{
    const float FadeInTime = 0.2f;
    const float HoldTime = 0.7f;
    const float FadeOutTime = 0.9f;

    public static void PlayGoldNumAnim(RectTransform animRt, int collectNum)
    {
        if (animRt == null)
            return;

        var numTr = animRt.Find("goldtreeNum");
        if (numTr == null)
            return;

        var go = numTr.gameObject;

        // 没有收集数量时不展示飘字
        if (collectNum <= 0)
        {
            go.SetActive(false);
            return;
        }

        go.SetActive(true);

        var tmp = numTr.GetComponent<TMP_Text>();
        if (tmp != null)
            tmp.text = "+" + collectNum;

        var cg = numTr.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = go.AddComponent<CanvasGroup>();

        cg.DOKill();
        cg.alpha = 0f;

        var seq = DOTween.Sequence();
        seq.SetTarget(cg);
        seq.Append(cg.DOFade(1f, FadeInTime));
        seq.AppendInterval(HoldTime);
        seq.Append(cg.DOFade(0f, FadeOutTime));
        seq.SetLink(go, LinkBehaviour.KillOnDestroy);
        seq.Play();
    }
}
