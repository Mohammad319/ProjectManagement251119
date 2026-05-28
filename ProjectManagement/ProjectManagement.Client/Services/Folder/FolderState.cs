using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Folder;

namespace ProjectManagement.Client.Services.Folder
{
    public class FolderState
    {
        public List<ListDTO> Departments { get; private set; } = [];
        public List<FolderMVVM> FoldersList { get; private set; } = [];

        public FolderMVVM? FolderSelected { get; private set; }
        public ListProjectMVVM? ProjectSelected { get; private set; }
        public CalculationMVVM? Calculation { get; private set; }
        public bool OtherDepartment { get; private set; }

        public event Action? OnChange;

        public Type? Control { get; private set; }
        public bool ControlUI { get; private set; }

        public void SetControl(bool show, Type value)
        {
            ControlUI = show;
            Control = value;
            Notify();
        }

        public void SetDepartments(List<ListDTO> departments)
        {
            Departments = departments ?? [];
            Notify();
        }

        public void SetFolders(List<FolderMVVM> folders)
        {
            FoldersList = folders?
                .OrderByDescending(f => f.Order)
                .ToList() ?? [];

            Notify();
        }

        public void AddFolder(FolderMVVM folder)
        {
            if (folder is null) return;

            FoldersList.Add(folder);
            Notify();
        }

        public void UpdateFolder(FolderMVVM updatedFolder)
        {
            if (updatedFolder is null) return;

            var existing = FoldersList.FirstOrDefault(f => f.Id == updatedFolder.Id);
            if (existing != null)
            {
                existing.Name = updatedFolder.Name;
                existing.Color = updatedFolder.Color;
                existing.Order = updatedFolder.Order;
                existing.IsVisible = updatedFolder.IsVisible;
                Notify();
            }
        }

        public void RemoveFolder(FolderMVVM folder)
        {
            if (folder is null) return;

            FoldersList.Remove(folder);
            Notify();
        }

        public void SortFoldersDescending()
        {
            FoldersList = FoldersList
                .OrderByDescending(f => f.Order)
                .ToList();

            Notify();
        }

        public void SetOtherDepartment(bool value)
        {
            OtherDepartment = value;
            Notify();
        }

        public void ClearFolders()
        {
            FoldersList.Clear();
            Notify();
        }

        public void ClearSelection()
        {
            ControlUI = false;
            SetSelection(null, null, null);
            Notify();
        }

        public void ClearCalculation()
        {
            ControlUI = false;
            Calculation = null;
            Notify();
        }

        private void SetSelection(FolderMVVM? folder, ListProjectMVVM? project, CalculationMVVM? calculation)
        {
            FolderSelected = folder;
            ProjectSelected = project;
            Calculation = calculation;
        }

        public void SelectFolder(FolderMVVM? folder)
        {
            ControlUI = false;
            SetSelection(folder, null, null);
            Notify();
        }

        public void SelectProject(FolderMVVM folder, ListProjectMVVM project)
        {
            ControlUI = false;
            SetSelection(folder, project, null);
            Notify();
        }

        public void SelectCalculation(FolderMVVM folder, ListProjectMVVM project, CalculationMVVM? calculation)
        {
            ControlUI = false;
            calculation?.ExecuteCalculation();
            SetSelection(folder, project, calculation);
            Notify();
        }

        // للحفاظ على التوافق مع الكود الحالي
        public void SetCalculation(CalculationMVVM? calculation)
        {
            if (FolderSelected is not null && ProjectSelected is not null)
            {
                SelectCalculation(FolderSelected, ProjectSelected, calculation);
                return;
            }

            ControlUI = false;
            calculation?.ExecuteCalculation();
            Calculation = calculation;
            Notify();
        }

        public void SetCalculation(CalculationMVVM? calculation, ListProjectMVVM? project, FolderMVVM folder)
        {
            if (project is null)
            {
                SelectFolder(folder);
            }
            else if (calculation is null)
            {
                SelectProject(folder, project);
            }
            else
            {
                SelectCalculation(folder, project, calculation);
            }
        }

        public void Notify() => OnChange?.Invoke();
    }
}
