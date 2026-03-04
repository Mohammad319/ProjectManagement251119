using Domain.Entities.Application;
using Domain.Entities.Base;
using Domain.Entities.Organisation;
using Domain.Entities.Project;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public sealed class CalculationEntity : AuditableSoftDeletableEntity<int>
    {
        private CalculationData? _metadata;
        public CalculationData Metadata
        {
            get => _metadata ??= new CalculationData();
            private set => _metadata = value;
        }
        public List<HourlyPriceListGroupDTO> HourlyPrice { get; set; }
        public List<OHFactors> Factors { get; set; }
        public CalculationEntity()
        {
            Tasks = [];
            SharesCalc = [];
            Offers = [];
            Opportunities = [];
            AttributesTender = [];
            Tenders = [];
            HourlyPrice = [];
            Factors = [];
        }
        // Denormalized for query performance
        public int DepartmentId { get; private set; }
        public void AssignDepartment(int departmentId)
        {
            if (departmentId <= 0)
                throw new ArgumentOutOfRangeException(nameof(departmentId));

            DepartmentId = departmentId;
        }
        // -----------------------
        // Basic fields
        // -----------------------

        [Required, MaxLength(FieldLengths.Code)]
        public string Code { get; private set; } = string.Empty;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// نسبة الضريبة (0 - 100)
        /// </summary>
        [Range(0, 100)]
        public int Tax { get; private set; } = 25;

        public Procurement Procurement { get; private set; }

        public DateTime TenderDeadline { get; private set; } = DateTime.UtcNow;
        public DateTime TenderQA { get; private set; } = DateTime.UtcNow;
        public DateTime StartDate { get; private set; } = DateTime.UtcNow;
        public DateTime EndDate { get; private set; } = DateTime.UtcNow.AddMonths(2);

        public double SortOrder { get; set; }

        public DateTime? PublicationDate { get; private set; } = DateTime.UtcNow;
        public DateTime? DecisionDate { get; private set; } = DateTime.UtcNow;





        public bool IsPrivate { get; private set; }
        public bool IsVisible { get; private set; } = true;

        // -----------------------
        // Organisation / Type / Status / Procurement / Contracting
        // -----------------------

        public int? OrganisationId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(OrganisationId))]
        public OrganisationEntity? Organisation { get; private set; }

        public int? TypeId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(TypeId))]
        public TypeEntity? Type { get; private set; }

        public int? StatusId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(StatusId))]
        public StatusEntity? Status { get; private set; }

        public int? ProcurementMethodsId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(ProcurementMethodsId))]
        public ProcurementMethodEntity? ProcurementMethods { get; private set; }

        public int? CompensationId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(CompensationId))]
        public CompensationEntity? Compensation { get; private set; }

        public int? ContractId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(ContractId))]
        public ContractEntity? Contract { get; private set; }

        // -----------------------
        // Project
        // -----------------------

        public Guid ProjectId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(ProjectId))]
        public ProjectEntity Project { get; private set; } = null!;

        // -----------------------
        // Template
        // -----------------------

        public int? TemplateId { get; private set; }

        [JsonIgnore]
        public TemplateEntity? Template { get; private set; }

        // -----------------------
        // Tenders / Attributes
        // -----------------------

        [JsonIgnore]
        public ICollection<TenderAttributeDefinitionEntity> AttributesTender { get; private set; } = [];

        [JsonIgnore]
        public ICollection<TenderEntity> Tenders { get; private set; } = [];

        // -----------------------
        // Tasks
        // -----------------------

        public ICollection<TaskEntity> Tasks { get; private set; } = [];

        // -----------------------
        // Shares / Offers / Opportunities / Applications
        // -----------------------

        public ICollection<ShareCalcEntity> SharesCalc { get; private set; } = [];

        [JsonIgnore]
        public ICollection<OfferEntity> Offers { get; private set; } = [];

        [JsonIgnore]
        public ICollection<OpportunityEntity> Opportunities { get; private set; } = [];

        [JsonIgnore]
        public ICollection<ApplicationValuesEntity> Applications { get; private set; } = [];

        // =========================================================
        // Factory + Update methods
        // =========================================================


        public static CalculationEntity CreateCopy(CalculationEntity original, Guid newProjectId, int userId)
        {
            var copy = new CalculationEntity
            {
                ProjectId = newProjectId,
                Name = original.Name,
                Code = original.Code,
                OrganisationId = original.OrganisationId,
                CompensationId = original.CompensationId,
                ContractId = original.ContractId,
                ProcurementMethodsId = original.ProcurementMethodsId,
                Procurement = original.Procurement,
                TemplateId = original.TemplateId,
                Tax = original.Tax,
                PublicationDate = original.PublicationDate,
                DecisionDate = original.DecisionDate,
                HourlyPrice = original.HourlyPrice,//original.HourlyPriceFactorData.Clone(),
                Factors = original.Factors,
                Metadata = new(),//original.Metadata.Clone(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            // clone child tasks + child resources
            foreach (var task in original.Tasks)
                copy.Tasks.Add(TaskEntity.CloneForCalculation(task));

            return copy;
        }
        public void SetTemplate(int? tempId)
        {
            TemplateId = tempId;
        }
        public void UpdateFactors(List<OHFactors> factors)
        {
            Factors = factors;
        }
        public void UpdateHourlyPriceList(List<HourlyPriceListGroupDTO> hourlyPriceList)
        {
            HourlyPrice = hourlyPriceList;
        }
        public void Update(CalculationPostDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new ValidationException("Calculation code is required.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ValidationException("Calculation name is required.");

            Code = dto.Code;
            Name = dto.Name;

            SetTax(dto.Tax);
            Procurement = dto.Procurement;

            SetDates(dto.StartDate, dto.EndDate);
            SetTenderDates(dto.TenderDeadline, dto.TenderQA, dto.PublicationDate, dto.DecisionDate);

            SortOrder = dto.Order;

            Metadata = dto.Metadata ?? new CalculationData();

            IsPrivate = dto.IsPrivate;
            IsVisible = dto.IsVisible;

            OrganisationId = dto.OrganisationId;
            TypeId = dto.TypeId;
            StatusId = dto.StatusId;
            ProcurementMethodsId = dto.ProcurementMethodsId;
            CompensationId = dto.CompensationId;
            ContractId = dto.ContractId;
            TemplateId = dto.TemplateId;
        }

        // =========================================================
        // Small behavior methods (invariants)
        // =========================================================

        public void SetTax(int tax)
        {
            if (tax < 0 || tax > 100)
                throw new ArgumentOutOfRangeException(nameof(tax), "Tax must be between 0 and 100.");

            Tax = tax;
        }

        public void SetDates(DateTime start, DateTime end)
        {
            if (end < start)
                throw new ArgumentException("EndDate cannot be before StartDate.");

            StartDate = start;
            EndDate = end;
        }

        public void SetTenderDates(
            DateTime tenderDeadline,
            DateTime tenderQA,
            DateTime? publicationDate,
            DateTime? decisionDate)
        {
            TenderDeadline = tenderDeadline;
            TenderQA = tenderQA;
            PublicationDate = publicationDate;
            DecisionDate = decisionDate;
        }

        public void SetVisibility(bool isPrivate, bool isVisible)
        {
            IsPrivate = isPrivate;
            IsVisible = isVisible;
        }

        public void SetStatus(int? statusId)
        {
            StatusId = statusId;
        }

        public void AssignToProject(Guid projectId)
        {
            ProjectId = projectId;
        }
    }
}
