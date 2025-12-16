using Application.Feature.Account.Commands;
using BlazorMHD.UI.Core.DesignSystem;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.DTO.Account;
namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountImportFromFile
{
    [Inject] private ICommandDispatcher MicroBus { get; set; } = default!;
    [Inject] private ContextMenuService ContextService { get; set; } = default!;
    [Inject] private MhdServices MHD { get; set; } = default!;
    [Inject] private IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;

    private IBrowserFile? File;
    private bool IsBusy;

    private readonly List<PostAccountGroupWithAccountsDTO> Groups = [];

    private int RowStart = 1;

    // Column numbers (1-based)
    private int GroupCol = 3;
    private int NameCol = 4;
    private int CodeCol = 5;
    private int Comment1Col = 6;
    private int Comment2Col = 7;

    private string Separator = ",";

    private bool CanImport => File is not null && !IsBusy;
    private bool CanSave => Groups.Count > 0 && !IsBusy;

    private void OnFileSelection(InputFileChangeEventArgs e) => File = e.File;

    private void Save()
    {
        MHD.MessageYesNo(
            ResourceApp.save,
            ResourceApp.DoYouWanTtoSaveTheListInDatabase,
            MhdState.Primary,
            EventCallback.Factory.Create(this, SaveConfirm));
    }

    private async Task ImportByFormatAsync()
    {
        if (File is null) return;

        IsBusy = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            Groups.Clear();

            // إذا العمودين نفس بعض: يعني Format2 (حسب منطقك السابق)
            if (GroupCol == NameCol)
                await ImportHierarchicalAsync();
            else
                await ImportFlatAsync();
        }
        finally
        {
            IsBusy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ImportFlatAsync()
    {
        using var stream = File!.OpenReadStream(10 * 1024 * 1024);
        using var reader = new StreamReader(stream);

        var content = await reader.ReadToEndAsync();
        var lines = content.Split(Environment.NewLine).Skip(Math.Max(0, RowStart - 1));

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(Separator);

            // safe access helper
            string GetCol(int col1Based) =>
                col1Based <= 0 || col1Based > cols.Length ? string.Empty : cols[col1Based - 1].Trim();

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

            if (!string.IsNullOrEmpty(c1)) account.Data.Comments.Add(c1);
            if (!string.IsNullOrEmpty(c2)) account.Data.Comments.Add(c2);

            group.Accounts.Add(account);
        }
    }

    // Format: group rows separate accounts rows (منطق Import() السابق)
    private async Task ImportHierarchicalAsync()
    {
        using var stream = File!.OpenReadStream(10 * 1024 * 1024);
        using var reader = new StreamReader(stream);

        var content = await reader.ReadToEndAsync();
        var lines = content.Split(Environment.NewLine).Skip(Math.Max(0, RowStart - 1));

        PostAccountGroupWithAccountsDTO? current = null;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(Separator);

            string GetCol(int col1Based) =>
                col1Based <= 0 || col1Based > cols.Length ? string.Empty : cols[col1Based - 1].Trim();

            var groupName = GetCol(GroupCol);
            var name = GetCol(NameCol);
            var code = GetCol(CodeCol);

            // إذا الكود فاضي اعتبره صف مجموعة
            if (string.IsNullOrEmpty(code))
            {
                if (!string.IsNullOrEmpty(groupName))
                {
                    current = new PostAccountGroupWithAccountsDTO { Name = groupName };
                    Groups.Add(current);
                }
                continue;
            }

            // صف حساب
            current ??= new PostAccountGroupWithAccountsDTO { Name = "G1_" };
            if (!Groups.Contains(current)) Groups.Add(current);

            current.Accounts.Add(new PostAccountDTO
            {
                Name = name,
                Account = code,
                Data = new AccountData()
            });
        }

        // تنظيف المجموعة الافتراضية إن كانت فارغة
        if (Groups.Count > 0 && Groups[0].Name == "G1_" && !Groups[0].Accounts.Any())
            Groups.RemoveAt(0);
    }

    private void RemoveGroup(PostAccountGroupWithAccountsDTO group) => Groups.Remove(group);

    private void RemoveAccount(PostAccountGroupWithAccountsDTO group, PostAccountDTO account) =>
        group.Accounts.Remove(account);

    private async Task SaveConfirm()
    {
        var command = new CreateRangeAccountGroupCommand(Groups);
        await MicroBus.Send(command);
    }
}
