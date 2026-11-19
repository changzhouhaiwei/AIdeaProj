using System;
using System.Collections.Generic;
using System.Text;

namespace Game.GamePlay.ColorSort
{
	public static class ColorSortRules
	{
		private const int CascadeGuard = 40;

		private const int ClosedStateCap = 80;

		[ThreadStatic]
		private static StringBuilder keyBuilder;

		public static ColorSortState MakeState(ColorSortLevel level)
		{
			ColorSortState colorSortState = new ColorSortState();
			for (int i = 0; i < level.Columns; i++)
			{
				colorSortState.Cols.Add(new List<int>(level.BoardCols[i]));
				colorSortState.Jars.Add(level.BoardJars[i]);
				colorSortState.Caps.Add(level.Capacity);
				colorSortState.Gone.Add(item: false);
				colorSortState.Locks.Add(level.BoardLocks[i].Clone());
			}
			colorSortState.Done = 0;
			colorSortState.LateLeft = level.LatePark;
			return colorSortState;
		}

		public static bool IsLocked(ColorSortState st, int ci)
		{
			if (ci < 0 || ci >= st.Locks.Count)
			{
				return false;
			}
			return !st.Locks[ci].IsEmpty;
		}

		public static int MoveInfo(ColorSortState st, int src, int j, int dst)
		{
			if (src == dst)
			{
				return 0;
			}
			if (src < 0 || dst < 0 || src >= st.Cols.Count || dst >= st.Cols.Count)
			{
				return 0;
			}
			if (IsLocked(st, src) || IsLocked(st, dst))
			{
				return 0;
			}
			List<int> list = st.Cols[src];
			List<int> list2 = st.Cols[dst];
			int num = list.Count - j;
			if (j < 0 || num <= 0)
			{
				return 0;
			}
			if (list2.Count + num > st.Caps[dst])
			{
				return 0;
			}
			if (list2.Count == 0)
			{
				return num;
			}
			if (ColorSortLevel.IsHidden(list[j]))
			{
				return 0;
			}
			if (list2[list2.Count - 1] != list[j])
			{
				return 0;
			}
			return num;
		}

		public static ColorSortReject RejectReason(ColorSortState st, int src, int j, int dst)
		{
			if (MoveInfo(st, src, j, dst) > 0)
			{
				return ColorSortReject.None;
			}
			if (src == dst || src < 0 || dst < 0 || src >= st.Cols.Count || dst >= st.Cols.Count)
			{
				return ColorSortReject.Invalid;
			}
			List<int> list = st.Cols[src];
			int num = list.Count - j;
			if (j < 0 || num <= 0)
			{
				return ColorSortReject.Invalid;
			}
			List<int> list2 = st.Cols[dst];
			if (IsLocked(st, src) || IsLocked(st, dst))
			{
				return ColorSortReject.Locked;
			}
			if (list2.Count + num > st.Caps[dst])
			{
				return ColorSortReject.Full;
			}
			if (ColorSortLevel.IsHidden(list[j]))
			{
				return ColorSortReject.Mystery;
			}
			return ColorSortReject.Color;
		}

		public static ColorSortMoveRecord ApplyMove(ColorSortState st, ColorSortLevel level, int src, int j, int dst, bool noDeposit = false)
		{
			int num = MoveInfo(st, src, j, dst);
			if (num == 0)
			{
				return null;
			}
			List<int> list = st.Cols[src];
			List<int> list2 = st.Cols[dst];
			int head = list[j];
			for (int i = 0; i < num; i++)
			{
				list2.Add(list[j + i]);
			}
			list.RemoveRange(j, num);
			ColorSortMoveRecord colorSortMoveRecord = new ColorSortMoveRecord
			{
				Src = src,
				J = j,
				Dst = dst,
				K = num,
				Head = head
			};
			if (!noDeposit)
			{
				ColorSortState snapshot = Snapshot(st);
				RunDeposit(st, level, colorSortMoveRecord.Deposits);
				if (colorSortMoveRecord.Deposits.Count == 0)
				{
					snapshot = null;
				}
				else
				{
					UnlockPass(st, colorSortMoveRecord.UnlockedColumns);
					colorSortMoveRecord.Snapshot = snapshot;
				}
			}
			colorSortMoveRecord.Revealed.AddRange(RevealTails(st));
			return colorSortMoveRecord;
		}

