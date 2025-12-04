using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class TemplateEntity : TemplateBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        TemplateData data;
        public TemplateData Data { get { data ??= new TemplateData(); return data; } set { data = value; } }

        [JsonIgnore] public bool IsVisible { get; set; } = true;
        [JsonIgnore] public int TenantId { get; set; }
        [JsonIgnore] public int? DepartmentId { get; set; }
        [JsonIgnore] public DepartmentEntity Department { get; set; }
        [JsonIgnore] public ICollection<CalculationEntity> Calculations { get; set; }
    }
}
