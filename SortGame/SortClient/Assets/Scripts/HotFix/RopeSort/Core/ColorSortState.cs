using System.Collections.Generic;
using Game.UI.ColorSort.Collectible;

namespace Game.GamePlay.ColorSort
{
	public sealed class ColorSortState
	{
		public int Done;

		public int LateLeft;

		public bool ExtraRopeUsed;

		public ColorSortCollectible Collectibles;

		public List<List<int>> Cols { get; } = new List<List<int>>();


		public List<int> Jars { get; } = new List<int>();


		public List<int> Caps { get; } = new List<int>();


		public List<bool> Gone { get; } = new List<bool>();


		public List<ColorSortLock> Locks { get; } = new List<ColorSortLock>();


		public List<int> Cups { get; } = new List<int>();

	}
}

