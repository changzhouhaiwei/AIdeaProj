using DG.Tweening;
using Spine.Unity;
using TileGameCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PuppyAdventureIcon : MonoBehaviour
{
    const string IconAnim = "Icon_Idle";

    [SerializeField] GameObject redDot;
    [SerializeField] TextMeshProUGUI timeTex;
    [SerializeField] TextMeshProUGUI progressTex;
    SkeletonGraphic iconSpine;
    Button button;
    bool clickBound;

    void Awake()
    {
        EnsureClickable();
    }

    void OnEnable()
    {
        transform.localScale = Vector3.one;
        EnsureClickable();
        BindLabels();
        PlaySpineLoop();
        Refresh();
        CancelInvoke(nameof(Refresh));
        InvokeRepeating(nameof(Refresh), 1f, 1f);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(Refresh));
        DOTween.Kill(transform);
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnEntryClicked);
    }

    public void EnsureClickable()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            SetLayerRecursively(gameObject, uiLayer);

        Image hit = GetComponent<Image>();
        if (hit == null)
            hit = gameObject.AddComponent<Image>();

        Transform icon = transform.Find("Icon");
        Image iconImg = icon != null ? icon.GetComponent<Image>() : null;
        if (hit.sprite == null && iconImg != null)
            hit.sprite = iconImg.sprite;
        if (hit.color.a <= 0f && hit.sprite != null)
            hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;
        if (iconImg != null)
            iconImg.raycastTarget = true;

        button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<TButton>();
        button.interactable = true;
        button.transition = Selectable.Transition.None;
        if (button.targetGraphic == null)
            button.targetGraphic = hit;

        if (!clickBound)
        {
            button.onClick.RemoveListener(OnEntryClicked);
            button.onClick.AddListener(OnEntryClicked);
            clickBound = true;
        }

        transform.SetAsLastSibling();
    }

    public void Refresh()
    {
        EnsureClickable();
        BindLabels();

        bool ready = PuppyAdventureDemoState.StartMode == PuppyAdventureStartMode.Ready;
        bool showProgress = PuppyAdventureDemoState.IsRunning;
        if (redDot != null)
            redDot.SetActive(ready);
        if (timeTex != null)
        {
            timeTex.gameObject.SetActive(true);
            timeTex.text = ready
                ? PuppyAdventureLoc.Get("UI_Start")
                : PuppyAdventureDemoState.FormatTime(ActivitySecondsLeft());
        }
        if (progressTex != null)
        {
            progressTex.gameObject.SetActive(showProgress);
            if (showProgress)
            {
                PuppyAdventureDemoState.ResolveHomeProgress(out _, out int win);
                progressTex.text = $"{win}/{PuppyAdventureDemoState.DisplayWinTarget}";
            }
        }
    }

    static long ActivitySecondsLeft()
    {
        if (PuppyAdventureDemoState.IsRunning)
            return PuppyAdventureDemoState.ChallengeSecondsLeft;
        if (PuppyAdventureDemoState.StartMode == PuppyAdventureStartMode.DailyClosed)
            return PuppyAdventureDemoState.DailyClosedSecondsLeft;
        return PuppyAdventureDemoState.CooldownSecondsLeft;
    }

    void BindLabels()
    {
        TextMeshProUGUI clock = FindLabel("timeLb", "time");
        if (clock != null)
        {
            if (timeTex != null && timeTex != clock)
                timeTex.gameObject.SetActive(false);
            clock.gameObject.SetActive(true);
            timeTex = clock;
        }

        if (progressTex == null)
            progressTex = FindLabel("ProgressLb", "Progress");
    }

    TextMeshProUGUI FindLabel(string preferred, string fallback)
    {
        Transform node = FindChild(preferred);
        if (node == null)
            node = FindChild(fallback);
        return node != null ? node.GetComponent<TextMeshProUGUI>() : null;
    }

    Transform FindChild(string childName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return child;
        }
        return null;
    }

    void PlaySpineLoop()
    {
        if (iconSpine == null)
            iconSpine = GetComponentInChildren<SkeletonGraphic>(true);
        if (iconSpine == null)
            return;
        if (!iconSpine.IsValid)
            iconSpine.Initialize(false);
        if (!iconSpine.IsValid || iconSpine.AnimationState == null)
            return;

        iconSpine.startingAnimation = IconAnim;
        iconSpine.startingLoop = true;
        Spine.TrackEntry current = iconSpine.AnimationState.GetCurrent(0);
        if (current != null && current.Loop && current.Animation != null && current.Animation.Name == IconAnim)
            return;
        iconSpine.AnimationState.SetAnimation(0, IconAnim, true);
    }

    void OnEntryClicked()
    {
        Debug.Log("[PuppyAdventure] entry clicked");
        PuppyAdventureFlow.OpenFromEntry();
        if (AudioController.AudioClips != null)
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }
}
