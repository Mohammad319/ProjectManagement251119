using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public sealed class OrganisationCategoryEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// التصنيف الأب (في حالة وجود تسلسل هرمي).
        /// </summary>
        public int? ParentCategoryId { get; set; }

        [JsonIgnore]
        public OrganisationCategoryEntity? ParentCategory { get; set; }

        /// <summary>
        /// التصنيفات الفرعية.
        /// </summary>
        public ICollection<OrganisationCategoryEntity> ChildCategories { get; set; } = [];

        /// <summary>
        /// المؤسسات التي تنتمي لهذا التصنيف.
        /// </summary>
        [JsonIgnore]
        public ICollection<OrganisationEntity> Organisations { get; set; } = [];
    }
}
