// using cfg.Guide;
using DG.Tweening;
using TMPro;
using UnityEngine;
using FishFramework;

public class GuideUI : MonoBehaviour
{
    // [SerializeField] private RectTransform tipBgRectTf;
    // [SerializeField] private TextMeshProUGUI descText;
    // [SerializeField] private GameObject normalArrow;
    // [SerializeField] private GameObject fightArrow;
    // [SerializeField] private GameObject finger;

    // private bool canUpdate;

    // // private GuideConfig guideCfg;
    // private RectTransform targetRectTf;

    // private Vector3 lastPos = Vector3.zero;

    // private Sequence moveSequence1;
    // private Sequence moveSequence2;

    // private Camera m_guideCamera;

    // private void Awake()
    // {
    //     tipBgRectTf.SetActiveEx(false);
    //     normalArrow.SetActive(false);
    //     fightArrow.SetActive(false);
    // }

    // public void Clear()
    // {
    //     if (moveSequence1 != null)
    //     {
    //         moveSequence1.Kill();
    //         moveSequence1 = null;
    //     }

    //     if (moveSequence2 != null)
    //     {
    //         moveSequence2.Kill();
    //         moveSequence2 = null;
    //     }

    //     canUpdate = false;
    // }

    // public void SetRefreshParams(GuideConfig guideConfig, RectTransform target_RectTf,Camera _guideCamera )
    // {
    //     m_guideCamera = _guideCamera;

    //     guideCfg = guideConfig;
    //     targetRectTf = target_RectTf;
    //     finger.SetActive(guideConfig.IsShowFinger && guideCfg.ClickType != 3);
    //     if (guideCfg.ObjType == 1)
    //     {
    //         fightArrow.SetActive(false);
    //     }
    //     else
    //     {
    //         fightArrow.SetActive(guideCfg.EffectPiecePos.Count > 1);
    //     }
    //     canUpdate = true;
    // }

    // private void LateUpdate()
    // {
    //     if (!canUpdate)
    //     {
    //         return;
    //     }

    //     switch (guideCfg.ObjType)
    //     {
    //         case 1:
    //         {
    //             Vector3 localPoint = Utility.WorldPointToLocalPoint(targetRectTf.position,m_guideCamera);
    //             localPoint.x -= targetRectTf.rect.width * (targetRectTf.pivot.x - 0.5f);
    //             localPoint.y -= targetRectTf.rect.height * (targetRectTf.pivot.y - 0.5f);

    //             finger.SetLocalPos(localPoint.x + 70, localPoint.y - 70f, 0f);
    //             normalArrow.SetLocalPos(localPoint.x + 70, localPoint.y - 120f, 0f);
    //             break;
    //         }
    //         case 2:
    //         {
                
    //             break;
    //         }
    //     }

    //     if (string.IsNullOrEmpty(guideCfg.Desc))
    //     {
    //         tipBgRectTf.SetActiveEx(false);
    //     }
    //     else
    //     {
    //         tipBgRectTf.SetActiveEx(true);
    //         tipBgRectTf.anchoredPosition = new Vector2(0, guideCfg.BgHeight);
    //         descText.text = guideCfg.Desc;
    //     }
    // }

    // private void SlowFastMovement()
    // {
    //     cfg.Common.VectorInt pos1 = guideCfg.EffectPiecePos[0];
    //     cfg.Common.VectorInt pos2 = guideCfg.EffectPiecePos[1];
    
    // }
}