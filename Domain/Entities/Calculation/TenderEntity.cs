using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.Base.Calculation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class TenderAttributeBindEntity : IDataKeyFilterReadOnly
    {
        public int TenderId { get; set; }
        [ForeignKey(nameof(TenderId))] public TenderEntity Tender { get; set; }
        public int TenderAttributeId { get; set; }
        [ForeignKey(nameof(TenderAttributeId))] public AttributeNameTenderEntity TenderAttribute { get; set; }
        [JsonIgnore] public int TenantId { get; set; }

        public double Value { get; set; }
    }
    public class TenderEntity : TenderBase, IDataKeyFilterReadOnly
    {
        [Key] public int Id { get; set; }
        public int CalculationId { get; set; }
        [ForeignKey(nameof(CalculationId))] public CalculationEntity Calculation { get; set; }
        public int OrganisationId { get; set; }
        [ForeignKey(nameof(OrganisationId))] public OrganisationEntity Organisation { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public ICollection<TenderAttributeBindEntity> TendersAttributes { get; set; }
    }
}
