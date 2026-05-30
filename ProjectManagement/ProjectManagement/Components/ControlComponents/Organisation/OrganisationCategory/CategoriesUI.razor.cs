using Application.Feature.Organisation.OrganisationCategory.Commands;
using Application.Feature.Organisation.OrganisationCategory.Queries;
using Domain.DTO.Category;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Components.ControlComponents.Organisation.OrganisationCategory
{
    public partial class CategoriesUI
    {
        private int PageNr = 1;

        private List<ListOrganisationCategoryDTO> Categories { get; set; } = [];
        private ILookup<int?, ListOrganisationCategoryDTO> _byParent = default!;

        private ListOrganisationCategoryDTO? SelectedCategory;

        protected override async Task OnInitializedAsync()
        {
            await LoadCategoriesAsync();
        }

        private void RebuildIndex()
        {
            _byParent = Categories.ToLookup(x => x.ParentCategoryId);
        }

        private async Task LoadCategoriesAsync()
        {
            Categories = await Dispatcher.Send(new GetOrganisationCategoryQuery()) ?? [];
            RebuildIndex();
            await InvokeAsync(StateHasChanged);
        }

        private async Task<bool> CanManageAsync()
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated != true)
                return false;

            return PMRolesConst.Tenant.AdminManger
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(user.IsInRole);
        }

        private async Task Context(ListOrganisationCategoryDTO item)
        {
            if (!await CanManageAsync())
                return;

            List<MenuItem> list = [];

            if (item.ParentCategoryId is null)
            {
                list.Add(new MenuItem
                {
                    Label = $"➕ {AppLoc[LocalizerConst.New, ResourceLoc.category]}",
                    OnClickAsync = () =>
                    {
                        ModalForm(new ListOrganisationCategoryDTO { ParentCategoryId = item.Id });
                        return Task.CompletedTask;
                    }
                });
            }

            list.Add(new MenuItem
            {
                Label = $"✏️ {AppLoc[nameof(ResourceApp.update)]}",
                OnClickAsync = () =>
                {
                    ModalForm(item);
                    return Task.CompletedTask;
                }
            });

            list.Add(new MenuItem
            {
                Label = $"🗑️ {AppLoc[nameof(ResourceApp.delete)]}",
                OnClickAsync = () =>
                {
                    Remove(item);
                    return Task.CompletedTask;
                }
            });

            await ContextService.ShowMenuAsync(list);
        }

        private void ModalForm(ListOrganisationCategoryDTO model) =>
            MHD.Modal.ShowComponent<CategoryFormUI>(
                model.Id != 0
                    ? AppLoc[LocalizerConst.Update, model.Name]
                    : AppLoc[LocalizerConst.New, ResourceLoc.category],
                new Dictionary<string, object>
                {
                    [nameof(CategoryFormUI.OrganisationCategory)] = model,
                    [nameof(CategoryFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, CallbackAsync)
                }, BlazorMHD.UI.Core.Services.DialogSize.Large);

        private void Remove(ListOrganisationCategoryDTO category)
        {
            MHD.DeleteMessage(category.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(category)));
        }

        private Task OnOrganisationsClosed()
        {
            SelectedCategory = null;
            PageNr = 1;
            return Task.CompletedTask;
        }

        private async Task ConfirmRemoveAsync(ListOrganisationCategoryDTO st)
        {
            bool result = await Dispatcher.Send(new DeleteOrganisationCategoryCommand(st.Id));

            if (result)
            {
                Categories.RemoveAll(x => x.Id == st.Id || x.ParentCategoryId == st.Id);
                if (SelectedCategory?.Id == st.Id || SelectedCategory?.ParentCategoryId == st.Id)
                {
                    SelectedCategory = null;
                    PageNr = 1;
                }
                RebuildIndex();
            }

            MHD.Notifications(ToastType.Delete, result);
            await InvokeAsync(StateHasChanged);
        }

        private async Task CallbackAsync(bool isSuccess)
        {
            MHD.Modal.Close();

            if (isSuccess)
                await LoadCategoriesAsync();

            await InvokeAsync(StateHasChanged);
        }

        private void OpenTypePage()
        {
            PageNr = 1;
            SelectedCategory = null;
        }

        private void OnTreeSelect(ListOrganisationCategoryDTO cat)
        {
            PageNr = 2;
            SelectedCategory = cat;
        }

        private async Task OnTreeContext(ListOrganisationCategoryDTO cat)
        {
            await Context(cat);
        }
    }
}
