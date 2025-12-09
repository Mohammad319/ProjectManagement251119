using Domain.Entities.Base;
using Domain.Entities.Project;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Folder
{
    public class FolderEntity : AuditableEntity<Guid>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }

        [StringLength(7, ErrorMessageResourceName = ErrorsMessages.StringLength, ErrorMessageResourceType = typeof(ResLocalize), MinimumLength = 7)]
        public string Color { get; set; } = "#08bf66";
        public double SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// Department that owns this folder (required).
        /// </summary>
        public int DepartmentId { get; set; }

        [JsonIgnore]
        public DepartmentEntity Department { get; set; } = null!;

        /// <summary>
        /// Projects contained in this folder.
        /// </summary>
        public ICollection<ProjectEntity> FolderProjects { get; set; } = [];
    }
}
