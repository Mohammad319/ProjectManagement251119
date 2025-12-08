using Domain.Entities.Base;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Base.Calculation;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class StorageEntity : IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        public string StorageValue { get; set; }
        public CalculationItemType StorageType { get; set; }
        public StorageSort StorageSort { get; set; }
        public AuthorityStorage StorageLevel { get; set; }
        public int UserId { get; set; }
        public int DepartmentId { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
    }
}