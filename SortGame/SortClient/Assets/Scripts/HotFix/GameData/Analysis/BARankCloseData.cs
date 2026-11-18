public class BARankCloseData : BaseAnaData
{
    public int Rank;
    public int num;
    public int leaderboard;
    public int total_level;

    public override void ClearData()
    {
        Rank = 0;
        num = 0;
        leaderboard = 0;
        total_level = 0;
    }
}
