using ProjectManagement.Shared.Base.AppTenant;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class ResourcePostDTO
    {
        public int Id { get; set; }
        /// <summary>
        /// Concurrency token (rowversion). Send this back on updates to detect stale edits.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
public ResourceTypesEnum ResType { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ProjectManagement.Shared.Resource.ResLocalize))]
        [MaxLength(FieldLengths.Name, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ProjectManagement.Shared.Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ✅ Alias for SortOrder (old clients may still use Order)
        /// </summary>
        public int Order
        {
            get => SortOrder;
            set => SortOrder = value;
        }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// ✅ مصدر واحد للحقيقة لكل بيانات الحساب (Quantity/Cost/Factors/...)
        /// </summary>
        private ResourceMetadata? data = new();
        private decimal? quantity;
        private string unit = string.Empty;

        public ResourceMetadata Data
        {
            get
            {
                data ??= new ResourceMetadata();
                return data;
            }
            set
            {
                data = CalculationItemMetadataMapper.CloneResourceMetadata(value);
                quantity ??= data.Quantity;
                if (string.IsNullOrWhiteSpace(unit))
                    unit = data.Unit ?? string.Empty;
                SyncDataQuantityUnit();
            }
        }

        public int? OfferId { get; set; }
        public int? AccountId { get; set; }
        public int? StatusId { get; set; }
        public int? ResourceSortId { get; set; }
        public int? ResourceTypeId { get; set; }
        public int? OpportunityId { get; set; }

        public int SortOrder { get; set; }

        // -----------------------------
        // ✅ Proxy properties (منع التكرار/التناقض بين الحقول و Data)
        // -----------------------------

        [MaxLength(FieldLengths.Comment)]
        public string? Note
        {
            get => string.IsNullOrWhiteSpace(Data.Note) ? null : Data.Note;
            set => Data.Note = value ?? string.Empty;
        }

        [MaxLength(FieldLengths.Unit)]
        public string? Unit
        {
            get
            {
                var value = string.IsNullOrWhiteSpace(unit) ? data?.Unit : unit;
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            set
            {
                unit = value ?? string.Empty;
                SyncDataQuantityUnit();
            }
        }

        [Required]
        public decimal Quantity
        {
            get => quantity ?? data?.Quantity ?? 0m;
            set
            {
                quantity = value;
                SyncDataQuantityUnit();
            }
        }

        public double? CO2
        {
            get => Data.CO2;
            set => Data.CO2 = value;
        }

        public decimal Cost
        {
            get => Data.Cost;
            set => Data.Cost = value;
        }

        public decimal? BaseCost
        {
            get => Data.BaseCost;
            set => Data.BaseCost = value;
        }

        public decimal ChangeFactor1
        {
            get => Data.ChangeFactor1;
            set => Data.ChangeFactor1 = value;
        }

        public decimal ChangeFactor2
        {
            get => Data.ChangeFactor2;
            set => Data.ChangeFactor2 = value;
        }

        public List<RoleDTO> CostRole { get; set; } = [];

        // kept for UI state / formulas
        public decimal ActuallyQuantity { get; set; } = 0;
        public decimal WorkedQ { get; set; } = 0;

        [JsonIgnore] public List<string> Formulas { get; set; } = [];
        [JsonIgnore] public bool IsAdded { get; set; }
        [JsonIgnore] public int? GroupId { get; set; } = null;
        [JsonIgnore] public List<ResourcePropertyBindDto> Properties { get; set; } = [];

        private void SyncDataQuantityUnit()
        {
            data ??= new ResourceMetadata();
            data.Quantity = quantity;
            data.Unit = unit ?? string.Empty;
        }
    }

    public class ResourceStorageListDTO : ResourceBase
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        private decimal? quantity;
        private string unit = string.Empty;

        private ResourceMetadata? data = new();

        public ResourceMetadata Data
        {
            get
            {
                data ??= new ResourceMetadata();
                return data;
            }
            set
            {
                data = CalculationItemMetadataMapper.CloneResourceMetadata(value);
                quantity ??= data.Quantity;
                if (string.IsNullOrWhiteSpace(unit))
                    unit = data.Unit ?? string.Empty;
                SyncDataQuantityUnit();
            }
        }
        public decimal? Quantity
        {
            get => quantity ?? data?.Quantity;
            set
            {
                quantity = value;
                SyncDataQuantityUnit();
            }
        }

        public string Unit
        {
            get => string.IsNullOrWhiteSpace(unit) ? data?.Unit ?? string.Empty : unit;
            set
            {
                unit = value ?? string.Empty;
                SyncDataQuantityUnit();
            }
        }

        [JsonIgnore] public bool Colspan = false;

        private void SyncDataQuantityUnit()
        {
            data ??= new ResourceMetadata();
            data.Quantity = quantity;
            data.Unit = unit ?? string.Empty;
        }
    }

    public class ResourceListDTO : ResourceBase
    {
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        private decimal? quantity;
        private string unit = string.Empty;

        private ResourceMetadata? data = new();

        public ResourceMetadata Data
        {
            get
            {
                data ??= new ResourceMetadata();
                return data;
            }
            set
            {
                data = CalculationItemMetadataMapper.CloneResourceMetadata(value);
                quantity ??= data.Quantity;
                if (string.IsNullOrWhiteSpace(unit))
                    unit = data.Unit ?? string.Empty;
                SyncDataQuantityUnit();
            }
        }
        public decimal? Quantity
        {
            get => quantity ?? data?.Quantity;
            set
            {
                quantity = value;
                SyncDataQuantityUnit();
            }
        }

        public string Unit
        {
            get => string.IsNullOrWhiteSpace(unit) ? data?.Unit ?? string.Empty : unit;
            set
            {
                unit = value ?? string.Empty;
                SyncDataQuantityUnit();
            }
        }

        public int TaskId { get; set; }
        public int? OfferId { get; set; }
        public int? OpportunityId { get; set; }
        public string Opportunity { get; set; } = string.Empty;
        public int Id { get; set; }
        public int? AccountId { get; set; }
        public string Account { get; set; } = string.Empty;
        public string AccountCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public int? StatusId { get; set; }
        public int? ResourceSortId { get; set; }
        public int? ResourceTypeId { get; set; }
        public string ResName { get; set; } = string.Empty;
        public string Sort { get; set; } = string.Empty;
        public List<ListOfferDTO> Offers { get; set; } = [];

        private void SyncDataQuantityUnit()
        {
            data ??= new ResourceMetadata();
            data.Quantity = quantity;
            data.Unit = unit ?? string.Empty;
        }
    }
}
