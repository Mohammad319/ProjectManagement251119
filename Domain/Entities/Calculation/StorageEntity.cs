using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.Enums;

namespace Domain.Entities.Calculation
{
    public sealed class StorageEntity : AuditableEntity<int>
    {
        public string StorageValue { get; private set; } = string.Empty;

        public CalculationItemType StorageType { get; private set; }
        public StorageSort StorageSort { get; private set; }
        public AuthorityStorage StorageLevel { get; private set; }

        public int DepartmentId { get; private set; }
        public DepartmentEntity Department { get; private set; } = null!;

        private StorageEntity() { }

        public StorageEntity(
            string storageValue,
            CalculationItemType type,
            StorageSort sort,
            AuthorityStorage level,
            int departmentId,
            int createdBy)
        {
            StorageValue = storageValue;
            StorageType = type;
            StorageSort = sort;
            StorageLevel = level;
            DepartmentId = departmentId;
            CreatedBy = createdBy;
        }

        public void UpdateValue(string newValue)
        {
            StorageValue = newValue;
        }
    }
}
