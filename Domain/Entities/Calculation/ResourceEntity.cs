using Domain.Entities.Base;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Resource;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class ResourceEntity : IntBaseEntity
    {
        public ResourceTypesEnum ResType { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        public string? Name { get; set; }
        public double SortOrder { get; set; }
        public bool Active { get; set; } = true;
        ResourceData _metadata = new();
        public ResourceData Metadata { get { _metadata ??= new ResourceData(); return _metadata; } set { _metadata = value; } }

        public int TaskId { get; set; }
        [JsonIgnore] public TaskEntity Task { get; set; } = null!;
        public int? OpportunityId { get; set; }
        public OpportunityEntity? Opportunity { get; set; }
        public int? AccountId { get; set; }
        public AccountEntity? Account { get; set; }
        public int? StatusId { get; set; }
        public StatusResourcesEntity? Status { get; set; }
        public int? ResourceSortId { get; set; }
        public ResourceSortEntity? ResourceSort { get; set; }
        public int? ResourceTypeId { get; set; }
        public ResourceTypeEntity? ResourceType { get; set; }
        public int? OfferId { get; set; }
        public ICollection<OfferEntity> Offers { get; set; } = [];
    }
}
