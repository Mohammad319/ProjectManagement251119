using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Shared;

namespace ProjectManagement.Components.Shared;

/// <summary>
/// Base class to unify localization access (ResourceApp/CalcResource/FormatResource)
/// and reduce repetitive injections across components.
/// </summary>
public abstract class AppComponentBase : ComponentBase
{
    // Core app resources
    [Inject] protected IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
    [Inject] protected IStringLocalizer<ResourceLoc> Loc { get; set; } = default!;

    // Calculation resources (if you use CalcResource in many components)
    [Inject] protected IStringLocalizer<CalcResource> CalcLoc { get; set; } = default!;
    [Inject] protected IStringLocalizer<PMAppFormatResource> FormatLoc { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected MhdServices MHD { get; set; } = default!;
    [Inject] protected ICommandDispatcher Dispatcher { get; set; } = default!;
    [Inject] protected ContextMenuService ContextService { get; set; } = default!;
}
