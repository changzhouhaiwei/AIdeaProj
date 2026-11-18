using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UniLog;
using XingYun.Core.Singletons;
using XingYun.Res;

namespace XingYun.Level
{
	public sealed class LevelRepository
	{
		private readonly string[] dimensionKeys;

		private readonly string root;

		private string repositoryName;

		private readonly Dictionary<string, (int start, int len)> offsets = new Dictionary<string, (int, int)>(StringComparer.Ordinal);

		private readonly List<string> catalog = new List<string>();

		private readonly Dictionary<string, (float min, float max)> ranges = new Dictionary<string, (float, float)>(StringComparer.Ordinal);

		private readonly Dictionary<string, Dictionary<string, List<string>>> dimValues = new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.Ordinal);

		private readonly Dictionary<string, Dictionary<string, List<LevelEntry>>> csvCache = new Dictionary<string, Dictionary<string, List<LevelEntry>>>(StringComparer.Ordinal);

		private readonly Dictionary<string, (float min, float max)> fileDifficulties = new Dictionary<string, (float, float)>(StringComparer.Ordinal);

		public LevelRepository(string rootPath, string[] dimensionKeys = null)
		{
			root = rootPath;
			this.dimensionKeys = dimensionKeys ?? new string[4] { "Difficulty", "Difficulty2", "WinLose", "Weight" };
			LoadCatalog();
			LoadOffsets();
			LoadRanges();
			BuildDimValues();
		}

		public bool HasCsv(string csvId)
		{
			return offsets.ContainsKey(csvId);
		}

		public IEnumerable<string> AllCsvIds()
		{
			return catalog;
		}

		public IEnumerable<string> GetBucketsForDifficulty(float difficulty)
		{
			foreach (KeyValuePair<string, (float, float)> range in ranges)
			{
				var (num, num2) = range.Value;
				if (difficulty >= num && difficulty <= num2)
				{
					yield return range.Key;
				}
			}
		}

		public IEnumerable<string> GetDimensionValues(string dimKey, string anchor = null)
		{
			if (!dimValues.TryGetValue(dimKey, out var value) || value.Count == 0)
			{
				return Array.Empty<string>();
			}
			Dictionary<string, List<string>>.KeyCollection keys = value.Keys;
			int result;
			if (int.TryParse(anchor, out var a))
			{
				return keys.OrderBy((string v) => Math.Abs((int.TryParse(v, out result) ? result : int.MaxValue) - a));
			}
			return keys.OrderBy((string v) => v);
		}

		public List<LevelEntry> GetCsvEntries(string csvId)
		{
			string repositoryPath = LevelSystem.GetConfig().RepositoryPath;
			if (csvCache.TryGetValue(repositoryPath, out var value) && value.TryGetValue(csvId, out var value2))
			{
				return value2;
			}
			if (!offsets.TryGetValue(csvId, out (int, int) value3))
			{
				return null;
			}
			byte[] array = MonoSingleton<ResManager>.Instance.LoadRawFileOffset(root + "_level_data", value3.Item1, value3.Item2);
			if (array == null)
			{
				Log.LogError("XYFW-LevelRepository", root + "_level_data GetCsvEntries " + csvId + " failed, blob is null");
				return null;
			}
			if (!csvCache.ContainsKey(repositoryPath))
			{
				csvCache.Add(repositoryPath, new Dictionary<string, List<LevelEntry>>());
			}
			List<LevelEntry> list = ParseCsvBlob(array);
			csvCache[repositoryPath][csvId] = list;
			return list;
		}

		public Dictionary<string, (float min, float max)> GetRangesDictionary()
		{
			return ranges;
		}

		public float GetGlobalDifficultyRange()
		{
			List<KeyValuePair<string, (float, float)>> list = LevelSystem.GetRepository().GetRangesDictionary().ToList();
			if (list.Count == 0)
			{
				return 1000f;
			}
			float num = list.Max<KeyValuePair<string, (float, float)>>((KeyValuePair<string, (float min, float max)> r) => r.Value.max);
			float num2 = list.Min<KeyValuePair<string, (float, float)>>((KeyValuePair<string, (float min, float max)> r) => r.Value.min);
			return num - num2;
		}

