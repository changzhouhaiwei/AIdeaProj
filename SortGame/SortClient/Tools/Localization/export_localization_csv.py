#!/usr/bin/env python3
"""
将 LocalizationConfig.xlsx 指定工作表导出为 UTF-8（带 BOM）CSV，供 I2 Import_CSV(Replace) 使用。
无需安装 Microsoft Excel；依赖：pip install -r requirements.txt

换行处理（规避 RFC4180 多行字段整段加引号、CSV 物理行折断）：
- 将单元格内真实换行统一为 \\n 后，再写成「反斜杠 + 字母 n」两个字符写入 CSV。
- Unity 导入后在 I2LocalizationOneClickImporter 内写回真实换行并保存 I2Languages。
"""
from __future__ import annotations

import argparse
import csv
import sys
from pathlib import Path


def flatten_cell_for_i2_csv(value: object) -> str:
    """将单元格内真实换行替换为字面量反斜杠+n，便于 CSV 单行存储。"""
    if value is None:
        return ""
    s = str(value).replace("\r\n", "\n").replace("\r", "\n")
    return s.replace("\n", "\\n")


def main() -> int:
    p = argparse.ArgumentParser(description="Export one xlsx sheet to UTF-8-SIG CSV for I2 Localization.")
    p.add_argument("--xlsx", required=True, type=Path, help="Path to .xlsx")
    p.add_argument("--sheet", required=True, help="Worksheet name")
    p.add_argument("--out", required=True, type=Path, help="Output .csv path")
    args = p.parse_args()

    if not args.xlsx.is_file():
        print(f"[export_localization_csv] xlsx not found: {args.xlsx}", file=sys.stderr)
        return 2

    try:
        from openpyxl import load_workbook
    except ImportError:
        print(
            "[export_localization_csv] openpyxl not installed. Run: pip install -r requirements.txt",
            file=sys.stderr,
        )
        return 3

    wb = load_workbook(filename=str(args.xlsx), read_only=False, data_only=True)
    if args.sheet not in wb.sheetnames:
        print(f"[export_localization_csv] sheet not found: {args.sheet!r}", file=sys.stderr)
        print(f"[export_localization_csv] available: {wb.sheetnames}", file=sys.stderr)
        wb.close()
        return 4

    ws = wb[args.sheet]
    args.out.parent.mkdir(parents=True, exist_ok=True)

    # utf-8-sig: BOM + UTF-8，与原先 C# UTF8Encoding(true) 写入的 CSV 一致，便于 I2/编辑器识别
    with open(args.out, "w", encoding="utf-8-sig", newline="") as f:
        writer = csv.writer(f)
        for row in ws.iter_rows(
            min_row=1,
            max_row=ws.max_row or 1,
            min_col=1,
            max_col=ws.max_column or 1,
            values_only=True,
        ):
            writer.writerow([flatten_cell_for_i2_csv(c) for c in row])

    wb.close()
    print(f"[export_localization_csv] wrote {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
