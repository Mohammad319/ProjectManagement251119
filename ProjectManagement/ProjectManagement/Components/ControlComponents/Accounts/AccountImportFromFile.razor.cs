using Application.Feature.Account.Commands;
using BlazorMHD.UI.Core.DesignSystem;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.DTO.Account;
using System.Text;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountImportFromFile
{
    [Parameter] public EventCallback<bool> OnSaved { get; set; }

    private IBrowserFile? File;
    private bool IsBusy;

    private readonly List<PostAccountGroupWithAccountsDTO> Groups = [];

    private int RowStart = 1;
    private int GroupCol = 3;
    private int NameCol = 4;
    private int CodeCol = 5;
    private int Comment1Col = 6;
    private int Comment2Col = 7;
    private string Separator = ",";

    private bool CanImport => File is not null && !IsBusy;
    private bool CanSave => Groups.Count > 0 && !IsBusy;

    private void OnFileSelection(InputFileChangeEventArgs e)
    {
        File = e.File;
        Groups.Clear();
    }

    private void CloseModal() => DialogService.Close();

    private void Save()
    {
        MHD.MessageYesNo(
            AppLoc[nameof(ResourceApp.save)],
            AppLoc[nameof(ResourceApp.DoYouWanTtoSaveTheListInDatabase)],
            MhdState.Primary,
            EventCallback.Factory.Create(this, SaveConfirmAsync));
    }

    private async Task ImportByFormatAsync()
    {
        if (File is null || IsBusy)
            return;

        IsBusy = true;
        StateHasChanged();

        try
        {
            Groups.Clear();

            if (GroupCol == NameCol)
                await ImportHierarchicalAsync();
            else
                await ImportFlatAsync();
        }
        finally
        {
            IsBusy = false;
            StateHasChanged();
        }
    }

    private async Task ImportFlatAsync()
    {
        using var stream = File!.OpenReadStream(10 * 1024 * 1024);
        using var reader = new StreamReader(stream);

        var toSkip = Math.Max(0, RowStart - 1);
        for (var i = 0; i < toSkip; i++)
        {
            if (await reader.ReadLineAsync() is null)
                return;
        }

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cols = SplitCsv(line, Separator);

            string GetCol(int col1Based) =>
                col1Based <= 0 || col1Based > cols.Count ? string.Empty : cols[col1Based - 1].Trim();

            var groupName = GetCol(GroupCol);
            var name = GetCol(NameCol);
            var code = GetCol(CodeCol);

            if (string.IsNullOrEmpty(groupName) && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(code))
                continue;

            var group = Groups.FirstOrDefault(x => x.Name == groupName);
            if (group is null)
            {
                group = new PostAccountGroupWithAccountsDTO { Name = groupName };
                Groups.Add(group);
            }

            var account = new PostAccountDTO
            {
                Name = name,
                Account = code,
                Data = new AccountData()
            };

            var c1 = GetCol(Comment1Col);
            var c2 = GetCol(Comment2Col);

            if (!string.IsNullOrEmpty(c1))
                account.Data.Comments.Add(c1);

            if (!string.IsNullOrEmpty(c2))
                account.Data.Comments.Add(c2);

            group.Accounts.Add(account);
        }
    }

    private async Task ImportHierarchicalAsync()
    {
        using var stream = File!.OpenReadStream(10 * 1024 * 1024);
        using var reader = new StreamReader(stream);

        var toSkip = Math.Max(0, RowStart - 1);
        for (var i = 0; i < toSkip; i++)
        {
            if (await reader.ReadLineAsync() is null)
                return;
        }

        PostAccountGroupWithAccountsDTO? current = null;

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cols = SplitCsv(line, Separator);

            string GetCol(int col1Based) =>
                col1Based <= 0 || col1Based > cols.Count ? string.Empty : cols[col1Based - 1].Trim();

            var groupName = GetCol(GroupCol);
            var name = GetCol(NameCol);
            var code = GetCol(CodeCol);

            if (string.IsNullOrEmpty(code))
            {
                if (!string.IsNullOrEmpty(groupName))
                {
                    current = new PostAccountGroupWithAccountsDTO { Name = groupName };
                    Groups.Add(current);
                }

                continue;
            }

            current ??= new PostAccountGroupWithAccountsDTO { Name = "G1_" };
            if (!Groups.Contains(current))
                Groups.Add(current);

            current.Accounts.Add(new PostAccountDTO
            {
                Name = name,
                Account = code,
                Data = new AccountData()
            });
        }

        if (Groups.Count > 0 && Groups[0].Name == "G1_" && !Groups[0].Accounts.Any())
            Groups.RemoveAt(0);
    }

    private void RemoveGroup(PostAccountGroupWithAccountsDTO group) => Groups.Remove(group);

    private void RemoveAccount(PostAccountGroupWithAccountsDTO group, PostAccountDTO account)
    {
        group.Accounts.Remove(account);

        if (group.Accounts.Count == 0)
            Groups.Remove(group);
    }

    private async Task SaveConfirmAsync()
    {
        if (IsBusy || Groups.Count == 0)
            return;

        IsBusy = true;
        StateHasChanged();

        try
        {
            var result = await Dispatcher.Send(new CreateRangeAccountGroupCommand(Groups));
            var ok = result is { Count: > 0 };

            MHD.Notifications(ToastType.Add, ok);
            await OnSaved.InvokeAsync(ok);

            if (ok)
                CloseModal();
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