		public (float min, float max) GetGlobalDifficultyMinMax()
		{
			List<KeyValuePair<string, (float, float)>> list = LevelSystem.GetRepository().GetRangesDictionary().ToList();
			if (list.Count == 0)
			{
				Log.LogError("XYFW-LevelAbilityUpdate", "No difficulty ranges found in repository.");
				return (min: 500f, max: 2000f);
			}
			float num = float.MinValue;
			float num2 = float.MaxValue;
			foreach (KeyValuePair<string, (float, float)> item in list)
			{
				if (item.Value.Item2 > num)
				{
					num = item.Value.Item2;
				}
				if (item.Value.Item1 < num2)
				{
					num2 = item.Value.Item1;
				}
			}
			return (min: num2, max: num);
		}

		public int DimensionCountFromCatalog()
		{
			using (List<string>.Enumerator enumerator = catalog.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return Path.GetFileNameWithoutExtension(enumerator.Current).Split('_').Length;
				}
			}
			return 0;
		}

		private void LoadCatalog()
		{
			catalog.Clear();
			fileDifficulties.Clear();
			byte[] array = MonoSingleton<ResManager>.Instance.LoadRawFile(root + "_level_catalog");
			if (array == null)
			{
				Log.LogError("XYFW-LevelRepository", root + "_level_catalog LoadCatalog failed, bytes is null");
				return;
			}
			using MemoryStream input = new MemoryStream(array);
			using BinaryReader binaryReader = new BinaryReader(input, Encoding.UTF8);
			repositoryName = ReadRepositoryName(binaryReader);
			ReadString(binaryReader);
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int count = binaryReader.ReadInt32();
				string @string = Encoding.UTF8.GetString(binaryReader.ReadBytes(count));
				float item = binaryReader.ReadSingle();
				float item2 = binaryReader.ReadSingle();
				catalog.Add(@string);
				fileDifficulties[@string] = (item, item2);
			}
		}

		public (float min, float max)? GetFileDifficultyRange(string fileName)
		{
			if (fileDifficulties.TryGetValue(fileName, out (float, float) value))
			{
				return value;
			}
			return null;
		}

		public IEnumerable<string> GetFilesInDifficultyRange(float minDifficulty, float maxDifficulty)
		{
			foreach (KeyValuePair<string, (float, float)> fileDifficulty in fileDifficulties)
			{
				var (num, num2) = fileDifficulty.Value;
				if (num <= maxDifficulty && num2 >= minDifficulty)
				{
					yield return fileDifficulty.Key;
				}
			}
		}

		public IEnumerable<string> GetFilesContainingDifficulty(float difficulty)
		{
			foreach (KeyValuePair<string, (float, float)> fileDifficulty in fileDifficulties)
			{
				var (num, num2) = fileDifficulty.Value;
				if (difficulty >= num && difficulty <= num2)
				{
					yield return fileDifficulty.Key;
				}
			}
		}

		public IEnumerable<KeyValuePair<string, (float min, float max)>> GetAllFileDifficulties()
		{
			return fileDifficulties;
		}

		private void LoadOffsets()
		{
			offsets.Clear();
			byte[] array = MonoSingleton<ResManager>.Instance.LoadRawFile(root + "_level_data_offsets");
			if (array == null)
			{
				Log.LogError("XYFW-LevelRepository", root + "_level_data_offsets LoadOffsets failed, bytes is null");
				return;
			}
			using MemoryStream input = new MemoryStream(array);
			using BinaryReader binaryReader = new BinaryReader(input, Encoding.UTF8);
			string text = ReadRepositoryName(binaryReader);
			ReadString(binaryReader);
			if (!string.IsNullOrEmpty(repositoryName) && text != repositoryName)
			{
				throw new InvalidDataException("Repository name mismatch : catalog=" + repositoryName + ", offsets=" + text);
			}
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int count = binaryReader.ReadInt32();
				string @string = Encoding.UTF8.GetString(binaryReader.ReadBytes(count));
				int item = binaryReader.ReadInt32();
				int item2 = binaryReader.ReadInt32();
				offsets[@string] = (item, item2);
			}
		}

		private void LoadRanges()
		{
			ranges.Clear();
			byte[] array = MonoSingleton<ResManager>.Instance.LoadRawFile(root + "_level_ranges");
			if (array == null)
			{
				Log.LogError("XYFW-LevelRepository", root + "_level_ranges LoadRanges failed, bytes is null");
				return;
			}
			using MemoryStream input = new MemoryStream(array);
			using BinaryReader binaryReader = new BinaryReader(input, Encoding.UTF8);
			string text = ReadRepositoryName(binaryReader);
			ReadString(binaryReader);
			if (!string.IsNullOrEmpty(repositoryName) && text != repositoryName)
			{
				throw new InvalidDataException("Repository name mismatch : catalog=" + repositoryName + ", ranges=" + text);
			}
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				int count = binaryReader.ReadInt32();
				string @string = Encoding.UTF8.GetString(binaryReader.ReadBytes(count));
				float item = binaryReader.ReadSingle();
				float item2 = binaryReader.ReadSingle();
				ranges[@string] = (item, item2);
			}
		}

		private static string ReadRepositoryName(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			return Encoding.UTF8.GetString(reader.ReadBytes(count));
		}

		private static string ReadString(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			return Encoding.UTF8.GetString(reader.ReadBytes(count));
		}

		private void BuildDimValues()
		{
			dimValues.Clear();
			int num = DimensionCountFromCatalog();
			if (num <= 0)
			{
				return;
			}
			List<IDimensionStrategy> originalDimensions = LevelSystem.GetOriginalDimensions();
			List<string> list = new List<string>(num);
			for (int i = 0; i < num; i++)
			{
				string text = ((i < originalDimensions.Count) ? originalDimensions[i].Key : $"d{i}");
				list.Add(text);
				dimValues[text] = new Dictionary<string, List<string>>(StringComparer.Ordinal);
			}
			foreach (string item in catalog)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
				string[] array = fileNameWithoutExtension.Split('_');
				for (int j = 0; j < Math.Min(array.Length, num); j++)
				{
					string key = list[j];
					string key2 = array[j];
					if (!dimValues[key].ContainsKey(key2))
					{
						dimValues[key][key2] = new List<string>();
					}
					dimValues[key][key2].Add(fileNameWithoutExtension);
				}
			}
		}

		public List<string> GetFilesWithDimensionValue(string dimensionKey, string dimensionValue)
		{
			if (dimValues.TryGetValue(dimensionKey, out var value) && value.TryGetValue(dimensionValue, out var value2))
			{
				return value2;
			}
			return new List<string>();
		}

		private List<LevelEntry> ParseCsvBlob(byte[] blob)
		{
			try
			{
				using MemoryStream stream = new MemoryStream(blob);
				using BrotliStream brotliStream = new BrotliStream(stream, CompressionMode.Decompress);
				using MemoryStream memoryStream = new MemoryStream();
				brotliStream.CopyTo(memoryStream);
				byte[] blob2 = memoryStream.ToArray();
				return ParseDecompressedBlob(blob2);
			}
			catch
			{
				return ParseDecompressedBlob(blob);
			}
		}

		private List<LevelEntry> ParseDecompressedBlob(byte[] blob)
		{
			using MemoryStream memoryStream = new MemoryStream(blob);
			using BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8);
			int num = binaryReader.ReadInt32();
			long num2 = memoryStream.Position + num;
			List<(int, float, int, int)> list = new List<(int, float, int, int)>();
			while (memoryStream.Position < num2)
			{
				int item = binaryReader.ReadInt32();
				float item2 = binaryReader.ReadSingle();
				int item3 = binaryReader.ReadInt32();
				int item4 = binaryReader.ReadInt32();
				list.Add((item, item2, item3, item4));
			}
			long position = memoryStream.Position;
			List<LevelEntry> list2 = new List<LevelEntry>(list.Count);
			foreach (var item5 in list)
			{
				memoryStream.Position = position + item5.Item3;
				byte[] bytes = binaryReader.ReadBytes(item5.Item4);
				string @string = Encoding.UTF8.GetString(bytes);
				list2.Add(new LevelEntry
				{
					Index = item5.Item1,
					Difficulty = item5.Item2,
					Content = @string
				});
			}
			return list2;
		}
	}
}

