using Domain.Entities.Base;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.Base.Application;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Application
{
    public class ApplicationValuesEntity : ApplicationValuesBase, IDataKeyFilterReadOnly
    {
        [Key] public int Id { get; set; }
        public int CalculationId { get; set; }
        [JsonIgnore]
        public CalculationEntity Calculation { get; set; }
        public int ApplicationId { get; set; }
        public ApplicationEntity Application { get; set; }
        [JsonIgnore] public int TenantId { get; set; }

    }
}
