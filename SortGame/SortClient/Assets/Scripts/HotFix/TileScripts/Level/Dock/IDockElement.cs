using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishTilePlay
{
    public interface IDockElement
    {
        Transform Transform { get; }
        GameObject GameObject { get; }

        void MoveTo(Vector3[] path, bool isSlotsMovement, SimpleCallback onCompleted);
    }
}
