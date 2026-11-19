namespace Game.GamePlay.ColorSort
{
	public struct ColorSortHintMove
	{
		public int Src;

		public int J;

		public int Dst;

		public int K;

		public int Head;

		public bool Valid
		{
			get
			{
				if (Src >= 0 && Dst >= 0 && J >= 0)
				{
					return K > 0;
				}
				return false;
			}
		}
	}
}

