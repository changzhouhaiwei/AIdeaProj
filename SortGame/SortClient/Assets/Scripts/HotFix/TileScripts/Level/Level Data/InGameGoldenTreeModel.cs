using Unity.Mathematics;

public class InGameGoldenTreeModel
{
    private int _maxGoldTreeCnt;
    public int MaxGoldenTreeCnt
    {
        get { return _maxGoldTreeCnt; }
        set { _maxGoldTreeCnt = math.min(6, value); }
    }


    private int _currentGoldTreeCnt;
    public int CurrentGoldenTreeCnt
    {
        get { return _currentGoldTreeCnt; }
        set { _currentGoldTreeCnt = value; }
    }

    private int _currentGoldTreeScore;
    public int CurrentGoldenTreeScore
    {
        get { return _currentGoldTreeScore; }
        set { _currentGoldTreeScore = value; }
    }


    public void Clear()
    {
        MaxGoldenTreeCnt = 0;
        CurrentGoldenTreeCnt = 0;
        CurrentGoldenTreeScore = 0;
    }

    public void IncreaseCurrentCnt(int scoreAdd = 1)
    {
        CurrentGoldenTreeCnt++;
        CurrentGoldenTreeCnt = math.min(MaxGoldenTreeCnt, CurrentGoldenTreeCnt);
        CurrentGoldenTreeScore += scoreAdd;
        CurrentGoldenTreeScore = math.min(12, CurrentGoldenTreeScore);
    }

    public bool GetBPerfect()
    {
        return CurrentGoldenTreeCnt >= MaxGoldenTreeCnt && MaxGoldenTreeCnt > 0;
    }
}
