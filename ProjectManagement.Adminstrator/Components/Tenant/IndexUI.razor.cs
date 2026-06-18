using BlazorMHD.UI.Components.Feedback.Dialog;
﻿using BlazorMHD.UI.Core.Navigation;
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

        // Search state
        protected string Search { get; set; } = string.Empty;

        protected IReadOnlyList<GetTenantsDTO> FilteredTenants
        {
            get
            {
                IEnumerable<GetTenantsDTO> all = Tenants;
                if (!string.IsNullOrWhiteSpace(Search))
                {
                    var term = Search.Trim();
                    all = all.Where(t =>
                        (!string.IsNullOrEmpty(t.Name) && t.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrEmpty(t.DB) && t.DB.Contains(term, StringComparison.OrdinalIgnoreCase)));
                }

                return all.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        // Summary stats
        protected int TotalCount => Tenants.Count;
        protected int DedicatedCount => Tenants.Count(t => t.HasOwnDb);
        protected int SharedCount => Tenants.Count(t => !t.HasOwnDb);
        protected int ExpiredCount => Tenants.Count(IsExpired);
        protected int ExpiringSoonCount => Tenants.Count(IsExpiringSoon);
        protected int BlockedCount => Tenants.Count(t => t.IsBlocked);

        protected static bool IsExpired(GetTenantsDTO t)
            => t.DateExpire.HasValue && t.DateExpire.Value < DateTimeOffset.Now;

        protected static bool IsExpiringSoon(GetTenantsDTO t)
            => t.DateExpire.HasValue
               && t.DateExpire.Value >= DateTimeOffset.Now
               && t.DateExpire.Value <= DateTimeOffset.Now.AddDays(30);

        void UpdateForm(GetTenantsDTO model) {

            Modal.ShowComponent<FormUI>(model.Id == 0 ?
                AppLoc[LocalizerConst.New, AppControll.tenant] : ResourceApp.update,
                new Dictionary<string, object>
                {
                    [nameof(FormUI.Id)] = model.Id,
                    [nameof(FormUI.Callback)] = EventCallback.Factory.Create<bool>(this, CallBack)
                }, MhdDialogSize.ExtraLarge);
        } 

        // Shows a confirmation dialog; the actual delete runs in RemoveAsync after the user confirms.
        void AskRemove(GetTenantsDTO obj)
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
            var result = await ExHandlers.RunCheckTokenAsync(() => sersService.BlockTenantAsync(id, block));
            MHD.ToastMessage(AppControll.block, ToastType.Update, result);
            await GetTenantsAsync();
        }

        async Task CallBack(bool refresh)
        {
            await Modal.CloseAsync();
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
            var list = new List<MhdContextMenuItem>
            {
                new() { Label = $"ℹ️ {ResourceLoc.details}", OnClickAsync = () => { DetailsPage = item; return Task.CompletedTask; } }
            };

            if (CanManageTenants)
            {
                list.Add(new() { Label = $"✏️ {ResourceApp.edit}", OnClickAsync = () => { UpdateForm(item); return Task.CompletedTask; } });
                list.Add(new() { Label = $"🗑️ {ResourceApp.delete}", OnClickAsync = () => { AskRemove(item); return Task.CompletedTask; } });
                list.Add(new() { Label = $"🔒 {AppControll.block}", OnClickAsync = () => { TenantBlock(item.Id, true); return Task.CompletedTask; } });
                list.Add(new() { Label = $"🔓 {AppControll.blockout}", OnClickAsync = () => { TenantBlock(item.Id, false); return Task.CompletedTask; } });
            }

            await ContextService.ShowMenuAsync(list);
        }
    }
}
