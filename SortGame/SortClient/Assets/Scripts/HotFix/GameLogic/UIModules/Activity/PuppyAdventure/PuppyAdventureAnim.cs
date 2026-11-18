using DG.Tweening;
using Spine.Unity;
using TMPro;
using UnityEngine;

public static class PuppyAdventureAnim
{
    public const string BgLoopAnim = "BG_Loop";
    public const string ClockLoopAnim = "Clock_Loop";
    public const string PillarIdleAnim = "ZZ_Idle";
    public const string PillarDownAnim = "ZZ_DownO";
    public static Tween PunchIdle(Transform target, float strength = 4f)
    {
        if (target == null)
            return null;
        DOTween.Kill(target);
        return target.DOPunchRotation(new Vector3(0f, 0f, strength), 1.4f, 2, 0.4f)
            .SetLoops(-1, LoopType.Restart)
            .SetLink(target.gameObject);
    }

    public static Tween Pulse(Transform target, float scale = 1.08f)
    {
        if (target == null)
            return null;
        DOTween.Kill(target);
        target.localScale = Vector3.one;
        return target.DOScale(scale, 0.55f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(target.gameObject);
    }

    public static Tween Parallax(RectTransform target, float distance, float duration)
    {
        if (target == null)
            return null;
        DOTween.Kill(target);
        Vector2 start = target.anchoredPosition;
        return target.DOAnchorPosX(start.x + distance, duration)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(target.gameObject);
    }

    public const float JumpAnticipateDuration = 0.09f;
    public const float JumpAirDuration = 0.52f;
    public const float JumpLandDuration = 0.16f;
    public const float FallShakeDuration = 0.28f;
    public const float FallLeanDuration = 0.08f;
    public const float FallOffDuration = 0.58f;
    // InQuad 下落：只提前一点点开水花，人已经贴近水面。
    public const float FallSplashLead = 0.04f;
    // Chr_Fall：scale 在 0.033 前为 0，首波 RGBA 0.233 才实，约 0.36 盖住人。
    public const float FallSplashVisibleAt = 0.16f;
    public const float FallCoverAfterSplash = 0.36f;
    public static float JumpTotalDuration =>
        JumpAnticipateDuration + JumpAirDuration + JumpLandDuration;
    public static float FallTotalDuration =>
        FallShakeDuration + FallLeanDuration + FallOffDuration;
    public static float FallPlungeDelay =>
        Mathf.Max(0.05f, FallTotalDuration - FallSplashLead);
    public static float FallHideDelay => FallPlungeDelay + FallCoverAfterSplash;
    public static float FallFadeDuration =>
        Mathf.Max(0.06f, FallCoverAfterSplash - FallSplashVisibleAt);

    public static Sequence JumpTo(RectTransform mover, Vector2 dest, float jumpPower = 68f)
    {
        return JumpTo(mover, dest, jumpPower, mover != null ? mover.localScale : Vector3.one);
    }

    public static Sequence JumpTo(RectTransform mover, Vector2 dest, float jumpPower, Vector3 restScale)
    {
        return JumpTo(mover, dest, jumpPower, restScale, true);
    }

    public static Sequence JumpTo(RectTransform mover, Vector2 dest, float jumpPower, Vector3 restScale, bool squash,
        System.Action onTakeOff = null)
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true).Pause();
        if (mover == null)
            return seq;

        Vector3 rest = restScale;
        if (Mathf.Abs(rest.x) < 0.001f)
            rest.x = 1f;
        if (Mathf.Abs(rest.y) < 0.001f)
            rest.y = 1f;
        if (Mathf.Abs(rest.z) < 0.001f)
            rest.z = 1f;

        float crouchT = JumpAnticipateDuration * Random.Range(0.78f, 1.24f);
        float air = JumpAirDuration * Random.Range(0.88f, 1.14f);
        float landT = JumpLandDuration * Random.Range(0.82f, 1.2f);
        mover.localScale = rest;

        if (!squash)
        {
            seq.AppendInterval(crouchT);
            seq.AppendCallback(() => onTakeOff?.Invoke());
            seq.Append(mover.DOJumpAnchorPos(dest, jumpPower, 1, air).SetEase(Ease.InOutQuad).SetUpdate(true));
            seq.AppendInterval(landT);
            seq.SetLink(mover.gameObject);
            seq.SetTarget(mover);
            return seq;
        }

        float crouchAmt = Random.Range(0.03f, 0.06f);
        float landAmt = Random.Range(0.035f, 0.065f);
        Vector3 crouch = new Vector3(rest.x * (1f + crouchAmt), rest.y * (1f - crouchAmt), rest.z);
        Vector3 land = new Vector3(rest.x * (1f + landAmt), rest.y * (1f - landAmt), rest.z);

