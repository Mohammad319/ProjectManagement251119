using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Domain.Helper.Organisation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Organisation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public sealed class OrganisationEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        private OrganisationData? _metadata;
        public OrganisationData Metadata
        {
            get => _metadata ??= new OrganisationData();
            private set => _metadata = value;
        }

        public int OrganisationCategoryId { get; private set; }
        public OrganisationCategoryEntity OrganisationCategory { get; private set; } = null!;

        public int? OrganisationTypeId { get; private set; }
        public OrganisationTypeEntity? OrganisationType { get; private set; }

        public bool IsVisible { get; private set; } = true;

        [JsonIgnore]
        public ICollection<OfferEntity> Offers { get; private set; } = [];

        [JsonIgnore]
        public ICollection<TenderEntity> Tenders { get; private set; } = [];

        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; private set; } = [];

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; private set; } = [];

        private OrganisationEntity() { }

        public static OrganisationEntity Create(PostOrganisationDTO dto)
        {
            var entity = new OrganisationEntity();
            entity.Update(dto);
            return entity;
        }

        public void Update(PostOrganisationDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            if (dto.CategoryId <= 0)
                throw new ValidationException("Organisation category is required.");

            Name = NormalizeName(dto.Name);
            OrganisationCategoryId = dto.CategoryId;
            OrganisationTypeId = dto.OrganisationTypeID;
            IsVisible = dto.IsVisible;
            Metadata = OrganisationDataFactory.From(dto);
        }

        public void SetVisibility(bool visible) => IsVisible = visible;

        private static string NormalizeName(string? value)
        {
            var normalized = (value ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalized))
                throw new ValidationException("Organisation name is required.");

            if (normalized.Length > FieldLengths.Name)
                throw new ValidationException($"Organisation name exceeds max length {FieldLengths.Name}.");

            return normalized;
        }
    }
}
