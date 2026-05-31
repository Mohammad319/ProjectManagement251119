using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.Base.Calculation
{
    public class QuanityListDTO
    {
        public string Name { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
    }
    public class OHFactors
    {
        public ResourceTypesEnum ResourceType { get; set; }
        public int? SortId { get; set; }
        public int? ResId { get; set; }
        public bool IsLocked { get; set; } = true;
        public decimal Earnings { get; set; } = 20;
        public decimal Key { get; set; } = 0;
        public string Unit { get; set; } = string.Empty;
        public int DivisionKey { get; set; } = 0;
        public string Selected { get; set; } = "all";
    }
    public class CalculationBase
    {
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        [Range(0, 100, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]

        public double Tax { get; set; } = 25;
        public Procurement Procurement { get; set; }
        public BidRole BidRole { get; set; } = BidRole.MainBid;

        public DateTime TenderDeadline { get; set; } = DateTime.Now;
        public DateTime TenderQA { get; set; } = DateTime.Now;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(2);
        public int Order { get; set; }
        public DateTime? PublicationDate { get; set; } = DateTime.Now;
        public DateTime? DecisionDate { get; set; } = DateTime.Now;
    }
}
