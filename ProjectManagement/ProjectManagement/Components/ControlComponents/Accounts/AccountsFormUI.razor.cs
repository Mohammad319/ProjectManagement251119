using Application.Feature.Account.Commands;
using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Components.ControlComponents.Department;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.General;

namespace ProjectManagement.Components.ControlComponents.Accounts;

public partial class AccountsFormUI
{
    public const string DialogFormId = "accountForm";
    [Parameter] public int Id { get; set; }

    [Parameter, EditorRequired]
    public required PostAccountDTO Model { get; set; }
    [Parameter] public EventCallback<bool> OnSaved { get; set; }

    /// <summary>Account groups available for the "Kontogrupp" dropdown.</summary>
    [Parameter] public List<ListDTO> Groups { get; set; } = [];

    private IReadOnlyList<AppSelect<int>.Option> GroupOptions =>
        Groups.Select(g => new AppSelect<int>.Option(g.Id, g.Name)).ToList();

    [Inject] private ILogger<AccountsFormUI> Logger { get; set; } = default!;

    private PostAccountDTO EditModel { get; set; } = new();
    private bool IsLoading;
    private int LastId = -1;
    private PostAccountDTO? LastModelReference;

    // Inline, Swedish validation state so a missing required field is shown in the dialog and never
    // reaches the backend as an unexplained HTTP 400.
    private string? _codeError;
    private string? _nameError;
    private string? _groupError;
    private string? _saveError;

    protected override void OnParametersSet()
    {
        var sameReference = ReferenceEquals(LastModelReference, Model);
        if (sameReference && LastId == Id)
            return;

        EditModel = new PostAccountDTO
        {
            Name = Model?.Name ?? string.Empty,
            Account = Model?.Account ?? string.Empty,
            AccountGroupId = Model?.AccountGroupId ?? 0,
            IsVisible = Model?.IsVisible ?? true,
            Data = Model?.Data ?? new AccountData()
        };

        EnsureTwoComments();

        LastId = Id;
        LastModelReference = Model;
    }

    // The Kontoplan uses exactly two fixed comment slots (Kommentar 1 / Kommentar 2); make sure both
    // always exist so the two-way bindings are stable.
    private void EnsureTwoComments()
    {
        EditModel.Data ??= new AccountData();
        EditModel.Data.Comments ??= [];
        while (EditModel.Data.Comments.Count < 2)
            EditModel.Data.Comments.Add(string.Empty);
    }

    private void CloseModal() => DialogService.CloseAsync();

    private async Task HandleSubmitAsync()
    {
        if (IsLoading) return;

        if (!Validate())
            return;

        IsLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            // Trim empty trailing comment slots so we never persist blank "Kommentar 2" noise.
            EditModel.Data.Comments = (EditModel.Data.Comments ?? [])
                .Select(c => (c ?? string.Empty).Trim())
                .ToList();

            var ok = await CreateOrUpdateAsync(Id, EditModel);

            if (ok)
            {
                MHD.ToastInfo("Kontot har sparats.", string.Empty, true);
                await OnSaved.InvokeAsync(true);
                CloseModal();
            }
            else
            {
                _saveError = "Det gick inte att spara kontot. Kontrollera obligatoriska fält och försök igen.";
                await OnSaved.InvokeAsync(false);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save Account. Id={Id}", Id);
            _saveError = "Det gick inte att spara kontot. Kontrollera obligatoriska fält och försök igen.";
            await OnSaved.InvokeAsync(false);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private bool Validate()
    {
        _saveError = null;
        _codeError = string.IsNullOrWhiteSpace(EditModel.Account) ? "Kod är obligatoriskt." : null;
        _nameError = string.IsNullOrWhiteSpace(EditModel.Name) ? "Namn är obligatoriskt." : null;
        _groupError = EditModel.AccountGroupId <= 0 ? "Välj kontogrupp." : null;

        return _codeError is null && _nameError is null && _groupError is null;
    }

    private async Task<bool> CreateOrUpdateAsync(int id, PostAccountDTO dto)
    {
        if (id == 0)
            return await Dispatcher.Send(new CreateAccountCommand(dto)) > 0;

        return await Dispatcher.Send(new UpdateAccountCommand(dto, id));
    }
}
