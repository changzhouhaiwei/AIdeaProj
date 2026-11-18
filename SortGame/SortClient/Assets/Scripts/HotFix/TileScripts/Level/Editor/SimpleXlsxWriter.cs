#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace FishTilePlay
{
    internal static class SimpleXlsxWriter
    {
        internal const int AlertStyle = 3;

        public readonly struct Sheet
        {
            public readonly string Name;
            public readonly IList<IList<string>> Rows;
            public readonly IList<IList<int>> CellStyles;

            public Sheet(string name, IList<IList<string>> rows)
                : this(name, rows, null)
            {
            }

            public Sheet(string name, IList<IList<string>> rows, IList<IList<int>> cellStyles)
            {
                Name = name;
                Rows = rows;
                CellStyles = cellStyles;
            }
        }

        public static void Write(string filePath, IList<IList<string>> rows)
        {
            WriteSheets(filePath, new[] { new Sheet("LevelLayerTileCount", rows) });
        }

        public static void WriteSheets(string filePath, IList<Sheet> sheets)
        {
            if (sheets == null || sheets.Count == 0)
            {
                throw new ArgumentException("At least one sheet is required.", nameof(sheets));
            }

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using FileStream stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create);

            WriteEntry(archive, "[Content_Types].xml", GetContentTypesXml(sheets.Count));
            WriteEntry(archive, "_rels/.rels", GetRootRelsXml());
            WriteEntry(archive, "docProps/core.xml", GetCorePropsXml());
            WriteEntry(archive, "docProps/app.xml", GetAppPropsXml());
            WriteEntry(archive, "xl/workbook.xml", GetWorkbookXml(sheets));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", GetWorkbookRelsXml(sheets.Count));
            WriteEntry(archive, "xl/styles.xml", GetStylesXml());
            for (int i = 0; i < sheets.Count; i++)
            {
                WriteEntry(archive, $"xl/worksheets/sheet{i + 1}.xml", GetSheetXmlBytes(sheets[i].Rows, sheets[i].CellStyles));
            }
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            WriteEntry(archive, name, Encoding.UTF8.GetBytes(content));
        }

        private static void WriteEntry(ZipArchive archive, string name, byte[] content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using Stream stream = entry.Open();
            stream.Write(content, 0, content.Length);
        }

        private static string GetContentTypesXml(int sheetCount)
        {
            StringBuilder sheetOverrides = new StringBuilder();
            for (int i = 1; i <= sheetCount; i++)
            {
                sheetOverrides.Append("  <Override PartName=\"/xl/worksheets/sheet");
                sheetOverrides.Append(i);
                sheetOverrides.Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>\n");
            }

            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml"" ContentType=""application/xml""/>
  <Override PartName=""/docProps/core.xml"" ContentType=""application/vnd.openxmlformats-package.core-properties+xml""/>
  <Override PartName=""/docProps/app.xml"" ContentType=""application/vnd.openxmlformats-officedocument.extended-properties+xml""/>
  <Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
" + sheetOverrides + @"  <Override PartName=""/xl/styles.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>";
        }

        private static string GetRootRelsXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"" Target=""docProps/core.xml""/>
  <Relationship Id=""rId3"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties"" Target=""docProps/app.xml""/>
</Relationships>";
        }

        private static string GetCorePropsXml()
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            return $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<cp:coreProperties xmlns:cp=""http://schemas.openxmlformats.org/package/2006/metadata/core-properties"" xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:dcterms=""http://purl.org/dc/terms/"" xmlns:dcmitype=""http://purl.org/dc/dcmitype/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <dc:creator>Tile2Client</dc:creator>
  <dc:title>LevelLayerTileCount</dc:title>
  <dcterms:created xsi:type=""dcterms:W3CDTF"">{timestamp}</dcterms:created>
  <dcterms:modified xsi:type=""dcterms:W3CDTF"">{timestamp}</dcterms:modified>
</cp:coreProperties>";
        }

        private static string GetAppPropsXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Properties xmlns=""http://schemas.openxmlformats.org/officeDocument/2006/extended-properties"" xmlns:vt=""http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes"">
  <Application>Tile2Client</Application>
