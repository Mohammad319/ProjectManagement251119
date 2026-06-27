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
                    dialogService.CloseAsync();
                    return;
                }

                int newOrder = folderState.FoldersList.Count > 0
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
                    dialogService.CloseAsync();
                    return;
                }

                existing.Name = folder.Name;
                existing.Color = folder.Color;
                existing.IsVisible = folder.IsVisible;
                folderState.UpdateFolder(existing);
            }

            dialogService.CloseAsync();
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

        /// <summary>
        /// Loads the "Alla tillgängliga" scope: every folder the user can reach across departments.
        /// Folders from departments the user has no normal access to arrive tagged as read-only visual
        /// groups (<see cref="FolderMVVM.IsReadOnlyGroup"/>), mapped from the server's IsSharedGroup flag.
        /// </summary>
        public Task LoadAccessibleFoldersAsync() =>
            LoadFoldersAsync(() => folderRepo.GetAccessibleFoldersAsync(ShowArchived));

        public async Task SetProjectsToFolder(FolderMVVM folder)
        {
            if (folder is null) return;

            if (!folder.ProjectsLoaded)
            {
                folder.Projects = await projectRepo.GetByFolderIdAsync(folder.Id, ShowArchived);
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
            // Only expand folders that actually have projects to show.
            folder.ShowProjects = (folder.Projects?.Count ?? 0) > 0;
        }

        public async Task SetCalcsToProject(ListProjectMVVM project)
        {
            if (project is null) return;

            if (!project.CalculationsLoaded)
            {
                var all = await calcRepo.GetAsync(project.Id, isArchived: false);
                if (ShowArchived)
                    all.AddRange(await calcRepo.GetAsync(project.Id, isArchived: true));

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
            // Only expand projects that actually have calculations to show.
            project.ShowCalculations = (project.Calculations?.Count ?? 0) > 0;
        }
    }
}
