using System.Collections.Generic;

namespace Game.GamePlay.ColorSort
{
	public sealed class ColorSortDepositLog
	{
		public int Col;

		public int BeadType;

		public int Q;

		public int Nth;

		public string Slot = "retire";

		public bool Keep;

		public List<ColorSortSpawnLog> Spawns = new List<ColorSortSpawnLog>();
	}
}

