using System;
using cfg.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserFrame : UserFrameNodeBase
{
    public Action<UserFrame> OnClickCb;

    private int mFrameId;
    private bool mUnlocked;
    private bool mbSelected;

    private TButton emptyButton;
    private Image frame;
    private Image bSel;
    private Image lockNode;
    private GameObject selectedNode;

    public int GetFrameId()
    {
        return mFrameId;
    }

    public bool IsUnlocked()
    {
        return mUnlocked;
    }

    protected override void OnBindCompsAndEvents()
    {
        base.OnBindCompsAndEvents();
        BindRefsByName();
        if (emptyButton != null)
        {
            BindEvent(emptyButton);
            EnsureClickReceiver();
        }
    }

    protected override void OnUnbindCompsAndEvents()
    {
        if (emptyButton != null)
        {
            UnbindEvent(emptyButton);
        }

        emptyButton = null;
        frame = null;
        bSel = null;
        lockNode = null;
        selectedNode = null;
        base.OnUnbindCompsAndEvents();
    }

    void BindRefsByName()
    {
        var root = viewBehaviour != null ? viewBehaviour.transform : null;
        if (root == null)
        {
            return;
        }

        emptyButton = FindNamedComponent<TButton>(root, "EmptyButton");

        var nestedFrame = root.Find("UserFrameNode/frame");
        if (nestedFrame != null)
        {
            frame = nestedFrame.GetComponent<Image>();
        }

        if (frame == null)
        {
            var frameTran = root.Find("frame");
            frame = frameTran != null ? frameTran.GetComponent<Image>() : null;
        }

        bSel = FindNamedComponent<Image>(root, "bSel");
        lockNode = FindNamedComponent<Image>(root, "lock");
        var selectedTran = FindDeepChild(root, "selected");
        selectedNode = selectedTran != null ? selectedTran.gameObject : null;
    }

    /// <summary>
    /// AvatarNode 上 UserFrameNode 叠在 EmptyButton 之上会挡住点击：按钮置顶，装饰图关 raycast。
    /// </summary>
    void EnsureClickReceiver()
    {
        if (emptyButton == null)
        {
            return;
        }

        emptyButton.interactable = true;
        emptyButton.transform.SetAsLastSibling();

        var root = viewBehaviour != null ? viewBehaviour.transform : emptyButton.transform.parent;
        if (root == null)
        {
            return;
        }

        var graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            var g = graphics[i];
            if (g == null)
            {
                continue;
            }

            if (g.transform == emptyButton.transform || g.transform.IsChildOf(emptyButton.transform))
            {
                continue;
            }

            g.raycastTarget = false;
        }
    }

    static T FindNamedComponent<T>(Transform root, string name) where T : Component
    {
        var found = FindDeepChild(root, name);
        return found != null ? found.GetComponent<T>() : null;
    }

    static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        SetBSelected(false);
        HideIcon();
        EnsureClickReceiver();
        RefreshTotalWinCount();
    }

    void HideIcon()
    {
        var iconTran = gameObject.transform.Find("icon");
        if (iconTran != null)
        {
            iconTran.gameObject.SetActiveEx(false);
        }
    }

    public void SetBSelected(bool b)
    {
        mbSelected = b;
        if (bSel != null)
        {
            bSel.gameObject.SetActiveEx(b);
        }

        if (selectedNode != null)
        {
            selectedNode.SetActiveEx(b);
        }
    }

    public void UpdateFrame(AvatarFrameCfg cfg, bool unlocked, bool selected, string remainText)
    {
        mFrameId = cfg.Id;
        mUnlocked = unlocked;
        HideIcon();
        EnsureClickReceiver();

        if (frame != null)
        {
            frame.sprite = LoadAtlasSprite("UserInfo", cfg.Pic);
            frame.gameObject.SetActiveEx(true);
        }

        if (lockNode != null)
        {
            lockNode.gameObject.SetActiveEx(!unlocked);
            lockNode.raycastTarget = false;
        }

        var scene = unlocked
            ? AvatarFrameDecorScene.FrameListUnlocked
            : AvatarFrameDecorScene.FrameListLocked;
        AvatarFrameUiHelper.ApplyFrameDecor(gameObject.transform, cfg.Id, unlocked, scene, remainText);
        HideHonor();
        EnsureClickReceiver();
        SetBSelected(selected);
    }

    public void UpdatePreview(int frameId)
    {
        mFrameId = frameId;
        mUnlocked = frameId <= 0
            || !GPUserDataManager.HasInit()
            || GPUserDataManager.Instance.IsAvatarFrameUnlocked(frameId);
        HideIcon();

        if (lockNode != null)
        {
            lockNode.gameObject.SetActiveEx(false);
        }

        SetBSelected(false);

        if (emptyButton != null)
        {
            emptyButton.interactable = false;
        }

        ApplyFrameSprite(frameId);
        RefreshTotalWinCount();
    }

    public void RefreshRemainDisplay(int frameId)
    {
        mFrameId = frameId;
        mUnlocked = frameId <= 0
            || !GPUserDataManager.HasInit()
            || GPUserDataManager.Instance.IsAvatarFrameUnlocked(frameId);
        RefreshTotalWinCount();
    }

    public void ApplyFrameSprite(int frameId)
    {
        mFrameId = frameId;
        if (frame == null)
        {
            return;
        }

        if (frameId <= 0)
        {
            frame.gameObject.SetActiveEx(false);
            return;
        }

        var cfg = ConfigHelper.Tables.TbAvatarFrameCfg.GetOrDefault(frameId);
        if (cfg == null)
        {
            frame.gameObject.SetActiveEx(false);
            return;
        }

        frame.sprite = LoadAtlasSprite("UserInfo", cfg.Pic);
        frame.gameObject.SetActiveEx(true);
    }

    void HideHonor()
    {
        var honor = AvatarFrameUiHelper.FindHonorNode(gameObject.transform);
        if (honor != null)
        {
            honor.gameObject.SetActiveEx(false);
        }
    }

    public void RefreshTotalWinCount()
    {
        if (viewBehaviour != null)
        {
            AvatarFrameUiHelper.RefreshTotalWinCount(viewBehaviour.transform, mFrameId);
            return;
        }

        TextMeshProUGUI text = totalWinCount;
        AvatarFrameUiHelper.RefreshTotalWinCountText(text);
    }

    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == emptyButton)
        {
            OnClickCb?.Invoke(this);
        }
    }
}