        seq.Append(mover.DOScale(crouch, crouchT).SetEase(Ease.OutQuad).SetUpdate(true));
        seq.AppendCallback(() => onTakeOff?.Invoke());
        seq.Append(mover.DOJumpAnchorPos(dest, jumpPower, 1, air).SetEase(Ease.InOutQuad).SetUpdate(true));
        seq.Join(mover.DOScale(rest, 0.1f).SetEase(Ease.OutQuad).SetUpdate(true));
        seq.Insert(crouchT + air - landT * 0.65f,
            mover.DOScale(land, landT * 0.65f).SetEase(Ease.InQuad).SetUpdate(true));
        seq.Append(mover.DOScale(rest, landT).SetEase(Ease.OutQuad).SetUpdate(true));
        seq.SetLink(mover.gameObject);
        seq.SetTarget(mover);
        return seq;
    }

    public static Sequence UnstableThenFall(RectTransform mover, Vector2 dest, System.Action onPlunge = null)
    {
        return UnstableThenFall(mover, dest, onPlunge, null);
    }

    public static Sequence UnstableThenFall(RectTransform mover, Vector2 dest,
        System.Action onPlunge, System.Action onCovered)
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true).Pause();
        if (mover == null)
            return seq;

        Vector2 oldPivot = mover.pivot;
        SetPivotKeepVisual(mover, new Vector2(0.5f, 0f));
        Vector2 start = mover.anchoredPosition;
        seq.Append(mover.DOPunchRotation(new Vector3(0f, 0f, 18f), FallShakeDuration, 12, 0.6f).SetUpdate(true));
        seq.Join(mover.DOPunchAnchorPos(new Vector2(8f, 5f), FallShakeDuration, 12, 0.5f).SetUpdate(true));
        AppendFall(seq, mover, dest, start, FallOffDuration);
        float hold = Mathf.Max(0f, FallHideDelay - FallTotalDuration);
        if (hold > 0.001f)
            seq.AppendInterval(hold);
        seq.InsertCallback(FallPlungeDelay, () => onPlunge?.Invoke());
        seq.InsertCallback(FallHideDelay, () => onCovered?.Invoke());
        seq.OnKill(() =>
        {
            // 还看得见时回轴心会整个人往上窜一截，隐藏后再由 RestoreAvatarHome 复位。
            if (mover != null && mover.gameObject.activeInHierarchy)
                SetPivotKeepVisual(mover, oldPivot);
        });
        seq.SetLink(mover.gameObject, LinkBehaviour.KillOnDestroy);
        seq.SetTarget(mover);
        return seq;
    }

    public static Sequence FallOff(RectTransform mover, Vector2 dest, float duration = 0.95f)
    {
        Sequence seq = DOTween.Sequence().SetUpdate(true).Pause();
        if (mover == null)
            return seq;

        AppendFall(seq, mover, dest, mover.anchoredPosition, duration);
        seq.SetLink(mover.gameObject);
        seq.SetTarget(mover);
        return seq;
    }

    static void AppendFall(Sequence seq, RectTransform mover, Vector2 dest, Vector2 start, float duration = 0.95f)
    {
        if (seq == null || mover == null)
            return;

        duration = Mathf.Max(0.2f, duration);
        float side = dest.x >= start.x ? -1f : 1f;
        float lean = FallLeanDuration;
        float jumpPower = Mathf.Clamp((start.y - dest.y) * 0.16f, 18f, 36f);

        seq.Append(mover.DORotate(new Vector3(0f, 0f, 22f * side), lean).SetEase(Ease.OutQuad).SetUpdate(true));
        seq.Append(mover.DOJumpAnchorPos(dest, jumpPower, 1, duration).SetEase(Ease.InQuad).SetUpdate(true));
        seq.Join(mover.DORotate(new Vector3(0f, 0f, 148f * side), duration).SetEase(Ease.InQuad).SetUpdate(true));
    }

    public static void SetPivotKeepVisual(RectTransform rect, Vector2 pivot)
    {
        if (rect == null)
            return;
        Vector2 size = rect.rect.size;
        Vector2 delta = pivot - rect.pivot;
        Vector2 offset = new Vector2(
            delta.x * size.x * rect.localScale.x,
            delta.y * size.y * rect.localScale.y);
        rect.pivot = pivot;
        rect.anchoredPosition += offset;
    }

    public static Tween CountTo(TMP_Text label, int from, int to, float duration)
    {
        return CountTo(label, from, to, duration, null);
    }

    public static Tween CountTo(TMP_Text label, int from, int to, float duration, System.Func<int, string> format)
    {
        if (label == null)
            return null;
        int value = from;
        label.text = format != null ? format(value) : value.ToString();
        return DOTween.To(() => value, x =>
        {
            value = x;
            label.text = format != null ? format(value) : value.ToString();
        }, to, duration).SetEase(Ease.InExpo).SetUpdate(true).SetLink(label.gameObject);
    }

    public static void SetPaiziCoin(Transform root, int prize)
    {
        if (root == null)
            return;
        Transform paizi = FindNamed(root, "Paizi");
        Transform coin = paizi != null ? FindNamed(paizi, "CoinText") : null;
        TMP_Text label = coin != null ? coin.GetComponent<TMP_Text>() : null;
        if (label != null)
            label.text = prize.ToString();
    }

    static Transform FindNamed(Transform node, string name)
    {
        if (node == null)
            return null;
        if (node.name == name)
            return node;
        for (int i = 0; i < node.childCount; i++)
        {
            Transform found = FindNamed(node.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    public static string ResolveAnimation(SkeletonGraphic spine, params string[] candidates)
    {
        if (spine == null)
            return null;
        if (spine.AnimationState == null || spine.Skeleton == null)
            spine.Initialize(false);
        var data = spine.Skeleton != null ? spine.Skeleton.Data : null;
        if (data == null)
            return null;

        if (candidates != null)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                string name = candidates[i];
                if (!string.IsNullOrEmpty(name) && data.FindAnimation(name) != null)
                    return name;
            }
        }

        var animations = data.Animations;
        if (animations == null || animations.Count == 0)
            return null;
        if (candidates != null)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                string key = candidates[i];
                if (string.IsNullOrEmpty(key))
                    continue;
                for (int n = 0; n < animations.Count; n++)
                {
                    var anim = animations.Items[n];
                    if (anim != null && anim.Name != null
                        && anim.Name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return anim.Name;
                }
            }
        }

        return animations.Items[0] != null ? animations.Items[0].Name : null;
    }

    public static void PlayLoop(SkeletonGraphic spine, string animName, float trackTime = 0f)
    {
        string resolved = ResolveAnimation(spine, animName);
        if (!PrepareSpine(spine, resolved))
            return;

        var entry = spine.AnimationState.SetAnimation(0, resolved, true);
        if (entry == null)
            return;
        entry.MixDuration = 0f;
        if (entry.Animation != null && entry.Animation.Duration > 0.001f)
            entry.TrackTime = Mathf.Repeat(Mathf.Max(0f, trackTime), entry.Animation.Duration);
        spine.freeze = false;
        spine.Update(0f);
    }

    public static void PlayOnceAndHold(SkeletonGraphic spine, string animName)
    {
        string resolved = string.IsNullOrEmpty(animName)
            ? ResolveAnimation(spine)
            : ResolveAnimation(spine, animName, "Down");
        if (!PrepareSpine(spine, resolved))
            return;

        var entry = spine.AnimationState.SetAnimation(0, resolved, false);
        if (entry == null)
            return;
        entry.MixDuration = 0f;
        entry.TrackTime = 0f;
        entry.Complete += track => HoldTrackEnd(spine, track);
        spine.freeze = false;
        spine.Update(0f);
    }

    public static void HoldLastFrame(SkeletonGraphic spine, string animName)
    {
        string resolved = ResolveAnimation(spine, animName, "Down");
        if (!PrepareSpine(spine, resolved))
            return;

        var entry = spine.AnimationState.SetAnimation(0, resolved, false);
        if (entry == null)
            return;
        entry.MixDuration = 0f;
        HoldTrackEnd(spine, entry);
    }

    public static bool IsPlaying(SkeletonGraphic spine, string animName)
    {
        if (spine == null || spine.AnimationState == null || string.IsNullOrEmpty(animName))
            return false;
        var entry = spine.AnimationState.GetCurrent(0);
        return entry != null
            && entry.Animation != null
            && !string.IsNullOrEmpty(animName)
            && (entry.Animation.Name == animName
                || entry.Animation.Name.IndexOf(animName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            && entry.TimeScale > 0.001f;
    }

    static bool PrepareSpine(SkeletonGraphic spine, string animName)
    {
        if (spine == null || string.IsNullOrEmpty(animName))
            return false;
        if (!spine.isActiveAndEnabled)
            return false;
        if (spine.AnimationState == null)
            spine.Initialize(false);
        if (spine.AnimationState == null || spine.Skeleton == null || spine.Skeleton.Data == null)
            return false;
        return spine.Skeleton.Data.FindAnimation(animName) != null;
    }

    static void HoldTrackEnd(SkeletonGraphic spine, Spine.TrackEntry track)
    {
        if (spine == null || spine.AnimationState == null)
            return;
        var entry = track ?? spine.AnimationState.GetCurrent(0);
        if (entry == null || entry.Animation == null)
            return;

        entry.TimeScale = 0f;
        entry.TrackTime = entry.AnimationEnd;
        spine.AnimationState.Apply(spine.Skeleton);
        spine.Update(0f);
        spine.LateUpdate();
    }
}
