using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    [System.Serializable]
    public class CellModel
    {
        public bool IsFilled;
        public XSortEffectType Effect = XSortEffectType.None;

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
            if(Effect == XSortEffectType.GoldenTree)
            {
                Effect = XSortEffectType.None;
            }
        }
    }
}