		public static bool HasAnyMove(ColorSortState st)
		{
			int count = st.Cols.Count;
			for (int i = 0; i < count; i++)
			{
				if (IsLocked(st, i))
				{
					continue;
				}
				List<int> list = st.Cols[i];
				if (list.Count == 0)
				{
					continue;
				}
				for (int j = 0; j < count; j++)
				{
					if (j == i || IsLocked(st, j))
					{
						continue;
					}
					List<int> list2 = st.Cols[j];
					int num = st.Caps[j] - list2.Count;
					if (num <= 0)
					{
						continue;
					}
					if (list2.Count == 0)
					{
						return true;
					}
					if (ColorSortLevel.IsHidden(list[list.Count - 1]))
					{
						continue;
					}
					int num2 = list2[list2.Count - 1];
					int num3 = list.Count - 1;
					while (num3 >= 0 && list.Count - num3 <= num)
					{
						if (!ColorSortLevel.IsHidden(list[num3]) && list[num3] == num2)
						{
							return true;
						}
						num3--;
					}
				}
			}
			return false;
		}

		public static int AddRope(ColorSortState st, int capacity)
		{
			if (st == null || st.ExtraRopeUsed || capacity <= 0)
			{
				return -1;
			}
			if (st.Cols.Count >= 9)
			{
				return -1;
			}
			int count = st.Cols.Count;
			if (st.Jars.Count != count || st.Caps.Count != count || st.Gone.Count != count || st.Locks.Count != count)
			{
				return -1;
			}
			st.Cols.Add(new List<int>());
			st.Jars.Add(-1);
			st.Caps.Add(capacity);
			st.Gone.Add(item: false);
			st.Locks.Add(new ColorSortLock());
			st.ExtraRopeUsed = true;
			return count;
		}

		public static int[] TypeCounts(ColorSortState st, int types)
		{
			int[] array = new int[types];
			foreach (List<int> col in st.Cols)
			{
				foreach (int item in col)
				{
					int num = ColorSortLevel.TrueType(item);
					if (num >= 0 && num < types)
					{
						array[num]++;
					}
				}
			}
			return array;
		}

		public static int MaxGroupsLeft(ColorSortState st, ColorSortLevel level)
		{
			if (st == null || level == null || level.Types <= 0)
			{
				return 0;
			}
			int[] array = TypeCounts(st, level.Types);
			int[] array2 = level.FutureTally(st.Done);
			int num = 0;
			for (int i = 0; i < level.Types; i++)
			{
				num += (array[i] + array2[i]) / level.Capacity;
			}
			return num;
		}

		public static bool IsDoomed(ColorSortState st, ColorSortLevel level)
		{
			if (st == null || level == null || IsWin(st, level))
			{
				return false;
			}
			return MaxGroupsLeft(st, level) < level.GroupsTotal - st.Done;
		}

		public static void CollectUsefulMoves(ColorSortState st, List<ColorSortHintMove> into)
		{
			into.Clear();
			if (st == null)
			{
				return;
			}
			int count = st.Cols.Count;
			int[] array = new int[count];
			int[] array2 = new int[count];
			for (int i = 0; i < count; i++)
			{
				if (IsLocked(st, i) || st.Gone[i])
				{
					array[i] = 0;
					array2[i] = -1;
					continue;
				}
				List<int> list = st.Cols[i];
				array[i] = st.Caps[i] - list.Count;
				int num = i;
				int num2;
				if (list.Count != 0)
				{
					num2 = list[list.Count - 1];
				}
				else
				{
					num2 = -1;
				}
				array2[num] = num2;
			}
			for (int j = 0; j < count; j++)
			{
				if (IsLocked(st, j) || st.Gone[j])
				{
					continue;
				}
				List<int> list2 = st.Cols[j];
				int count2 = list2.Count;
				if (count2 == 0)
				{
					continue;
				}
				bool flag = false;
				foreach (int item in list2)
				{
					if (ColorSortLevel.IsHidden(item))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					for (int k = 0; k < count2; k++)
					{
						for (int l = 0; l < count; l++)
						{
							if (!st.Gone[l])
							{
								int num3 = MoveInfo(st, j, k, l);
								if (num3 > 0)
								{
									into.Add(new ColorSortHintMove
									{
										Src = j,
										J = k,
										Dst = l,
										K = num3,
										Head = list2[k]
									});
								}
							}
						}
					}
					continue;
				}
				int num4 = 0;
				while (num4 < count2)
				{
					int num5 = list2[num4];
					int m;
					for (m = num4; m + 1 < count2 && list2[m + 1] == num5; m++)
					{
					}
					int num6 = count2 - m;
					for (int n = 0; n < count; n++)
					{
						if (n == j || st.Gone[n] || num6 > array[n])
						{
							continue;
						}
						int num7 = array2[n];
						if (num7 == -1 || (!ColorSortLevel.IsHidden(num5) && num7 == num5))
						{
							int num8 = Math.Max(num4, count2 - array[n]);
							into.Add(new ColorSortHintMove
							{
								Src = j,
								J = num8,
								Dst = n,
								K = count2 - num8,
								Head = list2[num8]
							});
							if (m != num8)
							{
								into.Add(new ColorSortHintMove
								{
									Src = j,
									J = m,
									Dst = n,
									K = num6,
									Head = list2[m]
								});
							}
						}
					}
					num4 = m + 1;
				}
			}
		}

