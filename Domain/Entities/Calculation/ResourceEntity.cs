using Domain.Entities.Base;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class ResourceEntity : ResourceBase, IDataKeyFilterReadOnly
    {
        [Key] public int Id { get; set; }
        ResourceData data = new();
        public ResourceData Data { get { data ??= new ResourceData(); return data; } set { data = value; } }

        public int TaskId { get; set; }
        [JsonIgnore] public TaskEntity Task { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        public int? OpportunityId { get; set; }
        public OpportunityEntity Opportunity { get; set; }
        public int? AccountId { get; set; }
        public AccountEntity Account { get; set; }
        public int? StatusId { get; set; }
        public StatusResourcesEntity Status { get; set; }
        public int? ResourceSortId { get; set; }
        public ResourceSortEntity ResourceSort { get; set; }
        public int? ResourceTypeId { get; set; }
        public ResourceTypeEntity ResourceType { get; set; }
        public int? OfferId { get; set; }
        //public OfferEntity Offer { get; set; }
        public ICollection<OfferEntity> Offers { get; set; }
    }
}
