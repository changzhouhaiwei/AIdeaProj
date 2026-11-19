using UnityEngine;

namespace FishXSortPlay
{
    [CreateAssetMenu(fileName = "Power Ups Database", menuName = "Content/Power Ups/Database")]
    public class PUDatabase : ScriptableObject
    {
        [SerializeField] PUConfig[] powerUps;
        public PUConfig[] PowerUps => powerUps;
    }
}
