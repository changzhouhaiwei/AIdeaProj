using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TButton : Button
{
    public bool clickDoTween = true;
    public float downScale = 0.96f;
    public float upScale = 1.05f;

    /// <summary>
    /// Optional transform that receives press scale. When set, show/hide can own
    /// this button's root scale without fighting click feedback (and vice versa).
    /// </summary>
    [SerializeField] Transform scaleTarget;

    public string audioId = "1001";

    private bool isLongPressing;
    private const float LONGPRESS_TIME = 0.5f;

    public Action onPointerDown; // 按下
    public Action onPointerUp; // 抬起
    public Action onLongClickHandler; // 长按

    private static Material sharedGreyMaterial;

    /// <summary>
    /// Disabled 时是否套灰色材质。个别按钮只需要不可点，保持彩色时关掉。
    /// </summary>
    public bool disableToGray = true;

    /// <summary>
    /// Visual "normal" scale used by press feedback. Must not be captured while an
    /// appear/unlock tween has the transform near zero (e.g. PUUIActor.Activate).
    /// </summary>
    private Vector3 restingScale = Vector3.one;
    private bool isPressTweening;
    private const float MinRestingAxis = 0.5f;

    private static Material GreyMaterial
    {
        get
        {
            if (sharedGreyMaterial == null)
            {
                var request = xasset.Asset.Load("Assets/CABRes/Shaders/Gray.shader", typeof(Shader));
                sharedGreyMaterial = new Material(request.asset as Shader);
            }
            return sharedGreyMaterial;
        }
    }

    /// <summary>Transform that press tweens. Defaults to this button's transform.</summary>
    public Transform ScaleTransform => scaleTarget != null ? scaleTarget : transform;

    /// <summary>True when press scale is isolated from root show/hide scale.</summary>
    public bool UsesExternalScaleTarget => scaleTarget != null && scaleTarget != transform;

    protected override void Start()
    {
        base.Start();
        transition = Transition.None;
        TryAdoptRestingScale(ScaleTransform.localScale);
    }

    private static bool IsPlausibleRestingScale(Vector3 scale)
    {
        return scale.x >= MinRestingAxis && scale.y >= MinRestingAxis && scale.z >= MinRestingAxis;
    }

    private void TryAdoptRestingScale(Vector3 scale)
    {
        if (IsPlausibleRestingScale(scale))
            restingScale = scale;
    }

    private void KillClickTween()
    {
        DOTween.Kill(this, false);
        isPressTweening = false;
    }

    /// <summary>
    /// Stop press feedback without changing resting scale.
    /// Use before external show/hide scale tweens so restore cannot fight hide-to-zero.
    /// When press uses an isolated visual child, optionally snap that child back to resting.
    /// </summary>
    public void CancelPressTween(bool restoreRestingScale = false)
    {
        KillClickTween();
        if (restoreRestingScale || UsesExternalScaleTarget)
            ScaleTransform.localScale = restingScale;
    }

    /// <summary>
    /// Graphic used for disable-to-gray. Prefer targetGraphic so PressScaleVisual
    /// hierarchy moves still gray the visible Image (Button.image is same-GO only).
    /// </summary>
    private Image GrayTargetImage
    {
        get
        {
            if (targetGraphic is Image targetImage)
                return targetImage;
            return image;
        }
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);

        switch (state)
        {
            case SelectionState.Disabled:
                if (disableToGray)
                {
                    var grayImage = GrayTargetImage;
                    if (grayImage == null)
                        break;
                    grayImage.material = GreyMaterial;
                }
                break;
            default:
                if (disableToGray)
                {
                    var grayImage = GrayTargetImage;
                    if (grayImage != null)
                        grayImage.material = null;
                }
                break;
        }
    }

    protected override void OnDisable()
    {
        // Mid-press disable/hide must not leave the visual stuck at downScale.
        if (isPressTweening)
            CancelPressTween(restoreRestingScale: true);
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        KillClickTween();
        ScaleTransform.DOKill();
        CancelInvoke();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (interactable)
        {
            if (clickDoTween)
                TryStartPressTween();

            Invoke(nameof(ExecuteLongClickHandler), LONGPRESS_TIME);
            onPointerDown?.Invoke();
        }
    }

    private void TryStartPressTween()
    {
        var scaleXf = ScaleTransform;

        // Root intentionally hidden via scale (show/hide). Do not snap visible or
        // run press feedback while the entrance is scaled away.
        if (!IsPlausibleRestingScale(transform.localScale))
            return;

        // Press target near-zero (should not happen for isolated visuals).
        if (!isPressTweening && !IsPlausibleRestingScale(scaleXf.localScale))
            return;

        bool externalTweening = DOTween.IsTweening(scaleXf);

        // Only re-adopt when settled. Mid appear / ping-pong / hide must not poison resting.
        if (!isPressTweening && !externalTweening)
            TryAdoptRestingScale(scaleXf.localScale);

        KillClickTween();

        // Only kill tweens on the press target. Never DOKill the root when scaleTarget
        // is set — that would cancel show/hide / tip bounce on the shared root.
        if (DOTween.IsTweening(scaleXf))
        {
            scaleXf.DOKill(false);
            scaleXf.localScale = restingScale;
        }

        isPressTweening = true;
        scaleXf.DOScale(downScale * restingScale, 0.06f)
            .SetId(this)
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        // Always finish press scale even if interactable flipped mid-press.
        if (clickDoTween && isPressTweening)
        {
            var scaleXf = ScaleTransform;
            KillClickTween();
            isPressTweening = true;
            DOTween.Sequence()
                .Append(scaleXf.DOScale(upScale * restingScale, 0.12f))
                .Append(scaleXf.DOScale(1.0f * restingScale, 0.06f))
                .SetId(this)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => isPressTweening = false)
                .OnKill(() => isPressTweening = false);
        }

        if (interactable)
        {
            isLongPressing = false;
            CancelInvoke(nameof(ExecuteLongClickHandler));
            onPointerUp?.Invoke();
        }
        else
        {
            isLongPressing = false;
            CancelInvoke(nameof(ExecuteLongClickHandler));
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        if (interactable && !string.IsNullOrEmpty(audioId))
        {
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        if (isLongPressing)
        {
            isLongPressing = false;
            CancelInvoke(nameof(ExecuteLongClickHandler));
        }
    }

    private void ExecuteLongClickHandler()
    {
        isLongPressing = true;
        onLongClickHandler?.Invoke();
    }

    public void SetAudioId(string value)
    {
        audioId = value;
    }

    /// <summary>
    /// Sync resting scale used by click tween. Call when external code changes the
    /// press target to a visible resting size. Near-zero values are ignored so
    /// hide-by-scale cannot poison press. When <see cref="scaleTarget"/> is set,
    /// this updates the visual child only — root show/hide scale is left alone.
    /// </summary>
    public void SetRestingScale(Vector3 scale)
    {
        KillClickTween();
        if (IsPlausibleRestingScale(scale))
            restingScale = scale;
        ScaleTransform.localScale = scale;
    }
}
