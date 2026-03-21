using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectManagement.Shared.DTO.Calculation
{
    public class CalculationHourlyPriceFactorData
    {
        public List<HourlyPriceListGroupDTO> HourlyPrice { get; set; } = [];
        public List<OHFactors> Factors { get; set; } = [];

        public CalculationHourlyPriceFactorData Clone()
        {
            return new CalculationHourlyPriceFactorData
            {
                HourlyPrice = CalculationCloneHelper.CloneHourlyPrice(HourlyPrice),
                Factors = CalculationCloneHelper.CloneFactors(Factors)
            };
        }
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

        public CalculationData Clone()
        {
            return new CalculationData
            {
                QuanityList = CalculationCloneHelper.CloneQuantities(QuanityList),
                TimeMonth = TimeMonth,
                Priority = Priority,
                Address = MetadataCloneHelper.CloneAddresses(Address),
                Notes = MetadataCloneHelper.CloneStrings(Notes),
                Responsibles = MetadataCloneHelper.CloneStrings(Responsibles),
                Contacts = MetadataCloneHelper.CloneContacts(Contacts),
                Income = CalculationCloneHelper.CloneIncome(Income),
                Maps = MetadataCloneHelper.CopyText(Maps),
                Developer = MetadataCloneHelper.CopyText(Developer),
                ClientsManager = MetadataCloneHelper.CopyText(ClientsManager),
                Designer = MetadataCloneHelper.CopyText(Designer),
                OverviewInfo = MetadataCloneHelper.CopyText(OverviewInfo),
                ContactPerson = MetadataCloneHelper.CopyText(ContactPerson),
                Supervisor = MetadataCloneHelper.CopyText(Supervisor),
                Inspector = MetadataCloneHelper.CopyText(Inspector)
            };
        }
    }

    public class CalculationDataBase : CalculationBase
    {
        private CalculationData? data = new();
        private CalculationHourlyPriceFactorData? priceData = new();

        [JsonIgnore]
        public CalculationData Data
        {
            get
            {
                data ??= new CalculationData();
                return data;
            }
            set => data = value?.Clone() ?? new CalculationData();
        }

        [JsonIgnore]
        public CalculationHourlyPriceFactorData PriceData
        {
            get
            {
                priceData ??= new CalculationHourlyPriceFactorData();
                return priceData;
            }
            set => priceData = value?.Clone() ?? new CalculationHourlyPriceFactorData();
        }

        public List<HourlyPriceListGroupDTO> HourlyPrice
        {
            get => PriceData.HourlyPrice;
            set => PriceData.HourlyPrice = CalculationCloneHelper.CloneHourlyPrice(value);
        }

        public List<OHFactors> Factors
        {
            get => PriceData.Factors;
            set => PriceData.Factors = CalculationCloneHelper.CloneFactors(value);
        }

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [Range(0, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double TimeMonth
        {
            get => Data.TimeMonth;
            set => Data.TimeMonth = value;
        }

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [Range(-999, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int Priority
        {
            get => Data.Priority;
            set => Data.Priority = value;
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public List<AddressDTO> Address
        {
            get => Data.Address;
            set => Data.Address = MetadataCloneHelper.CloneAddresses(value);
        }

        public List<string> Notes
        {
            get => Data.Notes;
            set => Data.Notes = MetadataCloneHelper.CloneStrings(value);
        }

        public List<string> Responsibles
        {
            get => Data.Responsibles;
            set => Data.Responsibles = MetadataCloneHelper.CloneStrings(value);
        }

        public List<UnderContactOrganisationBase> Contacts
        {
            get => Data.Contacts;
            set => Data.Contacts = MetadataCloneHelper.CloneContacts(value);
        }

        public List<IncomeBase> Income
        {
            get => Data.Income;
            set => Data.Income = CalculationCloneHelper.CloneIncome(value);
        }

        public string Maps
        {
            get => Data.Maps;
            set => Data.Maps = MetadataCloneHelper.CopyText(value);
        }

        public string Developer
        {
            get => Data.Developer;
            set => Data.Developer = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsManager
        {
            get => Data.ClientsManager;
            set => Data.ClientsManager = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Designer
        {
            get => Data.Designer;
            set => Data.Designer = MetadataCloneHelper.CopyText(value);
        }

        public string OverviewInfo
        {
            get => Data.OverviewInfo;
            set => Data.OverviewInfo = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ContactPerson
        {
            get => Data.ContactPerson;
            set => Data.ContactPerson = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Supervisor
        {
            get => Data.Supervisor;
            set => Data.Supervisor = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Inspector
        {
            get => Data.Inspector;
            set => Data.Inspector = MetadataCloneHelper.CopyText(value);
        }
    }

    public class CalculationPostDTO
    {
        public int? TemplateId { get; set; }

        private CalculationData? metadata = new();
        private CalculationHourlyPriceFactorData? priceData = new();

        public CalculationData Metadata
        {
            get
            {
                metadata ??= new CalculationData();
                return metadata;
            }
            set => metadata = value?.Clone() ?? new CalculationData();
        }

        [JsonIgnore]
        public CalculationData Data
        {
            get => Metadata;
            set => Metadata = value;
        }

        [JsonIgnore]
        public CalculationHourlyPriceFactorData PriceData
        {
            get
            {
                priceData ??= new CalculationHourlyPriceFactorData();
                return priceData;
            }
            set => priceData = value?.Clone() ?? new CalculationHourlyPriceFactorData();
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

        public List<HourlyPriceListGroupDTO> HourlyPrice
        {
            get => PriceData.HourlyPrice;
            set => PriceData.HourlyPrice = CalculationCloneHelper.CloneHourlyPrice(value);
        }

        public List<OHFactors> Factors
        {
            get => PriceData.Factors;
            set => PriceData.Factors = CalculationCloneHelper.CloneFactors(value);
        }

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [Range(0, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public double TimeMonth
        {
            get => Metadata.TimeMonth;
            set => Metadata.TimeMonth = value;
        }

        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        [Range(-999, 999, ErrorMessageResourceName = ErrorsMessages.Range, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public int Priority
        {
            get => Metadata.Priority;
            set => Metadata.Priority = value;
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public List<AddressDTO> Address
        {
            get => Metadata.Address;
            set => Metadata.Address = MetadataCloneHelper.CloneAddresses(value);
        }

        public List<string> Notes
        {
            get => Metadata.Notes;
            set => Metadata.Notes = MetadataCloneHelper.CloneStrings(value);
        }

        public List<string> Responsibles
        {
            get => Metadata.Responsibles;
            set => Metadata.Responsibles = MetadataCloneHelper.CloneStrings(value);
        }

        public List<UnderContactOrganisationBase> Contacts
        {
            get => Metadata.Contacts;
            set => Metadata.Contacts = MetadataCloneHelper.CloneContacts(value);
        }

        public List<IncomeBase> Income
        {
            get => Metadata.Income;
            set => Metadata.Income = CalculationCloneHelper.CloneIncome(value);
        }

        public string Maps
        {
            get => Metadata.Maps;
            set => Metadata.Maps = MetadataCloneHelper.CopyText(value);
        }

        public string Developer
        {
            get => Metadata.Developer;
            set => Metadata.Developer = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ClientsManager
        {
            get => Metadata.ClientsManager;
            set => Metadata.ClientsManager = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Designer
        {
            get => Metadata.Designer;
            set => Metadata.Designer = MetadataCloneHelper.CopyText(value);
        }

        public string OverviewInfo
        {
            get => Metadata.OverviewInfo;
            set => Metadata.OverviewInfo = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(160, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string ContactPerson
        {
            get => Metadata.ContactPerson;
            set => Metadata.ContactPerson = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Supervisor
        {
            get => Metadata.Supervisor;
            set => Metadata.Supervisor = MetadataCloneHelper.CopyText(value);
        }

        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(Resource.ResLocalize))]
        public string Inspector
        {
            get => Metadata.Inspector;
            set => Metadata.Inspector = MetadataCloneHelper.CopyText(value);
        }

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
