using System.Collections.Generic;

namespace Game.GamePlay.ColorSort
{
	public sealed class ColorSortLevelSource
	{
		public bool IsObject;

		public bool HasId;

		public ColorSortNumber Id;

		public ColorSortNumber Capacity;

		public ColorSortNumber GroupsTotal;

		public bool InitialIsArray;

		public List<ColorSortColumnSource> Initial;

		public bool IncomingIsArray;

		public List<ColorSortColumnSource> Incoming;
	}
}

