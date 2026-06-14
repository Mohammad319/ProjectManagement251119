using BlazorMHD.UI.Core.Services;
using BlazorMHD.UI.Core.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Adminstrator.Handless;
using ProjectManagement.Adminstrator.Services.MHDBlazor;

namespace ProjectManagement.Adminstrator.Components
{
    public abstract class AppComponentBase : ComponentBase
    {
        [Inject] protected IStringLocalizer<ResourceApp> AppLoc { get; set; } = default!;
        protected DialogService Modal => MHD.Modal;
        [Inject] protected ContextMenuService ContextService { get; set; } = default!;
        [Inject] protected IExceptionHandlers ExHandlers { get; set; } = default!;
        [Inject] protected MhdServices MHD { get; set; } = default!;
    }
}
