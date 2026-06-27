using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.Project
{
    public static class ProjectMappingExtensions
    {
        public static ProjectData ToMetadata(this PostProjectDTO dto)
            => ProjectMetadataMapper.Build(dto);
    }

    public sealed class ProjectEntity : AuditableSoftDeletableEntity<Guid>
    {
        [Range(0, 5)]
        public int Priority { get; private set; } = 3;

        [Required, MaxLength(FieldLengths.LongName)]
        public string Name { get; private set; } = string.Empty;

        [MaxLength(FieldLengths.Code)]
        public string? Code { get; private set; }

        // Nullable so a project can have no schedule/tender dates (new projects start empty
        // instead of auto-filling today's date).
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public DateTime? TenderDeadline { get; private set; }
        public DateTime? TenderQA { get; private set; }

        public int SortOrder { get; private set; }

        private ProjectData? _metadata;
        public ProjectData Metadata
        {
            get => _metadata ??= new ProjectData();
            private set => _metadata = ProjectMetadataMapper.Build(value);
        }

        public int? ProjectTypeId { get; private set; }
        [JsonIgnore]
        public TypeEntity? ProjectType { get; private set; }

        public int? ProjectStatusId { get; private set; }
        [JsonIgnore]
        public ProjectStatusEntity? ProjectStatus { get; private set; }

        public Guid FolderId { get; private set; }
        [JsonIgnore]
        public FolderEntity Folder { get; private set; } = null!;

        public int? OrganisationId { get; private set; }
        [JsonIgnore]
        public OrganisationEntity? Organisation { get; private set; }

        public int? ProcurementMethodId { get; private set; }
        [JsonIgnore]
        public ProcurementMethodEntity? ProcurementMethod { get; private set; }

        public int? ProcurementProcedureId { get; private set; }
        [JsonIgnore]
        public ProcurementProcedureEntity? ProcurementProcedure { get; private set; }

        public int? CompensationId { get; private set; }
        [JsonIgnore]
        public CompensationEntity? Compensation { get; private set; }

        public int? ContractId { get; private set; }
        [JsonIgnore]
        public ContractEntity? Contract { get; private set; }

        public bool IsArchived { get; private set; } = false;

        /// <summary>Beräkningsmetod för projektets anbud (Anbud-fönstret).</summary>
        public BidEvaluationModel BidEvaluationModel { get; private set; } = BidEvaluationModel.LowestComparison;

        /// <summary>Utvärderingsgrund för projektets anbud — styr vilka beräkningsmetoder som är tillgängliga.
        /// Standard för nya projekt är Pris och kvalitet (med Lägst jämförelsesumma med mervärdeavdrag).</summary>
        public BidEvaluationBasis BidEvaluationBasis { get; private set; } = BidEvaluationBasis.PriceQuality;

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; private set; } = [];

        [JsonIgnore]
        public ICollection<ProjectBidEntity> Bids { get; private set; } = [];

        /// <summary>Interna delningar av projektet (intern projektdelning).</summary>
        [JsonIgnore]
        public ICollection<ProjectShareEntity> Shares { get; private set; } = [];

        private ProjectEntity() { }

        public static ProjectEntity Create(
            PostProjectDTO dto,
            Guid folderId,
            int createdBy,
            int sortOrder)
        {
            if (folderId == Guid.Empty)
                throw new ValidationException("FolderId is required.");

            var project = new ProjectEntity
            {
                FolderId = folderId,
                CreatedBy = createdBy,
                SortOrder = sortOrder
            };

            project.Update(dto);
            return project;
        }

        public void Update(PostProjectDTO dto)
        {
            Name = NormalizeRequired(dto.Name, nameof(dto.Name), FieldLengths.Name);
            Code = NormalizeOptional(dto.Code, FieldLengths.Code);

            SetDates(dto.StartDate, dto.EndDate);
            TenderDeadline = dto.TenderDeadline;
            TenderQA = dto.TenderQA;
            IsArchived = dto.IsArchived;

            ProjectTypeId = dto.TypeId;
            ProjectStatusId = dto.StatusId;
            OrganisationId = dto.OrganisationId;
            ProcurementMethodId = dto.ProcurementMethodsId;
            ProcurementProcedureId = dto.ProcurementProcedureId;
            CompensationId = dto.CompensationId;
            ContractId = dto.ContractId;

            Metadata = dto.ToMetadata();
        }

        public ProjectData GetMetadataSnapshot()
            => ProjectMetadataMapper.Build(_metadata);

        public void UpdateMetadata(Action<ProjectData> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            Metadata = snapshot;
        }

        public void UpdateOrder(int newOrder) => SortOrder = newOrder;

        public void SetBidEvaluationModel(BidEvaluationModel model) => BidEvaluationModel = model;

        // Set evaluation basis + method together. The method is normalized to one that is
        // valid for the chosen basis so the two can never drift out of sync.
        public void SetBidEvaluation(BidEvaluationBasis basis, BidEvaluationModel method)
        {
            BidEvaluationBasis = basis;
            BidEvaluationModel = basis switch
            {
                BidEvaluationBasis.Price => BidEvaluationModel.LowestComparison,
                BidEvaluationBasis.Cost => BidEvaluationModel.LowestTotalCost,
                BidEvaluationBasis.PriceQuality => method == BidEvaluationModel.HighestPoints
                    ? BidEvaluationModel.HighestPoints
                    : BidEvaluationModel.LowestComparison,
                _ => method
            };
        }

        public void MoveToFolder(Guid folderId)
        {
            if (folderId == Guid.Empty)
                throw new ValidationException("FolderId is required.");

            FolderId = folderId;
        }

        public void SetPriority(int priority)
        {
            if (priority < 0 || priority > 5)
                throw new ValidationException("Priority must be between 0 and 5.");

            Priority = priority;
        }

        public void MarkDeleted(int? deletedBy, DateTime? utcNow = null)
        {
            if (IsDeleted)
                return;

            IsDeleted = true;
            DeletedAt = utcNow ?? DateTime.UtcNow;
            DeletedBy = deletedBy;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
        }

        private void SetDates(DateTime? startDate, DateTime? endDate)
        {
            // Only enforce ordering when both dates are present; either may be empty now.
            if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
                throw new ValidationException("EndDate cannot be before StartDate.");

            StartDate = startDate;
            EndDate = endDate;
        }

        private static string NormalizeRequired(string? value, string fieldName, int maxLength)
        {
            var normalized = value?.Trim();

            if (string.IsNullOrWhiteSpace(normalized))
                throw new ValidationException($"{fieldName} is required.");

            return normalized.Length > maxLength
                ? normalized[..maxLength]
                : normalized;
        }

        private static string? NormalizeOptional(string? value, int maxLength)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            return normalized.Length > maxLength
                ? normalized[..maxLength]
                : normalized;
        }
    }
}
