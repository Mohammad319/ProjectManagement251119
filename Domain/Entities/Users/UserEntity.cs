using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.Users
{
    /// <summary>
    /// Represents an application user that belongs to a specific tenant.
    /// </summary>
    public sealed class UserEntity : AuditableEntity<int>
    {
        /// <summary>External authentication provider user id (Azure AD / Identity).</summary>
        [MaxLength(200)]
        public string ExternalAuthId { get; private set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(FieldLengths.Email)]
        public string Email { get; private set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string UserName { get; private set; } = string.Empty;

        /// <summary>
        /// Optional department the user belongs to.
        /// </summary>
        public int? DepartmentId { get; private set; }

        [MaxLength(80)]
        public string? FirstName { get; private set; }

        [MaxLength(80)]
        public string? LastName { get; private set; }

        /// <summary>
        /// Navigation to the user's department.
        /// </summary>
        public DepartmentEntity? Department { get; private set; }

        public ICollection<UserDepartmentAccessEntity> DepartmentAccesses { get; private set; } = [];

        private UserEntity() { }

        public static UserEntity Create(
            int tenantId,
            string email,
            string userName,
            int? departmentId,
            string? firstName,
            string? lastName,
            string? externalAuthId = null)
        {
            var entity = new UserEntity
            {
                TenantId = tenantId
            };

            entity.SetEmail(email);
            entity.SetUserName(userName);
            entity.UpdateProfile(firstName, lastName, departmentId);
            entity.SetExternalAuthId(externalAuthId);

            return entity;
        }

        public void SetExternalAuthId(string? externalAuthId)
        {
            ExternalAuthId = NormalizeOptional(externalAuthId, 200) ?? string.Empty;
        }

        public void ClearExternalAuthId()
        {
            ExternalAuthId = string.Empty;
        }

        public void SetDepartment(int? departmentId)
        {
            if (departmentId <= 0)
                DepartmentId = null;
            else
                DepartmentId = departmentId;
        }

        public void UpdateProfile(string? firstName, string? lastName, int? departmentId)
        {
            FirstName = NormalizeOptional(firstName, 80);
            LastName = NormalizeOptional(lastName, 80);
            SetDepartment(departmentId);
        }

        public void SetEmail(string email)
        {
            Email = NormalizeRequired(email, nameof(Email), FieldLengths.Email);
        }

        public void SetUserName(string userName)
        {
            UserName = NormalizeRequired(userName, nameof(UserName), 100);
        }

        private static string NormalizeRequired(string? value, string fieldName, int maxLength)
        {
            var normalized = value?.Trim();

            if (string.IsNullOrWhiteSpace(normalized))
                throw new ValidationException($"{fieldName} is required.");

            return normalized.Length > maxLength
                ? normalized[..maxLength]
                : normalized;
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            return normalized.Length > maxLength
                ? normalized[..maxLength]
                : normalized;
        }
    }
}
