using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Project;
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

        [Required, MaxLength(FieldLengths.Name)]
        public string Name { get; private set; } = string.Empty;

        [MaxLength(FieldLengths.Code)]
        public string? Code { get; private set; }

        public DateTime StartDate { get; private set; } = DateTime.UtcNow;
        public DateTime EndDate { get; private set; } = DateTime.UtcNow.AddMonths(2);
        public DateTime TenderDeadline { get; private set; } = DateTime.UtcNow;
        public DateTime TenderQA { get; private set; } = DateTime.UtcNow;

        public int SortOrder { get; private set; }

        private ProjectData? _metadata;
        public ProjectData Metadata
        {
            get => _metadata ??= new ProjectData();
            private set => _metadata = ProjectMetadataMapper.Build(value);
        }

        public int? ProjectTypeId { get; private set; }
        public TypeEntity? ProjectType { get; private set; }

        public Guid FolderId { get; private set; }
        public FolderEntity Folder { get; private set; } = null!;

        public int? OrganisationId { get; private set; }
        public OrganisationEntity? Organisation { get; private set; }

        public int? ProcurementMethodId { get; private set; }
        public ProcurementMethodEntity? ProcurementMethod { get; private set; }

        public int? CompensationId { get; private set; }
        public CompensationEntity? Compensation { get; private set; }

        public int? ContractId { get; private set; }
        public ContractEntity? Contract { get; private set; }

        public bool IsVisible { get; private set; } = true;

        [JsonIgnore]
        public ICollection<CalculationEntity> Calculations { get; private set; } = [];

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
            IsVisible = dto.IsVisible;

            ProjectTypeId = dto.TypeId;
            OrganisationId = dto.OrganisationId;
            ProcurementMethodId = dto.ProcurementMethodsId;
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

        private void SetDates(DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
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
