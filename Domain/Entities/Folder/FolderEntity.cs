using Domain.Entities.Base;
using Domain.Entities.Project;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Project;
using System.Text.Json.Serialization;

namespace Domain.Entities.Folder
{
    public class FolderEntity : FolderBase, IDataKeyFilterReadOnly
    {
        public Guid Id { get; set; }

        [JsonIgnore]
        public int TenantId { get; set; }

        /// <summary>
        /// Department that owns this folder (required).
        /// </summary>
        public int DepartmentId { get; set; }

        [JsonIgnore]
        public DepartmentEntity Department { get; set; } = null!;

        /// <summary>
        /// Optional user that owns this folder.
        /// </summary>
        public int? UserId { get; set; }

        [JsonIgnore]
        public UserEntity? User { get; set; }

        /// <summary>
        /// Projects contained in this folder.
        /// </summary>
        public ICollection<ProjectEntity> Projects { get; set; } = [];
    }
}
