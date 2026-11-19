using System;
using System.Collections.Generic;

namespace Game.GamePlay.ColorSort
{
	public sealed class ColorSortLevel
	{
		private sealed class DecodeResult
		{
			public int Jar = -1;

			public List<int> Beads = new List<int>();

			public bool Retire;

			public ColorSortLock Lock = new ColorSortLock();

			public string Error;
		}

		private sealed class UnwrapResult
		{
			public object Raw;

			public ColorSortLock Lock = new ColorSortLock();

			public string Error;
		}

		private sealed class LockParseResult
		{
			public ColorSortLock Lock = new ColorSortLock();

			public string Error;
		}

		public const string SlotJar = "jar";

		public const string SlotPark = "park";

		public const string SlotRetire = "retire";

		public const int MaxLatePark = 4;

		public const int MaxBaseColumns = 8;

		public const int MaxTotalColumns = 9;

		public const int NoJar = -1;

		public const string ColSep = "|";

		public const string ParkChar = "-";

		public const string RetireChar = "x";

		public const int HiddenBase = 1000;

		public const string HiddenChar = "?";

		public const string LockType = "unlock";

		public const string LockCollectType = "collect_type";

		public const string LockCollectCount = "collect_count";

		private readonly List<int[]> _futureTally = new List<int[]>();

		private readonly List<string> _errors = new List<string>();

		public int Id { get; private set; }

		public int Capacity { get; private set; }

		public int GroupsTotal { get; private set; }

		public int Types { get; private set; }

		public int Columns { get; private set; }

		public int InitJars { get; private set; }

		public int LatePark { get; private set; }

		public int ParkLeft { get; private set; }

		public List<List<int>> BoardCols { get; } = new List<List<int>>();


		public List<int> BoardJars { get; } = new List<int>();


		public List<ColorSortLock> BoardLocks { get; } = new List<ColorSortLock>();


		public List<ColorSortIncoming> Incoming { get; } = new List<ColorSortIncoming>();


		public List<int> JarSeq { get; } = new List<int>();


		public IReadOnlyList<string> Errors => _errors;

		public static bool IsHidden(int beadType)
		{
			return beadType >= 1000;
		}

		public static int TrueType(int beadType)
		{
			if (!IsHidden(beadType))
			{
				return beadType;
			}
			return beadType - 1000;
		}

		public static int HideType(int beadType)
		{
			return beadType + 1000;
		}

