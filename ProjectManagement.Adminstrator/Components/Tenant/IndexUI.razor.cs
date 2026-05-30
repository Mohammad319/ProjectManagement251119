using BlazorMHD.UI.Components.Feedback.Dialog;
﻿using ContextMenuMHD;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ProjectManagement.Adminstrator.Constants;
using ProjectManagement.Adminstrator.Shared.ResourceFiles;
using ProjectManagement.Adminstrator.Shared.ResourceFiles.AppControll;
using ProjectManagement.Adminstrator.Services.MHDBlazor;
using ProjectManagement.Shared.DTO.Tenant;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Adminstrator.Components.Tenant
{
    public partial class IndexUI : AppComponentBase
    {


        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [Parameter] public EventCallback<bool> Callback { get; set; }
        List<GetTenantsDTO> Tenants = [];
        GetTenantsDTO? DetailsPage;
        bool CanManageTenants;

        void UpdateForm(GetTenantsDTO model) {

            Modal.ShowComponent<FormUI>(model.Id == 0 ?
                AppLoc[LocalizerConst.New, AppControll.tenant] : ResourceApp.update,
                new Dictionary<string, object>
                {
                    [nameof(FormUI.Id)] = model.Id,
                    [nameof(FormUI.Callback)] = EventCallback.Factory.Create<bool>(this, CallBack)
                }, DialogSize.ExtraLarge);
        } 

        void Remove(GetTenantsDTO obj)
        {
            MHD.DeleteMessage(obj.Name, EventCallback.Factory.Create(this, () => RemoveAsync(obj)));
        }

        async Task RemoveAsync(GetTenantsDTO obj)
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
            MHD.MessageYesNo(AppControll.block,
                block ? AppControll.confirmTenantBlock : AppControll.confirmTenantBlockout,
                MhdState.Warning,
                EventCallback.Factory.Create(this, () => ConfirmBlockAsync(id, block)));
        }

        async Task ConfirmBlockAsync(int id, bool block)
        {
            await GetTenantsAsync();
        }

        async Task CallBack(bool refresh)
        {
            Modal.Close();
            if (refresh)
            {
                await GetTenantsAsync();
            }
        }

        async Task GetTenantsAsync()
        {
            Tenants = await ExHandlers.RunCheckTokenAsync(() => sersService.GetAsync()) ?? [];
        }

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var principal = authState.User;
            CanManageTenants = principal.IsInRole(PMRolesConst.APP.Admin) || principal.IsInRole(PMRolesConst.APP.Manger);

            await GetTenantsAsync();
        }

        async Task Context(GetTenantsDTO item)
        {
            var list = new List<MenuItem>
            {
                new() { Label = $"ℹ️ {ResourceLoc.details}", OnClickAsync = () => { DetailsPage = item; return Task.CompletedTask; } }
            };

            if (CanManageTenants)
            {
                list.Add(new() { Label = $"✏️ {ResourceApp.edit}", OnClickAsync = () => { UpdateForm(item); return Task.CompletedTask; } });
                list.Add(new() { Label = $"🗑️ {ResourceApp.delete}", OnClickAsync = () => { Remove(item); return Task.CompletedTask; } });
                list.Add(new() { Label = $"🔓 {AppControll.blockout}", OnClickAsync = () => { TenantBlock(item.Id, true); return Task.CompletedTask; } });
                list.Add(new() { Label = $"🔒 {AppControll.block}", OnClickAsync = () => { TenantBlock(item.Id, false); return Task.CompletedTask; } });
            }

            await ContextService.ShowMenuAsync(list);
        }
    }
}
