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
        public int AmountIndex { get; set; } = 7;
        public int RowEnd { get; set; } = 1;
        public int Score { get; set; }
        public bool IsDetected => Score > 0;
    }

    public sealed class ExcelImportResult
    {
        public List<TaskPostDTO> Tasks { get; set; } = [];
        public Dictionary<TaskPostDTO, int> RowNumbers { get; set; } = new();
        // Rows where the app had to fill in dashes that were missing in the original Excel
        public HashSet<TaskPostDTO> AppCompletedBarCodeRows { get; set; } = new(ReferenceEqualityComparer.Instance);
    }

    public class ExcelHelper
    {
        static readonly HashSet<string> KnownUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            "-", "%", "st", "stk", "pcs", "pc", "ea", "kpl", "m", "m1", "m2", "m3",
            "cm", "mm", "km", "kg", "g", "ton", "t", "l", "liter", "h", "hr", "tim", "timme",
            "timmar", "dag", "dygn", "sek", "kr", "man", "month", "styck", "each", "lm", "m²", "m³"
        };

        static readonly HashSet<string> HeaderWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "code", "kod", "amakod", "amacode", "itemcode", "artikelkod", "id", "nr", "nummer",
            "pos", "position", "littera", "ama", "name", "namn", "itemname", "artikeltext",
            "text", "benamning", "beteckning", "beskrivning", "description", "item", "artikel",
            "rubrik", "post", "aktivitet", "arbete", "arbetsmoment", "unit", "uom", "enhet",
            "enh", "eh", "me", "mattenhet", "quantity", "qty", "q", "antal", "kvantitet",
            "mangd", "mangdberaknad", "volym", "langd", "area", "price", "unitprice",
            "priceunit", "pris", "apris", "aprisenhet", "enhetspris", "prisenhet",
            "kostnadenhet", "belopp", "amount", "sum", "summa", "total", "totalt",
            "totalpris", "totalcost", "kostnad", "radsumma"
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
            => Import(sheetNr, workbook, rowStart, 0, codeIndex, nameIndex, unitIndex, quantityIndex, priceIndex, 0, isOH);

        public static List<TaskPostDTO> Import(int sheetNr, XLWorkbook workbook, int rowStart, int rowEnd,
            int codeIndex, int nameIndex, int unitIndex, int quantityIndex, int priceIndex, int amountIndex, bool isOH)
            => ImportWithRowNumbers(sheetNr, workbook, rowStart, rowEnd, codeIndex, nameIndex, unitIndex, quantityIndex, priceIndex, amountIndex, isOH).Tasks;

        public static ExcelImportResult ImportWithRowNumbers(int sheetNr, XLWorkbook workbook, int rowStart, int rowEnd,
            int codeIndex, int nameIndex, int unitIndex, int quantityIndex, int priceIndex, int amountIndex, bool isOH)
        {
            if (!workbook.Worksheets.Any())
                return new ExcelImportResult();

            int safeSheetNr = Math.Clamp(sheetNr, 1, workbook.Worksheets.Count);
            codeIndex = Math.Max(codeIndex, 1);
            nameIndex = Math.Max(nameIndex, 1);
            unitIndex = Math.Max(unitIndex, 1);
            quantityIndex = Math.Max(quantityIndex, 1);
            priceIndex = Math.Max(priceIndex, 1);
            amountIndex = amountIndex > 0 ? amountIndex : Math.Max(priceIndex + 1, codeIndex + 7);
            rowStart = Math.Max(rowStart, 1);

            List<TaskPostDTO> sections = new();
            Dictionary<TaskPostDTO, int> rowNumbers = new();
            HashSet<TaskPostDTO> leafCodeRows = new();
            HashSet<TaskPostDTO> appCompletedBarCodeRows = new(ReferenceEqualityComparer.Instance);
            IXLWorksheet sheet = workbook.Worksheet(safeSheetNr);

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            int effectiveLastRow = rowEnd >= rowStart ? Math.Min(rowEnd, lastRow) : lastRow;
            for (int row = rowStart; row <= effectiveLastRow; row++)
            {
                if (IsImportRowEmpty(sheet, row, codeIndex, nameIndex, unitIndex, quantityIndex, priceIndex, amountIndex))
                    continue;

                string code = CellText(sheet, row, codeIndex);
                string name = CellText(sheet, row, nameIndex);

                if (IsHeaderWord(code) && IsHeaderWord(name))
                    continue;

                string unit = IgnoreNonQuantityError(CellText(sheet, row, unitIndex));
                string quantityStr = CellText(sheet, row, quantityIndex);
                string priceStr = IgnoreNonQuantityError(CellText(sheet, row, priceIndex));
                string amountStr = IgnoreNonQuantityError(CellText(sheet, row, amountIndex));
                bool hasImportantCells = !string.IsNullOrWhiteSpace(unit)
                    || !string.IsNullOrWhiteSpace(quantityStr)
                    || IsDash(priceStr)
                    || IsDash(amountStr)
                    || HasNonZeroNumber(priceStr)
                    || HasNonZeroNumber(amountStr);

                if (!string.IsNullOrWhiteSpace(code) || !string.IsNullOrWhiteSpace(name) || hasImportantCells)
                {
                    if (LooksLikeNumber(unit) && IsLikelyUnit(quantityStr))
                        (unit, quantityStr) = (quantityStr, unit);

                    bool isLocalOrdinalCode = IsLocalOrdinalCode(code);
                    string sourceName = isLocalOrdinalCode ? JoinCodeAndName(code, name) : name;
                    if (string.IsNullOrWhiteSpace(sourceName))
                        sourceName = MissingNameText();
                    string importedName = LimitText(sourceName, FieldLengths.TaskName);
                    string importedCode = isLocalOrdinalCode ? string.Empty : LimitText(code, FieldLengths.Code);
                    string importedUnit = LimitText(unit, FieldLengths.Unit);

                    var task = new TaskPostDTO
                    {
                        Name = !string.IsNullOrWhiteSpace(importedName) ? importedName : "Task",
                        Tasks = new(),
                        Colspan = false,
                        Unit = importedUnit,
                        Metadata = new TaskMetadata
                        {
                            Code = importedCode,
                            Type = TaskType.Task,
                            IsOH = isOH
                        }
                    };

                    if (sourceName.Length > FieldLengths.TaskName)
                        task.Metadata.Note = LimitText(sourceName, FieldLengths.Note);

                    bool unitDash = IsDash(unit);
                    bool quantityDash = IsDash(quantityStr);
                    bool priceDash = IsDash(priceStr);
                    bool amountDash = IsDash(amountStr);
                    bool hasUnit = !string.IsNullOrWhiteSpace(unit) && !unitDash;
                    bool hasQuantity = TryReadDecimal(quantityStr, out decimal quantity);
                    // 4-streckad: price is dash AND (amount is dash OR amount is a real non-zero number)
                    bool isFourDashCodeRow = priceDash && (amountDash || HasNonZeroNumber(amountStr));
                    // 3-streckad: quantity or price has dash but not classified as 4-streckad
                    bool isThreeDashRow = !isFourDashCodeRow && (quantityDash || priceDash);
                    // CodeText: no bar-code signals, no real unit, no real quantity (name/text only ± price/amount numbers)
                    bool hasTextOnlyShape = !unitDash && !quantityDash && !priceDash && !amountDash
                        && !hasUnit && !hasQuantity;
                    // kalkylpost: has unit or quantity; amountDash alone does not disqualify
                    bool hasCalculationShape = !string.IsNullOrWhiteSpace(sourceName)
                        && (hasUnit || hasQuantity || unitDash)
                        && !quantityDash
                        && !priceDash;

                    if (isFourDashCodeRow)
                    {
                        task.Metadata.Type = TaskType.FourBarCode;
                        task.Unit = "-";
                        task.Quantity = null;
                        task.Metadata.PriceSubDB = null;
                        leafCodeRows.Add(task);
                        // Mark as completed if the original Excel didn't have all four dashes
                        if (!(unitDash && quantityDash && priceDash && amountDash))
                            appCompletedBarCodeRows.Add(task);
                    }
                    else if (hasTextOnlyShape)
                    {
                        task.Metadata.Type = TaskType.CodeName;
                        task.Quantity = null;
                    }
                    else if (isThreeDashRow)
                    {
                        task.Metadata.Type = TaskType.ThreeBarCode;
                        task.Quantity = 1m;
                        task.Metadata.BaseQuantity = 1m;
                        task.Metadata.QuantityParam = ConstValues.FixedQ;
                        task.Metadata.ChangeFactor1 = 1m;
                        task.Metadata.ChangeFactor2 = 1m;
                        // Mark as completed if the original Excel didn't have all three dashes (unit/quantity/price)
                        if (!(unitDash && quantityDash && priceDash))
                            appCompletedBarCodeRows.Add(task);
                    }
                    else
                    {
                        if (hasCalculationShape)
                        {
                            task.Metadata.QuantityParam = ConstValues.FixedQ;
                            task.Quantity = hasQuantity ? quantity : null;
                            task.Metadata.PriceSubDB = TryReadDecimal(priceStr, out decimal price) && price != 0m ? price : null;
                        }
                        else
                        {
                            task.Metadata.Type = TaskType.CodeName;
                            task.Quantity = hasQuantity ? quantity : null;
                            task.Metadata.PriceSubDB = null;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(task.Name))
                    {
                        sections.Add(task);
                        rowNumbers[task] = row;
                    }
                }
            }

            ReSort(sections, leafCodeRows);
            return new ExcelImportResult
            {
                Tasks = sections,
                RowNumbers = rowNumbers,
                AppCompletedBarCodeRows = appCompletedBarCodeRows
            };
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
            int amountIndex = header?.AmountIndex ?? DetectAmountColumn(profiles, priceIndex);

            ExcelImportLayout layout = new()
            {
                SheetIndex = sheetIndex,
                SheetName = sheet.Name,
                CodeIndex = codeIndex,
                NameIndex = nameIndex,
                UnitIndex = unitIndex,
                QuantityIndex = quantityIndex,
                PriceIndex = priceIndex,
                AmountIndex = amountIndex,
            };

            int startSearchRow = header == null ? firstRow : Math.Min(header.RowNumber + 1, lastRow);
            layout.RowStart = DetectStartRow(sheet, startSearchRow, lastRow, layout, header != null);
            layout.RowEnd = DetectEndRow(sheet, layout.RowStart, lastRow, layout);
            layout.Score = ScoreLayout(sheet, firstRow, lastRow, layout) + (header?.Score ?? 0);

            return layout;
        }

        static HeaderLayout? DetectHeaderLayout(IXLWorksheet sheet, int firstRow, int lastRow, int firstColumn, int lastColumn)
        {
            HeaderLayout? best = null;
            int maxHeaderRow = Math.Min(lastRow, firstRow + 150);

            for (int row = firstRow; row <= maxHeaderRow; row++)
            {
                HeaderLayout current = new() { RowNumber = row };
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
                    else if (current.AmountIndex == null && IsAmountHeader(key))
                    {
                        current.AmountIndex = column;
                        current.Matches++;
                    }
                }

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

                    if (IsDash(text))
                        profile.DashCount++;

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
                .Where(x => x.ColumnNumber > nameIndex && x.ColumnNumber <= nameIndex + 10 && x.ColumnNumber != unitIndex && (x.NumericCount > 0 || x.DashCount > 0));

            ColumnProfile? closestToUnit = candidates
                .OrderBy(x => Math.Abs(x.ColumnNumber - unitIndex))
                .ThenByDescending(x => x.NumericCount)
                .ThenByDescending(x => x.DashCount)
                .FirstOrDefault();

            if (closestToUnit != null)
                return closestToUnit.ColumnNumber;

            return unitIndex + 1;
        }

        static int DetectPriceColumn(List<ColumnProfile> profiles, int unitIndex, int quantityIndex)
        {
            int after = Math.Max(unitIndex, quantityIndex);

            ColumnProfile? profile = profiles
                .Where(x => x.ColumnNumber > after && x.ColumnNumber <= after + 8 && (x.NumericCount > 0 || x.DashCount > 0))
                .OrderBy(x => x.ColumnNumber)
                .ThenByDescending(x => x.NumericCount + x.DashCount)
                .FirstOrDefault();

            return profile?.ColumnNumber ?? after + 1;
        }

        static int DetectAmountColumn(List<ColumnProfile> profiles, int priceIndex)
        {
            ColumnProfile? profile = profiles
                .Where(x => x.ColumnNumber > priceIndex && x.ColumnNumber <= priceIndex + 6 && (x.NumericCount > 0 || x.DashCount > 0))
                .OrderBy(x => x.ColumnNumber)
                .ThenByDescending(x => x.NumericCount + x.DashCount)
                .FirstOrDefault();

            return profile?.ColumnNumber ?? priceIndex + 1;
        }

        static int DetectStartRow(IXLWorksheet sheet, int firstRow, int lastRow, ExcelImportLayout layout, bool hasHeader)
        {
            if (hasHeader)
            {
                for (int row = firstRow; row <= lastRow; row++)
                {
                    string name = CellText(sheet, row, layout.NameIndex);
                    if (IsMeaningfulName(name))
                        return row;
                }
            }

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

        static int DetectEndRow(IXLWorksheet sheet, int firstRow, int lastRow, ExcelImportLayout layout)
        {
            const int emptyLookaheadLimit = 50;
            int lastDataRow = firstRow;
            int emptyRun = 0;

            for (int row = firstRow; row <= lastRow; row++)
            {
                if (HasImportRowData(sheet, row, layout))
                {
                    lastDataRow = row;
                    emptyRun = 0;
                    continue;
                }

                emptyRun++;
                if (emptyRun >= emptyLookaheadLimit)
                    break;
            }

            return lastDataRow;
        }

        static bool HasImportRowData(IXLWorksheet sheet, int row, ExcelImportLayout layout) =>
            !string.IsNullOrWhiteSpace(CellText(sheet, row, layout.CodeIndex))
            || !string.IsNullOrWhiteSpace(CellText(sheet, row, layout.NameIndex))
            || !string.IsNullOrWhiteSpace(CellText(sheet, row, layout.UnitIndex))
            || !string.IsNullOrWhiteSpace(CellText(sheet, row, layout.QuantityIndex))
            || !string.IsNullOrWhiteSpace(CellText(sheet, row, layout.PriceIndex))
            || !string.IsNullOrWhiteSpace(CellText(sheet, row, layout.AmountIndex));
        

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

        static bool RowContainsOnlyCodeNameAndDashes(IXLWorksheet sheet, int row, int codeIndex, int nameIndex, int minimumDashCount)
        {
            int lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            int dashCount = 0;

            for (int column = 1; column <= lastColumn; column++)
            {
                if (column == codeIndex || column == nameIndex)
                    continue;

                string text = CellText(sheet, row, column);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (!IsDash(text))
                    return false;

                dashCount++;
            }

            return dashCount >= minimumDashCount;
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

            if (text.Contains(':') || text.Contains(';') || text.Contains(',') || text.Length > 4)
                return false;

            int letterCount = key.Count(char.IsLetter);
            int digitCount = key.Count(char.IsDigit);

            return letterCount > 0 && digitCount <= 2 && key.Length <= 4;
        }

        static bool LooksLikeNumber(string text) => TryReadDecimal(text, out _);

        static bool IsDash(string text) => NormalizeText(text) == "-";

        static bool HasNonZeroNumber(string text) =>
            TryReadDecimal(text, out decimal value) && value != 0m;

        static string IgnoreNonQuantityError(string text) =>
            IsSpreadsheetError(text) ? string.Empty : text;

        static bool IsSpreadsheetError(string text)
        {
            text = NormalizeText(text);
            return text.Length > 1
                && text[0] == '#'
                && (text.Contains('!') || text.Contains("ERROR", StringComparison.OrdinalIgnoreCase) || text.Contains("FEL", StringComparison.OrdinalIgnoreCase));
        }

        static string MissingNameText() =>
            CultureInfo.CurrentUICulture.Name.StartsWith("sv", StringComparison.OrdinalIgnoreCase)
                ? "Namn saknas"
                : "Name missing";

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
        static bool IsCodeHeader(string key) => key is "kod" or "amakod" or "amacode" or "code" or "itemcode" or "artikelkod" or "nr" or "nummer" or "pos" or "position" or "id" or "littera" or "ama";
        static bool IsNameHeader(string key) => key is "text" or "namn" or "name" or "item" or "artikel" or "itemname" or "artikeltext" or "description" or "benamning" or "beteckning" or "beskrivning" or "rubrik" or "post" or "aktivitet" or "arbete" or "arbetsmoment";
        static bool IsUnitHeader(string key) => key is "enhet" or "unit" or "uom" or "enh" or "eh" or "me" or "mattenhet";
        static bool IsQuantityHeader(string key) => key is "kvantitet" or "mangd" or "quantity" or "qty" or "q" or "antal" or "volym" or "langd" or "area" or "mangdberaknad";
        static bool IsPriceHeader(string key) => key is "apris" or "aprisenhet" or "pris" or "price" or "unitprice" or "priceunit" or "enhetspris" or "prisenhet" or "kostnadenhet";
        static bool IsAmountHeader(string key) => key is "belopp" or "amount" or "sum" or "summa" or "total" or "totalt" or "totalpris" or "totalcost" or "kostnad" or "radsumma";

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

        static void ReSort(List<TaskPostDTO> sections, ISet<TaskPostDTO>? leafTasks = null)
        {
            // Build a global code→task index so parent lookup is independent of input order.
            var keyToTask = new Dictionary<string, TaskPostDTO>(StringComparer.OrdinalIgnoreCase);
            foreach (var task in sections)
            {
                string k = CodeHierarchyKey(task.Metadata.Code ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(k))
                    keyToTask.TryAdd(k, task);
            }

            for (int child = sections.Count - 1; child >= 0; child--)
            {
                var childTask = sections[child];
                string childKey = CodeHierarchyKey(childTask.Metadata.Code ?? string.Empty);

                TaskPostDTO? bestParent = null;

                if (!string.IsNullOrWhiteSpace(childKey))
                {
                    // Pick the most specific parent: longest key that is a strict prefix of childKey.
                    // This ensures e.g. BBC always goes under BB (not B) when BB exists.
                    int bestLen = 0;
                    foreach (var (parentKey, parentTask) in keyToTask)
                    {
                        if (ReferenceEquals(parentTask, childTask)) continue;
                        if (leafTasks?.Contains(parentTask) == true) continue;
                        if (parentKey.Length >= childKey.Length) continue;
                        if (!childKey.StartsWith(parentKey, StringComparison.OrdinalIgnoreCase)) continue;
                        if (parentKey.Length > bestLen)
                        {
                            bestLen = parentKey.Length;
                            bestParent = parentTask;
                        }
                    }
                }
                else
                {
                    // No code: place under the nearest preceding item that has a code.
                    for (int p = child - 1; p >= 0; p--)
                    {
                        if (!string.IsNullOrWhiteSpace(sections[p].Metadata.Code) && leafTasks?.Contains(sections[p]) != true)
                        {
                            bestParent = sections[p];
                            break;
                        }
                    }
                }

                if (bestParent != null)
                {
                    bestParent.Colspan = true;
                    bestParent.Tasks.Insert(0, childTask);
                    sections.RemoveAt(child);
                }
            }
        }

        static bool IsParentCode(string parentCode, string childCode)
        {
            parentCode = NormalizeCode(parentCode);
            childCode = NormalizeCode(childCode);

            if (string.IsNullOrWhiteSpace(parentCode) ||
                string.IsNullOrWhiteSpace(childCode) ||
                string.Equals(parentCode, childCode, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (IsLocalOrdinalCode(childCode) && !IsLocalOrdinalCode(parentCode))
                return true;

            string parentPrefix = parentCode.TrimEnd('.');
            if (string.IsNullOrWhiteSpace(parentPrefix) ||
                !childCode.StartsWith(parentPrefix, StringComparison.OrdinalIgnoreCase))
                return false;

            if (childCode.Length == parentPrefix.Length)
                return false;

            char next = childCode[parentPrefix.Length];
            if (next is '.' or '-' or '_' or '/' or '\\')
                return true;

            if (char.IsLetterOrDigit(next))
                return IsLikelyCodeHierarchyStep(parentPrefix, childCode);

            return false;
        }

        static bool IsLikelyCodeHierarchyStep(string parentCode, string childCode)
        {
            string parentKey = CodeHierarchyKey(parentCode);
            string childKey = CodeHierarchyKey(childCode);

            return parentKey.Length > 0
                && childKey.Length > parentKey.Length
                && childKey.StartsWith(parentKey, StringComparison.OrdinalIgnoreCase);
        }

        static string CodeHierarchyKey(string code)
        {
            string normalized = NormalizeCode(code);
            StringBuilder key = new(normalized.Length);
            foreach (char ch in normalized)
            {
                if (char.IsLetterOrDigit(ch))
                    key.Append(ch);
            }

            return key.ToString();
        }

        static string NormalizeCode(string? code)
        {
            string normalized = NormalizeText(code);
            while (normalized.Length > 0 && char.IsWhiteSpace(normalized[^1]))
                normalized = normalized[..^1];

            return normalized;
        }

        static bool IsLocalOrdinalCode(string code)
        {
            code = NormalizeCode(code);
            if (code.Length < 1 || code.Length > 6)
                return false;

            char suffix = code[^1];
            string number = suffix is '.' or ')' ? code[..^1] : code;
            return number.Length > 0 && number.All(char.IsDigit);
        }

        static string JoinCodeAndName(string code, string name)
        {
            code = NormalizeCode(code);
            name = NormalizeText(name);

            if (string.IsNullOrWhiteSpace(code))
                return name;

            if (string.IsNullOrWhiteSpace(name))
                return code;

            if (name.StartsWith(code, StringComparison.OrdinalIgnoreCase))
                return name;

            return $"{code} {name}";
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
            public int DashCount { get; set; }
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
            public int? AmountIndex { get; set; }
            public int Matches { get; set; }
            public int Score { get; set; }
        }
    }
}