		public static void CollectMoves(ColorSortState st, List<ColorSortHintMove> into)
		{
			into.Clear();
			if (st == null)
			{
				return;
			}
			int count = st.Cols.Count;
			for (int i = 0; i < count; i++)
			{
				if (IsLocked(st, i) || st.Gone[i])
				{
					continue;
				}
				List<int> list = st.Cols[i];
				for (int j = 0; j < list.Count; j++)
				{
					for (int k = 0; k < count; k++)
					{
						if (k != i && !st.Gone[k])
						{
							int num = MoveInfo(st, i, j, k);
							if (num > 0)
							{
								into.Add(new ColorSortHintMove
								{
									Src = i,
									J = j,
									Dst = k,
									K = num,
									Head = list[j]
								});
							}
						}
					}
				}
			}
		}

		public static bool IsDead(ColorSortState st, ColorSortLevel level)
		{
			if (st == null || level == null || IsWin(st, level))
			{
				return false;
			}
			if (!HasAnyMove(st) || IsDoomed(st, level))
			{
				return true;
			}
			return IsClosedCycle(st, level);
		}

		public static bool IsClosedCycle(ColorSortState st, ColorSortLevel level)
		{
			if (st == null || level == null || AnyReady(st))
			{
				return false;
			}
			if (!HasMechanics(st) && HasIrreversibleMove(st))
			{
				return false;
			}
			Dictionary<string, int> dictionary = new Dictionary<string, int>(80);
			List<ColorSortState> list = new List<ColorSortState>(16);
			List<List<int>> list2 = new List<List<int>>(16);
			dictionary[StateKey(st)] = 0;
			list.Add(Snapshot(st));
			list2.Add(new List<int>(4));
			Stack<int> stack = new Stack<int>();
			stack.Push(0);
			while (stack.Count > 0)
			{
				int index = stack.Pop();
				ColorSortState colorSortState = list[index];
				int count = colorSortState.Cols.Count;
				for (int i = 0; i < count; i++)
				{
					List<int> list3 = colorSortState.Cols[i];
					for (int j = 0; j < list3.Count; j++)
					{
						for (int k = 0; k < count; k++)
						{
							if (MoveInfo(colorSortState, i, j, k) <= 0)
							{
								continue;
							}
							ColorSortState colorSortState2 = Snapshot(colorSortState);
							if (ApplyMove(colorSortState2, level, i, j, k, noDeposit: true) == null)
							{
								continue;
							}
							if (AnyReady(colorSortState2))
							{
								return false;
							}
							string key = StateKey(colorSortState2);
							if (!dictionary.TryGetValue(key, out var value))
							{
								if (dictionary.Count >= 80)
								{
									return false;
								}
								value = (dictionary[key] = list.Count);
								list.Add(colorSortState2);
								list2.Add(new List<int>(4));
								stack.Push(value);
							}
							list2[index].Add(value);
						}
					}
				}
			}
			int count3 = list.Count;
			List<int>[] array = new List<int>[count3];
			for (int l = 0; l < count3; l++)
			{
				array[l] = new List<int>(4);
			}
			for (int m = 0; m < count3; m++)
			{
				List<int> list4 = list2[m];
				for (int n = 0; n < list4.Count; n++)
				{
					array[list4[n]].Add(m);
				}
			}
			bool[] array2 = new bool[count3];
			array2[0] = true;
			int num = 1;
			Stack<int> stack2 = new Stack<int>();
			stack2.Push(0);
			while (stack2.Count > 0)
			{
				int num2 = stack2.Pop();
				List<int> list5 = array[num2];
				for (int num3 = 0; num3 < list5.Count; num3++)
				{
					int num4 = list5[num3];
					if (!array2[num4])
					{
						array2[num4] = true;
						num++;
						stack2.Push(num4);
					}
				}
			}
			return num == count3;
		}

