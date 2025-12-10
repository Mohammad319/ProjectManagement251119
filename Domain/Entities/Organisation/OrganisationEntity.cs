using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Organisation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public sealed class OrganisationEntity : AuditableEntity<int>
    {
        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        private OrganisationData? _metadata;
        public OrganisationData Metadata
        {
            get => _metadata ??= new OrganisationData();
            set => _metadata = value ?? new OrganisationData();
        }

        public int OrganisationCategoryId { get; set; }
        public OrganisationCategoryEntity OrganisationCategory { get; set; } = null!;

        public int? OrganisationTypeId { get; set; }
        public OrganisationTypeEntity? OrganisationType { get; set; }

        [JsonIgnore]
        public ICollection<OfferEntity> Offers { get; set; } = [];

        [JsonIgnore]
        public ICollection<TenderEntity> Tenders { get; set; } = [];

        [JsonIgnore]
        public ICollection<ProjectEntity> Projects { get; set; } = [];

        public ICollection<CalculationEntity> Calculations { get; set; } = [];

        public bool IsVisible { get; set; } = true;
    }
}
