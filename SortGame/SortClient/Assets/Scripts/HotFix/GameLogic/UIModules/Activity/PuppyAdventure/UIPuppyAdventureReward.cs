using DG.Tweening;
using FishFramework;
using FishXSortPlay;
using XSortGamePlay;
using UnityEngine;
using UnityEngine.UI;

public class UIPuppyAdventureReward : UIPuppyAdventureRewardBase
{
    bool closing;
    bool canClose;
    Tween readyTween;

    protected override void PlayOpenAnim(System.Action onFinish = null)
    {
        animState = UIPanelAnimState.Idle;
        onFinish?.Invoke();
    }

    protected override void OnCreating()
    {
        EnsureClickable(closeBtn);
        EnsureClickable(tapBtn);
        base.OnCreating();
    }

    protected override void OnOpen()
    {
        closing = false;
        canClose = false;
        readyTween?.Kill();
        // In 动画 60fps：第 40 帧开始滚金币，第 130 帧到账（比原来的 70 帧多 1 秒）。未滚完忽略关闭。
        const float InFps = 60f;
        const int CoinCountFromFrame = 40;
        const int CoinCountToFrame = 130;
        float countDelay = CoinCountFromFrame / InFps;
        float countDuration = (CoinCountToFrame - CoinCountFromFrame) / InFps;
        if (coin != null)
            coin.gameObject.SetActive(true);

        if (XSortGameCore.AudioController.AudioClips != null
            && XSortGameCore.AudioController.AudioClips.jungle_reward != null)
            XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.jungle_reward);

        // 固定文案：预制体 I2 Localize（titleTex/shareTex/PaiziText/Label）
        // 动态文案：仅 nextTex（含 {0}）与 coinNum 在代码里设
        int others = Mathf.Max(0, PuppyAdventureDemoState.Data.lastRewardPlayers - 1);
        int coins = PuppyAdventureDemoState.Data.lastRewardCoins;

        LocalizedTMPRefresher.SetText(nextTex, () =>
            PuppyAdventureLoc.Get("JungleQuest_ShareReward", others));

        if (coinNumTex != null)
            coinNumTex.text = "0";
        readyTween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        Sequence countSeq = (Sequence)readyTween;
        countSeq.AppendInterval(countDelay);
        countSeq.AppendCallback(() => PuppyAdventureAnim.CountTo(coinNumTex, 0, coins, countDuration));
        countSeq.AppendInterval(countDuration);
        countSeq.AppendCallback(() => canClose = true);
        PuppyAdventureAnim.SetPaiziCoin(gameObject != null ? gameObject.transform : null,
            PuppyAdventureDemoState.DisplayPrizePool);

        ApplyRewardAvatars();
        FindRewardCoinPanel()?.SetupPanel();

