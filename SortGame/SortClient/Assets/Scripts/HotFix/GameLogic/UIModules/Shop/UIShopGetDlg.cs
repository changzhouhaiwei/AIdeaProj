using System;
using System.Collections.Generic;
using System.Linq;
using Core.Tools.MethodExtension;
using FishXSortPlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UIShopGetDlg : UIShopGetDlgBase
{
    private List<GameObject> bgList = new();
    private List<GameObject> cellList = new();
    private Action mOnConfirm;
    private bool mConfirmInvoked;

    protected override void OnOpen()
    {
        base.OnOpen();
        mOnConfirm = null;
        mConfirmInvoked = false;
        InitNodes();
        ApplyOrangeFontMaterials();
    }

    protected override void OnDestroying()
    {
        InvokeConfirm();
        base.OnDestroying();
    }

    void InitNodes()
    {
        bgList.Clear();
        cellList.Clear();

        bgList.Add(cellBg1.gameObject);
        bgList.Add(cellBg2.gameObject);
        bgList.Add(cellBg3.gameObject);
        bgList.Add(cellBg4.gameObject);
        bgList.Add(cellBg5.gameObject);

        cellList.Add(cell1.gameObject);
        cellList.Add(cell2.gameObject);
        cellList.Add(cell3.gameObject);
        cellList.Add(cell4.gameObject);
        cellList.Add(cell5.gameObject);
    }

    void ApplyOrangeFontMaterials()
    {
        var nameText = viewBehaviour != null
            ? viewBehaviour.transform.Find("nineBg/nameText (1)")?.GetComponent<TextMeshProUGUI>()
            : null;
        var claimText = okBtn != null
            ? okBtn.transform.Find("text")?.GetComponent<TextMeshProUGUI>()
            : null;

        // 预制体已挂橙色材质，本地化换字体会映射到 Material_outline_shop；运行时强制回 orange。
        PlayBtnDifficultyFont.Apply(nameText, 0, isCoinLevel: true);
        PlayBtnDifficultyFont.Apply(claimText, 0, isCoinLevel: true);
    }

    public void ShowAwards(cfg.Game.ShopCfg oneCfg)
    {
        int cnt = oneCfg.Award.Count;
        line2.SetActiveEx(cnt >= 3);

        float scale = 1.2f;
        if (cnt <= 1)
        {
            scale = 1.6f;
        }

        int slotCnt = bgList.Count;
        for (int i = 0; i < slotCnt; i++)
        {
            bgList[i].SetActive(i < cnt);
            if (i < cnt)
            {
                UpdateCellData(cellList[i], oneCfg.Award[i]);
                cellList[i].transform.SetLocalScale(scale);
            }
        }

        XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.rewardGet);
    }

    public void ShowAwards(IList<string> awards, Action onConfirm)
    {
        if (bgList.Count == 0)
        {
            InitNodes();
        }

        mOnConfirm = onConfirm;
        mConfirmInvoked = false;

        int cnt = awards != null ? awards.Count : 0;
        line2.SetActiveEx(cnt >= 3);

        float scale = cnt <= 1 ? 1.6f : 1.2f;
        int slotCnt = bgList.Count;
        for (int i = 0; i < slotCnt; i++)
        {
            bool show = i < cnt;
            bgList[i].SetActive(show);
            if (show)
            {
                BindAwardCell(cellList[i], awards[i]);
                cellList[i].transform.SetLocalScale(scale);
            }
        }

        XSortGameCore.AudioController.PlaySound(XSortGameCore.AudioController.AudioClips.rewardGet);
    }


    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == okBtn)
        {
            CloseSelf();
        }
    }

    protected override bool OnEscButtonPressed()
    {
        CloseSelf();
        return true;
    }

    void InvokeConfirm()
    {
        if (mConfirmInvoked)
        {
            return;
        }

        mConfirmInvoked = true;
        var callback = mOnConfirm;
        mOnConfirm = null;
        callback?.Invoke();
    }


    public void UpdateCellData(GameObject obj, string awardStr)
    {
        if (!BindAwardCell(obj, awardStr, out int id, out int num))
        {
            return;
        }

        GPUserDataManager.Instance.AddCommonItemData(id, num);
    }

    void BindAwardCell(GameObject obj, string awardStr)
    {
        BindAwardCell(obj, awardStr, out _, out _);
    }

    bool BindAwardCell(GameObject obj, string awardStr, out int id, out int num)
    {
        id = 0;
        num = 0;
        if (obj == null)
        {
            return false;
        }

        Image icon = obj.GetComponentInChildren<Image>();
        TextMeshProUGUI textMesh = obj.GetComponentInChildren<TextMeshProUGUI>();
        var arr = awardStr.Split(",").ToList();
        if (arr.Count != 2)
        {
            return false;
        }

        id = arr[0].ToInt();
        num = arr[1].ToInt();
        if (icon != null)
        {
            if (id == 4)
            {
                icon.sprite = LoadSprite("ShopIcon", GameData.Shop.GetCoinSpritePathByCnt(num));
            }
            else
            {
                icon.sprite = LoadSprite("LevelAward", "item_" + id);
            }
        }

        if (textMesh != null)
        {
            textMesh.text = "x" + num;
        }

        return true;
    }
}
