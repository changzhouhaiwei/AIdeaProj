
// using CodiceApp.EventTracking.Plastic;
using TMPro;
using UnityEngine;
using FishFramework;

public class UIGoldTreeCount : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI timeText;

    [SerializeField]
    GameObject noNetSp;

    [SerializeField]
    TextMeshProUGUI rankLb;

    bool bStart = false;
    float timeCount = 0f;
    int lastTime = -1;
    long mPrevCountdownRemaining = -1;

    void Update()
    {
        timeCount += Time.deltaTime;
        if ((int)timeCount == lastTime)
        {
            return;
        }

        if (!GameData.BFromStartScene || !bStart)
            return;

        if (GameData.Rank == null || GameData.GoldTreeActData == null || timeText == null)
            return;

        if (!GPUserDataManager.HasInit() || GPUserDataManager.GPUserData == null)
            return;

        lastTime = (int)timeCount;

        long remaining = GameData.GoldTreeActData.GetCountdownRemaining();
        GameData.GoldTreeActData.TryReportActivityCountdownEnded(mPrevCountdownRemaining, remaining);
        if (remaining >= 0)
            mPrevCountdownRemaining = remaining;

        if (GameData.Rank.GetCurrentBoardId() > GPUserDataManager.GPUserData.goldenTreeBoardId)
        {
            timeText.text = I2LocalizeMgr.GetString("UI_Finished");
            bStart = false;
        }
        else
        {
            var nowServerTime = GameData.GetServerTime();
            var endTime = GameData.GoldTreeActData.GetEndTime();
            var deltaTime = endTime - nowServerTime;

            if (deltaTime > 0)
            {
                timeText.text = FishFramework.Utility.GetHourMinSec(deltaTime);
            }
            else
            {
                timeText.text = I2LocalizeMgr.GetString("UI_Finished");
            }
        }
    }

    void Start()
    {
        if (noNetSp != null)
        {
            noNetSp.SetActive(false);
        }
        RefreshRankLabel();
    }

    void OnEnable()
    {
        RefreshRankLabel();
    }

    void RefreshRankLabel()
    {
        if (rankLb == null)
        {
            return;
        }

        bool showRank = false;
        string rankText = string.Empty;

        if (isActiveAndEnabled
            && GPUserDataManager.GPUserData.goldenTreeBoardId >= 0
            && GPUserDataManager.GPUserData.goldenTreeActPoint > 0)
        {
            try
            {
                var my = GameData.Rank.GetMyRankData();
                if (my != null && my.rank > 0)
                {
                    showRank = true;
                    rankText = "# " + my.rank;
                }
            }
            catch
            {
                showRank = false;
            }
        }

        SetRankLabelVisible(showRank, showRank ? rankText : string.Empty);
    }

    void SetRankLabelVisible(bool visible, string text = "")
    {
        if (rankLb == null)
        {
            return;
        }

        rankLb.gameObject.SetActive(visible);
        rankLb.text = visible ? text : string.Empty;
    }

    public void CheckStartCount()
    {
        bStart = false;
        mPrevCountdownRemaining = -1;
        if (GPUserDataManager.GPUserData.goldenTreeBoardId == -1)
        {
            gameObject.SetActive(false);
            SetRankLabelVisible(false);
        }
        else if (GameData.GoldTreeActData.GetBShowActBtn())
        {
            gameObject.SetActive(true);
            bStart = true;
            RefreshRankLabel();
        }
        else
        {
            gameObject.SetActive(false);
            SetRankLabelVisible(false);
        }
    }
}
