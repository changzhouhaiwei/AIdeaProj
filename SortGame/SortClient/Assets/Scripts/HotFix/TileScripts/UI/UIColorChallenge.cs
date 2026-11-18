using System;
using System.Collections.Generic;
using FishFramework;
using FishTilePlay;
using I2.Loc;
using Spine.Unity;
using TileGameCore;
using TileGamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 颜色关进关界面。走 UI 框架打开，播完再生成棋子，不走 TipsDlg。
/// </summary>
public class UIColorChallenge : UIPanel, IUIOpen<string>
{
    const string SpriteDir = "Assets/CABRes/Sprites/ColorChallenge/";
    const float FallbackAnimDuration = 2.5f;

    static readonly Dictionary<int, Color> progressParticleBaselines = new Dictionary<int, Color>();
    static Action pendingFinished;

    Action finished;
    bool raycastDisabled;
    Timer greyProgressWatch;

    public override PanelLayer Layer { get; set; }
    protected override string PrefabPath => "Assets/CABRes/Prefabs/UIMain/ColorChallenge.prefab";

    public static void Play(string color, Action onFinished)
    {
        pendingFinished = onFinished;
        GameModule.UI.OpenPanel<UIColorChallenge, string>(PanelLayer.Tips, PanelOpenType.Single, color ?? "");
    }

    public void OnOpen(string color)
    {
        finished = pendingFinished;
        pendingFinished = null;

        ApplyPictures(gameObject, color);
        ApplyTexts(gameObject.transform, color);
        ReplaySpine(gameObject);
        SetParticleSortOrder(gameObject);
        RaycastController.Disable();
        raycastDisabled = true;
        float duration = PlayAnimator(gameObject);
        WatchGreyProgress();
        Delay(duration, CloseWhenDone, true);
    }

    protected override void OnInternalCreating()
    {
        base.OnInternalCreating();
        canvas.additionalShaderChannels =
            AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            SetLayer(rectTransform, uiLayer);
    }

    protected override void PlayOpenAnim(Action onFinish = null)
    {
        animState = UIPanelAnimState.Idle;
        onFinish?.Invoke();
    }

    protected override void PlayOpenSound()
    {
    }

    protected override void OnDestroyed()
    {
        if (raycastDisabled)
        {
            RaycastController.Enable();
            raycastDisabled = false;
        }

        finished = null;
        greyProgressWatch = null;
        base.OnDestroyed();
    }

    void WatchGreyProgress()
    {
        Transform grey = FindNamedChild(gameObject.transform, "Picture_Grey");
        BrushStrokeDissolveAnim dissolve = grey != null ? grey.GetComponent<BrushStrokeDissolveAnim>() : null;
        if (dissolve == null)
            return;

        float previous = dissolve.progress;
        greyProgressWatch = Interval(0.02f, _ =>
        {
            if (dissolve == null)
            {
                StopGreyProgressWatch();
                return;
            }

            float now = dissolve.progress;
            if (previous <= 0.001f && now > previous)
            {
                StopGreyProgressWatch();
                AudioClip clip = AudioController.AudioClips != null ? AudioController.AudioClips.colorChallenge : null;
                if (clip != null)
                    AudioController.PlaySound(clip);
                return;
            }

            previous = now;
        }, null, false, false);
    }

    void StopGreyProgressWatch()
    {
        Timer watch = greyProgressWatch;
        greyProgressWatch = null;
        if (watch != null)
            Unschedule(watch);
    }

    void CloseWhenDone()
    {
        Action callback = finished;
        finished = null;
        if (GameModule.UI.ExistPanel(panelId))
            GameModule.UI.ClosePanel(panelId, callback);
        else
            callback?.Invoke();
    }

    static void SetLayer(Transform node, int layer)
    {
        node.gameObject.layer = layer;
        for (int i = 0; i < node.childCount; i++)
            SetLayer(node.GetChild(i), layer);
    }

    static float PlayAnimator(GameObject node)
    {
        Animator animator = node != null ? node.GetComponent<Animator>() : null;
        AnimationClip[] clips = animator != null ? animator.runtimeAnimatorController?.animationClips : null;
        if (clips == null)
            return FallbackAnimDuration;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
                continue;

            animator.Rebind();
            animator.Play(clip.name, 0, 0f);
            return Mathf.Max(clip.length, 0.1f);
        }