		public static ColorSortLevel Load(ColorSortLevelSource raw)
		{
			ColorSortLevel colorSortLevel = new ColorSortLevel();
			if (raw == null || !raw.IsObject)
			{
				colorSortLevel._errors.Add("关卡必须是 JSON 对象");
				return colorSortLevel;
			}
			if (raw.HasId)
			{
				colorSortLevel.Id = ReadInteger(raw.Id, "id", 1, colorSortLevel._errors);
			}
			colorSortLevel.Capacity = ReadInteger(raw.Capacity, "capacity", 2, colorSortLevel._errors);
			colorSortLevel.GroupsTotal = ReadInteger(raw.GroupsTotal, "targetCups", 2, colorSortLevel._errors);
			if (!raw.InitialIsArray)
			{
				colorSortLevel._errors.Add("initial 必须是数组");
			}
			if (!raw.IncomingIsArray)
			{
				colorSortLevel._errors.Add("incoming 必须是数组");
			}
			if (!raw.InitialIsArray || !raw.IncomingIsArray)
			{
				return colorSortLevel;
			}
			List<ColorSortColumnSource> initial = raw.Initial;
			List<ColorSortColumnSource> incoming = raw.Incoming;
			if (initial.Count < 3)
			{
				colorSortLevel._errors.Add("initial 缺失或少于 3 列");
			}
			colorSortLevel.Columns = initial.Count;
			for (int i = 0; i < colorSortLevel.Columns; i++)
			{
				colorSortLevel.BoardCols.Add(new List<int>());
				colorSortLevel.BoardJars.Add(-1);
				colorSortLevel.BoardLocks.Add(new ColorSortLock());
			}
			for (int j = 0; j < colorSortLevel.Columns; j++)
			{
				DecodeResult decodeResult = DecodeCol(initial[j], $"initial 第 {j + 1} 条");
				if (!string.IsNullOrEmpty(decodeResult.Error))
				{
					colorSortLevel._errors.Add(decodeResult.Error);
					continue;
				}
				if (decodeResult.Retire)
				{
					colorSortLevel._errors.Add(string.Format("initial 第 {0} 条写了 \"{1}\" —— 退出盘面只能出现在 incoming 里", j + 1, "x"));
					continue;
				}
				colorSortLevel.BoardJars[j] = decodeResult.Jar;
				colorSortLevel.BoardCols[j] = decodeResult.Beads;
				colorSortLevel.BoardLocks[j] = decodeResult.Lock;
			}
			if (colorSortLevel.GroupsTotal >= 2 && incoming.Count > colorSortLevel.GroupsTotal - 1)
			{
				colorSortLevel._errors.Add($"incoming 有 {incoming.Count} 条,不能超过 targetCups-1={colorSortLevel.GroupsTotal - 1} 条");
			}
			foreach (ColorSortColumnSource item in incoming)
			{
				DecodeResult decodeResult2 = DecodeCol(item, "incoming");
				if (!string.IsNullOrEmpty(decodeResult2.Error))
				{
					colorSortLevel._errors.Add(decodeResult2.Error);
					continue;
				}
				int jar = decodeResult2.Jar;
				string slot = (decodeResult2.Retire ? "retire" : ((jar == -1) ? "park" : "jar"));
				colorSortLevel.Incoming.Add(new ColorSortIncoming(slot, jar, decodeResult2.Beads, decodeResult2.Lock));
			}
			while (colorSortLevel.Incoming.Count < colorSortLevel.GroupsTotal - 1)
			{
				colorSortLevel.Incoming.Add(new ColorSortIncoming("retire", -1, new List<int>(), new ColorSortLock()));
			}
			colorSortLevel.Derive();
			return colorSortLevel;
		}

		public ColorSortLevel CreateSearchSnapshot()
		{
			ColorSortLevel colorSortLevel = new ColorSortLevel
			{
				Id = Id,
				Capacity = Capacity,
				GroupsTotal = GroupsTotal,
				Types = Types,
				Columns = Columns,
				InitJars = InitJars,
				LatePark = LatePark,
				ParkLeft = ParkLeft
			};
			for (int i = 0; i < BoardCols.Count; i++)
			{
				colorSortLevel.BoardCols.Add(new List<int>(BoardCols[i]));
			}
			colorSortLevel.BoardJars.AddRange(BoardJars);
			for (int j = 0; j < BoardLocks.Count; j++)
			{
				colorSortLevel.BoardLocks.Add(BoardLocks[j].Clone());
			}
			for (int k = 0; k < Incoming.Count; k++)
			{
				ColorSortIncoming colorSortIncoming = Incoming[k];
				colorSortLevel.Incoming.Add(new ColorSortIncoming(colorSortIncoming.Slot, colorSortIncoming.Jar, new List<int>(colorSortIncoming.Beads), colorSortIncoming.Lock.Clone()));
			}
			colorSortLevel.JarSeq.AddRange(JarSeq);
			for (int l = 0; l < _futureTally.Count; l++)
			{
				colorSortLevel._futureTally.Add((int[])_futureTally[l].Clone());
			}
			colorSortLevel._errors.AddRange(_errors);
			return colorSortLevel;
		}

		public bool Validate(out string firstError)
		{
			firstError = null;
			if (_errors.Count > 0)
			{
				firstError = _errors[0];
				return false;
			}
			if (Columns <= 0 || Capacity <= 0 || GroupsTotal <= 0 || Types <= 0)
			{
				firstError = $"基本参数有 0:列数={Columns} 容量={Capacity} 杯数={GroupsTotal} 种类={Types}";
				return false;
			}
			if (Incoming.Count != GroupsTotal - 1)
			{
				firstError = $"incoming 长度 {Incoming.Count},必须等于 targetCups-1={GroupsTotal - 1}";
				return false;
			}
			for (int i = 0; i < BoardCols.Count; i++)
			{
				List<int> list = BoardCols[i];
				if (list.Count > 0)
				{
					if (IsHidden(list[list.Count - 1]))
					{
						firstError = $"initial 第 {i + 1} 列串尾是问号 —— 串尾必须是明牌";
						return false;
					}
				}
			}
			for (int j = 0; j < Incoming.Count; j++)
			{
				List<int> beads = Incoming[j].Beads;
				if (beads.Count > 0)
				{
					if (IsHidden(beads[beads.Count - 1]))
					{
						firstError = $"incoming 第 {j + 1} 条末珠是问号 —— 落在串尾必须是明牌";
						return false;
					}
				}
			}
			return true;
		}

