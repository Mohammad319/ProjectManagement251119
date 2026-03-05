using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.DTO.ProjectAppStorage;
using ProjectManagement.Shared.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class ResourcePostDTO
    {
        public int Id { get; set; }
        public ResourceTypesEnum ResType { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Order { get; set; }
        public bool IsActive { get; set; } = true;
        public ResourceMetadata Data { get; set; } = new();
        public int? OfferId { get; set; }
        public int? AccountId { get; set; }
        public int? StatusId { get; set; }
        public int? ResourceSortId { get; set; }
        public int? ResourceTypeId { get; set; }
        public int? OpportunityId { get; set; }

        public double SortOrder { get; set; }

        [MaxLength(FieldLengths.Comment)]
        public string? Note { get; set; }

        [MaxLength(FieldLengths.Unit)]
        public string? Unit { get; set; }
        [Required]
        public double Quantity { get; set; }
        public double? CO2 { get; set; }
        public decimal Cost { get; set; }
        public decimal? BaseCost { get; set; }
        public double? ChangeFactor1 { get; set; }
        public double? ChangeFactor2 { get; set; }

        //public CostValue Cost { get; private set; } = null!;
        public decimal ActuallyQuantity { get; set; } = 0;
        public decimal WorkedQ { get; set; } = 0;

        [JsonIgnore] public List<string> Formulas { get; set; } = [];
        [JsonIgnore] public bool IsAdded { get; set; }
        [JsonIgnore] public int? GroupId { get; set; } = null;
        [JsonIgnore] public List<ResourcePropertyBindDto> Properties { get; set; } = [];

    }
    public class ResourceStorageListDTO : ResourceBase
    {
        public int Id { get; set; }
        public int GroupId { get; set; }

        public ResourceMetadata Data { get; set; } = new();
        [JsonIgnore] public bool Colspan = false;
    }
    public class ResourceListDTO : ResourceBase
    {
        public double SortOrder { get; set; }

        public ResourceMetadata Data { get; set; } = new();
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
    }
}
