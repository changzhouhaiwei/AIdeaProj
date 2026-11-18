using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishTilePlay
{
    [System.Serializable]
    public class BackgroundModel
    {
        [SerializeField] GameObject backgroundPrefab;
        public GameObject BackgroundPrefab => backgroundPrefab;

        [SerializeField] int availableFromLevel;
        public int AvailableFromLevel => availableFromLevel;

        [SerializeField] int collectionId;
        public int CollectionId => collectionId;
    }
}