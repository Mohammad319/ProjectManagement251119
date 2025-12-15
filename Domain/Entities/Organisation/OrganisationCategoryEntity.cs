using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Organisation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public sealed class OrganisationCategoryEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        public int? ParentCategoryId { get; private set; }

        [JsonIgnore]
        public OrganisationCategoryEntity? ParentCategory { get; private set; }

        [JsonIgnore]
        public ICollection<OrganisationCategoryEntity> ChildCategories { get; private set; } = [];

        [JsonIgnore]
        public ICollection<OrganisationEntity> Organisations { get; private set; } = [];

        private OrganisationCategoryEntity() { } // EF

        public static OrganisationCategoryEntity Create(PostOrganisationCategoryDTO dto)
        {
            ValidateParent(dto.CategoryId);
            return new OrganisationCategoryEntity
            {
                Name = dto.Name,
                ParentCategoryId = dto.CategoryId
            };
        }

        public void Update(PutOrganisationCategoryDTO dto)
        {
            Name = dto.Name;
        }

        private static void ValidateParent(int? parentId)
        {
            if (parentId.HasValue && parentId <= 0)
                throw new ValidationException("Invalid parent category.");
        }
    }
}
