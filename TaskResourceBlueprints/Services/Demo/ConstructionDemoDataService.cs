using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Entities.Resources;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Demo
{
    public sealed class ConstructionDemoSeedResult
    {
        public int FoldersCreated { get; set; }
        public int ResourcesCreated { get; set; }
        public int PropertyGroupsCreated { get; set; }
        public int PropertiesCreated { get; set; }
        public int LookupsCreated { get; set; }
        public int UnitGroupsCreated { get; set; }
        public int TasksCreated { get; set; }
        public int AssignmentsCreated { get; set; }

        public bool HasChanges =>
            FoldersCreated > 0 ||
            ResourcesCreated > 0 ||
            PropertyGroupsCreated > 0 ||
            PropertiesCreated > 0 ||
            LookupsCreated > 0 ||
            UnitGroupsCreated > 0 ||
            TasksCreated > 0 ||
            AssignmentsCreated > 0;
    }

}
