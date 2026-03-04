using ProjectManagement.Shared.Base.Offer;
using ProjectManagement.Shared.Enums;
using System;

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

        public double? MaxCost { get; set; }
        public double? MinCost { get; set; }

        public double? MaxBaseCost { get; set; }
        public double? MinBaseCost { get; set; }

        public int? OrganisationId { get; set; }

    }
    public class PostOfferDTO : OfferBase
    {
        public string Contact { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public decimal BaseCost { get; set; }
        //[Range(1, int.MaxValue, ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int ResourceId { get; set; }
        public int? OrganisationId { get; set; }
        public int? ContactOrganisationId { get; set; }
    }
    public class OfferData
    {
        public string Comment { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public decimal BaseCost { get; set; }
    }
    public class ListOfferDTO
    {
        public int Id { get; set; }
        public string Organisation { get; set; } = string.Empty;
        public int? OrganisationId { get; set; }
        //public string Unit { get; set; } = string.Empty;
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
