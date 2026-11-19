using UnityEngine;

namespace FishXSortPlay
{
    public interface IStackOccludedXSortEffect
    {
        Renderer OverlayRenderer { get; }

        void RefreshVisibility();
    }
}