		public ColorSortIncoming IncomingAt(int k)
		{
			if (k < 0 || k >= Incoming.Count)
			{
				return null;
			}
			return Incoming[k];
		}

		private void Derive()
		{
			ParkLeft = 0;
			InitJars = 0;
			for (int i = 0; i < Columns; i++)
			{
				if (BoardJars[i] == -1)
				{
					ParkLeft++;
				}
				else
				{
					InitJars++;
				}
			}
			JarSeq.Clear();
			foreach (int boardJar in BoardJars)
			{
				if (boardJar != -1)
				{
					JarSeq.Add(boardJar);
				}
			}
			LatePark = 0;
			foreach (ColorSortIncoming item in Incoming)
			{
				if (item.Slot == "jar")
				{
					JarSeq.Add(item.Jar);
				}
				else if (item.Slot == "park")
				{
					LatePark++;
				}
			}
			int num = -1;
			foreach (int item2 in JarSeq)
			{
				num = Math.Max(num, item2);
			}
			foreach (List<int> boardCol in BoardCols)
			{
				foreach (int item3 in boardCol)
				{
					num = Math.Max(num, TrueType(item3));
				}
			}
			foreach (ColorSortIncoming item4 in Incoming)
			{
				foreach (int bead in item4.Beads)
				{
					num = Math.Max(num, TrueType(bead));
				}
			}
			Types = num + 1;
			BuildFutureTally();
		}

		public int[] FutureTally(int done)
		{
			if (_futureTally.Count == 0)
			{
				return new int[Math.Max(Types, 0)];
			}
			int num = done;
			if (num < 0)
			{
				num = 0;
			}
			if (num >= _futureTally.Count)
			{
				num = _futureTally.Count - 1;
			}
			return _futureTally[num];
		}

		private void BuildFutureTally()
		{
			_futureTally.Clear();
			int count = Incoming.Count;
			for (int i = 0; i <= count; i++)
			{
				_futureTally.Add(new int[Types]);
			}
			for (int num = count - 1; num >= 0; num--)
			{
				Array.Copy(_futureTally[num + 1], _futureTally[num], Types);
				foreach (int bead in Incoming[num].Beads)
				{
					int num2 = TrueType(bead);
					if (num2 >= 0 && num2 < Types)
					{
						_futureTally[num][num2]++;
					}
				}
			}
		}

		private static int ReadInteger(ColorSortNumber raw, string fieldName, int minimum, List<string> errors)
		{
			if (raw.Missing || raw.IsBoolean || !raw.IsNumeric || !raw.Parsed)
			{
				errors.Add($"{fieldName} 必须是 >= {minimum} 的整数");
				return 0;
			}
			if (raw.Value < minimum)
			{
				errors.Add($"{fieldName}={raw.RawText},必须是 >= {minimum} 的整数");
				return 0;
			}
			return raw.Value;
		}

