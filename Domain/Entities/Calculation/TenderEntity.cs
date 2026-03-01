using Domain.Entities.Base;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class TenderAttributeBindEntity : IntBaseEntity
    {
        public int TenderId { get; private set; }

        [ForeignKey(nameof(TenderId))]
        public TenderEntity Tender { get; private set; } = null!;

        public int TenderAttributeId { get; private set; }

        [ForeignKey(nameof(TenderAttributeId))]
        public TenderAttributeDefinitionEntity TenderAttribute { get; private set; } = null!;

        public decimal Value { get; private set; }

        private TenderAttributeBindEntity() { }

        public TenderAttributeBindEntity(int tenderId, int attributeId, double value)
        {
            TenderId = tenderId;
            TenderAttributeId = attributeId;
            SetValue(value);
        }

        public void SetValue(double value)
        {
            // هنا يمكنك إضافة قواعد (min/max) إذا احتجت
            Value = value;
        }
    }
    public sealed class TenderEntity : IntBaseEntity
    {
        // لو Attributes (string) لم تعد ضرورية يمكنك لاحقاً حذفها
        public string? Attributes { get; private set; }

        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; private set; }

        public int CalculationId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(CalculationId))]
        public CalculationEntity Calculation { get; private set; } = null!;

        public int OrganisationId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(OrganisationId))]
        public OrganisationEntity Organisation { get; private set; } = null!;

        [JsonIgnore]
        public ICollection<TenderAttributeBindEntity> TendersAttributes { get; private set; } = [];

        private TenderEntity() { }

        public TenderEntity(int calculationId, int organisationId, string? note)
        {
            CalculationId = calculationId;
            OrganisationId = organisationId;
            Note = note;
        }

        public void UpdateNote(string? note)
        {
            Note = note;
        }

        public void SetAttributesRaw(string? json)
        {
            Attributes = json;
        }

        public void SetAttributeValue(int attributeId, double value)
        {
            var existing = TendersAttributes
                .FirstOrDefault(x => x.TenderAttributeId == attributeId);

            if (existing is null)
            {
                TendersAttributes.Add(new TenderAttributeBindEntity(Id, attributeId, value));
            }
            else
            {
                existing.SetValue(value);
            }
        }
    }

}
