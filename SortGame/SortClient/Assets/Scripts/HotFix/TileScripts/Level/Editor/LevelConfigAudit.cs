#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FishFramework;
using FishTilePlay;
using SimpleJSON;
using UnityEngine;

namespace TileGameCore
{
    /// <summary>
    /// 按关卡区间核对已导出的 GameLevel 配置。
    /// 结论 sheet 汇总格式、z、安全区；另外两个 sheet 只列有问题的关。
    /// </summary>
    public static class LevelConfigAudit
    {
        public const string FileName = "GameLevel配置查验表.xlsx";

        private const string FormatIssue = "格式异常";
        private const string MissingZIssue = "z无对应组合";
        private const string DefaultXIssue = "default的x大于连胜档";
        private const string WinOrderIssue = "连胜x不递增";
        private const string SetCountEqualIssue = "配置数达到set数";
        private const string SetCountOverIssue = "配置数超过set数";
        private const string EmptyIssue = "空配置";
        private const string MissingCfgIssue = "无配置";
        private const string MissingLevelIssue = "无关卡资源";
        private const string Win4YIssue = "win4难度不合法";
        private const string EasyYIssue = "default/fail难度不合法";
        private const string DifficultyWinIssue = "难度关win2/win3不为x,0";
        private const string HardYOnNormalIssue = "非难关出现难度y";
        private const string CoinSetIssue = "金币关set数大于20";
        private const string LayoutIssue = "超出安全区";
        private const string EggCoverIssue = LevelSpecialPlayChecks.EggCoverIssue;
        private const string BoxSameLayerIssue = LevelSpecialPlayChecks.BoxSameLayerIssue;
        private const string BoxUpperIssue = LevelSpecialPlayChecks.BoxUpperIssue;
        private const string BoxAndEggIssue = LevelSpecialPlayChecks.BoxAndEggIssue;
        private const string ExtraEffectIssue = LevelSpecialPlayChecks.ExtraEffectIssue;

        private static readonly string[] AllSlots =
        {
            "defaultCfg", "win1", "win2", "win3", "win4", "fail1", "fail2", "fail3", "fail4"
        };

        private static readonly string[] EmptySlots =
        {
            "defaultCfg", "win2", "win3", "win4", "fail2", "fail3", "fail4"
        };

        private static readonly string[] IssueOrder =
        {
            FormatIssue, MissingZIssue, DefaultXIssue, WinOrderIssue, SetCountEqualIssue, SetCountOverIssue,
            Win4YIssue, EasyYIssue, DifficultyWinIssue, HardYOnNormalIssue, CoinSetIssue, EmptyIssue, MissingCfgIssue, MissingLevelIssue, LayoutIssue,
            EggCoverIssue, BoxSameLayerIssue, BoxUpperIssue, BoxAndEggIssue, ExtraEffectIssue
        };

        public static string Export(LevelPlatform platform, int startIndex, int endIndex, LevelModel[] levels)
        {
            string outputPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FileName));
            Dictionary<int, JSONNode> levelRows = LoadRows(GetLevelConfigPath(platform));
            Dictionary<int, string> tileLists = LoadTileLists(GetTilesConfigPath(platform));
            Dictionary<int, string> tileFormatIssues = CollectTileFormatIssues(tileLists);

            List<IList<string>> byIssue = new List<IList<string>>
            {
                new List<string> { "检查条目", "关卡号", "说明" }
            };
            List<IList<string>> byLevel = new List<IList<string>>
            {
                new List<string>
                {
                    "关卡号", "列表序号", "关卡资源", "set数", "异常说明",
                    "defaultCfg", "win1", "win2", "win3", "win4", "fail1", "fail2", "fail3", "fail4"
                }
            };

            Dictionary<string, List<IList<string>>> issueRows = new Dictionary<string, List<IList<string>>>();
            for (int i = 0; i < IssueOrder.Length; i++)
            {
                issueRows[IssueOrder[i]] = new List<IList<string>>();
            }

            List<int> tileIssueIds = new List<int>(tileFormatIssues.Keys);
            tileIssueIds.Sort();
            for (int i = 0; i < tileIssueIds.Count; i++)
            {
                int tileId = tileIssueIds[i];
                issueRows[FormatIssue].Add(new List<string>
                {
                    FormatIssue,
                    "",
                    "",
                    "组合" + tileId,
                    "棋子组合表 id=" + tileId + "，" + tileFormatIssues[tileId]
                });
            }

