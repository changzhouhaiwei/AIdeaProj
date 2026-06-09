using System;
using System.Collections.Generic;
using System.Globalization;

[Serializable]
public class ThreeLevelData
{
    public int level;
    public int levelType;
    public int steps;
    public int xMax;
    public int yMax;
    public int fillType;

    public List<TargetInfo> targetInfo = new List<TargetInfo>();
    public List<SlotInfo> slotInfo = new List<SlotInfo>();
    public List<BottomInfo> bottomInfo = new List<BottomInfo>();
    public List<PieceInfo> pieceInfo = new List<PieceInfo>();
    public List<UpperInfo> upperInfo = new List<UpperInfo>();
    public List<int> emptyFillRule = new List<int>();
    public List<SpawnerRuleInfo> spawnerRuleInfo = new List<SpawnerRuleInfo>();
    public List<SpawnerRuleAllocation> spawnerRuleAllocation = new List<SpawnerRuleAllocation>();

    public static ThreeLevelData FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Level json is empty.");
        }

        ThreeLevelData levelData = new ThreeLevelData();

        levelData.level = ParseIntField(json, "level", true);
        levelData.levelType = ParseIntField(json, "levelType", true);
        levelData.steps = ParseIntField(json, "steps", true);
        levelData.xMax = ParseIntField(json, "xMax", true);
        levelData.yMax = ParseIntField(json, "yMax", true);
        levelData.fillType = ParseIntField(json, "fillType", false);

        levelData.targetInfo = ParseTargetInfoList(ParseIntMatrix(ExtractJsonValue(json, "targetInfo")));
        levelData.slotInfo = ParseSlotInfoList(ParseIntMatrix(ExtractJsonValue(json, "slotInfo")));
        levelData.bottomInfo = ParseBottomInfoList(ParseIntMatrix(ExtractJsonValue(json, "bottomInfo")));
        levelData.pieceInfo = ParsePieceInfoList(ParseIntMatrix(ExtractJsonValue(json, "pieceInfo")));
        levelData.upperInfo = ParseUpperInfoList(ParseIntMatrix(ExtractJsonValue(json, "upperInfo")));
        levelData.emptyFillRule = ParseIntList(ExtractJsonValue(json, "emptyFillRule"));
        levelData.spawnerRuleInfo = ParseSpawnerRuleInfoList(ExtractJsonValue(json, "spawnerRuleInfo"));
        levelData.spawnerRuleAllocation = ParseSpawnerRuleAllocationList(ParseIntMatrix(ExtractJsonValue(json, "spawnerRuleAllocation")));

        return levelData;
    }

    public static bool TryFromJson(string json, out ThreeLevelData data, out string error)
    {
        data = null;
        error = string.Empty;

        try
        {
            data = FromJson(json);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static List<TargetInfo> ParseTargetInfoList(List<int[]> rows)
    {
        List<TargetInfo> result = new List<TargetInfo>();
        for (int i = 0; i < rows.Count; i++)
        {
            int[] row = rows[i];
            if (row.Length < 2)
            {
                continue;
            }

            result.Add(new TargetInfo(row[0], row[1]));
        }

        return result;
    }

    private static List<SlotInfo> ParseSlotInfoList(List<int[]> rows)
    {
        List<SlotInfo> result = new List<SlotInfo>();
        for (int i = 0; i < rows.Count; i++)
        {
            int[] row = rows[i];
            if (row.Length < 4)
            {
                continue;
            }

            result.Add(new SlotInfo(row[0], row[1], row[2], row[3]));
        }

        return result;
    }

    private static List<BottomInfo> ParseBottomInfoList(List<int[]> rows)
    {
        List<BottomInfo> result = new List<BottomInfo>();
        for (int i = 0; i < rows.Count; i++)
        {
            int[] row = rows[i];
            if (row.Length < 6)
            {
                continue;
            }

            result.Add(new BottomInfo(row[0], row[1], row[2], row[3], row[4], row[5]));
        }

        return result;
    }

    private static List<PieceInfo> ParsePieceInfoList(List<int[]> rows)
    {
        List<PieceInfo> result = new List<PieceInfo>();
        for (int i = 0; i < rows.Count; i++)
        {
            int[] row = rows[i];
            if (row.Length < 7)
            {
                continue;
            }

            result.Add(new PieceInfo(row[0], row[1], row[2], row[3], row[4], row[5], row[6]));
        }

        return result;
    }

    private static List<UpperInfo> ParseUpperInfoList(List<int[]> rows)
    {
        List<UpperInfo> result = new List<UpperInfo>();
        for (int i = 0; i < rows.Count; i++)
        {
            int[] row = rows[i];
            if (row.Length < 6)
            {
                continue;
            }

            result.Add(new UpperInfo(row[0], row[1], row[2], row[3], row[4], row[5]));
        }

        return result;
    }

    private static List<SpawnerRuleAllocation> ParseSpawnerRuleAllocationList(List<int[]> rows)
    {
        List<SpawnerRuleAllocation> result = new List<SpawnerRuleAllocation>();
        for (int i = 0; i < rows.Count; i++)
        {
            int[] row = rows[i];
            if (row.Length < 3)
            {
                continue;
            }

            result.Add(new SpawnerRuleAllocation(row[0], row[1], row[2]));
        }

        return result;
    }

    private static List<SpawnerRuleInfo> ParseSpawnerRuleInfoList(string jsonArrayText)
    {
        List<SpawnerRuleInfo> result = new List<SpawnerRuleInfo>();
        if (string.IsNullOrWhiteSpace(jsonArrayText))
        {
            return result;
        }

        int index = 0;
        SkipWhiteSpace(jsonArrayText, ref index);
        ExpectChar(jsonArrayText, ref index, '[');

        while (index < jsonArrayText.Length)
        {
            SkipWhiteSpace(jsonArrayText, ref index);
            if (TryConsumeChar(jsonArrayText, ref index, ']'))
            {
                break;
            }

            string objectJson = ExtractBalancedJson(jsonArrayText, index, '{', '}');
            if (string.IsNullOrEmpty(objectJson))
            {
                break;
            }

            index += objectJson.Length;

            SpawnerRuleInfo info = new SpawnerRuleInfo();
            info.index = ParseIntField(objectJson, "index", false);
            info.rule = ParseIntList(ExtractJsonValue(objectJson, "rule"));
            info.totalWeights = ParseIntField(objectJson, "totalWeights", false);
            result.Add(info);

            SkipWhiteSpace(jsonArrayText, ref index);
            TryConsumeChar(jsonArrayText, ref index, ',');
        }

        return result;
    }

    private static List<int> ParseIntList(string jsonArrayText)
    {
        List<int> result = new List<int>();
        if (string.IsNullOrWhiteSpace(jsonArrayText))
        {
            return result;
        }

        int index = 0;
        SkipWhiteSpace(jsonArrayText, ref index);
        ExpectChar(jsonArrayText, ref index, '[');

        while (index < jsonArrayText.Length)
        {
            SkipWhiteSpace(jsonArrayText, ref index);
            if (TryConsumeChar(jsonArrayText, ref index, ']'))
            {
                break;
            }

            int value = ParseIntToken(jsonArrayText, ref index);
            result.Add(value);

            SkipWhiteSpace(jsonArrayText, ref index);
            if (TryConsumeChar(jsonArrayText, ref index, ','))
            {
                continue;
            }

            if (TryConsumeChar(jsonArrayText, ref index, ']'))
            {
                break;
            }

            throw new FormatException("Invalid int list json format.");
        }

        return result;
    }

    private static List<int[]> ParseIntMatrix(string jsonArrayText)
    {
        List<int[]> result = new List<int[]>();
        if (string.IsNullOrWhiteSpace(jsonArrayText))
        {
            return result;
        }

        int index = 0;
        SkipWhiteSpace(jsonArrayText, ref index);
        ExpectChar(jsonArrayText, ref index, '[');

        while (index < jsonArrayText.Length)
        {
            SkipWhiteSpace(jsonArrayText, ref index);
            if (TryConsumeChar(jsonArrayText, ref index, ']'))
            {
                break;
            }

            int[] row = ParseIntArray(jsonArrayText, ref index);
            result.Add(row);

            SkipWhiteSpace(jsonArrayText, ref index);
            if (TryConsumeChar(jsonArrayText, ref index, ','))
            {
                continue;
            }

            if (TryConsumeChar(jsonArrayText, ref index, ']'))
            {
                break;
            }

            throw new FormatException("Invalid int matrix json format.");
        }

        return result;
    }

    private static int[] ParseIntArray(string text, ref int index)
    {
        List<int> numbers = new List<int>();
        SkipWhiteSpace(text, ref index);
        ExpectChar(text, ref index, '[');

        while (index < text.Length)
        {
            SkipWhiteSpace(text, ref index);
            if (TryConsumeChar(text, ref index, ']'))
            {
                break;
            }

            int value = ParseIntToken(text, ref index);
            numbers.Add(value);

            SkipWhiteSpace(text, ref index);
            if (TryConsumeChar(text, ref index, ','))
            {
                continue;
            }

            if (TryConsumeChar(text, ref index, ']'))
            {
                break;
            }

            throw new FormatException("Invalid int array json format.");
        }

        return numbers.ToArray();
    }

    private static int ParseIntField(string json, string key, bool required)
    {
        string rawValue = ExtractJsonValue(json, key);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            if (required)
            {
                throw new FormatException("Missing required field: " + key);
            }

            return 0;
        }

        if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw new FormatException("Invalid int field: " + key);
        }

        return value;
    }

    private static string ExtractJsonValue(string json, string key)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        string keyToken = "\"" + key + "\"";
        int keyIndex = json.IndexOf(keyToken, StringComparison.Ordinal);
        if (keyIndex < 0)
        {
            return null;
        }

        int colonIndex = json.IndexOf(':', keyIndex + keyToken.Length);
        if (colonIndex < 0)
        {
            return null;
        }

        int valueStart = colonIndex + 1;
        SkipWhiteSpace(json, ref valueStart);
        if (valueStart >= json.Length)
        {
            return null;
        }

        char first = json[valueStart];
        if (first == '[')
        {
            return ExtractBalancedJson(json, valueStart, '[', ']');
        }

        if (first == '{')
        {
            return ExtractBalancedJson(json, valueStart, '{', '}');
        }

        int valueEnd = valueStart;
        while (valueEnd < json.Length)
        {
            char c = json[valueEnd];
            if (c == ',' || c == '}' || c == ']')
            {
                break;
            }

            valueEnd++;
        }

        return json.Substring(valueStart, valueEnd - valueStart).Trim();
    }

    private static string ExtractBalancedJson(string text, int startIndex, char openChar, char closeChar)
    {
        int depth = 0;
        bool inString = false;
        bool escaped = false;

        for (int i = startIndex; i < text.Length; i++)
        {
            char c = text[i];

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == openChar)
            {
                depth++;
            }
            else if (c == closeChar)
            {
                depth--;
                if (depth == 0)
                {
                    return text.Substring(startIndex, i - startIndex + 1);
                }
            }
        }

        throw new FormatException("Unbalanced json block.");
    }

    private static int ParseIntToken(string text, ref int index)
    {
        SkipWhiteSpace(text, ref index);
        if (index >= text.Length)
        {
            throw new FormatException("Unexpected end when parsing int.");
        }

        int start = index;
        if (text[index] == '-')
        {
            index++;
        }

        while (index < text.Length && char.IsDigit(text[index]))
        {
            index++;
        }

        if (index == start || (index == start + 1 && text[start] == '-'))
        {
            throw new FormatException("Invalid int token.");
        }

        string token = text.Substring(start, index - start);
        if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw new FormatException("Invalid int token: " + token);
        }

        return value;
    }

    private static void SkipWhiteSpace(string text, ref int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }
    }

    private static void ExpectChar(string text, ref int index, char expected)
    {
        SkipWhiteSpace(text, ref index);
        if (index >= text.Length || text[index] != expected)
        {
            throw new FormatException("Expected character: " + expected);
        }

        index++;
    }

    private static bool TryConsumeChar(string text, ref int index, char expected)
    {
        SkipWhiteSpace(text, ref index);
        if (index < text.Length && text[index] == expected)
        {
            index++;
            return true;
        }

        return false;
    }
}

