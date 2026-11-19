using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishXSortPlay;
using XSortGameCore;
using TMPro;
using System;

public class UITipsDlg : MonoBehaviour
{
    const float FallbackAnimDuration = 1.5f;

    [SerializeField] List<GameObject> bgList;

    [SerializeField] List<TextMeshProUGUI> lbList;

    [SerializeField] List<TextMeshProUGUI> rateLbList;

    [SerializeField] CanvasGroup tintCanvasGroupNode;

    GameObject cellNode;

    public void PlayShowTint(Action afterDoFunc = null)
    {
        tintCanvasGroupNode.SetActiveEx(false);
        var levelIndex = GameDirector.levelController.GetLoadedLevelIndex();
        var cfg = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);
        if (cfg == null || string.IsNullOrEmpty(cfg.PassRate))
        {
            afterDoFunc?.Invoke();
            return;
        }

        BindNodes();
        var passRate = cfg.PassRate ?? "";
        bool isCoinLevel = ActivityData.IsCoinRewardLevelEnabled() && cfg.Coinlevel == 1;
        if (cellNode != null)
            cellNode.SetActive(true);
        CheckShowBgAndText(passRate, cfg.Difficulty, isCoinLevel);
        if (isCoinLevel)
        {
            var intoClip = AudioController.AudioClips?.coinlevel_into;
            if (intoClip != null)
                AudioController.PlaySound(intoClip);
        }

        tintCanvasGroupNode.alpha = 1f;
        tintCanvasGroupNode.transform.localScale = Vector3.one;
        tintCanvasGroupNode.gameObject.SetActive(true);
        BringAboveBoxCoverHud();

        RaycastController.Disable();
        float duration = PlayNodeAnimator(cellNode);
        Tween.DelayedCall(duration, () =>
        {
            if (this != null && tintCanvasGroupNode != null)
                tintCanvasGroupNode.gameObject.SetActive(false);
            ReleaseAboveBoxCoverHud();
            RaycastController.Enable();
            afterDoFunc?.Invoke();
        });
    }

    void BindNodes()
    {
        if (cellNode != null)
            return;

        var cell = transform.Find("cell");
        if (cell != null)
            cellNode = cell.gameObject;
    }

    float PlayNodeAnimator(GameObject node)
    {
        var animator = node != null ? node.GetComponent<Animator>() : null;
        var clips = animator != null ? animator.runtimeAnimatorController?.animationClips : null;
        if (clips == null)
            return FallbackAnimDuration;

        for (int i = 0; i < clips.Length; i++)
        {
            var clip = clips[i];
            if (clip == null)
                continue;

            animator.Rebind();
            animator.Play(clip.name, 0, 0f);
            return Mathf.Max(clip.length, 0.1f);
        }

        return FallbackAnimDuration;
    }

    Canvas overlayCanvas;

    void BringAboveBoxCoverHud()
    {
        Canvas own = GetComponent<Canvas>();
        Canvas parentCanvas = null;
        var canvases = GetComponentsInParent<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i] != own)
            {
                parentCanvas = canvases[i];
                break;
            }
        }

        if (overlayCanvas == null)
        {
            overlayCanvas = own != null && own != parentCanvas
                ? own
                : gameObject.AddComponent<Canvas>();
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        if (parentCanvas != null)
            overlayCanvas.additionalShaderChannels = parentCanvas.additionalShaderChannels;

        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = (parentCanvas != null ? parentCanvas.sortingOrder : 0) + 5;
    }

    void ReleaseAboveBoxCoverHud()
    {
        if (overlayCanvas != null)
            overlayCanvas.overrideSorting = false;
    }

    void CheckShowBgAndText(string passRate, int diff = 0, bool isCoinLevel = false)
    {
        if (bgList == null || bgList.Count == 0)
            return;

        int textIdx = Mathf.Clamp(diff, 0, 2);
        int bgIdx = isCoinLevel && bgList.Count > 3
            ? 3
            : Mathf.Clamp(textIdx, 0, bgList.Count - 1);

        for (int i = 0; i < bgList.Count; i++)
        {
            var obj = bgList[i];
            if (obj == null)
                continue;
            obj.SetActive(i == bgIdx);
        }

        // 金币关 cellBg4 用 prefab / I2 默认文案，不套通关率模板
        if (isCoinLevel)
            return;

        int lbIdx = bgIdx < lbList.Count ? bgIdx : textIdx;
        var lb = lbList[lbIdx];
        var template = I2LocalizeMgr.GetString("EnterTheLevelTips" + (textIdx + 1));
        // 文案曾用 {passRate}，现多为 {0}；两种都替换，避免占位符原样显示。
        string result = template
            .Replace("{passRate}", passRate)
            .Replace("{0}", passRate);
        lb.text = result;

        if (rateLbList != null && bgIdx < rateLbList.Count && rateLbList[bgIdx] != null)
        {
            rateLbList[bgIdx].text = passRate;
        }
    }
}

