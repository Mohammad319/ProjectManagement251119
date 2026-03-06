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

        private OrganisationCategoryEntity() { }

        public static OrganisationCategoryEntity Create(PostOrganisationCategoryDTO dto)
        {
            var entity = new OrganisationCategoryEntity();
            entity.SetName(dto.Name);
            entity.SetParentCategory(dto.CategoryId);
            return entity;
        }

        public void Update(PutOrganisationCategoryDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            SetName(dto.Name);
        }

        public void SetName(string? name)
        {
            var trimmed = name?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException("Organisation category name is required.");
            Name = trimmed;
        }

        public void SetParentCategory(int? parentId)
        {
            if (parentId.HasValue && parentId <= 0)
                throw new ValidationException("Invalid parent category.");
            ParentCategoryId = parentId;
        }
    }
}
