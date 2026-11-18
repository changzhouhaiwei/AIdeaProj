using System;

namespace Game.GamePlay.ColorSort
{
	public sealed class ColorSortLock
	{
		public string Condition;

		public int Count;

		public int TypeId = -1;

		public bool IsEmpty => string.IsNullOrEmpty(Condition);

		public static ColorSortLock CollectCount(int count)
		{
			return new ColorSortLock
			{
				Condition = "collect_count",
				Count = count
			};
		}

		public static ColorSortLock CollectType(int typeId, int count)
		{
			return new ColorSortLock
			{
				Condition = "collect_type",
				TypeId = typeId,
				Count = count
			};
		}

		public ColorSortLock Clone()
		{
			return new ColorSortLock
			{
				Condition = Condition,
				Count = Count,
				TypeId = TypeId
			};
		}

		public ColorSortLock CloneForAppearance(int collectedColumns)
		{
			ColorSortLock colorSortLock = Clone();
			if (colorSortLock.Condition == "collect_count")
			{
				colorSortLock.Count = (int)Math.Min((long)colorSortLock.Count + (long)collectedColumns, 2147483647L);
			}
			return colorSortLock;
		}
	}
}

