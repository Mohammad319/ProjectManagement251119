using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.Repositories.Folder;
using ProjectManagement.Client.Shared.Repositories.Project;

namespace ProjectManagement.Client.Services.Folder
{
    public class FolderService(
        IFolderRepository folderRepo,
        MhdServices mhd,
        IProjectRepository projectRepo,
        ICalculationRepository calcRepo,
        FolderState folderState, DialogService dialogService)
    {
        public FolderState State => folderState;

        private bool _isLoaded;
        public bool IsVisible { get; set; } = true;

        public async Task EnsureLoadedAsync()
        {
            if (_isLoaded) return;
            await LoadFoldersAsync(() => folderRepo.GetByVisible(IsVisible));
        }

        public async Task LoadFoldersAsync(Func<Task<List<FolderMVVM>>> loadFunc)
        {
            var folders = await loadFunc() ?? [];
            folderState.SetFolders(folders);
            _isLoaded = true;
        }

        public void AddOrUpdateFolder(FolderModel folder)
        {
            if (folder == null) return;

            var existing = folderState.FoldersList.FirstOrDefault(f => f.Id == folder.Id);
            if (existing == null)
            {
                int newOrder = folderState.FoldersList.Any()
                    ? folderState.FoldersList.Max(f => f.Order) + 100
                    : 0;

                var newFolder = new FolderMVVM
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Color = folder.Color,
                    Order = newOrder
                };

                folderState.AddFolder(newFolder);
            }
            else
            {
                existing.Name = folder.Name;
                existing.Color = folder.Color;
                folderState.UpdateFolder(existing);
            }

            dialogService.Close();
        }

        public void RemoveFolder(FolderMVVM folder)
        {
            if (folder is null) return;

            mhd.DeleteMessage(folder.Name, EventCallback.Factory.Create(this, () => ConfirmRemoveAsync(folder)));
        }

        private async Task ConfirmRemoveAsync(FolderMVVM folder)
        {
            bool result = await folderRepo.DeleteAsync(folder.Id);
            if (result)
            {
                folderState.RemoveFolder(folder);
            }

            mhd.Notifications(ToastType.Delete, result);
        }

        public Task LoadPrivateAndGroupFoldersAsync() =>
            LoadFoldersAsync(() => folderRepo.GetByVisible(IsVisible));

        public async Task LoadHiddenVisibleFoldersAsync()
        {
            IsVisible = !IsVisible;
            await LoadFoldersAsync(() => folderRepo.GetByVisible(IsVisible));
        }

        public Task LoadFoldersByDepartmentAsync(string departmentId)
        {
            if (!int.TryParse(departmentId, out int id) || id <= 0)
            {
                folderState.ClearFolders();
                return Task.CompletedTask;
            }

            return LoadFoldersAsync(() => folderRepo.GetByDepartmentAsync(id));
        }

        public async Task SetProjectsToFolder(FolderMVVM folder)
        {
            if (folder is null) return;

            folder.Projects ??= folderState.OtherDepartment
                ? await projectRepo.GetOtherDepartmentAsync(folder.Id)
                : await projectRepo.GetByFolderIdAsync(folder.Id);

            if (folder.Projects != null)
                folder.Projects = folder.Projects.OrderByDescending(x => x.Order).ToList();
        }

        public async Task NewFolder(FolderMVVM folder)
        {
            if (folder is null) return;

            await SetProjectsToFolder(folder);
            folderState.SetCalculation(null, null, folder);
            folder.ShowProjects = true;
        }

        public async Task SetCalcsToProject(ListProjectMVVM project)
        {
            if (project is null) return;

            project.Calculations ??= folderState.OtherDepartment
                ? await calcRepo.GetShareCalculationsAsync(project.Id)
                : await calcRepo.GetAsync(project.Id);

            if (project.Calculations != null)
                project.Calculations = project.Calculations.OrderByDescending(x => x.Order).ToList();
        }

        public async Task NewProject(ListProjectMVVM project)
        {
            if (project is null || State.FolderSelected is null) return;

            await SetCalcsToProject(project);
            folderState.SetCalculation(null, project, State.FolderSelected);
            project.ShowCalculations = true;
        }
    }
}
