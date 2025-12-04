using Domain.Entities.Base;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Calculation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class AttributeNameTenderEntity : AttributeNameTenderBase, IDataKeyFilterReadOnly
    {
        [Key]public int Id { get; set; }
        public int CalculationId { get; set; }
        [JsonIgnore][ForeignKey(nameof(CalculationId))] public CalculationEntity Calculation { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        //public ICollection<TenderAttributeBindEntity> TendersAttributes { get; set; }
        public ICollection<TenderAttributeBindEntity> TendersAttributes { get; set; }

    }
}
