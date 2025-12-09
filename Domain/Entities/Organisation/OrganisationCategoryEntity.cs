using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public sealed class OrganisationCategoryEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }

        /// <summary>
        /// التصنيف الأب (في حالة وجود تسلسل هرمي).
        /// </summary>
        [JsonIgnore]
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
