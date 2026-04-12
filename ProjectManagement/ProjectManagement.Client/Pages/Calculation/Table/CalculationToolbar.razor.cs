using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles;

namespace ProjectManagement.Client.Pages.Calculation.Table;

public partial class CalculationToolbar : ComponentBase, IDisposable
{
    [Inject] private ICalculationTableCoordinator TableCoordinator { get; set; } = default!;
    [Inject] private IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
    [Inject] private CalculationService CalcService { get; set; } = default!;
    [Inject] private CalculationInteractionState InteractionState { get; set; } = default!;
    [Inject] private FolderState FolderState { get; set; } = default!;
    [Inject] private ITemplateRepository TemplateRepository { get; set; } = default!;

    private Action? _onInteractionChanged;
    private List<TemplateMVVM> TemplateOptions { get; set; } = [];
    private bool IsLoadingTemplates { get; set; } = true;
    private bool IsUpdatingTemplate { get; set; }

    private CalculationMVVM Calc => FolderState.Calculation ?? throw new InvalidOperationException("CalculationToolbar requires an active calculation.");

    private bool CanPasteTasks => TableCoordinator.CanPaste(CalculationItemType.task);
    private int? CurrentTemplateId => Calc.TemplateId.GetValueOrDefault() > 0 ? Calc.TemplateId : null;
    private bool IsTemplateSelectionDisabled => IsLoadingTemplates || IsUpdatingTemplate;
    private string SelectedTemplateValue => CurrentTemplateId?.ToString() ?? string.Empty;

    protected override void OnInitialized()
    {
        _onInteractionChanged = () => _ = InvokeAsync(StateHasChanged);
        InteractionState.Changed += _onInteractionChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadTemplateOptionsAsync();
    }

    private async Task LoadTemplateOptionsAsync()
    {
        IsLoadingTemplates = true;

        var templates = await TemplateRepository.GetAsync() ?? [];

        if (CurrentTemplateId is int currentTemplateId
            && currentTemplateId > 0
            && templates.All(x => x.Id != currentTemplateId)
            && !string.IsNullOrWhiteSpace(Calc.Template?.Name))
        {
            templates.Insert(0, Calc.Template);
        }

        TemplateOptions =
            [.. templates
                .Where(x => x.Id > 0)
                .GroupBy(x => x.Id)
                .Select(x => x.First())];

        IsLoadingTemplates = false;
    }

    private async Task OnTemplateChangedAsync(ChangeEventArgs args)
    {
        int? newTemplateId = null;
        var rawValue = args.Value?.ToString();

        if (!string.IsNullOrWhiteSpace(rawValue))
        {
            if (!int.TryParse(rawValue, out var parsedId))
                return;

            newTemplateId = parsedId;
        }

        if (CurrentTemplateId == newTemplateId)
            return;

        IsUpdatingTemplate = true;

        try
        {
            var updatedTemplate = await TemplateRepository.SetDefaultAsync(Calc.Id, newTemplateId);
            Calc.Template = updatedTemplate ?? new TemplateMVVM();
            Calc.TemplateId = newTemplateId;

            if (newTemplateId is int selectedTemplateId
                && selectedTemplateId > 0
                && TemplateOptions.All(x => x.Id != selectedTemplateId)
                && !string.IsNullOrWhiteSpace(Calc.Template.Name))
            {
                TemplateOptions = [Calc.Template, .. TemplateOptions];
            }

            Calc.NotifyGridRefresh();
        }
        finally
        {
            IsUpdatingTemplate = false;
        }
    }

    public void Dispose()
    {
        if (_onInteractionChanged != null)
            InteractionState.Changed -= _onInteractionChanged;
    }
}
