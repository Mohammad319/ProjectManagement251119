using Domain.Entities.Base;
using Domain.Entities.ResourceType;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.ValueObjects.Calculation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    [Index(nameof(TenantId), nameof(TaskId))]
    public sealed class ResourceEntity : IntBaseEntity
    {
        public ResourceTypesEnum ResType { get; set; }

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;
        public CostValue Cost { get; private set; } = null!;

        public double SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; set; }

        [MaxLength(FieldLengths.Unit)]
        public string? Unit { get; set; }
        [Required]
        public double? Quantity { get; set; }
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

        public void Update(ResourcePostDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required", nameof(dto));

            // --------- بيانات أساسية ---------
            ResType = dto.ResType;
            Name = dto.Name;
            SortOrder = dto.SortOrder;
            IsActive = dto.IsActive;

            Note = dto.Note;
            Unit = dto.Unit;

            Quantity = dto.Quantity;
            CO2 = dto.CO2;

            Metadata = dto.Data;

            // --------- العلاقات ---------
            OpportunityId = dto.OpportunityId;
            AccountId = dto.AccountId;
            StatusId = dto.StatusId;
            ResourceSortId = dto.ResourceSortId;
            ResourceTypeId = dto.ResourceTypeId;
            PrimaryOfferId = dto.OfferId;

            // --------- التكلفة (CostValue) ---------
            if (Cost is null)
            {
                Cost = new CostValue(
                    dto.Cost,
                    dto.BaseCost,
                    dto.ChangeFactor1,
                    dto.ChangeFactor2
                );
            }
            else
            {
                Cost.Set(
                    dto.Cost,
                    dto.BaseCost,
                    dto.ChangeFactor1,
                    dto.ChangeFactor2
                );
            }
        }
        public static ResourceEntity CloneForTask(ResourceEntity r)
        {
            var clone = new ResourceEntity
            {
                Name = r.Name,
                ResType = r.ResType,
                IsActive = r.IsActive,
                Unit = r.Unit,
                Note = r.Note,
                Quantity = r.Quantity,
                CO2 = r.CO2,
                Cost = new CostValue(
                    r.Cost.Cost,
                    r.Cost.BaseCost,
                    r.Cost.ChangeFactor1,
                    r.Cost.ChangeFactor2
                ),
                Metadata = r.Metadata.Clone(),
                SortOrder = r.SortOrder,
            };

            return clone;
        }

        // (اختياري) دالة إنشاء جديدة من DTO:
        public static ResourceEntity Create(int parentTaskId, ResourcePostDTO dto, double sortOrder)
        {
            var entity = new ResourceEntity
            {
                TaskId = parentTaskId,
                SortOrder = sortOrder,
            };

            entity.Update(dto);

            return entity;
        }
    }

}
