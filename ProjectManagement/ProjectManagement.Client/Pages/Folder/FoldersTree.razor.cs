using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ProjectManagement.Client.Helper.DropDown;
using ProjectManagement.Client.Pages.Folder.Component;
using ProjectManagement.Client.Shared.Model.Project;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.ResourceFiles;
using ProjectManagement.Shared.Constant;
using System.Text.Json;

namespace ProjectManagement.Client.Pages.Folder
{
    public partial class FoldersTree : IDisposable
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private const string LastSelectionKey = "LastSelection";

        private class LastSelection
        {
            public Guid FolderId { get; set; }
            public Guid? ProjectId { get; set; }
            public int? CalculationId { get; set; }
        }
        private async Task SeFolder(FolderMVVM folder)
        {
            await Folder.NewFolder(folder);
            await SaveLastSelection(folder, null, null);
        }
        private async Task SaveLastSelection(FolderMVVM folder, ListProjectMVVM? project = null, ListCalculationMVVM? calculation = null)
        {
            var selection = new LastSelection
            {
                FolderId = folder.Id,
                ProjectId = project?.Id,
                CalculationId = calculation?.Id
            };

            var json = JsonSerializer.Serialize(selection);
            await JS.InvokeVoidAsync("localStorage.setItem", LastSelectionKey, json);
        }
        private bool _initializedFromLastSelection = false;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !_initializedFromLastSelection)
            {
                _initializedFromLastSelection = true;
                //await RestoreLastSelection();
            }
        }
        private async Task RestoreLastSelection()
        {
            var json = await JS.InvokeAsync<string?>("localStorage.getItem", LastSelectionKey);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var selection = JsonSerializer.Deserialize<LastSelection>(json);
            if (selection is null)
                return;

            var folder = UoWService.Folder.State.FoldersList?
                .FirstOrDefault(f => f.Id == selection.FolderId);
            if (folder is null)
                return;

            folder.ShowProjects = true;

            ListProjectMVVM? project = null;
            ListCalculationMVVM? calc = null;

            if (selection.ProjectId.HasValue)
            {
                await Folder.SetProjectsToFolder(folder);
                project = folder.Projects?.FirstOrDefault(p => p.Id == selection.ProjectId.Value);
                if (project is not null)
                {
                    project.ShowCalculations = true;
                    await Folder.SetCalcsToProject(project);
                }
            }

            if (selection.CalculationId.HasValue && project is not null)
            {
                calc = project.Calculations?.FirstOrDefault(c => c.Id == selection.CalculationId.Value);
                if (calc is not null)
                {
                    await CalcService.SetCalc(calc.Id, project, folder);
                }
            }
        }
        private object? CalcDraging { get; set; }

        protected override void OnInitialized()
        {
            UoWService.Folder.State.OnChange += Refresh;
        }

        public void Dispose()
        {
            UoWService.Folder.State.OnChange -= Refresh;
        }
        public void Refresh() => InvokeAsync(StateHasChanged);

        private async Task CollapseFolder(FolderMVVM folder)
        {
            await Folder.SetProjectsToFolder(folder);
            folder.ShowProjects = !folder.ShowProjects;
        }

        private async Task CollapseProject(ListProjectMVVM project)
        {
            await Folder.SetCalcsToProject(project);
            project.ShowCalculations = !project.ShowCalculations;
        }

        private async Task SetProject(FolderMVVM folder, ListProjectMVVM project)
        {
            await Folder.SetCalcsToProject(project);
            Folder.State.SetCalculation(null, project, folder);
            project.ShowCalculations = true;
            await SaveLastSelection(folder, project, null);
        }

        private async Task NewCalculations(FolderMVVM folder, ListProjectMVVM project, ListCalculationMVVM calculation)
        {
            await CalcService.SetCalc(calculation.Id, project, folder);
            await SaveLastSelection(folder, project, calculation);
        }

        private async Task HandleDrop(FolderMVVM folder)
        {
            if (CalcDraging is not FolderMVVM draggingFolder)
                return;

            draggingFolder.IsDragOver = false;
            folder.IsDragOver = false;

            double order = DropDownHelper.HandleDrop(folder, draggingFolder, UoWService.Folder.State.FoldersList);
            if (order > -1)
            {
                UoWService.Folder.State.SortFoldersDescending();
                await Repo.Folder.ReOrderAsync(draggingFolder.Id, draggingFolder.Order);
                await SaveLastSelection(draggingFolder, null, null);
            }

            CalcDraging = null;
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleDrop(ListProjectMVVM project)
        {
            if (CalcDraging is not ListProjectMVVM draggingProject)
                return;

            draggingProject.IsDragOver = false;
            project.IsDragOver = false;

            var folder = UoWService.Folder.State.FoldersList
                .FirstOrDefault(f => f.Projects != null && f.Projects.Any(p => p.Id == project.Id));
            if (folder is null) return;

            double order = DropDownHelper.HandleDrop(project, draggingProject, folder.Projects);
            if (order > -1)
            {
                folder.Projects = folder.Projects.OrderByDescending(x => x.Order).ToList();
                await Repo.Project.ReOrderAsync(draggingProject.Id, draggingProject.Order);
            }

            CalcDraging = null;
            await InvokeAsync(StateHasChanged);
        }

        // drag enter/leave/over للمجلدات
        private void OnDragEnterFolder(FolderMVVM folder)
        {
            if (CalcDraging is FolderMVVM dragging && dragging != folder)
                folder.IsDragOver = true;
        }

        private void OnDragLeaveFolder(FolderMVVM folder)
        {
            folder.IsDragOver = false;
        }

        private void ModalForm(FolderModel model) =>
            Modal.ShowComponent<FolderFormUI>(
                model.Id == Guid.Empty
                    ? AppLoc[LocalizerConst.New, ResourceLoc.folder]
                    : AppLoc[LocalizerConst.Update, model.Name],
                Icons.Folder,
                new Dictionary<string, object>
                {
                    [nameof(FolderFormUI.FolderForm)] = model,
                    [nameof(FolderFormUI.OnClickCallback)] =
                        EventCallback.Factory.Create(this, (FolderModel f) => UoWService.Folder.AddOrUpdateFolder(f))
                }
            );

        private void ModalForm(FolderMVVM model) =>
            Modal.ShowComponent<DetailsUI>(
                model.Name,
                Icons.Details,
                new Dictionary<string, object>
                {
                    [nameof(DetailsUI.Id)] = model.Id,
                    [nameof(DetailsUI.CallBack)] = EventCallback.Factory.Create(this, Modal.Close)
                });

        private void UpdateForm(FolderMVVM folder) =>
            ModalForm(new FolderModel
            {
                Name = folder.Name,
                Color = folder.Color,
                Id = folder.Id
            });

        private async Task Context(FolderMVVM item)
        {
            List<MenuItem> list = [];

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            bool isInAnyRole = PMRolesConst.Tenant.AdminManger.Split(',').Any(r => user.IsInRole(r));

            if (!Folder.State.OtherDepartment && user.Identity?.IsAuthenticated == true && isInAnyRole)
            {
                list.Add(new()
                {
                    IconHtml = Icons.Edit,
                    Label = ResourceApp.edit,
                    OnClickAsync = () =>
                    {
                        UpdateForm(item);
                        return Task.CompletedTask;
                    }
                });

                list.Add(new()
                {
                    IconHtml = Icons.Delete,
                    Label = ResourceApp.delete,
                    OnClickAsync = () =>
                    {
                        UoWService.Folder.RemoveFolder(item);
                        return Task.CompletedTask;
                    }
                });
            }

            list.Add(new()
            {
                IconHtml = Icons.Details,
                Label = ResourceLoc.details,
                OnClickAsync = () =>
                {
                    ModalForm(item);
                    return Task.CompletedTask;
                }
            });

            await ContextService.ShowMenuAsync(list);
        }
    }
}
