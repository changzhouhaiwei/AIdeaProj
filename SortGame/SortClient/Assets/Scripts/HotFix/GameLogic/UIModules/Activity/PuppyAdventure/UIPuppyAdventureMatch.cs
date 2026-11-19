using DG.Tweening;
using FishFramework;
using XSortGamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPuppyAdventureMatch : UIPuppyAdventureMatchBase
{
    const float CountDuration = 2.75f;
    const float PopDuration = 0.25f;
    const float PopGap = 0.18f;
    const string TapToContinueName = "tapToContinue";

    // 与 UIPuppyAdventureMatch_Avatars_Anim 里头像 scale 首次非零对齐；资源换成短音后按「每个头像 1 次」播
    static readonly float[] AvatarAppearTimes =
    {
        0.0786f, 0.1910f, 0.2826f, 0.2827f, 0.4358f, 0.5274f, 0.5481f, 0.6357f,
        0.7929f, 0.8745f, 0.9561f, 1.0377f, 1.1194f, 1.2010f, 1.2826f, 1.3642f,
        1.4353f, 1.4660f, 1.6090f, 1.6801f, 1.6882f, 1.8484f, 1.9300f, 1.9698f,
        2.0329f, 2.1799f, 2.1961f, 2.3104f, 2.4247f, 2.5063f, 2.5980f, 2.6369f,
    };

    DG.Tweening.Tween countTween;
    DG.Tweening.Tween stayTween;
    Sequence introSeq;
    Sequence matchSoundSeq;
    TextMeshProUGUI tapToContinueTex;
    Button tapToContinueBtn;
    bool canContinue;
    bool handoffToMain;

    protected override void OnOpen()
    {
        canContinue = false;
        handoffToMain = false;
        LocalizedTMPRefresher.SetText(findTex, () => PuppyAdventureLoc.Get("JungleQuest_MatchingDes"));
        if (prizeNumTex != null)
            prizeNumTex.text = PuppyAdventureDemoState.PrizePool.ToString();
        PuppyAdventureAnim.SetPaiziCoin(gameObject != null ? gameObject.transform : null,
            PuppyAdventureDemoState.PrizePool);
        CacheTapToContinue();
        SetTapToContinueVisible(false);
        PlayMatch();
        base.OnOpen();
    }

    void PlayMatch()
    {
        ApplyHeadIcons();

        Animator headAnim = avatars != null ? avatars.GetComponent<Animator>() : null;
        if (headAnim != null)
        {
            headAnim.Rebind();
            headAnim.Update(0f);
            headAnim.Play("UIPuppyAdventureMatch_Avatars_Anim", 0, 0f);
            headAnim.Update(0f);
            headAnim.speed = 0f;
        }

        Transform title = FindNamed("titleTex");
        Transform chest = FindNamed("PrizeBowl");
        Transform sign = FindNamed("Paizi");
        Transform banner = FindNamed("BgMid");
        Transform prize = prizeNumTex != null ? prizeNumTex.transform : null;
        HideForPop(title);
        HideForPop(chest);
        HideForPop(sign);
        HideForPop(prize);
        HideForPop(banner);

        float titleAt = EntranceRemain();
        float chestAt = titleAt + PopGap;
        float bannerAt = chestAt + PopGap;
        float countAt = bannerAt + PopGap;
        if (playerNumTex != null)
            playerNumTex.text = "0/100";

        introSeq?.Kill();
        introSeq = DOTween.Sequence().SetUpdate(true);
        InsertPop(introSeq, title, titleAt);
        InsertPop(introSeq, chest, chestAt);
        InsertPop(introSeq, sign, chestAt);
        InsertPop(introSeq, prize, chestAt);
        InsertPop(introSeq, banner, bannerAt);

        // 人数滚到 100 再出 tap / 进主界面（不再用固定 stay）
        stayTween?.Kill();
        stayTween = null;
        countTween?.Kill();
        countTween = DOVirtual.DelayedCall(countAt, () =>
        {
            if (headAnim != null)
            {
                headAnim.speed = 1f;
                headAnim.Play("UIPuppyAdventureMatch_Avatars_Anim", 0, 0f);
            }
            ScheduleMatchAvatarSounds();
            DG.Tweening.Tween count = PuppyAdventureAnim.CountTo(
                playerNumTex, 0, 100, CountDuration, n => n + "/100");
            if (count == null)
            {
                OnMatchHoldFinished();
                return;
            }

            countTween = count.OnComplete(OnMatchHoldFinished);
        }).SetUpdate(true);
    }

    void OnMatchHoldFinished()
    {
        if (handoffToMain)
            return;
        // 主城 / 胜利页：人数到 100 后都出 tapToContinue，点击再进 Main
        CacheTapToContinue();
        canContinue = true;
        if (tapToContinueTex == null)
        {
            GoMain();
            return;
        }

        SetTapToContinueVisible(true);
    }

    void CacheTapToContinue()
    {
        if (tapToContinueTex != null)
            return;

        Transform node = FindNamed(TapToContinueName);
        if (node == null)
            return;

        tapToContinueTex = node.GetComponent<TextMeshProUGUI>();
        if (tapToContinueTex == null)
            return;

        tapToContinueBtn = node.GetComponent<Button>();
        if (tapToContinueBtn == null)
            tapToContinueBtn = node.gameObject.AddComponent<Button>();
        tapToContinueBtn.targetGraphic = tapToContinueTex;
        tapToContinueBtn.transition = Selectable.Transition.None;
        tapToContinueBtn.onClick.RemoveListener(OnTapToContinue);
        tapToContinueBtn.onClick.AddListener(OnTapToContinue);
    }

    void SetTapToContinueVisible(bool visible)
    {
        CacheTapToContinue();
        if (tapToContinueTex == null)
            return;

        tapToContinueTex.raycastTarget = visible;
        if (tapToContinueBtn != null)
            tapToContinueBtn.interactable = visible;
        tapToContinueTex.gameObject.SetActive(visible);
    }

    void OnTapToContinue()
    {
        // 人数未到 100：逻辑忽略，不做置灰等显示处理
        if (!canContinue || handoffToMain)
            return;
        stayTween?.Kill();
        stayTween = null;
        countTween?.Kill();
        countTween = null;
        GoMain();
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        // 匹配中不可关；人数到齐后点空白等同 tap
        if (!canContinue || handoffToMain)
            return;
        GoMain();
    }

    void ScheduleMatchAvatarSounds()
    {
        AudioClip clip = XSortGameCore.AudioController.AudioClips != null
            ? XSortGameCore.AudioController.AudioClips.jungle_match
            : null;
        if (clip == null)
            return;

        matchSoundSeq?.Kill();
        matchSoundSeq = DOTween.Sequence().SetUpdate(true);
        for (int i = 0; i < AvatarAppearTimes.Length; i++)
        {
            float at = AvatarAppearTimes[i];
            matchSoundSeq.InsertCallback(at, () => XSortGameCore.AudioController.PlaySound(clip));
        }
    }

    Transform FindNamed(string nodeName)
    {
        if (gameObject == null || string.IsNullOrEmpty(nodeName))
            return null;
        Transform[] all = gameObject.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == nodeName)
                return all[i];
        }
        return null;
    }

    static void HideForPop(Transform target)
    {
        if (target == null)
            return;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        target.localScale = target.localScale * 0.9f;
    }

    static void InsertPop(Sequence seq, Transform target, float at)
    {
        if (seq == null || target == null)
            return;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        Vector3 rest = target.localScale / 0.9f;
        seq.Insert(at, group.DOFade(1f, PopDuration));
        seq.Insert(at, target.DOScale(rest, PopDuration).SetEase(Ease.OutCubic));
    }

    float EntranceRemain()
    {
        Animator anim = gameObject != null ? gameObject.GetComponent<Animator>() : null;
        if (anim == null || anim.runtimeAnimatorController == null)
            return 0f;

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        if (state.length > 0.01f)
        {
            if (!state.loop && state.normalizedTime >= 1f)
                return 0f;
            return Mathf.Max(0f, (1f - Mathf.Min(state.normalizedTime, 1f)) * state.length);
        }

        float length = 0f;
        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].length > length)
                length = clips[i].length;
        }
        return length;
    }

    const int AvatarPoolCount = 12;

    void ApplyHeadIcons()
    {
        if (avatars == null)
            return;

        int playerHead = 1;
        if (GPUserDataManager.HasInit() && GPUserDataManager.GPUserData != null)
            playerHead = Mathf.Max(1, GPUserDataManager.GPUserData.userhead);

        int[] pool = new int[AvatarPoolCount];
        int poolSize = 0;
        for (int id = 1; id <= AvatarPoolCount; id++)
        {
            if (id == playerHead)
                continue;
            pool[poolSize++] = id;
        }
        if (poolSize == 0)
        {
            for (int id = 1; id <= AvatarPoolCount; id++)
                pool[poolSize++] = id;
        }
        Shuffle(pool, poolSize);

        int cursor = 0;
        int lastId = -1;
        for (int i = 0; i < avatars.childCount; i++)
        {
            RectTransform child = avatars.GetChild(i) as RectTransform;
            if (child == null || child.Find("icon") == null)
                continue;
            if (child.name == "Self")
            {
                SetHeadIcon(child, playerHead);
                ApplyFrame(child, PuppyAdventureUiUtil.GetPlayerFrameId());
                continue;
            }

            if (cursor >= poolSize)
            {
                Shuffle(pool, poolSize);
                if (poolSize > 1 && pool[0] == lastId)
                {
                    int swap = pool[0];
                    pool[0] = pool[1];
                    pool[1] = swap;
                }
                cursor = 0;
            }

            int headId = pool[cursor++];
            lastId = headId;
            SetHeadIcon(child, headId);
            ApplyFrame(child, PuppyAdventureUiUtil.PickRandomFrameId());
        }
    }

    static void Shuffle(int[] pool, int count)
    {
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int swap = pool[i];
            pool[i] = pool[j];
            pool[j] = swap;
        }
    }

    void SetHeadIcon(RectTransform avatar, int headId)
    {
        if (avatar == null)
            return;
        Image icon = avatar.Find("icon")?.GetComponent<Image>();
        if (icon == null)
            return;
        Sprite sprite = LoadAtlasSprite("UserInfo", "Avatar_" + headId);
        if (sprite != null)
            icon.sprite = sprite;
    }

    void ApplyFrame(RectTransform avatar, int frameId)
    {
        string pic = PuppyAdventureUiUtil.GetFramePic(frameId);
        Sprite frameSprite = string.IsNullOrEmpty(pic) ? null : LoadAtlasSprite("UserInfo", pic);
        PuppyAdventureUiUtil.ApplyAvatarFrame(avatar, frameSprite);
    }

    void GoMain()
    {
        if (handoffToMain)
            return;
        handoffToMain = true;
        canContinue = false;
        countTween?.Kill();
        countTween = null;
        stayTween?.Kill();
        stayTween = null;
        introSeq?.Kill();
        introSeq = null;
        matchSoundSeq?.Kill();
        matchSoundSeq = null;
        PuppyAdventureDemoState.SetPresentation(PuppyAdventurePresentation.Current);
        // Match 进 Main：显示 Close/Info，不显示 tapBtn（与主界面入口浏览态一致）
        CloseSelf(() => PuppyAdventureFlow.EnterMainFromMatch());
    }

    protected override void OnDestroying()
    {
        if (tapToContinueBtn != null)
            tapToContinueBtn.onClick.RemoveListener(OnTapToContinue);
        tapToContinueBtn = null;
        tapToContinueTex = null;
        countTween?.Kill();
        countTween = null;
        stayTween?.Kill();
        stayTween = null;
        introSeq?.Kill();
        introSeq = null;
        matchSoundSeq?.Kill();
        matchSoundSeq = null;
        StopMatchSounds();
        // 未进 Main 就销毁：补 FinishMain，避免 UnlockNextLevel 丢失
        if (!handoffToMain)
            PuppyAdventureFlow.FinishMainIfNotSuppressed();
        base.OnDestroying();
    }

    static void StopMatchSounds()
    {
        AudioClip clip = XSortGameCore.AudioController.AudioClips != null
            ? XSortGameCore.AudioController.AudioClips.jungle_match
            : null;
        if (clip != null)
            XSortGameCore.AudioController.StopSound(clip);
    }
}
