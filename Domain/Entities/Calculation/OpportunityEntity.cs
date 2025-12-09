using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class OpportunityEntity : IntBaseEntity
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(500, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public required string OpportunitiesRisks { get; set; }
        [MaxLength(500, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string? Type { get; set; }
        public int CalculationId { get; set; }
        OpportunityData? _metadata;
        public OpportunityData Metadata { get { _metadata ??= new OpportunityData(); return _metadata; } set { _metadata = value; } }

        [JsonIgnore] public CalculationEntity Calculation { get; set; } = null!;
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; } = [];
        [JsonIgnore] public ICollection<TaskEntity> Tasks { get; set; } = [];
    }
}
