using FishFramework;
using Spine;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// Keeps the loading Spine alive above loading panels while they switch.
/// </summary>
public static class LoadingZzSpinePlayback
{
    private static SkeletonGraphic s_PersistentSpine;

    public static void Show(SkeletonGraphic fallbackSpine, string animationName)
    {
        if (s_PersistentSpine != null)
        {
            if (fallbackSpine != null && fallbackSpine != s_PersistentSpine)
            {
                fallbackSpine.gameObject.SetActive(false);
            }

            return;
        }

        if (fallbackSpine == null || string.IsNullOrEmpty(animationName))
        {
            return;
        }

        s_PersistentSpine = fallbackSpine;
        MoveToOverlayLayer(s_PersistentSpine);
        s_PersistentSpine.gameObject.SetActive(true);
        s_PersistentSpine.raycastTarget = false;

        if (!s_PersistentSpine.IsValid)
        {
            s_PersistentSpine.Initialize(false);
        }

        if (!s_PersistentSpine.IsValid)
        {
            s_PersistentSpine = null;
            return;
        }

        s_PersistentSpine.AnimationState.ClearTrack(0);
        TrackEntry trackEntry = s_PersistentSpine.AnimationState.SetAnimation(0, animationName, true);
        if (trackEntry.Animation != null && trackEntry.Animation.Duration > 0f)
        {
            // Start from the matching global phase if the loading UI was opened during an active loop.
            trackEntry.TrackTime = Mathf.Repeat(Time.time, trackEntry.Animation.Duration);
        }

        s_PersistentSpine.Update(0f);
    }

    public static void Release()
    {
        if (s_PersistentSpine == null)
        {
            return;
        }

        if (s_PersistentSpine.IsValid)
        {
            s_PersistentSpine.AnimationState.ClearTrack(0);
        }

        Object.Destroy(s_PersistentSpine.gameObject);
        s_PersistentSpine = null;
    }

    public static void SetVisible(bool visible)
    {
        if (s_PersistentSpine != null)
        {
            s_PersistentSpine.gameObject.SetActive(visible);
        }
    }

    private static void MoveToOverlayLayer(SkeletonGraphic spine)
    {
        var overlayParent = GameModule.UI != null ? GameModule.UI.GetLayerRect(PanelLayer.Top) : null;
        if (overlayParent == null)
        {
            return;
        }

        var rect = spine.rectTransform;
        rect.SetParent(overlayParent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.SetAsLastSibling();
    }
}
