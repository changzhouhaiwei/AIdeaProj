using UnityEngine;
using UnityEngine.UI;

namespace FishFramework
{
    public class CanvasScalerEx : CanvasScaler
    {
        protected override void HandleScaleWithScreenSize()
        {
            float originalMatchWidthOrHeight = matchWidthOrHeight;
            if (Screen.width > 0 && (float)Screen.height / Screen.width > 2.3f)
            {
                matchWidthOrHeight = 0f;
            }

            base.HandleScaleWithScreenSize();
            matchWidthOrHeight = originalMatchWidthOrHeight;
        }
    }
}