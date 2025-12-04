using Domain.Entities.Base;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Base.Calculation;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class StorageEntity : StorageBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
    }
}