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

        private OrganisationTypeEntity() { }

        public static OrganisationTypeEntity Create(PostOrganisationTypeDTO dto)
        {
            var entity = new OrganisationTypeEntity();
            entity.Update(dto);
            return entity;
        }

        public void Update(PostOrganisationTypeDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            Name = NormalizeName(dto.Name);
            IsVisible = dto.IsVisible;
        }

        public void SetVisibility(bool visible) => IsVisible = visible;

        private static string NormalizeName(string? value)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                throw new ValidationException("Organisation type name is required.");
            return trimmed;
        }
    }
}
