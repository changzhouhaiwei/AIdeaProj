using System;
using System.Collections.Generic;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;

namespace FishXSortPlay
{
    /// <summary>
    /// 统一飞金币：屏幕中心附近随机散开 9 枚 CoinNew，
    /// 按间隔逐个出现并播 Spine；可配置原地停留与移动时长。
    /// 到达目标时复用一份 Coin_Epx：排队播放，播完再播下一次。
    /// </summary>
    public class CoinFlyAnim : MonoBehaviour
    {
        public const int CoinCount = 9;

        [SerializeField] private SkeletonGraphic[] coins = new SkeletonGraphic[CoinCount];
        [Tooltip("与金币平级的唯一到达特效（Coin_Epx），到达后排队播完再播下一次")]
        [SerializeField] private SkeletonGraphic arriveEffect;
        [SerializeField] private Transform target;
        [SerializeField] private string spineAnimName = "Coin_Loop";
        [SerializeField] private string arriveAnimName = "Coin_Epx";
        [SerializeField] private float scatterRadius = 180f;
        [SerializeField] private float appearInterval = 0.08f;
        [Tooltip("出现后原地停留时长（不移动，只播 Spine）")]
        [SerializeField] private float holdBeforeMove = 0.2f;
        [Tooltip("金币飞向目标的移动时长")]
        [SerializeField] private float moveDuration = 0.8f;
        [SerializeField] private float startScale = 1f;
        [SerializeField] private float endScale = 0.55f;
        [SerializeField] private Ease flyEase = Ease.InOutQuad;
        [Tooltip("找不到到达动画时长时的回退值")]
        [SerializeField] private float fallbackArriveDuration = 0.37f;

        private Sequence _seq;
        private readonly List<Tween> _coinTweens = new List<Tween>(CoinCount);
        private RectTransform _rect;
        private Tween _arriveTween;
        private Vector3 _arriveTargetLocal;
        private int _arrivePending;
        private bool _arrivePlaying;

        /// <summary>每次真正开始播到达特效时回调（含最后一次打断重播）。</summary>
        public Action OnArrivePlay;

        public bool IsPlaying => _seq != null && _seq.IsActive() && _seq.IsPlaying();

        private void Awake()
        {
            _rect = transform as RectTransform;
            HideAll();
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Stop();
        }

        public void SetTarget(Transform flyTarget)
        {
            target = flyTarget;
        }

