using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FishXSortPlay
{
    public class PUUndoActor : PUActor
    {
        private PUUndoSettings customSettings;

        public override void Init()
        {
            customSettings = (PUUndoSettings)settings;
        }

        public override bool Activate(Action beforeUseFunc)
        {
            if (!LevelDirector.IsBusy)
            {
                IsBusy = true;

                RaycastController.Disable();

                LevelDirector.SetBusyState(true);

                bool retB =
                 LevelDirector.ReturnXSorts(customSettings.RevertElementsCount, () =>
                {
                    IsBusy = false;
                    RaycastController.Enable();
                    LevelDirector.SetBusyState(false);
                    PUDirector.PowerUpsUIController.RefreshStates();
                    
                }, beforeUseFunc);

                return retB;
            }

            return false;
        }

        public override void ResetBehavior()
        {
            IsBusy = false;
        }
    }
}
