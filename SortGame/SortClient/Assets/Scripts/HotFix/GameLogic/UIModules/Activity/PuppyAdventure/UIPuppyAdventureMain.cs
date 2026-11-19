using System.Collections.Generic;
using DG.Tweening;
using FishFramework;
using Spine.Unity;
using XSortGamePlay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class UIPuppyAdventureMain : UIPuppyAdventureMainBase
{
    const int RobotSlots = PuppyAdventureDemoState.VisualRobotSlots;
    const float JumpPower = 68f;
    const float JumpStaggerMin = 0.018f;
    const float JumpStaggerMax = 0.1f;
    const float HideAfterFall = 0.28f;
    const float PillarDownAfterJump = 0.2f;
    const float FallStagger = 0.05f;
    const float FallerJumpGap = 0.12f;

    const string BgNodeName = "BG";
    const string BgSpineName = "SkeletonGraphic (Pool_BG)";
    const string ClockSpineName = "SkeletonGraphic (Pool_Clock)";
    const string PillarSpineName = "SkeletonGraphic (Pool_ZZ)";
    const string SplashSpineKey = "Pool_ChrFall";
    const string StandLeftName = "StandLeft";
    const string StandRightName = "StandRight";

    bool waitingTap;
    bool finishedWin;
    bool dismissing;
    bool handoffToReward;
    DG.Tweening.Tween timerTween;
    DG.Tweening.Tween hudTween;
    DG.Tweening.Tween rewardDelay;
    DG.Tweening.Tween fallHideTween;
    DG.Tweening.Tween jumpTween;
    DG.Tweening.Tween pillarDownTween;
    DG.Tweening.Tween tapFade;
    float lastJumpWatchdog = 0.8f;
    PuppyAdventureHotkeys hotkeys;

    RectTransform playersRoot;
    Transform playersHomeParent;
    readonly List<RectTransform> robots = new();
    readonly List<bool> robotAlive = new();
    readonly List<Vector2> robotHomePos = new();
    readonly List<Vector3> robotHomeScale = new();
    readonly List<Transform> playersChildOrder = new();
    Vector2 playerHomePos;
    Vector3 playerHomeScale = Vector3.one;
    Transform playerSplashRoot;
    Transform playerSplashHome;
    int playerSplashSibling = -1;
    Vector2 playerSplashHomePos;

    protected override void OnOpen()
    {
        waitingTap = false;
        finishedWin = PuppyAdventureDemoState.Presentation == PuppyAdventurePresentation.Complete;
        dismissing = false;
        handoffToReward = false;
        SetFail(false);
        LayoutTrail();
        EnsureAvatars();
        StartIdle();

        if (advanceBtn != null)
            advanceBtn.gameObject.SetActive(false);
        if (failBtn != null)
            failBtn.gameObject.SetActive(false);
        // prefab 默认全屏 tap 可点；结算演出结束前必须关掉，否则点空白会关界面并丢掉 continuation
        if (tapBtn != null)
            tapBtn.gameObject.SetActive(false);
        bool showHomeChrome = PuppyAdventureFlow.BrowseFromHome;
        if (infoBtn != null)
            infoBtn.gameObject.SetActive(showHomeChrome);
        if (closeBtn != null)
            closeBtn.gameObject.SetActive(showHomeChrome);

        switch (PuppyAdventureDemoState.Presentation)
        {
            case PuppyAdventurePresentation.Advance:
                PlayAdvanceVisual(false);
                break;
            case PuppyAdventurePresentation.Complete:
                PlayAdvanceVisual(true);
                break;
            case PuppyAdventurePresentation.Fail:
                SetFail(true);
                RefreshHud(PuppyAdventureDemoState.Data.lastFailureWin, PuppyAdventureDemoState.AnimFromPlayers);
                SyncAlive(PuppyAdventureDemoState.VisualRobotCount(PuppyAdventureDemoState.AnimFromPlayers));
                SnapToProgress(PuppyAdventureDemoState.Data.lastFailureWin);
                PlayFail();
                break;
            default:
                PuppyAdventureDemoState.ResolveHomeProgress(out _, out int homeWin);
                RefreshHud(homeWin, PuppyAdventureDemoState.RemainPlayers);
                SyncAlive(PuppyAdventureDemoState.VisualRobotCount(PuppyAdventureDemoState.RemainPlayers));
                SnapToProgress(homeWin);
                // 结算推进演出才出 tap；Match/主界面入口进 Main 显示 Close/Info。
                if (!PuppyAdventureFlow.BrowseFromHome)
                    ShowTap();
                break;
        }

        EnsureHudOnTop();
        timerTween = DOVirtual.DelayedCall(1f, RefreshTimer)
            .SetLoops(-1)
            .SetUpdate(true);
        BindHotkeys();
        base.OnOpen();
    }

    void StartIdle()
    {
        PuppyAdventureAnim.Parallax(bgMid, 24f, 8f);
        PuppyAdventureAnim.Parallax(bgNear, -18f, 6f);
        PlayBgLoop();
        PlayClockLoop();
        PlayPillarIdlesStaggered();
        HideTrailSplashes();
    }

    void PlayBgLoop()
    {
        PuppyAdventureAnim.PlayLoop(FindBgSpine(), PuppyAdventureAnim.BgLoopAnim);
    }

    void PlayClockLoop()
    {
        PuppyAdventureAnim.PlayLoop(FindClockSpine(), PuppyAdventureAnim.ClockLoopAnim);
    }

    void PlayPillarIdlesStaggered()
    {
        List<RectTransform> nodes = CollectActiveTrailNodes();
        int count = Mathf.Max(1, nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            SkeletonGraphic spine = FindPillarSpine(nodes[i]);
            if (spine == null)
                continue;
            PuppyAdventureAnim.PlayLoop(spine, PuppyAdventureAnim.PillarIdleAnim);
            var entry = spine.AnimationState != null ? spine.AnimationState.GetCurrent(0) : null;
            if (entry?.Animation == null || entry.Animation.Duration <= 0.001f)
                continue;
            entry.TrackTime = entry.Animation.Duration * i / count;
            spine.Update(0f);
        }
    }

    void SyncPillarSpines(int progress)
    {
        List<RectTransform> nodes = CollectActiveTrailNodes();
        if (nodes.Count == 0)
            return;
        int occupied = PuppyAdventureDemoState.TrailNodeIndex(
            progress, PuppyAdventureDemoState.DisplayWinTarget, nodes.Count);
        occupied = Mathf.Clamp(occupied, 0, nodes.Count - 1);
        for (int i = 0; i < nodes.Count; i++)
        {
            SkeletonGraphic spine = FindPillarSpine(nodes[i]);
            if (spine == null)
                continue;
            if (i < occupied)
            {
                if (PuppyAdventureAnim.IsPlaying(spine, PuppyAdventureAnim.PillarDownAnim))
                    continue;
                PuppyAdventureAnim.HoldLastFrame(spine, PuppyAdventureAnim.PillarDownAnim);
                continue;
            }

            if (!PuppyAdventureAnim.IsPlaying(spine, PuppyAdventureAnim.PillarIdleAnim))
            {
                PuppyAdventureAnim.PlayLoop(
                    spine,
                    PuppyAdventureAnim.PillarIdleAnim,
                    i);
            }
        }
    }

    void PlayPillarDown(Transform trailNode)
    {
        SkeletonGraphic spine = FindPillarSpine(trailNode);
        if (spine == null || !spine.isActiveAndEnabled)
            return;
        PuppyAdventureAnim.PlayOnceAndHold(spine, PuppyAdventureAnim.PillarDownAnim);
    }

    RectTransform FindOccupiedTrailNode()
    {
        if (playersRoot == null)
            return null;
        List<RectTransform> nodes = CollectActiveTrailNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            RectTransform node = nodes[i];
            if (node != null && playersRoot.IsChildOf(node))
                return node;
        }
        return FindTrailNodeFromSlot(playersRoot.parent);
    }

    SkeletonGraphic FindBgSpine()
    {
        Transform anim = trailNodes != null && trailNodes.parent != null
            ? trailNodes.parent.parent
            : (gameObject != null ? gameObject.transform : null);
        Transform bg = anim != null ? anim.Find(BgNodeName) : null;
        if (bg == null && gameObject != null)
            bg = FindNamedRecursive(gameObject.transform, BgNodeName);
        return FindNamedSpine(bg, BgSpineName);
    }

    SkeletonGraphic FindClockSpine()
    {
        Transform named = gameObject != null
            ? FindNamedRecursive(gameObject.transform, ClockSpineName)
            : null;
        if (named != null)
            return named.GetComponent<SkeletonGraphic>();
        if (timerTex != null)
            return FindNamedSpine(timerTex.transform.parent, ClockSpineName);
        return null;
    }

    static SkeletonGraphic FindPillarSpine(Transform node)
    {
        return FindNamedSpine(node, PillarSpineName);
    }

    static SkeletonGraphic FindNamedSpine(Transform root, string childName)
    {
        if (root == null)
            return null;
        Transform named = root.Find(childName);
        if (named != null)
        {
            SkeletonGraphic namedSpine = named.GetComponent<SkeletonGraphic>();
            if (namedSpine != null)
                return namedSpine;
        }

        SkeletonGraphic[] spines = root.GetComponentsInChildren<SkeletonGraphic>(true);
        for (int i = 0; i < spines.Length; i++)
        {
            SkeletonGraphic spine = spines[i];
            if (spine != null && spine.gameObject.name.IndexOf("Pool_ZZ", System.StringComparison.Ordinal) >= 0)
                return spine;
        }
        return spines.Length > 0 ? spines[0] : null;
    }

    static readonly string[] TrailPathNames =
    {
        "start",
        "N1 (1)",
        "N1 (2)",
        "N1 (3)",
        "N1 (4)",
        "N1 (5)",
        "N1 (6)",
        "end",
    };

    void LayoutTrail()
    {
        if (trailNodes == null)
            return;
        bool fiveStep = PuppyAdventureDemoState.DisplayWinTarget <= 5;
        for (int i = 0; i < TrailPathNames.Length; i++)
        {
            Transform child = trailNodes.Find(TrailPathNames[i]);
            if (child == null)
                continue;
            string name = child.name;
            bool hide = fiveStep && (name == "N1 (2)" || name == "N1 (5)");
            child.gameObject.SetActive(!hide);
        }
    }

    void EnsureAvatars()
    {
        robots.Clear();
        robotAlive.Clear();
        robotHomePos.Clear();
        robotHomeScale.Clear();
        playersChildOrder.Clear();
        RectTransform boundPlayer = playerAvatar;
        playerAvatar = null;

        playersRoot = FindPlayersRoot();
        if (playersRoot == null)
            return;

        if (trailNodes != null && trailNodes.parent != null)
            playersHomeParent = trailNodes.parent;

        RectTransform template = null;
        for (int i = 0; i < playersRoot.childCount; i++)
        {
            RectTransform child = playersRoot.GetChild(i) as RectTransform;
            if (child == null)
                continue;
            if (IsSelfAvatarName(child.name))
            {
                playerAvatar = child;
                TrimAvatarExtras(child);
                continue;
            }

            if (child.name.StartsWith("robot"))
            {
                robots.Add(child);
                template = child;
                TrimAvatarExtras(child);
            }
        }

        if (playerAvatar == null && boundPlayer != null && boundPlayer.parent == playersRoot)
            playerAvatar = boundPlayer;

        if (playerAvatar == null)
        {
            for (int i = 0; i < playersRoot.childCount; i++)
            {
                RectTransform child = playersRoot.GetChild(i) as RectTransform;
                if (child == null || child.name.StartsWith("robot"))
                    continue;
                playerAvatar = child;
                break;
            }
        }

        if (template == null && robots.Count > 0)
            template = robots[0];
        if (template == null && playerAvatar != null)
            template = playerAvatar;

        while (robots.Count < RobotSlots && template != null)
        {
            RectTransform clone = Object.Instantiate(template, playersRoot, false);
            clone.name = "robot (" + robots.Count + ")";
            clone.gameObject.SetActive(true);
            TrimAvatarExtras(clone);
            robots.Add(clone);
        }

        while (robots.Count > RobotSlots)
        {
            RectTransform extra = robots[robots.Count - 1];
            robots.RemoveAt(robots.Count - 1);
            if (extra != null)
                Object.Destroy(extra.gameObject);
        }

        ApplyAvatarIcons();
        PlaceSelfAvatar();
        PackStandHomes(CollectStandingRobots(), ActiveTrailNode(0), true);
        SortCrowdDrawOrder();

        for (int i = 0; i < robots.Count; i++)
        {
            RectTransform robot = robots[i];
            robotAlive.Add(true);
            if (robotHomePos.Count <= i)
                robotHomePos.Add(robot != null ? robot.anchoredPosition : Vector2.zero);
            robotHomeScale.Add(robot != null ? robot.localScale : Vector3.one);
        }

        for (int i = 0; i < playersRoot.childCount; i++)
            playersChildOrder.Add(playersRoot.GetChild(i));
    }

    List<RectTransform> CollectStandingRobots()
    {
        var standers = new List<RectTransform>();
        for (int i = 0; i < robots.Count; i++)
        {
            RectTransform robot = robots[i];
            if (robot == null || robot == playerAvatar)
                continue;
            if (i < robotAlive.Count && !robotAlive[i])
                continue;
            if (!robot.gameObject.activeSelf && robotAlive.Count > 0)
                continue;
            standers.Add(robot);
        }
        return standers;
    }

    float PadPack(RectTransform node)
    {
        List<RectTransform> nodes = CollectActiveTrailNodes();
        int idx = 0;
        if (node != null)
        {
            idx = nodes.IndexOf(node);
            if (idx < 0)
                idx = 0;
        }
        float t = nodes.Count <= 1 ? 0f : idx / (float)(nodes.Count - 1);
        float scale = 1f;
        if (node != null)
            scale = Mathf.Clamp(Mathf.Abs(node.localScale.x), 0.5f, 1.15f);
        return Mathf.Clamp01(Mathf.Lerp(1f, 0.46f, t) * scale);
    }

    static float StartCrowdY(int column, int row)
    {
        float[] wave = { -10f, 8f, -4f, 12f, 1f, -8f, 6f, -2f };
        return wave[(column * 3 + row) % wave.Length];
    }

    static void FillStartCrowdRows(int n, List<int> rows)
    {
        rows.Clear();
        if (n <= 0)
            return;
        if (n <= 3)
        {
            rows.Add(n);
            return;
        }

        int rowCount = n >= 13 ? 4 : 3;
        int remain = n;
        for (int r = 0; r < rowCount; r++)
        {
            int left = rowCount - r;
            int take = (remain + left - 1) / left;
            rows.Add(take);
            remain -= take;
        }
    }

    static void FillCrowdRows(int n, List<int> rows)
    {
        rows.Clear();
        if (n <= 0)
            return;
        if (n <= 2)
        {
            rows.Add(n);
            return;
        }

        int rowCount = n >= 11 ? 3 : 2;
        int remain = n;
        for (int r = 0; r < rowCount; r++)
        {
            int left = rowCount - r;
            int take = (remain + left - 1) / left;
            rows.Add(take);
            remain -= take;
        }
    }

    void PackStandHomes(List<RectTransform> standers, RectTransform node, bool applyNow)
    {
        if (standers == null || standers.Count == 0)
            return;

        Vector2 origin = playerHomePos;
        if (origin.y < 8f)
            origin = new Vector2(origin.x, 22f);

        float pack = PadPack(node);
        bool start = IsStartTrailNode(node);
        int n = standers.Count;
        var rows = new List<int>(4);
        if (start)
            FillStartCrowdRows(n, rows);
        else
            FillCrowdRows(n, rows);

        float spacing = start ? 26f : Mathf.Lerp(15f, 22f, pack);
        float rowStep = start ? 18f : Mathf.Lerp(3.2f, 5.4f, pack);
        float maxWidth = start ? 150f : Mathf.Lerp(42f, 114f, pack);
        float frontLift = start ? 4f : Mathf.Lerp(2.5f, 4.5f, pack);
        float yJitterHi = 1.15f;
        float yJitterLo = -0.85f;

        int index = 0;
        for (int r = 0; r < rows.Count; r++)
        {
            int count = rows[r];
            if (count <= 0)
                continue;
            float y = origin.y + frontLift + (rows.Count - 1 - r) * rowStep;
            float usedSpacing = count > 1 ? Mathf.Min(spacing, maxWidth / (count - 1)) : spacing;
            float width = (count - 1) * usedSpacing;
            float stagger = start
                ? usedSpacing * 0.42f * ((r % 2 == 0) ? 1f : -1f)
                : (r == rows.Count - 1) ? 0f : usedSpacing * 0.34f * ((r % 2 == 0) ? 1f : -1f);
            float x0 = origin.x - width * 0.5f + stagger;
            for (int c = 0; c < count && index < n; c++, index++)
            {
                RectTransform robot = standers[index];
                if (robot == null)
                    continue;
                float x = x0 + c * usedSpacing;
                if (!start && r == rows.Count - 1 && Mathf.Abs(x - origin.x) < 11f)
                    x += x >= origin.x ? 13f : -13f;
                float yJitter = start
                    ? StartCrowdY(c, r)
                    : ((c + r) % 2 == 0 ? yJitterHi : yJitterLo);
                Vector2 pos = new Vector2(x, y + yJitter);
                WriteRobotHome(robot, pos);
                if (applyNow)
                    robot.anchoredPosition = pos;
            }
        }
    }

    void SortCrowdDrawOrder()
    {
        if (playersRoot == null)
            return;
        var byDepth = new List<RectTransform>(robots);
        byDepth.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            int byY = b.anchoredPosition.y.CompareTo(a.anchoredPosition.y);
            return byY != 0 ? byY : a.anchoredPosition.x.CompareTo(b.anchoredPosition.x);
        });
        int sibling = 0;
        Transform selfPos = playersRoot.Find("SelfPos");
        if (selfPos != null)
        {
            selfPos.SetAsFirstSibling();
            sibling = 1;
        }
        for (int i = 0; i < byDepth.Count; i++)
        {
            if (byDepth[i] == null)
                continue;
            byDepth[i].SetSiblingIndex(sibling++);
        }
        if (playerAvatar != null)
            playerAvatar.SetAsLastSibling();
    }

    void WriteRobotHome(RectTransform robot, Vector2 pos)
    {
        for (int i = 0; i < robots.Count; i++)
        {
            if (robots[i] != robot)
                continue;
            while (robotHomePos.Count <= i)
                robotHomePos.Add(pos);
            robotHomePos[i] = pos;
            return;
        }
    }

    void PlaceSelfAvatar()
    {
        if (playersRoot == null || playerAvatar == null)
            return;

        if (playerAvatar.parent != playersRoot)
            playerAvatar.SetParent(playersRoot, false);

        RectTransform slot = EnsureSelfPos();
        if (slot != null)
            playerAvatar.anchoredPosition = slot.anchoredPosition;
        playerAvatar.SetAsLastSibling();
        playerAvatar.gameObject.SetActive(true);
        playerHomePos = playerAvatar.anchoredPosition;
        playerHomeScale = playerAvatar.localScale;
    }

    RectTransform EnsureSelfPos()
    {
        if (playersRoot == null)
            return null;

        Transform found = playersRoot.Find("SelfPos");
        if (found != null)
            return found as RectTransform;

        var go = new GameObject("SelfPos", typeof(RectTransform));
        var slot = go.GetComponent<RectTransform>();
        slot.SetParent(playersRoot, false);
        slot.SetAsFirstSibling();
        slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0.5f, 0.5f);
        slot.sizeDelta = Vector2.zero;
        slot.localScale = Vector3.one;
        slot.localRotation = Quaternion.identity;
        slot.anchoredPosition = GuessSelfStandPos();
        return slot;
    }

    Vector2 GuessSelfStandPos()
    {
        int n = 0;
        float y = 0f;
        for (int i = 0; i < robots.Count; i++)
        {
            if (robots[i] == null)
                continue;
            y += robots[i].anchoredPosition.y;
            n++;
        }

        return new Vector2(0f, n > 0 ? y / n : 36f);
    }

    /// <summary>
    /// Prefab 里 Players 挂在 start/PlayersPos 下，不是 trail 或台子的直接子节点。
    /// </summary>
    RectTransform FindPlayersRoot()
    {
        if (trailNodes == null)
            return null;

        Transform trail = trailNodes.parent;
        Transform found = FindPlayersUnder(trail);
        if (found != null)
            return found as RectTransform;

        for (int i = 0; i < trailNodes.childCount; i++)
        {
            found = FindPlayersUnder(trailNodes.GetChild(i));
            if (found != null)
                return found as RectTransform;
        }

        found = FindNamedRecursive(trail != null ? trail : trailNodes, "Players");
        if (found == null)
            Debug.LogError("[PuppyAdventure] Players not found under trail/start");
        return found as RectTransform;
    }

    static Transform FindPlayersUnder(Transform node)
    {
        if (node == null)
            return null;
        Transform found = node.Find("Players");
        if (found != null)
            return found;
        return node.Find(PuppyAdventureDemoState.PlayersNestedPath);
    }

    static Transform FindNamedRecursive(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamedRecursive(root.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    void TrimAvatarExtras(Transform avatar)
    {
        if (avatar == null)
            return;
        Transform honor = AvatarFrameUiHelper.FindHonorNode(avatar);
        if (honor != null)
            honor.gameObject.SetActive(false);
        Transform wins = avatar.Find("TotalWinCount");
        if (wins != null)
            wins.gameObject.SetActive(false);
    }

    void ApplyAvatarIcons()
    {
        int playerHead = 1;
        if (GPUserDataManager.HasInit() && GPUserDataManager.GPUserData != null)
            playerHead = Mathf.Max(1, GPUserDataManager.GPUserData.userhead);
        SetAvatarIcon(playerAvatar, playerHead);
        ApplyFrame(playerAvatar, PuppyAdventureUiUtil.GetPlayerFrameId());

        int[] icons = PuppyAdventureDemoState.Data.robotIcons;
        for (int i = 0; i < robots.Count; i++)
        {
            int id = icons != null && i < icons.Length && icons[i] > 0
                ? icons[i]
                : Random.Range(1, 9);
            SetAvatarIcon(robots[i], id);
            ApplyFrame(robots[i], PuppyAdventureUiUtil.PickRandomFrameId());
        }
    }

    void SetAvatarIcon(RectTransform avatar, int headId)
    {
        if (avatar == null)
            return;
        Image icon = avatar.Find("icon")?.GetComponent<Image>();
        if (icon == null)
            icon = avatar.GetComponentInChildren<Image>(true);
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

    void SyncAlive(int visualRobots)
    {
        visualRobots = Mathf.Clamp(visualRobots, 0, robots.Count);
        while (robotAlive.Count < robots.Count)
            robotAlive.Add(true);
        for (int i = 0; i < robots.Count; i++)
        {
            bool alive = i < visualRobots;
            robotAlive[i] = alive;
            RectTransform robot = robots[i];
            if (robot == null)
                continue;
            RestoreAvatarHome(robot, i < robotHomePos.Count ? robotHomePos[i] : Vector2.zero,
                i < robotHomeScale.Count ? robotHomeScale[i] : robot.localScale);
            robot.gameObject.SetActive(alive);
        }

        if (playerAvatar != null)
        {
            RestoreAvatarHome(playerAvatar, playerHomePos, playerHomeScale);
            playerAvatar.gameObject.SetActive(true);
        }
    }

    void RestoreAvatarHome(RectTransform avatar, Vector2 homePos, Vector3 homeScale)
    {
        if (avatar == null || playersRoot == null)
            return;
        // 已在 Players 下就不要 SetParent，避免 sibling 被挪到末尾。
        if (avatar.parent != playersRoot)
            avatar.SetParent(playersRoot, false);
        avatar.pivot = new Vector2(0.5f, 0.5f);
        avatar.anchoredPosition = homePos;
        avatar.localRotation = Quaternion.identity;
        avatar.localScale = homeScale;
        CanvasGroup fade = avatar.GetComponent<CanvasGroup>();
        if (fade != null)
            fade.alpha = 1f;
    }

    void AttachPlayersToNode(RectTransform node, bool worldStay)
    {
        if (playersRoot == null || node == null)
            return;
        RectTransform slot = GetPlayersPos(node);
        playersRoot.SetParent(slot != null ? slot : node, worldStay);
    }

    RectTransform GetPlayersPos(RectTransform trailNode)
    {
        if (trailNode == null)
            return null;
        Transform found = trailNode.Find("PlayersPos");
        return found as RectTransform;
    }

    void SnapToProgress(int progress)
    {
        RectTransform node = ActiveTrailNode(progress);
        if (playersRoot != null)
        {
            if (node != null)
                AttachPlayersToNode(node, true);
            else if (playersHomeParent != null)
                playersRoot.SetParent(playersHomeParent, false);
            playersRoot.anchoredPosition = Vector2.zero;
            playersRoot.localRotation = Quaternion.identity;
            if (!IsEndTrailNode(node))
                playersRoot.localScale = Vector3.one;
        }
        PackStandHomes(CollectStandingRobots(), node, true);
        SyncPillarSpines(progress);
    }

    DG.Tweening.Tween JumpPlayersToProgress(int progress, System.Action onArrived,
        IList<RectTransform> lateJumpers = null,
        IDictionary<RectTransform, Vector2> landingOverride = null,
        HashSet<RectTransform> stayPut = null)
    {
        RectTransform node = ActiveTrailNode(progress);
        RectTransform slot = GetPlayersPos(node);
        if (playersRoot == null || node == null)
        {
            onArrived?.Invoke();
            return null;
        }

        Transform destParent = slot != null ? (Transform)slot : node;
        RectTransform leaveNode = FindOccupiedTrailNode();
        Transform oldParent = playersRoot.parent;
        Vector3 world = playersRoot.position;
        jumpTween?.Kill();
        pillarDownTween?.Kill();
        DOTween.Kill(playersRoot);
        KillAvatarTweens(stayPut);

        RectTransform flyLayer = trailNodes != null ? trailNodes : playersRoot.parent as RectTransform;
        if (flyLayer == null)
            flyLayer = destParent as RectTransform;

        // 先提到 trail 最上层再跳，避免还在原台子上时被台子 Spine 挡住。
        playersRoot.SetParent(flyLayer, true);
        playersRoot.SetAsLastSibling();
        playersRoot.localRotation = Quaternion.identity;

        Vector2 from = playersRoot.anchoredPosition;
        if (from.sqrMagnitude < 1f && oldParent != flyLayer)
        {
            from = flyLayer.InverseTransformPoint(world);
            playersRoot.anchoredPosition = from;
        }

        Vector2 localDelta = playersRoot.InverseTransformPoint(
            destParent != null ? destParent.position : node.position);

        List<RectTransform> jumpers = OrderJumpers(lateJumpers, stayPut);
        lastJumpWatchdog = JumpStaggerMax * Mathf.Max(0, jumpers.Count - 1)
            + PuppyAdventureAnim.JumpTotalDuration;
        if (lateJumpers != null && lateJumpers.Count > 0)
            lastJumpWatchdog += FallerJumpGap;

        if (!PuppyAdventureDemoState.JumpNeedsTween(from, oldParent != destParent))
        {
            SettlePlayersToNode(node, landingOverride, stayPut);
            onArrived?.Invoke();
            return null;
        }

        if (jumpers.Count == 0)
        {
            SettlePlayersToNode(node, landingOverride, stayPut);
            onArrived?.Invoke();
            return null;
        }

        Sequence master = DOTween.Sequence().SetUpdate(true).SetTarget(playersRoot).Pause();
        int pending = jumpers.Count;
        bool settled = false;
        void FinishJump()
        {
            if (settled)
                return;
            settled = true;
            SettlePlayersToNode(node, landingOverride, stayPut);
            onArrived?.Invoke();
        }

        var lateSet = new HashSet<RectTransform>();
        if (lateJumpers != null)
        {
            for (int i = 0; i < lateJumpers.Count; i++)
            {
                if (lateJumpers[i] != null)
                    lateSet.Add(lateJumpers[i]);
            }
        }

        float delay = Random.Range(0f, 0.03f);
        float lastStartDelay = delay;
        float maxEnd = 0f;
        bool gapApplied = lateSet.Count == 0;
        for (int i = 0; i < jumpers.Count; i++)
        {
            RectTransform jumper = jumpers[i];
            if (i > 0)
                delay += Random.Range(JumpStaggerMin, JumpStaggerMax);
            if (!gapApplied && lateSet.Contains(jumper))
            {
                delay += Random.Range(FallerJumpGap * 0.8f, FallerJumpGap * 1.35f);
                gapApplied = true;
            }

            Vector2 land = LandingPosOf(jumper, landingOverride);
            Vector2 dest = land + localDelta;
            float power = JumpPower * Random.Range(0.86f, 1.16f);
            Sequence jump = PuppyAdventureAnim.JumpTo(
                jumper, dest, power, HomeScaleOf(jumper), !IsEndTrailNode(node),
                () => PlayJungleClip(XSortGameCore.AudioController.AudioClips?.jungle_forward));
            float jumpDur = jump.Duration(false);
            jump.SetDelay(delay);
            jump.OnComplete(() =>
            {
                pending--;
                if (pending <= 0)
                    FinishJump();
            });
            jump.Play();
            lastStartDelay = delay;
            maxEnd = Mathf.Max(maxEnd, delay + jumpDur);
        }

        lastJumpWatchdog = maxEnd;
        if (leaveNode != null && leaveNode != node)
        {
            pillarDownTween?.Kill();
            float downAt = lastStartDelay
                + PuppyAdventureAnim.JumpAnticipateDuration
                + PillarDownAfterJump;
            pillarDownTween = DOVirtual.DelayedCall(downAt, () =>
                PlayPillarDown(leaveNode)).SetUpdate(true);
        }

        master.AppendInterval(lastJumpWatchdog + 0.05f);
        master.OnComplete(FinishJump);
        master.Play();
        jumpTween = master;
        return master;
    }

    List<RectTransform> OrderJumpers(IList<RectTransform> lateJumpers, HashSet<RectTransform> stayPut = null)
    {
        var lateSet = new HashSet<RectTransform>();
        if (lateJumpers != null)
        {
            for (int i = 0; i < lateJumpers.Count; i++)
            {
                if (lateJumpers[i] != null)
                    lateSet.Add(lateJumpers[i]);
            }
        }

        var all = CollectActiveJumpers(stayPut);
        var allSet = new HashSet<RectTransform>(all);
        var early = new List<RectTransform>();
        for (int i = 0; i < all.Count; i++)
        {
            if (!lateSet.Contains(all[i]))
                early.Add(all[i]);
        }

        early.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
        for (int i = 0; i < early.Count - 1; i++)
        {
            if (Random.value > 0.4f)
                continue;
            RectTransform swap = early[i];
            early[i] = early[i + 1];
            early[i + 1] = swap;
            i++;
        }
        if (lateJumpers != null)
        {
            for (int i = 0; i < lateJumpers.Count; i++)
            {
                RectTransform jumper = lateJumpers[i];
                if (jumper != null && allSet.Contains(jumper))
                    early.Add(jumper);
            }
        }

        return early;
    }

    Vector2 HomePosOf(RectTransform avatar)
    {
        if (avatar == null)
            return Vector2.zero;
        if (avatar == playerAvatar)
            return playerHomePos;
        for (int i = 0; i < robots.Count; i++)
        {
            if (robots[i] == avatar)
                return i < robotHomePos.Count ? robotHomePos[i] : avatar.anchoredPosition;
        }
        return avatar.anchoredPosition;
    }

    Vector2 LandingPosOf(RectTransform avatar, IDictionary<RectTransform, Vector2> landingOverride)
    {
        if (avatar != null && landingOverride != null && landingOverride.TryGetValue(avatar, out Vector2 edge))
            return edge;
        return HomePosOf(avatar);
    }

    List<RectTransform> CollectActiveJumpers(HashSet<RectTransform> stayPut = null)
    {
        var jumpers = new List<RectTransform>();
        if (playerAvatar != null && playerAvatar.gameObject.activeSelf
            && (stayPut == null || !stayPut.Contains(playerAvatar)))
            jumpers.Add(playerAvatar);
        for (int i = 0; i < robots.Count; i++)
        {
            RectTransform robot = robots[i];
            if (robot == null || robot == playerAvatar || !robot.gameObject.activeSelf)
                continue;
            if (stayPut != null && stayPut.Contains(robot))
                continue;
            if (i < robotAlive.Count && !robotAlive[i])
                continue;
            jumpers.Add(robot);
        }

        jumpers.Sort((a, b) =>
        {
            int cmp = a.anchoredPosition.x.CompareTo(b.anchoredPosition.x);
            return cmp != 0 ? cmp : a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
        });
        return jumpers;
    }

    Vector3 HomeScaleOf(RectTransform avatar)
    {
        if (avatar == null)
            return Vector3.one;
        if (avatar == playerAvatar)
            return playerHomeScale;
        for (int i = 0; i < robots.Count; i++)
        {
            if (robots[i] == avatar)
                return i < robotHomeScale.Count ? robotHomeScale[i] : avatar.localScale;
        }
        return avatar.localScale;
    }

    void RestoreAllAvatarHomes(IDictionary<RectTransform, Vector2> skip = null,
        HashSet<RectTransform> stayPut = null)
    {
        if (playerAvatar != null && (skip == null || !skip.ContainsKey(playerAvatar))
            && (stayPut == null || !stayPut.Contains(playerAvatar)))
            RestoreAvatarHome(playerAvatar, playerHomePos, playerHomeScale);
        for (int i = 0; i < robots.Count; i++)
        {
            if (robots[i] == null || (skip != null && skip.ContainsKey(robots[i])))
                continue;
            if (stayPut != null && stayPut.Contains(robots[i]))
                continue;
            RestoreAvatarHome(robots[i],
                i < robotHomePos.Count ? robotHomePos[i] : Vector2.zero,
                i < robotHomeScale.Count ? robotHomeScale[i] : Vector3.one);
        }
    }

    void SettlePlayersToNode(RectTransform node, IDictionary<RectTransform, Vector2> landingOverride = null,
        HashSet<RectTransform> stayPut = null)
    {
        if (playersRoot == null)
            return;
        if (node != null)
            AttachPlayersToNode(node, true);
        playersRoot.anchoredPosition = Vector2.zero;
        playersRoot.localRotation = Quaternion.identity;
        if (!IsEndTrailNode(node))
            playersRoot.localScale = Vector3.one;
        RestoreAllAvatarHomes(landingOverride, stayPut);
        if (landingOverride == null)
            return;
        foreach (var kv in landingOverride)
        {
            if (kv.Key == null || (stayPut != null && stayPut.Contains(kv.Key)))
                continue;
            RestoreAvatarHome(kv.Key, kv.Value, HomeScaleOf(kv.Key));
        }
    }

    void KillAvatarTweens(HashSet<RectTransform> stayPut = null)
    {
        if (playerAvatar != null && (stayPut == null || !stayPut.Contains(playerAvatar)))
            DOTween.Kill(playerAvatar);
        for (int i = 0; i < robots.Count; i++)
        {
            if (robots[i] != null && (stayPut == null || !stayPut.Contains(robots[i])))
                DOTween.Kill(robots[i]);
        }
    }

    RectTransform FindTrailNodeFromSlot(Transform slot)
    {
        Transform current = slot;
        while (current != null && current != trailNodes)
        {
            if (current.parent == trailNodes)
                return current as RectTransform;
            current = current.parent;
        }
        return null;
    }

    List<RectTransform> CollectActiveTrailNodes()
    {
        var actives = new List<RectTransform>();
        if (trailNodes == null)
            return actives;
        for (int i = 0; i < TrailPathNames.Length; i++)
        {
            Transform child = trailNodes.Find(TrailPathNames[i]);
            if (child == null || !child.gameObject.activeSelf)
                continue;
            actives.Add(child as RectTransform);
        }
        return actives;
    }

    RectTransform ActiveTrailNode(int progress)
    {
        List<RectTransform> actives = CollectActiveTrailNodes();
        if (actives.Count == 0)
            return null;
        int index = PuppyAdventureDemoState.TrailNodeIndex(
            progress, PuppyAdventureDemoState.DisplayWinTarget, actives.Count);
        if (index < 0 || index >= actives.Count)
            return null;
        return actives[index];
    }

    static bool IsStartTrailNode(RectTransform node)
    {
        if (node == null || string.IsNullOrEmpty(node.name))
            return false;
        return node.name.Equals("start", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool IsEndTrailNode(RectTransform node)
    {
        if (node == null || string.IsNullOrEmpty(node.name))
            return false;
        return node.name.Equals("end", System.StringComparison.OrdinalIgnoreCase);
    }

    Transform PickFallSlot(RectTransform destNode, bool left)
    {
        if (destNode == null)
            return null;
        Transform side = destNode.Find(left ? "PosLeft" : "PosRight");
        if (side != null)
            return side;
        Transform other = destNode.Find(left ? "PosRight" : "PosLeft");
        return other != null ? other : destNode;
    }

    Transform FindStandSlot(RectTransform destNode, bool left)
    {
        if (destNode == null)
            return null;
        Transform named = destNode.Find(left ? StandLeftName : StandRightName);
        if (named != null)
            return named;
        named = destNode.Find(left ? "XXLeft" : "XXRight");
        return named;
    }

    List<int> PickRandomFallers(int dropCount)
    {
        var picked = new List<int>();
        var candidates = new List<int>();
        for (int i = 0; i < robots.Count; i++)
        {
            if (robots[i] == null || robots[i] == playerAvatar)
                continue;
            if (i < robotAlive.Count && robotAlive[i] && robots[i].gameObject.activeSelf)
                candidates.Add(i);
        }

        dropCount = Mathf.Min(dropCount, candidates.Count);
        for (int n = 0; n < dropCount; n++)
        {
            int pick = Random.Range(0, candidates.Count);
            picked.Add(candidates[pick]);
            candidates.RemoveAt(pick);
        }
        return picked;
    }

    void BuildFallerLandings(List<int> fallers, RectTransform destNode,
        List<RectTransform> lateJumpers, Dictionary<RectTransform, Vector2> landingOverride,
        Dictionary<RectTransform, bool> fallerLeft)
    {
        if (fallers == null)
            return;
        int leftN = 0;
        int rightN = 0;
        for (int i = 0; i < fallers.Count; i++)
        {
            int robotIndex = fallers[i];
            if (robotIndex < 0 || robotIndex >= robots.Count || robots[robotIndex] == null)
                continue;
            bool left = i % 2 == 0;
            int sideIndex = left ? leftN++ : rightN++;
            RectTransform robot = robots[robotIndex];
            lateJumpers.Add(robot);
            if (fallerLeft != null)
                fallerLeft[robot] = left;
            landingOverride[robot] = EdgeStandPos(destNode, left, sideIndex);
        }
    }

    Vector2 EdgeStandPos(RectTransform destNode, bool left, int sideIndex)
    {
        float y = playerHomePos != Vector2.zero ? playerHomePos.y : GuessSelfStandPos().y;
        float x = left ? -90f : 82f;
        Transform stand = FindStandSlot(destNode, left);
        RectTransform pad = GetPlayersPos(destNode);
        if (stand != null && destNode != null)
        {
            Vector3 standLocal = destNode.InverseTransformPoint(stand.position);
            Vector3 padLocal = pad != null
                ? destNode.InverseTransformPoint(pad.position)
                : Vector3.zero;
            x = standLocal.x - padLocal.x;
            y = standLocal.y - padLocal.y;
        }

        x += (left ? -1f : 1f) * sideIndex * 16f;
        y = playerHomePos != Vector2.zero ? playerHomePos.y : y;
        return new Vector2(x, y);
    }

    static SkeletonGraphic FindSplashSpine(Transform slot)
    {
        if (slot == null)
            return null;
        SkeletonGraphic[] spines = slot.GetComponentsInChildren<SkeletonGraphic>(true);
        for (int i = 0; i < spines.Length; i++)
        {
            SkeletonGraphic spine = spines[i];
            if (spine == null)
                continue;
            string name = spine.gameObject.name;
            if (name.IndexOf(SplashSpineKey, System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("ChrFall", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return spine;
        }
        return spines.Length > 0 ? spines[0] : null;
    }

    bool PlayFallSplash(Transform slot)
    {
        SkeletonGraphic spine = FindSplashSpine(slot);
        return PlayFallSplash(spine, slot);
    }

    bool PlayFallSplash(SkeletonGraphic spine, Transform slot)
    {
        if (spine == null)
            spine = FindSplashSpine(slot);
        if (spine == null)
            return false;
        if (slot != null)
            slot.SetAsLastSibling();
        Transform root = SplashRootOf(spine);
        if (root != null)
            root.gameObject.SetActive(true);
        spine.gameObject.SetActive(true);
        spine.enabled = true;
        spine.freeze = false;
        PuppyAdventureAnim.PlayOnceAndHold(spine, null);
        var entry = spine.AnimationState != null ? spine.AnimationState.GetCurrent(0) : null;
        if (entry != null)
        {
            entry.TrackTime = 0.03f;
            spine.Update(0f);
        }
        return true;
    }

    static Transform SplashRootOf(SkeletonGraphic spine)
    {
        if (spine == null)
            return null;
        Transform current = spine.transform;
        Transform parent = current.parent;
        if (parent != null && (parent.name.IndexOf(SplashSpineKey, System.StringComparison.OrdinalIgnoreCase) >= 0
            || parent.name.IndexOf("ChrFall", System.StringComparison.OrdinalIgnoreCase) >= 0))
            return parent;
        return current;
    }

    void HideTrailSplashes()
    {
        RestorePlayerSplash();
        List<RectTransform> nodes = CollectActiveTrailNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            HideSplashOnSlot(nodes[i] != null ? nodes[i].Find("PosLeft") : null);
            HideSplashOnSlot(nodes[i] != null ? nodes[i].Find("PosRight") : null);
        }
    }

    static void HideSplashOnSlot(Transform slot)
    {
        SkeletonGraphic spine = FindSplashSpine(slot);
        if (spine == null)
            return;
        spine.freeze = true;
        spine.gameObject.SetActive(false);
    }

    void RefreshHud(int win, int players)
    {
        if (progressTex != null)
            progressTex.text = $"{win}/{PuppyAdventureDemoState.DisplayWinTarget}";
        if (playersTex != null)
            playersTex.text = $"{Mathf.Max(0, players)}/100";
        if (coinText != null)
            coinText.text = PuppyAdventureDemoState.DisplayPrizePool.ToString();
        LocalizedTMPRefresher.SetText(remainDesTex, () => PuppyAdventureLoc.Get(
            "JungleQuest_MainRemainDes",
            Mathf.Max(0, PuppyAdventureDemoState.DisplayWinTarget - win)));
        LocalizedTMPRefresher.SetText(failTex, () =>
            PuppyAdventureDemoState.Presentation == PuppyAdventurePresentation.Fail
                ? PuppyAdventureLoc.Get("JungleQuest_FailCooldownDes")
                : PuppyAdventureLoc.Get("JungleQuest_FailDes"));
        RefreshTimer();
    }

    void RefreshTimer()
    {
        if (timerTex != null)
        {
            long seconds = PuppyAdventureDemoState.IsRunning
                ? PuppyAdventureDemoState.ChallengeSecondsLeft
                : PuppyAdventureDemoState.CooldownSecondsLeft;
            timerTex.text = PuppyAdventureDemoState.FormatTime(seconds);
        }
    }

    void SetFail(bool fail)
    {
        if (failRoot != null)
            failRoot.gameObject.SetActive(fail);
        if (successRoot != null)
            successRoot.gameObject.SetActive(!fail);
    }

    protected override void OnClicked(Button button)
    {
        if (button == infoBtn)
        {
            PlayButtonClick();
            GameModule.UI.OpenPanel<UIPuppyAdventureHelp>();
            return;
        }

        if (button == tapBtn || button == closeBtn)
        {
            if (!CanUserDismiss(button))
                return;
            DismissMain();
            return;
        }

        if (button == advanceBtn)
        {
            PlayButtonClick();
            OnDebugAdvance();
            return;
        }

        if (button == failBtn)
        {
            PlayButtonClick();
            OnDebugFail();
            return;
        }
    }

    /// <summary>
    /// 结算跳跃/掉落未播完时禁止关闭。过早 CloseSelf(null) 或不带 FinishMain 会丢掉
    /// ContinueAfterPuppyWin / UnlockNextLevelAfterActivity，表现为下一关点不动。
    /// </summary>
    bool CanUserDismiss(Button button)
    {
        if (dismissing || handoffToReward)
            return false;
        if (PuppyAdventureFlow.BrowseFromHome)
            return button == closeBtn || button == tapBtn;
        // 局内结算：仅 ShowTap 之后（全屏 tap 已显）可点继续
        return button == tapBtn && tapBtn != null && tapBtn.gameObject.activeSelf;
    }

    void DismissMain()
    {
        if (dismissing)
            return;
        dismissing = true;
        // 先杀 watchdog，避免关动画期间 FinishAdvance 再二次 CloseSelf / 开 Reward
        KillPreviewTweens();
        CloseSelf(PuppyAdventureFlow.FinishMain);
    }

    protected override void OnBackgroundClicked(UIPanelBgClickEventType bgClickEventType)
    {
        // 禁止默认 CloseSelf(null)：会清面板但不调 FinishMain，continuation 丢失后下一关卡死
        if (PuppyAdventureFlow.BrowseFromHome)
        {
            DismissMain();
            return;
        }

        if (tapBtn != null && tapBtn.gameObject.activeSelf)
            DismissMain();
    }

    static void PlayButtonClick()
    {
        if (XSortGameCore.AudioController.AudioClips != null
            && XSortGameCore.AudioController.AudioClips.buttonSound != null)
            XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.buttonSound);
    }

    void BindHotkeys()
    {
        if (gameObject == null)
            return;
        hotkeys = gameObject.GetComponent<PuppyAdventureHotkeys>();
        if (hotkeys == null)
            hotkeys = gameObject.AddComponent<PuppyAdventureHotkeys>();
        hotkeys.Owner = this;
    }

    public void OnHotkeyAdvance()
    {
        KillPreviewTweens();
        waitingTap = false;
        finishedWin = false;
        PuppyAdventureDemoState.DebugPreviewWin();
        SetFail(false);
        if (tapBtn != null)
            tapBtn.gameObject.SetActive(false);
        PlayAdvanceVisual(PuppyAdventureDemoState.Presentation == PuppyAdventurePresentation.Complete);
    }

    public void OnHotkeyFail()
    {
        KillPreviewTweens();
        waitingTap = false;
        finishedWin = false;
        PuppyAdventureDemoState.DebugPreviewFail();
        RestorePlayerForPreview();
        SetFail(true);
        if (tapBtn != null)
            tapBtn.gameObject.SetActive(false);
        int progress = PuppyAdventureDemoState.Data.lastFailureWin;
        int players = PuppyAdventureDemoState.AnimFromPlayers > 0
            ? PuppyAdventureDemoState.AnimFromPlayers
            : PuppyAdventureDemoState.RemainPlayers;
        SyncAlive(PuppyAdventureDemoState.VisualRobotCount(players));
        PlayPillarIdlesStaggered();
        HideTrailSplashes();
        RefreshHud(progress, players);
        SnapToProgress(progress);
        PlayFail();
    }

    void RestorePlayerForPreview()
    {
        if (playerAvatar == null || playersRoot == null)
            return;
        if (playerAvatar.parent != playersRoot)
        {
            playerAvatar.SetParent(playersRoot, false);
            playerAvatar.SetSiblingIndex(Mathf.Clamp(playersRoot.childCount - 1, 0, playersRoot.childCount - 1));
        }
        playerAvatar.gameObject.SetActive(true);
        RestoreAvatarHome(playerAvatar, playerHomePos, playerHomeScale);
    }

    public void OnHotkeyReset()
    {
        KillPreviewTweens();
        waitingTap = false;
        finishedWin = false;
        PuppyAdventureDemoState.DebugResetToStart();
        SetFail(false);
        if (tapBtn != null)
            tapBtn.gameObject.SetActive(false);
        SyncAlive(PuppyAdventureDemoState.VisualRobotCount(PuppyAdventureDemoState.RemainPlayers));
        PlayPillarIdlesStaggered();
        HideTrailSplashes();
        RefreshHud(0, PuppyAdventureDemoState.RemainPlayers);
        SnapToProgress(0);
        ShowTap();
    }

    void KillPreviewTweens()
    {
        hudTween?.Kill();
        hudTween = null;
        rewardDelay?.Kill();
        rewardDelay = null;
        fallHideTween?.Kill();
        fallHideTween = null;
        jumpTween?.Kill();
        jumpTween = null;
        pillarDownTween?.Kill();
        pillarDownTween = null;
        tapFade?.Kill();
        tapFade = null;
        if (playersRoot != null)
            DOTween.Kill(playersRoot);
        KillAvatarTweens();
    }

    void OnDebugAdvance()
    {
        if (waitingTap && tapBtn != null && tapBtn.gameObject.activeSelf)
            return;

        if (PuppyAdventureDemoState.IsRunning)
            PuppyAdventureDemoState.RecordLevelWin();

        bool complete = PuppyAdventureDemoState.Presentation == PuppyAdventurePresentation.Complete;
        SetFail(false);
        if (tapBtn != null)
            tapBtn.gameObject.SetActive(false);
        PlayAdvanceVisual(complete);
    }

    void OnDebugFail()
    {
        if (waitingTap && tapBtn != null && tapBtn.gameObject.activeSelf
            && PuppyAdventureDemoState.Presentation == PuppyAdventurePresentation.Fail)
            return;

        if (PuppyAdventureDemoState.IsRunning)
            PuppyAdventureDemoState.RecordGiveUp();

        SetFail(true);
        if (tapBtn != null)
            tapBtn.gameObject.SetActive(false);
        SyncAlive(PuppyAdventureDemoState.VisualRobotCount(
            PuppyAdventureDemoState.AnimFromPlayers > 0
                ? PuppyAdventureDemoState.AnimFromPlayers
                : PuppyAdventureDemoState.RemainPlayers));
        SnapToProgress(PuppyAdventureDemoState.Data.lastFailureWin);
        PlayFail();
    }

    void PlayAdvanceVisual(bool complete)
    {
        waitingTap = false;
        if (tapBtn != null)
            tapBtn.gameObject.SetActive(false);
        HideTrailSplashes();
        PuppyAdventureDemoState.ResolveHomeProgress(out int fromWin, out int toWin);
        PuppyAdventureDemoState.ResolveHomePlayers(out int fromPlayers, out int toPlayers);

        int fromVisual = PuppyAdventureDemoState.VisualRobotCount(fromPlayers);
        int toVisual = PuppyAdventureDemoState.VisualRobotCount(toPlayers);
        SyncAlive(fromVisual);
        RefreshHud(fromWin, fromPlayers);
        SnapToProgress(fromWin);

        bool finished = false;
        bool jumpDone = false;
        void FinishAdvance()
        {
            if (finished || dismissing)
                return;
            finished = true;
            hudTween?.Kill();
            hudTween = null;
            RefreshHud(toWin, toPlayers);
            SnapToProgress(toWin);
            if (complete)
            {
                finishedWin = true;
                handoffToReward = true;
                if (tapBtn != null)
                    tapBtn.gameObject.SetActive(false);
                rewardDelay?.Kill();
                rewardDelay = DOVirtual.DelayedCall(0.25f, () =>
                    {
                        if (dismissing)
                            return;
                        CloseSelf(() => GameModule.UI.OpenPanel<UIPuppyAdventureReward>());
                    })
                    .SetUpdate(true);
            }
            else
            {
                PuppyAdventureDemoState.SetPresentation(PuppyAdventurePresentation.Current);
                ShowTap();
            }
        }

        hudTween?.Kill();
        if (fromPlayers != toPlayers)
            hudTween = PuppyAdventureAnim.CountTo(playersTex, fromPlayers, toPlayers, 0.55f, n => n + "/100");

        int dropCount = Mathf.Max(0, fromVisual - toVisual);
        if (dropCount < 1 && fromVisual > 0)
            dropCount = 1;
        RectTransform destNode = ActiveTrailNode(toWin);
        var lateJumpers = new List<RectTransform>();
        var landingOverride = new Dictionary<RectTransform, Vector2>();
        var fallerLeft = new Dictionary<RectTransform, bool>();
        List<int> fallers = PickRandomFallers(dropCount);
        BuildFallerLandings(fallers, destNode, lateJumpers, landingOverride, fallerLeft);
        var fallerSet = new HashSet<RectTransform>();
        for (int i = 0; i < lateJumpers.Count; i++)
        {
            if (lateJumpers[i] != null)
                fallerSet.Add(lateJumpers[i]);
        }
        var standers = new List<RectTransform>();
        for (int i = 0; i < robots.Count; i++)
        {
            RectTransform robot = robots[i];
            if (robot == null || robot == playerAvatar || !robot.gameObject.activeSelf)
                continue;
            if (i < robotAlive.Count && !robotAlive[i])
                continue;
            if (fallerSet.Contains(robot))
                continue;
            standers.Add(robot);
        }
        PackStandHomes(standers, destNode, false);

        void AfterJump()
        {
            if (jumpDone || finished)
                return;
            jumpDone = true;
            SettlePlayersToNode(destNode, landingOverride);

            if (fallers.Count == 0)
            {
                FinishAdvance();
                return;
            }

            int pending = fallers.Count;
            int leftN = 0;
            int rightN = 0;
            for (int n = 0; n < fallers.Count; n++)
            {
                int robotIndex = fallers[n];
                if (robotIndex < 0 || robotIndex >= robots.Count)
                {
                    pending--;
                    continue;
                }
                robotAlive[robotIndex] = false;
                RectTransform robot = robots[robotIndex];
                bool goLeft = robot != null && fallerLeft.TryGetValue(robot, out bool assigned)
                    ? assigned
                    : robot != null && robot.anchoredPosition.x <= 0f;
                int sideIndex = goLeft ? leftN++ : rightN++;
                FallAvatarIntoSlot(robot, destNode, goLeft, () =>
                {
                    pending--;
                    if (pending > 0)
                        return;
                    fallHideTween?.Kill();
                    fallHideTween = DOVirtual.DelayedCall(HideAfterFall, () =>
                    {
                        for (int i = 0; i < robots.Count; i++)
                        {
                            if (i < robotAlive.Count && !robotAlive[i] && robots[i] != null)
                                robots[i].gameObject.SetActive(false);
                        }
                        CheckPlayersKeep();
                        FinishAdvance();
                    }).SetUpdate(true);
                }, n * FallStagger, sideIndex);
            }

            if (pending <= 0)
                FinishAdvance();
        }

        DG.Tweening.Tween jump = JumpPlayersToProgress(toWin, AfterJump, lateJumpers, landingOverride);
        if (jump == null)
            return;
        rewardDelay?.Kill();
        rewardDelay = DOVirtual.DelayedCall(lastJumpWatchdog + 0.08f, AfterJump).SetUpdate(true);
    }

    void PlayFail()
    {
        waitingTap = false;
        if (tapBtn != null)
            tapBtn.gameObject.SetActive(false);
        HideTrailSplashes();
        SetFail(true);
        int progress = PuppyAdventureDemoState.Data.lastFailureWin;
        RectTransform currentNode = ActiveTrailNode(progress);
        RectTransform nextNode = ActiveTrailNode(progress + 1);
        bool jumpAhead = nextNode != null && nextNode != currentNode;
        bool jumpDone = !jumpAhead;
        bool fallDone = playerAvatar == null;
        bool failFinished = false;

        int playerSibling = -1;
        if (playerAvatar != null)
        {
            playerSibling = playerAvatar.GetSiblingIndex();
            if (playerAvatar.parent == playersRoot)
                playerAvatar.SetAsLastSibling();
        }

        void FinishFail()
        {
            if (failFinished || dismissing || !jumpDone || !fallDone)
                return;
            failFinished = true;
            if (playerAvatar != null && playersRoot != null && playerAvatar.parent != playersRoot)
            {
                playerAvatar.SetParent(playersRoot, false);
                if (playerSibling >= 0)
                    playerAvatar.SetSiblingIndex(Mathf.Clamp(playerSibling, 0, playersRoot.childCount - 1));
                playerAvatar.gameObject.SetActive(false);
            }
            CheckPlayersKeep();
            ShowTap();
        }

        bool fallStarted = false;
        void BeginPlayerFall()
        {
            if (fallStarted)
                return;
            fallStarted = true;
            jumpDone = true;
            RectTransform pad = jumpAhead ? nextNode : currentNode;
            LiftPlayerAboveGroup();
            if (playerAvatar == null)
            {
                fallDone = true;
                FinishFail();
                return;
            }

            FallAvatarIntoSlot(playerAvatar, pad, false, () =>
            {
                fallHideTween?.Kill();
                fallHideTween = DOVirtual.DelayedCall(HideAfterFall, () =>
                {
                    if (playerAvatar != null)
                        playerAvatar.gameObject.SetActive(false);
                    fallDone = true;
                    FinishFail();
                }).SetUpdate(true);
            }, 0f, 0, true);
        }

        if (!jumpAhead)
        {
            BeginPlayerFall();
            return;
        }

        // 自己和大家一起跳到下一块台子，没有别人落水。落地后再没站稳垂直掉下去。
        DG.Tweening.Tween jump = JumpPlayersToProgress(progress + 1, BeginPlayerFall);
        if (jump == null)
            return;
        rewardDelay?.Kill();
        rewardDelay = DOVirtual.DelayedCall(lastJumpWatchdog + 0.08f, () =>
        {
            if (fallStarted)
                return;
            jumpTween?.Kill();
            BeginPlayerFall();
        }).SetUpdate(true);
    }

    void LiftPlayerAboveGroup()
    {
        if (playerAvatar == null)
            return;
        RectTransform layer = trailNodes != null
            ? trailNodes
            : playersRoot != null ? playersRoot.parent as RectTransform : null;
        if (layer == null)
            return;
        if (playerAvatar.parent != layer)
            playerAvatar.SetParent(layer, true);
        playerAvatar.SetAsLastSibling();
    }

    static Transform PickWaterSlot(RectTransform destNode)
    {
        if (destNode == null)
            return null;
        Transform left = destNode.Find("PosLeft");
        if (left != null)
            return left;
        Transform right = destNode.Find("PosRight");
        return right != null ? right : destNode;
    }

    void PlaySplashAboveAvatar(RectTransform avatar, Transform slot)
    {
        SkeletonGraphic spine = FindSplashSpine(slot);
        if (!PlayFallSplash(spine, slot) || avatar == null)
            return;
        Transform root = SplashRootOf(spine);
        if (root == null)
            return;
        if (playerSplashRoot != null && playerSplashRoot != root)
            RestorePlayerSplash();
        if (playerSplashRoot == null)
        {
            playerSplashRoot = root;
            playerSplashHome = root.parent;
            playerSplashSibling = root.GetSiblingIndex();
            RectTransform homeRect = root as RectTransform;
            playerSplashHomePos = homeRect != null ? homeRect.anchoredPosition : Vector2.zero;
        }
        RectTransform layer = avatar.parent as RectTransform;
        if (layer != null && root.parent != layer)
            root.SetParent(layer, true);
        Vector3 pos = root.position;
        pos.x = avatar.position.x;
        root.position = pos;
        root.SetAsLastSibling();
    }

    void RestorePlayerSplash()
    {
        if (playerSplashRoot == null)
            return;
        Transform root = playerSplashRoot;
        Transform home = playerSplashHome;
        playerSplashRoot = null;
        playerSplashHome = null;
        if (home != null)
        {
            root.SetParent(home, false);
            root.SetSiblingIndex(Mathf.Clamp(playerSplashSibling, 0, Mathf.Max(0, home.childCount - 1)));
            RectTransform rect = root as RectTransform;
            if (rect != null)
                rect.anchoredPosition = playerSplashHomePos;
        }
        root.gameObject.SetActive(false);
    }

    void FallAvatarIntoSlot(RectTransform avatar, RectTransform destNode, bool left,
        System.Action onLanded, float delay = 0f, int sideIndex = 0, bool straightDown = false)
    {
        if (avatar == null)
        {
            onLanded?.Invoke();
            return;
        }

        Transform slot = straightDown ? PickWaterSlot(destNode) : PickFallSlot(destNode, left);
        if (slot == null)
        {
            onLanded?.Invoke();
            return;
        }

        // 不换父：Players 子节点顺序保持，幸存者本地坐标也不动。
        RectTransform space = avatar.parent as RectTransform;
        if (space == null)
            space = playersRoot;
        PuppyAdventureAnim.SetPivotKeepVisual(avatar, new Vector2(0.5f, 0f));
        Vector2 dest = space.InverseTransformPoint(slot.position);
        if (straightDown)
        {
            Vector3 world = slot.position;
            Transform pad = slot.parent;
            Transform waterL = pad != null ? pad.Find("PosLeft") : null;
            Transform waterR = pad != null ? pad.Find("PosRight") : null;
            if (waterL != null && waterR != null)
                world.y = (waterL.position.y + waterR.position.y) * 0.5f;
            world.x = avatar.position.x;
            dest = space.InverseTransformPoint(world);
            dest.x = avatar.anchoredPosition.x;
        }
        else
        {
            dest.x += (dest.x >= 0f ? 1f : -1f) * sideIndex * 12f;
        }
        dest.y -= 18f;
        if (!straightDown)
            slot.SetAsLastSibling();
        SkeletonGraphic splashSpine = FindSplashSpine(slot);
        DOTween.Kill(avatar, false);
        bool isPlayer = avatar == playerAvatar;
        Sequence fall = PuppyAdventureAnim.UnstableThenFall(avatar, dest,
            () =>
            {
                // 机器人落水 jungle_down；玩家落水改播 jungle_fail（去掉原开场 fail 音）
                PlayJungleClip(isPlayer
                    ? XSortGameCore.AudioController.AudioClips?.jungle_fail
                    : XSortGameCore.AudioController.AudioClips?.jungle_down);
                if (straightDown)
                {
                    LiftPlayerAboveGroup();
                    PlaySplashAboveAvatar(avatar, slot);
                }
                else
                    PlayFallSplash(splashSpine, slot);
                FadeFallerIntoSplash(avatar);
            },
            () => HideFallerCovered(avatar));
        if (fall == null)
        {
            onLanded?.Invoke();
            return;
        }
        if (delay > 0.001f)
            fall.SetDelay(delay);
        fall.OnComplete(() =>
        {
            HideFallerCovered(avatar);
            onLanded?.Invoke();
        });
        fall.Play();
    }

    void FadeFallerIntoSplash(RectTransform avatar)
    {
        if (avatar == null)
            return;
        CanvasGroup fade = avatar.GetComponent<CanvasGroup>();
        if (fade == null)
            fade = avatar.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 1f;
        fade.blocksRaycasts = false;
        fade.interactable = false;
        DOTween.Sequence().SetUpdate(true)
            .AppendInterval(PuppyAdventureAnim.FallSplashVisibleAt)
            .Append(fade.DOFade(0f, PuppyAdventureAnim.FallFadeDuration).SetEase(Ease.InCubic))
            .SetTarget(avatar)
            .SetLink(avatar.gameObject);
    }

    static void HideFallerCovered(RectTransform avatar)
    {
        if (avatar == null || !avatar.gameObject.activeSelf)
            return;
        CanvasGroup fade = avatar.GetComponent<CanvasGroup>();
        if (fade != null)
            fade.alpha = 0f;
        avatar.gameObject.SetActive(false);
    }

    static bool IsSelfAvatarName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        return name.Equals("AvatarNode", System.StringComparison.OrdinalIgnoreCase)
            || name.Equals("player", System.StringComparison.OrdinalIgnoreCase)
            || name.Equals("self", System.StringComparison.OrdinalIgnoreCase);
    }

    void CheckPlayersKeep()
    {
        if (playersRoot == null)
            return;
        if (playersRoot.childCount != playersChildOrder.Count)
            Debug.LogError("[PuppyAdventure] Players child count changed");
        int n = Mathf.Min(playersRoot.childCount, playersChildOrder.Count);
        for (int i = 0; i < n; i++)
        {
            if (playersRoot.GetChild(i) != playersChildOrder[i])
                Debug.LogError("[PuppyAdventure] Players child order changed at " + i);
        }

        for (int i = 0; i < robots.Count; i++)
        {
            RectTransform robot = robots[i];
            if (robot == null)
                continue;
            bool alive = i < robotAlive.Count && robotAlive[i];
            if (!alive && robot.gameObject.activeSelf)
                Debug.LogError("[PuppyAdventure] fallen still visible: " + robot.name);
            if (alive && i < robotHomePos.Count
                && (robot.anchoredPosition - robotHomePos[i]).sqrMagnitude > 1f)
                Debug.LogError("[PuppyAdventure] survivor local pos changed: " + robot.name);
        }
    }

    void ShowTap()
    {
        if (dismissing || handoffToReward)
            return;
        waitingTap = true;
        ApplyCommittedHomeHud();
        if (tapBtn != null)
        {
            tapBtn.gameObject.SetActive(true);
            tapBtn.downScale = 0.98f;
            tapBtn.upScale = 1.02f;
            FadeInTap();
        }
        EnsureHudOnTop();
    }

    void FadeInTap()
    {
        CanvasGroup group = tapBtn.GetComponent<CanvasGroup>();
        if (group == null)
            group = tapBtn.gameObject.AddComponent<CanvasGroup>();
        tapFade?.Kill();
        group.alpha = 0f;
        tapFade = group.DOFade(1f, 0.28f).SetUpdate(true).SetLink(tapBtn.gameObject);
    }

    void EnsureHudOnTop()
    {
        Transform hud = infoBtn != null ? infoBtn.transform.parent : null;
        if (hud == null && successRoot != null)
            hud = successRoot.parent;
        hud?.SetAsLastSibling();
    }

    void ApplyCommittedHomeHud()
    {
        if (PuppyAdventureDemoState.Presentation == PuppyAdventurePresentation.Fail)
            return;
        PuppyAdventureDemoState.ResolveHomeProgress(out _, out int toWin);
        int toPlayers = PuppyAdventureDemoState.AnimToPlayers > 0
            ? PuppyAdventureDemoState.AnimToPlayers
            : PuppyAdventureDemoState.RemainPlayers;
        RefreshHud(toWin, toPlayers);
        SnapToProgress(toWin);
    }

    static void PlayJungleClip(AudioClip clip)
    {
        if (clip == null)
            return;
        XSortGameCore.AudioController.PlaySound(clip);
    }

    protected override void OnDestroying()
    {
        dismissing = true;
        timerTween?.Kill();
        timerTween = null;
        KillPreviewTweens();
        KillAvatarTweens();
        // 非 Reward 交接且非浏览态：兜底 FinishMain，避免 CloseSelf(null)/Destroy 漏调导致下一关卡死
        if (!handoffToReward && !PuppyAdventureFlow.BrowseFromHome)
            PuppyAdventureFlow.FinishMainIfNotSuppressed();
        if (hotkeys != null)
        {
            hotkeys.Owner = null;
            hotkeys = null;
        }
        base.OnDestroying();
    }

    sealed class PuppyAdventureHotkeys : MonoBehaviour
    {
        public UIPuppyAdventureMain Owner;

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || Owner == null)
                return;
            if (keyboard.f5Key.wasPressedThisFrame)
                Owner.OnHotkeyAdvance();
            else if (keyboard.f11Key.wasPressedThisFrame)
                Owner.OnHotkeyFail();
            else if (keyboard.f12Key.wasPressedThisFrame)
                Owner.OnHotkeyReset();
        }
    }
}
