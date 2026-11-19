using UnityEngine;
using XSortGameCore;

namespace FishXSortPlay
{
    public abstract class PUCustomSettings : PUConfig
    {
        [Group("Variables")]
        [SerializeField] string title;
        public string Title => title;
    }
}
