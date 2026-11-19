using System.Collections.Generic;

namespace Game.GamePlay.ColorSort
{
	public sealed class ColorSortIncoming
	{
		public string Slot { get; }

		public int Jar { get; }

		public List<int> Beads { get; }

		public ColorSortLock Lock { get; }

		public ColorSortIncoming(string slot, int jar, List<int> beads, ColorSortLock colLock)
		{
			Slot = slot;
			Jar = jar;
			Beads = beads;
			Lock = colLock ?? new ColorSortLock();
		}
	}
}

