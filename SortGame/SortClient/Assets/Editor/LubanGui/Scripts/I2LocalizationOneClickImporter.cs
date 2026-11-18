using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using I2.Loc;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// 从 Profile 目录的 LocalizationConfig.xlsx 导出 UTF-8 CSV，并 Replace 导入到 I2 LanguageSource。
/// 与 Luban.Editor.LubanAfterGenI2Localization 配合：配表生成后一键刷新词条。
/// </summary>
/// <remarks>
/// <para><b>优先：</b>Python + openpyxl（<c>Tools/Localization/</c>，无需本机 Excel）。</para>
/// <para><b>回退：</b>PowerShell + Excel COM。</para>
/// <para>路径约定：相对 Unity 工程根，与 Luban.asset 的 <c>../SortProfile</c> 同级。</para>
/// </remarks>
public static class I2LocalizationOneClickImporter
{
    private const string MainXlsxRelativePath = "../SortProfile/LocalizationConfig.xlsx";
    private const string MainXlsxSheetName = "LocalizationConfig";

    private const string MainCsvRelativePath = "Assets/Resources/LocalizationConfig.csv";
    private const string MainLanguageAssetPath = "Assets/Resources/I2Languages.asset";

    private const char CsvSeparator = ',';
    private static readonly Encoding CsvEncoding = Encoding.UTF8;

    private const string PythonScriptRelativePath = "Tools/Localization/export_localization_csv.py";

    /// <summary>语言包导出并导入成功后触发（例如收集 TMP 字体）。</summary>
    public static Action OnLanguagePackageExported;

    [MenuItem("游戏工具/一键导出语言包")]
    public static void ExportSheetAndImportReplaceMenu()
    {
        ExportSheetAndImportReplace();
    }

