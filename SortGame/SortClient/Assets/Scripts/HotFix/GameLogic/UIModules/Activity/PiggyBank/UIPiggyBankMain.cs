using System.Collections.Generic;
using Coffee.UIExtensions;
using DG.Tweening;
using FishFramework;
using FishTilePlay;
using Spine;
using Spine.Unity;
using TMPro;
using TileGameCore;
using TileGamePlay;
using UnityEngine;
using UnityEngine.UI;

public class UIPiggyBankMain : UIPanel
{
    const string Lv1Idle = "Lv1_Pig_Idle";
    const string Lv1Tap = "Lv1_Pig_Tap";
    const string Lv1Tap2 = "Lv1_Pig_Tap_2";
    const string Lv1Change = "Lv1_CoinChange";
    const string Lv1ToLv2 = "Lv1toLv2";
    const string Lv2Idle = "Lv2_Pig_Idle";
    const string Lv2Tap = "Lv2_Pig_Tap";
    const string Lv2Tap2 = "Lv2_Pig_Tap_2";
    const string Lv2Change = "Lv2_CoinChange";
    const string Lv2ToLv3 = "Lv2toLv3";
    const string Lv3Idle = "Lv3_Pig_Idle";
    const string Lv3Tap = "Lv3_Pig_Tap";
    const string Lv3Tap2 = "Lv3_Pig_Tap_2";
    const string Lv3Change = "Lv3_CoinChange";
    const string PigCoinEvent = "PigCoin";
    const string Lv1BrokenName = "lv1_PigBroken";
    const string Lv2BrokenName = "lv2_PigBroken";
    const string Lv3BrokenName = "lv3_PigBroken";
    const string FlyCoinName = "FlyCoin";
    const string PigCoin2Name = "PigCoin_2";
    const string CelebrateName = "Celebrate_Normal_new";
    const string BodyGlow1Name = "Body_Glow1";
    const string BodyChangeName = "Body_ChangeEff";
    const string BodyChangeEvent = "BodyChange";
    const string BodyTapName = "Body_TapEff";
    const string TapTopCoinDownName = "TapTopCoinDown";
    const string TapLoopSetName = "TapLoopSet";
    const string TapHintName = "Tap";
    const string TapToContinueName = "tapToContinue";
    const float TapBreathMin = 0.95f;
    const float TapBreathMax = 1.12f;
    const float TapBreathHalfPeriod = 0.8f;
    const float TapLoopIdleDelay = 3f;
    const float TapToContinueCloseDelay = 1f;
    const float TapTextAppearDelay = 0.5f;
    // ponytail: Lv*_Pig_Tap_2 的 XZ 从 1/3 秒开始放大，三档关键帧相同；动画改了要重对。
    const float Tap2SwellDelay = 1f / 3f;
    const string TapSetAnimName = "TapSet_Anim";

    struct PigStep
    {
        public string[] clips;
        public bool loopLast;
        public bool freeze;

        public PigStep(string[] clips, bool loopLast, bool freeze)
        {
            this.clips = clips;
            this.loopLast = loopLast;
            this.freeze = freeze;
        }
    }

    static readonly PigStep[] Stage3Steps =
    {
        new PigStep(new[] { Lv1Tap, Lv1Idle }, true, false),
        new PigStep(new[] { Lv1Tap, Lv1Idle }, true, false),
        new PigStep(new[] { Lv1Tap, Lv1Idle }, true, false),
        new PigStep(new[] { Lv1Tap, Lv1Idle }, true, false),
        new PigStep(new[] { Lv1Tap, Lv1Idle }, true, false),
        new PigStep(new[] { Lv1Tap2, Lv1Change }, false, true),
    };

    static readonly PigStep[] Stage5Steps =
    {
        new PigStep(new[] { Lv1Tap, Lv1Idle }, true, false),
        new PigStep(new[] { Lv1Tap, Lv1Idle }, true, false),
        new PigStep(new[] { Lv1Tap, Lv1ToLv2, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap2, Lv2Change }, false, true),
    };

    static readonly PigStep[] Stage7Steps =
    {
        new PigStep(new[] { Lv1Tap, Lv1Idle }, true, false),
        new PigStep(new[] { Lv1Tap, Lv1Idle }, true, false),
        new PigStep(new[] { Lv1Tap, Lv1Idle }, true, false),
        new PigStep(new[] { Lv1Tap, Lv1ToLv2, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap, Lv2Idle }, true, false),
        new PigStep(new[] { Lv2Tap, Lv2ToLv3, Lv3Idle }, true, false),
        new PigStep(new[] { Lv3Tap, Lv3Idle }, true, false),
        new PigStep(new[] { Lv3Tap, Lv3Idle }, true, false),
        new PigStep(new[] { Lv3Tap2, Lv3Change }, false, true),
    };

    public override PanelLayer Layer { set; get; } = PanelLayer.Tips;
    protected override string PrefabPath =>
        "Assets/CABRes/Prefabs/UIActivity/PiggyBank/UIPiggyBankMain.prefab";

