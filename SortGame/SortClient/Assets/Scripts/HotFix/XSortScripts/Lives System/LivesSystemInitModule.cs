using UnityEngine;
using XSortGameCore;

namespace FishXSortPlay
{
    [RegisterModule("Lives System", core: false)]
    public class LivesSystemInitModule : InitModule
    {
        public override string ModuleName => "Lives System";

        [SerializeField] LivesModel livesData;

        public override void CreateComponent()
        {
            if (livesData == null)
            {
                Debug.LogWarning("LivesModel is not assigned in Project Init Settings", this);

                return;
            }

            LivesSystem.Init(livesData);
        }
    }
}