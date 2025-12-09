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
    public class TenderAttributeBindEntity : IntBaseEntity
    {
        public int TenderId { get; set; }
        [ForeignKey(nameof(TenderId))] public TenderEntity Tender { get; set; } = null!;
        public int TenderAttributeId { get; set; }
        [ForeignKey(nameof(TenderAttributeId))] 
        public TenderAttributeDefinitionEntity TenderAttribute { get; set; } = null!;
        public double Value { get; set; }
    }
    public class TenderEntity : IntBaseEntity
    {
        public string? Attributes { get; set; }
        public string? Note { get; set; }
        public int CalculationId { get; set; }
        [ForeignKey(nameof(CalculationId))] 
        public CalculationEntity Calculation { get; set; } = null!;
        public int OrganisationId { get; set; }
        [ForeignKey(nameof(OrganisationId))] 
        public OrganisationEntity Organisation { get; set; } = null!;
        public ICollection<TenderAttributeBindEntity> TendersAttributes { get; set; } = [];
    }
}
