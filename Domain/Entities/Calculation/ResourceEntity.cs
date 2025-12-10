using Domain.Entities.Base;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class ResourceEntity : IntBaseEntity
    {
        public ResourceTypesEnum ResType { get; set; }

        [Required, MaxLength(FieldLengths.Name)]
        public required string Name { get; set; }

        public double SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; set; }
        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;
        [MaxLength(FieldLengths.Unit)]
        public string? Unit { get; set; }
        [Required]
        public decimal Cost { get; set; }
        public decimal? BaseCost { get; set; }
        public double? CO2 { get; set; }
        private ResourceData? _metadata;
        public ResourceData Metadata
        {
            get => _metadata ??= new ResourceData();
            set => _metadata = value;
        }


        // -----------------------
        // Relations
        // -----------------------

        public int TaskId { get; set; }

        [JsonIgnore]
        public TaskEntity Task { get; set; } = null!;

        public int? OpportunityId { get; set; }

        [JsonIgnore]
        public OpportunityEntity? Opportunity { get; set; }

        public int? AccountId { get; set; }

        [JsonIgnore]
        public AccountEntity? Account { get; set; }

        public int? StatusId { get; set; }

        [JsonIgnore]
        public StatusResourcesEntity? Status { get; set; }

        public int? ResourceSortId { get; set; }

        [JsonIgnore]
        public ResourceSortEntity? ResourceSort { get; set; }

        public int? ResourceTypeId { get; set; }

        [JsonIgnore]
        public ResourceTypeEntity? ResourceType { get; set; }

        public int? PrimaryOfferId { get; set; }
        [JsonIgnore]
        public OfferEntity? PrimaryOffer { get; set; }
        [JsonIgnore]
        public ICollection<OfferEntity> Offers { get; set; } = [];
    }
}