    UIPiggyBankMainView view;
    PigStep[] steps = Stage3Steps;
    int tapIndex;
    bool animating;
    bool playingFreeze;
    bool frozen;
    bool closing;
    bool awaitBodyChange;
    int playToken;
    ParticleSystem lv1Broken;
    ParticleSystem lv2Broken;
    ParticleSystem lv3Broken;
    ParticleSystem flyCoin;
    ParticleSystem pigCoin2;
    GameObject celebrateRoot;
    GameObject bodyGlow1;
    GameObject bodyChangeRoot;
    GameObject bodyTapRoot;
    GameObject tapTopCoinDownRoot;
    readonly List<ParticleSystem> pigCoinCopies = new List<ParticleSystem>();
    readonly List<ParticleSystem> flyCoinCopies = new List<ParticleSystem>();
    readonly List<ParticleSystem> pigCoin2Copies = new List<ParticleSystem>();
    readonly List<GameObject> celebrateCopies = new List<GameObject>();
    readonly List<GameObject> bodyChangeCopies = new List<GameObject>();
    readonly List<GameObject> bodyTapCopies = new List<GameObject>();
    readonly List<GameObject> tapTopCoinDownCopies = new List<GameObject>();
    TextMeshProUGUI rewardLabel;
    int[] rewardSteps;
    int shownCoins;
    int rewardQueued;
    int rewardPlayed;
    bool rewardTweening;
    int rewardTweenToken;
    DG.Tweening.Tween rewardCountTween;
    GameObject tapLoopSet;
    RectTransform tapHint;
    CanvasGroup tapToContinueGroup;
    DG.Tweening.Tween tapBreathTween;
    DG.Tweening.Tween tapToContinueTween;
    DG.Tweening.Tween tapLoopIdleTween;
    DG.Tweening.Tween tapToContinueCloseTween;
    DG.Tweening.Tween piggyBackDelay;
    bool tapLoopHidden;
    bool piggyBackPlaying;
    bool tapToContinueShown;
    bool tapToContinueCloseable;

    protected override void OnCreating()
    {
        view = gameObject != null ? gameObject.GetComponent<UIPiggyBankMainView>() : null;
        BindTap();
        PrepareTapPrompt();
        base.OnCreating();
    }

    protected override void OnOpen()
    {
        closing = false;
        animating = false;
        playingFreeze = false;
        frozen = false;
        awaitBodyChange = false;
        tapIndex = 0;
        playToken++;
        if (view == null && gameObject != null)
            view = gameObject.GetComponent<UIPiggyBankMainView>();

        BindTap();
        if (view != null && view.Piggy != null)
            view.Piggy.SetActive(false);

        steps = StepsFor(ResolveAnimTier());
        PreparePigCoin();
        PrepareFlyCoin();
        PreparePigCoin2();
        PrepareCelebrate();
        PrepareBodyChange();
        PrepareBodyTap();
        PrepareTapTopCoinDown();
        PrepareTapPrompt();
        PrepareBroken();
        PrepareBodyGlow();
        PlayIdle(Lv1Idle);
        BeginRewardPlan();

        base.OnOpen();
    }

    protected override void PlayOpenAnim(System.Action onFinish = null)
    {
        animState = UIPanelAnimState.Idle;
        onFinish?.Invoke();
    }

    protected override void PlayOpenSound()
    {
        PlayPiggySmashOpen();
    }

    protected override void OnDestroyed()
    {
        StopPiggySmashBack();
        base.OnDestroyed();
    }

    void BeginRewardPlan()
    {
        rewardTweenToken++;
        if (rewardCountTween != null)
            rewardCountTween.Kill();
        rewardCountTween = null;
        rewardTweening = false;
        rewardQueued = 0;
        rewardPlayed = 0;
        shownCoins = 0;
        rewardLabel = FindRewardLabel();
        int total = PiggyBankState.DisplayRewardCoins();
        int count = steps != null && steps.Length > 0 ? steps.Length : 1;
        rewardSteps = BuildRewardSteps(total, count);
        if (rewardLabel != null)
            rewardLabel.rectTransform.localScale = Vector3.one;
        SetRewardText(0);
        int token = rewardTweenToken;
        DOVirtual.DelayedCall(0f, () =>
        {
            if (token != rewardTweenToken || shownCoins != 0)
                return;
            SetRewardText(0);
        }).SetUpdate(true);
    }

    void EnqueueRewardStep()
    {
        if (rewardSteps == null || rewardQueued >= rewardSteps.Length)
            return;
        rewardQueued++;
        if (!rewardTweening)
            PlayNextRewardTween();
    }

    void PlayNextRewardTween()
    {
        if (rewardSteps == null || rewardPlayed >= rewardQueued)
        {
            rewardTweening = false;
            return;
        }

        int add = rewardSteps[rewardPlayed];
        rewardPlayed++;
        int to = shownCoins + add;
        int token = rewardTweenToken;
        rewardTweening = true;

        if (rewardLabel != null)
        {
            rewardLabel.rectTransform.DOKill();
            rewardLabel.rectTransform.localScale = Vector3.one;
            rewardLabel.rectTransform.DOScale(1.16f, 0.08f).SetEase(Ease.OutBack).SetUpdate(true)
                .OnComplete(() =>
                {
                    if (rewardLabel != null)
                        rewardLabel.rectTransform.DOScale(1f, 0.1f).SetEase(Ease.InQuad).SetUpdate(true);
                });
        }

        rewardCountTween = DOTween.To(() => shownCoins, value =>
        {
            shownCoins = value;
            SetRewardText(value);
        }, to, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true).OnComplete(() =>
        {
            if (token != rewardTweenToken)
                return;
            shownCoins = to;
            SetRewardText(to);
            PlayNextRewardTween();
        });
    }