            Dictionary<int, LevelAudit> audits = new Dictionary<int, LevelAudit>();
            LevelSpecialPlayChecks.Reload();
            List<LevelSpecialPlayChecks.Finding> playFindings = new List<LevelSpecialPlayChecks.Finding>();
            for (int index = startIndex; index <= endIndex; index++)
            {
                int levelId = index + 1;
                LevelModel level = levels != null && index >= 0 && index < levels.Length ? levels[index] : null;
                string levelName = level != null ? level.name : "";
                int setsAmount = level != null ? level.SetsAmount : -1;
                levelRows.TryGetValue(levelId, out JSONNode row);

                LevelAudit audit = InspectLevel(levelId, index, levelName, setsAmount, row, tileLists, tileFormatIssues);
                if (level != null)
                {
                    string layout = DescribeUnsafeSpan(level);
                    if (!string.IsNullOrEmpty(layout))
                    {
                        audit.Issues.Add(new Issue(LayoutIssue, layout));
                    }

                    playFindings.Clear();
                    LevelSpecialPlayChecks.Collect(level, levelId, playFindings);
                    for (int p = 0; p < playFindings.Count; p++)
                    {
                        audit.Issues.Add(new Issue(playFindings[p].Category, playFindings[p].Detail));
                    }
                }

                if (setsAmount >= 0 && setsAmount <= 3 && row != null && !string.IsNullOrWhiteSpace(ReadString(row, "defaultCfg")))
                {
                    audit.Issues.RemoveAll(issue =>
                        issue.Category == MissingCfgIssue || issue.Category == SetCountOverIssue);
                }

                if (row != null && ReadInt(row, "coinlevel") != 0 && setsAmount > 20)
                {
                    audit.Issues.Add(new Issue(
                        CoinSetIssue,
                        "coinlevel=" + ReadInt(row, "coinlevel") + "，set数" + setsAmount + "大于20"));
                }

                audits[levelId] = audit;
            }

            ApplyDifficultyWinRule(audits, levelRows, startIndex, endIndex, levels);

            List<int> auditIds = new List<int>(audits.Keys);
            auditIds.Sort();
            for (int n = 0; n < auditIds.Count; n++)
            {
                LevelAudit audit = audits[auditIds[n]];
                if (audit.Issues.Count > 0)
                {
                    byLevel.Add(audit.ToLevelRow());
                }

                for (int i = 0; i < audit.Issues.Count; i++)
                {
                    Issue issue = audit.Issues[i];
                    issueRows[issue.Category].Add(new List<string>
                    {
                        issue.Category,
                        audit.LevelId.ToString(),
                        audit.Index.ToString(),
                        audit.LevelName,
                        issue.Detail
                    });
                }
            }

            for (int i = 0; i < IssueOrder.Length; i++)
            {
                List<IList<string>> rows = issueRows[IssueOrder[i]];
                for (int r = 0; r < rows.Count; r++)
                {
                    IList<string> row = rows[r];
                    string levelId = row.Count > 1 ? row[1] : "";
                    string resource = row.Count > 3 ? row[3] : "";
                    string detail = row.Count > 4 ? row[4] : "";
                    byIssue.Add(new List<string>
                    {
                        DisplayName(IssueOrder[i]),
                        string.IsNullOrEmpty(levelId) ? resource : levelId,
                        detail
                    });
                }
            }

            try
            {
                List<IList<string>> conclusions = BuildConclusions(issueRows);
                SimpleXlsxWriter.WriteSheets(outputPath, new[]
                {
                    new SimpleXlsxWriter.Sheet("结论", conclusions, BuildConclusionStyles(conclusions)),
                    new SimpleXlsxWriter.Sheet("按条目", byIssue),
                    new SimpleXlsxWriter.Sheet("按关卡", byLevel)
                });
            }
            catch (IOException)
            {
                throw new IOException("无法写入查验表，请先关闭已打开的 " + FileName);
            }

