/// <summary>
/// 角色基础属性
/// </summary>
public struct RoleProperty
{
    /// <summary>
    /// 力量
    /// </summary>
    public float strength;

    /// <summary>
    /// 智力
    /// </summary>
    public float intelligence;

    /// <summary>
    /// 敏捷
    /// </summary>
    public float agility;

    /// <summary>
    /// 体质
    /// </summary>
    public float constitution;

    /// <summary>
    /// 幸运
    /// </summary>
    public float criticalRate;

    public RoleProperty(float strength, float intelligence, float agility, float constitution, float criticalRate)
    {
        this.strength = strength;
        this.intelligence = intelligence;
        this.agility = agility;
        this.constitution = constitution;
        this.criticalRate = criticalRate;
    }

    public static RoleProperty Zero = new RoleProperty(0, 0, 0, 0, 0);

    // 定义加法和减法的用法
    public static RoleProperty operator +(RoleProperty a, RoleProperty b)
    {
        return new RoleProperty(
            a.strength + b.strength,
            a.intelligence + b.intelligence,
            a.agility + b.agility,
            a.constitution + b.constitution,
            a.criticalRate + b.criticalRate
        );
    }

    public static RoleProperty operator -(RoleProperty a, RoleProperty b)
    {
        return new RoleProperty(
            a.strength - b.strength,
            a.intelligence - b.intelligence,
            a.agility - b.agility,
            a.constitution - b.constitution,
            a.criticalRate - b.criticalRate
        );
    }
}

public struct RoleAttribute
{
    public int attack;
    public int hp;
    public float attackSpeed;
    public int criticalRate;
}