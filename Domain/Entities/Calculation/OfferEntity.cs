using Domain.Entities.Base;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.Base.Offer;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class OfferEntity : OfferBase, IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        OfferData data = new();
        public OfferData Data { get { data ??= new OfferData(); return data; } set { data = value; } }

        [JsonIgnore] public int TenantId { get; set; }
        public int? OrganisationId { get; set; }
        public OrganisationEntity Organisation { get; set; }
        public int ResourceID { get; set; }
        public ResourceEntity Resource { get; set; }
    }
}
