using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    /// <summary>
    /// A project-specific evaluation part (tidigare "priskolumn") for the bid (Anbud) window,
    /// e.g. "Grundanbud", "Option 1", "Miljö". Parts belong to one project and are never shared
    /// between projects. A part has a <see cref="PartType"/> so the model is not locked to price.
    /// </summary>
    public sealed class ProjectBidPriceColumnEntity : AuditableEntity<int>
    {
        public Guid ProjectId { get; private set; }

        [JsonIgnore]
        public ProjectEntity Project { get; private set; } = null!;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        /// <summary>Typ av utvärderingsdel, t.ex. Pris eller Poäng.</summary>
        public BidPartType PartType { get; private set; } = BidPartType.Price;

        public int SortOrder { get; private set; }

        private ProjectBidPriceColumnEntity() { }

        public ProjectBidPriceColumnEntity(Guid projectId, string name, int sortOrder, BidPartType partType = BidPartType.Price)
        {
            ProjectId = projectId;
            Name = Normalize(name);
            SortOrder = sortOrder;
            PartType = partType;
        }

        public void Rename(string name) => Name = Normalize(name);

        public void SetPartType(BidPartType partType) => PartType = partType;

        public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

        private static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