		private static DecodeResult DecodeCol(ColorSortColumnSource raw, string where)
		{
			UnwrapResult unwrapResult = UnwrapColumn(raw, where);
			if (!string.IsNullOrEmpty(unwrapResult.Error))
			{
				return new DecodeResult
				{
					Error = unwrapResult.Error
				};
			}
			if (!(unwrapResult.Raw is string text))
			{
				return new DecodeResult
				{
					Error = where + " 必须是字符串或 {column:\"…\"} 形态"
				};
			}
			if (text == "x" || text == "x|")
			{
				if (!unwrapResult.Lock.IsEmpty)
				{
					return new DecodeResult
					{
						Error = where + " 是退出盘面，不能带锁"
					};
				}
				return new DecodeResult
				{
					Retire = true
				};
			}
			string[] array = text.Split("|", 2);
			if (array.Length != 2)
			{
				return new DecodeResult
				{
					Error = where + " 格式错误: " + text
				};
			}
			string text2 = array[0];
			string text3 = array[1];
			int num = -1;
			if (text2 != "-")
			{
				if (text2.StartsWith("?", StringComparison.Ordinal))
				{
					return new DecodeResult
					{
						Error = where + " 的机器不能隐藏"
					};
				}
				num = ParseUint(text2);
				if (num < 0)
				{
					return new DecodeResult
					{
						Error = where + " 机器类型无效: " + text2
					};
				}
				if (num >= 1000)
				{
					return new DecodeResult
					{
						Error = $"{where} 机器类型 {num} 太大"
					};
				}
			}
			List<int> list = new List<int>();
			if (!string.IsNullOrEmpty(text3))
			{
				string[] array2 = text3.Split(',');
				foreach (string text4 in array2)
				{
					bool flag = text4.StartsWith("?", StringComparison.Ordinal);
					string text5;
					if (!flag)
					{
						text5 = text4;
					}
					else
					{
						string text6 = text4;
						text5 = text6.Substring(1, text6.Length - 1);
					}
					int num2 = ParseUint(text5);
					if (num2 < 0)
					{
						return new DecodeResult
						{
							Error = where + " 水果序列格式错误"
						};
					}
					if (num2 >= 1000)
					{
						return new DecodeResult
						{
							Error = $"{where} 珠类型 {num2} 太大"
						};
					}
					list.Add(flag ? HideType(num2) : num2);
				}
			}
			return new DecodeResult
			{
				Jar = num,
				Beads = list,
				Lock = unwrapResult.Lock
			};
		}

		private static UnwrapResult UnwrapColumn(ColorSortColumnSource raw, string where)
		{
			if (raw == null || !raw.IsObject)
			{
				return new UnwrapResult
				{
					Raw = raw?.PlainText
				};
			}
			string columnType = raw.ColumnType;
			if (columnType == "unlock")
			{
				LockParseResult lockParseResult = ParseLockInfo(raw, where);
				if (!string.IsNullOrEmpty(lockParseResult.Error))
				{
					return new UnwrapResult
					{
						Error = lockParseResult.Error
					};
				}
				return new UnwrapResult
				{
					Raw = raw.Column,
					Lock = lockParseResult.Lock
				};
			}
			if (!string.IsNullOrEmpty(columnType) && columnType != "default")
			{
				return new UnwrapResult
				{
					Error = where + " columnType 不支持: " + columnType
				};
			}
			return new UnwrapResult
			{
				Raw = raw.Column
			};
		}

		private static LockParseResult ParseLockInfo(ColorSortColumnSource raw, string where)
		{
			if (raw == null || !raw.LockInfoIsObject)
			{
				return new LockParseResult
				{
					Error = where + " 解锁 columnInfo 必须是对象"
				};
			}
			string text = raw.LockCondition ?? "";
			if (text != "collect_count" && text != "collect_type")
			{
				return new LockParseResult
				{
					Error = where + " 解锁 condition 无效"
				};
			}
			List<string> list = new List<string>();
			int count = ReadInteger(raw.LockCount, where + " count", 1, list);
			if (list.Count > 0)
			{
				return new LockParseResult
				{
					Error = list[0]
				};
			}
			if (text == "collect_count")
			{
				return new LockParseResult
				{
					Lock = ColorSortLock.CollectCount(count)
				};
			}
			int num = ReadInteger(raw.LockTypeId, where + " typeId", 0, list);
			if (list.Count > 0)
			{
				return new LockParseResult
				{
					Error = list[0]
				};
			}
			if (num >= 1000)
			{
				return new LockParseResult
				{
					Error = where + " typeId 太大"
				};
			}
			return new LockParseResult
			{
				Lock = ColorSortLock.CollectType(num, count)
			};
		}

		private static int ParseUint(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return -1;
			}
			foreach (char c in text)
			{
				if (c < '0' || c > '9')
				{
					return -1;
				}
			}
			return int.Parse(text);
		}
	}
}

