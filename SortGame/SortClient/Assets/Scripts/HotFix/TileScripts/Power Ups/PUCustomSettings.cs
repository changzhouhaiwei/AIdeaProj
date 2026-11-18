using UnityEngine;
using TileGameCore;

namespace FishTilePlay
{
    public abstract class PUCustomSettings : PUConfig
    {
        [Group("Variables")]
        [SerializeField] string title;
        public string Title => title;
    }
}
