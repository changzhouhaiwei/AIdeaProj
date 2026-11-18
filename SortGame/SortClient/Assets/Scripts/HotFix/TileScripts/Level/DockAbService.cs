using FishTilePlay;
using UnityEngine;

public class DockAbService : MonoBehaviour //ab测试开启第七格子判定,现已经移除,默认都是false
{
    public void WatchAdByClickDockSlot()
    {

    }

    public int GetMaxShowSlotCnt()
    {
        return 7;
    }

    public int GetMaxSlotCnt()
    {
        return 7;
    }

    //为了AB测试提取出来的数据.
    public int GetRestEmptyCnt()
    {
        int occupiedCnt = LevelDirector.Dock.GetEffectiveOccupiedSlotsCount();
        return GetMaxSlotCnt() - occupiedCnt;
    }
}
