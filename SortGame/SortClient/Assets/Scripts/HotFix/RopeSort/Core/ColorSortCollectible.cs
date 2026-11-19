using System;
using System.Collections.Generic;
using Game.GamePlay.ColorSort;

namespace Game.UI.ColorSort.Collectible
{
	public sealed class ColorSortCollectible
	{
		private const int MinCount = 4;

		private const int CountKinds = 3;

		private const int CountWeight = 50;

		private static readonly Random RandomGenerator;
		private int[] targetIds = Array.Empty<int>();
		private int[] columnIds = Array.Empty<int>();
		private int collectedMask;
		public int TotalCount => targetIds.Length;
		public int CollectedCount
		{
			get
			{
				int num = 0;
				int num2 = collectedMask;
				while (num2 != 0)
				{
					num2 &= num2 - 1;
					num++;
				}
				return num;
			}
		}

		public static ColorSortCollectible Create(ColorSortLevel level, bool enabled, Random random = null)
		{
			ColorSortCollectible colorSortCollectible = new ColorSortCollectible();
			if (!enabled || level == null)
			{
				return colorSortCollectible;
			}
			if (random == null)
			{
				random = RandomGenerator;
			}
			List<int> list = new List<int>(level.Columns + level.Incoming.Count);
			colorSortCollectible.columnIds = new int[9];
			for (int i = 0; i < colorSortCollectible.columnIds.Length; i++)
			{
				colorSortCollectible.columnIds[i] = -1;
			}
			for (int j = 0; j < level.Columns; j++)
			{
				if (level.BoardJars[j] != -1)
				{
					colorSortCollectible.columnIds[j] = j;
					list.Add(j);
				}
			}
			for (int k = 0; k < level.Incoming.Count; k++)
			{
				if (level.Incoming[k].Slot == "jar")
				{
					list.Add(level.Columns + k);
				}
			}
			int val = 4 + random.Next(150) / 50;
			val = Math.Min(val, list.Count);
			colorSortCollectible.targetIds = new int[val];
			for (int l = 0; l < val; l++)
			{
				int index = random.Next(l, list.Count);
				int num = list[index];
				list[index] = list[l];
				list[l] = num;
				colorSortCollectible.targetIds[l] = num;
			}
			return colorSortCollectible;
		}

		public bool HasCollectible(int columnIndex)
		{
			if (columnIndex < 0 || columnIndex >= columnIds.Length || columnIds[columnIndex] < 0)
			{
				return false;
			}
			int num = columnIds[columnIndex];
			for (int i = 0; i < targetIds.Length; i++)
			{
				if (targetIds[i] == num && (collectedMask & (1 << i)) == 0)
				{
					return true;
				}
			}
			return false;
		}

		public bool ApplyDeposit(ColorSortLevel level, int columnIndex, int depositIndex)
		{
			if (targetIds.Length == 0 || level == null || columnIndex < 0 || columnIndex >= columnIds.Length)
			{
				return false;
			}
			bool result = false;
			int num = columnIds[columnIndex];
			for (int i = 0; i < targetIds.Length; i++)
			{
				int num2 = 1 << i;
				if (targetIds[i] == num && (collectedMask & num2) == 0)
				{
					collectedMask |= num2;
					result = true;
					break;
				}
			}
			ColorSortIncoming colorSortIncoming = level.IncomingAt(depositIndex);
			columnIds[columnIndex] = ((colorSortIncoming?.Slot == "jar") ? (level.Columns + depositIndex) : (-1));
			return result;
		}

		public ColorSortCollectible Clone()
		{
			if (targetIds.Length == 0)
			{
				return this;
			}
			return new ColorSortCollectible
			{
				targetIds = targetIds,
				columnIds = (int[])columnIds.Clone(),
				collectedMask = collectedMask
			};
		}

		static ColorSortCollectible() { RandomGenerator = new Random(); }
	}
}