        base.OnOpen();
    }

    const int AvatarPoolCount = 12;
    // 头像布局宽 212、缩放 0.7。这个负间距让画出的头像中心距约 152，和原来 6 人挤在一起时的缝一样。
    const float RewardAvatarSpacing = -59.85f;

    void ApplyRewardAvatars()
    {
        int playerHead = 1;
        if (GPUserDataManager.HasInit() && GPUserDataManager.GPUserData != null)
            playerHead = Mathf.Max(1, GPUserDataManager.GPUserData.userhead);

        Transform player = FindNamed("player");
        if (player is RectTransform playerRt && player.Find("icon") != null)
        {
            SetHeadIcon(playerRt, playerHead);
            ApplyFrame(playerRt, PuppyAdventureUiUtil.GetPlayerFrameId());
        }

        Transform bot = FindNamed("Bot");
        if (bot == null)
            return;

        Transform[] nodes = bot.GetComponentsInChildren<Transform>(true);
        RectTransform[] slots = new RectTransform[nodes.Length];
        int slotCount = 0;
        for (int i = 0; i < nodes.Length; i++)
        {
            Transform node = nodes[i];
            if (node == null || !IsRewardAvatarSlot(node.name) || node.Find("icon") == null)
                continue;
            RectTransform avatar = node as RectTransform;
            if (avatar == null)
                continue;
            slots[slotCount++] = avatar;
        }
        for (int i = 1; i < slotCount; i++)
        {
            RectTransform current = slots[i];
            int index = AvatarSlotIndex(current.name);
            int j = i;
            while (j > 0 && AvatarSlotIndex(slots[j - 1].name) > index)
            {
                slots[j] = slots[j - 1];
                j--;
            }
            slots[j] = current;
        }

        // 中间 player 是自己。底下这排是另外的赢家，和分享文案的 {0} 一致。预制体只有 6 个槽。
        int others = Mathf.Max(0, PuppyAdventureDemoState.Data.lastRewardPlayers - 1);
        int show = Mathf.Min(others, slotCount);

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
        for (int i = 0; i < slotCount; i++)
        {
            RectTransform avatar = slots[i];
            if (i >= show)
            {
                avatar.gameObject.SetActive(false);
                continue;
            }

            avatar.gameObject.SetActive(true);
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
            SetHeadIcon(avatar, headId);
            ApplyFrame(avatar, PuppyAdventureUiUtil.PickRandomFrameId());
        }

        if (slotCount > 0)
            PackRewardAvatarRow(slots[0].parent as RectTransform);
    }

    static void PackRewardAvatarRow(RectTransform row)
    {
        if (row == null)
            return;
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            return;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = RewardAvatarSpacing;
        LayoutRebuilder.ForceRebuildLayoutImmediate(row);
    }

    static bool IsRewardAvatarSlot(string name)
    {
        return name == "AvatarNode" || (name != null && name.StartsWith("AvatarNode ("));
    }

    static int AvatarSlotIndex(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "AvatarNode")
            return 0;
        int open = name.LastIndexOf('(');
        int close = name.LastIndexOf(')');
        if (open < 0 || close <= open)
            return 0;
        int n;
        if (int.TryParse(name.Substring(open + 1, close - open - 1).Trim(), out n))
            return n;
        return 0;
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

    protected override void OnClicked(Button button)
    {
        if (button == tapBtn || button == closeBtn)
            CloseReward();
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        CloseReward();
    }

    void CloseReward()
    {
        if (closing || !canClose)
            return;
        closing = true;
        readyTween?.Kill();
        readyTween = null;

        var data = PuppyAdventureDemoState.Data;
        int stage = data.lastChallengeStage > 0 ? data.lastChallengeStage : PuppyAdventureDemoState.Stage;
        int key = data.lastChallengeTarget > 0 ? data.lastChallengeTarget : PuppyAdventureDemoState.CurWin;
        int coins = PuppyAdventureDemoState.TakePendingRewardCoins();
        // activity status=2 领取奖励；飞币时入账
        GameBAData.ExcutePuppyActivity(
            2,
            stage,
            key,
            PuppyAdventureDemoState.GetActivityUniqueId(),
            coins);

        // 关界面前记下飞起点（面板销毁后 transform 不可用）
        Vector3? flyFromWorld = null;
        if (coin != null)
            flyFromWorld = coin.position;
        else if (coinGlow != null)
            flyFromWorld = coinGlow.position;

        // 先关 Reward，再等一帧用底层货币栏飞币（避免仍命中本界面 Currency Panel）
        CloseSelf(() =>
        {
            DOVirtual.DelayedCall(0.05f, () => PlayCoinFlyAfterClose(coins, flyFromWorld))
                .SetUpdate(true);
        });
    }

    static void PlayCoinFlyAfterClose(int coins, Vector3? flyFromWorld)
    {
        if (coins <= 0)
        {
            PuppyAdventureFlow.FinishReward();
            return;
        }

        CoinPanelView coinPanel = FindSettlementCoinPanel();
        if (coinPanel == null)
        {
            GPUserDataManager.Instance?.AddCoin(coins);
            PuppyAdventureFlow.FinishReward();
            return;
        }

        // 飞起点挂到货币栏 Canvas 下，避免世界坐标空节点导致飞币画不出来
        Transform startFrom = null;
        GameObject startGo = null;
        if (flyFromWorld.HasValue)
        {
            startGo = new GameObject("PuppyRewardCoinFlyStart");
            startGo.transform.SetParent(coinPanel.transform, false);
            startGo.transform.position = flyFromWorld.Value;
            startFrom = startGo.transform;
        }

        if (XSortGameCore.AudioController.AudioClips != null
            && XSortGameCore.AudioController.AudioClips.coinAdd != null)
            XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.coinAdd);

        coinPanel.PlayCoinGainBatched(coins, batch =>
        {
            GPUserDataManager.Instance?.AddCoin(batch);
        });
        coinPanel.PlayCoinFly(() =>
        {
            if (startGo != null)
                Object.Destroy(startGo);
            PuppyAdventureFlow.FinishReward();
        }, startFrom);
    }

    protected override void OnDestroying()
    {
        readyTween?.Kill();
        readyTween = null;
        // 仅异常 Destroy（未走 CloseReward→飞币）：补续流。正常关闭由飞币回调 FinishReward
        if (!closing)
            PuppyAdventureFlow.FinishMainIfNotSuppressed();
        base.OnDestroying();
    }

    CoinPanelView FindRewardCoinPanel()
    {
        if (gameObject == null)
            return null;
        return gameObject.GetComponentInChildren<CoinPanelView>(true);
    }

    /// <summary>优先结算/主界面/局内货币栏，不要用已销毁的 Reward 内嵌栏。</summary>
    static CoinPanelView FindSettlementCoinPanel()
    {
        CoinPanelView panel = FindCoinPanelOnPage<UIComplete>();
        if (panel != null)
            return panel;
        panel = FindCoinPanelOnPage<UIMainMenu>();
        if (panel != null)
            return panel;
        panel = FindCoinPanelOnPage<UIGame>();
        if (panel != null)
            return panel;

        CoinPanelView[] panels = Object.FindObjectsOfType<CoinPanelView>(true);
        if (panels == null)
            return null;
        for (int i = 0; i < panels.Length; i++)
        {
            CoinPanelView p = panels[i];
            if (p == null || p.gameObject == null || !p.gameObject.activeInHierarchy)
                continue;
            if (p.GetComponentInParent<UIPuppyAdventureReward>() != null)
                continue;
            return p;
        }
        return null;
    }

    static CoinPanelView FindCoinPanelOnPage<T>() where T : XSortGameCore.UIPage
    {
        if (!XSortGameCore.UIController.IsDisplayed<T>())
            return null;
        T page = XSortGameCore.UIController.GetPage<T>();
        if (page == null || page.gameObject == null || !page.gameObject.activeInHierarchy)
            return null;
        CoinPanelView panel = page.GetComponentInChildren<CoinPanelView>(true);
        if (panel != null && panel.gameObject.activeInHierarchy)
            return panel;
        return null;
    }

    static void EnsureClickable(TButton btn)
    {
        if (btn == null)
            return;
        Image hit = btn.GetComponent<Image>();
        if (hit == null)
        {
            hit = btn.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            btn.clickDoTween = false;
        }
        hit.raycastTarget = true;
        if (btn.targetGraphic == null)
            btn.targetGraphic = hit;
    }
}
