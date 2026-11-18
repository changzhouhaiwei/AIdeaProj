using System;
using System.Collections.Generic;
using Core.Tools.MethodExtension;
using DG.Tweening;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;
using TileGameCore;

public class UICompleteAdAward : GPViewBase
{
    [Header("Scroll Config")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private List<RectTransform> cells;

    [SerializeField] private GameObject niceBg;
    [SerializeField] private GameObject niceEffect;
    [SerializeField] private GameObject luckyBg;
    [SerializeField] private GameObject luckEffect;

    [SerializeField] private Animation nlAnimation;

    private List<string> awardDatas = new();

    public float cellHeight = 100f;
    public float maxSpeed = 1500f;
    public float duration = 2.5f;
    public bool moveUp = true;

    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private float elapsed;
    private float contentHeight;
    private bool isAnimating;
    private float moveDis;

    private bool hasShowQuality;

    bool hasInitData = false;

    private int nextLapToAnnounce = 1;
    private Action scrollCompleteAction;

    // 滚动动画是否正在播放，用于外部在动画结束前屏蔽点击
    public bool IsAnimating => isAnimating;

    void Start()
    {
        contentHeight = cellHeight * cells.Count;

        EnsureAwardDataReady();
    }

    /// <summary>
    /// 惰性初始化广告奖励配置。通关滚动可能在 Start 之前触发，不能只依赖 Start。
    /// </summary>
    public bool EnsureAwardDataReady()
    {
        if (hasInitData && awardDatas.Count > 0)
        {
            return true;
        }

        CheckInitData();
        return hasInitData && awardDatas.Count > 0;
    }

    void CheckInitData()
    {
        if (hasInitData && awardDatas.Count > 0)
        {
            return;
        }

        var dic = LevelDynamicConfigProvider.GetAdRewardMap();
        if (dic == null || dic.Count == 0 || cells == null || cells.Count == 0)
        {
            Debug.LogWarning("[UICompleteAdAward] Ad reward config or cells missing, skip init.");
            return;
        }

        awardDatas.Clear();
        int slotCount = Mathf.Min(3, cells.Count);
        for (int i = 0; i < slotCount; i++)
        {
            if (!dic.TryGetValue(i + 1, out var oneData) || oneData?.Award == null || oneData.Award.Count == 0)
            {
                Debug.LogWarning($"[UICompleteAdAward] Missing ad reward config id={i + 1}");
                awardDatas.Clear();
                return;
            }

            var oneCell = cells[i];
            string oneAward = oneData.Award[0];
            if (string.IsNullOrEmpty(oneAward))
            {
                Debug.LogWarning($"[UICompleteAdAward] Empty award string at id={i + 1}");
                awardDatas.Clear();
                return;
            }

            Image icon = oneCell.GetComponentInChildren<Image>();
            TextMeshProUGUI text = oneCell.GetComponentInChildren<TextMeshProUGUI>();

            var parts = oneAward.Split(',');
            var id = parts[0];
            if (icon != null)
            {
                icon.sprite = LoadSprite("LevelAward", "item_" + id);
            }

            if (text != null && parts.Length > 1)
            {
                text.text = "X " + parts[1];
            }

            awardDatas.Add(oneAward);
        }

        hasInitData = awardDatas.Count > 0;
    }

    /// <summary>
    /// 播放滚动（随机结果）
    /// </summary>
    public void PlayScrollAnim(Action onComplete = null)
    {
        int randomIndex = UnityEngine.Random.Range(0, cells.Count);
        if (randomIndex > 0)
        {

        }
        ResetCellPos();
        PlayScrollAnim(randomIndex, onComplete);
    }

    void ResetCellPos()
    {
        niceBg.SetActive(false);
        niceEffect.SetActive(false);
        luckyBg.SetActive(false);
        luckEffect.SetActive(false);

        for (int i = 0; i < cells.Count; i++)
        {
            var oneCell = cells[i];
            oneCell.anchoredPosition = new Vector2(0, -cellHeight * i);
        }
    }

    void CheckShowNiceOrLucky()
    {

        if (!hasShowQuality)
        {
            niceEffect.SetActive(false);
            luckEffect.SetActive(false);

            // nlAnimation.enabled = true;
            nlAnimation.Play();

            niceBg.SetActive(mCurTargetIdx == 1);
            luckyBg.SetActive(mCurTargetIdx == 2);

            Utility.DGDelayedCall(45f / 60f, () =>
            {
                niceEffect.SetActive(mCurTargetIdx == 1);
                luckEffect.SetActive(mCurTargetIdx == 2);
            });

            hasShowQuality = true;
        }
    }

    /// <summary>
    /// 播放滚动（指定停在某个 Cell）
    /// </summary>
    /// 

    float targetDis = 1000f;

    int mCurTargetIdx = 0;

    /// <summary>
    /// 不定格滚动，直接停在目标奖励格（结算默认展示用）。
    /// </summary>
    public void SnapToReward(int target)
    {
        isAnimating = false;
        scrollCompleteAction = null;
        elapsed = 0f;
        moveDis = 0f;
        mCurTargetIdx = target;
        hasShowQuality = true;

        ResetCellPos();
        if (cells == null || cells.Count == 0)
        {
            return;
        }

        int count = cells.Count;
        int normalized = ((target % count) + count) % count;
        for (int i = 0; i < count; i++)
        {
            int offset = i - normalized;
            cells[i].anchoredPosition = new Vector2(0, -cellHeight * offset);
        }

        niceBg.SetActive(normalized == 1);
        luckyBg.SetActive(normalized == 2);
        niceEffect.SetActive(normalized == 1);
        luckEffect.SetActive(normalized == 2);
    }

    public void PlayScrollAnim(int target, Action onComplete = null)
    {
        ResetCellPos();
        elapsed = 0;
        moveDis = 0;
        targetDis = cells.Count * 3 * 100 + target * 100;
        mCurTargetIdx = target;
        hasShowQuality = false;
        isAnimating = true;
        nextLapToAnnounce = 1;
        scrollCompleteAction = onComplete;
    }

    public string GetRewardString(int idx)
    {
        if (!EnsureAwardDataReady())
        {
            return null;
        }

        if (idx < 0 || idx >= awardDatas.Count)
        {
            Debug.LogError($"[UICompleteAdAward] GetRewardString idx={idx} out of range, count={awardDatas.Count}");
            return null;
        }

        return awardDatas[idx];
    }

    public int RewardCount
    {
        get
        {
            EnsureAwardDataReady();
            return awardDatas.Count;
        }
    }

    void FixedUpdate()
    {
        if (!isAnimating) return;

        elapsed += Time.fixedDeltaTime;
        float t = Mathf.Clamp01(moveDis / targetDis);

        if (t >= 0.7)
        {
            CheckShowNiceOrLucky();
        }

        float speed = maxSpeed * easeCurve.Evaluate(t);
        float delta = speed * Time.deltaTime * (moveUp ? 1 : -1);
        moveDis += delta;

        //播放音效,滚动1/3轮播一次
        int thirdLapsDone = Mathf.FloorToInt(Mathf.Abs(moveDis) / (contentHeight / 3f));
        while (thirdLapsDone >= nextLapToAnnounce)
        {
            TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.circleRotation);
            nextLapToAnnounce++;
        }

        Reposition();

        float deltaTarget = 0;
        Action completeAction = null;
        if (math.abs(moveDis) >= math.abs(targetDis))
        {
            isAnimating = false;
            deltaTarget = targetDis - moveDis;
            completeAction = scrollCompleteAction;
            scrollCompleteAction = null;
            // SnapToTarget();
        }

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2 pos = cells[i].anchoredPosition;
            pos.y += delta + deltaTarget;
            cells[i].anchoredPosition = pos;
        }

