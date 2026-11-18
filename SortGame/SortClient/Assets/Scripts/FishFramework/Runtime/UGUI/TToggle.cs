using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FishFramework
{
    public class TToggle : Toggle
    {
        public string AudioIdOne = "1058";
        public string AudioCancle = "2167";
        private bool musicPlayState = true;

        /// <summary>
        /// Same press-scale feedback as TButton. Off by default so existing toggles stay unchanged.
        /// </summary>
        public bool clickDoTween = false;
        public float downScale = 0.96f;
        public float upScale = 1.05f;

        [SerializeField] private Color _NoSelectColor = Color.white;

        public Color NoSelectColor
        {
            get { return _NoSelectColor; }
            set
            {
                _NoSelectColor = value;
                SetLabelColor(isOn ? SelectColor : NoSelectColor);
            }
        }

        [SerializeField] private Color _SelectColor = Color.black;

        public Color SelectColor
        {
            get { return _SelectColor; }
            set
            {
                _SelectColor = value;
                SetLabelColor(isOn ? SelectColor : NoSelectColor);
            }
        }

        [SerializeField] private TextMeshProUGUI _ToggleLabel;

        public TextMeshProUGUI ToggleLabel
        {
            get { return _ToggleLabel; }
            set
            {
                _ToggleLabel = value;
                SetLabelColor(isOn ? SelectColor : NoSelectColor);
            }
        }

        [SerializeField] private GameObject _NoSelectNode = null;

        public GameObject NoSelectNode
        {
            get { return _NoSelectNode; }
            set
            {
                _NoSelectNode = value;
                SetSelectShow();
            }
        }

        [SerializeField] private GameObject _SelectNode = null;

        public GameObject SelectNode
        {
            get { return _SelectNode; }
            set
            {
                _SelectNode = value;
                SetSelectShow();
            }
        }

        private Vector3 restingScale = Vector3.one;
        private bool isPressTweening;
        private const float MinRestingAxis = 0.5f;

        protected override void Awake()
        {
            base.Awake();
            onValueChanged.AddListener(isOn => OnToggleClick());
        }

        protected override void Start()
        {
            base.Start();
            if (clickDoTween)
            {
                transition = Transition.None;
            }

            TryAdoptRestingScale(transform.localScale);
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

        public void CancelPressTween(bool restoreRestingScale = false)
        {
            KillClickTween();
            if (restoreRestingScale)
                transform.localScale = restingScale;
        }

        /// <summary>
        /// Sync resting scale used by click tween. Near-zero values are ignored.
        /// </summary>
        public void SetRestingScale(Vector3 scale)
        {
            KillClickTween();
            if (IsPlausibleRestingScale(scale))
                restingScale = scale;
            transform.localScale = scale;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (interactable && clickDoTween)
                TryStartPressTween();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            // Finish press scale even if interactable flipped mid-press.
            if (clickDoTween && isPressTweening)
            {
                KillClickTween();
                isPressTweening = true;
                DOTween.Sequence()
                    .Append(transform.DOScale(upScale * restingScale, 0.12f))
                    .Append(transform.DOScale(1.0f * restingScale, 0.06f))
                    .SetId(this)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                    .OnComplete(() => isPressTweening = false)
                    .OnKill(() => isPressTweening = false);
            }
        }

        protected override void OnDisable()
        {
            if (isPressTweening)
                CancelPressTween(restoreRestingScale: true);
            base.OnDisable();
        }

        private void TryStartPressTween()
        {
            if (!isPressTweening && !IsPlausibleRestingScale(transform.localScale))
                return;

            bool externalTweening = DOTween.IsTweening(transform);
            if (!isPressTweening && !externalTweening)
                TryAdoptRestingScale(transform.localScale);

            KillClickTween();

            if (DOTween.IsTweening(transform))
            {
                transform.DOKill(false);
                transform.localScale = restingScale;
            }

            isPressTweening = true;
            transform.DOScale(downScale * restingScale, 0.06f)
                .SetId(this)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            KillClickTween();
            transform.DOKill();
        }

        public void OnToggleClick()
        {
            if (musicPlayState)
            {
                if (isOn)
                {
                    if (!string.IsNullOrEmpty(AudioIdOne))
                    {
                    }
                }
                else
                {
                }
            }

            SetLabelColor(isOn ? SelectColor : NoSelectColor);
            SetSelectShow();
        }

        public void SetLabelColor(Color color)
        {
            if (ToggleLabel != null)
            {
                ToggleLabel.color = color;
            }
        }

        public void SetSelectShow()
        {
            if (SelectNode != null)
            {
                SelectNode.SetActive(isOn);
            }

            if (NoSelectNode != null)
            {
                NoSelectNode.SetActive(!isOn);
            }
        }

        public void SetAudioId(string audioId, string cancelAudioId)
        {
            if (!string.IsNullOrEmpty(audioId))
                AudioIdOne = audioId;

            if (!string.IsNullOrEmpty(cancelAudioId))
                AudioCancle = cancelAudioId;
        }

        public void CloseMusic()
        {
            musicPlayState = false;
        }

        public void OpenMusic()
        {
            musicPlayState = true;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetLabelColor(isOn ? SelectColor : NoSelectColor);
            SetSelectShow();
        }
#endif
    }
}
