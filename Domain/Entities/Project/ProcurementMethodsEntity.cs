using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Project;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public class ProcurementMethodsEntity : ProcurementMethodsBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public ICollection<CalculationEntity> Calculations { get; set; } 
        public ICollection<ProjectEntity> Projects { get; set; }
    }
}
