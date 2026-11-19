using DG.Tweening;
using XSortGameCore;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Tween = XSortGameCore.Tween;

namespace FishXSortPlay
{
    /// <summary>
    /// 失败页主题特效入口。onPlay 拖本脚本的 PlayIce / PlayWood / PlayBubble。
    /// </summary>
    public class ThemeFailEffect : MonoBehaviour
    {
        static readonly int ProgressId = Shader.PropertyToID("_Progress");
        const Ease IceEase = Ease.OutCubic;

        [SerializeField] UnityEvent onPlay = new UnityEvent();

        Image overlay;
        TweenClip progressTween;
        Material materialInstance;

        public GameObject SourcePrefab { get; private set; }
        public float LastDuration { get; private set; }

        public void BindSourcePrefab(GameObject prefab)
        {
            SourcePrefab = prefab;
        }

        public void Play(float duration)
        {
            LastDuration = duration;
            gameObject.SetActive(true);
            onPlay?.Invoke();
        }

        public void PlayIce()
        {
            if (!EnsureProgressMaterial())
            {
                return;
            }

            Stop();
            SetProgress(0f);
            float t = 0f;
            var tween = DOTween.To(
                    () => t,
                    value =>
                    {
                        t = value;
                        SetProgress(value);
                    },
                    1f,
                    Mathf.Max(0f, LastDuration))
                .SetEase(IceEase)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            progressTween = Tween.Wrap(tween);
        }

        public void PlayWood()
        {
        }

        public void PlayBubble()
        {
        }

        public void Stop()
        {
            if (progressTween != null && progressTween.IsActive)
            {
                progressTween.Kill();
            }

            progressTween = null;
        }

        public void ResetProgress()
        {
            Stop();
            if (EnsureProgressMaterial())
            {
                SetProgress(0f);
            }
        }

        void OnDestroy()
        {
            Stop();
            if (materialInstance != null)
            {
                Destroy(materialInstance);
                materialInstance = null;
            }
        }

        bool EnsureProgressMaterial()
        {
            if (overlay == null)
            {
                overlay = GetComponent<Image>();
            }

            if (overlay == null)
            {
                return false;
            }

            if (materialInstance != null)
            {
                return true;
            }

            Material source = overlay.material;
            if (source == null || !source.HasProperty(ProgressId))
            {
                return false;
            }

            materialInstance = Instantiate(source);
            overlay.material = materialInstance;
            return true;
        }

        void SetProgress(float progress)
        {
            if (materialInstance == null)
            {
                return;
            }

            materialInstance.SetFloat(ProgressId, progress);
            overlay.SetMaterialDirty();
        }
    }
}
