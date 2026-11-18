using System.Collections.Generic;
using Spine.Unity;
using TileGameCore;
using FishTilePlay;
using UnityEngine;

public class TileHint : MonoBehaviour
{
    [SerializeField]
    List<GameObject> effList;

    List<TileActor> targetList = new();
    public void Start()
    {
        HideEff();
    }

    public bool ShowEff()
    {
        targetList.Clear();
        List<GameObject> newList = new();
        var list = LevelDirector.GetHintTiles();
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
                item.GetComponent<TileActor>().PlayHintAnim();
                float scal = item.GetComponent<TileActor>().VisualsTransform.localScale.x;
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
            item.GetComponent<TileActor>().StopHintAnim();
        }   
    }
}