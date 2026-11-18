using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishTilePlay
{
    [System.Serializable]
    public class CellModel
    {
        public bool IsFilled;
        public TileEffectType Effect = TileEffectType.None;
        public bool GoldenTreeIsX2;

        [SerializeField] float deltaX;
        [SerializeField] float deltaY;

        public float DeltaX => deltaX;
        public float DeltaY => deltaY;

        public void SetDelta(float x, float y)
        {
            deltaX = x;
            deltaY = y;
        }

        public void CheckResetEffect()
        {
            if(Effect == TileEffectType.GoldenTree)
            {
                Effect = TileEffectType.None;
                GoldenTreeIsX2 = false;
            }
        }
    }
}