		public static int RunDeposit(ColorSortState st, ColorSortLevel level, List<ColorSortDepositLog> log)
		{
			int num = 0;
			int num2 = 0;
			while (num2 < 40)
			{
				num2++;
				bool flag = false;
				for (int i = 0; i < st.Cols.Count; i++)
				{
					int num3 = DepositReady(st, i);
					if (num3 == 0)
					{
						continue;
					}
					int num4 = st.Jars[i];
					int done = st.Done;
					st.Collectibles?.ApplyDeposit(level, i, done);
					st.Cols[i].Clear();
					st.Done = done + 1;
					st.Cups.Add(num4);
					ColorSortIncoming colorSortIncoming = level.IncomingAt(done);
					string text = colorSortIncoming?.Slot ?? "retire";
					bool keep = text != "retire";
					if (colorSortIncoming != null)
					{
						string slot = colorSortIncoming.Slot;
						if (!(slot == "jar"))
						{
							if (slot == "park")
							{
								st.Jars[i] = -1;
								st.LateLeft = Math.Max(0, st.LateLeft - 1);
							}
							else
							{
								st.Jars[i] = -1;
								st.Caps[i] = 0;
								st.Gone[i] = true;
							}
						}
						else
						{
							st.Jars[i] = colorSortIncoming.Jar;
						}
						List<ColorSortSpawnLog> list = null;
						if (colorSortIncoming.Beads.Count > 0)
						{
							list = PlaceBatch(st, colorSortIncoming.Beads, i);
						}
						st.Locks[i] = (st.Gone[i] ? new ColorSortLock() : colorSortIncoming.Lock.CloneForAppearance(st.Done));
						log?.Add(new ColorSortDepositLog
						{
							Col = i,
							BeadType = num4,
							Q = num3,
							Nth = done,
							Slot = text,
							Keep = keep,
							Spawns = (list ?? new List<ColorSortSpawnLog>())
						});
						num++;
						flag = true;
					}
					else
					{
						st.Jars[i] = -1;
						st.Caps[i] = 0;
						st.Gone[i] = true;
						keep = false;
						text = "retire";
						log?.Add(new ColorSortDepositLog
						{
							Col = i,
							BeadType = num4,
							Q = num3,
							Nth = done,
							Slot = text,
							Keep = keep,
							Spawns = new List<ColorSortSpawnLog>()
						});
						num++;
						flag = true;
					}
				}
				if (!flag)
				{
					break;
				}
			}
			return num;
		}

		private static int DepositReady(ColorSortState st, int ci)
		{
			return DepositReadyColumn(st, ci);
		}

		public static int DepositReadyColumn(ColorSortState st, int ci)
		{
			if (IsLocked(st, ci))
			{
				return 0;
			}
			int num = st.Jars[ci];
			if (num < 0)
			{
				return 0;
			}
			List<int> list = st.Cols[ci];
			int num2 = st.Caps[ci];
			if (list.Count != num2 || num2 <= 0)
			{
				return 0;
			}
			foreach (int item in list)
			{
				if (item != num)
				{
					return 0;
				}
			}
			return num2;
		}

		private static bool AnyReady(ColorSortState st)
		{
			for (int i = 0; i < st.Cols.Count; i++)
			{
				if (DepositReady(st, i) > 0)
				{
					return true;
				}
			}
			return false;
		}

