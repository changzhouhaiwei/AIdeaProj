public static partial class Constants
{
    public const int StaminaId = 3;
}

/// <summary>
/// 战斗相关常量
/// </summary>
public static partial class Constants
{
    public const float CameraMaxWidthSize = 11f; // 相机宽度最大大小
    
    // 战斗升级宝箱掉落ID
    public const int UpgradeSkillDropId = 4;

    // 打完Boss后宝箱掉落ID
    public const int BossSkillDropId = 5;

    public const string DamagePrefabPath = "Assets/CABRes/Prefabs/Fight/DamageText.prefab";
    public const int NormalSortingOrder = 0;
    public const int AttackSortingOrder = 10;
    public const int TopSortingOrder = 100;

    public const int InitialPromotedCarryHeroCount = 6;

    // 棋子交换的速度
    public const float SwapSpeed = 6f;

    // 棋子移除速度
    public const float RemoveSpeed = 5;

    //棋子创建缩放时间
    public const float CreatingScaleDuration = 0.3f;

    //棋子创建后等待的时间
    public const float CreatingDelayDuration = 0.4f;

    //棋盘初始上来的速度
    public const float CreatingInitRiseSpeed = 20f;

    //生成的棋子的移动时间
    public const float RiseTimeInBoard = 0.2f;

    //生成的棋子在晋级时的移动时间
    public const float RiseTimeInPromotion = 0.4f;

    //晋级时棋子的移动时间
    public const float MoveTimeInPromotion = 2f;

    //棋子在晋级时棋子消除时间
    public const float PieceRemoveTimeInPromotion = 0.22f;

    //棋子在晋级时特效消除时间
    public const float ParticleRemoveTimeInPromotion = 0.6f;

    //棋子在晋级时拾取间隔时间
    public const float PickupTimeInPromotion = 0.22f;

    //棋子在晋级后创建间隔时间
    public const float CreateTimeInPromotion = 0.22f;

    //棋子在合成后创建间隔时间
    public const float CreateTimeInMatched = 0f;
}