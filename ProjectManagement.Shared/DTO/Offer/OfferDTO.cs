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
        public string Contact { get; set; }
        public decimal Cost { get; set; }
        public decimal BaseCost { get; set; }
        //[Range(1, int.MaxValue, ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int ResourceId { get; set; }
        public int? OrganisationId { get; set; }
        public int? ContactOrganisationId { get; set; }
    }
    public class OfferData
    {
        public string Comment { get; set; }
        public string Contact { get; set; }
        public decimal Cost { get; set; }
        public decimal BaseCost { get; set; }
    }
    public class ListOfferDTO
    {
        public int Id { get; set; }
        public string Organisation { get; set; }
        public int? OrganisationId { get; set; }
        //public string Unit { get; set; }
        public decimal BaseCost { get; set; }
        public decimal Cost { get; set; }
        public string SubCategory { get; set; }
        public string Category { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
        public string UCFirstName { get; set; }
        public string UCLastName { get; set; }
        public string UCDepartment { get; set; }
        public string UCStatus { get; set; }
        public string UCTelefone { get; set; }
        public string UCMobile { get; set; }
        public string Contact { get; set; }
    }


    public class ListOfferCalcInfo : ListOfferDTO
    {
        public string CalcCode { get; set; }
        public string CalcName { get; set; }

        public string TaskName { get; set; }
        public string TaskCode { get; set; }

        public string ResName { get; set; }
        public string ResCode { get; set; }
    }
}