		private static bool HasMechanics(ColorSortState st)
		{
			for (int i = 0; i < st.Cols.Count; i++)
			{
				List<int> list = st.Cols[i];
				for (int j = 0; j < list.Count; j++)
				{
					if (ColorSortLevel.IsHidden(list[j]))
					{
						return true;
					}
				}
			}
			for (int k = 0; k < st.Locks.Count; k++)
			{
				if (!st.Locks[k].IsEmpty)
				{
					return true;
				}
			}
			return false;
		}

		private static bool HasIrreversibleMove(ColorSortState st)
		{
			int count = st.Cols.Count;
			for (int i = 0; i < count; i++)
			{
				List<int> list = st.Cols[i];
				for (int j = 0; j < list.Count; j++)
				{
					if (j == 0 || list[j - 1] == list[j])
					{
						continue;
					}
					for (int k = 0; k < count; k++)
					{
						if (MoveInfo(st, i, j, k) > 0)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static string StateKey(ColorSortState st)
		{
			StringBuilder stringBuilder = keyBuilder ?? (keyBuilder = new StringBuilder(128));
			stringBuilder.Clear();
			AppendStateKey(st, stringBuilder);
			return stringBuilder.ToString();
		}

		internal static void AppendStateKey(ColorSortState st, StringBuilder builder)
		{
			for (int i = 0; i < st.Cols.Count; i++)
			{
				if (i > 0)
				{
					builder.Append('|');
				}
				List<int> list = st.Cols[i];
				for (int j = 0; j < list.Count; j++)
				{
					if (j > 0)
					{
						builder.Append(',');
					}
					builder.Append(list[j]);
				}
				builder.Append('#');
				builder.Append(st.Jars[i]);
				builder.Append(':').Append(st.Caps[i]).Append(':')
					.Append(st.Gone[i] ? 1 : 0);
				ColorSortLock colorSortLock = st.Locks[i];
				builder.Append(':').Append(colorSortLock.Condition).Append(':')
					.Append(colorSortLock.Count)
					.Append(':')
					.Append(colorSortLock.TypeId);
			}
			builder.Append(";done=").Append(st.Done).Append(";late=")
				.Append(st.LateLeft)
				.Append(";extra=")
				.Append(st.ExtraRopeUsed ? 1 : 0)
				.Append(";cups=");
			for (int k = 0; k < st.Cups.Count; k++)
			{
				builder.Append(st.Cups[k]).Append(',');
			}
		}

		private static List<ColorSortSpawnLog> PlaceBatch(ColorSortState st, List<int> typesIn, int clearedCi)
		{
			List<int> list = new List<int>();
			if (clearedCi >= 0 && clearedCi < st.Cols.Count)
			{
				list.Add(clearedCi);
			}
			for (int i = 0; i < st.Cols.Count; i++)
			{
				if (i != clearedCi)
				{
					list.Add(i);
				}
			}
			Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
			foreach (int item in typesIn)
			{
				bool flag = false;
				foreach (int item2 in list)
				{
					if (IsLocked(st, item2))
					{
						continue;
					}
					List<int> list2 = st.Cols[item2];
					if (list2.Count < st.Caps[item2])
					{
						list2.Add(item);
						if (!dictionary.TryGetValue(item2, out var value))
						{
							value = (dictionary[item2] = new List<int>());
						}
						value.Add(item);
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					break;
				}
			}
			List<ColorSortSpawnLog> list4 = new List<ColorSortSpawnLog>(dictionary.Count);
			foreach (KeyValuePair<int, List<int>> item3 in dictionary)
			{
				list4.Add(new ColorSortSpawnLog
				{
					Col = item3.Key,
					Types = item3.Value
				});
			}
			return list4;
		}

		private static List<ColorSortRevealLog> RevealTails(ColorSortState st)
		{
			List<ColorSortRevealLog> list = new List<ColorSortRevealLog>();
			for (int i = 0; i < st.Cols.Count; i++)
			{
				List<int> list2 = st.Cols[i];
				if (list2.Count != 0)
				{
					int index = list2.Count - 1;
					int num = list2[index];
					if (ColorSortLevel.IsHidden(num))
					{
						int type = (list2[index] = ColorSortLevel.TrueType(num));
						list.Add(new ColorSortRevealLog
						{
							Col = i,
							Index = index,
							Before = num,
							Type = type
						});
					}
				}
			}
			return list;
		}

		private static void UnlockPass(ColorSortState st, List<int> unlockedColumns)
		{
			for (int i = 0; i < st.Locks.Count; i++)
			{
				ColorSortLock colorSortLock = st.Locks[i];
				if (!colorSortLock.IsEmpty && LockSatisfied(st, colorSortLock))
				{
					st.Locks[i] = new ColorSortLock();
					unlockedColumns.Add(i);
				}
			}
		}

		private static bool LockSatisfied(ColorSortState st, ColorSortLock colLock)
		{
			if (colLock.Condition == "collect_count")
			{
				return st.Done >= colLock.Count;
			}
			int num = 0;
			foreach (int cup in st.Cups)
			{
				if (cup == colLock.TypeId)
				{
					num++;
				}
			}
			return num >= colLock.Count;
		}

		public static ColorSortState Snapshot(ColorSortState st)
		{
			ColorSortState colorSortState = new ColorSortState
			{
				Done = st.Done,
				LateLeft = st.LateLeft,
				ExtraRopeUsed = st.ExtraRopeUsed,
				Collectibles = st.Collectibles?.Clone()
			};
			foreach (List<int> col in st.Cols)
			{
				colorSortState.Cols.Add(new List<int>(col));
			}
			colorSortState.Jars.AddRange(st.Jars);
			colorSortState.Caps.AddRange(st.Caps);
			colorSortState.Gone.AddRange(st.Gone);
			colorSortState.Cups.AddRange(st.Cups);
			foreach (ColorSortLock @lock in st.Locks)
			{
				colorSortState.Locks.Add(@lock.Clone());
			}
			return colorSortState;
		}

		public static void Restore(ColorSortState st, ColorSortState snap)
		{
			st.Cols.Clear();
			foreach (List<int> col in snap.Cols)
			{
				st.Cols.Add(new List<int>(col));
			}
			st.Jars.Clear();
			st.Jars.AddRange(snap.Jars);
			st.Caps.Clear();
			st.Caps.AddRange(snap.Caps);
			st.Gone.Clear();
			st.Gone.AddRange(snap.Gone);
			st.Cups.Clear();
			st.Cups.AddRange(snap.Cups);
			st.Locks.Clear();
			foreach (ColorSortLock @lock in snap.Locks)
			{
				st.Locks.Add(@lock.Clone());
			}
			st.Done = snap.Done;
			st.LateLeft = snap.LateLeft;
			st.ExtraRopeUsed = snap.ExtraRopeUsed;
			st.Collectibles = snap.Collectibles?.Clone();
		}

		public static bool IsWin(ColorSortState st, ColorSortLevel level)
		{
			return st.Done >= level.GroupsTotal;
		}

		public static void UndoMove(ColorSortState st, ColorSortMoveRecord rec)
		{
			if (st == null || rec == null || rec.K <= 0)
			{
				return;
			}
			if (rec.Snapshot != null)
			{
				Restore(st, rec.Snapshot);
			}
			else if (rec.Revealed != null)
			{
				for (int i = 0; i < rec.Revealed.Count; i++)
				{
					ColorSortRevealLog colorSortRevealLog = rec.Revealed[i];
					if (colorSortRevealLog != null && colorSortRevealLog.Col >= 0 && colorSortRevealLog.Col < st.Cols.Count)
					{
						List<int> list = st.Cols[colorSortRevealLog.Col];
						if (colorSortRevealLog.Index >= 0 && colorSortRevealLog.Index < list.Count)
						{
							list[colorSortRevealLog.Index] = colorSortRevealLog.Before;
						}
					}
				}
			}
			if (rec.Src < 0 || rec.Dst < 0 || rec.Src >= st.Cols.Count || rec.Dst >= st.Cols.Count)
			{
				return;
			}
			List<int> list2 = st.Cols[rec.Dst];
			List<int> list3 = st.Cols[rec.Src];
			int num = list2.Count - rec.K;
			if (num >= 0)
			{
				for (int j = 0; j < rec.K; j++)
				{
					list3.Add(list2[num + j]);
				}
				list2.RemoveRange(num, rec.K);
			}
		}
	}
}