    void SetRewardText(int value)
    {
        if (rewardLabel == null)
            rewardLabel = FindRewardLabel();
        if (rewardLabel == null)
            return;
        rewardLabel.text = value.ToString();
        rewardLabel.ForceMeshUpdate();
    }

    TextMeshProUGUI FindRewardLabel()
    {
        if (view != null && view.NumTex != null)
            return view.NumTex;
        if (gameObject == null)
            return null;

        TextMeshProUGUI[] labels = gameObject.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null && labels[i].name == "num")
                return labels[i];
        }

        return null;
    }

    static int[] BuildRewardSteps(int total, int count)
    {
        var parts = new int[Mathf.Max(0, count)];
        if (parts.Length == 0 || total <= 0)
            return parts;
        if (parts.Length == 1)
        {
            parts[0] = total;
            return parts;
        }

        int average = Mathf.Max(1, total / parts.Length);
        int min = Mathf.Max(1, Mathf.RoundToInt(average * 0.55f));
        int max = Mathf.Max(min + 1, Mathf.RoundToInt(average * 1.6f));
        while (min * parts.Length > total && min > 1)
            min--;
        while (max * parts.Length < total)
            max++;

        int remaining = total;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            int left = parts.Length - i;
            int low = Mathf.Max(min, remaining - max * (left - 1));
            int high = Mathf.Min(max, remaining - min * (left - 1));
            if (high < low)
                high = low;
            parts[i] = Random.Range(low, high + 1);
            remaining -= parts[i];
        }

        parts[parts.Length - 1] = remaining;
        int wantSpread = Mathf.Max(2, Mathf.RoundToInt(average * 0.45f));
        for (int n = 0; n < 6 && Spread(parts) < wantSpread; n++)
            Transfer(parts, min, max, Mathf.Max(1, wantSpread / 2));
        return parts;
    }

    static int Spread(int[] parts)
    {
        int min = parts[0];
        int max = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            if (parts[i] < min)
                min = parts[i];
            if (parts[i] > max)
                max = parts[i];
        }

        return max - min;
    }

    static void Transfer(int[] parts, int min, int max, int amount)
    {
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] - amount < min)
                continue;
            for (int j = 0; j < parts.Length; j++)
            {
                if (i == j || parts[j] + amount > max)
                    continue;
                parts[i] -= amount;
                parts[j] += amount;
                return;
            }
        }
    }

    void BindTap()
    {
        if (view == null || view.TapButton == null)
            return;
        view.TapButton.onClick.RemoveListener(OnTap);
        view.TapButton.onClick.AddListener(OnTap);
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        OnTap();
    }

    static int ResolveAnimTier()
    {
        var cfg = PiggyBankState.CurrentCfg;
        if (cfg == null)
            return 3;
        // 配置里 Stage 是第几档（1/2/3），Level 是该档连胜目标（3/5/7）。
        if (cfg.Stage >= 3 || cfg.Level >= 7)
            return 7;
        if (cfg.Stage == 2 || cfg.Level == 5)
            return 5;
        return 3;
    }

    static PigStep[] StepsFor(int stageLevel)
    {
        if (stageLevel >= 7)
            return Stage7Steps;
        if (stageLevel >= 5)
            return Stage5Steps;
        return Stage3Steps;
    }

    void OnTap()
    {
        if (closing)
            return;

        if (frozen)
        {
            if (!tapToContinueCloseable)
                return;
            CloseAndClaim();
            return;
        }

        if (animating && playingFreeze)
            return;

        if (!tapLoopHidden)
            HideTapLoop();
        CancelTapLoopReturn();

        if (tapIndex == 0)
        {
            steps = StepsFor(ResolveAnimTier());
            BeginRewardPlan();
        }

        if (tapIndex >= steps.Length)
            return;

        bool rapid = animating;
        PigStep step = steps[tapIndex];
        if (awaitBodyChange && rapid)
            PlayBodyChange();
        if (StepHasLevelUp(step))
        {
            if (rapid)
                PlayBodyChange();
            else
                awaitBodyChange = true;
        }

        bool finalTap = step.clips != null && step.clips.Length > 0 && IsFinalTap(step.clips[0]);
        if (!finalTap)
        {
            PlayPiggySmashClick();
            PlayPigCoin();
        }
        PlayFlyCoin();
        if (!finalTap)
            PlayTapBurst();
        EnqueueRewardStep();
        PlayStep(step);
        tapIndex++;
        if (!finalTap && !step.freeze)
            ScheduleTapLoopReturn();
    }

    void PlayIdle(string idle)
    {
        SkeletonGraphic spine = view != null ? view.GoldenPig : null;
        if (spine == null)
            return;

        spine.UnscaledTime = true;
        if (!spine.IsValid)
            spine.Initialize(false);
        if (spine.AnimationState == null)
            return;

        TrackEntry entry = spine.AnimationState.SetAnimation(0, idle, true);
        if (entry != null)
            entry.MixDuration = 0f;
    }

    void PlayStep(PigStep step)
    {
        SkeletonGraphic spine = view != null ? view.GoldenPig : null;
        if (spine == null || step.clips == null || step.clips.Length == 0)
        {
            animating = false;
            frozen = step.freeze;
            return;
        }

        spine.UnscaledTime = true;
        if (!spine.IsValid)
            spine.Initialize(false);
        Spine.AnimationState state = spine.AnimationState;
        if (state == null)
        {
            frozen = step.freeze;
            return;
        }

        BeginStep(state, step, 0);
    }

    void BeginStep(Spine.AnimationState state, PigStep step, int startIndex)
    {
        animating = true;
        playingFreeze = step.freeze;
        int token = ++playToken;
        state.Data.DefaultMix = 0f;
        PlayClipAt(state, step, startIndex, token);
    }

    void PlayClipAt(Spine.AnimationState state, PigStep step, int index, int token)
    {
        if (this == null || token != playToken || state == null)
            return;
        if (step.clips == null || index < 0 || index >= step.clips.Length)
        {
            ReleaseStep(token, step.freeze);
            return;
        }

        bool loop = step.loopLast && index == step.clips.Length - 1;
        TrackEntry entry = state.SetAnimation(0, step.clips[index], loop);
        if (entry != null)
        {
            entry.MixDuration = 0f;
            if (IsFinalTap(step.clips[index]))
            {
                BindPigCoinEvent(entry, token);
                SchedulePiggySmashBroken(token);
            }
            if (IsLevelUp(step.clips[index]) && awaitBodyChange)
                BindBodyChangeEvent(entry, token);
            if (step.freeze && IsCoinChange(step.clips[index]))
            {
                PlayBroken();
                HideBodyGlow1();
                entry.Complete += _ =>
                {
                    if (token != playToken)
                        return;
                    ShowTapToContinue();
                };
            }
        }

        if (loop || entry == null)
        {
            ReleaseStep(token, false);
            return;
        }

        float duration = entry.Animation != null ? entry.Animation.Duration : 0.4f;
        bool last = index >= step.clips.Length - 1;
        bool freeze = step.freeze;
        DOVirtual.DelayedCall(Mathf.Max(0.05f, duration), () =>
        {
            if (last)
                ReleaseStep(token, freeze);
            else
                PlayClipAt(state, step, index + 1, token);
        }).SetUpdate(true).SetTarget(this);
    }

    void ReleaseStep(int token, bool freeze)
    {
        if (this == null || token != playToken)
            return;
        animating = false;
        if (freeze)
        {
            frozen = true;
            CancelTapLoopReturn();
            HideTapLoop();
            ShowTapToContinue();
        }
    }

    void PlayPigCoin()
    {
        PlayCoinBurst(view != null ? view.PigCoin : null, pigCoinCopies);
    }

    void PlayFlyCoin()
    {
        if (flyCoin == null)
            flyCoin = FindParticle(FlyCoinName);
        PlayCoinBurst(flyCoin, flyCoinCopies);
    }

    void PlayPigCoin2()
    {
        if (pigCoin2 == null)
            pigCoin2 = FindParticle(PigCoin2Name);
        PlayCoinBurst(pigCoin2, pigCoin2Copies);
    }

    void PlayCelebrate()
    {
        if (celebrateRoot == null)
            celebrateRoot = FindChild(CelebrateName);
        if (celebrateRoot == null)
            return;

        GameObject effect = RentEffect(celebrateRoot, celebrateCopies);
        PlayEffect(effect);
        float life = EffectLifetime(effect);
        DOVirtual.DelayedCall(life, () =>
        {
            if (effect != null)
                HoldEffect(effect);
        }).SetUpdate(true).SetLink(effect);
    }

    GameObject RentEffect(GameObject template, List<GameObject> copies)
    {
        for (int i = 0; i < copies.Count; i++)
        {
            GameObject copy = copies[i];
            if (copy != null && !copy.activeSelf)
                return copy;
        }

        GameObject clone = UnityEngine.Object.Instantiate(template, template.transform.parent);
        clone.name = template.name;
        HoldEffect(clone);
        copies.Add(clone);
        return clone;
    }

    void PrepareCelebrate()
    {
        celebrateRoot = FindChild(CelebrateName);
        if (celebrateRoot != null)
            HoldEffect(celebrateRoot);
        for (int i = 0; i < celebrateCopies.Count; i++)
        {
            if (celebrateCopies[i] != null)
                HoldEffect(celebrateCopies[i]);
        }
    }

    static void HoldEffect(GameObject effect)
    {
        if (effect == null)
            return;

        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;
            ParticleSystem.MainModule main = systems[i].main;
            main.useUnscaledTime = true;
            main.playOnAwake = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        effect.SetActive(false);
    }

    static void PlayEffect(GameObject effect)
    {
        if (effect == null)
            return;
        if (!effect.activeSelf)
            effect.SetActive(true);

        UIParticle uiParticle = effect.GetComponent<UIParticle>();
        if (uiParticle != null)
        {
            uiParticle.RefreshParticles();
            uiParticle.Play();
            return;
        }

        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null)
                systems[i].Play(true);
        }
    }

    static float EffectLifetime(GameObject effect)
    {
        float life = 0.5f;
        if (effect == null)
            return life;

        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;
            ParticleSystem.MainModule main = systems[i].main;
            float end = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
            if (end > life)
                life = end;
        }

        return life;
    }

    void PlayCoinBurst(ParticleSystem template, List<ParticleSystem> copies)
    {
        if (template == null)
            return;

        ParticleSystem coin = RentCoin(template, copies);
        PlayCoinInstance(coin);
        float life = CoinLifetime(coin);
        DOVirtual.DelayedCall(life, () =>
        {
            if (coin != null)
                PrepareCoinInstance(coin);
        }).SetUpdate(true).SetLink(coin.gameObject);
    }

    static ParticleSystem RentCoin(ParticleSystem template, List<ParticleSystem> copies)
    {
        for (int i = 0; i < copies.Count; i++)
        {
            ParticleSystem copy = copies[i];
            if (copy != null && !copy.gameObject.activeSelf)
                return copy;
        }

        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.transform.parent);
        clone.name = template.gameObject.name;
        ParticleSystem created = clone.GetComponent<ParticleSystem>();
        PrepareCoinInstance(created);
        copies.Add(created);
        return created;
    }

    void PreparePigCoin()
    {
        PrepareCoinPool(view != null ? view.PigCoin : null, pigCoinCopies);
    }

    void PrepareFlyCoin()
    {
        flyCoin = FindParticle(FlyCoinName);
        PrepareCoinPool(flyCoin, flyCoinCopies);
    }

    void PreparePigCoin2()
    {
        pigCoin2 = FindParticle(PigCoin2Name);
        PrepareCoinPool(pigCoin2, pigCoin2Copies);
    }

    static void PrepareCoinPool(ParticleSystem template, List<ParticleSystem> copies)
    {
        if (template != null)
            PrepareCoinInstance(template);

        for (int i = 0; i < copies.Count; i++)
        {
            if (copies[i] != null)
                PrepareCoinInstance(copies[i]);
        }
    }

    static void PrepareCoinInstance(ParticleSystem coin)
    {
        if (coin == null)
            return;

        ParticleSystem[] systems = coin.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;
            ParticleSystem.MainModule main = systems[i].main;
            main.useUnscaledTime = true;
            main.playOnAwake = false;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        }

        coin.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        coin.gameObject.SetActive(false);
    }

    static void PlayCoinInstance(ParticleSystem coin)
    {
        if (coin == null)
            return;

        GameObject coinObject = coin.gameObject;
        if (!coinObject.activeSelf)
            coinObject.SetActive(true);

        UIParticle uiParticle = coinObject.GetComponent<UIParticle>();
        if (uiParticle != null)
        {
            uiParticle.Play();
            return;
        }

        coin.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        coin.Play(true);
    }

    static float CoinLifetime(ParticleSystem coin)
    {
        float life = 0.5f;
        ParticleSystem[] systems = coin.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;
            ParticleSystem.MainModule main = systems[i].main;
            float end = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
            if (end > life)
                life = end;
        }

        return life;
    }

    void PrepareTapPrompt()
    {
        tapToContinueShown = false;
        tapToContinueCloseable = false;
        CancelTapToContinueClose();
        CancelTapLoopReturn();
        ShowTapLoop();

        GameObject continueNode = FindChild(TapToContinueName);
        if (continueNode == null)
            continueNode = FindChild("TapToContinue");
        if (continueNode != null)
        {
            tapToContinueGroup = continueNode.GetComponent<CanvasGroup>();
            if (tapToContinueGroup == null)
                tapToContinueGroup = continueNode.AddComponent<CanvasGroup>();
            if (tapToContinueTween != null)
                tapToContinueTween.Kill();
            tapToContinueGroup.alpha = 0f;
            continueNode.SetActive(false);
        }
    }

    void ShowTapLoop()
    {
        if (closing || frozen)
            return;

        bool comingBack = tapLoopHidden;
        tapLoopHidden = false;
        if (tapLoopSet == null)
            tapLoopSet = FindChild(TapLoopSetName);
        if (tapLoopSet == null)
            return;

        tapLoopSet.SetActive(true);
        CanvasGroup loopGroup = tapLoopSet.GetComponent<CanvasGroup>();
        Animator loopAnimator = tapLoopSet.GetComponent<Animator>();
        if (comingBack)
        {
            if (loopAnimator != null)
                loopAnimator.enabled = false;
            if (loopGroup != null)
                loopGroup.alpha = 1f;
            PlayPiggySmashBack();
        }
        else if (!piggyBackPlaying && piggyBackDelay == null)
        {
            if (loopAnimator != null)
            {
                loopAnimator.enabled = true;
                loopAnimator.Play(TapSetAnimName, 0, 0f);
                loopAnimator.Update(0f);
            }
            SchedulePiggySmashBack();
        }
        else if (loopAnimator != null)
            loopAnimator.enabled = true;

        Transform tap = tapLoopSet.transform.Find(TapHintName);
        if (tap == null)
        {
            Transform[] children = tapLoopSet.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == TapHintName)
                {
                    tap = children[i];
                    break;
                }
            }
        }

        tapHint = tap as RectTransform;
        if (tapHint == null)
            return;

        tapHint.DOKill();
        tapHint.localScale = new Vector3(TapBreathMin, TapBreathMin, 1f);
        if (tapBreathTween != null)
            tapBreathTween.Kill();
        tapBreathTween = tapHint
            .DOScale(TapBreathMax, TapBreathHalfPeriod)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(UpdateType.Late, true)
            .SetLink(tapHint.gameObject);
    }

    void ScheduleTapLoopReturn()
    {
        CancelTapLoopReturn();
        tapLoopIdleTween = DOVirtual.DelayedCall(TapLoopIdleDelay, () =>
        {
            tapLoopIdleTween = null;
            ShowTapLoop();
        }).SetUpdate(true).SetLink(gameObject);
    }

    void CancelTapLoopReturn()
    {
        if (tapLoopIdleTween != null && tapLoopIdleTween.IsActive())
            tapLoopIdleTween.Kill();
        tapLoopIdleTween = null;
    }

    void HideTapLoop()
    {
        StopPiggySmashBack();
        tapLoopHidden = true;
        if (tapBreathTween != null)
            tapBreathTween.Kill();
        tapBreathTween = null;
        if (tapHint != null)
            tapHint.localScale = Vector3.one;

        if (gameObject == null)
            return;

        Transform[] nodes = gameObject.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null || nodes[i].name != TapLoopSetName)
                continue;

            GameObject node = nodes[i].gameObject;
            Animator loopAnimator = node.GetComponent<Animator>();
            if (loopAnimator != null)
                loopAnimator.enabled = false;
            CanvasGroup loopGroup = node.GetComponent<CanvasGroup>();
            if (loopGroup != null)
                loopGroup.alpha = 0f;
            node.SetActive(false);
            tapLoopSet = node;
        }
    }

    void ShowTapToContinue()
    {
        if (tapToContinueShown)
            return;
        if (tapToContinueGroup == null)
        {
            GameObject continueNode = FindChild(TapToContinueName);
            if (continueNode == null)
                continueNode = FindChild("TapToContinue");
            if (continueNode == null)
            {
                tapToContinueCloseable = true;
                return;
            }
            tapToContinueGroup = continueNode.GetComponent<CanvasGroup>();
            if (tapToContinueGroup == null)
                tapToContinueGroup = continueNode.AddComponent<CanvasGroup>();
        }

        tapToContinueShown = true;
        GameObject node = tapToContinueGroup.gameObject;
        node.SetActive(true);
        CanvasRenderer renderer = node.GetComponent<CanvasRenderer>();
        if (renderer != null)
            renderer.cullTransparentMesh = false;
        if (tapToContinueTween != null)
            tapToContinueTween.Kill();
        tapToContinueGroup.alpha = 0f;
        tapToContinueTween = tapToContinueGroup
            .DOFade(1f, 0.35f)
            .SetUpdate(UpdateType.Late, true)
            .SetLink(node);
        ScheduleTapToContinueClose();
    }

    void ScheduleTapToContinueClose()
    {
        CancelTapToContinueClose();
        tapToContinueCloseable = false;
        tapToContinueCloseTween = DOVirtual.DelayedCall(TapToContinueCloseDelay, () =>
        {
            tapToContinueCloseTween = null;
            tapToContinueCloseable = true;
        }).SetUpdate(true).SetLink(gameObject);
    }

    void CancelTapToContinueClose()
    {
        if (tapToContinueCloseTween == null)
            return;
        tapToContinueCloseTween.Kill();
        tapToContinueCloseTween = null;
    }

    void PrepareBodyGlow()
    {
        bodyGlow1 = FindChild(BodyGlow1Name);
        if (bodyGlow1 != null)
            bodyGlow1.SetActive(true);
    }

    void HideBodyGlow1()
    {
        if (bodyGlow1 == null)
            bodyGlow1 = FindChild(BodyGlow1Name);
        if (bodyGlow1 != null)
            bodyGlow1.SetActive(false);
    }

    GameObject FindChild(string nodeName)
    {
        if (gameObject == null)
            return null;

        Transform[] nodes = gameObject.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].name == nodeName)
                return nodes[i].gameObject;
        }

        return null;
    }

    static bool IsFinalTap(string clip)
    {
        return clip == Lv1Tap2 || clip == Lv2Tap2 || clip == Lv3Tap2;
    }

    static bool IsLevelUp(string clip)
    {
        return clip == Lv1ToLv2 || clip == Lv2ToLv3;
    }

    static bool StepHasLevelUp(PigStep step)
    {
        if (step.clips == null)
            return false;
        for (int i = 0; i < step.clips.Length; i++)
        {
            if (IsLevelUp(step.clips[i]))
                return true;
        }

        return false;
    }

    void BindBodyChangeEvent(TrackEntry entry, int token)
    {
        bool played = false;
        entry.Event += (_, spineEvent) =>
        {
            if (played || this == null || token != playToken || !awaitBodyChange)
                return;
            if (spineEvent?.Data == null || spineEvent.Data.Name != BodyChangeEvent)
                return;
            played = true;
            PlayBodyChange();
        };
    }

    void PrepareBodyChange()
    {
        bodyChangeRoot = FindChild(BodyChangeName);
        if (bodyChangeRoot != null)
        {
            ShowEffectScale(bodyChangeRoot);
            HoldEffect(bodyChangeRoot);
        }

        for (int i = 0; i < bodyChangeCopies.Count; i++)
        {
            if (bodyChangeCopies[i] != null)
                HoldEffect(bodyChangeCopies[i]);
        }
    }

    void PlayBodyChange()
    {
        awaitBodyChange = false;
        if (bodyChangeRoot == null)
            bodyChangeRoot = FindChild(BodyChangeName);
        if (bodyChangeRoot == null)
            return;

        ShowEffectScale(bodyChangeRoot);
        GameObject effect = RentEffect(bodyChangeRoot, bodyChangeCopies);
        ShowEffectScale(effect);
        PlayEffect(effect);
        float life = EffectLifetime(effect);
        DOVirtual.DelayedCall(life, () =>
        {
            if (effect != null)
                HoldEffect(effect);
        }).SetUpdate(true).SetLink(effect);
    }

    void PlayTapBurst()
    {
        PlayBodyTap();
        PlayTapTopCoinDown();
    }

    void PrepareBodyTap()
    {
        bodyTapRoot = FindChild(BodyTapName);
        if (bodyTapRoot != null)
        {
            ShowEffectScale(bodyTapRoot);
            HoldEffect(bodyTapRoot);
        }

        for (int i = 0; i < bodyTapCopies.Count; i++)
        {
            if (bodyTapCopies[i] != null)
                HoldEffect(bodyTapCopies[i]);
        }
    }

    void PlayBodyTap()
    {
        if (bodyTapRoot == null)
            bodyTapRoot = FindChild(BodyTapName);
        if (bodyTapRoot == null)
            return;

        ShowEffectScale(bodyTapRoot);
        GameObject effect = RentEffect(bodyTapRoot, bodyTapCopies);
        ShowEffectScale(effect);
        PlayEffect(effect);
        float life = EffectLifetime(effect);
        DOVirtual.DelayedCall(life, () =>
        {
            if (effect != null)
                HoldEffect(effect);
        }).SetUpdate(true).SetLink(effect);
    }

    void PrepareTapTopCoinDown()
    {
        PrepareSpawnedEffect(TapTopCoinDownName, ref tapTopCoinDownRoot, tapTopCoinDownCopies);
    }

    void PlayTapTopCoinDown()
    {
        PlaySpawnedEffect(TapTopCoinDownName, ref tapTopCoinDownRoot, tapTopCoinDownCopies);
    }

    void PrepareSpawnedEffect(string nodeName, ref GameObject root, List<GameObject> copies)
    {
        root = FindChild(nodeName);
        if (root != null)
        {
            ShowEffectScale(root);
            HoldEffect(root);
        }

        for (int i = 0; i < copies.Count; i++)
        {
            if (copies[i] != null)
                HoldEffect(copies[i]);
        }
    }

    void PlaySpawnedEffect(string nodeName, ref GameObject root, List<GameObject> copies)
    {
        if (root == null)
            root = FindChild(nodeName);
        if (root == null)
            return;

        ShowEffectScale(root);
        GameObject effect = RentEffect(root, copies);
        ShowEffectScale(effect);
        PlayEffect(effect);
        float life = EffectLifetime(effect);
        DOVirtual.DelayedCall(life, () =>
        {
            if (effect != null)
                HoldEffect(effect);
        }).SetUpdate(true).SetLink(effect);
    }

    static void ShowEffectScale(GameObject effect)
    {
        if (effect == null)
            return;
        if (effect.transform.localScale.sqrMagnitude < 0.0001f)
            effect.transform.localScale = Vector3.one;
    }

    void BindPigCoinEvent(TrackEntry entry, int token)
    {
        bool played = false;
        entry.Event += (_, spineEvent) =>
        {
            if (played || this == null || token != playToken)
                return;
            if (spineEvent?.Data == null || spineEvent.Data.Name != PigCoinEvent)
                return;
            played = true;
            PlayPigCoin2();
            PlayCelebrate();
            PlayTapBurst();
        };
    }

    static bool IsCoinChange(string clip)
    {
        return clip == Lv1Change || clip == Lv2Change || clip == Lv3Change;
    }

    void PrepareBroken()
    {
        lv1Broken = FindParticle(Lv1BrokenName);
        lv2Broken = FindParticle(Lv2BrokenName);
        lv3Broken = FindParticle(Lv3BrokenName);
        HoldParticle(lv1Broken);
        HoldParticle(lv2Broken);
        HoldParticle(lv3Broken);
    }

    void PlayBroken()
    {
        StopPiggySmashBack();
        if (lv1Broken == null && lv2Broken == null && lv3Broken == null)
            PrepareBroken();

        int count = steps == Stage7Steps ? 3 : steps == Stage5Steps ? 2 : 1;
        if (count >= 1)
            PlayParticle(lv1Broken);
        if (count >= 2)
            PlayParticle(lv2Broken);
        if (count >= 3)
            PlayParticle(lv3Broken);
    }

    void SchedulePiggySmashBroken(int token)
    {
        DOVirtual.DelayedCall(Tap2SwellDelay, () =>
        {
            if (this == null || token != playToken || closing)
                return;
            StopPiggySmashBack();
            PlayPiggySmashBroken();
        }).SetUpdate(true).SetLink(gameObject);
    }

    void SchedulePiggySmashBack()
    {
        if (closing || frozen || tapLoopHidden)
            return;
        if (piggyBackDelay != null && piggyBackDelay.IsActive())
            piggyBackDelay.Kill();
        piggyBackDelay = DOVirtual.DelayedCall(TapTextAppearDelay, () =>
        {
            piggyBackDelay = null;
            if (this == null || closing || frozen || tapLoopHidden)
                return;
            PlayPiggySmashBack();
        }).SetLink(gameObject);
    }

    void StopPiggySmashBack()
    {
        piggyBackPlaying = false;
        if (piggyBackDelay != null && piggyBackDelay.IsActive())
            piggyBackDelay.Kill();
        piggyBackDelay = null;
        AudioClips clips = AudioController.AudioClips;
        if (clips != null && clips.piggySmashBack != null)
            AudioController.StopSound(clips.piggySmashBack);
    }

    void PlayPiggySmashBack()
    {
        if (closing || frozen || tapLoopHidden)
            return;
        piggyBackPlaying = true;
        AudioClips clips = AudioController.AudioClips;
        if (clips != null && clips.piggySmashBack != null)
            AudioController.PlayLoopSound(clips.piggySmashBack, 0.5f);
    }

    static void PlayPiggySmashOpen()
    {
        AudioClips clips = AudioController.AudioClips;
        if (clips != null && clips.piggySmashOpen != null)
            AudioController.PlaySound(clips.piggySmashOpen);
    }

    static void PlayPiggySmashClick()
    {
        AudioClips clips = AudioController.AudioClips;
        if (clips != null && clips.piggySmashClick != null)
            AudioController.PlaySound(clips.piggySmashClick);
    }

    static void PlayPiggySmashBroken()
    {
        AudioClips clips = AudioController.AudioClips;
        if (clips != null && clips.piggySmashBroken != null)
            AudioController.PlaySound(clips.piggySmashBroken);
    }

    ParticleSystem FindParticle(string nodeName)
    {
        if (gameObject == null)
            return null;

        Transform[] nodes = gameObject.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].name == nodeName)
                return nodes[i].GetComponent<ParticleSystem>();
        }

        return null;
    }

    static void HoldParticle(ParticleSystem particle)
    {
        if (particle == null)
            return;

        ParticleSystem[] systems = particle.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null)
                continue;
            ParticleSystem.MainModule main = systems[i].main;
            main.useUnscaledTime = true;
            main.playOnAwake = false;
        }

        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particle.gameObject.SetActive(false);
    }

    static void PlayParticle(ParticleSystem particle)
    {
        if (particle == null)
            return;

        GameObject particleObject = particle.gameObject;
        if (!particleObject.activeSelf)
            particleObject.SetActive(true);
        particleObject.transform.SetAsLastSibling();

        UIParticle uiParticle = particleObject.GetComponent<UIParticle>();
        if (uiParticle == null)
        {
            particle.Play(true);
            return;
        }

        uiParticle.RefreshParticles();
        var systems = uiParticle.particles;
        for (int i = 0; i < systems.Count; i++)
        {
            ParticleSystem system = systems[i];
            if (system == null)
                continue;
            ParticleSystem.MainModule main = system.main;
            main.useUnscaledTime = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        }

        uiParticle.Play();
    }

    void CloseAndClaim()
    {
        if (closing)
            return;
        closing = true;

        int coins = PiggyBankState.Claim();
        Vector3? flyFrom = null;
        if (view != null && view.CoinFlyFrom != null)
            flyFrom = view.CoinFlyFrom.position;
        else if (view != null && view.NumTex != null)
            flyFrom = view.NumTex.transform.position;

        CloseSelf(() =>
        {
            DOVirtual.DelayedCall(0.05f, () => PlayCoinFlyAfterClose(coins, flyFrom))
                .SetUpdate(true);
        });
    }

    static void PlayCoinFlyAfterClose(int coins, Vector3? flyFromWorld)
    {
        if (coins <= 0)
        {
            PiggyBankFlow.FinishClaim();
            return;
        }

        CoinPanelView coinPanel = FindSettlementCoinPanel();
        if (coinPanel == null)
        {
            GPUserDataManager.Instance?.AddCoin(coins);
            PiggyBankFlow.FinishClaim();
            return;
        }

        Transform startFrom = null;
        GameObject startGo = null;
        if (flyFromWorld.HasValue)
        {
            startGo = new GameObject("PiggyRewardCoinFlyStart");
            startGo.transform.SetParent(coinPanel.transform, false);
            startGo.transform.position = flyFromWorld.Value;
            startFrom = startGo.transform;
        }

        if (AudioController.AudioClips != null && AudioController.AudioClips.coinAdd != null)
            AudioController.PlaySound(AudioController.AudioClips.coinAdd);

        coinPanel.PlayCoinGainBatched(coins, batch =>
        {
            GPUserDataManager.Instance?.AddCoin(batch);
        });
        coinPanel.PlayCoinFly(() =>
        {
            if (startGo != null)
                Object.Destroy(startGo);
            PiggyBankFlow.FinishClaim();
        }, startFrom);
    }

    static CoinPanelView FindSettlementCoinPanel()
    {
        CoinPanelView panel = FindCoinPanelOnPage<UIComplete>();
        if (panel != null)
            return panel;
        panel = FindCoinPanelOnPage<UIMainMenu>();
        if (panel != null)
            return panel;

        CoinPanelView[] panels = Object.FindObjectsOfType<CoinPanelView>(true);
        if (panels == null)
            return null;
        for (int i = 0; i < panels.Length; i++)
        {
            CoinPanelView p = panels[i];
            if (p == null || !p.gameObject.activeInHierarchy)
                continue;
            return p;
        }
        return null;
    }

    static CoinPanelView FindCoinPanelOnPage<T>() where T : UIPage
    {
        if (!UIController.IsDisplayed<T>())
            return null;
        T page = UIController.GetPage<T>();
        if (page == null || page.gameObject == null || !page.gameObject.activeInHierarchy)
            return null;
        CoinPanelView panel = page.GetComponentInChildren<CoinPanelView>(true);
        if (panel != null && panel.gameObject.activeInHierarchy)
            return panel;
        return null;
    }

    protected override void OnDestroying()
    {
        CancelTapLoopReturn();
        CancelTapToContinueClose();
        if (view != null && view.TapButton != null)
            view.TapButton.onClick.RemoveListener(OnTap);
        if (!closing)
            PiggyBankFlow.FinishClaim();
        base.OnDestroying();
    }
}
