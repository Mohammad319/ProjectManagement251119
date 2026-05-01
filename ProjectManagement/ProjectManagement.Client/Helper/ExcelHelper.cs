using ClosedXML.Excel;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ProjectManagement.Client.Helper
{
    public sealed class ExcelWorksheetOption
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName => $"{Index}. {Name}";
    }

    public sealed class ExcelImportLayout
    {
        public int SheetIndex { get; set; } = 1;
        public string SheetName { get; set; } = string.Empty;
        public int RowStart { get; set; } = 1;
        public int CodeIndex { get; set; } = 1;
        public int NameIndex { get; set; } = 2;
        public int UnitIndex { get; set; } = 4;
        public int QuantityIndex { get; set; } = 5;
        public int PriceIndex { get; set; } = 6;
        public int Score { get; set; }
        public bool IsDetected => Score > 0;
    }

    public class ExcelHelper
    {
        static readonly HashSet<string> KnownUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            "-", "%", "st", "stk", "pcs", "pc", "ea", "kpl", "m", "m1", "m2", "m3",
            "cm", "mm", "km", "kg", "g", "ton", "t", "l", "liter", "h", "hr", "tim", "timme",
            "timmar", "dag", "dygn", "sek", "kr", "man", "month"
        };

        static readonly HashSet<string> HeaderWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "code", "kod", "nr", "nummer", "name", "namn", "text", "benamning", "beskrivning",
            "unit", "enhet", "quantity", "qty", "antal", "mangd", "price", "pris", "apris", "belopp"
        };

        public static List<ExcelWorksheetOption> GetWorksheetOptions(XLWorkbook workbook)
        {
            List<ExcelWorksheetOption> sheets = new();
            int index = 1;

            foreach (IXLWorksheet worksheet in workbook.Worksheets)
            {
                sheets.Add(new ExcelWorksheetOption
                {
                    Index = index,
                    Name = worksheet.Name
                });
                index++;
            }

            return sheets;
        }

        public static ExcelImportLayout DetectLayout(XLWorkbook workbook)
        {
            ExcelImportLayout? best = null;
            int index = 1;

            foreach (IXLWorksheet worksheet in workbook.Worksheets)
            {
                ExcelImportLayout current = DetectLayout(worksheet, index);
                if (best == null || current.Score > best.Score)
                    best = current;

                index++;
            }

            return best ?? new ExcelImportLayout();
        }

        public static ExcelImportLayout DetectLayout(XLWorkbook workbook, int sheetNr)
        {
            if (!workbook.Worksheets.Any())
                return new ExcelImportLayout();

            int safeSheetNr = Math.Clamp(sheetNr, 1, workbook.Worksheets.Count);
            return DetectLayout(workbook.Worksheet(safeSheetNr), safeSheetNr);
        }

        public static List<TaskPostDTO> Import(int sheetNr, XLWorkbook workbook, int rowStart,
            int codeIndex, int nameIndex, int unitIndex, int quantityIndex, int priceIndex, bool isOH)
        {
            if (!workbook.Worksheets.Any())
                return new List<TaskPostDTO>();

            int safeSheetNr = Math.Clamp(sheetNr, 1, workbook.Worksheets.Count);
            codeIndex = Math.Max(codeIndex, 1);
            nameIndex = Math.Max(nameIndex, 1);
            unitIndex = Math.Max(unitIndex, 1);
            quantityIndex = Math.Max(quantityIndex, 1);
            priceIndex = Math.Max(priceIndex, 1);
            rowStart = Math.Max(rowStart, 1);

            int belopIndex = Math.Max(priceIndex, codeIndex + 7);
            List<TaskPostDTO> sections = new();
            IXLWorksheet sheet = workbook.Worksheet(safeSheetNr);

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int row = rowStart; row <= lastRow; row++)
            {
                if (IsImportRowEmpty(sheet, row, codeIndex, nameIndex, unitIndex, quantityIndex, priceIndex))
                    continue;

                string code = CellText(sheet, row, codeIndex);
                string name = CellText(sheet, row, nameIndex);

                if (IsHeaderWord(code) && IsHeaderWord(name))
                    continue;

                if (!string.IsNullOrWhiteSpace(name))
                {
                    string unit = CellText(sheet, row, unitIndex);
                    string quantityStr = CellText(sheet, row, quantityIndex);
                    string priceStr = CellText(sheet, row, priceIndex);
                    string belopStr = CellText(sheet, row, belopIndex);

                    if (LooksLikeNumber(unit) && IsLikelyUnit(quantityStr))
                        (unit, quantityStr) = (quantityStr, unit);

                    string importedName = LimitText(name, FieldLengths.TaskName);
                    string importedCode = LimitText(code, FieldLengths.Code);
                    string importedUnit = LimitText(unit, FieldLengths.Unit);

                    var task = new TaskPostDTO
                    {
                        Name = !string.IsNullOrWhiteSpace(importedName) ? importedName : "Task",
                        Tasks = new(),
                        Colspan = false,
                        Metadata = new TaskMetadata
                        {
                            Code = importedCode,
                            Unit = importedUnit,
                            Type = TaskType.Task,
                            IsOH = isOH
                        }
                    };

                    if (name.Length > FieldLengths.TaskName)
                        task.Metadata.Note = LimitText(name, FieldLengths.Note);

                    if (string.IsNullOrWhiteSpace(unit) && string.IsNullOrWhiteSpace(priceStr) && string.IsNullOrWhiteSpace(quantityStr))
                    {
                        task.Metadata.Type = TaskType.CodeName;
                        task.Metadata.Quantity = null;
                    }
                    else if (unit == "-" && quantityStr == "-" && priceStr == "-")
                    {
                        task.Metadata.Type = TaskType.Minus;
                        task.Metadata.Quantity = belopStr == "-" ? 0 : 1;
                    }
                    else
                    {
                        task.Metadata.QuantityParam = ConstValues.FixedQ;
                        _ = TryReadDecimal(quantityStr, out decimal quantity);
                        _ = TryReadDecimal(priceStr, out decimal price);
                        task.Metadata.Quantity = quantity;
                        task.Metadata.PriceSubDB = price;
                    }

                    if (!string.IsNullOrWhiteSpace(task.Name))
                        sections.Add(task);
                }
            }

            ReSort(sections);
            return sections;
        }

        static ExcelImportLayout DetectLayout(IXLWorksheet sheet, int sheetIndex)
        {
            ExcelImportLayout fallback = new()
            {
                SheetIndex = sheetIndex,
                SheetName = sheet.Name
            };

            IXLRange? range = sheet.RangeUsed();
            if (range == null)
                return fallback;

            int firstRow = range.RangeAddress.FirstAddress.RowNumber;
            int lastRow = range.RangeAddress.LastAddress.RowNumber;
            int firstColumn = range.RangeAddress.FirstAddress.ColumnNumber;
            int lastColumn = range.RangeAddress.LastAddress.ColumnNumber;

            List<ColumnProfile> profiles = BuildColumnProfiles(sheet, firstRow, lastRow, firstColumn, lastColumn);
            if (profiles.Count == 0)
                return fallback;

            HeaderLayout? header = DetectHeaderLayout(sheet, firstRow, lastRow, firstColumn, lastColumn);

            int nameIndex = header?.NameIndex ?? DetectNameColumn(profiles, firstColumn);
            int codeIndex = header?.CodeIndex ?? DetectCodeColumn(profiles, nameIndex);
            int unitIndex = header?.UnitIndex ?? DetectUnitColumn(profiles, nameIndex);
            int quantityIndex = header?.QuantityIndex ?? DetectQuantityColumn(profiles, nameIndex, unitIndex);
            int priceIndex = header?.PriceIndex ?? DetectPriceColumn(profiles, unitIndex, quantityIndex);

            ExcelImportLayout layout = new()
            {
                SheetIndex = sheetIndex,
                SheetName = sheet.Name,
                CodeIndex = codeIndex,
                NameIndex = nameIndex,
                UnitIndex = unitIndex,
                QuantityIndex = quantityIndex,
                PriceIndex = priceIndex,
            };

            int startSearchRow = header == null ? firstRow : Math.Min(header.RowNumber + 1, lastRow);
            layout.RowStart = DetectStartRow(sheet, startSearchRow, lastRow, layout);
            layout.Score = ScoreLayout(sheet, firstRow, lastRow, layout) + (header?.Score ?? 0);

            return layout;
        }

        static HeaderLayout? DetectHeaderLayout(IXLWorksheet sheet, int firstRow, int lastRow, int firstColumn, int lastColumn)
        {
            HeaderLayout? best = null;
            int maxHeaderRow = Math.Min(lastRow, firstRow + 100);

            for (int row = firstRow; row <= maxHeaderRow; row++)
            {
                HeaderLayout current = new() { RowNumber = row };
                int? amountIndex = null;

                for (int column = firstColumn; column <= lastColumn; column++)
                {
                    string key = TextKey(CellText(sheet, row, column));
                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    if (current.CodeIndex == null && IsCodeHeader(key))
                    {
                        current.CodeIndex = column;
                        current.Matches++;
                    }
                    else if (current.NameIndex == null && IsNameHeader(key))
                    {
                        current.NameIndex = column;
                        current.Matches++;
                    }
                    else if (current.UnitIndex == null && IsUnitHeader(key))
                    {
                        current.UnitIndex = column;
                        current.Matches++;
                    }
                    else if (current.QuantityIndex == null && IsQuantityHeader(key))
                    {
                        current.QuantityIndex = column;
                        current.Matches++;
                    }
                    else if (current.PriceIndex == null && IsPriceHeader(key))
                    {
                        current.PriceIndex = column;
                        current.Matches++;
                    }
                    else if (amountIndex == null && IsAmountHeader(key))
                    {
                        amountIndex = column;
                        current.Matches++;
                    }
                }

                current.PriceIndex ??= amountIndex;

                if (current.NameIndex == null || current.Matches < 2)
                    continue;

                current.Score = (current.Matches * 25) - row;
                if (best == null || current.Score > best.Score)
                    best = current;
            }

            return best;
        }

        static List<ColumnProfile> BuildColumnProfiles(IXLWorksheet sheet, int firstRow, int lastRow, int firstColumn, int lastColumn)
        {
            List<ColumnProfile> profiles = new();

            for (int column = firstColumn; column <= lastColumn; column++)
            {
                ColumnProfile profile = new(column);

                for (int row = firstRow; row <= lastRow; row++)
                {
                    string text = CellText(sheet, row, column);
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    profile.NonEmptyCount++;

                    if (LooksLikeNumber(text))
                        profile.NumericCount++;
                    else if (IsLikelyUnit(text))
                        profile.UnitLikeCount++;
                    else if (IsLikelyCode(text))
                        profile.CodeLikeCount++;

                    if (IsMeaningfulName(text))
                    {
                        profile.TextCount++;
                        if (text.Length >= 8)
                            profile.LongTextCount++;
                    }
                }

                if (profile.NonEmptyCount > 0)
                    profiles.Add(profile);
            }

            return profiles;
        }

        static int DetectNameColumn(List<ColumnProfile> profiles, int firstColumn)
        {
            ColumnProfile? profile = profiles
                .Where(x => x.TextCount > 0)
                .OrderByDescending(x => (x.LongTextCount * 5) + (x.TextCount * 2) - (x.UnitLikeCount * 2) - x.NumericCount)
                .ThenBy(x => Math.Abs(x.ColumnNumber - firstColumn))
                .FirstOrDefault();

            return profile?.ColumnNumber ?? Math.Max(firstColumn, 2);
        }

        static int DetectCodeColumn(List<ColumnProfile> profiles, int nameIndex)
        {
            ColumnProfile? profile = profiles
                .Where(x => x.ColumnNumber < nameIndex)
                .OrderByDescending(x => (x.CodeLikeCount * 6) + x.NonEmptyCount - Math.Abs(nameIndex - x.ColumnNumber))
                .FirstOrDefault(x => x.CodeLikeCount > 0);

            return profile?.ColumnNumber ?? Math.Max(1, nameIndex - 1);
        }

        static int DetectUnitColumn(List<ColumnProfile> profiles, int nameIndex)
        {
            ColumnProfile? profile = profiles
                .Where(x => x.ColumnNumber > nameIndex && x.ColumnNumber <= nameIndex + 10)
                .OrderByDescending(x => (x.UnitLikeCount * 6) - (x.NumericCount * 2) - Math.Abs(x.ColumnNumber - nameIndex))
                .FirstOrDefault(x => x.UnitLikeCount > 0);

            return profile?.ColumnNumber ?? nameIndex + 2;
        }

        static int DetectQuantityColumn(List<ColumnProfile> profiles, int nameIndex, int unitIndex)
        {
            IEnumerable<ColumnProfile> candidates = profiles
                .Where(x => x.ColumnNumber > nameIndex && x.ColumnNumber <= nameIndex + 10 && x.ColumnNumber != unitIndex && x.NumericCount > 0);

            ColumnProfile? closestToUnit = candidates
                .OrderBy(x => Math.Abs(x.ColumnNumber - unitIndex))
                .ThenByDescending(x => x.NumericCount)
                .FirstOrDefault();

            if (closestToUnit != null)
                return closestToUnit.ColumnNumber;

            return unitIndex + 1;
        }

        static int DetectPriceColumn(List<ColumnProfile> profiles, int unitIndex, int quantityIndex)
        {
            int after = Math.Max(unitIndex, quantityIndex);

            ColumnProfile? profile = profiles
                .Where(x => x.ColumnNumber > after && x.ColumnNumber <= after + 8 && x.NumericCount > 0)
                .OrderBy(x => x.ColumnNumber)
                .FirstOrDefault();

            return profile?.ColumnNumber ?? after + 1;
        }

        static int DetectStartRow(IXLWorksheet sheet, int firstRow, int lastRow, ExcelImportLayout layout)
        {
            for (int row = firstRow; row <= lastRow; row++)
            {
                string code = CellText(sheet, row, layout.CodeIndex);
                string name = CellText(sheet, row, layout.NameIndex);

                if (IsLikelyCode(code) && IsMeaningfulName(name))
                    return row;
            }

            for (int row = firstRow; row <= lastRow; row++)
            {
                if (ScoreRow(sheet, row, layout) >= 6)
                    return row;
            }

            return firstRow;
        }

        static int ScoreLayout(IXLWorksheet sheet, int firstRow, int lastRow, ExcelImportLayout layout)
        {
            int score = 0;

            for (int row = firstRow; row <= lastRow; row++)
            {
                int rowScore = ScoreRow(sheet, row, layout);
                if (rowScore > 0)
                    score += rowScore;
            }

            return score;
        }

        static int ScoreRow(IXLWorksheet sheet, int row, ExcelImportLayout layout)
        {
            string code = CellText(sheet, row, layout.CodeIndex);
            string name = CellText(sheet, row, layout.NameIndex);
            string unit = CellText(sheet, row, layout.UnitIndex);
            string quantity = CellText(sheet, row, layout.QuantityIndex);
            string price = CellText(sheet, row, layout.PriceIndex);

            if (!IsMeaningfulName(name))
                return 0;

            int score = 2;

            if (IsLikelyCode(code))
                score += 6;

            if (IsLikelyUnit(unit))
                score += 3;

            if (LooksLikeNumber(quantity))
                score += 3;

            if (LooksLikeNumber(unit) && IsLikelyUnit(quantity))
                score += 4;

            if (LooksLikeNumber(price))
                score += 1;

            return score;
        }

        static bool IsImportRowEmpty(IXLWorksheet sheet, int row, params int[] columns)
        {
            foreach (int column in columns.Distinct())
            {
                if (!string.IsNullOrWhiteSpace(CellText(sheet, row, column)))
                    return false;
            }

            return sheet.Row(row).IsEmpty();
        }

        static string CellText(IXLWorksheet sheet, int row, int column)
        {
            if (row < 1 || column < 1)
                return string.Empty;

            return (sheet.Cell(row, column).GetFormattedString() ?? string.Empty).Trim();
        }

        static bool IsMeaningfulName(string text)
        {
            text = NormalizeText(text);
            if (text.Length < 3)
                return false;

            if (IsHeaderWord(text))
                return false;

            return !LooksLikeNumber(text) && !IsLikelyUnit(text);
        }

        static bool IsLikelyCode(string text)
        {
            text = NormalizeText(text);
            if (string.IsNullOrWhiteSpace(text) || text.Length > 35 || IsLikelyUnit(text))
                return false;

            if (IsHeaderWord(text))
                return false;

            int whitespaceCount = text.Count(char.IsWhiteSpace);
            if (whitespaceCount > 1)
                return false;

            if (LooksLikeNumber(text))
                return true;

            bool hasDigit = text.Any(char.IsDigit);
            bool hasLetter = text.Any(char.IsLetter);
            bool hasSeparator = text.Any(x => x is '.' or '-' or '_' or '/' or '\\');
            bool allCodeChars = text.All(x => char.IsLetterOrDigit(x) || x is '.' or '-' or '_' or '/' or '\\');
            bool allLettersUpper = text.Where(char.IsLetter).All(char.IsUpper);

            if (hasLetter && allCodeChars && text.Length <= 20 && (allLettersUpper || hasDigit || hasSeparator))
                return true;

            return hasDigit && (hasLetter || hasSeparator);
        }

        static bool IsLikelyUnit(string text)
        {
            text = NormalizeText(text);
            if (string.IsNullOrWhiteSpace(text) || LooksLikeNumber(text))
                return false;

            string key = TextKey(text);
            if (KnownUnits.Contains(key))
                return true;

            if (!text.Any(char.IsLower))
                return false;

            if (text.Contains(':') || text.Contains(';') || text.Contains(',') || text.Length > 8)
                return false;

            int letterCount = key.Count(char.IsLetter);
            int digitCount = key.Count(char.IsDigit);

            return letterCount > 0 && digitCount <= 2 && key.Length <= 8;
        }

        static bool LooksLikeNumber(string text) => TryReadDecimal(text, out _);

        static string LimitText(string text, int maxLength)
        {
            text = NormalizeText(text);
            return text.Length <= maxLength ? text : text[..maxLength];
        }

        static bool TryReadDecimal(string text, out decimal value)
        {
            text = NormalizeText(text).Replace(" ", string.Empty);

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
                return true;

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                return true;

            CultureInfo sv = CultureInfo.GetCultureInfo("sv-SE");
            if (decimal.TryParse(text, NumberStyles.Number, sv, out value))
                return true;

            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("en-US"), out value);
        }

        static bool IsHeaderWord(string text) => HeaderWords.Contains(TextKey(text));
        static bool IsCodeHeader(string key) => key is "kod" or "code" or "nr" or "nummer";
        static bool IsNameHeader(string key) => key is "text" or "namn" or "name" or "benamning" or "beskrivning";
        static bool IsUnitHeader(string key) => key is "enhet" or "unit";
        static bool IsQuantityHeader(string key) => key is "mangd" or "quantity" or "qty" or "antal";
        static bool IsPriceHeader(string key) => key is "apris" or "pris" or "price" or "unitprice";
        static bool IsAmountHeader(string key) => key is "belopp" or "amount" or "summa" or "total";

        static string NormalizeText(string? text) => (text ?? string.Empty).Replace('\u00a0', ' ').Trim();

        static string TextKey(string? text)
        {
            string normalized = NormalizeText(text).ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder key = new();

            foreach (char ch in normalized)
            {
                if (ch == '\u00b2')
                {
                    key.Append('2');
                    continue;
                }

                if (ch == '\u00b3')
                {
                    key.Append('3');
                    continue;
                }

                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(ch))
                    key.Append(ch);
            }

            return key.ToString();
        }

        static void ReSort(List<TaskPostDTO> sections)
        {
            for (int child = sections.Count - 1; child >= 0; child--)
            {
                for (int parent = child - 1; parent >= 0; parent--)
                {
                    bool parentHasCode = !string.IsNullOrEmpty(sections[parent].Metadata.Code);
                    bool childHasCode = !string.IsNullOrEmpty(sections[child].Metadata.Code);

                    if ((parentHasCode && childHasCode && sections[child].Metadata.Code.StartsWith(sections[parent].Metadata.Code))
                        || (!childHasCode && parentHasCode))
                    {
                        sections[parent].Tasks.Insert(0, sections[child]);
                        sections.RemoveAt(child);
                        break;
                    }
                }
            }
        }

        public static void Export(DataTable data, string sheetName, string path)
        {
            using XLWorkbook workbook = new();
            workbook.AddWorksheet(data, sheetName);
            using MemoryStream memory = new();
            workbook.SaveAs(memory);
            File.WriteAllBytes(path, memory.ToArray());
        }

        sealed class ColumnProfile
        {
            public ColumnProfile(int columnNumber)
            {
                ColumnNumber = columnNumber;
            }

            public int ColumnNumber { get; }
            public int NonEmptyCount { get; set; }
            public int NumericCount { get; set; }
            public int UnitLikeCount { get; set; }
            public int CodeLikeCount { get; set; }
            public int TextCount { get; set; }
            public int LongTextCount { get; set; }
        }

        sealed class HeaderLayout
        {
            public int RowNumber { get; set; }
            public int? CodeIndex { get; set; }
            public int? NameIndex { get; set; }
            public int? UnitIndex { get; set; }
            public int? QuantityIndex { get; set; }
            public int? PriceIndex { get; set; }
            public int Matches { get; set; }
            public int Score { get; set; }
        }
    }
}
