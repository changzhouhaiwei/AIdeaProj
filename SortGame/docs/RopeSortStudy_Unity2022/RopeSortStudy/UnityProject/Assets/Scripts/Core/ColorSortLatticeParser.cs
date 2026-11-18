using System;
using System.Collections.Generic;

namespace Game.GamePlay.ColorSort
{
	public static class ColorSortLatticeParser
	{
		private sealed class Vine
		{
			public int Color = -1;

			public bool FreezeByColor;

			public int FreezeColor = -1;

			public int FreezeCount;

			public bool HasFreeze;

			public List<(int Color, bool Mystery)> Flowers = new List<(int, bool)>();

			public bool IsHolder => Color == -1;
		}

		private const string Base62 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

		private const int Holder = -1;

		private const int ElementMystery = 1;

		private const int BitsPerMask = 5;

		private const int MaxMaskFields = 6;

		private const int AttrFreezeCount = 1;

		private const int AttrFreezeColor = 2;

		public static bool LooksLike(string content)
		{
			if (string.IsNullOrWhiteSpace(content))
			{
				return false;
			}
			string text = content.Trim();
			if (text[0] == '{')
			{
				return false;
			}
			int num = text.IndexOf(':');
			if (num <= 0)
			{
				return false;
			}
			string text2 = text.Substring(0, num);
			int num2 = text2.IndexOf(';');
			if (num2 > 0)
			{
				return num2 < text2.Length - 1;
			}
			return false;
		}

		public static ColorSortLevel Parse(string content, int? overrideId = null)
		{
			return ColorSortLevel.Load(ToSource(content, overrideId));
		}

		public static ColorSortLevelSource ToSource(string content, int? overrideId = null)
		{
			if (string.IsNullOrWhiteSpace(content))
			{
				throw new FormatException("点阵串为空");
			}
			string text = content.Trim();
			int num = text.IndexOf(':');
			if (num <= 0)
			{
				throw new FormatException("点阵串缺少 ':'");
			}
			string text2 = text.Substring(0, num);
			string text3 = text.Substring(num + 1);
			string[] array = text2.Split(';');
			if (array.Length != 2 || !int.TryParse(array[0], out var result) || !int.TryParse(array[1], out var result2))
			{
				throw new FormatException("点阵头部非法: " + text2);
			}
			if (result <= 0 || result2 <= 0)
			{
				throw new FormatException("capacity/boardCount 必须 > 0: " + text2);
			}
			string[] array2 = ((text3.Length == 0) ? Array.Empty<string>() : text3.Split(';'));
			if (array2.Length < result2)
			{
				throw new FormatException($"开局盆数 {result2}，实际只有 {array2.Length} 段");
			}
			List<Vine> list = new List<Vine>(array2.Length);
			for (int i = 0; i < array2.Length; i++)
			{
				string where = ((i < result2) ? $"开局第 {i + 1} 藤" : $"来藤第 {i - result2 + 1}");
				list.Add(DecodeVine(array2[i], where));
			}
			int num2 = 0;
			foreach (Vine item in list)
			{
				if (!item.IsHolder)
				{
					num2++;
				}
			}
			if (num2 < 2)
			{
				throw new FormatException($"目标盆数过少: {num2}");
			}
			ColorSortLevelSource colorSortLevelSource = new ColorSortLevelSource
			{
				IsObject = true,
				Capacity = Num(result),
				GroupsTotal = Num(num2),
				InitialIsArray = true,
				IncomingIsArray = true,
				Initial = new List<ColorSortColumnSource>(result2),
				Incoming = new List<ColorSortColumnSource>(Math.Max(0, array2.Length - result2))
			};
			if (overrideId.HasValue)
			{
				colorSortLevelSource.HasId = true;
				colorSortLevelSource.Id = Num(overrideId.Value);
			}
			for (int j = 0; j < result2; j++)
			{
				colorSortLevelSource.Initial.Add(ToColumn(list[j]));
			}
			for (int k = result2; k < list.Count; k++)
			{
				colorSortLevelSource.Incoming.Add(ToColumn(list[k]));
			}
			return colorSortLevelSource;
		}

