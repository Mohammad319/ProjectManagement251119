using Domain.Entities.Base;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class StorageEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.StorageValue)]
        public string StorageValue { get; private set; } = string.Empty;

        public CalculationItemType StorageType { get; private set; }
        public StorageSort StorageSort { get; private set; }
        public AuthorityStorage StorageLevel { get; private set; }

        public int DepartmentId { get; private set; }

        [JsonIgnore]
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
            if (string.IsNullOrWhiteSpace(newValue))
                throw new ValidationException("StorageValue is required.");

            if (newValue.Length > FieldLengths.StorageValue)
                throw new ValidationException($"StorageValue exceeds max length {FieldLengths.StorageValue}.");

            StorageValue = newValue;
        }
    }
}