    /// <summary>导出并导入语言包；成功时触发 <see cref="OnLanguagePackageExported"/>。</summary>
    public static bool ExportSheetAndImportReplace()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[I2] 无法定位 Unity 工程根目录");
            return false;
        }

        if (!ExportAndImportLanguagePackage(projectRoot, MainXlsxRelativePath, MainXlsxSheetName,
                MainCsvRelativePath, MainLanguageAssetPath))
        {
            return false;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[I2] 完成: 已刷新 {MainLanguageAssetPath}。");

        try
        {
            OnLanguagePackageExported?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[I2] 语言包导出后回调异常: {ex}");
            return false;
        }

        return true;
    }

    private static bool ExportAndImportLanguagePackage(
        string projectRoot,
        string xlsxRelativePath,
        string sheetName,
        string csvRelativePath,
        string languageAssetPath)
    {
        string xlsxPath = Path.GetFullPath(Path.Combine(projectRoot, xlsxRelativePath));
        string csvPath = Path.GetFullPath(Path.Combine(projectRoot, csvRelativePath));

        if (!File.Exists(xlsxPath))
        {
            Debug.LogError($"[I2] XLSX不存在: {xlsxPath}");
            return false;
        }

        if (!ExportXlsxSheetToUtf8Csv(projectRoot, xlsxPath, sheetName, csvPath))
            return false;

        var sourceAsset = AssetDatabase.LoadAssetAtPath<LanguageSourceAsset>(languageAssetPath);
        if (sourceAsset == null)
        {
            Debug.LogError($"[I2] 找不到语言资源: {languageAssetPath}");
            return false;
        }

        string csvContent = File.ReadAllText(csvPath, CsvEncoding);
        string error =
            sourceAsset.mSource.Import_CSV(string.Empty, csvContent, eSpreadsheetUpdateMode.Replace,
                CsvSeparator);

        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError($"[I2] 导入失败 ({languageAssetPath}): {error}");
            return false;
        }

        ApplyEscapedNewlinesFromPythonExportToTerms(sourceAsset);

        EditorUtility.SetDirty(sourceAsset);
        return true;
    }

    private static void ApplyEscapedNewlinesFromPythonExportToTerms(LanguageSourceAsset sourceAsset)
    {
        var src = sourceAsset.mSource;
        if (src?.mTerms == null)
            return;

        foreach (var term in src.mTerms)
        {
            if (term.Languages == null)
                continue;

            for (int i = 0; i < term.Languages.Length; i++)
            {
                if (string.IsNullOrEmpty(term.Languages[i]))
                    continue;

                term.Languages[i] = term.Languages[i].Replace("\\n", "\n");
            }
        }
    }

    private static bool ExportXlsxSheetToUtf8Csv(
        string projectRoot,
        string xlsxPath,
        string sheetName,
        string csvPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(csvPath) ?? string.Empty);

        if (TryExportViaPython(projectRoot, xlsxPath, sheetName, csvPath, out string pyLog))
        {
            Debug.Log($"[I2] 使用 Python 导出 CSV（无需 Excel）。{pyLog}");
            return true;
        }

        if (!string.IsNullOrEmpty(pyLog))
            Debug.LogWarning($"[I2] Python 导出不可用或失败，将尝试 Excel COM。\n{pyLog}");

        return ExportXlsxSheetToUtf8CsvViaExcelCom(xlsxPath, sheetName, csvPath);
    }

    private static bool TryExportViaPython(
        string projectRoot,
        string xlsxPath,
        string sheetName,
        string csvPath,
        out string log)
    {
        log = string.Empty;
        string script = Path.Combine(projectRoot, PythonScriptRelativePath);
        if (!File.Exists(script))
        {
            log = $"未找到脚本: {script}";
            return false;
        }

        string Q(string p) => $"\"{p}\"";
        string args = $"{Q(script)} --xlsx {Q(xlsxPath)} --sheet {Q(sheetName)} --out {Q(csvPath)}";

        var commands = new List<(string FileName, string Arguments)>
        {
            ("python", args),
            ("py", "-3 " + args),
            ("python3", args),
        };

        foreach (var (fileName, arguments) in commands)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = projectRoot,
                };

                using var process = Process.Start(psi);
                if (process == null)
                    continue;

                string stdOut = process.StandardOutput.ReadToEnd();
                string stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && File.Exists(csvPath))
                {
                    log = $"{fileName}: {(stdOut + stdErr).Trim()}";
                    return true;
                }

                log = $"{fileName} ExitCode={process.ExitCode}\n{stdErr}\n{stdOut}\n";
            }
            catch (Exception ex)
            {
                log += $"{fileName}: {ex.Message}\n";
            }
        }

        log += "提示: 请先安装 Python，并在工程根下执行: pip install -r Tools/Localization/requirements.txt";
        return false;
    }

    private static bool ExportXlsxSheetToUtf8CsvViaExcelCom(string xlsxPath, string sheetName, string csvPath)
    {
        string psScript = $@"
$ErrorActionPreference = 'Stop'
$xlsx = '{EscapeForSingleQuotedPs(xlsxPath)}'
$sheetName = '{EscapeForSingleQuotedPs(sheetName)}'
$csv = '{EscapeForSingleQuotedPs(csvPath)}'
$excel = New-Object -ComObject Excel.Application
$excel.DisplayAlerts = $false
$excel.Visible = $false
try {{
    $workbook = $excel.Workbooks.Open($xlsx)
    $sheet = $workbook.Worksheets.Item($sheetName)
    if ($null -eq $sheet) {{ throw ""Sheet not found: $sheetName"" }}
    $usedRange = $sheet.UsedRange
    $rowCount = $usedRange.Rows.Count
    $colCount = $usedRange.Columns.Count
    $lines = New-Object System.Collections.Generic.List[string]
    for ($r = 1; $r -le $rowCount; $r++) {{
        $cells = New-Object System.Collections.Generic.List[string]
        for ($c = 1; $c -le $colCount; $c++) {{
            $cellValue = $usedRange.Item($r, $c).Text
            if ($null -eq $cellValue) {{ $cellValue = '' }}
            $cellValue = [string]$cellValue
            $quoteStr = [string][char]34
            $quoteEscaped = $quoteStr + $quoteStr
            $needQuote = $cellValue.Contains(',') -or $cellValue.Contains($quoteStr) -or $cellValue.Contains([Environment]::NewLine) -or $cellValue.Contains('`n') -or $cellValue.Contains('`r')
            if ($cellValue.Contains($quoteStr)) {{
                $cellValue = $cellValue.Replace($quoteStr, $quoteEscaped)
            }}
            if ($needQuote) {{
                $cellValue = $quoteStr + $cellValue + $quoteStr
            }}
            $cells.Add($cellValue)
        }}
        $lines.Add([string]::Join(',', $cells))
    }}
    [System.IO.File]::WriteAllLines($csv, $lines, [System.Text.UTF8Encoding]::new($true))
    $workbook.Close($false)
}}
finally {{
    $excel.Quit()
}}
";

        var psi = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments =
                "-NoProfile -ExecutionPolicy Bypass -Command \"" + psScript.Replace("\"", "\\\"") + "\"",
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        try
        {
            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    Debug.LogError("[I2] 无法启动 powershell 导出 xlsx");
                    return false;
                }

                string stdOut = process.StandardOutput.ReadToEnd();
                string stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Debug.LogError($"[I2] Excel COM 导出失败，ExitCode={process.ExitCode}\n{stdErr}\n{stdOut}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[I2] xlsx导出csv异常: {ex.Message}");
            return false;
        }

        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[I2] 导出后未找到CSV: {csvPath}");
            return false;
        }

        return true;
    }

    private static string EscapeForSingleQuotedPs(string value)
    {
        return value.Replace("'", "''");
    }
}
