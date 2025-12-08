using Domain.Entities.Base;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Resource;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class ResourceEntity : IDataKeyFilterReadOnly
    {
        [Key] public int Id { get; set; }
        public ResourceTypesEnum ResType { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }
        public double Order { get; set; }
        public bool Active { get; set; } = true;
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
