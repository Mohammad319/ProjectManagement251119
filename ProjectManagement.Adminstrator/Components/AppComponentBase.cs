using ContextMenuMHD;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using ProjectManagement.Client.Adminstrator.Handless;
using ProjectManagement.Client.Adminstrator.Services.MHDBlazor;

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
