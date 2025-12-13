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

        // EF فقط
        private OpportunityEntity() { }

        public OpportunityEntity(
            string opportunitiesRisks,
            string? opportunityType,
            int calculationId,
            OpportunityData metadata)
        {
            SetRisks(opportunitiesRisks);
            OpportunityType = opportunityType;
            CalculationId = calculationId;
            Metadata = metadata ?? new OpportunityData();
        }

        public void SetRisks(string risks)
        {
            if (string.IsNullOrWhiteSpace(risks))
                throw new ValidationException("Opportunities/Risks text is required.");

            OpportunitiesRisks = risks.Trim();
        }

        public void Update(
            string opportunitiesRisks,
            string? opportunityType,
            OpportunityData metadata)
        {
            SetRisks(opportunitiesRisks);
            OpportunityType = opportunityType;
            Metadata = metadata ?? new OpportunityData();
        }

        // نسخة خفيفة للإرسال عبر Hub بدون مشاكل tracking
        public OpportunityEntity CreateSnapshot()
        {
            return new OpportunityEntity
            {
                Id = Id,
                OpportunitiesRisks = OpportunitiesRisks,
                OpportunityType = OpportunityType,
                CalculationId = CalculationId,
                Metadata = Metadata
            };
        }
    }
}
