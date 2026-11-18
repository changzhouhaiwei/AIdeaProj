using System;
using System.Collections.Generic;
using Game.Feature.Gameplay;
using Game.Level;
using UniLog;
using XingYun.Core.Singletons;
using XingYun.Level;

namespace Game.GamePlay.ColorSort
{
	public static class ColorSortLevelPicker
	{
		public sealed class PickResult
		{
			public ColorSortLevel Level;

			public LevelInfo Info;

			public string Content;

			public bool FromLinearFallback;
		}

		private const string Tag = "ColorSortLevelPicker";

		private static string _gmForcedContent;

		private static string _gmForcedCsv;

		private static int _gmForcedIndex = -1;

		public static bool AllowLinearFallback { get; set; }

		public static ColorSortLevelPickMode Mode { get; set; } = ColorSortLevelPickMode.Dynamic;


		public static void SetGmForcedPick(string content, string csvId = null, int index = -1)
		{
			_gmForcedContent = content;
			_gmForcedCsv = csvId;
			_gmForcedIndex = index;
		}

		public static void ClearGmForcedPick()
		{
			_gmForcedContent = null;
			_gmForcedCsv = null;
			_gmForcedIndex = -1;
		}

		public static PickResult PickAndLoad(int levelId, int gmOrderIndex = -1)
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Expected O, but got Unknown
			if (levelId < 1)
			{
				levelId = 1;
			}
			if (!string.IsNullOrWhiteSpace(_gmForcedContent))
			{
				string gmForcedContent = _gmForcedContent;
				string matchedCsv = (string.IsNullOrEmpty(_gmForcedCsv) ? "gm-special" : _gmForcedCsv);
				int gmForcedIndex = _gmForcedIndex;
				ClearGmForcedPick();
				LevelInfo info = new LevelInfo
				{
					LevelId = gmForcedContent,
					MatchedCsv = matchedCsv,
					MatchedIndex = gmForcedIndex
				};
				return TryLoadContent(gmForcedContent, levelId, info, "gm-special");
			}
			if (Mode == ColorSortLevelPickMode.Linear)
			{
				return LoadLinear(levelId, fromFallback: false);
			}
			bool allowFallback = Singleton<GameplayFeatureManager>.Instance.ResolveLevelStrategyOverride()?.AllowFallback ?? true;
			if (!ColorSortLevelSystemBootstrap.EnsureReady())
			{
				Log.LogError("ColorSortLevelPicker", "Bootstrap 失败 level={0}", new object[1] { levelId });
				return TryFallbacks(levelId, allowFallback);
			}
			try
			{
				Dictionary<string, object> dictionary = ((gmOrderIndex >= 0) ? new Dictionary<string, object>(1) { ["ColorSort.GmOrderIndex"] = gmOrderIndex } : null);
				LevelInfo levelInfo = GameApp.Level.GetLevelInfo(levelId, dictionary);
				if (levelInfo != null && string.Equals(levelInfo.MatchedCsv, "forced", StringComparison.Ordinal))
				{
					if (ColorSortLevelCodec.LooksLikeLevel(levelInfo.LevelId))
					{
						PickResult pickResult = TryLoadContent(levelInfo.LevelId, levelId, levelInfo, "strategy-forced");
						if (pickResult != null)
						{
							return pickResult;
						}
					}
					PickResult pickResult2 = TryPickFixedFallback(levelId, allowFallback);
					if (pickResult2 != null)
					{
						LogPickDetail("fixed-table", levelId, pickResult2);
						return pickResult2;
					}
				}
				else if (IsDynamicInfoUsable(levelInfo))
				{
					(ColorSortLevel, string) tuple = ColorSortLevelPoolRepository.LoadFromLevelInfo(levelInfo, levelId);
					if (tuple.Item1 != null)
					{
						PickResult pickResult3 = new PickResult
						{
							Level = tuple.Item1,
							Info = levelInfo,
							Content = tuple.Item2,
							FromLinearFallback = false
						};
						LogPickDetail("dynamic", levelId, pickResult3);
						return pickResult3;
					}
				}
				PickResult pickResult4 = TryPickFixedFallback(levelId, allowFallback);
				if (pickResult4 != null)
				{
					LogPickDetail("fixed-fallback", levelId, pickResult4);
					return pickResult4;
				}
				Log.LogError("ColorSortLevelPicker", "取题失败 level={0} csv={1} index={2} levelIdLen={3} poolOk={4} strategy={5}", new object[6]
				{
					levelId,
					levelInfo?.MatchedCsv ?? "-",
					levelInfo?.MatchedIndex ?? (-1),
					(levelInfo?.LevelId?.Length).GetValueOrDefault(),
					ColorSortLevelSystemBootstrap.IsPoolHealthy(),
					LevelSystemBusinessExtensions.HasRuleStrategy()
				});
				return TryFallbacks(levelId, allowFallback);
			}
			catch (Exception ex)
			{
				Log.LogError("ColorSortLevelPicker", "动态取题异常 level={0}: {1}", new object[2] { levelId, ex.Message });
				PickResult pickResult5 = TryPickFixedFallback(levelId, allowFallback);
				if (pickResult5 != null)
				{
					LogPickDetail("fixed-exception", levelId, pickResult5);
					return pickResult5;
				}
				return TryFallbacks(levelId, allowFallback);
			}
		}

