using UnityEngine;
using TileGameCore;


namespace FishTilePlay
{

    [CreateAssetMenu(fileName = "PU Undo Settings", menuName = "Content/Power Ups/PU Undo Settings")]
    public class PUUndoSettings : PUCustomSettings
    {
        [LineSpacer("Settings")]
        [SerializeField] int revertElementsCount = 1;
        public int RevertElementsCount => revertElementsCount;

        public override void Init()
        {

        }
    }
}
