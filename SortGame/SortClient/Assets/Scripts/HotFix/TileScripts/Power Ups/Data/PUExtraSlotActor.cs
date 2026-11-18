
using System;
using UnityEngine;

namespace FishTilePlay
{
    public class PUExtraSlotActor : PUActor
    {
        private PUExtraSlotSettings customSettings;

        public override void Init()
        {
            customSettings = (PUExtraSlotSettings)settings;
        }

        public override bool Activate(Action beforeUseFunc)
        {
            return false;
        }

        public override void ResetBehavior()
        {
            IsBusy = false;
        }
    }
}
