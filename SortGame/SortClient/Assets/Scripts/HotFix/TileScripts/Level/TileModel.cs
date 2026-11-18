#pragma warning disable 0649

using System;
using UnityEngine;
using TileGameCore;

namespace FishTilePlay
{
    [System.Serializable]
    public class TileModel
    {
        public static  int GoldenActivityTileIdx = 25;
        [SerializeField] GameObject prefab;
        public GameObject Prefab => prefab;

        [SerializeField] int availableFromLevel;
        public int AvailableFromLevel => availableFromLevel; //根据关卡解锁,但是配置上似乎没用上.

        [SerializeField] int collectionID;
        public int CollectionID => collectionID;

        private Pool pool;
        public Pool Pool => pool;

        [SerializeField] private int index; //但是不可更改
        public int TileIndex => index;

        [NonSerialized] private TileSkinRuntimeData runtimeSkin;
        public TileSkinRuntimeData RuntimeSkin => runtimeSkin;
        public string RuntimeSkinName => runtimeSkin != null ? runtimeSkin.Name : "";

        public void Init(int index)
        {
            this.index = index;

            pool = new Pool(prefab, string.Format("Tile_{0}_{1}", prefab.name, index));
        }

        public void SetRuntimeSkin(TileSkinRuntimeData skin)
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
