using System.Collections.Generic;
using UnityEngine;
using XSortGameCore;

namespace FishXSortPlay
{
    public class LayersMap
    {
        private readonly List<LayerBoard> layers;
        public List<LayerBoard> Layers => layers;

        public LevelElement this[ElementPosition pos]
        {
            get => layers[pos.LayerId][pos];
            set => layers[pos.LayerId][pos] = value;
        }

        public LayerBoard GetLayerGrid(int layerId)
        {
            return layers[layerId];
        }

        public int Count => layers.Count;

        public LayerBoard this[int layerId] => layers[layerId];

        public LayersMap(LevelModel level, GameObject layersParent)
        {
            layers = new List<LayerBoard>(level.AmountOfLayers);

            for (int i = 0; i < level.AmountOfLayers; i++)
            {
                GameObject layerGameObject = new(string.Format("Layer {0}", i.ToString()));
                layerGameObject.transform.SetParent(layersParent.transform);
                layerGameObject.transform.ResetLocal();

                var size = (LevelDirector.Level.AmountOfLayers - i - 1) % 2 == 0 ? LevelDirector.EvenLayerSize : LevelDirector.OddLayerSize;
                LayerBoard layerRepresentation = new(layerGameObject, size.x, size.y);

                layers.Add(layerRepresentation);

                layerGameObject.name = $"Layer{i} size_{size.x}x{size.y}";
                //从高到低LayerGrid存储信息
            }
        }

        public void Clear()
        {
            foreach(LayerBoard layer in layers)
            {
                layer.Clear();
            }

            layers.Clear();
        }
    }
}