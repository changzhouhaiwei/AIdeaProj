using System.Collections.Generic;
using Spine.Unity;
using XSortGameCore;
using FishXSortPlay;
using UnityEngine;

public class XSortHint : MonoBehaviour
{
    [SerializeField]
    List<GameObject> effList;

    List<XSortActor> targetList = new();
    public void Start()
    {
        HideEff();
    }

    public bool ShowEff()
    {
        targetList.Clear();
        List<GameObject> newList = new();
        var list = LevelDirector.GetHintXSorts();
        if (!list.IsNullOrEmpty())
        {
            GameBAData.ExcuteStageEvent(12);
            foreach (var item in list)
            {
                newList.Add(item.gameObject);
                targetList.Add(item);
            }
        }
        else
        {
            return false;
        }

        int idx = 0;
        foreach (var item in newList)
        {
            if (idx < 3)
            {
                var eff = effList[idx];
                eff.transform.position = item.transform.position;
                eff.SetActive(true);
                eff.GetComponent<SkeletonGraphic>().AnimationState.SetAnimation(0, "animation", transform);
                item.GetComponent<XSortActor>().PlayHintAnim();
                float scal = item.GetComponent<XSortActor>().VisualsTransform.localScale.x;
                eff.transform.localScale = Vector3.one * scal;
            }
            idx++;
        }


        return true;

    }

    public void HideEff()
    {
        foreach (var item in effList)
        {
            item.SetActive(false);
        }

        foreach (var item in targetList)
        {
            item.GetComponent<XSortActor>().StopHintAnim();
        }   
    }
}