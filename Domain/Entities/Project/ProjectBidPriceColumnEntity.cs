using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    /// <summary>
    /// A project-specific price column for the bid (Anbud) window, e.g. "Grundanbud", "Option 1".
    /// Columns belong to one project and are never shared between projects.
    /// </summary>
    public sealed class ProjectBidPriceColumnEntity : AuditableEntity<int>
    {
        public Guid ProjectId { get; private set; }

        [JsonIgnore]
        public ProjectEntity Project { get; private set; } = null!;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        public int SortOrder { get; private set; }

        private ProjectBidPriceColumnEntity() { }

        public ProjectBidPriceColumnEntity(Guid projectId, string name, int sortOrder)
        {
            ProjectId = projectId;
            Name = Normalize(name);
            SortOrder = sortOrder;
        }

        public void Rename(string name) => Name = Normalize(name);

        public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
