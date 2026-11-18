using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>
/// 红点树视图
/// </summary>
public class RedDotTreeView : TreeView
{
    private RedDotTreeViewItem m_Root;

    private int m_Id;

    public RedDotTreeView(TreeViewState state) : base(state)
    {
        Reload();

        useScrollView = true;

        RedDotMgr.Instance.NodeNumChangeCallback += Reload;
        RedDotMgr.Instance.NodeValueChangeCallback += Repaint;
    }

    protected override TreeViewItem BuildRoot()
    {
        m_Id = 0;

        m_Root = PreOrder(RedDotMgr.Instance.Root);
        m_Root.depth = -1;

        SetupDepthsFromParentsAndChildren(m_Root);

        return m_Root;
    }

    private RedDotTreeViewItem PreOrder(TreeNode root)
    {
        if (root == null)
        {
            return null;
        }

        RedDotTreeViewItem item = new RedDotTreeViewItem(m_Id++, root);

        if (root.ChildrenCount > 0)
        {
            foreach (TreeNode child in root.Children)
            {
                item.AddChild(PreOrder(child));
            }
        }

        return item;
    }

    private void Repaint(TreeNode node, int value)
    {
        Repaint();
    }

    public void OnDestory()
    {
        RedDotMgr.Instance.NodeNumChangeCallback -= Reload;
        RedDotMgr.Instance.NodeValueChangeCallback -= Repaint;
    }


}
