using System.Collections;
using System.Collections.Generic;
// using cfg;
// using cfg.Guide;
using UnityEngine;
using UnityEngine.EventSystems;
using FishFramework;

public class GuideManager : MonoSingleton<GuideManager>
{
    // private bool isBusying;
    // private bool isFirstStep;
    // private bool isClientOpenStep;

    // private int guideTaskId;
    // private GuideTasksConfig guideTasksCfg;

    // private List<int> genStepsArr;

    // private List<int> allStepsIndex = new();
    // private int currentStepIndex;

    // private int guideId;
    // private GuideConfig guideCfg;
    // private GameObject targetObj;

    // private GuideController guideController;
    // private RectTransform replaceTf;
    // private TButton fullScreenButton;
    // private TButton fullScreenTouchPassButton;

    // private Camera m_guideCamera;

    // private GuideUI guideUI;

    // private void Awake()
    // {
    //     DontDestroyOnLoad(gameObject);
    // }

    // public void Initialize(GameObject guideMgrGo, GameObject guideUiGo)
    // {
    //     guideController = guideMgrGo.transform.Find("GuideController").GetComponent<GuideController>();
    //     guideController.SetActiveEx(false);

    //     replaceTf = guideMgrGo.transform.Find("ReplaceObj").GetComponent<RectTransform>();
    //     replaceTf.SetActiveEx(false);

    //     fullScreenButton = guideMgrGo.transform.Find("FullScreenButton").GetComponent<TButton>();
    //     fullScreenButton.SetActiveEx(false);
    //     fullScreenButton.onClick.AddListener(NextStep);

    //     fullScreenTouchPassButton = guideMgrGo.transform.Find("FullScreenTouchPassButton").GetComponent<TButton>();
    //     fullScreenTouchPassButton.SetActiveEx(false);
    //     fullScreenTouchPassButton.onClick.AddListener(NextStep);

    //     //挖空
    //     guideUI = guideUiGo.GetComponent<GuideUI>();
    //     guideUI.SetActiveEx(false);
    // }

    // public void HideGuideUI()
    // {
    //     //隐藏
    //     if (IsGuiding())
    //     {
    //         guideUI.SetActiveEx(false);
    //         // M3GameManager.Instance.guideCoverCtrl.HideGuiding();
    //     }
    // }

    // private void ResetState()
    // {
    //     targetObj = null;
    // }

    // public void InitClientOpenStep(int guideTask_Id)
    // {
    //     isBusying = false;
    //     if (isBusying)
    //     {
    //         return;
    //     }
    //     currentStepIndex = 0;
    //     isClientOpenStep = true;
    //     InitStep(guideTask_Id);
    // }

    // public void InitStep(int guideTask_Id)
    // {
    //     StartCoroutine(IEInitStep(guideTask_Id));
    // }

    // public Camera GetGuideCamera()
    // {
    //     return m_guideCamera;
    // }

    // private IEnumerator IEInitStep(int guideTask_Id)
    // {
    //     while (isBusying)
    //     {
    //         yield return null;
    //     }

    //     ResetState();
    //     isBusying = true;
    //     guideTaskId = guideTask_Id;
    //     guideTasksCfg = ConfigHelper.Tables.TbGuideTasksConfig.Get(guideTask_Id);
    //     genStepsArr = guideTasksCfg.GenStepArr;

    //     if (isFirstStep && guideTasksCfg.CutGuideSteps.Count > 0)
    //     {
    //         allStepsIndex = guideTasksCfg.CutGuideSteps;
    //     }
    //     else
    //     {
    //         allStepsIndex = guideTasksCfg.GuideSteps;
    //     }

    //     currentStepIndex = 0;
    //     isFirstStep = false;

    //     // GameBAData.ExcuteGuide(guideTasksCfg.Id.ToString(), guideTasksCfg.Name, "0");

    //     yield return StartCoroutine(ExcuteStep());
    // }

