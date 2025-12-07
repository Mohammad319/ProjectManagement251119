using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.DTO.Organisation;
using System.Text.Json.Serialization;

namespace Domain.Entities.Organisation
{
    public class OrganisationEntity : OrganisationBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        //public OrganisationData Data { get; set; } = new();
        private OrganisationData data = new();
        public OrganisationData Data
        {
            get { return data ?? new OrganisationData(); }
            set { data = value ?? new OrganisationData(); }
        }
        public int CategoryId { get; set; }
        public OrganisationCategoryEntity Category { get; set; }
        public int? OrganisationTypeId { get; set; }
        public OrganisationTypeEntity OrganisationType { get; set; }

        //public ICollection<UnderContactOrganisationEntity> Contacts { get; set; }
        [JsonIgnore] public ICollection<OfferEntity> Offers { get; set; }
        [JsonIgnore] public ICollection<TenderEntity> Tenders { get; set; }
        [JsonIgnore] public ICollection<ProjectEntity> Projects { get; set; }
        public ICollection<CalculationEntity> Calculations { get; set; }

        public bool IsVisible { get; set; } = true;
        [JsonIgnore] public int TenantId { get; set; }
    }
}
