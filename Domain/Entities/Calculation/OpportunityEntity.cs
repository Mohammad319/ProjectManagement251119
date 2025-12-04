using Domain.Entities.Base;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class OpportunityEntity : OpportunityBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public int CalculationId { get; set; }
        OpportunityData data;
        public OpportunityData Data { get { data ??= new OpportunityData(); return data; } set { data = value; } }

        [JsonIgnore]public CalculationEntity Calculation { get; set; }
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; }
        [JsonIgnore] public ICollection<TaskEntity> Tasks { get; set; }
    }
}
