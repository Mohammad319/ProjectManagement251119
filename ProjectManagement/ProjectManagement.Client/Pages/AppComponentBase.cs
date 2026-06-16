using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Services;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Client.Services.Calculation;
using ProjectManagement.Client.Services.Folder;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.Repositories;

namespace ProjectManagement.Client.Pages
{
    public abstract class AppComponentBase : ComponentBase
    {
        [Inject] protected IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
        [Inject] protected IStringLocalizer<ResourceStorage> StorageLoc { get; set; } = default!;
        [Inject] protected IStringLocalizer<CalcResource> CalcLoc { get; set; } = default!;

        // 🟩 Common Services
        [Inject] protected IClientLogger ClientLog { get; set; } = default!;
        protected MhdServices MHD => UoWService.Mhd;
        // Single source for the dialog service (same scoped instance the facade uses).
        protected DialogService Modal => MHD.Modal;
        protected CalculationMVVM Calc => Folder?.State?.Calculation ?? default!;
        protected TemplateMVVM Template => Folder?.State?.Calculation?.Template?? default!;
        // 🟩 Repository
        [Inject] protected IUnitOfWorkRepository Repo { get; set; } = default!;

        // 🟨 Injected Services
        [Inject] protected ContextMenuService ContextService { get; set; } = default!;
        [Inject] protected CalculationService CalcService { get; set; } = default!;
        [Inject] protected CalculationInteractionState InteractionState { get; set; } = default!;
        [Inject] protected FolderService Folder { get; set; } = default!;
        [Inject] protected IUnitOfWorkService UoWService { get; set; } = default!;
    }
}
