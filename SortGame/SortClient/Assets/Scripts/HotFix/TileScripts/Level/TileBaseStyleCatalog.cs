using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// 经典底板列表。没有序号。破碎直接用预制体，空着则用当前关卡主题。
    /// </summary>
    [CreateAssetMenu(menuName = "Content/Tile Base Style Catalog", fileName = "TileBaseStyleCatalog")]
    public class TileBaseStyleCatalog : ScriptableObject
    {
        public List<Entry> styles = new List<Entry>();

        [Serializable]
        public class Entry
        {
            public string styleName;
            public string description;
            public Sprite tileBase;
            public Sprite unknown;
            public GameObject brokenEffect;
            public AudioClip brokenSound;
            public AudioClip collectSound;
            public GameObject trail;
        }
    }
}
