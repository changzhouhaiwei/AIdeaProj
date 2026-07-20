namespace PotatoSiege
{
    public enum ClassId
    {
        Potato = 0,
        Gunner = 1,
        Puncher = 2,
        Medic = 3,
        Gambler = 4,
        Engineer = 5,
        Assassin = 6,
        Farmer = 7,
        Berserker = 8,
        Daoist = 9
    }

    public enum WeaponTag
    {
        Melee = 0,
        Ranged = 1,
        Element = 2,
        Engineer = 3
    }

    public enum ElementKind
    {
        None = 0,
        Burn = 1,
        Poison = 2,
        Shock = 3
    }

    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Legendary = 3
    }

    public enum GamePhase
    {
        ClassSelect,
        Wave,
        Shop,
        Victory,
        Defeat
    }

    public static class PSConst
    {
        public const float WaveTime = 60f;
        public const int MaxWaves = 20;
        public const int WeaponSlots = 6;
        public const float ArenaHalf = 9.5f;
        public const float AspdIntervalFloor = 0.08f;
        public const float CritSoftCap = 50f;
        public const float CritHardCap = 70f;
        public const float ShieldConvert = 0.6f;
        public const float ShieldMaxRatio = 0.5f;
    }
}
