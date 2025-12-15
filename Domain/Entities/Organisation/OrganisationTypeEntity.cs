using Domain.Entities.Base;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Organisation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public sealed class OrganisationTypeEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        public bool IsVisible { get; private set; } = true;

        [JsonIgnore]
        public ICollection<OrganisationEntity> Organisations { get; private set; } = [];

        private OrganisationTypeEntity() { } // EF

        public static OrganisationTypeEntity Create(PostOrganisationTypeDTO dto)
        {
            return new OrganisationTypeEntity
            {
                Name = dto.Name,
                IsVisible = dto.IsVisible
            };
        }

        public void Update(PostOrganisationTypeDTO dto)
        {
            Name = dto.Name;
            IsVisible = dto.IsVisible;
        }

        public void SetVisibility(bool visible)
        {
            IsVisible = visible;
        }
    }
}
