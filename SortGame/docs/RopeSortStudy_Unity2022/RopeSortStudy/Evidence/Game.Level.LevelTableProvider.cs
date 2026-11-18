using System;
using System.Collections.Generic;
using Example.Cfg;
using Game.ColorSort.Model;

namespace Game.Level
{
	public static class LevelTableProvider
	{
		private const string LogTag = "LevelTable";

		private static readonly Dictionary<int, Example.Cfg.Level> LevelMap = new Dictionary<int, Example.Cfg.Level>();

		private static int maxConfiguredLv;

		private static int loopStartLv;

		private static int loopEndLv;

		private static int loopLength;

		private static bool loaded;

		public static void Invalidate()
		{
			loaded = false;
			LevelMap.Clear();
			maxConfiguredLv = 0;
			loopStartLv = 0;
			loopEndLv = 0;
			loopLength = 0;
		}

		public static bool TryGetLevelInfo(int levelId, out LevelConfigInfo info)
		{
			info = null;
			if (levelId <= 0)
			{
				return false;
			}
			EnsureLoaded();
			if (!loaded || LevelMap.Count == 0)
			{
				return false;
			}
			int num = ResolveConfigLv(levelId);
			if (!LevelMap.TryGetValue(num, out var value) || value == null)
			{
				return false;
			}
			info = new LevelConfigInfo
			{
				LevelId = levelId,
				ConfigLv = num,
				Difficulty = DifficultyTierConvert.FromLevelCode(value.Diff),
				CoinReward = value.Coin_Reward,
				AdvMul = value.Adv_Mul,
				AdvJud = value.Adv_Jud,
				Loop = value.Loop
			};
			return true;
		}

		public static bool TryGetSettlementCoinRewards(int levelId, out int continueCoins, out int adCoins)
		{
			continueCoins = 0;
			adCoins = 0;
			if (!TryGetLevelInfo(levelId, out var info) || info == null)
			{
				return false;
			}
			continueCoins = Math.Max(0, info.CoinReward);
			int num = ((info.AdvMul > 0) ? info.AdvMul : 2);
			adCoins = Math.Max(0, info.CoinReward * num);
			return true;
		}

		public static bool ShouldShowSettlementAdReward(int levelId)
		{
			if (TryGetLevelInfo(levelId, out var info) && info != null)
			{
				return info.AdvMul > 0;
			}
			return false;
		}

		private static int ResolveConfigLv(int levelId)
		{
			if (levelId <= maxConfiguredLv)
			{
				return levelId;
			}
			if (loopLength <= 0)
			{
				return maxConfiguredLv;
			}
			int num = (levelId - maxConfiguredLv - 1) % loopLength;
			if (num < 0)
			{
				num += loopLength;
			}
			return loopStartLv + num;
		}

		private static void EnsureLoaded()
		{
			if (!loaded && TryLoadFromExampleTables())
			{
				if (LevelMap.Count == 0)
				{
					ClearLoadBuffers();
				}
				else
				{
					loaded = true;
				}
			}
		}

		private static void ClearLoadBuffers()
		{
			LevelMap.Clear();
			maxConfiguredLv = 0;
			loopStartLv = 0;
			loopEndLv = 0;
			loopLength = 0;
		}

		private static bool TryLoadFromExampleTables()
		{
			try
			{
				if (GameApp.Config == null || !GameApp.Config.IsTableLoaded<ExampleTables>())
				{
					return false;
				}
				IReadOnlyList<Example.Cfg.Level> readOnlyList = GameApp.Config.GetTable<ExampleTables>()?.TbLevel?.DataList;
				if (readOnlyList == null || readOnlyList.Count == 0)
				{
					return false;
				}
				ClearLoadBuffers();
				int num = 0;
				int num2 = 0;
				for (int i = 0; i < readOnlyList.Count; i++)
				{
					Example.Cfg.Level level = readOnlyList[i];
					if (level != null && level.Lv > 0)
					{
						LevelMap[level.Lv] = level;
						if (level.Lv > maxConfiguredLv)
						{
							maxConfiguredLv = level.Lv;
						}
						if (level.Loop == 1 && num == 0)
						{
							num = level.Lv;
						}
						else if (level.Loop == 2)
						{
							num2 = level.Lv;
						}
					}
				}
				loopStartLv = num;
				loopEndLv = ((num2 > 0) ? num2 : maxConfiguredLv);
				if (loopStartLv > 0 && loopEndLv >= loopStartLv)
				{
					loopLength = loopEndLv - loopStartLv + 1;
				}
				else
				{
					loopStartLv = 0;
					loopEndLv = 0;
					loopLength = 0;
				}
				return LevelMap.Count > 0;
			}
			catch (Exception)
			{
				ClearLoadBuffers();
				return false;
			}
		}
	}
}

