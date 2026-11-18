using System;
using System.Collections.Generic;
using cfg;
// using cfg.GM;
using SuperScrollView;
using UnityEngine;
using UnityEngine.UI;
using FishFramework;
using Utility = FishFramework.Utility;

public class GMData
{
    // public GmCommandConfig gmCommandCfg;
    public Dictionary<string, int> commandDic = new();
    public string configKey;
}

public class GMSelectData
{
    public int itemId;
    public string desc;
}

public class GMDlg : GMDlgBase
{
//     private GmCommandConfig selectGmCommandCfg;

//     private readonly List<GMData> gmDataList = new();
//     private readonly List<GMSelectData> gmSelectDataList = new();

//     protected override void OnClicked(Button button)
//     {
//         if (button == touchButton || button == closeButton)
//         {
//             SetBgVisible(false);
//         }
//         else if (button == sureButton)
//         {
//             SetCommandData();
//         }
//         else if (button == gmButton)
//         {
//             SetBgVisible(true);
//         }
//     }

//     private void SetBgVisible(bool value)
//     {
//         touchButton.SetActiveEx(value);
//         nineBg.SetActiveEx(value);
//         uidText.text = $"UID：{GameData.Player.Uid.ToString()}";
//     }

//     protected override void OnOpen()
//     {
//         searchInput.onValueChanged.AddListener(Change);
//         SetBgVisible(false);

//         MakeConfigData();

//         loopListView1.InitListView(gmDataList.Count, OnGetItemByIndex1);
//         loopListView2.InitListView(0, OnGetItemByIndex2);

//         OnGMCell1(0);
//     }

//     private void Change(string arg0)
//     {
//         loopListView2.SetListItemCount(Filtrate(gmSelectDataList, arg0).Count);
//         loopListView2.RefreshAllShownItem();
//     }

//     private List<GMSelectData> Filtrate(List<GMSelectData> pData, string filtr)
//     {
//         List<GMSelectData> data = new(pData);
//         if (filtr == "")
//         {
//             return data;
//         }

//         for (int i = data.Count - 1; i >= 0; i--)
//         {
//             if (data[i].desc.IndexOf(filtr, StringComparison.Ordinal) != -1)
//             {
//                 continue;
//             }

//             data.Remove(gmSelectDataList[i]);
//         }

//         return data;
//     }

//     private void MakeConfigData()
//     {
//         foreach (var cfg in ConfigHelper.Tables.TbGmCommandConfig.DataList)
//         {
//             if (cfg.Command == "add_item")
//             {
//                 GMData gmData = new GMData
//                 {
//                     gmCommandCfg = cfg,
//                     configKey = cfg.Key
//                 };
//                 Dictionary<string, int> temp = new Dictionary<string, int>
//                 {
//                     ["itemId"] = 0,
//                     ["itemCnt"] = 0
//                 };
//                 temp["itemType"] = cfg.Key switch
//                 {
//                     "item" => (int)ItemType.Res,
//                     "role" => (int)ItemType.Role,
//                     "equip" => (int)ItemType.Equip,
//                     _ => temp["itemType"]
//                 };

//                 gmData.commandDic = temp;
//                 gmDataList.Add(gmData);
//             }
//             else
//             {
//                 GMData gmData = new GMData
//                 {
//                     gmCommandCfg = cfg,
//                 };
//                 gmDataList.Add(gmData);
//             }
//         }
//     }

//     private LoopListViewItem2 OnGetItemByIndex1(LoopListView2 listView, int index)
//     {
//         if (index < 0 || index >= gmDataList.Count)
//         {
//             return null;
//         }

//         LoopListViewItem2 item = listView.NewListViewItem("GMCell");
//         GMCell itemScript = item.GetComponent<GMCell>();
//         if (item.IsInitHandlerCalled == false)
//         {
//             item.IsInitHandlerCalled = true;
//             itemScript.AddListener(OnGMCell1);
//         }

//         itemScript.UpdateUI(gmDataList[index].gmCommandCfg.Name, index);
//         return item;
//     }

//     private void OnGMCell1(int idx)
//     {
//         gmSelectDataList.Clear();

//         inputItemType.SetActiveEx(gmDataList[idx].commandDic.ContainsKey("itemType"));
//         inputItemId.SetActiveEx(gmDataList[idx].commandDic.ContainsKey("itemId"));
//         inputItemCnt.SetActiveEx(gmDataList[idx].commandDic.ContainsKey("itemCnt"));
//         inputItemCnt.text = "1";

//         selectGmCommandCfg = gmDataList[idx].gmCommandCfg;
//         if (selectGmCommandCfg.Command == "add_item")
//         {
//             inputItemType.text = gmDataList[idx].commandDic["itemType"].ToString();
//             switch (gmDataList[idx].configKey)
//             {
//                 case "item":
//                     {
//                         foreach (var cfg in ConfigHelper.Tables.TbItemConfig.DataList)
//                         {
//                             gmSelectDataList.Add(new GMSelectData
//                             {
//                                 itemId = cfg.Id,
//                                 desc = $"{cfg.Id}   {cfg.ItemName}"
//                             });
//                         }

//                         break;
//                     }
//                 case "role":
//                     {
//                         // foreach (var cfg in ConfigHelper.Tables.TbCharacterInfoConfig.DataList)
//                         // {
//                         //     gmSelectDataList.Add(new GMSelectData
//                         //     {
//                         //         itemId = cfg.Id,
//                         //         desc = $"{cfg.Id}   {ConfigHelper.Tables.TbCharacterConfig.Get(cfg.BaseId).Name}-{cfg.Star}星"
//                         //     });
//                         // }

//                         break;
//                     }
//                 case "equip":
//                     {
//                         foreach (var cfg in ConfigHelper.Tables.TbEquipmentConfig.DataList)
//                         {
//                             gmSelectDataList.Add(new GMSelectData
//                             {
//                                 itemId = cfg.Id,
//                                 desc = $"{cfg.Id}   {cfg.Name}-{cfg.Star}星"
//                             });
//                         }

//                         break;
//                     }
//             }
//         }

//         gmSelectDataList.Sort((a, b) => a.itemId.CompareTo(b.itemId));
//         loopListView2.SetListItemCount(gmSelectDataList.Count);
//         loopListView2.RefreshAllShownItem();
//     }

//     private LoopListViewItem2 OnGetItemByIndex2(LoopListView2 listView, int index)
//     {
//         if (index < 0 || index >= Filtrate(gmSelectDataList, searchInput.text).Count)
//         {
//             return null;
//         }

//         LoopListViewItem2 item = listView.NewListViewItem("GMCell");
//         GMCell itemScript = item.GetComponent<GMCell>();
//         if (item.IsInitHandlerCalled == false)
//         {
//             item.IsInitHandlerCalled = true;
//             itemScript.AddListener(OnGMCell2);
//         }

//         itemScript.UpdateUI(Filtrate(gmSelectDataList, searchInput.text)[index].desc, index);
//         return item;
//     }

//     private void OnGMCell2(int idx)
//     {
//         inputItemId.text = Filtrate(gmSelectDataList, searchInput.text)[idx].itemId.ToString();
//     }

//     private class GMPostData
//     {
//         public uint userId;
//         public string command;
//         public string args;
//     }

//     private void SetCommandData()
//     {
//         GMPostData gmPostData = new GMPostData
//         {
//             userId = GameData.Player.Uid,
//             command = selectGmCommandCfg.Command,
//         };
//         if (inputItemType.gameObject.activeSelf)
//         {
//             gmPostData.args = $"{gmPostData.args}{inputItemType.text}";
//         }

//         if (inputItemId.gameObject.activeSelf)
//         {
//             gmPostData.args = $"{gmPostData.args} {inputItemId.text}";
//         }

//         if (inputItemCnt.gameObject.activeSelf)
//         {
//             gmPostData.args = $"{gmPostData.args} {inputItemCnt.text}";
//         }

//         string url = GameModule.Setting.GetGMURL();
//         Utility.RequestByJsonBodyPost(url,
//             JsonUtility.ToJson(gmPostData),
//             (isOk, str) =>
//             {
//                 // Alert1Dlg.ShowDlg(isOk ? "操作成功" : $"操作失败{str}");
//             });
//     }
}