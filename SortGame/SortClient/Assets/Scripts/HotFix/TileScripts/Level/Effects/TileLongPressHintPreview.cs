using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// 预览用：显示/隐藏场景常驻特效。
    /// </summary>
    public class TileLongPressHintPreview : MonoBehaviour
    {
        [SerializeField] bool playOnStart;

        void Start()
        {
            if (playOnStart)
                Play();
        }

        [ContextMenu("Play Preview")]
        public void Play()
        {
            var eff = TileLongPressHintEffect.Instance;
            if (eff == null)
            {
                Debug.LogWarning("[TileLongPressHintPreview] 场景中没有常驻 TileLongPressHintEffect（应挂在 Dock 下）");
                return;
            }

            eff.transform.position = transform.position;
            eff.transform.localScale = Vector3.one * transform.lossyScale.x;
            eff.gameObject.SetActive(true);
            eff.ApplyParamsNow();
        }

        [ContextMenu("Stop Preview")]
        public void Stop()
        {
            TileLongPressHintEffect.Instance?.Hide();
        }

        void OnDisable()
        {
            Stop();
        }
    }
}
