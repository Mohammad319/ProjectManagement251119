using BlazorMHD.UI.Core.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Client.Shared.Components;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.ResourceFiles.Calculation;
using ProjectManagement.Client.Shared.ResourceFiles.Identity;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Pages.Project.ProjectPages
{
    public partial class ProjectForm : AppComponentBase
    {
        public const string DialogFormId = "projectForm";
        [Parameter] public EventCallback<Tuple<bool, ListProjectMVVM>> Callback { get; set; }
        [Parameter] public required ListProjectMVVM Project { get; set; }
        [Parameter] public Guid FolderId { get; set; }

        bool IsLoading = true;

        PostProjectDTO ProjectUpdate = new();
        GetProjectCalcConfigDTO? Config;
        private EditContext? editContext;
        private AddressDTO MainAddress { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            ProjectUpdate.FolderId = FolderId;
            ResetEditContext();

            if (Project.Id != Guid.Empty)
            {
                ProjectUpdate = await Repo.Project.GetToPostAsync(Project.Id)?? new PostProjectDTO();
            }

            ProjectUpdate.Notes ??= [];
            if (ProjectUpdate.Notes.Count == 0)
                ProjectUpdate.Notes.Add(string.Empty);
            ProjectUpdate.Responsibles ??= [];
            ProjectUpdate.Contacts ??= [];
            ProjectUpdate.Address ??= [];
            MainAddress = EnsureMainAddress();

            Config = await
                Repo.Project.GetConfig(
                    ProjectUpdate.ProcurementMethodsId,
                    ProjectUpdate.ContractId,
                    ProjectUpdate.CompensationId,
                    ProjectUpdate.TypeId);

            ResetEditContext();
            IsLoading = false;
        }

        private void ResetEditContext()
        {
            editContext = new EditContext(ProjectUpdate);
            editContext.SetFieldCssClassProvider(RequiredFieldCssClassProvider.Instance);
        }

        private async Task HandleSubmitAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;
            SyncProjectStatusName();

            PostProjectDTO entity = new();
            PropertyCopier.CopyPropertiesTo(ProjectUpdate, entity);

            var resultInfo = Tuple.Create(ProjectUpdate.IsVisible, new ListProjectMVVM());
            PropertyCopier.CopyPropertiesTo(ProjectUpdate, resultInfo.Item2);
            resultInfo.Item2.Status = ProjectUpdate.StatusName;
            resultInfo.Item2.Responsible = ProjectUpdate.Responsibles.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

            bool result;

            if (Project.Id != Guid.Empty)
                result = await
                    Repo.Project.UpdateAsync(Project.Id, entity);
            else
            {
                resultInfo.Item2.Id = await Repo.Project.CreateAsync(entity);

                result = resultInfo.Item2.Id != Guid.Empty;
            }

            if (result)
                await Callback.InvokeAsync(resultInfo);

            MHD.Notifications(Project.Id != Guid.Empty ? ToastType.Update : ToastType.Add, result);

            IsLoading = false;
        }

        private AddressDTO EnsureMainAddress()
        {
            ProjectUpdate.Address ??= [];

            if (ProjectUpdate.Address.Count == 0)
                ProjectUpdate.Address.Add(new AddressDTO());

            return ProjectUpdate.Address[0];
        }

        private void SyncProjectStatusName()
        {
            ProjectUpdate.StatusName = ProjectUpdate.StatusId.HasValue
                ? Config?.Statuses?.FirstOrDefault(x => x.Id == ProjectUpdate.StatusId.Value)?.Name ?? string.Empty
                : string.Empty;
        }

        private void AddNote() => ProjectUpdate.Notes.Add(string.Empty);

        private void RemoveNote(int idx)
        {
            if (idx >= 0 && idx < ProjectUpdate.Notes.Count && ProjectUpdate.Notes.Count > 1)
                ProjectUpdate.Notes.RemoveAt(idx);
        }

    }
}