/// <summary>
/// 同一关每个本地日历日最多打开 TipsDlg 三次。没有通关率文案时本来就不播，不计次。
/// 有限关按关卡索引；无限关按当前关卡序号，避免复用布局把不同关算成同一关。
/// </summary>
public static class TipsDlgDailyCap
{
    public const int MaxPerLevel = 3;

    public static bool Allow(int levelIndex)
    {
        SelfCheckOnce();

        var cfg = LevelDynamicConfigProvider.GetLevelConfig(levelIndex + 1);
        if (cfg == null || string.IsNullOrEmpty(cfg.PassRate))
            return true;

        var mgr = GPUserDataManager.Instance;
        var user = mgr != null ? mgr.UserData : null;
        if (user == null)
            return true;

        if (!TryConsume(user.GetTodayLocalDate(), CapKey(levelIndex), user.tipsDlgDailyOpen, out string next))
            return false;

        user.tipsDlgDailyOpen = next;
        mgr.SaveData();
        return true;
    }

    static int CapKey(int levelIndex)
    {
        var mgr = GPUserDataManager.Instance;
        if (mgr != null && mgr.GetBInfinitMode())
            return -(mgr.GetShowLevelNum() + 1);
        return levelIndex;
    }

    public static bool TryConsume(int today, int levelKey, string record, out string next)
    {
        var map = new Dictionary<int, int>();
        if (!string.IsNullOrEmpty(record))
        {
            int bar = record.IndexOf('|');
            if (bar > 0
                && int.TryParse(record.Substring(0, bar), out int savedDay)
                && savedDay == today)
            {
                string body = record.Substring(bar + 1);
                if (body.Length > 0)
                {
                    string[] parts = body.Split(',');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        int colon = parts[i].IndexOf(':');
                        if (colon <= 0)
                            continue;
                        if (int.TryParse(parts[i].Substring(0, colon), out int id)
                            && int.TryParse(parts[i].Substring(colon + 1), out int count))
                            map[id] = count;
                    }
                }
            }
        }

        map.TryGetValue(levelKey, out int shown);
        if (shown >= MaxPerLevel)
        {
            next = record ?? "";
            return false;
        }

        map[levelKey] = shown + 1;
        var pieces = new string[map.Count];
        int n = 0;
        foreach (var pair in map)
            pieces[n++] = pair.Key + ":" + pair.Value;
        next = today + "|" + string.Join(",", pieces);
        return true;
    }

    static bool sChecked;

    static void SelfCheckOnce()
    {
        if (sChecked)
            return;
        sChecked = true;

        Debug.Assert(TryConsume(20261112, 3, "", out string first) && first == "20261112|3:1");
        Debug.Assert(TryConsume(20261112, 3, first, out string second) && second == "20261112|3:2");
        Debug.Assert(TryConsume(20261112, 3, "20261112|3:2", out string third) && third == "20261112|3:3");
        Debug.Assert(!TryConsume(20261112, 3, "20261112|3:3", out _));
        Debug.Assert(TryConsume(20261113, 3, "20261112|3:3", out string nextDay) && nextDay == "20261113|3:1");
        Debug.Assert(TryConsume(20261112, 4, "20261112|3:3", out string other) && other.Contains("4:1") && other.Contains("3:3"));
    }
}
