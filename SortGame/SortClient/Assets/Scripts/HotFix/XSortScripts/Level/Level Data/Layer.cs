using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishXSortPlay
{
    [System.Serializable]
    public class Layer
    {
        [SerializeField] LayerRow[] rows;

        [SerializeField] float deltaX;
        [SerializeField] float deltaY;
        [SerializeField] List<BoxCoverPlacementData> boxCovers = new();

        public float DeltaX => deltaX;
        public float DeltaY => deltaY;
        public IReadOnlyList<BoxCoverPlacementData> BoxCovers
        {
            get
            {
                return boxCovers ?? (IReadOnlyList<BoxCoverPlacementData>)
                    System.Array.Empty<BoxCoverPlacementData>();
            }
        }

        public int AmountOfRows => rows.Length;

        public LayerRow this[int i]
        {
            get => rows[i];
        }

        //获得当前行
        public LayerRow GetRow(int i)
        {
            if (i < AmountOfRows && i >= 0) return rows[i];

            return null;
        }

        public int GetAmountOfFilledCells()
        {
            int counter = 0;

            for (int i = 0; i < AmountOfRows; i++)
            {
                counter += rows[i].GetAmountOfFilledCells();
            }

            return counter;
        }

        public BoxCoverPlacementData GetBoxCoverConfiguration(BoxCoverSize size)
        {
            EnsureBoxCoverConfigurations();
            return boxCovers.Find(configuration => configuration != null && configuration.Size == size);
        }

        public void ClearBoxCovers()
        {
            EnsureBoxCoverConfigurations();
            for (int i = 0; i < boxCovers.Count; i++)
            {
                boxCovers[i]?.Clear();
            }
        }

        private void EnsureBoxCoverConfigurations()
        {
            boxCovers ??= new List<BoxCoverPlacementData>();
            AddMissingConfiguration(BoxCoverSize.Size2x2);
            AddMissingConfiguration(BoxCoverSize.Size2x3);
            AddMissingConfiguration(BoxCoverSize.Size3x3);
        }

        private void AddMissingConfiguration(BoxCoverSize size)
        {
            if (!boxCovers.Exists(configuration => configuration != null && configuration.Size == size))
            {
                boxCovers.Add(new BoxCoverPlacementData(size));
            }
        }
    }
}
