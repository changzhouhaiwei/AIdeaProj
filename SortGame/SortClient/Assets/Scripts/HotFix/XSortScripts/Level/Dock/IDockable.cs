using UnityEngine;
using System;

namespace FishXSortPlay
{
    public interface IDockable
    {
        void Clear();
        void SubmitMove(Transform position, Vector3 scale, SimpleCallback onFinish);
        void SubmitMove(Vector3 position, Vector3 scale, SimpleCallback onFinish);
        void DockShuffleMove(Transform position, SimpleCallback onFinish);
        void SetSortingOrder(int order);
        bool IsSameType(IDockable other);
        void MatchAnimation(float delay);
        void FinalMatchMoveAnimation(Vector3 dstPos);
        void FinalMatchAnimation(float delay ,Action act = null);
        

        int UniqueElementID { get; }
        Transform Transform { get; }
        Transform VisualsTransform { get; }
    }
}