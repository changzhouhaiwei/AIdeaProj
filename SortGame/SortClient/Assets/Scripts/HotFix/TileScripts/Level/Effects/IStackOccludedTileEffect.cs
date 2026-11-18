using UnityEngine;

namespace FishTilePlay
{
    public interface IStackOccludedTileEffect
    {
        Renderer OverlayRenderer { get; }

        void RefreshVisibility();
    }
}
