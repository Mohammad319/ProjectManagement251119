using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.DTO.Project;
using System.Collections.Generic;

namespace ProjectManagement.Client.Shared.MVVM.Folder
{
    public class SearchProjectsMVVM : SearchProjectDTO{ }
    public class ListProjectMVVM : ListProjectDTO
    {
        public bool IsDragOver { get; set; }
        public bool ShowCalculations = false;
        public List<ListCalculationMVVM> Calculations { get; set; } = [];
        public bool CalculationsLoaded { get; set; }
        public bool IsLoading { get; set; } = false; // جديد

    }
}
