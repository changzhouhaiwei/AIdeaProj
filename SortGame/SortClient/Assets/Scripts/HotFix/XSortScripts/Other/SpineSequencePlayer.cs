using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// 按顺序播放 Spine 动画：中间段不循环，播完自动进入下一段；最后一段可单独设置是否循环。
/// 每次激活（OnEnable）都会按 Inspector 序列从头重新播放。
/// 若最后一段不循环，播完后触发 <see cref="onLastAnimationFinished"/>，可在此衔接下一组序列。
/// </summary>
public class SpineSequencePlayer : MonoBehaviour
{
    [SerializeField] SkeletonAnimation skeletonAnimation;
    [SerializeField] SkeletonGraphic skeletonGraphic;

    [Tooltip("按顺序的动画名称（字符串），与 Skeleton 资源中的 Animation 名一致")]
    [FormerlySerializedAs("animationSequence")]
    [SerializeField] string[] animationNames;
    [Tooltip("仅作用于序列中的最后一个动画")]
    [SerializeField] bool lastAnimationLoop;

    [SerializeField] UnityEvent onLastAnimationFinished;

    IAnimationStateComponent _animState;
    int _playToken;

    void Awake()
    {
        _animState = skeletonAnimation != null
            ? (IAnimationStateComponent)skeletonAnimation
            : skeletonGraphic;
    }

    void OnEnable()
    {
        Play();
    }

    void OnDestroy()
    {
        _playToken++;
    }

    /// <summary>使用 Inspector 中配置的动画名序列播放。</summary>
    public void Play()
    {
        Play(animationNames);
    }

    /// <summary>使用动画名字符串序列播放（至少一项）。可传入 <c>string[]</c> 或 List。</summary>
    public void Play(IReadOnlyList<string> animationNameSequence)
    {
        if (_animState == null)
        {
            Debug.LogError($"{nameof(SpineSequencePlayer)}: 未指定 SkeletonAnimation 或 SkeletonGraphic。", this);
            return;
        }

        var names = new List<string>();
        if (animationNameSequence != null)
        {
            for (int i = 0; i < animationNameSequence.Count; i++)
            {
                var n = animationNameSequence[i];
                if (!string.IsNullOrEmpty(n))
                    names.Add(n);
            }
        }

        if (names.Count == 0)
            return;

        _playToken++;
        int token = _playToken;

        var state = _animState.AnimationState;
        state.ClearTrack(0);

        TrackEntry lastEntry;
        if (names.Count == 1)
        {
            lastEntry = state.SetAnimation(0, names[0], lastAnimationLoop);
        }
        else
        {
            state.SetAnimation(0, names[0], false);
            for (int i = 1; i < names.Count - 1; i++)
                state.AddAnimation(0, names[i], false, 0f);
            lastEntry = state.AddAnimation(0, names[names.Count - 1], lastAnimationLoop, 0f);
        }

        if (!lastAnimationLoop && lastEntry != null)
        {
            lastEntry.Complete += te =>
            {
                if (token != _playToken)
                    return;
                onLastAnimationFinished?.Invoke();
            };
        }

        RefreshFrameZero();
    }

    void RefreshFrameZero()
    {
        if (skeletonAnimation != null)
            skeletonAnimation.Update(0f);
        else if (skeletonGraphic != null)
            skeletonGraphic.Update(0f);
    }
}
