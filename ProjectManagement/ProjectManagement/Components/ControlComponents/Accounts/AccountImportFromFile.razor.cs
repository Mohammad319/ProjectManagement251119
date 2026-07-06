using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.DTO.Account;
using System.Text;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountImportFromFile
{
    [Parameter] public EventCallback<bool> OnSaved { get; set; }

    private enum ImportFormat { Table, WideGroup }
    private enum DuplicateStrategy { Skip, Update }

    private enum PreviewStatus { New, Update, Duplicate, Error }

    private sealed class PreviewRow
    {
        public string Group = string.Empty;
        public string Code = string.Empty;
        public string Name = string.Empty;
        public string Comment1 = string.Empty;
        public string Comment2 = string.Empty;
        public PreviewStatus Status;
        public string? Error;
        public bool IsValid => Status != PreviewStatus.Error && Status != PreviewStatus.Duplicate;
        public bool IsSavable => Status is PreviewStatus.New or PreviewStatus.Update;
    }

    private IBrowserFile? File;
    private string FileName = string.Empty;
    private byte[]? _fileBytes;
    private bool _isExcel;
    private bool _isCsv;
    private bool IsBusy;
    private string? _fileError;
    private string? _importError;

    private List<string> SheetNames = [];
    private int SelectedSheet = 1;

    private ImportFormat Format = ImportFormat.Table;
    private DuplicateStrategy Strategy = DuplicateStrategy.Skip;

    // Vanligt tabellformat (one account per row)
    private int RowStart = 1;
    private int GroupCol = 1;
    private int CodeCol = 2;
    private int NameCol = 3;
    private int Comment1Col = 0;
    private int Comment2Col = 0;
    private string Separator = ",";

    // Brett gruppformat (several account groups side by side)
    private int HeaderRow = 1;
    private int WideRowStart = 2;
    private int FirstCodeCol = 1;
    private int FirstNameCol = 2;
    private int ColsPerGroup = 2;

    private readonly List<PreviewRow> Preview = [];
    private bool _previewDone;
    private List<string> NewGroupNames = [];

    // Existing data for duplicate/new detection: keyed on (group name, code), both lower-cased.
    private readonly HashSet<(string Group, string Code)> _existingKeys = [];
    private readonly HashSet<string> _existingGroupNames = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        var existing = await Dispatcher.Send(new GetAccountsOverviewQuery()) ?? [];
        foreach (var a in existing)
        {
            _existingGroupNames.Add(a.AccountGroupName);
            _existingKeys.Add((a.AccountGroupName.Trim().ToLowerInvariant(), a.Code.Trim().ToLowerInvariant()));
        }
    }

    private bool CanImport
        => File is not null && !IsBusy && _fileError is null && RequiredSettingsFilled;

    private bool RequiredSettingsFilled => Format switch
    {
        ImportFormat.Table => RowStart > 0 && GroupCol > 0 && CodeCol > 0 && NameCol > 0,
        ImportFormat.WideGroup => HeaderRow > 0 && WideRowStart > 0 && FirstCodeCol > 0 && FirstNameCol > 0 && ColsPerGroup > 0,
        _ => false
    };

    private bool CanSave => _previewDone && Preview.Any(p => p.IsSavable) && !IsBusy;

    private int SavableCount => Preview.Count(p => p.IsSavable);
    private int ErrorCount => Preview.Count(p => p.Status == PreviewStatus.Error);
    private int DuplicateCount => Preview.Count(p => p.Status == PreviewStatus.Duplicate);
    private int SavableGroupCount => Preview.Where(p => p.IsSavable).Select(p => p.Group.Trim().ToLowerInvariant()).Distinct().Count();

    private void CloseModal() => DialogService.CloseAsync();

    private async Task OnFileSelection(InputFileChangeEventArgs e)
    {
        ResetPreview();
        _importError = null;
        _fileError = null;
        File = e.File;
        FileName = e.File.Name;

        var ext = Path.GetExtension(FileName).ToLowerInvariant();
        _isExcel = ext is ".xlsx" or ".xlsm";
        _isCsv = ext is ".csv" or ".txt";

        if (!_isExcel && !_isCsv)
        {
            _fileError = "Filen kunde inte läsas. Kontrollera att filen är en Excel- eller CSV-fil.";
            File = null;
            _fileBytes = null;
            return;
        }

        try
        {
            await using var stream = e.File.OpenReadStream(25 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _fileBytes = ms.ToArray();

            if (_isExcel)
                LoadSheetNames();
        }
        catch
        {
            _fileError = "Filen kunde inte läsas. Kontrollera att filen är en Excel- eller CSV-fil.";
            File = null;
            _fileBytes = null;
        }
    }

    private void LoadSheetNames()
    {
        try
        {
            using var ms = new MemoryStream(_fileBytes!);
            using var workbook = new XLWorkbook(ms);
            SheetNames = workbook.Worksheets.Select(w => w.Name).ToList();
            SelectedSheet = 1;
        }
        catch
        {
            _fileError = "Filen kunde inte läsas som Excel.";
            SheetNames = [];
        }
    }

    private void ResetPreview()
    {
        Preview.Clear();
        NewGroupNames = [];
        _previewDone = false;
    }

    private void BuildPreview()
    {
        if (IsBusy)
            return;

        _importError = null;

        if (File is null || _fileBytes is null)
        {
            _importError = "Välj en fil först.";
            return;
        }

        // Format-specific required-field validation with clear Swedish messages.
        if (Format == ImportFormat.Table)
        {
            if (RowStart <= 0) { _importError = "Börja från rad nummer måste vara större än 0."; return; }
            if (CodeCol <= 0) { _importError = "Kod kolumnnummer saknas."; return; }
            if (NameCol <= 0) { _importError = "Namn kolumnnummer saknas."; return; }
            if (GroupCol <= 0) { _importError = "Kontogrupp kolumnnummer saknas."; return; }
        }
        else
        {
            if (WideRowStart <= 0) { _importError = "Börja från rad nummer måste vara större än 0."; return; }
            if (FirstCodeCol <= 0) { _importError = "Första kodkolumn saknas."; return; }
            if (FirstNameCol <= 0) { _importError = "Första namnkolumn saknas."; return; }
            if (ColsPerGroup <= 0) { _importError = "Antal kolumner per grupp måste vara större än 0."; return; }
        }

        IsBusy = true;
        try
        {
            ResetPreview();

            var matrix = ReadMatrix();
            if (matrix.Count == 0)
            {
                _importError = _isExcel
                    ? "Filen kunde inte läsas som Excel."
                    : "Filen kunde inte läsas. Kontrollera att filen är en Excel- eller CSV-fil.";
                return;
            }

            if (Format == ImportFormat.Table)
                BuildTablePreview(matrix);
            else
                BuildWidePreview(matrix);

            NewGroupNames = Preview
                .Where(p => p.IsSavable)
                .Select(p => p.Group.Trim())
                .Where(g => g.Length > 0 && !_existingGroupNames.Contains(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _previewDone = true;

            if (SavableCount == 0)
                _importError = "Preview saknar giltiga konton.";
        }
        catch
        {
            _importError = _isExcel
                ? "Filen kunde inte läsas som Excel."
                : "Filen kunde inte läsas. Kontrollera att filen är en Excel- eller CSV-fil.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // A 1-based [row][col] view of the file so both formats can address cells by number.
    private List<string[]> ReadMatrix()
    {
        if (_isExcel)
            return ReadExcelMatrix();

        return ReadCsvMatrix();
    }

    private List<string[]> ReadExcelMatrix()
    {
        using var ms = new MemoryStream(_fileBytes!);
        using var workbook = new XLWorkbook(ms);
        if (workbook.Worksheets.Count == 0)
            return [];

        var sheet = workbook.Worksheet(Math.Clamp(SelectedSheet, 1, workbook.Worksheets.Count));
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        var rows = new List<string[]>(lastRow);
        for (var r = 1; r <= lastRow; r++)
        {
            var cells = new string[lastCol];
            for (var c = 1; c <= lastCol; c++)
                cells[c - 1] = (sheet.Cell(r, c).GetFormattedString() ?? string.Empty).Trim();
            rows.Add(cells);
        }

        return rows;
    }

    private List<string[]> ReadCsvMatrix()
    {
        var text = Encoding.UTF8.GetString(_fileBytes!);
        var lines = text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        return lines.Select(l => SplitCsv(l, Separator).ToArray()).ToList();
    }

    private static string Cell(List<string[]> matrix, int row1, int col1)
    {
        if (row1 <= 0 || col1 <= 0 || row1 > matrix.Count)
            return string.Empty;

        var cells = matrix[row1 - 1];
        return col1 <= cells.Length ? (cells[col1 - 1] ?? string.Empty).Trim() : string.Empty;
    }

    private void BuildTablePreview(List<string[]> matrix)
    {
        var seen = new HashSet<(string, string)>();

        for (var row = RowStart; row <= matrix.Count; row++)
        {
            var group = Cell(matrix, row, GroupCol);
            var code = Cell(matrix, row, CodeCol);
            var name = Cell(matrix, row, NameCol);
            var c1 = Comment1Col > 0 ? Cell(matrix, row, Comment1Col) : string.Empty;
            var c2 = Comment2Col > 0 ? Cell(matrix, row, Comment2Col) : string.Empty;

            if (group.Length == 0 && code.Length == 0 && name.Length == 0)
                continue;

            Preview.Add(Classify(group, code, name, c1, c2, seen));
        }
    }

    private void BuildWidePreview(List<string[]> matrix)
    {
        var seen = new HashSet<(string, string)>();
        var lastCol = matrix.Count == 0 ? 0 : matrix.Max(r => r.Length);
        var nameOffset = FirstNameCol - FirstCodeCol;

        for (var groupCol = FirstCodeCol; groupCol <= lastCol; groupCol += ColsPerGroup)
        {
            var groupName = Cell(matrix, HeaderRow, groupCol);
            if (groupName.Length == 0)
                continue;

            var codeCol = groupCol;
            var nameCol = groupCol + nameOffset;

            for (var row = WideRowStart; row <= matrix.Count; row++)
            {
                var code = Cell(matrix, row, codeCol);
                var name = Cell(matrix, row, nameCol);

                if (code.Length == 0 && name.Length == 0)
                    continue;

                Preview.Add(Classify(groupName, code, name, string.Empty, string.Empty, seen));
            }
        }
    }

    private PreviewRow Classify(string group, string code, string name, string c1, string c2, HashSet<(string, string)> seen)
    {
        var row = new PreviewRow { Group = group, Code = code, Name = name, Comment1 = c1, Comment2 = c2 };

        if (code.Length == 0) { row.Status = PreviewStatus.Error; row.Error = "Kod saknas"; return row; }
        if (name.Length == 0) { row.Status = PreviewStatus.Error; row.Error = "Namn saknas"; return row; }
        if (group.Length == 0) { row.Status = PreviewStatus.Error; row.Error = "Kontogrupp saknas"; return row; }

        var key = (group.Trim().ToLowerInvariant(), code.Trim().ToLowerInvariant());

        // Duplicate within the same file → always skipped to avoid ambiguous double-writes.
        if (!seen.Add(key))
        {
            row.Status = PreviewStatus.Duplicate;
            row.Error = "Dubblett kod (i filen)";
            return row;
        }

        if (_existingKeys.Contains(key))
        {
            row.Status = Strategy == DuplicateStrategy.Update ? PreviewStatus.Update : PreviewStatus.Duplicate;
            if (row.Status == PreviewStatus.Duplicate)
                row.Error = "Kontot finns redan";
            return row;
        }

        row.Status = PreviewStatus.New;
        return row;
    }

    private void Save()
    {
        if (!_previewDone)
        {
            _importError = "Spara kan inte göras innan importen har granskats.";
            return;
        }

        if (SavableCount == 0)
        {
            _importError = "Preview saknar giltiga konton.";
            return;
        }

        MHD.MessageYesNo(
            AppLoc[nameof(ResourceApp.save)],
            AppLoc[nameof(ResourceApp.DoYouWanTtoSaveTheListInDatabase)],
            BlazorMHD.UI.Core.DesignSystem.MhdState.Primary,
            EventCallback.Factory.Create(this, SaveConfirmAsync));
    }

    private async Task SaveConfirmAsync()
    {
        if (IsBusy || SavableCount == 0)
            return;

        IsBusy = true;
        StateHasChanged();

        try
        {
            var items = Preview
                .Where(p => p.IsSavable)
                .GroupBy(p => p.Group.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new PostAccountGroupWithAccountsDTO
                {
                    Name = g.Key,
                    Accounts = g.Select(p =>
                    {
                        var comments = new List<string>();
                        if (!string.IsNullOrWhiteSpace(p.Comment1)) comments.Add(p.Comment1.Trim());
                        if (!string.IsNullOrWhiteSpace(p.Comment2)) comments.Add(p.Comment2.Trim());

                        return new PostAccountDTO
                        {
                            Account = p.Code.Trim(),
                            Name = p.Name.Trim(),
                            IsVisible = true,
                            Data = new AccountData { Comments = comments }
                        };
                    }).ToList()
                })
                .ToList();

            var result = await Dispatcher.Send(new ImportAccountGroupsCommand(items, Strategy == DuplicateStrategy.Update));

            if (result is { Success: true })
            {
                var summary = $"Import klar: {result.GroupsCreated} nya kontogrupper och " +
                              $"{result.AccountsCreated + result.AccountsUpdated} konton behandlades.";
                if (result.AccountsSkipped > 0)
                    summary += $" {result.AccountsSkipped} rader hoppades över.";

                MHD.ToastInfo(summary, string.Empty, true);
                await OnSaved.InvokeAsync(true);
                CloseModal();
            }
            else
            {
                _importError = "Det gick inte att spara importen. Kontrollera filen och försök igen.";
                await OnSaved.InvokeAsync(false);
            }
        }
        catch
        {
            _importError = "Det gick inte att spara importen. Kontrollera filen och försök igen.";
            await OnSaved.InvokeAsync(false);
        }
        finally
        {
            IsBusy = false;
            StateHasChanged();
        }
    }

    private static IReadOnlyList<string> SplitCsv(string line, string separator)
    {
        var sepChar = string.IsNullOrEmpty(separator) ? ',' : separator[0];

        var result = new List<string>(16);
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == sepChar && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString());
        return result;
    }
}
