using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ProjectManagement.Client.Pages.Project.ProjectPages;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Shared.Constant;
using System.Security.Claims;

namespace ProjectManagement.Client.Pages.Folder
{
    public partial class FolderIndex : IDisposable
    {
        bool SideBarVisible { get; set; } = true;
        bool Admin { get; set; }

        void ModalSeachForm() =>
        //Modal.AddModal<ProjectsSearch>(
        //    "",
        //    new Dictionary<string, object>
        //    {
        //        [nameof(ProjectsSearch.CallBack)] = EventCallback.Factory.Create(this, Modal.ClearModal)
        //    });
        Modal.Show(new DialogModel
        {
            Title = "Search Projects",
            Content = builder =>
            {
                builder.OpenComponent(0, typeof(ProjectsSearch));
                builder.AddAttribute(1, "CallBack",
                    EventCallback.Factory.Create(this, () =>
                    {
                        // هنا ما كان Modal.ClearModal
                        // في النظام الجديد: نغلق الـ Dialog
                        Modal.Close();
                    }));
                builder.CloseComponent();
            },
            Buttons = [], Size = DialogSize.ExtraLarge, IsDraggable = true, CloseOnOverlayClick = true
        });

        void ModalForm(FolderModel model) =>
            Modal.ShowComponent<FolderFormUI>(
                model.Id == Guid.Empty ? AppLoc[LocalizerConst.New, ResourceLoc.folder] : AppLoc[LocalizerConst.Update, model.Name],
                Icons.Folder,
                new Dictionary<string, object>
                {
                    [nameof(FolderFormUI.FolderForm)] = model,
                    [nameof(FolderFormUI.OnClickCallback)] = EventCallback.Factory.Create(this, (FolderModel f) => Folder.AddOrUpdateFolder(f))
                });

        public void Dispose()
        {
            UoWService.Folder.State.OnChange -= Refresh;
        }

        public void Refresh() => InvokeAsync(StateHasChanged);

        protected override async Task OnInitializedAsync()
        {
            UoWService.Folder.State.OnChange += Refresh;

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            ClaimsPrincipal user = authState.User;

            if (user.IsInRole(PMRolesConst.Tenant.Admin))
            {
                Admin = true;
                Folder.State.SetDepartments(await Repo.Departments.GetDepartmentsAsListAsync());
            }
            else
            {
                await Folder.LoadPrivateAndGroupFoldersAsync();
            }
        }

        async Task GetDepartmentAsync()
        {
            Folder.State.ClearFolders();
            Folder.State.SetOtherDepartment(!Folder.State.OtherDepartment);

            if (Folder.State.OtherDepartment)
            {
                Folder.State.SetDepartments(
                    await Repo.Departments.GetDepartmentsAsListAsync());
            }
            else
            {
                await Folder.LoadPrivateAndGroupFoldersAsync();
            }
        }
    }
}
