using System;
using System.Collections.Generic;
using Game.GamePlay.ColorSort;
using MemoryPack;
using MemoryPack.Formatters;
using MemoryPack.Internal;

namespace Game.UI.ColorSort.Collectible
{
	[MemoryPackable(/*Could not decode attribute arguments.*/)]
	public sealed class ColorSortCollectible : IMemoryPackable<ColorSortCollectible>, IMemoryPackFormatterRegister
	{
		[Preserve]
		private sealed class ColorSortCollectibleFormatter : MemoryPackFormatter<ColorSortCollectible>
		{
			[Preserve]
			public override void Serialize(ref MemoryPackWriter writer, ref ColorSortCollectible value)
			{
				ColorSortCollectible.Serialize(ref writer, ref value);
			}

			[Preserve]
			public override void Deserialize(ref MemoryPackReader reader, ref ColorSortCollectible value)
			{
				ColorSortCollectible.Deserialize(ref reader, ref value);
			}
		}

		private const int MinCount = 4;

		private const int CountKinds = 3;

		private const int CountWeight = 50;

		private static readonly Random RandomGenerator;

		[MemoryPackInclude]
		[ES3Serializable]
		private int[] targetIds = Array.Empty<int>();

		[MemoryPackInclude]
		[ES3Serializable]
		private int[] columnIds = Array.Empty<int>();

		[MemoryPackInclude]
		[ES3Serializable]
		private int collectedMask;

		[MemoryPackIgnore]
		public int TotalCount => targetIds.Length;

		[MemoryPackIgnore]
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

		static ColorSortCollectible()
		{
			RandomGenerator = new Random();
			RegisterFormatter();
		}

		[Preserve]
		public static void RegisterFormatter()
		{
			if (!MemoryPackFormatterProvider.IsRegistered<ColorSortCollectible>())
			{
				MemoryPackFormatterProvider.Register<ColorSortCollectible>((MemoryPackFormatter<ColorSortCollectible>)(object)new ColorSortCollectibleFormatter());
			}
			if (!MemoryPackFormatterProvider.IsRegistered<ColorSortCollectible[]>())
			{
				MemoryPackFormatterProvider.Register<ColorSortCollectible[]>((MemoryPackFormatter<ColorSortCollectible[]>)(object)new ArrayFormatter<ColorSortCollectible>());
			}
			if (!MemoryPackFormatterProvider.IsRegistered<int[]>())
			{
				MemoryPackFormatterProvider.Register<int[]>((MemoryPackFormatter<int[]>)(object)new ArrayFormatter<int>());
			}
		}

		[Preserve]
		public static void Serialize(ref MemoryPackWriter writer, ref ColorSortCollectible? value)
		{
			if (value == null)
			{
				((MemoryPackWriter)(ref writer)).WriteNullObjectHeader();
				return;
			}
			((MemoryPackWriter)(ref writer)).WriteObjectHeader((byte)3);
			((MemoryPackWriter)(ref writer)).WriteUnmanagedArray<int>(value.targetIds);
			((MemoryPackWriter)(ref writer)).WriteUnmanagedArray<int>(value.columnIds);
			((MemoryPackWriter)(ref writer)).WriteUnmanaged<int>(ref value.collectedMask);
		}

		[Preserve]
		public static void Deserialize(ref MemoryPackReader reader, ref ColorSortCollectible? value)
		{
			byte b = default(byte);
			if (!((MemoryPackReader)(ref reader)).TryReadObjectHeader(ref b))
			{
				value = null;
				return;
			}
			int[] array;
			int[] array2;
			int num = default(int);
			if (b == 3)
			{
				if (value != null)
				{
					array = value.targetIds;
					array2 = value.columnIds;
					num = value.collectedMask;
					((MemoryPackReader)(ref reader)).ReadUnmanagedArray<int>(ref array);
					((MemoryPackReader)(ref reader)).ReadUnmanagedArray<int>(ref array2);
					((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref num);
					goto IL_00c8;
				}
				array = ((MemoryPackReader)(ref reader)).ReadUnmanagedArray<int>();
				array2 = ((MemoryPackReader)(ref reader)).ReadUnmanagedArray<int>();
				((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref num);
			}
			else
			{
				if (b > 3)
				{
					MemoryPackSerializationException.ThrowInvalidPropertyCount(typeof(ColorSortCollectible), (byte)3, b);
					return;
				}
				if (value == null)
				{
					array = null;
					array2 = null;
					num = 0;
				}
				else
				{
					array = value.targetIds;
					array2 = value.columnIds;
					num = value.collectedMask;
				}
				if (b != 0)
				{
					((MemoryPackReader)(ref reader)).ReadUnmanagedArray<int>(ref array);
					if (b != 1)
					{
						((MemoryPackReader)(ref reader)).ReadUnmanagedArray<int>(ref array2);
						if (b != 2)
						{
							((MemoryPackReader)(ref reader)).ReadUnmanaged<int>(ref num);
							_ = 3;
						}
					}
				}
				if (value != null)
				{
					goto IL_00c8;
				}
			}
			value = new ColorSortCollectible
			{
				targetIds = array,
				columnIds = array2,
				collectedMask = num
			};
			return;
			IL_00c8:
			value.targetIds = array;
			value.columnIds = array2;
			value.collectedMask = num;
		}
	}
}

