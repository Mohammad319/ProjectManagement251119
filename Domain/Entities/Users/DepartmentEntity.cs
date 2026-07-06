using Domain.Entities.Base;
using Domain.Entities.Folder;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Users
{
    public sealed class DepartmentEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        [MaxLength(FieldLengths.Comment)]
        public string? Description { get; private set; }

        /// <summary>Optional UI accent colour (hex, e.g. "#10b981").</summary>
        [MaxLength(16)]
        public string? Color { get; private set; }

        /// <summary>Optional id of the user who heads this department (soft reference, no FK constraint).</summary>
        public int? HeadUserId { get; private set; }

        [JsonIgnore]
        public ICollection<FolderEntity> Folders { get; private set; } = [];

        [JsonIgnore]
        public ICollection<UserEntity> Users { get; private set; } = [];

        [JsonIgnore]
        public ICollection<UserDepartmentAccessEntity> UserAccesses { get; private set; } = [];

        private DepartmentEntity() { }

        public static DepartmentEntity Create(DepartmentBase dto)
        {
            return new DepartmentEntity
            {
                Name = NormalizeRequired(dto.Name),
                Description = NormalizeOptional(dto.Description),
                Color = NormalizeColor(dto.Color),
                HeadUserId = NormalizeHeadUserId(dto.HeadUserId)
            };
        }

        public void Update(DepartmentBase dto)
        {
            Name = NormalizeRequired(dto.Name);
            Description = NormalizeOptional(dto.Description);
            Color = NormalizeColor(dto.Color);
            HeadUserId = NormalizeHeadUserId(dto.HeadUserId);
        }

        public void ClearHeadIfMatches(int userId)
        {
            if (HeadUserId == userId)
                HeadUserId = null;
        }

        private static string? NormalizeColor(string? value)
        {
            var normalized = value?.Trim();
            return string.IsNullOrWhiteSpace(normalized)
                ? null
                : (normalized.Length > 16 ? normalized[..16] : normalized);
        }

        private static int? NormalizeHeadUserId(int? value)
            => value.HasValue && value.Value > 0 ? value : null;

        private static string NormalizeRequired(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ValidationException("Department name is required.");

            return value.Trim();
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
