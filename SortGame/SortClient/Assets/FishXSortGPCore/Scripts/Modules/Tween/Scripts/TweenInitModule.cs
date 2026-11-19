using UnityEngine;

namespace XSortGameCore
{
    [RegisterModule("Tween", false)]
    public class TweenInitModule : InitModule
    {
        public override string ModuleName => "Tween";

        public override void CreateComponent()
        {
            var go = new GameObject("Tween");
            go.transform.SetParent(Initializer.Transform);
            var t = go.AddComponent<Tween>();
            t.Init();
        }
    }
}
