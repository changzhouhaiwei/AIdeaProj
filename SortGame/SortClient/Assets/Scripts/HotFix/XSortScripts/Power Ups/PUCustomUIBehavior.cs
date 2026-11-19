using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XSortGameCore;

namespace FishXSortPlay
{
    public class PUCustomUIBehavior : PUUIActor
    {
        [Group("Refs")]
        [SerializeField] TextMeshProUGUI titleText;

        protected override void ApplyVisuals()
        {
            base.ApplyVisuals();

            PUCustomSettings customSettings = (PUCustomSettings)settings;

            titleText.text = customSettings.Title;
        }
    }
}