        return FallbackAnimDuration;
    }

    static readonly Dictionary<string, string> LabelFontColorHex = new Dictionary<string, string>
    {
        { "blue", "436bff" },
        { "purple", "b93eff" },
        { "green", "07b01b" },
        { "pink", "ff5ba4" },
        { "yellow", "f9cb14" },
        { "red", "f9143f" },
    };

    static void ApplyTexts(Transform root, string color)
    {
        TMP_Text colorLabel = FindNamedChild(root, "color")?.GetComponent<TMP_Text>();
        string token = (color ?? "").Trim().ToLowerInvariant();
        SetLocalized(colorLabel, () => token.Length == 0 ? "" : I2LocalizeMgr.GetString(token));
        ApplyLabelFontColor(colorLabel, token);

        var cfg = LevelDynamicConfigProvider.GetLevelConfig(
            GameDirector.levelController.GetLoadedLevelIndex() + 1);
        if (cfg == null)
            return;
        if (ActivityData.IsCoinRewardLevelEnabled() && cfg.Coinlevel == 1)
            return;

        int textIdx = Mathf.Clamp(cfg.Difficulty, 0, 2);
        string passRate = cfg.PassRate ?? "";
        SetLocalized(FindNamedChild(root, "perLb3")?.GetComponent<TMP_Text>(), () =>
        {
            string template = I2LocalizeMgr.GetString("EnterTheLevelTips" + (textIdx + 1));
            return template.Replace("{passRate}", passRate).Replace("{0}", passRate);
        });
    }

    static void ApplyLabelFontColor(TMP_Text tmp, string token)
    {
        if (tmp == null || !LabelFontColorHex.TryGetValue(token, out string hex))
            return;
        if (!ColorUtility.TryParseHtmlString("#" + hex, out Color fontColor))
            return;

        fontColor.a = tmp.color.a;
        tmp.color = fontColor;
    }

    static void SetLocalized(TMP_Text tmp, Func<string> getter)
    {
        if (tmp == null)
            return;

        var localize = tmp.GetComponent<Localize>();
        if (localize != null)
            localize.enabled = false;
        if (tmp.GetComponent<LocalizedTMPRefresher>() == null)
            tmp.gameObject.AddComponent<LocalizedTMPRefresher>();
        LocalizedTMPRefresher.SetText(tmp, getter);
    }

    static void ApplyPictures(GameObject root, string color)
    {
        if (root == null || string.IsNullOrEmpty(color))
            return;

        string token = ToSpriteColorToken(color);
        SetPictureSprite(root.transform, "Picture_Color", "Color_" + token);
        SetPictureSprite(root.transform, "Picture_Grey", "Grey_" + token);
        ApplyProgressParticleColor(root.transform, token);
    }

    static string ToSpriteColorToken(string color)
    {
        string trimmed = color.Trim();
        if (trimmed.Length == 0)
            return trimmed;
        if (trimmed.Length == 1)
            return char.ToUpperInvariant(trimmed[0]).ToString();
        return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1).ToLowerInvariant();
    }

    static void SetPictureSprite(Transform root, string nodeName, string spriteName)
    {
        Transform node = FindNamedChild(root, nodeName);
        if (node == null)
            return;

        Image image = node.GetComponent<Image>();
        if (image == null)
            return;

        Sprite sprite = UILoadHelper.LoadAsset<Sprite>(SpriteDir + spriteName + ".png");
        if (sprite != null)
            image.sprite = sprite;
    }

    static void ApplyProgressParticleColor(Transform root, string token)
    {
        progressParticleBaselines.Clear();
        Transform particleRoot = FindNamedChild(root, "UIProgress_Par2");
        if (particleRoot == null)
            return;

        Color targetHue = ProgressParticleHue(token);
        ParticleSystem[] systems = particleRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem system = systems[i];
            if (system == null)
                continue;

            int id = system.GetInstanceID();
            ParticleSystem.MainModule main = system.main;
            if (!progressParticleBaselines.TryGetValue(id, out Color baseline))
            {
                baseline = main.startColor.color;
                progressParticleBaselines[id] = baseline;
            }

            main.startColor = ShiftHue(baseline, targetHue);
        }
    }

    static Color ProgressParticleHue(string token)
    {
        switch (token)
        {
            case "Red":
                return Color.red;
            case "Yellow":
                return Color.yellow;
            case "Green":
                return Color.green;
            case "Pink":
                return new Color(1f, 0.35f, 0.7f);
            case "Purple":
                return new Color(0.62f, 0.28f, 1f);
            default:
                return new Color(0.2783019f, 0.76846063f, 1f);
        }
    }

    static Color ShiftHue(Color baseline, Color targetHue)
    {
        Color.RGBToHSV(baseline, out _, out float saturation, out float value);
        Color.RGBToHSV(targetHue, out float hue, out _, out _);
        Color shifted = Color.HSVToRGB(hue, saturation, value);
        shifted.a = baseline.a;
        return shifted;
    }

    static void ReplaySpine(GameObject root)
    {
        if (root == null)
            return;

        SkeletonGraphic[] spines = root.GetComponentsInChildren<SkeletonGraphic>(true);
        for (int i = 0; i < spines.Length; i++)
        {
            SkeletonGraphic spine = spines[i];
            if (spine == null)
                continue;

            if (!spine.IsValid)
                spine.Initialize(true);
            if (spine.AnimationState == null || spine.Skeleton == null)
                continue;

            spine.AnimationState.ClearTracks();
            spine.Skeleton.SetToSetupPose();

            string animName = spine.startingAnimation;
            Spine.Animation anim = !string.IsNullOrEmpty(animName)
                ? spine.Skeleton.Data.FindAnimation(animName)
                : null;
            if (anim == null && spine.Skeleton.Data.Animations.Count > 0)
                anim = spine.Skeleton.Data.Animations.Items[0];
            if (anim == null)
                continue;

            Spine.TrackEntry entry = spine.AnimationState.SetAnimation(0, anim, spine.startingLoop);
            entry.MixDuration = 0f;
            entry.TrackTime = 0f;
            entry.AnimationStart = 0f;
            spine.Update(0f);
            spine.LateUpdate();
        }
    }

    static Transform FindNamedChild(Transform root, string nodeName)
    {
        if (root == null)
            return null;
        if (root.name == nodeName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamedChild(root.GetChild(i), nodeName);
            if (found != null)
                return found;
        }

        return null;
    }
}
