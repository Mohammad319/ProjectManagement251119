using Domain.Entities.Base;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.Resource;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class OpportunityEntity : IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(500, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string OpportunitiesRisks { get; set; }
        [MaxLength(500, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Type { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public int CalculationId { get; set; }
        OpportunityData data;
        public OpportunityData Data { get { data ??= new OpportunityData(); return data; } set { data = value; } }

        [JsonIgnore]public CalculationEntity Calculation { get; set; }
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; }
        [JsonIgnore] public ICollection<TaskEntity> Tasks { get; set; }
    }
}
