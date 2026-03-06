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

        [Required, MaxLength(FieldLengths.Code)]
        public string Code { get; private set; } = string.Empty;

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        [Range(0, 100)]
        public int Tax { get; private set; } = 25;

        public Procurement Procurement { get; private set; }

        public DateTime TenderDeadline { get; private set; } = DateTime.UtcNow;
        public DateTime TenderQA { get; private set; } = DateTime.UtcNow;
        public DateTime StartDate { get; private set; } = DateTime.UtcNow;
        public DateTime EndDate { get; private set; } = DateTime.UtcNow.AddMonths(2);

        public double SortOrder { get; private set; }

        public DateTime? PublicationDate { get; private set; } = DateTime.UtcNow;
        public DateTime? DecisionDate { get; private set; } = DateTime.UtcNow;

        public bool IsPrivate { get; private set; }
        public bool IsVisible { get; private set; } = true;

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

        public Guid ProjectId { get; private set; }

        [JsonIgnore]
        [ForeignKey(nameof(ProjectId))]
        public ProjectEntity Project { get; private set; } = null!;

        public int? TemplateId { get; private set; }

        [JsonIgnore]
        public TemplateEntity? Template { get; private set; }

        [JsonIgnore]
        public ICollection<TenderAttributeDefinitionEntity> AttributesTender { get; private set; } = [];

        [JsonIgnore]
        public ICollection<TenderEntity> Tenders { get; private set; } = [];

        public ICollection<TaskEntity> Tasks { get; private set; } = [];

        public ICollection<ShareCalcEntity> SharesCalc { get; private set; } = [];

        [JsonIgnore]
        public ICollection<OpportunityEntity> Opportunities { get; private set; } = [];

        [JsonIgnore]
        public ICollection<ApplicationValuesEntity> Applications { get; private set; } = [];

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
                HourlyPrice = original.HourlyPrice,
                Factors = original.Factors,
                Metadata = new(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                IsPrivate = original.IsPrivate,
                IsVisible = original.IsVisible,
                TypeId = original.TypeId,
                StatusId = original.StatusId,
                StartDate = original.StartDate,
                EndDate = original.EndDate,
                TenderDeadline = original.TenderDeadline,
                TenderQA = original.TenderQA
            };

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
            Factors = factors ?? [];
        }

        public void UpdateHourlyPriceList(List<HourlyPriceListGroupDTO> hourlyPriceList)
        {
            HourlyPrice = hourlyPriceList ?? [];
        }

        public void Update(CalculationPostDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            Code = NormalizeRequired(dto.Code, nameof(dto.Code), FieldLengths.Code, "Calculation code is required.");
            Name = NormalizeRequired(dto.Name, nameof(dto.Name), FieldLengths.Name, "Calculation name is required.");

            SetTax(dto.Tax);
            Procurement = dto.Procurement;

            SetDates(dto.StartDate, dto.EndDate);
            SetTenderDates(dto.TenderDeadline, dto.TenderQA, dto.PublicationDate, dto.DecisionDate);
            UpdateOrder(dto.Order);

            Metadata = dto.Metadata ?? new CalculationData();
            HourlyPrice = dto.HourlyPrice ?? [];
            Factors = dto.Factors ?? [];

            SetVisibility(dto.IsPrivate, dto.IsVisible);

            OrganisationId = dto.OrganisationId;
            TypeId = dto.TypeId;
            StatusId = dto.StatusId;
            ProcurementMethodsId = dto.ProcurementMethodsId;
            CompensationId = dto.CompensationId;
            ContractId = dto.ContractId;
            TemplateId = dto.TemplateId;
        }

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
            if (projectId == Guid.Empty)
                throw new ArgumentException("ProjectId cannot be empty.", nameof(projectId));

            ProjectId = projectId;
        }

        public void UpdateOrder(double newOrder)
        {
            if (double.IsNaN(newOrder) || double.IsInfinity(newOrder))
                throw new ArgumentOutOfRangeException(nameof(newOrder), "SortOrder must be a finite number.");

            SortOrder = newOrder;
        }

        private static string NormalizeRequired(string? value, string paramName, int maxLength, string requiredMessage)
        {
            var normalized = (value ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalized))
                throw new ValidationException(requiredMessage);

            if (normalized.Length > maxLength)
                throw new ValidationException($"{paramName} exceeds max length {maxLength}.");

            return normalized;
        }
    }
}
