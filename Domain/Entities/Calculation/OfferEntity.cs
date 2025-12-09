using Domain.Entities.Base;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.Offer;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class OfferEntity : IntBaseEntity
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string? Comment { get; set; }
        OfferData data = new();
        public OfferData Data { get { data ??= new OfferData(); return data; } set { data = value; } }
        public int? OrganisationId { get; set; }
        public OrganisationEntity? Organisation { get; set; }
        public int ResourceID { get; set; }
        public ResourceEntity Resource { get; set; } = null!;
    }
}
