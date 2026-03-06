using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.App;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class CalculationHourlyPriceFactorData
    {
        public List<HourlyPriceListGroupDTO> HourlyPrice {  get; set; } = [];
        public List<OHFactors> Factors { get; set; } = [];
    }

    public class CalculationData
    {
        public List<QuanityListDTO> QuanityList { get; set; } = [];

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [Range(0, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double TimeMonth { get; set; } = 12;
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [Range(-999, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int Priority { get; set; } = 50;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]

        public List<AddressDTO> Address { get; set; } = [];
        public List<string> Notes { get; set; } = [];
        public List<string> Responsibles { get; set; } = [];
        public List<UnderContactOrganisationBase> Contacts { get; set; } = [];
        public List<IncomeBase> Income { get; set; } = [];

        public string Maps { get; set; } = string.Empty;
        public string Developer { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsManager { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Designer { get; set; } = string.Empty;
        public string OverviewInfo { get; set; } = string.Empty;
        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ContactPerson { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Supervisor { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Inspector { get; set; } = string.Empty;
    }
    public class CalculationDataBase : CalculationBase
    {
        public List<HourlyPriceListGroupDTO> HourlyPrice { get; set; } = [];
        public List<OHFactors> Factors { get; set; } = [];


        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [Range(0, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double TimeMonth { get; set; } = 12;
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [Range(-999, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int Priority { get; set; } = 50;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]

        public List<AddressDTO> Address { get; set; } = [];
        public List<string> Notes { get; set; } = [];
        public List<string> Responsibles { get; set; } = [];
        public List<UnderContactOrganisationBase> Contacts { get; set; } = [];
        public List<IncomeBase> Income { get; set; } = [];

        public string Maps { get; set; } = string.Empty;
        public string Developer { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsManager { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Designer { get; set; } = string.Empty;
        public string OverviewInfo { get; set; } = string.Empty;
        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ContactPerson { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Supervisor { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Inspector { get; set; } = string.Empty;
    }
    public class CalculationPostDTO
    {
        public int? TemplateId { get; private set; }
        private CalculationData? _metadata;
        public CalculationData Metadata
        {
            get => _metadata ??= new CalculationData();
            private set => _metadata = value;
        }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Name { get; set; } = string.Empty;
        [Range(0, 100, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]

        public int Tax { get; set; } = 25;
        public Procurement Procurement { get; set; }

        public DateTime TenderDeadline { get; set; } = DateTime.Now;
        public DateTime TenderQA { get; set; } = DateTime.Now;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(2);
        public int Order { get; set; }
        public DateTime? PublicationDate { get; set; } = DateTime.Now;
        public DateTime? DecisionDate { get; set; } = DateTime.Now;
        public List<HourlyPriceListGroupDTO> HourlyPrice { get; set; } = [];
        public List<OHFactors> Factors { get; set; } = [];


        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [Range(0, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double TimeMonth { get; set; } = 12;
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [Range(-999, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int Priority { get; set; } = 50;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]

        public List<AddressDTO> Address { get; set; } = [];
        public List<string> Notes { get; set; } = [];
        public List<string> Responsibles { get; set; } = [];
        public List<UnderContactOrganisationBase> Contacts { get; set; } = [];
        public List<IncomeBase> Income { get; set; } = [];

        public string Maps { get; set; } = string.Empty;
        public string Developer { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsManager { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Designer { get; set; } = string.Empty;
        public string OverviewInfo { get; set; } = string.Empty;
        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ContactPerson { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Supervisor { get; set; } = string.Empty;
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Inspector { get; set; } = string.Empty;
        public bool IsPrivate { get; set; }
        public int? StatusId { get; set; }
        public int? ContractId { get; set; }
        public int? TypeId { get; set; }
        public int? OrganisationId { get; set; }
        public int? ProcurementMethodsId { get; set; }
        public int? CompensationId { get; set; }
        public bool IsVisible { get; set; } = true;
    }
    public class CalculationDetailsDTO : CalculationDataBase
    {
        public string Type { get; set; } = string.Empty;
        public string ProcurementMethods { get; set; } = string.Empty;
        public string Compensation { get; set; } = string.Empty;
        public string Contract { get; set; } = string.Empty;
    }
    public class CalculationPageDTO
    {
        public List<OHFactors> Factors { get; set; } = [];
        public List<QuanityListDTO> QuanityList { get; set; } = [];

        public double Tax { get; set; }
        public double TimeMonth { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Responsible { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public int? OrganisationId { get; set; }
        public string Supervisor { get; set; } = string.Empty;
        public string Inspector { get; set; } = string.Empty;
        public string Compensation { get; set; } = string.Empty;
        public string Contract { get; set; } = string.Empty;
        public int? TemplateId { get; set; }
        public decimal AdditionalCostEarnings { get; set; } = 10m;
        public virtual List<TaskListDTO> Tasks { get; set; } = [];
    }
    public class ListCalculationDTO
    {
        public int Order { get; set; }
        public int Id { get; set; }
        public bool IsPrivate { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Responsible { get; set; } = string.Empty;
        //public List<string> Responsibles { get; set; } = [];
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(1);
        public DateTime TenderDeadline { get; set; }
        public DateTime TenderQA { get; set; }
    }

    public class CalculationPageOtherDepartmentDTO : CalculationPageDTO
    {
        public bool Tap1 { get; set; }
        public bool Tap2 { get; set; }
        public bool Tap3 { get; set; }
        public bool Tap4 { get; set; }
        public bool Tap5 { get; set; }
        public bool Tap6 { get; set; }
    }
}
