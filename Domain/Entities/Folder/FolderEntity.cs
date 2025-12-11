using Domain.Entities.Base;
using Domain.Entities.Project;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Folder
{
    public class FolderEntity : AuditableEntity<Guid>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
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
        /// /// Projects contained in this folder.
        /// /// </summary>
        public ICollection<ProjectEntity> FolderProjects { get; set; } = [];
    }
}
