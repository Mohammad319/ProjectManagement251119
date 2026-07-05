using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.ResourceType
{
    public class ResourceTypeData
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public decimal Cost { get; set; } = 1m;

        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit { get; set; } = string.Empty;

        public decimal? FixedQ { get; set; }
        public decimal ChangeFactor1 { get; set; } = 1m;
        public decimal ChangeFactor2 { get; set; } = 1m;
        public decimal? BaseCost { get; set; }
        public decimal CapWaste { get; set; }
        public double? CO2 { get; set; }

        /// <summary>
        /// Account ids the admin allows for this resource type/sort. Empty = no restriction.
        /// Stored in the JSON metadata column, so no schema migration is required.
        /// </summary>
        public List<int> AllowedAccountIds { get; set; } = [];

        /// <summary>
        /// Resource sort only: when true the sort defines its own allowed accounts,
        /// otherwise it inherits the allowed accounts from its resource type.
        /// </summary>
        public bool UseOwnAccountSettings { get; set; }

        public void Normalize()
        {
            Cost = RoundMoney(Cost);
            BaseCost = BaseCost.HasValue ? RoundMoney(BaseCost.Value) : null;
            FixedQ = FixedQ.HasValue ? RoundQuantity(FixedQ.Value) : null;

            ChangeFactor1 = RoundFactor(ChangeFactor1);
            ChangeFactor2 = RoundFactor(ChangeFactor2);
            CapWaste = RoundFactor(CapWaste);
        }

        public ResourceTypeData Clone()
        {
            var copy = new ResourceTypeData
            {
                Cost = Cost,
                Unit = MetadataCloneHelper.CopyText(Unit),
                FixedQ = FixedQ,
                ChangeFactor1 = ChangeFactor1,
                ChangeFactor2 = ChangeFactor2,
                BaseCost = BaseCost,
                CapWaste = CapWaste,
                CO2 = CO2,
                AllowedAccountIds = AllowedAccountIds is { Count: > 0 } ? new List<int>(AllowedAccountIds) : [],
                UseOwnAccountSettings = UseOwnAccountSettings
            };

            copy.Normalize();
            return copy;
        }

        private static decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal RoundQuantity(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);
        private static decimal RoundFactor(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);
    }

    public class ResourceTypeBaseData
    {
        private ResourceTypeData? data = new();

        [JsonIgnore]
        public ResourceTypeData Data
        {
            get
            {
                data ??= new ResourceTypeData();
                return data;
            }
            set => data = value?.Clone() ?? new ResourceTypeData();
        }

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;

        public int Order { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsDefault { get; set; }

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public decimal Cost
        {
            get => Data.Cost;
            set => Data.Cost = value;
        }

        [MaxLength(25, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Unit
        {
            get => Data.Unit;
            set => Data.Unit = MetadataCloneHelper.CopyText(value);
        }

        public decimal? FixedQ
        {
            get => Data.FixedQ;
            set => Data.FixedQ = value;
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

        public decimal? BaseCost
        {
            get => Data.BaseCost;
            set => Data.BaseCost = value;
        }

        public decimal CapWaste
        {
            get => Data.CapWaste;
            set => Data.CapWaste = value;
        }

        public double? CO2
        {
            get => Data.CO2;
            set => Data.CO2 = value;
        }

        public List<int> AllowedAccountIds
        {
            get => Data.AllowedAccountIds;
            set => Data.AllowedAccountIds = value ?? [];
        }

        public bool UseOwnAccountSettings
        {
            get => Data.UseOwnAccountSettings;
            set => Data.UseOwnAccountSettings = value;
        }
    }

    public sealed class ResourceSortModel : ResourceTypeBaseData
    {
        public int Id { get; set; }
        public int ResourceTypeId { get; set; }
        public int? AccountId { get; set; }
    }

    public class ResourceTypeModel : ResourceTypeBaseData
    {
        public ResourceTypesEnum Type { get; set; }
        public int Id { get; set; }
        public int? AccountId { get; set; }
    }

    public class PostResourceTypeDTO : ResourceTypeBaseData
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public ResourceTypesEnum Type { get; set; }

        public int? AccountId { get; set; }
    }

    public class ListResourceTypeDTO : ResourceTypeBaseData
    {
        public int Id { get; set; }

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public ResourceTypesEnum Type { get; set; }

        public int? AccountId { get; set; }
        public List<ListResourceSortDTO> ResourcesSort { get; set; } = [];
    }

    public class PostResourceSortDTO : ResourceTypeBaseData
    {
        public int? AccountId { get; set; }
        public int ResourceTypeId { get; set; }
    }

    public class ListResourceSortDTO : ResourceTypeBaseData
    {
        public int Id { get; set; }
        public int? AccountId { get; set; }
    }
}
