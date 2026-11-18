using System.Collections.Generic;
using System.Globalization;
using Core.Tools.MethodExtension;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public class CircleDailyAwardRuleUI : CircleDailyAwardRuleUIBase
{
    const int ItemSlotCount = 3;
    const int RateNumIndex = 9;

    private readonly List<UIWidgetBehaviour> mCellList = new();

    protected override void OnOpen()
    {
        base.OnOpen();
        InitListView();
    }

    protected override void OnClicked(Button button)
    {
        if (button == closeBtn)
        {
            CloseSelf();
        }
    }

    protected override bool OnEscButtonPressed()
    {
        CloseSelf();
        return true;
    }

    void InitListView()
    {
        var dataList = GameData.Activity.GetActivityCircleInfoCfgs();
        if (cellTemplate == null || dataList == null)
        {
            return;
        }

        CollectCells();
        EnsureCellCount(dataList.Count);

        for (int i = 0; i < mCellList.Count; i++)
        {
            var cell = mCellList[i];
            bool show = i < dataList.Count;
            cell.gameObject.SetActiveEx(show);
            if (!show)
            {
                continue;
            }

            cell.transform.SetSiblingIndex(i);
            BindCell(cell, dataList[i]);
        }

        var content = cellTemplate.transform.parent as RectTransform;
        if (content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
    }

    void CollectCells()
    {
        mCellList.Clear();
        mCellList.Add(cellTemplate);
        var parent = cellTemplate.transform.parent;
        if (parent == null)
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            var widget = parent.GetChild(i).GetComponent<UIWidgetBehaviour>();
            if (widget != null && !mCellList.Contains(widget))
            {
                mCellList.Add(widget);
            }
        }
    }

    void EnsureCellCount(int count)
    {
        var parent = cellTemplate.transform.parent;
        while (mCellList.Count < count)
        {
            var go = GameObject.Instantiate(cellTemplate.gameObject, parent);
            go.name = "cell" + (mCellList.Count + 1);
            mCellList.Add(go.GetComponent<UIWidgetBehaviour>());
        }
    }

    void BindCell(UIWidgetBehaviour cell, cfg.Game.ActivityCircleInfoCfg cfg)
    {
        var rewards = ActivityData.ParseCircleRewardField(cfg.Reward);
        for (int i = 0; i < ItemSlotCount; i++)
        {
            int rootIndex = i * 3;
            var item = cell.GetGameObjectByIndex(rootIndex);
            if (item == null)
            {
                continue;
            }

            bool show = i < rewards.Count;
            item.SetActiveEx(show);
            if (!show)
            {
                continue;
            }

            BindItemSlot(cell, rootIndex, rewards[i].itemId, rewards[i].count);
        }

        var numLb = cell.GetComponentByIndexs<TextMeshProUGUI>(RateNumIndex, 0);
        if (numLb != null)
        {
            numLb.text = FormatPassrate(cfg.Passrate);
        }
    }

    void BindItemSlot(UIWidgetBehaviour cell, int rootIndex, int itemId, int count)
    {
        var icon = cell.GetComponentByIndexs<Image>(rootIndex + 1, 0);
        if (icon != null)
        {
            if (itemId == 4)
            {
                icon.sprite = LoadSprite("ShopIcon", GameData.Shop.GetCoinSpritePathByCnt(count));
            }
            else
            {
                icon.sprite = LoadSprite("LevelAward", "item_" + itemId);
            }
        }

        var desc = cell.GetComponentByIndexs<TextMeshProUGUI>(rootIndex + 2, 0);
        if (desc != null)
        {
            desc.text = "x" + count;
        }
    }

    static string FormatPassrate(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        var text = raw.Trim().Trim('\u200b', '\u200c', '\ufeff');
        if (text.IndexOf('%') >= 0)
        {
            return text;
        }

        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            if (value <= 1f)
            {
                value *= 100f;
            }

            return value.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        }

        return text;
    }
}
