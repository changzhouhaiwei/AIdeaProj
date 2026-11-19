using System.Collections.Generic;
using System.Linq;
using cfg.Game;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;

class UIStoryBookCellWidget : UIStoryCellNodeBase
{
    private GameObject oriGridNodeObj;
    protected override void OnClicked(Button button)
    {
        base.OnClicked(button);
    }
    protected override void OnOpen()
    {
        base.OnOpen();
        oriGridNodeObj = storyGridNode_RectTransform.gameObject;
    }

    public void SetCellData(List<GameBgCfg> dataList)
    {
        var oneData = dataList.FirstOrDefault();
        countryNameText.text = I2LocalizeMgr.GetString(oneData.CountryName);

        var pTran = oriGridNodeObj.transform.parent;

        int minLevel = 19999;
        int maxLevel = 0;

        foreach (var item in dataList)
        {
            minLevel = math.min(minLevel, item.StartLevel);
            maxLevel = math.max(maxLevel, item.EndLevel);

            levelRangeText.text = I2LocalizeMgr.GetString("Level") + " " + minLevel + "-" + maxLevel;

            var oneObj = GameObject.Instantiate(oriGridNodeObj);
            var oneGrid = CreateWidget<UIStoryGridWidget>("grid" + item.Id, pTran, oneObj.GetComponent<UIWidgetBehaviour>());
            oneObj.transform.InitLocalTransform();
            oneGrid.SetCellData(item);
        }

        oriGridNodeObj.SetActiveEx(false);

        int curLevel = GPUserDataManager.Instance.GetLevelNum() + 1;
        finishSp.SetActiveEx(curLevel >= maxLevel);

    }

    private void InitCells()
    {

    }
}