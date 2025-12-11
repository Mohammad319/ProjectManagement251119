using Domain.Entities.Base;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    [Index(nameof(TenantId), nameof(CalculationId))]
    public sealed class TenderAttributeDefinitionEntity : IntBaseEntity
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; private set; }

        public int CalculationId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(CalculationId))]
        public CalculationEntity Calculation { get; private set; } = null!;

        [JsonIgnore]
        public ICollection<TenderAttributeBindEntity> TendersAttributes { get; private set; } = [];

        private TenderAttributeDefinitionEntity() { }

        public TenderAttributeDefinitionEntity(int calculationId, string name, string? note)
        {
            SetName(name);
            Note = note;
            CalculationId = calculationId;
        }

        public void Update(string name, string? note)
        {
            SetName(name);
            Note = note;
        }

        private void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Attribute name is required.");

            if (name.Length > FieldLengths.Name)
                throw new ValidationException($"Attribute name cannot exceed {FieldLengths.Name} characters.");

            Name = name.Trim();
        }
    }
}