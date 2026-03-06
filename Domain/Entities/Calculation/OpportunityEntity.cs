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
        public string OpportunitiesRisks { get; private set; } = string.Empty;

        [MaxLength(FieldLengths.Note)]
        public string? OpportunityType { get; private set; }

        public int CalculationId { get; private set; }

        private OpportunityData? _metadata;
        public OpportunityData Metadata
        {
            get => _metadata ??= new OpportunityData();
            private set => _metadata = value;
        }

        [JsonIgnore]
        public CalculationEntity Calculation { get; private set; } = null!;

        [JsonIgnore]
        public ICollection<ResourceEntity> Resources { get; private set; } = [];

        [JsonIgnore]
        public ICollection<TaskEntity> Tasks { get; private set; } = [];

        private OpportunityEntity() { }

        public OpportunityEntity(string opportunitiesRisks, string? opportunityType, int calculationId, OpportunityData metadata)
        {
            SetRisks(opportunitiesRisks);
            OpportunityType = NormalizeOptional(opportunityType);
            CalculationId = calculationId;
            Metadata = NormalizeMetadata(metadata);
        }

        public void SetRisks(string risks)
        {
            if (string.IsNullOrWhiteSpace(risks))
                throw new ValidationException("Opportunities/Risks text is required.");

            OpportunitiesRisks = risks.Trim();
        }

        public void Update(string opportunitiesRisks, string? opportunityType, OpportunityData metadata)
        {
            SetRisks(opportunitiesRisks);
            OpportunityType = NormalizeOptional(opportunityType);
            Metadata = NormalizeMetadata(metadata);
        }

        public OpportunityEntity CreateSnapshot()
        {
            return new OpportunityEntity
            {
                Id = Id,
                OpportunitiesRisks = OpportunitiesRisks,
                OpportunityType = OpportunityType,
                CalculationId = CalculationId,
                Metadata = new OpportunityData
                {
                    ProbabilityWorth = Metadata.ProbabilityWorth,
                    ProbabilityPercent = Metadata.ProbabilityPercent,
                    ProbabilityBest = Metadata.ProbabilityBest,
                    Value = Metadata.Value,
                    Comment = Metadata.Comment?.Trim() ?? string.Empty
                }
            };
        }

        private static OpportunityData NormalizeMetadata(OpportunityData? metadata)
        {
            metadata ??= new OpportunityData();
            metadata.Comment = metadata.Comment?.Trim() ?? string.Empty;
            return metadata;
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
