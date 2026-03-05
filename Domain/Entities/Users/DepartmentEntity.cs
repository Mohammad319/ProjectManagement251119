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

        [JsonIgnore]
        public ICollection<FolderEntity> Folders { get; private set; } = [];

        [JsonIgnore]
        public ICollection<UserEntity> Users { get; private set; } = [];

        private DepartmentEntity() { } // EF

        public static DepartmentEntity Create(DepartmentBase dto)
        {
            return new DepartmentEntity
            {
                Name = dto.Name,
                Description = dto.Description
            };
        }

        public void Update(DepartmentBase dto)
        {
            Name = dto.Name;
            Description = dto.Description;
        }
    }
}