        completeAction?.Invoke();
    }

    /// <summary>
    /// 循环复位
    /// </summary>
    void Reposition()
    {
        float upperBound = viewport.anchoredPosition.y + cellHeight;
        float lowerBound = viewport.anchoredPosition.y - contentHeight;

        for (int i = 0; i < cells.Count; i++)
        {
            RectTransform cell = cells[i];

            if (moveUp && cell.anchoredPosition.y > upperBound)
            {
                cell.anchoredPosition -= new Vector2(0, contentHeight);
            }
            else if (!moveUp && cell.anchoredPosition.y < lowerBound)
            {
                cell.anchoredPosition += new Vector2(0, contentHeight);
            }
        }
    }

    void PlayGetOtherItemAnim(GameObject animCell, Action act = null)
    {
        float animTime = 1.0f;

        var rt = animCell.GetComponent<RectTransform>();
        var dstPt = rt.anchoredPosition + new Vector2(0, 120);

        rt.DOAnchorPos(dstPt, animTime);
        animCell.transform.DOScale(0.4f, animTime);
        animCell.GetComponent<CanvasGroup>().DOFade(0, animTime).onComplete = () =>
        {
            act?.Invoke();
        };

    }

    Action mAwardGotAction = null;
    public void PlayGetAwardAnim(int idx, Action afterFunc = null)
    {
        mAwardGotAction = afterFunc;
        var cell = cells[idx];
        var iconT = cell.transform.Find("icon").gameObject;

        var animCell = GameObject.Instantiate(cell, gameObject.transform.parent).gameObject;
        animCell.transform.position = cell.transform.position;

        var curAwardData = awardDatas[idx];

        var id = curAwardData.Split(",")[0].ToInt();
        var num = curAwardData.Split(",")[1].ToInt();

        TileGameCore.AudioController.PlaySound(TileGameCore.AudioController.AudioClips.rewardGet);
        GPUserDataManager.Instance.AddCommonItemData(id, num);
        GameBAData.ExcuteMainStageReward(2, curAwardData);
        PlayGetOtherItemAnim(animCell, () =>
        {
            mAwardGotAction?.Invoke();
            Destroy(animCell);
        });

    }
}
