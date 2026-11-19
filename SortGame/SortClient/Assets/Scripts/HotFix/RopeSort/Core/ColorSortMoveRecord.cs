using System.Collections.Generic;

namespace Game.GamePlay.ColorSort
{
	public sealed class ColorSortMoveRecord
	{
		public int Src;

		public int J;

		public int Dst;

		public int K;

		public int Head;

		public List<ColorSortDepositLog> Deposits = new List<ColorSortDepositLog>();

		public List<ColorSortRevealLog> Revealed = new List<ColorSortRevealLog>();

		public List<int> UnlockedColumns = new List<int>();

		public ColorSortState Snapshot;
	}
}

