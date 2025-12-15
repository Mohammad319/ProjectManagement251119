using Application.Feature.Organisation.OrganisationCategory.Commands;
using Application.Feature.Organisation.OrganisationCategory.Queries;
using Domain.DTO.Category;
using Domain.Entities.Organisation;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Client.Shared.ResourceFiles.APP;
using ProjectManagement.Shared.Constant;

namespace ProjectManagement.Components.ControlComponents.Organisation.OrganisationCategory
{
    public partial class CategoriesUI
    {
        async Task Context(ListOrganisationCategoryDTO item)
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            bool isInAnyRole = PMRolesConst.Tenant.AdminSuperManger.Split(',').Any(r => user.IsInRole(r));
            List<MenuItem> list = [];
            if (item.ParentCategoryId == null)
                list.Add(new MenuItem { Label = $"➕ {AppLoc[LocalizerConst.New, ResourceLoc.category]}", OnClickAsync = () => {ModalForm(new ListOrganisationCategoryDTO() { ParentCategoryId = item.Id });
                    return Task.CompletedTask;
                }
                });
            list.Add(new MenuItem { Label = $"✏️ {ResourceApp.update}", OnClickAsync = () => {ModalForm(item);
                return Task.CompletedTask;
            }
            });
            list.Add(new MenuItem { Label = $"🗑️ {ResourceApp.delete}", OnClickAsync = () => {Remove(item);
                return Task.CompletedTask;
            }
            });
            if (user.Identity?.IsAuthenticated == true && isInAnyRole)
            {
                await ContextService.ShowMenuAsync(list);
            }
        }
        int PageNr = 0;
        List<ListOrganisationCategoryDTO> Categories = [];
        void ModalForm(ListOrganisationCategoryDTO model) =>
            MHD.Modal.ShowComponent<CategoryFormUI>(model.Id != 0 ? AppLoc[LocalizerConst.Update, model.Name] : AppLoc[LocalizerConst.New, ResourceLoc.group]
                , new Dictionary<string, object> { [nameof(CategoryFormUI.OrganisationCategory)] = model, [nameof(CategoryFormUI.Callback)] = EventCallback.Factory.Create<bool>(this, Callback) });

        ListOrganisationCategoryDTO? SelectedCategory;
        async Task GetCompaniesAsync(ListOrganisationCategoryDTO catID)
        {
            SelectedCategory = null;
            PageNr = 2;
            await Task.Delay(1);
            SelectedCategory = catID;
        }
        async Task GetCategories()
        {
            //DialogService.ClearModal();
            Categories = null;
            Categories = await MicroBus.Send(new GetOrganisationCategoryQuery());
        }
        protected async override Task OnInitializedAsync()
        {
            await GetCategories();
        }
        void Remove(ListOrganisationCategoryDTO category)
        {
            MHD.DeleteMessage(category.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(category)));
        }
        async Task ConfirmRemoveAsync(ListOrganisationCategoryDTO st)
        {
            bool result = await MicroBus.Send(new DeleteOrganisationCategoryCommand(st.Id));
            if (result)
            {
                if (Categories.Any(x => x.Id == st.Id))
                    Categories.Remove(st);
            }
            MHD.Notifications(ToastType.Delete, result);
            StateHasChanged();
        }
        async Task Callback(bool isSuccess)
        {
            MHD.Modal.Close();
            if (isSuccess)
                await GetCategories();
            StateHasChanged();
        }
    }
}