[Serializable]
public class TargetInfo
{
    public int targetType;
    public int targetCount;

    public TargetInfo(int targetType, int targetCount)
    {
        this.targetType = targetType;
        this.targetCount = targetCount;
    }
}

[Serializable]
public class SlotInfo
{
    public int x;
    public int y;
    public int slotType;
    public int spawnerFlag;

    public SlotInfo(int x, int y, int slotType, int spawnerFlag)
    {
        this.x = x;
        this.y = y;
        this.slotType = slotType;
        this.spawnerFlag = spawnerFlag;
    }
}

[Serializable]
public class BottomInfo
{
    public int x;
    public int y;
    public int bottomType;
    public int bottomParam;
    public int logicX;
    public int logicY;

    public BottomInfo(int x, int y, int bottomType, int bottomParam, int logicX, int logicY)
    {
        this.x = x;
        this.y = y;
        this.bottomType = bottomType;
        this.bottomParam = bottomParam;
        this.logicX = logicX;
        this.logicY = logicY;
    }
}

[Serializable]
public class PieceInfo
{
    public int x;
    public int y;
    public int pieceType;
    public int layer;
    public int logicX;
    public int logicY;
    public int color;

    public PieceInfo(int x, int y, int pieceType, int layer, int logicX, int logicY, int color)
    {
        this.x = x;
        this.y = y;
        this.pieceType = pieceType;
        this.layer = layer;
        this.logicX = logicX;
        this.logicY = logicY;
        this.color = color;
    }
}

[Serializable]
public class UpperInfo
{
    public int x;
    public int y;
    public int upperType;
    public int upperParam;
    public int logicX;
    public int logicY;

    public UpperInfo(int x, int y, int upperType, int upperParam, int logicX, int logicY)
    {
        this.x = x;
        this.y = y;
        this.upperType = upperType;
        this.upperParam = upperParam;
        this.logicX = logicX;
        this.logicY = logicY;
    }
}

[Serializable]
public class SpawnerRuleInfo
{
    public int index;
    public List<int> rule = new List<int>();
    public int totalWeights;
}

[Serializable]
public class SpawnerRuleAllocation
{
    public int x;
    public int y;
    public int ruleIndex;

    public SpawnerRuleAllocation(int x, int y, int ruleIndex)
    {
        this.x = x;
        this.y = y;
        this.ruleIndex = ruleIndex;
    }
}
