using Application.Feature.Account.Commands;
using Application.Feature.Account.Queries;
using BlazorMHD.UI.Core.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Account;
using System.Text;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountImportFromFile
{
    [Parameter] public EventCallback<bool> OnSaved { get; set; }

    internal enum ImportFormat { Table, PairColumns }
    internal enum DuplicateStrategy { Skip, Update }

    internal enum PreviewStatus
    {
        New,            // Ny
        Update,         // Uppdateras
        Exists,         // Finns redan (hoppas över)
        Ignored,        // Ignoreras (t.ex. dubblett i filen)
        Error,          // Fel: … (blockerande)
        GroupNew,       // Ny kontogrupp (rubrikrad i kolumnpar-format)
        GroupExisting   // Kontogrupp (finns redan)
    }

    internal sealed class PreviewRow
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsGroupRow;

        /// <summary>Pair format: which group-header row this account was read under (drives rename propagation).</summary>
        public Guid? GroupRowId;

        /// <summary>Set once the admin edits the account's group by hand — stops auto-propagation from the header row.</summary>
        public bool GroupManuallyEdited;

        public string PairLabel = string.Empty;
        public int SourceRow;

        public string Group = string.Empty;
        public string Code = string.Empty;
        public string Name = string.Empty;
        public string Comment1 = string.Empty;
        public string Comment2 = string.Empty;

        public PreviewStatus Status;
        public string? Error;

        public bool IsSavable => Status is PreviewStatus.New or PreviewStatus.Update;
        public bool IsBlocking => Status == PreviewStatus.Error;
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

    // Vanligt tabellformat (one account per row, group in an optional column).
    // Defaults assume the simplest file: data from row 1, code in column 1, name in column 2.
    private int RowStart = 1;
    private int? RowEnd;
    private int? GroupCol;
    private int CodeCol = 1;
    private int NameCol = 2;
    private int? Comment1Col;
    private int? Comment2Col;
    private string Separator = ",";

    // Rubrikbaserat kolumnpar (code+name pairs; a row without code but with name starts a new group)
    private int PairRowStart = 1;
    private int? PairRowEnd;
    private int FirstCodeCol = 1;
    private int FirstNameCol = 2;
    private int ColsPerPair = 2;
    private bool ReadAllPairs = true;
    private bool NameOnlyIsGroup = true;

    private readonly List<PreviewRow> Preview = [];
    private bool _previewDone;

    // True once the admin has edited/removed preview rows — closing then needs a confirmation.
    private bool _previewEdited;
    private MhdDialogModel? _dialogModel;

    private List<string> NewGroupNames = [];

    // Import history ("Senaste importer")
    private List<AccountImportBatchDTO> Batches = [];
    private int? _expandedBatchId;
    private List<AccountImportBatchRowDTO> _batchRows = [];

    // Existing data for duplicate/new detection: keyed on (group name, code), both lower-cased.
    private readonly HashSet<(string Group, string Code)> _existingKeys = [];
    private readonly HashSet<string> _existingGroupNames = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        // This component is always shown inside a dialog; the topmost dialog at init is ours.
        // The guard keeps X/ESC/Avbryt working but asks first when preview edits would be lost.
        _dialogModel = DialogService.Dialogs.Count > 0 ? DialogService.Dialogs[^1] : null;
        if (_dialogModel is not null)
            _dialogModel.OnBeforeCloseAsync = ConfirmCloseAsync;

        await LoadExistingAsync();
        await LoadBatchesAsync();
    }

    private Task<bool> ConfirmCloseAsync()
    {
        if (!_previewDone || !_previewEdited)
            return Task.FromResult(true);

        MHD.MessageYesNo(
            "Stäng importen",
            "Vill du stänga importen? Osparade ändringar i förhandsgranskningen försvinner.",
            BlazorMHD.UI.Core.DesignSystem.MhdState.Danger,
            EventCallback.Factory.Create(this, ForceCloseAsync));

        return Task.FromResult(false);
    }

    private async Task ForceCloseAsync()
    {
        if (_dialogModel is not null)
        {
            _dialogModel.OnBeforeCloseAsync = null;
            await DialogService.CloseAsync(_dialogModel);
        }
        else
        {
            await DialogService.CloseAsync();
        }
    }

    private async Task LoadExistingAsync()
    {
        _existingKeys.Clear();
        _existingGroupNames.Clear();

        var existing = await Dispatcher.Send(new GetAccountsOverviewQuery()) ?? [];
        foreach (var a in existing)
        {
            _existingGroupNames.Add(a.AccountGroupName);
            _existingKeys.Add((a.AccountGroupName.Trim().ToLowerInvariant(), a.Code.Trim().ToLowerInvariant()));
        }

        var groups = await Dispatcher.Send(new GetAccountGroupsQuery()) ?? [];
        foreach (var g in groups)
            _existingGroupNames.Add(g.Name);
    }

    private async Task LoadBatchesAsync()
    {
        Batches = await Dispatcher.Send(new GetAccountImportBatchesQuery(10)) ?? [];
    }

    // ---------------------------------------------------------------
    // File selection
    // ---------------------------------------------------------------
    private bool CanPreview
        => File is not null && !IsBusy && _fileError is null && RequiredSettingsFilled;

    private bool RequiredSettingsFilled => Format switch
    {
        ImportFormat.Table => RowStart > 0 && CodeCol > 0 && NameCol > 0,
        ImportFormat.PairColumns => PairRowStart > 0 && FirstCodeCol > 0 && FirstNameCol > 0 && ColsPerPair > 0,
        _ => false
    };

    private int SavableCount => Preview.Count(p => !p.IsGroupRow && p.IsSavable);
    private int NewCount => Preview.Count(p => !p.IsGroupRow && p.Status == PreviewStatus.New);
    private int UpdateCount => Preview.Count(p => !p.IsGroupRow && p.Status == PreviewStatus.Update);
    private int BlockingCount => Preview.Count(p => p.IsBlocking);
    private int ExistsCount => Preview.Count(p => p.Status == PreviewStatus.Exists);
    private int IgnoredCount => Preview.Count(p => p.Status == PreviewStatus.Ignored);

    private int SavableGroupCount => Preview
        .Where(p => !p.IsGroupRow && p.IsSavable)
        .Select(p => p.Group.Trim().ToLowerInvariant())
        .Concat(Preview.Where(p => p.IsGroupRow && p.Name.Trim().Length > 0).Select(p => p.Name.Trim().ToLowerInvariant()))
        .Distinct()
        .Count();

    private bool CanSave => _previewDone && !IsBusy && BlockingCount == 0 && SavableCount > 0;

    private string SaveTooltip
    {
        get
        {
            if (!_previewDone)
                return "Kör Förhandsgranska import och granska raderna först.";
            if (BlockingCount > 0)
                return "Det finns blockerande fel. Korrigera eller ta bort raderna innan du sparar.";
            if (SavableCount == 0)
                return "Det finns inga giltiga rader att spara.";
            return "Sparar giltiga rader från förhandsgranskningen till databasen.";
        }
    }

    private void CloseModal() => DialogService.CloseAsync();

    private async Task OnFileSelection(InputFileChangeEventArgs e)
    {
        if (IsBusy)
            return;

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
            _fileError = "Filen kunde inte läsas. Kontrollera att det är en giltig Excel- eller CSV-fil.";
            File = null;
            _fileBytes = null;
            return;
        }

        // Show the file name immediately and block double-selection while the bytes are read,
        // so the window never looks frozen after picking a file.
        IsBusy = true;
        StateHasChanged();

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
            _fileError = "Filen kunde inte läsas. Kontrollera att det är en giltig Excel- eller CSV-fil.";
            File = null;
            _fileBytes = null;
        }
        finally
        {
            IsBusy = false;
            StateHasChanged();
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
        _previewEdited = false;
    }

    // ---------------------------------------------------------------
    // Help windows
    // ---------------------------------------------------------------
    private void OpenHelp(bool pairFormat)
        => DialogService.ShowComponent<AccountImportHelpUI>(
            pairFormat ? "Hjälp — Rubrikbaserat kolumnpar" : "Hjälp — Vanligt tabellformat",
            new Dictionary<string, object>
            {
                [nameof(AccountImportHelpUI.IsPairFormat)] = pairFormat
            },
            BlazorMHD.UI.Core.Services.MhdDialogSize.ExtraLarge);

    // ---------------------------------------------------------------
    // Preview build
    // ---------------------------------------------------------------
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

        if (Format == ImportFormat.Table)
        {
            if (RowStart <= 0) { _importError = "Börja från rad nummer måste vara större än 0."; return; }
            if (CodeCol <= 0) { _importError = "Kod kolumnnummer saknas."; return; }
            if (NameCol <= 0) { _importError = "Namn kolumnnummer saknas."; return; }
        }
        else
        {
            if (PairRowStart <= 0) { _importError = "Börja från rad nummer måste vara större än 0."; return; }
            if (FirstCodeCol <= 0) { _importError = "Första kodkolumn saknas."; return; }
            if (FirstNameCol <= 0) { _importError = "Första namnkolumn saknas."; return; }
            if (FirstNameCol == FirstCodeCol) { _importError = "Första namnkolumn måste vara en annan kolumn än första kodkolumn."; return; }
            if (ColsPerPair <= 0) { _importError = "Antal kolumner per par måste vara större än 0."; return; }
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
                BuildPairPreview(matrix);

            ReclassifyAll();
            _previewDone = true;

            if (Preview.Count == 0)
                _importError = "Inga rader hittades i det valda området. Kontrollera rad- och kolumnnummer.";
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
        => _isExcel ? ReadExcelMatrix() : ReadCsvMatrix();

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

    // Empty "Till rad nummer" means: read to the last used row in the sheet.
    private int EffectiveRowEnd(int? rowEnd, int lastRow)
        => rowEnd is > 0 ? Math.Min(rowEnd.Value, lastRow) : lastRow;

    private void BuildTablePreview(List<string[]> matrix)
    {
        var rowEnd = EffectiveRowEnd(RowEnd, matrix.Count);

        for (var row = RowStart; row <= rowEnd; row++)
        {
            // 0/empty group column = "används inte"; the group can be filled in from the preview.
            var group = GroupCol is > 0 ? Cell(matrix, row, GroupCol.Value) : string.Empty;
            var code = Cell(matrix, row, CodeCol);
            var name = Cell(matrix, row, NameCol);
            var c1 = Comment1Col is > 0 ? Cell(matrix, row, Comment1Col.Value) : string.Empty;
            var c2 = Comment2Col is > 0 ? Cell(matrix, row, Comment2Col.Value) : string.Empty;

            if (group.Length == 0 && code.Length == 0 && name.Length == 0)
                continue;

            Preview.Add(new PreviewRow
            {
                SourceRow = row,
                Group = group,
                Code = code,
                Name = name,
                Comment1 = c1,
                Comment2 = c2
            });
        }
    }

    private void BuildPairPreview(List<string[]> matrix)
    {
        var rowEnd = EffectiveRowEnd(PairRowEnd, matrix.Count);
        var lastCol = matrix.Count == 0 ? 0 : matrix.Max(r => r.Length);
        var nameOffset = FirstNameCol - FirstCodeCol;
        var lastCodeCol = ReadAllPairs ? lastCol : FirstCodeCol;

        for (var codeCol = FirstCodeCol; codeCol <= lastCodeCol; codeCol += ColsPerPair)
        {
            var nameCol = codeCol + nameOffset;
            var pairLabel = $"{ColumnLetter(codeCol)}+{ColumnLetter(nameCol)}";
            PreviewRow? currentGroup = null;

            for (var row = PairRowStart; row <= rowEnd; row++)
            {
                var code = Cell(matrix, row, codeCol);
                var name = Cell(matrix, row, nameCol);

                // Both empty → the row is skipped.
                if (code.Length == 0 && name.Length == 0)
                    continue;

                // No code but a name → the row is a new account group (when enabled).
                if (code.Length == 0 && NameOnlyIsGroup)
                {
                    var groupRow = new PreviewRow
                    {
                        IsGroupRow = true,
                        PairLabel = pairLabel,
                        SourceRow = row,
                        Name = name,
                        Group = name
                    };
                    Preview.Add(groupRow);
                    currentGroup = groupRow;
                    continue;
                }

                Preview.Add(new PreviewRow
                {
                    PairLabel = pairLabel,
                    SourceRow = row,
                    Code = code,
                    Name = name,
                    Group = currentGroup?.Name.Trim() ?? string.Empty,
                    GroupRowId = currentGroup?.Id
                });
            }
        }
    }

    internal static string ColumnLetter(int col)
    {
        if (col <= 0)
            return "?";

        var sb = new StringBuilder();
        while (col > 0)
        {
            col--;
            sb.Insert(0, (char)('A' + col % 26));
            col /= 26;
        }

        return sb.ToString();
    }

    // ---------------------------------------------------------------
    // Classification (re-run after every edit so statuses stay live)
    // ---------------------------------------------------------------
    private void ReclassifyAll()
    {
        var seen = new HashSet<(string, string)>();

        foreach (var row in Preview)
        {
            if (row.IsGroupRow)
            {
                row.Error = null;
                row.Status = _existingGroupNames.Contains(row.Name.Trim())
                    ? PreviewStatus.GroupExisting
                    : PreviewStatus.GroupNew;
                continue;
            }

            ClassifyAccountRow(row, seen);
        }

        NewGroupNames = Preview
            .Where(p => !p.IsGroupRow && p.IsSavable)
            .Select(p => p.Group.Trim())
            .Concat(Preview.Where(p => p.IsGroupRow).Select(p => p.Name.Trim()))
            .Where(g => g.Length > 0 && !_existingGroupNames.Contains(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ClassifyAccountRow(PreviewRow row, HashSet<(string, string)> seen)
    {
        row.Error = null;

        var code = row.Code.Trim();
        var name = row.Name.Trim();
        var group = row.Group.Trim();

        if (code.Length == 0) { row.Status = PreviewStatus.Error; row.Error = "Kod saknas"; return; }
        if (name.Length == 0) { row.Status = PreviewStatus.Error; row.Error = "Namn saknas"; return; }
        if (code.Length > FieldLengths.Code) { row.Status = PreviewStatus.Error; row.Error = "Ogiltig kod"; return; }
        if (group.Length == 0) { row.Status = PreviewStatus.Error; row.Error = "Kontogrupp saknas"; return; }

        var key = (group.ToLowerInvariant(), code.ToLowerInvariant());

        // Duplicate within the same import → ignored to avoid ambiguous double-writes.
        if (!seen.Add(key))
        {
            row.Status = PreviewStatus.Ignored;
            row.Error = "Dubblett i filen";
            return;
        }

        if (_existingKeys.Contains(key))
        {
            row.Status = Strategy == DuplicateStrategy.Update ? PreviewStatus.Update : PreviewStatus.Exists;
            return;
        }

        row.Status = PreviewStatus.New;
    }

    // ---------------------------------------------------------------
    // Preview editing
    // ---------------------------------------------------------------
    private void OnRowEdited()
    {
        _previewEdited = true;
        ReclassifyAll();
    }

    private void OnAccountGroupEdited(PreviewRow row)
    {
        row.GroupManuallyEdited = true;
        _previewEdited = true;
        ReclassifyAll();
    }

    private void OnGroupRowNameChanged(PreviewRow groupRow, ChangeEventArgs e)
    {
        _previewEdited = true;
        var newName = (e.Value?.ToString() ?? string.Empty).Trim();
        groupRow.Name = newName;
        groupRow.Group = newName;

        // Rename propagates to the accounts read under this header — unless the admin
        // already changed that account's group by hand.
        foreach (var acc in Preview.Where(p => !p.IsGroupRow && p.GroupRowId == groupRow.Id && !p.GroupManuallyEdited))
            acc.Group = newName;

        ReclassifyAll();
    }

    private void RemoveRow(PreviewRow row)
    {
        _previewEdited = true;
        Preview.Remove(row);

        if (row.IsGroupRow)
        {
            // Accounts keep their group text; they just lose the rename link to the removed header.
            foreach (var acc in Preview.Where(p => p.GroupRowId == row.Id))
                acc.GroupRowId = null;
        }

        ReclassifyAll();
    }

    private void OnStrategyChanged(DuplicateStrategy value)
    {
        Strategy = value;
        if (_previewDone)
            ReclassifyAll();
    }

    // Summary per column pair, e.g. "A+B: 3 kontogrupper, 42 konton".
    private IReadOnlyList<(string Pair, int Groups, int Accounts)> PairSummaries
        => Preview
            .GroupBy(p => p.PairLabel)
            .Where(g => g.Key.Length > 0)
            .Select(g => (g.Key, g.Count(p => p.IsGroupRow), g.Count(p => !p.IsGroupRow)))
            .ToList();

    // ---------------------------------------------------------------
    // Save
    // ---------------------------------------------------------------
    private void Save()
    {
        if (!CanSave)
        {
            _importError = SaveTooltip;
            return;
        }

        MHD.MessageYesNo(
            "Spara import",
            AppLoc[nameof(ResourceApp.DoYouWanTtoSaveTheListInDatabase)],
            BlazorMHD.UI.Core.DesignSystem.MhdState.Primary,
            EventCallback.Factory.Create(this, SaveConfirmAsync));
    }

    private async Task SaveConfirmAsync()
    {
        if (IsBusy || !CanSave)
            return;

        IsBusy = true;
        StateHasChanged();

        try
        {
            var savableAccounts = Preview.Where(p => !p.IsGroupRow && p.IsSavable).ToList();

            var items = savableAccounts
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

            // Group-header rows that ended up without accounts are still created as empty groups.
            var coveredGroupNames = new HashSet<string>(items.Select(i => i.Name), StringComparer.OrdinalIgnoreCase);
            foreach (var groupRow in Preview.Where(p => p.IsGroupRow))
            {
                var name = groupRow.Name.Trim();
                if (name.Length == 0 || !coveredGroupNames.Add(name))
                    continue;

                items.Add(new PostAccountGroupWithAccountsDTO { Name = name, Accounts = [] });
            }

            var batchRows = savableAccounts
                .Select(p => new AccountImportBatchRowDTO
                {
                    Group = p.Group.Trim(),
                    Code = p.Code.Trim(),
                    Name = p.Name.Trim(),
                    Action = p.Status == PreviewStatus.Update ? "Uppdaterad" : "Ny"
                })
                .ToList();

            var batchInfo = new AccountImportBatchInfoDTO
            {
                FileName = FileName,
                ImportType = Format == ImportFormat.Table ? "Table" : "PairColumns",
                Rows = batchRows
            };

            var result = await Dispatcher.Send(new ImportAccountGroupsCommand(items, Strategy == DuplicateStrategy.Update, batchInfo));

            if (result is { Success: true })
            {
                var summary = $"Import klar: {result.GroupsCreated} nya kontogrupper och " +
                              $"{result.AccountsCreated + result.AccountsUpdated} konton behandlades.";
                if (result.AccountsSkipped > 0)
                    summary += $" {result.AccountsSkipped} rader hoppades över.";

                MHD.ToastInfo(summary, string.Empty, true);
                _previewEdited = false;   // saved — closing needs no confirmation anymore
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

    // ---------------------------------------------------------------
    // Import history ("Senaste importer")
    // ---------------------------------------------------------------
    private static string ImportTypeName(string key) => key switch
    {
        "PairColumns" => "Rubrikbaserat kolumnpar",
        _ => "Vanligt tabellformat"
    };

    private async Task ToggleBatchRowsAsync(AccountImportBatchDTO batch)
    {
        if (_expandedBatchId == batch.Id)
        {
            _expandedBatchId = null;
            _batchRows = [];
            return;
        }

        _batchRows = await Dispatcher.Send(new GetAccountImportBatchRowsQuery(batch.Id)) ?? [];
        _expandedBatchId = batch.Id;
    }

    private void UndoBatch(AccountImportBatchDTO batch)
    {
        if (!batch.CanUndo)
            return;

        MHD.MessageYesNo(
            "Ångra import",
            "Vill du ångra denna import? Konton och kontogrupper som skapades av importen tas bort.",
            BlazorMHD.UI.Core.DesignSystem.MhdState.Danger,
            EventCallback.Factory.Create(this, () => UndoBatchConfirmAsync(batch)));
    }

    private async Task UndoBatchConfirmAsync(AccountImportBatchDTO batch)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        StateHasChanged();

        try
        {
            var ok = await Dispatcher.Send(new UndoAccountImportBatchCommand(batch.Id));
            if (ok)
            {
                MHD.ToastInfo("Importen har ångrats.", string.Empty, true);
                await LoadBatchesAsync();
                await LoadExistingAsync();
                if (_previewDone)
                    ReclassifyAll();
                await OnSaved.InvokeAsync(true);
            }
            else
            {
                _importError = "Importen kunde inte ångras.";
            }
        }
        finally
        {
            IsBusy = false;
            StateHasChanged();
        }
    }

    private static string UndoTooltip(AccountImportBatchDTO batch)
    {
        if (batch.IsUndone)
            return "Importen är redan ångrad.";
        if (batch.AccountsUpdated > 0)
            return "Importen uppdaterade befintliga konton och kan inte ångras utan historik.";
        if (batch.AccountsCreated == 0 && batch.GroupsCreated == 0)
            return "Importen skapade inga nya konton eller kontogrupper.";
        return "Tar bort konton och kontogrupper som skapades av importen.";
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
