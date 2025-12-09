using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class OpportunityEntity : IntBaseEntity
    {
        [Required, MaxLength(FieldLengths.Note)]
        public string OpportunitiesRisks { get; set; } = string.Empty;
        [MaxLength(FieldLengths.Note)]
        public string? OpportunityType { get; set; }
        public int CalculationId { get; set; }
        OpportunityData? _metadata;
        public OpportunityData Metadata { get { _metadata ??= new OpportunityData(); return _metadata; } set { _metadata = value; } }

        [JsonIgnore] public CalculationEntity Calculation { get; set; } = null!;
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; } = [];
        [JsonIgnore] public ICollection<TaskEntity> Tasks { get; set; } = [];
    }
}
