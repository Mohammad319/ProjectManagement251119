using Domain.Entities.Base;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Calculation;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class StatusResourcesEntity : StatusResourceBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        public bool IsVisible { get; set; } = true;
        [JsonIgnore] public int TenantId { get; set; }
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; }
    }
}
