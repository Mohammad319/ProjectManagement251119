using Domain.Entities.Organisation;
using ProjectManagement.Shared.Constant;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.DTO.Category
{
    public class ListOrganisationCategoryDTO
    {
        public int Id { get; set; } = default!;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        public int? ParentCategoryId { get; set; }

        /// <summary>Number of customers/suppliers attached directly to this category (huvudgrupp or undergrupp).
        /// Used by "Hantera grupper" to show usage and to block deletion of groups that are in use.</summary>
        public int OrganisationCount { get; set; }

        [JsonIgnore]
        public OrganisationCategoryEntity? ParentCategory { get; private set; }

        [JsonIgnore]
        public ICollection<OrganisationCategoryEntity> ChildCategories { get; private set; } = [];

        [JsonIgnore]
        public ICollection<OrganisationEntity> Organisations { get; private set; } = [];
    }
}
