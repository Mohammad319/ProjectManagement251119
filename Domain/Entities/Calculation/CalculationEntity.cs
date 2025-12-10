using Domain.Entities.Application;
using Domain.Entities.Base;
using Domain.Entities.Organisation;
using Domain.Entities.Project;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class CalculationEntity : AuditableEntity<int>
    {
        public CalculationEntity()
        {
            Tasks = [];
        }

        [Required, MaxLength(FieldLengths.Code)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100)]
        public double Tax { get; set; } = 25;

        public Procurement Procurement { get; set; }

        public DateTime TenderDeadline { get; set; } = DateTime.UtcNow;
        public DateTime TenderQA { get; set; } = DateTime.UtcNow;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddMonths(2);

        public double SortOrder { get; set; }

        public DateTime? PublicationDate { get; set; } = DateTime.UtcNow;
        public DateTime? DecisionDate { get; set; } = DateTime.UtcNow;

        private CalculationData? _metadata;
        public CalculationData Metadata
        {
            get => _metadata ??= new CalculationData();
            set => _metadata = value;
        }

        private CalculationHourlyPriceFactorData? _hourlyPriceFactor;
        public CalculationHourlyPriceFactorData HourlyPriceFactorData
        {
            get => _hourlyPriceFactor ??= new CalculationHourlyPriceFactorData();
            set => _hourlyPriceFactor = value;
        }

        public bool IsPrivate { get; set; }
        public bool IsVisible { get; set; } = true;

        // -----------------------
        // Organisation / Type / Status
        // -----------------------

        public int? OrganisationId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(OrganisationId))]
        public OrganisationEntity? Organisation { get; set; }

        public int? TypeId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(TypeId))]
        public TypeEntity? Type { get; set; }

        public int? StatusId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(StatusId))]
        public StatusEntity? Status { get; set; }

        public int? ProcurementMethodsId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(ProcurementMethodsId))]
        public ProcurementMethodEntity? ProcurementMethods { get; set; }

        public int? CompensationId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(CompensationId))]
        public CompensationEntity? Compensation { get; set; }

        public int? ContractId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(ContractId))]
        public ContractEntity? Contract { get; set; }

        // -----------------------
        // Project
        // -----------------------

        public Guid ProjectId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(ProjectId))]
        public ProjectEntity Project { get; set; } = null!;

        // -----------------------
        // Audit users (from AuditableEntity: CreatedBy, UpdatedBy)
        // -----------------------

        [ForeignKey(nameof(CreatedBy))]
        [JsonIgnore]
        public UserEntity CreatedByUser { get; set; } = null!;

        [ForeignKey(nameof(UpdatedBy))]
        [JsonIgnore]
        public UserEntity? UpdatedByUser { get; set; }

        // -----------------------
        // Template
        // -----------------------

        public int? TemplateId { get; set; }

        [JsonIgnore]
        public TemplateEntity? Template { get; set; }

        // -----------------------
        // Tenders / Attributes
        // -----------------------

        [JsonIgnore]
        public ICollection<TenderAttributeDefinitionEntity> AttributesTender { get; set; } = [];

        [JsonIgnore]
        public ICollection<TenderEntity> Tenders { get; set; } = [];

        // -----------------------
        // Tasks
        // -----------------------

        public ICollection<TaskEntity> Tasks { get; set; } = [];

        // -----------------------
        // Shares / Offers / Opportunities / Applications
        // -----------------------

        public ICollection<ShareCalcEntity> SharesCalc { get; set; } = [];

        [JsonIgnore]
        public ICollection<OfferEntity> Offers { get; set; } = [];

        [JsonIgnore]
        public ICollection<OpportunityEntity> Opportunities { get; set; } = [];

        [JsonIgnore]
        public ICollection<ApplicationValuesEntity> Applications { get; set; } = [];
    }
}
