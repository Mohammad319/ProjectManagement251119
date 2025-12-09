using Domain.Entities.Base;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Base.Calculation;
using System.Text.Json.Serialization;
using Domain.Entities.Users;

namespace Domain.Entities.Calculation
{
    public sealed class StorageEntity : AuditableEntity<int>
    {
        public string StorageValue { get; set; } = string.Empty;
        public CalculationItemType StorageType { get; set; }
        public StorageSort StorageSort { get; set; }
        public AuthorityStorage StorageLevel { get; set; }
        public int DepartmentId { get; set; }
        public DepartmentEntity Department { get; set; } = null!;
    }
}