        /// <summary>
        /// 播放飞金币。
        /// overrideTarget：飞向目标；overrideStart：初始散开中心（默认屏幕中心）。
        /// </summary>
        public void StartFly(Action onComplete = null, Transform overrideTarget = null, Transform overrideStart = null)
        {
            if (coins == null || coins.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Stop();

            var flyTarget = overrideTarget != null ? overrideTarget : target;
            if (flyTarget == null)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 centerLocal = overrideStart != null
                ? transform.InverseTransformPoint(overrideStart.position)
                : GetScreenCenterLocal();
            Vector3 targetLocal = transform.InverseTransformPoint(flyTarget.position);
            float hold = Mathf.Max(0f, holdBeforeMove);
            float move = Mathf.Max(0.01f, moveDuration);

            int count = Mathf.Min(CoinCount, coins.Length);

            for (int i = 0; i < count; i++)
            {
                var coin = coins[i];
                if (coin == null) continue;

                Vector2 offset = UnityEngine.Random.insideUnitCircle * scatterRadius;
                var rt = coin.rectTransform;
                rt.localPosition = centerLocal + new Vector3(offset.x, offset.y, 0f);
                rt.localScale = Vector3.one * startScale;
                coin.gameObject.SetActive(false);
            }

            HideArriveEffect();
            _arrivePending = 0;
            _arrivePlaying = false;
            _arriveTargetLocal = targetLocal;

            _seq = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _seq.SetUpdate(true);

            for (int i = 0; i < count; i++)
            {
                int idx = i;
                bool isLast = (i == count - 1);
                float delay = i * appearInterval;
                _seq.InsertCallback(delay, () => AppearAndFly(idx, targetLocal, hold, move, isLast));
            }

            float arriveDur = ResolveArriveDuration();
            float totalTime = (count - 1) * appearInterval + hold + move + arriveDur + 0.05f;
            _seq.InsertCallback(totalTime, () =>
            {
                HideAll();
                onComplete?.Invoke();
            });
        }

        public void Stop()
        {
            if (_seq != null && _seq.IsActive())
            {
                _seq.Kill(false);
            }
            _seq = null;

            for (int i = 0; i < _coinTweens.Count; i++)
            {
                if (_coinTweens[i] != null && _coinTweens[i].IsActive())
                    _coinTweens[i].Kill(false);
            }
            _coinTweens.Clear();

            if (_arriveTween != null && _arriveTween.IsActive())
                _arriveTween.Kill(false);
            _arriveTween = null;
            _arrivePending = 0;
            _arrivePlaying = false;

            HideAll();
        }

        public void Hide()
        {
            Stop();
        }

        private void AppearAndFly(int index, Vector3 targetLocal, float hold, float move, bool isLast)
        {
            if (coins == null || index < 0 || index >= coins.Length) return;
            var coin = coins[index];
            if (coin == null) return;

            coin.gameObject.SetActive(true);
            coin.enabled = true;
            PlaySpineAnim(coin, spineAnimName, false);

            var rt = coin.rectTransform;
            var pair = DOTween.Sequence().SetUpdate(true);
            if (hold > 0f)
                pair.AppendInterval(hold);

            pair.Append(rt.DOLocalMove(targetLocal, move).SetEase(flyEase).SetUpdate(true));
            pair.Join(rt.DOScale(endScale, move).SetEase(flyEase).SetUpdate(true));
            pair.OnComplete(() =>
            {
                if (coin != null)
                    coin.gameObject.SetActive(false);

                if (isLast)
                    PlayFinalArriveEffect(targetLocal);
                else
                    EnqueueArriveEffect(targetLocal);
            });

            _coinTweens.Add(pair);
        }

        private void PlayFinalArriveEffect(Vector3 targetLocal)
        {
            if (arriveEffect == null) return;

            _arrivePending = 0;
            if (_arriveTween != null && _arriveTween.IsActive())
                _arriveTween.Kill(false);
            _arriveTween = null;

            _arriveTargetLocal = targetLocal;
            _arrivePlaying = true;

            var rt = arriveEffect.rectTransform;
            rt.localPosition = _arriveTargetLocal;
            rt.localScale = Vector3.one;

            arriveEffect.gameObject.SetActive(true);
            arriveEffect.enabled = true;
            PlaySpineAnim(arriveEffect, arriveAnimName, false);
            OnArrivePlay?.Invoke();

            float dur = ResolveArriveDuration();
            _arriveTween = DOVirtual.DelayedCall(dur, () =>
            {
                _arrivePlaying = false;
                HideArriveEffect();
            }, true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void EnqueueArriveEffect(Vector3 targetLocal)
        {
            if (arriveEffect == null) return;

            _arriveTargetLocal = targetLocal;
            _arrivePending++;
            if (!_arrivePlaying)
                PlayNextArriveEffect();
        }

        private void PlayNextArriveEffect()
        {
            if (arriveEffect == null || _arrivePending <= 0)
            {
                _arrivePlaying = false;
                HideArriveEffect();
                return;
            }

            _arrivePending--;
            _arrivePlaying = true;

            var rt = arriveEffect.rectTransform;
            rt.localPosition = _arriveTargetLocal;
            rt.localScale = Vector3.one;

            arriveEffect.gameObject.SetActive(true);
            arriveEffect.enabled = true;
            PlaySpineAnim(arriveEffect, arriveAnimName, false);
            OnArrivePlay?.Invoke();

            if (_arriveTween != null && _arriveTween.IsActive())
                _arriveTween.Kill(false);

            float dur = ResolveArriveDuration();
            _arriveTween = DOVirtual.DelayedCall(dur, () =>
            {
                _arrivePlaying = false;
                if (_arrivePending > 0)
                    PlayNextArriveEffect();
                else
                    HideArriveEffect();
            }, true).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void HideArriveEffect()
        {
            if (arriveEffect == null) return;

            if (arriveEffect.AnimationState != null)
                arriveEffect.AnimationState.ClearTracks();
            if (arriveEffect.Skeleton != null)
            {
                arriveEffect.Skeleton.SetToSetupPose();
                arriveEffect.Update(0f);
            }
            arriveEffect.gameObject.SetActive(false);
        }

        private float ResolveArriveDuration()
        {
            if (arriveEffect != null && arriveEffect.SkeletonDataAsset != null)
            {
                var data = arriveEffect.SkeletonDataAsset.GetSkeletonData(false);
                if (data != null)
                {
                    var anim = !string.IsNullOrEmpty(arriveAnimName)
                        ? data.FindAnimation(arriveAnimName)
                        : null;
                    if (anim != null && anim.Duration > 0.01f)
                        return anim.Duration;
                }
            }

            return fallbackArriveDuration;
        }

        private void PlaySpineAnim(SkeletonGraphic graphic, string animName, bool loop)
        {
            if (!graphic.IsValid)
                graphic.Initialize(true);

            if (graphic.AnimationState == null || graphic.Skeleton == null) return;

            graphic.AnimationState.ClearTracks();
            graphic.Skeleton.SetToSetupPose();

            Spine.Animation anim = null;
            if (!string.IsNullOrEmpty(animName))
                anim = graphic.Skeleton.Data.FindAnimation(animName);
            if (anim == null && graphic.Skeleton.Data.Animations.Count > 0)
                anim = graphic.Skeleton.Data.Animations.Items[0];

            if (anim == null) return;

            var entry = graphic.AnimationState.SetAnimation(0, anim, loop);
            entry.MixDuration = 0f;
            entry.TrackTime = 0f;
            entry.AnimationStart = 0f;

            graphic.Update(0f);
            graphic.LateUpdate();
        }

        private void HideAll()
        {
            if (coins != null)
            {
                for (int i = 0; i < coins.Length; i++)
                {
                    var coin = coins[i];
                    if (coin == null) continue;

                    if (coin.AnimationState != null)
                        coin.AnimationState.ClearTracks();
                    if (coin.Skeleton != null)
                    {
                        coin.Skeleton.SetToSetupPose();
                        coin.Update(0f);
                    }
                    coin.gameObject.SetActive(false);
                }
            }

            HideArriveEffect();
        }

        private Vector3 GetScreenCenterLocal()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                var canvasRt = canvas.transform as RectTransform;
                if (canvasRt != null)
                {
                    Vector3 worldCenter = canvasRt.TransformPoint(canvasRt.rect.center);
                    return transform.InverseTransformPoint(worldCenter);
                }
            }

            if (_rect != null && _rect.parent is RectTransform parentRt)
            {
                Vector3 worldCenter = parentRt.TransformPoint(parentRt.rect.center);
                return transform.InverseTransformPoint(worldCenter);
            }

            return Vector3.zero;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (coins == null || coins.Length != CoinCount)
            {
                var resized = new SkeletonGraphic[CoinCount];
                if (coins != null)
                {
                    for (int i = 0; i < Mathf.Min(coins.Length, CoinCount); i++)
                        resized[i] = coins[i];
                }
                coins = resized;
            }
        }
#endif
    }
}
