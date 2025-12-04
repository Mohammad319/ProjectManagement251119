using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Base.Project;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public class CompensationEntity : CompensationBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public ICollection<CalculationEntity> Calculations { get; set; }
        public ICollection<ProjectEntity> Projects { get; set; }
    }
}
