using Domain.Entities.Base;
using Domain.Entities.Project;
using Domain.Entities.Users;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Folder
{
    public sealed class FolderEntity : AuditableEntity<Guid>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        [Required, StringLength(FieldLengths.ColorHex, MinimumLength = FieldLengths.ColorHex)]
        public string Color { get; private set; } = "#08bf66";

        public double SortOrder { get; private set; }

        public bool IsVisible { get; private set; } = true;

        public int DepartmentId { get; private set; }

        [JsonIgnore]
        public DepartmentEntity Department { get; private set; } = null!;

        [JsonIgnore]
        public ICollection<ProjectEntity> FolderProjects { get; private set; } = [];

        private FolderEntity() { } // EF

        public FolderEntity(string name, string color, int departmentId, int createdBy, double sortOrder)
        {
            Update(name, color, true);
            DepartmentId = departmentId;
            CreatedBy = createdBy;
            SortOrder = sortOrder;
        }

        public void Update(string name, string color, bool isVisible)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Folder name is required.");

            Name = name.Trim();
            Color = color;
            IsVisible = isVisible;
        }

        public void UpdateOrder(double newOrder)
        {
            SortOrder = newOrder;
        }
    }
}
