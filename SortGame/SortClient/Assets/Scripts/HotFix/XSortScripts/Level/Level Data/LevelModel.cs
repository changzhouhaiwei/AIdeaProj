#pragma warning disable 0649

using System;
using System.Collections.Generic;
using UnityEngine;
using XSortGameCore;

namespace FishXSortPlay
{

    [System.Serializable]
    public class LevelModel : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] Layer[] layers; //0是顶部 -从顶部到底部
        public int AmountOfLayers => layers.Length;

        [SerializeField, LevelEditorSetting] int bottomLayerWidth = 10;
        public int BottomLayerWidth => bottomLayerWidth;

        [SerializeField, LevelEditorSetting] int bottomLayerHeight = 10;
        public int BottomLayerHeight => bottomLayerHeight;

        [SerializeField, LevelEditorSetting] bool useInRandomizer;
        public bool UseInRandomizer => useInRandomizer;

        [SerializeField, LevelEditorSetting] int elementsPerLevel = 8;
        public int ElementsPerLevel => elementsPerLevel;

        // Runtime reward only; not drawn in Level Editor UI (kept as LevelEditorSetting so DisplayProperties skips it).
        [SerializeField, LevelEditorSetting] int coinsReward = 20;
        public int CoinsReward => coinsReward;
        
        // 已废弃。运行时忽略，旧关卡数据保留。同色只认难度配置第三段指向的同色表。
        [SerializeField, LevelEditorSetting] string sameColorListStr;
        [Obsolete("已废弃。运行时忽略 sameColorListStr。")]
        public string SameColorListStr => sameColorListStr;

        //困难还是简单模式
        [SerializeField, LevelEditorSetting] int easyHardValue;
        public int EasyHardValue => easyHardValue; //同色棋子配置

        [SerializeField, LevelEditorSetting] string editorNote; // used only in level editor

        /// <summary>
        /// Display name after | in the editor list. Kept equal to the LevelModel asset file name
        /// (e.g. Android_Level_1 / IOS_Level_1). Editable via right-click Rename Level.
        /// </summary>
        [SerializeField, LevelEditorSetting] string dataId;
        public string DataId => dataId;

        [SerializeField,LevelEditorSetting] float showLayerScale = 1.0f;
        public float ShowLayerScale => showLayerScale;

        [SerializeField, LevelEditorSetting] float showLayerDeltaX = 0;
        public float ShowLayerDeltaX => showLayerDeltaX;

        [SerializeField, LevelEditorSetting] float showLayerDeltaY = 0;
        public float ShowLayerDeltaY => showLayerDeltaY;

        [SerializeField, LevelEditorSetting] BoxCoverType boxCoverType = BoxCoverType.Coke;
        public BoxCoverType BoxCoverType => boxCoverType;

        /// <summary>可乐箱：暂存区用无关卡号底板，并给目标栏留位。裂变棋由 GameLevel.iqchallenge 决定。</summary>
        public bool UsesSpecialDockPlate => HasBoxCovers;

        public int SetsAmount
        {
            get
            {
                int filled = GetAmountOfFilledCells();
                return (filled - (filled % 3)) / 3;
            }
        }
        public float Difficulty
        {
            get
            {
                int sets = SetsAmount;
                return Mathf.Round(Mathf.Clamp(sets / (float)elementsPerLevel, 1, float.MaxValue) * 10.0f) * 0.1f;
            }
        }

        public Layer GetLayer(int i)
        {
            if (i < AmountOfLayers && i >= 0) return layers[i];

            return null;
        }

        /// <summary>最大行 r、最大列 c、层数。列表前缀用这个，不含 sets。</summary>
        public string GetLayoutSizePrefix()
        {
            int maxRows = 0;
            int maxCols = 0;
            int layerCount = layers == null ? 0 : layers.Length;
            for (int i = 0; i < layerCount; i++)
            {
                Layer layer = layers[i];
                if (layer == null)
                {
                    continue;
                }

                int rows = layer.AmountOfRows;
                if (rows > maxRows)
                {
                    maxRows = rows;
                }

                LayerRow first = rows > 0 ? layer.GetRow(0) : null;
                int cols = first != null ? first.AmountOfCells : 0;
                if (cols > maxCols)
                {
                    maxCols = cols;
                }
            }

            return maxRows + "r_" + maxCols + "c_" + layerCount;
        }

        /// <summary>文件名：11r_11c_112_3406_426。112 = 第 1 层 12 个，3406 = 中间层 34 和 06，426 = 第 4 层 26 个。超过 8 层只写前 4 层和后 4 层。</summary>
        public string GetLayoutFileName()
        {
            int layerCount = layers == null ? 0 : layers.Length;
            int maxRows = 0;
            int maxCols = 0;
            int maxCount = 0;
            bool trimLayers = layerCount > 8;
            int tailStart = layerCount - 4;
            var counts = new int[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                Layer layer = layers[i];
                int filled = layer != null ? layer.GetAmountOfFilledCells() : 0;
                counts[i] = filled;
                bool kept = !trimLayers || i < 4 || i >= tailStart;
                if (kept && filled > maxCount)
                {
                    maxCount = filled;
                }

                if (layer == null)
                {
                    continue;
                }

                int rows = layer.AmountOfRows;
                if (rows > maxRows)
                {
                    maxRows = rows;
                }

                LayerRow firstRow = rows > 0 ? layer.GetRow(0) : null;
                int cols = firstRow != null ? firstRow.AmountOfCells : 0;
                if (cols > maxCols)
                {
                    maxCols = cols;
                }
            }

            string name = maxRows + "r_" + maxCols + "c";
            if (layerCount <= 0)
            {
                return name;
            }

            int width = maxCount >= 100 ? maxCount.ToString().Length : 2;
            string first = "1" + counts[0].ToString().PadLeft(width, '0');
            if (layerCount == 1)
            {
                return name + "_" + first;
            }

            string middle = "";
            for (int i = 1; i < layerCount - 1; i++)
            {
                if (trimLayers && i >= 4 && i < tailStart)
                {
                    continue;
                }

                middle += counts[i].ToString().PadLeft(width, '0');
            }

            string last = layerCount + counts[layerCount - 1].ToString().PadLeft(width, '0');
            name += "_" + first;
            if (middle.Length > 0)
            {
                name += "_" + middle;
            }

            return name + "_" + last;
        }

        public int GetAmountOfFilledCells()
        {
            int counter = 0;

            for (int i = 0; i < AmountOfLayers; i++)
            {
                counter += layers[i].GetAmountOfFilledCells();
            }

            return counter;
        }

        public bool HasBoxCovers
        {
            get
            {
                if (layers == null)
                {
                    return false;
                }

                for (int i = 0; i < AmountOfLayers; i++)
                {
                    IReadOnlyList<BoxCoverPlacementData> configurations = layers[i].BoxCovers;
                    for (int j = 0; j < configurations.Count; j++)
                    {
                        if (configurations[j] != null && configurations[j].Count > 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public int ConfiguredBoxCoverCount
        {
            get
            {
                int count = 0;
                if (layers == null)
                {
                    return 0;
                }

                for (int i = 0; i < AmountOfLayers; i++)
                {
                    IReadOnlyList<BoxCoverPlacementData> configurations = layers[i].BoxCovers;
                    for (int j = 0; j < configurations.Count; j++)
                    {
                        count += configurations[j]?.Count ?? 0;
                    }
                }

                return count;
            }
        }

        /// <summary>True if any cell has the given XSort effect (used by Level Editor list labels).</summary>
        public bool ContainsEffect(XSortEffectType effectType)
        {
            if (layers == null || effectType == XSortEffectType.None)
            {
                return false;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                Layer layer = layers[i];
                if (layer == null)
                {
                    continue;
                }

                for (int r = 0; r < layer.AmountOfRows; r++)
                {
                    LayerRow row = layer.GetRow(r);
                    if (row == null)
                    {
                        continue;
                    }

                    for (int c = 0; c < row.AmountOfCells; c++)
                    {
                        CellModel cell = row.GetCell(c);
                        if (cell != null && cell.Effect == effectType)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public string GetLevelCurCntStr()
        {
            string retStr = "";

            for (int i = 0; i < AmountOfLayers; i++)
            {
                var count = layers[i].GetAmountOfFilledCells();
                var str = $"l{i}_num:{count}";
                retStr += str + "\n";
            }

            return retStr;
        }
    }
}