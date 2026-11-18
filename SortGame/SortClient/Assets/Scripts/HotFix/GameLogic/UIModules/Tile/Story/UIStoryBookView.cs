using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

public class UIStoryBookView : UIStoryBookViewBase
{
    private GameObject oriCellNodeObj;
    private List<UIStoryBookCellWidget> m_cellList = new();
    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
        if (button == backButton)
        {
            uIStoryBookView.Play("UIStoryBookView_Out");
            Utility.DGDelayedCall(0.5f, () =>
            {
                CloseSelf();
            });
        }
    }

    private bool mBInEscAnim = false;

    protected override bool OnEscButtonPressed()
    {
        uIStoryBookView.Play("UIStoryBookView_Out");

        if (!mBInEscAnim)
        {
            mBInEscAnim = true;
            Utility.DGDelayedCall(0.5f, () =>
            {
                CloseSelf();
                mBInEscAnim = false;
            });
        }

        return true;
    }

    //创建
    protected override void OnCreating()
    {
        base.OnCreating();
        InitOriUI();

        uIStoryBookView.Play("UIStoryBookView_In");
    }

    void InitOriUI()
    {
        oriCellNodeObj = storyCellNode_RectTransform.gameObject;
        var pNode = oriCellNodeObj.transform.parent;

        var dic = GameData.StoryBook.GetCfgDic();
        var keys = dic.Keys.ToList();

        keys.Sort((a, b) =>
        {
            return a.CompareTo(b);
        });

        foreach (var item in keys)
        {
            var oneData = dic[item];
            var oneObj = GameObject.Instantiate(oriCellNodeObj);
            oneObj.transform.SetParent(pNode);
            oneObj.transform.InitLocalTransform();
            var widget = CreateWidget<UIStoryBookCellWidget>("cell" + item, pNode, oneObj.GetComponent<UIWidgetBehaviour>());
            m_cellList.Add(widget);
            widget.SetCellData(oneData);
        }

        oriCellNodeObj.SetActiveEx(false);
    }

    //打开
    protected override void OnOpen()
    {
        base.OnOpen();
    }
}