</Properties>";
        }

        private static string GetWorkbookXml(IList<Sheet> sheets)
        {
            StringBuilder sheetXml = new StringBuilder();
            for (int i = 0; i < sheets.Count; i++)
            {
                sheetXml.Append("    <sheet name=\"");
                sheetXml.Append(XmlEscape(sheets[i].Name));
                sheetXml.Append("\" sheetId=\"");
                sheetXml.Append(i + 1);
                sheetXml.Append("\" r:id=\"rId");
                sheetXml.Append(i + 1);
                sheetXml.Append("\"/>\n");
            }

            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <bookViews>
    <workbookView xWindow=""0"" yWindow=""0"" windowWidth=""28800"" windowHeight=""14400""/>
  </bookViews>
  <sheets>
" + sheetXml + @"  </sheets>
</workbook>";
        }

        private static string GetWorkbookRelsXml(int sheetCount)
        {
            StringBuilder rels = new StringBuilder();
            for (int i = 1; i <= sheetCount; i++)
            {
                rels.Append("  <Relationship Id=\"rId");
                rels.Append(i);
                rels.Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet");
                rels.Append(i);
                rels.Append(".xml\"/>\n");
            }

            rels.Append("  <Relationship Id=\"rId");
            rels.Append(sheetCount + 1);
            rels.Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>\n");

            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
" + rels + @"</Relationships>";
        }

        private static string XmlEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private const int HeaderStyle = 1;
        private const int BodyStyle = 2;
        private const double MinColumnWidth = 12;
        private const double MaxColumnWidth = 46;
        private const double HeaderRowHeight = 22;
        private const double LineHeight = 16;

        private static string GetStylesXml()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""3"">
    <font>
      <sz val=""11""/>
      <color theme=""1""/>
      <name val=""Calibri""/>
      <family val=""2""/>
      <scheme val=""minor""/>
    </font>
    <font>
      <b/>
      <sz val=""11""/>
      <color rgb=""FFFFFFFF""/>
      <name val=""Calibri""/>
      <family val=""2""/>
      <scheme val=""minor""/>
    </font>
    <font>
      <sz val=""11""/>
      <color rgb=""FFFF0000""/>
      <name val=""Calibri""/>
      <family val=""2""/>
      <scheme val=""minor""/>
    </font>
  </fonts>
  <fills count=""3"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill>
      <patternFill patternType=""solid"">
        <fgColor rgb=""FF2F75B6""/>
        <bgColor indexed=""64""/>
      </patternFill>
    </fill>
  </fills>
  <borders count=""1"">
    <border>
      <left/><right/><top/><bottom/><diagonal/>
    </border>
  </borders>
  <cellStyleXfs count=""1"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/>
  </cellStyleXfs>
  <cellXfs count=""4"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyAlignment=""1"">
      <alignment horizontal=""right"" vertical=""center"" wrapText=""1""/>
    </xf>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0"" applyAlignment=""1"">
      <alignment horizontal=""right"" vertical=""top"" wrapText=""1""/>
    </xf>
    <xf numFmtId=""0"" fontId=""2"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1"" applyAlignment=""1"">
      <alignment horizontal=""right"" vertical=""top"" wrapText=""1""/>
    </xf>
  </cellXfs>
  <cellStyles count=""1"">
    <cellStyle name=""Normal"" xfId=""0"" builtinId=""0""/>
  </cellStyles>
