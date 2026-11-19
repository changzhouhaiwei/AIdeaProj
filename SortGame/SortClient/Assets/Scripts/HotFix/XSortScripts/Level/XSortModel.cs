#pragma warning disable 0649

using System;
using UnityEngine;
using XSortGameCore;

namespace FishXSortPlay
{
    [System.Serializable]
    public class XSortModel
    {
        [SerializeField] GameObject prefab;
        public GameObject Prefab => prefab;

        [SerializeField] int availableFromLevel;
        public int AvailableFromLevel => availableFromLevel; //根据关卡解锁,但是配置上似乎没用上.

        [SerializeField] int collectionID;
        public int CollectionID => collectionID;

        private Pool pool;
        public Pool Pool => pool;

        [SerializeField] private int index; //但是不可更改
        public int XSortIndex => index;

        [NonSerialized] private XSortSkinRuntimeData runtimeSkin;
        public XSortSkinRuntimeData RuntimeSkin => runtimeSkin;
        public string RuntimeSkinName => runtimeSkin != null ? runtimeSkin.Name : "";

        public void Init(int index)
        {
            this.index = index;

            pool = new Pool(prefab, string.Format("XSort_{0}_{1}", prefab.name, index));
        }

        public void SetRuntimeSkin(XSortSkinRuntimeData skin)
        {
            runtimeSkin = skin;
        }

        public void ClearRuntimeSkin()
        {
            runtimeSkin = null;
        }

        public void Unload()
        {
            if(pool != null)
            {
                PoolManager.DestroyPool(pool);

                pool = null;
            }
        }

        public override int GetHashCode()
        {
            return index * (collectionID + 1);
        }
    }
}