		public static PickResult LoadFromCachedPick(int levelId, string content, LevelInfo info)
		{
			return TryLoadContent(content, levelId, info, "cache");
		}

		private static bool IsDynamicInfoUsable(LevelInfo info)
		{
			if (info == null || info.MatchedCsv == "error")
			{
				return false;
			}
			if (ColorSortLevelCodec.LooksLikeLevel(info.LevelId))
			{
				return true;
			}
			if (!string.IsNullOrEmpty(info.MatchedCsv))
			{
				return info.MatchedIndex >= 0;
			}
			return false;
		}

		private static PickResult TryLoadContent(string content, int levelId, LevelInfo info, string source)
		{
			if (string.IsNullOrWhiteSpace(content))
			{
				return null;
			}
			try
			{
				ColorSortLevel colorSortLevel = ColorSortLevelCodec.FromContent(content, levelId);
				if (!colorSortLevel.Validate(out var firstError))
				{
					Log.LogError("ColorSortLevelPicker", "Content 校验失败 source={0} level={1}: {2}", new object[3] { source, levelId, firstError });
					return null;
				}
				PickResult pickResult = new PickResult
				{
					Level = colorSortLevel,
					Info = info,
					Content = content,
					FromLinearFallback = false
				};
				LogPickDetail(source, levelId, pickResult);
				return pickResult;
			}
			catch (Exception ex)
			{
				Log.LogError("ColorSortLevelPicker", "Content 解析失败 source={0} level={1}: {2}", new object[3] { source, levelId, ex.Message });
				return null;
			}
		}

		private static PickResult TryPickFixedFallback(int levelId, bool allowFallback)
		{
			if (!allowFallback)
			{
				return null;
			}
			return ColorSortLevelMatchFixed.TryPick(levelId);
		}

		private static PickResult TryFallbacks(int levelId, bool allowFallback)
		{
			if (!allowFallback)
			{
				return null;
			}
			if (AllowLinearFallback)
			{
				return LoadLinear(levelId, fromFallback: true);
			}
			return null;
		}

		private static PickResult LoadLinear(int levelId, bool fromFallback)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Expected O, but got Unknown
			ColorSortLevel colorSortLevel = ColorSortLevelPoolRepository.TryLoadLevel(levelId);
			if (colorSortLevel == null)
			{
				return null;
			}
			string text = ColorSortLevelPoolRepository.TryGetRawContentByProgress(levelId);
			LevelInfo info = null;
			if (ColorSortLevelPoolRepository.TryGetLevelDifficulty(levelId, out var difficulty, out var csvId) && difficulty > 0f)
			{
				info = new LevelInfo
				{
					LevelId = text,
					MatchedCsv = (csvId ?? "linear"),
					MatchedIndex = levelId - 1,
					LevelDifficulty = difficulty,
					RepositoryPath = "GP101"
				};
			}
			PickResult pickResult = new PickResult
			{
				Level = colorSortLevel,
				Info = info,
				Content = text,
				FromLinearFallback = fromFallback
			};
			LogPickDetail(fromFallback ? "linear-fallback" : "linear", levelId, pickResult);
			return pickResult;
		}

		private static void LogPickDetail(string source, int levelId, PickResult pick)
		{
			if (pick != null)
			{
				LevelInfo info = pick.Info;
				GameLevelProxy obj = GameApp.Proxy.Get<GameLevelProxy>();
				((obj == null) ? null : obj.GetGameLevelProgress()?.GlobalAbility).GetValueOrDefault();
				ResolveTimeFactorM(info);
				ResolveSampledDifficulty(info);
				ResolveTargetCsv(info);
				_ = info?.MatchedCsv;
				_ = info?.MatchedIndex;
				_ = info?.LevelDifficulty;
				if (!string.IsNullOrEmpty(pick.Content))
				{
					_ = pick.Content;
				}
			}
		}

		private static string ResolveTargetCsv(LevelInfo info)
		{
			return ColorSortLevelTargetCsv.ResolveFromInfo(info);
		}

		private static float ResolveTimeFactorM(LevelInfo info)
		{
			if (info != null && info.TempTimeFactorM > 0f)
			{
				return info.TempTimeFactorM;
			}
			LevelSystemRuntimeConfig config = LevelSystem.GetConfig();
			AbilityUpdateConfig val = ((config != null) ? config.AbilityConfig : null);
			if (val == null)
			{
				return 0f;
			}
			return val.GetTimeFactorM();
		}

		private static float ResolveSampledDifficulty(LevelInfo info)
		{
			if (info?.DebugData != null && info.DebugData.TryGetValue("sampled_difficulty", out var value) && value != null)
			{
				try
				{
					return Convert.ToSingle(value);
				}
				catch
				{
				}
			}
			return info?.LevelDifficulty ?? 0f;
		}

		private static string Preview(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return "-";
			}
			if (s.Length > 48)
			{
				return s.Substring(0, 48) + "...";
			}
			return s;
		}
	}
}

