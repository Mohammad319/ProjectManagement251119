using Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TenderAttributeDefinitionEntity : IntBaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Note { get; set; }

        public int CalculationId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(CalculationId))]
        public CalculationEntity Calculation { get; set; } = null!;
        public ICollection<TenderAttributeBindEntity> TendersAttributes { get; set; } = [];
    }
}