            return outputPath;
        }

        private static LevelAudit InspectLevel(
            int levelId,
            int index,
            string levelName,
            int setsAmount,
            JSONNode row,
            Dictionary<int, string> tileLists,
            Dictionary<int, string> tileFormatIssues)
        {
            LevelAudit audit = new LevelAudit(levelId, index, levelName, setsAmount);
            if (row == null)
            {
                audit.Issues.Add(new Issue(MissingCfgIssue, "GameLevel 中没有 id=" + levelId));
                return audit;
            }

            Dictionary<string, Slot> slots = new Dictionary<string, Slot>();
            for (int i = 0; i < AllSlots.Length; i++)
            {
                string name = AllSlots[i];
                slots[name] = ParseSlot(name, ReadString(row, name));
            }

            List<string> formatDetails = new List<string>();
            List<string> missingZDetails = new List<string>();
            List<string> setEqualDetails = new List<string>();
            List<string> setOverDetails = new List<string>();
            for (int i = 0; i < AllSlots.Length; i++)
            {
                Slot slot = slots[AllSlots[i]];
                audit.Raw[slot.Name] = slot.Raw;
                if (slot.Empty)
                {
                    continue;
                }

                if (!slot.FormatOk)
                {
                    formatDetails.Add(slot.Name + slot.FormatReason + "：" + slot.Raw);
                    continue;
                }

                if (slot.HasZ && !tileLists.ContainsKey(slot.Z))
                {
                    missingZDetails.Add(slot.Name + "的z(" + slot.Z + ")在棋子组合表中不存在");
                }
                else if (slot.HasZ && tileFormatIssues.TryGetValue(slot.Z, out string tileReason))
                {
                    formatDetails.Add(slot.Name + "引用的组合" + slot.Z + "，" + tileReason);
                }

                if (slot.HasX && setsAmount > 5 && slot.X == setsAmount)
                {
                    setEqualDetails.Add(slot.Name + "的x(" + slot.X + ")达到本关set数(" + setsAmount + ")");
                }
                else if (slot.HasX && setsAmount >= 0 && slot.X > setsAmount)
                {
                    setOverDetails.Add(slot.Name + "的x(" + slot.X + ")超过本关set数(" + setsAmount + ")");
                }
            }

            AddJoined(audit, FormatIssue, formatDetails);
            if (setsAmount < 0)
            {
                audit.Issues.Add(new Issue(MissingLevelIssue, "列表序号" + index + "没有关卡资源，未对照set数"));
            }
            AddJoined(audit, MissingZIssue, missingZDetails);
            AddJoined(audit, SetCountEqualIssue, setEqualDetails);
            AddJoined(audit, SetCountOverIssue, setOverDetails);

            Slot defaultSlot = slots["defaultCfg"];
            List<string> defaultXDetails = new List<string>();
            string[] winSlots = { "win2", "win3", "win4" };
            for (int i = 0; i < winSlots.Length; i++)
            {
                Slot win = slots[winSlots[i]];
                if (!defaultSlot.HasX || !win.HasX || defaultSlot.X <= win.X)
                {
                    continue;
                }

                int gap = defaultSlot.X - win.X;
                if (win.HasY && win.Y == 1)
                {
                    if (gap <= 3)
                    {
                        continue;
                    }

                    defaultXDetails.Add("defaultCfg的x(" + defaultSlot.X + ")比" + win.Name + "的x(" + win.X + ")大" + gap + "，超过3；" + win.Name + "=" + win.Raw);
                    continue;
                }

                defaultXDetails.Add("defaultCfg的x(" + defaultSlot.X + ")大于" + win.Name + "的x(" + win.X + ")；" + win.Name + "=" + win.Raw);
            }

            AddJoined(audit, DefaultXIssue, defaultXDetails);

            List<string> orderDetails = new List<string>();
            AppendOrder(orderDetails, slots["win2"], slots["win3"]);
            AppendOrder(orderDetails, slots["win3"], slots["win4"]);
            AddJoined(audit, WinOrderIssue, orderDetails);
            AppendDifficultyRules(audit, slots);
            AppendHardYOnNormal(audit, slots, ReadInt(row, "Difficulty"));

            List<string> emptyNames = new List<string>();
            for (int i = 0; i < EmptySlots.Length; i++)
            {
                if (slots[EmptySlots[i]].Empty)
                {
                    emptyNames.Add(EmptySlots[i]);
                }
            }

            if (emptyNames.Count > 0)
            {
                audit.Issues.Add(new Issue(EmptyIssue, string.Join("、", emptyNames) + "为空"));
            }

            return audit;
        }

        private static void AppendOrder(List<string> details, Slot earlier, Slot later)
        {
            if (earlier.HasX && later.HasX && later.X < earlier.X)
            {
                details.Add(earlier.Name + "的x(" + earlier.X + ")大于" + later.Name + "的x(" + later.X + ")");
            }
        }

        private static void AddJoined(LevelAudit audit, string category, List<string> details)
        {
            if (details.Count == 0)
            {
                return;
            }

            audit.Issues.Add(new Issue(category, string.Join("；", details)));
        }

        private static void AppendDifficultyRules(LevelAudit audit, Dictionary<string, Slot> slots)
        {
            List<string> win4Details = new List<string>();
            Slot win4 = slots["win4"];
            if (!win4.Empty && win4.FormatOk && !IsAllowedWin4(win4))
            {
                win4Details.Add("win4为" + win4.Raw + "，只允许x,0、x,3、x,4、x,5");
            }

            AddJoined(audit, Win4YIssue, win4Details);

            string[] easySlots = { "defaultCfg", "fail1", "fail2", "fail3", "fail4" };
            List<string> easyDetails = new List<string>();
            for (int i = 0; i < easySlots.Length; i++)
            {
                Slot slot = slots[easySlots[i]];
                if (slot.Empty || !slot.FormatOk)
                {
                    continue;
                }

                if (!slot.HasY || slot.Y != 0)
                {
                    easyDetails.Add(slot.Name + "为" + slot.Raw + "，该档只允许x,0");
                }
            }

            AddJoined(audit, EasyYIssue, easyDetails);
        }

        private static void AppendHardYOnNormal(LevelAudit audit, Dictionary<string, Slot> slots, int difficulty)
        {
            if (difficulty != 0)
            {
                return;
            }

            List<string> details = new List<string>();
            for (int i = 0; i < AllSlots.Length; i++)
            {
                Slot slot = slots[AllSlots[i]];
                if (slot.Empty || !slot.FormatOk || !slot.HasY)
                {
                    continue;
                }

                if (slot.Y != 3 && slot.Y != 4 && slot.Y != 5)
                {
                    continue;
                }

                details.Add(slot.Name + "的y为" + slot.Y + "（" + slot.Raw + "），本关Difficulty=0");
            }

            AddJoined(audit, HardYOnNormalIssue, details);
        }

        private static bool IsAllowedWin4(Slot slot)
        {
            return slot.HasY && (slot.Y == 0 || slot.Y == 3 || slot.Y == 4 || slot.Y == 5);
        }

        private static string DescribeUnsafeSpan(LevelModel level)
        {
            List<string> parts = new List<string>();
            int layerCount = level.AmountOfLayers;
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                Layer layer = level.GetLayer(layerIndex);
                if (layer == null)
                {
                    continue;
                }

                int minX = int.MaxValue;
                int maxX = int.MinValue;
                int minY = int.MaxValue;
                int maxY = int.MinValue;
                bool any = false;
                for (int y = 0; y < layer.AmountOfRows; y++)
                {
                    LayerRow row = layer.GetRow(y);
                    if (row == null)
                    {
                        continue;
                    }

                    for (int x = 0; x < row.AmountOfCells; x++)
                    {
                        if (!row[x])
                        {
                            continue;
                        }

                        any = true;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }

                if (!any)
                {
                    continue;
                }

                int width = maxX - minX + 1;
                int height = maxY - minY + 1;
                if (width > 9 || height > 11)
                {
                    parts.Add("第" + (layerIndex + 1) + "层横向" + width + "格、竖向" + height + "格");
                }
            }

            if (parts.Count == 0)
            {
                return null;
            }

            bool notCentered = Mathf.Abs(level.ShowLayerScale - 1f) <= 0.0001f
                && Mathf.Abs(level.ShowLayerDeltaX) <= 0.0001f
                && Mathf.Abs(level.ShowLayerDeltaY) <= 0.0001f;
            if (notCentered)
            {
                parts.Add("未做Center");
            }

            return string.Join("；", parts);
        }

        private static readonly string[] ConclusionOrder =
        {
            DefaultXIssue, WinOrderIssue, SetCountOverIssue, SetCountEqualIssue,
            Win4YIssue, EasyYIssue, DifficultyWinIssue, HardYOnNormalIssue, CoinSetIssue, EmptyIssue, MissingCfgIssue, MissingLevelIssue,
            FormatIssue, MissingZIssue, LayoutIssue,
            EggCoverIssue, BoxSameLayerIssue, BoxUpperIssue, BoxAndEggIssue, ExtraEffectIssue
        };

        private static string DisplayName(string category)
        {
            if (category == FormatIssue)
            {
                return "格式异常";
            }

            if (category == MissingZIssue)
            {
                return "z是否存在";
            }

            if (category == LayoutIssue)
            {
                return "设计适配异常";
            }

            return category;
        }

        private static List<IList<string>> BuildConclusions(Dictionary<string, List<IList<string>>> issueRows)
        {
            List<IList<string>> rows = new List<IList<string>>
            {
                new List<string> { "检查条目", "异常数目", "异常关卡号陈列", "说明" }
            };
            for (int i = 0; i < ConclusionOrder.Length; i++)
            {
                string category = ConclusionOrder[i];
                issueRows.TryGetValue(category, out List<IList<string>> found);
                rows.Add(ConclusionRow(DisplayName(category), found, RuleText(category)));
            }

            return rows;
        }

        private static List<IList<int>> BuildConclusionStyles(List<IList<string>> conclusions)
        {
            List<IList<int>> styles = new List<IList<int>> { null };
            for (int i = 1; i < conclusions.Count; i++)
            {
                IList<string> row = conclusions[i];
                string count = row != null && row.Count > 1 ? row[1] : "无";
                if (count == "无" || count == "0")
                {
                    styles.Add(null);
                    continue;
                }

                int columnCount = row != null ? row.Count : 0;
                List<int> styleRow = new List<int>(columnCount);
                for (int c = 0; c < columnCount; c++)
                {
                    styleRow.Add(SimpleXlsxWriter.AlertStyle);
                }

                styles.Add(styleRow);
            }

            return styles;
        }

        private static string RuleText(string category)
        {
            if (category == DefaultXIssue)
            {
                return "比较 defaultCfg 与 win2、win3、win4 的 x，两边都能解析出 x 才比较。连胜档 y=1 时，只有 default 的 x 比它大超过 3 才上报（11,0 对 10,1 正常，11,0 对 7,1 上报）。连胜档 y 不是 1（如 x,0）时，default 的 x 只要更大就上报（11,0 对 7,0 上报）。";
            }

            if (category == WinOrderIssue)
            {
                return "看 win2 对 win3、win3 对 win4。相邻两档都能解析出 x 时，后一档的 x 不得小于前一档，相等不算异常。某一档为空或格式无法解析时，不参与比较。";
            }

            if (category == SetCountOverIssue)
            {
                return "default、win1–win4、fail1–fail4 里能解析出的 x，大于本关 set 数（实心格数÷3）就上报，不限 set 数大小。set 数≤3 且 defaultCfg 有内容的关，这一条不上报。";
            }

            if (category == SetCountEqualIssue)
            {
                return "上述各档的 x 等于本关 set 数才上报，且只查 set 数大于 5 的关。set 数≤5 不算这一条。x 大于 set 数归到「配置数超过set数」。";
            }

            if (category == Win4YIssue)
            {
                return "win4 非空且能解析时，必须写成 x,y，y 只能是 0、3、4、5。没有 y，或 y 是其他数，都上报。win4 空白归「空配置」，不在这一条。";
            }

            if (category == EasyYIssue)
            {
                return "defaultCfg、fail1、fail2、fail3、fail4 非空且能解析时，必须写成 x,0。没有 y，或 y 不是 0，都上报。";
            }

            if (category == DifficultyWinIssue)
            {
                return "Difficulty 不为 0 的关，本关以及前一关、后一关的 win2、win3 都必须是 x,0（y 必须是 0，可以带 z）。空白、缺 y、y 不是 0、格式无法解析，都上报。关卡库两端没有的相邻关不查。触发关在检测区间内时，区间外的相邻关也会上报。";
            }

            if (category == HardYOnNormalIssue)
            {
                return "y 为 3、4、5 是难度参数，只能出现在 Difficulty 不为 0 的关。其他关的 default、win1–win4、fail1–fail4 里只要出现这种 y，就上报。Difficulty 为 0 视为非难关。";
            }

            if (category == CoinSetIssue)
            {
                return "coinlevel 不为 0 的关是金币关。该关 set 数（实心格数÷3）大于 20 就上报。coinlevel 为 0 不查。";
            }

            if (category == EmptyIssue)
            {
                return "GameLevel 里有这一行，但必填格是空白。必填：defaultCfg、win2、win3、win4、fail2、fail3、fail4。win1、fail1 空着不算。";
            }

            if (category == MissingCfgIssue)
            {
                return "检测区间的关卡号（列表序号+1）在已导出的 GameLevel 里没有这个 id，整行都没有，default、win、fail 都读不到。";
            }

            if (category == MissingLevelIssue)
            {
                return "关卡库该列表序号没有关卡资源，得不到 set 数，因此不做配置数和 set 数的对照。配置行如果还在，格式、难度等仍会查。";
            }

            if (category == FormatIssue)
            {
                return "关卡各档和棋子组合表都查。全角逗号、全角分号、不间断空格、全角空格、空字段、无法解析成整数，都算异常；逗号两侧的普通空格会去掉，不算。组合内容还须是「整数_整数」，同组用 | 分隔，多组用英文逗号分隔。组合问题在关卡号里写成「组合id」，全表都查。某档引用了格式坏的组合，也会记在该关。";
            }

            if (category == MissingZIssue)
            {
                return "档位写成 x,y,z 三段时，z 必须是棋子组合表里已有的 id。只有 x,y 两段的没有 z，不查这一条。";
            }

            if (category == LayoutIssue)
            {
                return "任一层已填格子的包围盒，宽超过 9 格或高超过 11 格就上报（第 1 层是最上面一层）。该关缩放约等于 1 且位移约等于 0 时，明细里会再附「未做Center」。";
            }

            if (category == EggCoverIssue)
            {
                return "eggsmash 关（彩蛋关表）里，生成器实际可能放出来的 egg，上方棋子少于 2 个就上报。第 1 层是最上面一层。";
            }

            if (category == BoxSameLayerIssue)
            {
                return "boxcover 关里，同一层两个会一起生成的 boxcover 范围叠在一起就上报。只贴边不算。落点组合过多时，明细会说明只查了部分生成结果。";
            }

            if (category == BoxUpperIssue)
            {
                return "boxcover 关里，生成出来的 boxcover 上方没有任何棋子挡住就上报。只算棋子，不算上面另一只 boxcover。";
            }

            if (category == BoxAndEggIssue)
            {
                return "同一关既配置了 boxcover，又在彩蛋关表里。同一关只能有其中一种玩法。";
            }

            if (category == ExtraEffectIssue)
            {
                return "已填棋子上带了 unknown 以外的玩法就上报，包括 Crate、Ice、Link。普通棋子和 unknown 不算。数字是该玩法的棋子个数。";
            }

            return "";
        }

        private static List<string> ConclusionRow(string title, List<IList<string>> found, string rule)
        {
            List<string> labels = new List<string>();
            if (found != null)
            {
                for (int i = 0; i < found.Count; i++)
                {
                    IList<string> row = found[i];
                    string levelId = row.Count > 1 ? row[1] : "";
                    string resource = row.Count > 3 ? row[3] : "";
                    string label = string.IsNullOrEmpty(levelId) ? resource : "L" + levelId;
                    if (string.IsNullOrEmpty(label) || labels.Contains(label))
                    {
                        continue;
                    }

                    labels.Add(label);
                }
            }

            if (labels.Count == 0)
            {
                return new List<string> { title, "无", "无", rule };
            }

            return new List<string>
            {
                title,
                labels.Count.ToString(),
                string.Join("；", labels),
                rule
            };
        }

        private static Slot ParseSlot(string name, string raw)
        {
            Slot slot = new Slot { Name = name, Raw = raw ?? "" };
            if (string.IsNullOrWhiteSpace(slot.Raw))
            {
                slot.Empty = true;
                slot.FormatOk = true;
                return slot;
            }

            if (slot.Raw.IndexOf('，') >= 0)
            {
                slot.FormatReason = "含全角逗号";
                return slot;
            }

            if (slot.Raw.IndexOf('；') >= 0)
            {
                slot.FormatReason = "含全角分号";
                return slot;
            }

            if (slot.Raw.IndexOf('\u00A0') >= 0 || slot.Raw.IndexOf('\u3000') >= 0)
            {
                slot.FormatReason = "含异常空格";
                return slot;
            }

            string[] parts = slot.Raw.Split(',');
            int[] numbers = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string trimmed = parts[i].Trim();
                if (trimmed.Length == 0)
                {
                    slot.FormatReason = "有空字段";
                    return slot;
                }

                if (!int.TryParse(trimmed, out numbers[i]))
                {
                    slot.FormatReason = "无法解析成整数";
                    return slot;
                }
            }

            slot.FormatOk = true;
            slot.HasX = true;
            slot.X = numbers[0];
            if (numbers.Length >= 2)
            {
                slot.HasY = true;
                slot.Y = numbers[1];
            }

            if (numbers.Length >= 3)
            {
                slot.HasZ = true;
                slot.Z = numbers[2];
            }

            return slot;
        }

        private static Dictionary<int, string> CollectTileFormatIssues(Dictionary<int, string> tileLists)
        {
            Dictionary<int, string> issues = new Dictionary<int, string>();
            foreach (KeyValuePair<int, string> pair in tileLists)
            {
                string reason = CheckSameColorList(pair.Value);
                if (!string.IsNullOrEmpty(reason))
                {
                    issues[pair.Key] = reason + "：" + pair.Value;
                }
            }

            return issues;
        }

        private static string CheckSameColorList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "组合内容为空";
            }

            if (raw.IndexOf('，') >= 0)
            {
                return "含全角逗号";
            }

            if (raw.IndexOf('；') >= 0)
            {
                return "含全角分号";
            }

            if (raw.IndexOf('\u00A0') >= 0 || raw.IndexOf('\u3000') >= 0)
            {
                return "含异常空格";
            }

            string[] groups = raw.Split(',');
            for (int i = 0; i < groups.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(groups[i]))
                {
                    return "有空字段";
                }

                string[] items = groups[i].Split('|');
                for (int j = 0; j < items.Length; j++)
                {
                    string item = items[j].Trim();
                    string[] parts = item.Split('_');
                    if (parts.Length != 2 || !int.TryParse(parts[0], out _) || !int.TryParse(parts[1], out _))
                    {
                        return "无法解析「" + item + "」";
                    }
                }
            }

            return null;
        }

        private static Dictionary<int, JSONNode> LoadRows(string path)
        {
            Dictionary<int, JSONNode> map = new Dictionary<int, JSONNode>();
            JSONNode root = JSON.Parse(File.ReadAllText(path));
            if (root == null)
            {
                throw new InvalidDataException("无法解析配置：" + path);
            }

            foreach (JSONNode row in root.Children)
            {
                map[row["id"].AsInt] = row;
            }

            return map;
        }

        private static Dictionary<int, string> LoadTileLists(string path)
        {
            Dictionary<int, string> map = new Dictionary<int, string>();
            JSONNode root = JSON.Parse(File.ReadAllText(path));
            if (root == null)
            {
                throw new InvalidDataException("无法解析棋子组合表：" + path);
            }

            foreach (JSONNode row in root.Children)
            {
                map[row["id"].AsInt] = ReadString(row, "sameColorList");
            }

            return map;
        }

        private static void ApplyDifficultyWinRule(
            Dictionary<int, LevelAudit> audits,
            Dictionary<int, JSONNode> levelRows,
            int startIndex,
            int endIndex,
            LevelModel[] levels)
        {
            Dictionary<int, List<string>> details = new Dictionary<int, List<string>>();
            int catalogCount = levels != null ? levels.Length : 0;
            for (int index = startIndex; index <= endIndex; index++)
            {
                int sourceId = index + 1;
                if (!levelRows.TryGetValue(sourceId, out JSONNode source) || source == null)
                {
                    continue;
                }

                int difficulty = ReadInt(source, "Difficulty");
                if (difficulty == 0)
                {
                    continue;
                }

                int[] targets = { sourceId - 1, sourceId, sourceId + 1 };
                for (int t = 0; t < targets.Length; t++)
                {
                    int targetId = targets[t];
                    int targetIndex = targetId - 1;
                    if (targetIndex < 0 || targetIndex >= catalogCount)
                    {
                        continue;
                    }

                    string problem = DescribeWinNotZero(levelRows, targetId);
                    if (string.IsNullOrEmpty(problem))
                    {
                        continue;
                    }

                    if (!details.TryGetValue(targetId, out List<string> lines))
                    {
                        lines = new List<string>();
                        details[targetId] = lines;
                    }

                    string who = targetId == sourceId ? "本关" : "相邻关L" + sourceId;
                    lines.Add(who + " Difficulty=" + difficulty + "，" + problem);
                }
            }

            List<int> targetIds = new List<int>(details.Keys);
            targetIds.Sort();
            for (int i = 0; i < targetIds.Count; i++)
            {
                int targetId = targetIds[i];
                LevelAudit audit = GetOrCreateAudit(audits, targetId, levels, levelRows);
                audit.Issues.Add(new Issue(DifficultyWinIssue, string.Join("；", details[targetId])));
            }
        }

        private static string DescribeWinNotZero(Dictionary<int, JSONNode> levelRows, int levelId)
        {
            if (!levelRows.TryGetValue(levelId, out JSONNode row) || row == null)
            {
                return "L" + levelId + "无配置，win2、win3不是x,0";
            }

            List<string> problems = new List<string>();
            AppendIfNotX0(problems, ParseSlot("win2", ReadString(row, "win2")));
            AppendIfNotX0(problems, ParseSlot("win3", ReadString(row, "win3")));
            if (problems.Count == 0)
            {
                return null;
            }

            return string.Join("，", problems);
        }

        private static void AppendIfNotX0(List<string> problems, Slot slot)
        {
            if (!slot.Empty && slot.FormatOk && slot.HasY && slot.Y == 0)
            {
                return;
            }

            string shown = slot.Empty ? "空白" : slot.Raw;
            problems.Add(slot.Name + "为" + shown + "，须为x,0");
        }

        private static LevelAudit GetOrCreateAudit(
            Dictionary<int, LevelAudit> audits,
            int levelId,
            LevelModel[] levels,
            Dictionary<int, JSONNode> levelRows)
        {
            if (audits.TryGetValue(levelId, out LevelAudit audit))
            {
                return audit;
            }

            int index = levelId - 1;
            LevelModel level = levels != null && index >= 0 && index < levels.Length ? levels[index] : null;
            audit = new LevelAudit(
                levelId,
                index,
                level != null ? level.name : "",
                level != null ? level.SetsAmount : -1);
            if (levelRows.TryGetValue(levelId, out JSONNode row) && row != null)
            {
                for (int i = 0; i < AllSlots.Length; i++)
                {
                    audit.Raw[AllSlots[i]] = ReadString(row, AllSlots[i]);
                }
            }

            audits[levelId] = audit;
            return audit;
        }

        private static int ReadInt(JSONNode row, string key)
        {
            JSONNode node = row[key];
            if (node == null || node.IsNull || !node.IsNumber)
            {
                return 0;
            }

            return node.AsInt;
        }

        private static string ReadString(JSONNode row, string key)
        {
            JSONNode node = row[key];
            if (node == null || node.IsNull)
            {
                return "";
            }

            return node.Value ?? "";
        }

        private static string GetLevelConfigPath(LevelPlatform platform)
        {
            string file = platform == LevelPlatform.IOS
                ? "game_tbgameleveconfigios.json"
                : "game_tbgameleveconfigandroid.json";
            return Path.Combine(Application.dataPath, "CABRes", "Configs", file);
        }

        private static string GetTilesConfigPath(LevelPlatform platform)
        {
            string file = platform == LevelPlatform.IOS
                ? "game_tbgameleveltilescfgios.json"
                : "game_tbgameleveltilescfgandroid.json";
            return Path.Combine(Application.dataPath, "CABRes", "Configs", file);
        }

        private struct Slot
        {
            public string Name;
            public string Raw;
            public bool Empty;
            public bool FormatOk;
            public string FormatReason;
            public bool HasX;
            public int X;
            public bool HasY;
            public int Y;
            public bool HasZ;
            public int Z;
        }

        private struct Issue
        {
            public string Category;
            public string Detail;

            public Issue(string category, string detail)
            {
                Category = category;
                Detail = detail;
            }
        }

        private sealed class LevelAudit
        {
            public readonly int LevelId;
            public readonly int Index;
            public readonly string LevelName;
            public readonly int SetsAmount;
            public readonly List<Issue> Issues = new List<Issue>();
            public readonly Dictionary<string, string> Raw = new Dictionary<string, string>();

            public LevelAudit(int levelId, int index, string levelName, int setsAmount)
            {
                LevelId = levelId;
                Index = index;
                LevelName = levelName;
                SetsAmount = setsAmount;
            }

            public List<string> ToLevelRow()
            {
                StringBuilder detail = new StringBuilder();
                for (int i = 0; i < Issues.Count; i++)
                {
                    if (detail.Length > 0)
                    {
                        detail.Append('；');
                    }

                    detail.Append(Issues[i].Category);
                    detail.Append('：');
                    detail.Append(Issues[i].Detail);
                }

                return new List<string>
                {
                    LevelId.ToString(),
                    Index.ToString(),
                    LevelName,
                    SetsAmount >= 0 ? SetsAmount.ToString() : "",
                    detail.ToString(),
                    RawValue("defaultCfg"),
                    RawValue("win1"),
                    RawValue("win2"),
                    RawValue("win3"),
                    RawValue("win4"),
                    RawValue("fail1"),
                    RawValue("fail2"),
                    RawValue("fail3"),
                    RawValue("fail4")
                };
            }

            private string RawValue(string key)
            {
                return Raw.TryGetValue(key, out string value) ? value : "";
            }
        }
    }
}
#endif
