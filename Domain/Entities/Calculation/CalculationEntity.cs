using Domain.Entities.Application;
using Domain.Entities.Base;
using Domain.Entities.Organisation;
using Domain.Entities.Project;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Base.Organisation;
using ProjectManagement.Shared.Base.Project;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.Enums;
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
            private set => _metadata = CloneMetadata(value);
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

        // Denormalized for query performance — الـ Department لا يتغير بعد الإنشاء
        public int DepartmentId { get; private set; }

        [JsonIgnore]
        public DepartmentEntity? Department { get; private set; }

        public void AssignDepartment(int departmentId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(departmentId);

            DepartmentId = departmentId;
        }

        [Required, MaxLength(FieldLengths.Code)]
        public string Code { get; private set; } = string.Empty;

        [Required, MaxLength(FieldLengths.LongName)]
        public string Name { get; private set; } = string.Empty;

        [Range(0, 100)]
        public int Tax { get; private set; } = 25;



        // Nullable so a new calculation starts with empty dates instead of auto-filling today's
        // (possibly already-passed) date.
        public DateTime? TenderDeadline { get; private set; }
        public DateTime? TenderQA { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }

        public int SortOrder { get; private set; }

        private SortConfig? _sort;
        public SortConfig Sort
        {
            get => _sort ??= new SortConfig();
            private set => _sort = (value ?? new SortConfig()).Clone();
        }

        public DisplayOptionsPresetStore DisplayPresets { get; private set; } = new();

        public DateTime? PublicationDate { get; private set; } = DateTime.UtcNow;
        public DateTime? DecisionDate { get; private set; } = DateTime.UtcNow;

        public bool IsPrivate { get; private set; }
        public bool IsArchived { get; private set; } = false;
        public CalculationVersionType CalculationType { get; private set; } = CalculationVersionType.Tender;
        public BidRole BidRole { get; private set; } = BidRole.MainBid;
        public CalculationRole CalculationRole { get; private set; } = CalculationRole.MainBid;

        [MaxLength(FieldLengths.Name)]
        public string CustomCalculationRoleName { get; private set; } = string.Empty;
        public bool IsLocked { get; private set; }
        public DateTime? LockedAtUtc { get; private set; }
        public int? LockedByUserId { get; private set; }
        public int? ApprovedByUserId { get; private set; }
        public string ApprovedByName { get; private set; } = string.Empty;
        public DateTime? ApprovedAtUtc { get; private set; }
        public int? SourceCalculationId { get; private set; }
        public Guid VersionGroupId { get; private set; } = Guid.NewGuid();
        public int VersionNumber { get; private set; } = 1;
        public int? CreatedFromCalculationId { get; private set; }
        public bool IsCurrentVersion { get; private set; } = true;

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

        public int? TemplateColumnId { get; private set; }

        [JsonIgnore]
        public TemplateColumnEntity? TemplateColumn { get; private set; }

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
                DepartmentId = original.DepartmentId,
                Name = original.Name,
                Code = original.Code,
                OrganisationId = original.OrganisationId,
                CompensationId = original.CompensationId,
                ContractId = original.ContractId,
                ProcurementMethodsId = original.ProcurementMethodsId,
                TemplateId = original.TemplateId,
                TemplateColumnId = original.TemplateColumnId,
                Sort = original.Sort,
                Tax = original.Tax,
                PublicationDate = original.PublicationDate,
                DecisionDate = original.DecisionDate,
                HourlyPrice = CloneHourlyPrice(original.HourlyPrice),
                Factors = CloneFactors(original.Factors),
                Metadata = new(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                IsPrivate = original.IsPrivate,
                IsArchived = original.IsArchived,
                TypeId = original.TypeId,
                CalculationType = original.CalculationType,
                BidRole = original.BidRole,
                CalculationRole = original.CalculationRole,
                CustomCalculationRoleName = original.CustomCalculationRoleName,
                IsLocked = false,
                LockedAtUtc = null,
                LockedByUserId = null,
                ApprovedByUserId = null,
                ApprovedByName = string.Empty,
                ApprovedAtUtc = null,
                SourceCalculationId = original.SourceCalculationId,
                VersionGroupId = Guid.NewGuid(),
                VersionNumber = 1,
                CreatedFromCalculationId = null,
                IsCurrentVersion = true,
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

        public void SetTemplateColumn(int? templateColumnId)
        {
            TemplateColumnId = templateColumnId;
        }

        public void UpdateFactors(List<OHFactors> factors)
        {
            Factors = CloneFactors(factors);
        }

        public void UpdateHourlyPriceList(List<HourlyPriceListGroupDTO> hourlyPriceList)
        {
            HourlyPrice = CloneHourlyPrice(hourlyPriceList);
        }

        public CalculationData GetMetadataSnapshot()
            => CloneMetadata(_metadata);

        public void UpdateMetadata(Action<CalculationData> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            var snapshot = GetMetadataSnapshot();
            update(snapshot);
            Metadata = snapshot;
        }

        public void Update(CalculationPostDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            Code = NormalizeRequired(dto.Code, nameof(dto.Code), FieldLengths.Code, "Calculation code is required.");
            Name = NormalizeRequired(dto.Name, nameof(dto.Name), FieldLengths.Name, "Calculation name is required.");

            SetTax(dto.Tax);
            SetDates(dto.StartDate, dto.EndDate);
            SetTenderDates(dto.TenderDeadline, dto.TenderQA, dto.PublicationDate, dto.DecisionDate);
            UpdateOrder(dto.Order);

            var metadata = dto.Metadata;
            var existingImportInfo = GetMetadataSnapshot().ImportInfo;
            if (metadata.ImportInfo is null && existingImportInfo is not null)
                metadata.ImportInfo = existingImportInfo.Clone();

            Metadata = metadata;
            Sort = dto.Sort;
            HourlyPrice = CloneHourlyPrice(dto.HourlyPrice);
            Factors = CloneFactors(dto.Factors);

            SetVisibility(dto.IsPrivate, dto.IsArchived);

            OrganisationId = dto.OrganisationId;
            TypeId = dto.TypeId;
            StatusId = dto.StatusId;
            ProcurementMethodsId = dto.ProcurementMethodsId;
            CompensationId = dto.CompensationId;
            ContractId = dto.ContractId;
            TemplateId = dto.TemplateId;
            TemplateColumnId = dto.TemplateColumnId;
            CalculationType = dto.CalculationType;
            SetCalculationRole(dto.CalculationRole, dto.CustomCalculationRoleName);
        }

        public void InitializeVersionGroup()
        {
            if (VersionGroupId == Guid.Empty)
                VersionGroupId = Guid.NewGuid();

            VersionNumber = VersionNumber <= 0 ? 1 : VersionNumber;
            IsCurrentVersion = true;
        }

        public void MarkAsNewVersion(Guid versionGroupId, int versionNumber, int createdFromCalculationId)
        {
            if (versionGroupId == Guid.Empty)
                throw new ArgumentException("VersionGroupId cannot be empty.", nameof(versionGroupId));

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(versionNumber);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(createdFromCalculationId);

            VersionGroupId = versionGroupId;
            VersionNumber = versionNumber;
            CreatedFromCalculationId = createdFromCalculationId;
            IsCurrentVersion = true;
            IsLocked = false;
            LockedAtUtc = null;
            LockedByUserId = null;
            ApprovedByUserId = null;
            ApprovedByName = string.Empty;
            ApprovedAtUtc = null;
        }

        public void MarkAsNotCurrentVersion()
        {
            IsCurrentVersion = false;
        }

        // Promote this version to the current/head version of its family. Used when the
        // current version is deleted and an existing version must take its place.
        public void MarkAsCurrentVersion()
        {
            IsCurrentVersion = true;
        }

        public void SetCalculationType(CalculationVersionType calculationType)
        {
            CalculationType = calculationType;
        }

        public void SetCalculationRole(CalculationRole role, string? customRoleName)
        {
            CalculationRole = role;
            CustomCalculationRoleName = role == CalculationRole.Custom
                ? NormalizeOptionalText(customRoleName, FieldLengths.Name) ?? string.Empty
                : string.Empty;
            BidRole = ToBidRole(role);
        }

        public void LockAsTenderHistory(int userId)
        {
            CalculationType = CalculationVersionType.Tender;
            IsLocked = true;
            LockedAtUtc = DateTime.UtcNow;
            LockedByUserId = userId > 0 ? userId : null;
        }

        public void ApproveAndLock(int userId, string approvedByName)
        {
            var now = DateTime.UtcNow;
            ApprovedByUserId = userId > 0 ? userId : null;
            ApprovedByName = NormalizeOptionalText(approvedByName, 160) ?? string.Empty;
            ApprovedAtUtc = now;
            IsLocked = true;
            LockedAtUtc = now;
            LockedByUserId = userId > 0 ? userId : null;
        }

        public void MarkAsProductionCopy(int sourceCalculationId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceCalculationId);

            CalculationType = CalculationVersionType.Production;
            SourceCalculationId = sourceCalculationId;
            IsLocked = false;
            LockedAtUtc = null;
            LockedByUserId = null;
            ApprovedByUserId = null;
            ApprovedByName = string.Empty;
            ApprovedAtUtc = null;
        }

        public void MarkAsContractCopy(int sourceCalculationId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceCalculationId);

            CalculationType = CalculationVersionType.Contract;
            SourceCalculationId = sourceCalculationId;
            IsLocked = false;
            LockedAtUtc = null;
            LockedByUserId = null;
            ApprovedByUserId = null;
            ApprovedByName = string.Empty;
            ApprovedAtUtc = null;
        }

        public void SetTax(int tax)
        {
            if (tax < 0 || tax > 100)
                throw new ArgumentOutOfRangeException(nameof(tax), "Tax must be between 0 and 100.");

            Tax = tax;
        }

        public void SetDates(DateTime? start, DateTime? end)
        {
            // Only enforce ordering when both dates are present; either may be empty.
            if (start.HasValue && end.HasValue && end.Value < start.Value)
                throw new ArgumentException("EndDate cannot be before StartDate.");

            StartDate = start;
            EndDate = end;
        }

        public void SetTenderDates(
            DateTime? tenderDeadline,
            DateTime? tenderQA,
            DateTime? publicationDate,
            DateTime? decisionDate)
        {
            TenderDeadline = tenderDeadline;
            TenderQA = tenderQA;
            PublicationDate = publicationDate;
            DecisionDate = decisionDate;
        }

        public void SetVisibility(bool isPrivate, bool isArchived)
        {
            IsPrivate = isPrivate;
            IsArchived = isArchived;
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

        public void MoveToProject(Guid projectId, int departmentId)
        {
            AssignToProject(projectId);
            AssignDepartment(departmentId);
        }

        public void UpdateOrder(int newOrder)
        {
            if (double.IsNaN(newOrder) || double.IsInfinity(newOrder))
                throw new ArgumentOutOfRangeException(nameof(newOrder), "SortOrder must be a finite number.");

            SortOrder = newOrder;
        }

        public void UpdateSort(SortConfig sort)
        {
            Sort = sort;
        }

        public void UpdateDisplayPresets(DisplayOptionsPresetStore store)
        {
            DisplayPresets = DisplayOptionsPresetState.Normalize(store);
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

        private static string? NormalizeOptionalText(string? value, int maxLength)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }

        private static BidRole ToBidRole(CalculationRole role) => role switch
        {
            CalculationRole.Option => BidRole.Option,
            CalculationRole.ChangeOrder => BidRole.AdditionalWork,
            CalculationRole.InternalObject => BidRole.InternalObject,
            _ => BidRole.MainBid
        };

        private static CalculationData CloneMetadata(CalculationData? metadata)
        {
            metadata ??= new CalculationData();

            return new CalculationData
            {
                SchemaVersion = metadata.SchemaVersion,
                QuanityList = metadata.QuanityList?.Select(CloneQuantity).ToList() ?? [],
                TimeMonth = metadata.TimeMonth,
                Priority = metadata.Priority,
                Address = metadata.Address?.Select(CloneAddress).ToList() ?? [],
                Notes = metadata.Notes?.ToList() ?? [],
                Responsibles = metadata.Responsibles?.ToList() ?? [],
                Contacts = metadata.Contacts?.Select(CloneContact).ToList() ?? [],
                Income = metadata.Income?.Select(CloneIncome).ToList() ?? [],
                ImportInfo = metadata.ImportInfo is null ? null : new CalculationImportInfoDTO
                {
                    IsImportedCopy = metadata.ImportInfo.IsImportedCopy,
                    ImportedFrom = metadata.ImportInfo.ImportedFrom,
                    SourceFileName = metadata.ImportInfo.SourceFileName,
                    ImportedBy = metadata.ImportInfo.ImportedBy,
                    ImportedAtUtc = metadata.ImportInfo.ImportedAtUtc,
                    TargetProject = metadata.ImportInfo.TargetProject,
                    ImportedRows = metadata.ImportInfo.ImportedRows,
                    ImportedRowsWithIssues = metadata.ImportInfo.ImportedRowsWithIssues,
                    NotImportedRows = metadata.ImportInfo.NotImportedRows,
                    AutomaticallyMappedValues = metadata.ImportInfo.AutomaticallyMappedValues,
                    ManuallyMappedValues = metadata.ImportInfo.ManuallyMappedValues,
                    MainMappings = [.. metadata.ImportInfo.MainMappings.Select(x => new CalculationImportMappingDTO { Field = x.Field, OriginalValue = x.OriginalValue, MappedValue = x.MappedValue })],
                    Issues = [.. metadata.ImportInfo.Issues.Select(CloneImportIssue)],
                    NotImported = [.. metadata.ImportInfo.NotImported.Select(CloneImportIssue)]
                },
                Maps = metadata.Maps ?? string.Empty,
                Developer = metadata.Developer ?? string.Empty,
                ClientsManager = metadata.ClientsManager ?? string.Empty,
                Designer = metadata.Designer ?? string.Empty,
                OverviewInfo = metadata.OverviewInfo ?? string.Empty,
                ContactPerson = metadata.ContactPerson ?? string.Empty,
                Supervisor = metadata.Supervisor ?? string.Empty,
                Inspector = metadata.Inspector ?? string.Empty
            };
        }

        private static CalculationImportIssueDTO CloneImportIssue(CalculationImportIssueDTO value) => new()
        {
            ProblemType = value.ProblemType,
            OriginalValue = value.OriginalValue,
            AffectedRows = value.AffectedRows,
            Action = value.Action,
            RowNames = [.. value.RowNames]
        };

        private static List<HourlyPriceListGroupDTO> CloneHourlyPrice(List<HourlyPriceListGroupDTO>? groups)
            => groups?.Select(CloneHourlyPriceGroup).ToList() ?? [];

        private static List<OHFactors> CloneFactors(List<OHFactors>? factors)
            => factors?.Select(CloneFactor).ToList() ?? [];

        private static QuanityListDTO CloneQuantity(QuanityListDTO source)
            => new()
            {
                Name = source.Name ?? string.Empty,
                Quantity = source.Quantity
            };

        private static AddressDTO CloneAddress(AddressDTO source)
            => new()
            {
                Street = source.Street ?? string.Empty,
                ZIPCode = source.ZIPCode ?? string.Empty,
                Nr = source.Nr ?? string.Empty,
                City = source.City ?? string.Empty,
                Region = source.Region ?? string.Empty,
                Country = source.Country ?? string.Empty
            };

        private static UnderContactOrganisationBase CloneContact(UnderContactOrganisationBase source)
            => new()
            {
                CommentIsVisible = source.CommentIsVisible,
                FirstName = source.FirstName ?? string.Empty,
                LastName = source.LastName ?? string.Empty,
                Email = source.Email ?? string.Empty,
                Telefone = source.Telefone ?? string.Empty,
                Mobile = source.Mobile ?? string.Empty,
                Department = source.Department ?? string.Empty,
                Note = source.Note ?? string.Empty,
                Status = source.Status
            };

        private static IncomeBase CloneIncome(IncomeBase source)
            => new()
            {
                Year = source.Year,
                Description = source.Description,
                SubType = source.SubType,
                Q1 = source.Q1,
                Q2 = source.Q2,
                Q3 = source.Q3,
                Q4 = source.Q4
            };

        private static OHFactors CloneFactor(OHFactors source)
            => new()
            {
                ResourceType = source.ResourceType,
                SortId = source.SortId,
                ResId = source.ResId,
                IsLocked = source.IsLocked,
                Earnings = source.Earnings,
                Key = source.Key,
                Unit = source.Unit ?? string.Empty,
                DivisionKey = source.DivisionKey,
                Selected = source.Selected ?? "all"
            };

        private static HourlyPriceListGroupDTO CloneHourlyPriceGroup(HourlyPriceListGroupDTO source)
            => new()
            {
                Code = source.Code ?? string.Empty,
                Name = source.Name ?? string.Empty,
                Comment = source.Comment ?? string.Empty,
                SubItemsVisible = source.SubItemsVisible,
                Items = source.Items?.Select(CloneHourlyPriceItem).ToList() ?? []
            };

        private static HourlyPriceListItemDTO CloneHourlyPriceItem(HourlyPriceListItemDTO source)
            => new()
            {
                Code = source.Code ?? string.Empty,
                Name = source.Name ?? string.Empty,
                Unit = source.Unit ?? string.Empty,
                Quantity = source.Quantity,
                CostMarketPrices = source.CostMarketPrices,
                CostSubmittedPrices = source.CostSubmittedPrices,
                Comment = source.Comment ?? string.Empty
            };
    }
}