</styleSheet>";
        }

        private static byte[] GetSheetXmlBytes(IList<IList<string>> rows, IList<IList<int>> cellStyles)
        {
            XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

            int rowCount = rows.Count;
            int colCount = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null && rows[i].Count > colCount)
                {
                    colCount = rows[i].Count;
                }
            }

            if (colCount == 0)
            {
                colCount = 1;
            }

            string dimensionRef = rowCount > 0
                ? $"A1:{GetColumnName(colCount - 1)}{rowCount}"
                : "A1:A1";
            double[] columnWidths = FitColumnWidths(rows, colCount);

            XElement cols = new XElement(main + "cols");
            for (int colIndex = 0; colIndex < colCount; colIndex++)
            {
                cols.Add(new XElement(main + "col",
                    new XAttribute("min", colIndex + 1),
                    new XAttribute("max", colIndex + 1),
                    new XAttribute("width", columnWidths[colIndex].ToString("0.0", CultureInfo.InvariantCulture)),
                    new XAttribute("customWidth", "1")));
            }

            XElement sheetData = new XElement(main + "sheetData");

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                IList<string> row = rows[rowIndex];
                if (row == null)
                {
                    continue;
                }

                bool header = rowIndex == 0;
                int excelRow = rowIndex + 1;
                double rowHeight = FitRowHeight(row, columnWidths, header);
                XElement rowElement = new XElement(main + "row",
                    new XAttribute("r", excelRow),
                    new XAttribute("spans", $"1:{colCount}"),
                    new XAttribute("ht", rowHeight.ToString("0.0", CultureInfo.InvariantCulture)),
                    new XAttribute("customHeight", "1"));

                int style = header ? HeaderStyle : BodyStyle;
                IList<int> styleRow = !header && cellStyles != null && rowIndex < cellStyles.Count
                    ? cellStyles[rowIndex]
                    : null;
                for (int colIndex = 0; colIndex < row.Count; colIndex++)
                {
                    string value = row[colIndex] ?? string.Empty;
                    if (value.Length == 0)
                    {
                        continue;
                    }

                    int cellStyle = style;
                    if (styleRow != null && colIndex < styleRow.Count && styleRow[colIndex] >= 0)
                    {
                        cellStyle = styleRow[colIndex];
                    }

                    string cellRef = GetCellReference(colIndex, excelRow);
                    rowElement.Add(CreateCell(main, cellRef, value, cellStyle));
                }

                sheetData.Add(rowElement);
            }

            XElement worksheet = new XElement(main + "worksheet",
                new XAttribute(XNamespace.Xmlns + "r", r),
                new XElement(main + "dimension", new XAttribute("ref", dimensionRef)),
                new XElement(main + "sheetViews",
                    new XElement(main + "sheetView",
                        new XAttribute("workbookViewId", "0"),
                        new XElement(main + "pane",
                            new XAttribute("ySplit", "1"),
                            new XAttribute("topLeftCell", "A2"),
                            new XAttribute("activePane", "bottomLeft"),
                            new XAttribute("state", "frozen")),
                        new XElement(main + "selection",
                            new XAttribute("pane", "bottomLeft"),
                            new XAttribute("activeCell", "A2"),
                            new XAttribute("sqref", "A2")))),
                new XElement(main + "sheetFormatPr", new XAttribute("defaultRowHeight", "18")),
                cols,
                sheetData,
                new XElement(main + "autoFilter", new XAttribute("ref", dimensionRef)),
                new XElement(main + "pageMargins",
                    new XAttribute("left", "0.7"),
                    new XAttribute("right", "0.7"),
                    new XAttribute("top", "0.75"),
                    new XAttribute("bottom", "0.75"),
                    new XAttribute("header", "0.3"),
                    new XAttribute("footer", "0.3")));

            XDocument document = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                worksheet);

            // StringWriter is UTF-16; Excel rejects a utf-16 declaration over UTF-8 zip bytes. WPS is lenient.
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = false,
                Indent = false,
                CloseOutput = false
            };

            using MemoryStream memoryStream = new MemoryStream();
            using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, settings))
            {
                document.Save(xmlWriter);
            }

            byte[] bytes = memoryStream.ToArray();
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                byte[] withoutBom = new byte[bytes.Length - 3];
                Buffer.BlockCopy(bytes, 3, withoutBom, 0, withoutBom.Length);
                return withoutBom;
            }

            return bytes;
        }

        private static double[] FitColumnWidths(IList<IList<string>> rows, int colCount)
        {
            double[] widths = new double[colCount];
            for (int colIndex = 0; colIndex < colCount; colIndex++)
            {
                widths[colIndex] = MinColumnWidth;
            }

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                IList<string> row = rows[rowIndex];
                if (row == null)
                {
                    continue;
                }

                for (int colIndex = 0; colIndex < row.Count && colIndex < colCount; colIndex++)
                {
                    double width = MeasureText(row[colIndex]) + 2.5;
                    if (width > MaxColumnWidth)
                    {
                        width = MaxColumnWidth;
                    }

                    if (width > widths[colIndex])
                    {
                        widths[colIndex] = width;
                    }
                }
            }

            return widths;
        }

        private static double FitRowHeight(IList<string> row, double[] columnWidths, bool header)
        {
            if (header)
            {
                return HeaderRowHeight;
            }

            int lines = 1;
            if (row != null)
            {
                for (int colIndex = 0; colIndex < row.Count && colIndex < columnWidths.Length; colIndex++)
                {
                    int cellLines = CountWrappedLines(row[colIndex], columnWidths[colIndex]);
                    if (cellLines > lines)
                    {
                        lines = cellLines;
                    }
                }
            }

            double height = lines * LineHeight + 4;
            if (height < 18)
            {
                height = 18;
            }

            if (height > 409)
            {
                height = 409;
            }

            return height;
        }

        private static int CountWrappedLines(string text, double columnWidth)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 1;
            }

            double usable = columnWidth - 1.5;
            if (usable < 1)
            {
                usable = 1;
            }

            int lines = 1;
            double current = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n' || c == '\r')
                {
                    lines++;
                    current = 0;
                    continue;
                }

                double charWidth = c > 255 ? 2.1 : 1.05;
                if (current > 0 && current + charWidth > usable)
                {
                    lines++;
                    current = charWidth;
                }
                else
                {
                    current += charWidth;
                }
            }

            return lines;
        }

        private static double MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            double max = 0;
            double current = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\n' || c == '\r')
                {
                    if (current > max)
                    {
                        max = current;
                    }

                    current = 0;
                    continue;
                }

                current += c > 255 ? 2.1 : 1.05;
            }

            return current > max ? current : max;
        }

        private static XElement CreateCell(XNamespace main, string cellRef, string value, int styleIndex)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
            {
                return new XElement(main + "c",
                    new XAttribute("r", cellRef),
                    new XAttribute("s", styleIndex),
                    new XElement(main + "v", number.ToString(CultureInfo.InvariantCulture)));
            }

            return new XElement(main + "c",
                new XAttribute("r", cellRef),
                new XAttribute("s", styleIndex),
                new XAttribute("t", "inlineStr"),
                new XElement(main + "is",
                    new XElement(main + "t",
                        new XAttribute(XNamespace.Xml + "space", "preserve"),
                        new XText(value))));
        }

        private static string GetCellReference(int columnIndex, int rowIndex)
        {
            return GetColumnName(columnIndex) + rowIndex;
        }

        private static string GetColumnName(int columnIndex)
        {
            int dividend = columnIndex + 1;
            string columnName = string.Empty;

            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = (char)('A' + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }
    }
}
#endif
