using ProjectManagement.Shared.Base.Offer;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Helper;
using System;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Offer
{
    public class OfferFilterDTO
    {
        public ResourceTypesEnum? ResType { get; set; }
        public int? ResourceTypeId { get; set; }
        public int? ResourceSortId { get; set; }

        public Guid? FolderID { get; set; }
        public Guid? ProjectID { get; set; }
        public int? CalculationID { get; set; } = 0;
        public int? Account { get; set; }

        public decimal? MaxCost { get; set; }
        public decimal? MinCost { get; set; }

        public decimal? MaxBaseCost { get; set; }
        public decimal? MinBaseCost { get; set; }

        public int? OrganisationId { get; set; }
    }

    public class OfferData
    {
        public string Comment { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public decimal BaseCost { get; set; }

        public OfferData Clone()
        {
            return new OfferData
            {
                Comment = MetadataCloneHelper.CopyText(Comment),
                Contact = MetadataCloneHelper.CopyText(Contact),
                Status = MetadataCloneHelper.CopyText(Status),
                Cost = Cost,
                BaseCost = BaseCost
            };
        }

        public void Normalize()
        {
            Cost = RoundMoney(Cost);
            BaseCost = RoundMoney(BaseCost);
            Comment = MetadataCloneHelper.CopyText(Comment).Trim();
            Contact = MetadataCloneHelper.CopyText(Contact).Trim();
            Status = MetadataCloneHelper.CopyText(Status).Trim();
        }

        private static decimal RoundMoney(decimal value)
            => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public class PostOfferDTO : OfferBase
    {
        private OfferData? data = new();

        [JsonIgnore]
        public OfferData Data
        {
            get
            {
                data ??= new OfferData();
                return data;
            }
            set => data = value?.Clone() ?? new OfferData();
        }

        public string Contact
        {
            get => Data.Contact;
            set => Data.Contact = MetadataCloneHelper.CopyText(value);
        }

        public decimal Cost
        {
            get => Data.Cost;
            set => Data.Cost = value;
        }

        public decimal BaseCost
        {
            get => Data.BaseCost;
            set => Data.BaseCost = value;
        }

        public string Status
        {
            get => Data.Status;
            set => Data.Status = MetadataCloneHelper.CopyText(value);
        }

        public int ResourceId { get; set; }

        /// <summary>
        /// Concurrency token (rowversion). Send this back on updates to detect stale edits.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public int? OrganisationId { get; set; }
        public int? ContactOrganisationId { get; set; }
    }

    public class ListOfferDTO
    {
        public int Id { get; set; }

        /// <summary>
        /// Concurrency token (rowversion). Send this back on updates to detect stale edits.
        /// </summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string Organisation { get; set; } = string.Empty;
        public int? OrganisationId { get; set; }
        public decimal BaseCost { get; set; }
        public decimal Cost { get; set; }
        public string SubCategory { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string UCFirstName { get; set; } = string.Empty;
        public string UCLastName { get; set; } = string.Empty;
        public string UCDepartment { get; set; } = string.Empty;
        public string UCStatus { get; set; } = string.Empty;
        public string UCTelefone { get; set; } = string.Empty;
        public string UCMobile { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ListOfferCalcInfo : ListOfferDTO
    {
        public string CalcCode { get; set; } = string.Empty;
        public string CalcName { get; set; } = string.Empty;

        public string TaskName { get; set; } = string.Empty;
        public string TaskCode { get; set; } = string.Empty;

        public string ResName { get; set; } = string.Empty;
        public string ResCode { get; set; } = string.Empty;
    }
}