    // private IEnumerator ExcuteStep()
    // {
    //     targetObj = null;
    //     guideId = allStepsIndex[currentStepIndex];
    //     guideCfg = ConfigHelper.Tables.TbGuideConfig.Get(guideId);
    //     Debug.Log($"新手引导步骤 --- {guideId}");
    //     // 屏蔽触摸
    //     guideController.SetActiveEx(true);
    //     guideController.ResetMaskImage();

    //     RectTransform rectTf = GameModule.UI.UILayerRoot;
    //     switch (guideCfg.ObjType)
    //     {
    //         case 1:
    //             {
    //                 // UI
    //                 if (!string.IsNullOrEmpty(guideCfg.PathName))
    //                 {
    //                     Transform targetRect = rectTf.Find(guideCfg.PathName);

    //                     m_guideCamera = GameModule.UI.UICamera;
    //                     guideController.SetModuleCanvas(GameModule.UI.UICanvas);
    //                     //非UI框架中的UI,玩法界面的临时UI
    //                     if (targetRect == null)
    //                     {
    //                         // var tempObj = GameObject.Find(guideCfg.PathName);
    //                         // if (tempObj != null)
    //                         // {
    //                         //     m_guideCamera = M3GameManager.Instance.GetGPCamera();
    //                         //     guideController.SetModuleCanvas(M3GameManager.Instance.GameCanvas);
    //                         //     targetRect = tempObj.transform as RectTransform;
    //                         // }
    //                     }

    //                     while (targetRect == null)
    //                     {
    //                         targetRect = rectTf.Find(guideCfg.PathName);
    //                         yield return null;
    //                     }

    //                     targetObj = targetRect.gameObject;
    //                 }
    //                 else
    //                 {
    //                     Debug.LogError("UI--PathName--不能为空");
    //                 }

    //                 if (targetObj == null)
    //                 {
    //                     yield return null;
    //                 }
    //                 else
    //                 {
    //                     // 在场景中的是否显示
    //                     while (!targetObj.activeInHierarchy)
    //                     {
    //                         yield return null;
    //                     }
    //                 }

    //                 break;
    //             }
    //         case 2:
    //             {
    //                 // 2D

    //                 break;
    //             }
    //     }

    //     switch (guideCfg.ClickType)
    //     {
    //         case 0:
    //             {
    //                 if (targetObj)
    //                 {
    //                     EventTriggerListener.Get(targetObj).onClick.AddListener(OnGuideCallBack);
    //                 }

    //                 break;
    //             }
    //         case 1:
    //             {
    //                 fullScreenButton.SetActiveEx(true);
    //                 break;
    //             }
    //         case 2:
    //             {
    //                 fullScreenTouchPassButton.SetActiveEx(true);
    //                 break;
    //             }
    //         case 3:
    //             {
    //                 ExcuteGamePlayGuide();
    //                 break;
    //             }
    //         case 4:
    //             {
    //                 fullScreenButton.SetActiveEx(true);
    //                 ExcuteGamePlayIntro();
    //                 break;
    //             }
    //         default: break;
    //     }

    //     guideUI.SetActiveEx(true);
    //     switch (guideCfg.ObjType)
    //     {
    //         case 1:
    //             {
    //                 if (targetObj == null)
    //                 {
    //                     yield return null;
    //                 }
    //                 else
    //                 {
    //                     guideController.Guide(targetObj.GetComponent<RectTransform>(), (GuideMaskType)guideCfg.MaskType, guideCfg, guideCfg.IsShowMask == 1);
    //                     guideUI.SetRefreshParams(guideCfg, targetObj.GetComponent<RectTransform>(), m_guideCamera);
    //                 }

    //                 break;
    //             }
    //         case 2: //原本是用在spriteRender层.
    //             {
    //                 guideController.Guide(replaceTf, (GuideMaskType)guideCfg.MaskType, guideCfg, guideCfg.IsShowMask == 1);
    //                 guideUI.SetRefreshParams(guideCfg, replaceTf, m_guideCamera);
    //                 break;
    //             }
    //     }
    // }

