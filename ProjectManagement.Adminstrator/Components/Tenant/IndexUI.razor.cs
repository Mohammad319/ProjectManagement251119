using ContextMenuMHD;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Adminstrator.Constants;
using ProjectManagement.Adminstrator.Shared.ResourceFiles;
using ProjectManagement.Adminstrator.Shared.ResourceFiles.AppControll;
using ProjectManagement.Client.Adminstrator.Services.MHDBlazor;
using ProjectManagement.Shared.DTO.Tenant;
namespace ProjectManagement.Adminstrator.Components.Tenant
{
    public partial class IndexUI : AppComponentBase
    {
        [Parameter] public EventCallback<bool> Callback { get; set; }
        List<GetTenantsDTO> Tenants;
        GetTenantsDTO DetailsPage;
        void UpdateForm(GetTenantsDTO model) => Modal.AddModal<FormUI>(model.Id == 0 ?
            AppLoc[LocalizerConst.New, AppControll.tenant] : ResourceApp.update,
                new Dictionary<string, object>
                {
                    [nameof(FormUI.Id)] = model.Id,
                    [nameof(FormUI.Callback)] = EventCallback.Factory.Create<bool>(this, CallBack)
                });

        void Remove(GetTenantsDTO obj)
        {
            MHD.DeleteMessage(obj.Name, () => RemoveAsync(obj));
        }
        async System.Threading.Tasks.Task RemoveAsync(GetTenantsDTO obj)
        {
            bool result = await sersService.RemoveTenant(obj.Id);
            if (result)
            {
                Tenants?.Remove(obj);
            }
            MHD.ToastMessage(ResourceApp.delete, ToastType.Delete, result);
        }
        void TenantBlock(int id, bool block)
        {
            MHD.MessageYesNo(AppControll.block, block ?
                AppControll.confirmTenantBlock : AppControll.confirmTenantBlockout, MhdState.Warning, () => ConfirmBlockAsync(id, block));
        }
        async System.Threading.Tasks.Task ConfirmBlockAsync(int id, bool block)
        {
           //if (await ExHandlers.RunCheckTokenAsync(() => TenantRepo.BlockTenantAsync(id, block)))
            await GetTenantsAsync();
        }
        async System.Threading.Tasks.Task CallBack(bool refresh)
        {
            Modal.CloseLastModal();
            if (refresh)
                await GetTenantsAsync();
        }

        async System.Threading.Tasks.Task GetTenantsAsync()
        {
            Tenants = null;
            Tenants = await ExHandlers.RunCheckTokenAsync(() => sersService.GetAsync());
        }
        protected async override System.Threading.Tasks.Task OnInitializedAsync()
        {
            await GetTenantsAsync();
        }
        async System.Threading.Tasks.Task Context(GetTenantsDTO item)
        {
            var list = new List<MenuItem>()
            {
                        new() { Label = $"ℹ️ {ResourceLoc.details}", OnClickAsync = () =>{ DetailsPage=item ; return Task.CompletedTask; }},
                        new() { Label = $"✏️ {ResourceApp.edit}", OnClickAsync = () =>{ UpdateForm(item); return Task.CompletedTask; } },
                        new() { Label = $" {ResourceApp.delete}", OnClickAsync = () => {Remove(item); return Task.CompletedTask; } },
                        new() { Label = $"🔓 {AppControll.blockout}", OnClickAsync = () => {TenantBlock(item.Id,true); return Task.CompletedTask; } },
                        new() { Label = $"🔒 {AppControll.block}", OnClickAsync = () => {TenantBlock(item.Id,false); return Task.CompletedTask;
        }
    },
            };
            await ContextService.ShowMenuAsync(list);
        }

    }
}
