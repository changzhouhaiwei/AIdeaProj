using System;
using System.Collections.Generic;
using Example.Cfg;
using UniLog;
using XingYun.Config;
using XingYun.Level;

namespace Game.GamePlay.ColorSort
{
	public static class ColorSortLevelMatchFixed
	{
		private const string Tag = "ColorSortLevelMatchFixed";

		public static bool TryGetFixedContent(int levelId, out string content, out string ruleId)
		{
			content = null;
			ruleId = null;
			if (levelId < 1)
			{
				return false;
			}
			ConfigManager config = GameApp.Config;
			List<LevelMatchConfig> list = ((config == null) ? null : config.GetTable<ExampleTables>()?.TbLevelMatchConfig?.DataList);
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					LevelMatchConfig levelMatchConfig = list[i];
					if (levelMatchConfig.Type == LevelMatchType.Fixed && levelMatchConfig.Target == LevelMatchTarget.LevelId && levelMatchConfig.Operator == LevelMatchOperator.Equal && levelMatchConfig.IntParam1 == levelId && ColorSortLevelCodec.LooksLikeLevel(levelMatchConfig.StringValue1))
					{
						content = levelMatchConfig.StringValue1;
						ruleId = levelMatchConfig.Id;
						return true;
					}
				}
			}
			return false;
		}

		public static LevelInfo ToLevelInfo(string content, float difficulty = 0f)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Expected O, but got Unknown
			return new LevelInfo
			{
				LevelId = content,
				MatchedCsv = "forced",
				MatchedIndex = -1,
				LevelDifficulty = difficulty,
				RepositoryPath = (ColorSortLevelSystemBootstrap.ReadyPoolRoot ?? "GP101")
			};
		}

		public static ColorSortLevelPicker.PickResult TryPick(int levelId)
		{
			if (!TryGetFixedContent(levelId, out var content, out var ruleId))
			{
				return null;
			}
			try
			{
				ColorSortLevel colorSortLevel = ColorSortLevelCodec.FromContent(content, levelId);
				if (!colorSortLevel.Validate(out var firstError))
				{
					Log.LogError("ColorSortLevelMatchFixed", "Fixed Content 校验失败 level={0} rule={1}: {2}", new object[3] { levelId, ruleId, firstError });
					return null;
				}
				return new ColorSortLevelPicker.PickResult
				{
					Level = colorSortLevel,
					Info = ToLevelInfo(content),
					Content = content,
					FromLinearFallback = false
				};
			}
			catch (Exception ex)
			{
				Log.LogError("ColorSortLevelMatchFixed", "Fixed 关解析失败 level={0}: {1}", new object[2] { levelId, ex.Message });
				return null;
			}
		}
	}
}