    // private void OnGuideCallBack(GameObject go, PointerEventData eventData)
    // {
    //     EventTriggerListener.Get(targetObj).onClick.RemoveListener(OnGuideCallBack);
    //     NextStep();
    // }

    // public void NextStep()
    // {
    //     guideUI.Clear();
    //     guideUI.SetActiveEx(false);
    //     guideController.ResetMaskImage();
    //     guideController.SetActiveEx(false);
    //     fullScreenButton.SetActiveEx(false);
    //     fullScreenTouchPassButton.SetActiveEx(false);
    //     replaceTf.SetActiveEx(false);

    //     //玩法引导移除对应显示
    //     // M3GameManager.Instance.guideCoverCtrl.FinishGuiding();

    //     currentStepIndex++;
    //     if (currentStepIndex < allStepsIndex.Count && IsGuiding())
    //     {
    //         StartCoroutine(ExcuteStep());
    //     }
    //     else
    //     {
    //         isBusying = false;
    //         isClientOpenStep = false;
    //         // GameBAData.ExcuteGuide(guideTasksCfg.Id.ToString(), guideTasksCfg.Name, "1");
    //     }
    // }

    // public bool IsGuiding()
    // {
    //     return isClientOpenStep;
    // }

    // public bool IsCorrectSweepPiece(Vector2Int fromPos, Vector2Int toPos)
    // {
    //     if (guideCfg.EffectPiecePos.Count == 1)
    //     {
    //         return false;
    //     }

    //     cfg.Common.VectorInt pos1 = guideCfg.EffectPiecePos[0];
    //     cfg.Common.VectorInt pos2 = guideCfg.EffectPiecePos[1];

    //     if (pos1.X == fromPos.x && pos1.Y == fromPos.y && pos2.X == toPos.x && pos2.Y == toPos.y)
    //     {
    //         return true;
    //     }

    //     if (pos1.X == toPos.x && pos1.Y == toPos.y && pos2.X == fromPos.x && pos2.Y == fromPos.y)
    //     {
    //         return true;
    //     }

    //     return false;
    // }

    // public GuideConfig GetGuideConfig()
    // {
    //     return guideCfg;
    // }


    // //手指引导类型 
    // public void ExcuteGamePlayGuide()
    // {
    //     var guideCfg = GetGuideConfig();
    //     var fromGridPos = new Vector2Int(guideCfg.FromToPos[0].X, guideCfg.FromToPos[0].Y);
    //     var toGridPos = new Vector2Int(guideCfg.FromToPos[1].X, guideCfg.FromToPos[1].Y);
    //     var eptGridPosList = new List<Vector2Int>();
    //     foreach (var pos in guideCfg.EffectPiecePos)
    //     {
    //         eptGridPosList.Add(new Vector2Int(pos.X, pos.Y));
    //     }

    //     // var guideCoverCtrl = M3GameManager.Instance.guideCoverCtrl;
    //     // guideCoverCtrl.InitCover(eptGridPosList);
    //     // guideCoverCtrl.SetFromAndToGridPos(fromGridPos, toGridPos);
    //     // guideCoverCtrl.StartGuiding();
    // }

    // //纯介绍不
    // public void ExcuteGamePlayIntro()
    // {
    //     var guideCfg = GetGuideConfig();
    //     var eptGridPosList = new List<Vector2Int>();
    //     foreach (var pos in guideCfg.EffectPiecePos)
    //     {
    //         eptGridPosList.Add(new Vector2Int(pos.X, pos.Y));
    //     }

    //     // var guideCoverCtrl = M3GameManager.Instance.guideCoverCtrl;
    //     // guideCoverCtrl.InitCover(eptGridPosList);
    //     // guideCoverCtrl.StartGuiding();
    //     // guideCoverCtrl.StopFingerAnim();
    // }

    // //固定生成步骤
    // public List<int> GetGenStepsArr()
    // {
    //     return genStepsArr;
    // }
}