		private static Vine DecodeVine(string token, string where)
		{
			if (token == null)
			{
				throw new FormatException(where + ": 空段");
			}
			string[] array = token.Split('.');
			string[] array2 = array[0].Split(',');
			Vine vine = new Vine();
			if (array2[0].Length == 0)
			{
				vine.Color = -1;
			}
			else
			{
				if (array2[0].Length != 1)
				{
					throw new FormatException(where + ": 藤色应为单字符，实际「" + array2[0] + "」");
				}
				vine.Color = Value(array2[0][0], where);
			}
			for (int i = 1; i < array2.Length; i++)
			{
				string text = array2[i];
				if (text.Length == 0)
				{
					throw new FormatException(where + ": 空的冰封属性");
				}
				int[] array3 = new int[text.Length];
				for (int j = 0; j < text.Length; j++)
				{
					array3[j] = Value(text[j], where);
				}
				int num = ((array3[0] == 1) ? 1 : ((array3[0] == 2) ? 2 : (-1)));
				if (num < 0)
				{
					throw new FormatException($"{where}: 未定义冰封属性 {text[0]}");
				}
				if (array3.Length != 1 + num)
				{
					throw new FormatException(where + ": 冰封参数个数不对");
				}
				if (vine.HasFreeze)
				{
					throw new FormatException(where + ": 多种冰封");
				}
				vine.HasFreeze = true;
				if (array3[0] == 1)
				{
					vine.FreezeByColor = false;
					vine.FreezeCount = array3[1];
				}
				else
				{
					vine.FreezeByColor = true;
					vine.FreezeColor = array3[1];
					vine.FreezeCount = array3[2];
				}
			}
			int[] array4 = ((array.Length > 1) ? new int[array[1].Length] : Array.Empty<int>());
			for (int k = 0; k < array4.Length; k++)
			{
				array4[k] = Value(array[1][k], where);
			}
			int[] array5 = new int[array4.Length];
			int num2 = -1;
			for (int l = 2; l < array.Length; l++)
			{
				string text2 = array[l];
				string[] array6 = text2.Split(',');
				if (array6.Length < 2)
				{
					throw new FormatException(where + ": 花元素段「" + text2 + "」格式错误");
				}
				for (int m = 0; m < array6.Length; m++)
				{
					if (array6[m].Length != 1)
					{
						throw new FormatException(where + ": 花元素段「" + text2 + "」格式错误");
					}
				}
				if (array6.Length - 1 > 6)
				{
					throw new FormatException(where + ": 掩码太多");
				}
				int num3 = Value(array6[0][0], where);
				if (num3 < 0 || num3 >= array4.Length || num3 <= num2)
				{
					throw new FormatException($"{where}: 神秘花位置非法 {num3}");
				}
				int num4 = 0;
				for (int n = 1; n < array6.Length; n++)
				{
					int num5 = Value(array6[n][0], where);
					if (num5 >= 32)
					{
						throw new FormatException(where + ": 掩码越界");
					}
					num4 |= num5 << 5 * (n - 1);
				}
				if (((uint)num4 & 0xFFFFFFFEu) != 0)
				{
					throw new FormatException(where + ": 未定义元素位");
				}
				array5[num3] = num4;
				num2 = num3;
			}
			for (int num6 = 0; num6 < array4.Length; num6++)
			{
				vine.Flowers.Add((array4[num6], (array5[num6] & 1) != 0));
			}
			if (vine.Flowers.Count > 0 && vine.Flowers[vine.Flowers.Count - 1].Mystery)
			{
				throw new FormatException(where + ": 栈顶不能是神秘花");
			}
			return vine;
		}

		private static ColorSortColumnSource ToColumn(Vine vine)
		{
			string text = (vine.IsHolder ? "-" : vine.Color.ToString());
			List<string> list = new List<string>(vine.Flowers.Count);
			foreach (var flower in vine.Flowers)
			{
				list.Add(flower.Mystery ? ("?" + flower.Color) : flower.Color.ToString());
			}
			string column = text + "|" + string.Join(",", list);
			ColorSortColumnSource colorSortColumnSource = new ColorSortColumnSource
			{
				IsObject = true,
				Column = column,
				ColumnType = "default"
			};
			if (!vine.HasFreeze)
			{
				return colorSortColumnSource;
			}
			colorSortColumnSource.ColumnType = "unlock";
			colorSortColumnSource.LockInfoIsObject = true;
			colorSortColumnSource.LockCount = Num(vine.FreezeCount);
			if (vine.FreezeByColor)
			{
				colorSortColumnSource.LockCondition = "collect_type";
				colorSortColumnSource.LockTypeId = Num(vine.FreezeColor);
			}
			else
			{
				colorSortColumnSource.LockCondition = "collect_count";
			}
			return colorSortColumnSource;
		}

		private static ColorSortNumber Num(int value)
		{
			ColorSortNumber result = default(ColorSortNumber);
			result.IsNumeric = true;
			result.Parsed = true;
			result.Value = value;
			result.RawText = value.ToString();
			return result;
		}

		private static int Value(char c, string where)
		{
			int num = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(c);
			if (num < 0)
			{
				throw new FormatException($"{where}: 非法字符 '{c}'");
			}
			return num;
		}
	}
}

