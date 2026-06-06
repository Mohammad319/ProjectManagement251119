using BlazorMHD.UI.Core.Services;
using Microsoft.AspNetCore.Components;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.Repositories.Calculation;
using ProjectManagement.Client.Shared.Repositories.Folder;
using ProjectManagement.Client.Shared.Repositories.Project;
using ProjectManagement.Shared.Helper;

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
        public bool ShowArchived { get; private set; }

        public async Task EnsureLoadedAsync()
        {
            if (_isLoaded) return;
            await LoadFoldersAsync(() => folderRepo.GetByVisible(ShowArchived));
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
                if (!ShowArchived && !folder.IsVisible)
                {
                    dialogService.Close();
                    return;
                }

                int newOrder = folderState.FoldersList.Any()
                    ? folderState.FoldersList.Max(f => f.Order) + 100
                    : 0;

                var newFolder = new FolderMVVM
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Color = folder.Color,
                    Order = newOrder,
                    IsVisible = folder.IsVisible
                };

                folderState.AddFolder(newFolder);
            }
            else
            {
                if (!ShowArchived && !folder.IsVisible)
                {
                    folderState.RemoveFolder(existing);
                    dialogService.Close();
                    return;
                }

                existing.Name = folder.Name;
                existing.Color = folder.Color;
                existing.IsVisible = folder.IsVisible;
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
            LoadFoldersAsync(() => folderRepo.GetByVisible(ShowArchived));

        public async Task ToggleArchivedFoldersAsync()
        {
            ToggleArchivedFilter();
            await LoadFoldersAsync(() => folderRepo.GetByVisible(ShowArchived));
        }

        public void ToggleArchivedFilter() => ShowArchived = !ShowArchived;

        public Task LoadFoldersByDepartmentAsync(string departmentId)
        {
            if (!int.TryParse(departmentId, out int id) || id <= 0)
            {
                folderState.ClearFolders();
                return Task.CompletedTask;
            }

            return LoadFoldersAsync(() => folderRepo.GetByDepartmentAsync(id, ShowArchived));
        }

        public async Task SetProjectsToFolder(FolderMVVM folder)
        {
            if (folder is null) return;

            if (!folder.ProjectsLoaded)
            {
                folder.Projects = folderState.OtherDepartment
                    ? await projectRepo.GetOtherDepartmentAsync(folder.Id, ShowArchived)
                    : await projectRepo.GetByFolderIdAsync(folder.Id, ShowArchived);
                folder.ProjectsLoaded = true;
            }

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

            if (!project.CalculationsLoaded)
            {
                List<ProjectManagement.Client.Shared.MVVM.Calculation.ListCalculationMVVM> all;
                if (folderState.OtherDepartment)
                {
                    all = await calcRepo.GetShareCalculationsAsync(project.Id);
                    all = CalculationVersionSelector
                        .FilterFamiliesByCurrentVisibility(all, ShowArchived)
                        .ToList();
                }
                else
                {
                    all = await calcRepo.GetAsync(project.Id);
                    if (ShowArchived)
                        all.AddRange(await calcRepo.GetAsync(project.Id, isArchived: true));
                }

                project.Calculations = all.ToList();
                project.CalculationCount = CalculationVersionSelector.CountCurrentVersions(project.Calculations);
                project.CalculationsLoaded = true;
            }

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
