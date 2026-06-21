using System.IO.Compression;
using System.Text.Json;
using Application.Extension;
using Application.Feature.Transfer;
using Application.Mapping.CalcItems;
using Application.Mapping.Calculation;
using Application.Mapping.Project;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Factory;
using Persistence.Service.Access;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.DTO.Transfer;

namespace Persistence.Service.Transfer
{
    /// <summary>
    /// External export/import of project/calculation copies (ATACOST packages). The package is JSON zipped as .atacost.
    /// Import always creates a new project/calculation with fresh internal IDs and is fully standalone from the original.
    /// Tenant-specific references (status, account, organisation, templates, etc.) are cleared on import because
    /// the copy may be imported into a different tenant. The economy is preserved in metadata (price/cost/quantity/factors).
    /// </summary>
    public sealed class AtacostTransferService(
        IDbContextFactoryTenant dbFactory,
        ILogger<AtacostTransferService> logger) : IAtacostTransferService
    {
        private const string EntryName = "package.json";

        // User-safe Swedish messages (no technical details — exceptions are logged server-side only).
        private const string MsgCalcTargetMissing = "Målprojektet kunde inte hittas eller tillhör inte ditt företag. Välj ett projekt och försök igen.";
        private const string MsgProjectTargetMissing = "Målmappen kunde inte hittas eller tillhör inte ditt företag. Välj en mapp och försök igen.";
        private const string MsgNotAuthorized = "Du har inte behörighet att importera i detta projekt.";
        private const string MsgRequiredMapping = "Några obligatoriska värden saknar lokal mappning. Välj lokala värden innan import.";
        private const string MsgCalcInvalidFile = "Filen är inte en giltig kalkylkopia.";
        private const string MsgProjectInvalidFile = "Filen är inte en giltig projektkopia.";
        private const string MsgCalcSaveFailed = "Kalkylkopian kunde läsas men kunde inte sparas. Felet har loggats.";
        private const string MsgProjectSaveFailed = "Projektkopian kunde läsas men kunde inte sparas. Felet har loggats.";

        // Thrown internally to abort an import with a precise, user-safe reason. Caught at the import
        // boundary and turned into a structured AtacostImportResultDTO (never surfaced as a raw error).
        private sealed class AtacostImportException(AtacostImportStatus status, string userMessage)
            : Exception(userMessage)
        {
            public AtacostImportStatus Status { get; } = status;
        }

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // ─────────────────────────────── Export ───────────────────────────────

        public async Task<byte[]?> BuildProjectPackageAsync(
            Guid projectId,
            AtacostProjectExportRequest request,
            int userId,
            int? departmentId,
            bool isViewer,
            CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var project = await db.Projects
                .AsNoTracking()
                .Where(ProjectAccessRules.CanSee(userId, departmentId, isViewer))
                .FirstOrDefaultAsync(p => p.Id == projectId, ct);

            if (project is null)
                return null;

            var selectedIds = request.CalculationIds?.Distinct().ToList() ?? [];

            var calcs = await db.Calculations
                .AsNoTracking()
                .Include(c => c.Tasks)
                    .ThenInclude(t => t.Resources)
                .Where(c => c.ProjectId == projectId && !c.IsPrivate && selectedIds.Contains(c.Id))
                .Where(CalculationAccessRules.CanSee(userId, departmentId, isViewer))
                .ToListAsync(ct);

            var projectDto = project.ToPostDto();
            var lookups = await LoadSourceLookupsAsync(db, ct);
            PopulateProjectSourceNames(projectDto, lookups);

            var package = new AtacostPackageDTO
            {
                Kind = AtacostPackageDTO.KindProject,
                ExportedAtUtc = DateTime.UtcNow,
                DisplayName = project.Name ?? string.Empty,
                Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
                Project = new AtacostProjectPayload
                {
                    Project = projectDto,
                    Calculations = [.. calcs.OrderBy(c => c.SortOrder).Select(c => BuildCalculationPayload(c, lookups))]
                }
            };

            return Zip(package);
        }

        public async Task<byte[]?> BuildCalculationPackageAsync(
            int calculationId,
            AtacostCalculationExportRequest request,
            int userId,
            int? departmentId,
            bool isViewer,
            CancellationToken ct = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            // Private calculations can never be exported externally (not even by the owner).
            var calc = await db.Calculations
                .AsNoTracking()
                .Include(c => c.Tasks)
                    .ThenInclude(t => t.Resources)
                .Where(c => c.Id == calculationId && !c.IsPrivate)
                .Where(CalculationAccessRules.CanSee(userId, departmentId, isViewer))
                .FirstOrDefaultAsync(ct);

            if (calc is null)
                return null;

            var lookups = await LoadSourceLookupsAsync(db, ct);

            var package = new AtacostPackageDTO
            {
                Kind = AtacostPackageDTO.KindCalculation,
                ExportedAtUtc = DateTime.UtcNow,
                DisplayName = calc.Name ?? string.Empty,
                Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
                Calculation = BuildCalculationPayload(calc, lookups)
            };

            return Zip(package);
        }

        // ─────────────────────────────── Inspect ──────────────────────────────

        public Task<AtacostPackageInfoDTO> InspectPackageAsync(byte[] fileBytes, CancellationToken ct = default)
        {
            var package = TryUnzip(fileBytes);
            if (package is null)
                return Task.FromResult(new AtacostPackageInfoDTO { IsValid = false });

            var count = package.Kind == AtacostPackageDTO.KindProject
                ? package.Project?.Calculations.Count ?? 0
                : package.Calculation is null ? 0 : 1;

            return Task.FromResult(new AtacostPackageInfoDTO
            {
                IsValid = true,
                Kind = package.Kind,
                DisplayName = package.DisplayName,
                CalculationCount = count,
                Message = package.Message,
                SenderCompany = package.SenderCompany,
                ExportedAtUtc = package.ExportedAtUtc
            });
        }

        // ─────────────────────────────── Preview ──────────────────────────────

        public async Task<AtacostImportPreviewDTO> PreviewProjectPackageAsync(
            byte[] fileBytes, Guid targetFolderId, int userId, IReadOnlyList<AtacostManualMappingDTO>? overrides = null, CancellationToken ct = default)
        {
            var package = TryUnzip(fileBytes);
            if (package is null || package.Kind != AtacostPackageDTO.KindProject || package.Project is null)
                return new AtacostImportPreviewDTO { IsValid = false };

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var lookups = await LoadImportLookupsAsync(db, ct);
            var matchCtx = new MatchContext
            {
                Lookups = lookups,
                Overrides = BuildOverrideMap(overrides),
                Slots = [],
                BuildSlots = true
            };

            // Matching mutates the deserialized copy; that is fine because nothing is persisted here.
            var projectDto = package.Project.Project;
            var calcInfos = new List<CalculationImportInfoDTO>();
            foreach (var payload in package.Project.Calculations)
                calcInfos.Add(ApplyImportMatching(matchCtx, package, payload, projectDto.Name, userId));

            var targetSummary = targetFolderId == Guid.Empty
                ? string.Empty
                : await BuildTargetSummaryAsync(db, targetFolderId, ct);

            var info = ApplyProjectImportMatching(matchCtx, package, projectDto, userId, targetSummary, calcInfos);

            return new AtacostImportPreviewDTO
            {
                IsValid = true,
                CanImport = targetFolderId != Guid.Empty,
                Kind = package.Kind,
                DisplayName = package.DisplayName,
                TargetSummary = targetSummary,
                Message = package.Message,
                SenderCompany = package.SenderCompany,
                CalculationCount = package.Project.Calculations.Count,
                MappableSlots = matchCtx.Slots,
                Info = info
            };
        }

        public async Task<AtacostImportPreviewDTO> PreviewCalculationPackageAsync(
            byte[] fileBytes, Guid targetProjectId, int userId, IReadOnlyList<AtacostManualMappingDTO>? overrides = null, CancellationToken ct = default)
        {
            var package = TryUnzip(fileBytes);
            if (package is null || package.Kind != AtacostPackageDTO.KindCalculation || package.Calculation is null)
                return new AtacostImportPreviewDTO { IsValid = false };

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var lookups = await LoadImportLookupsAsync(db, ct);
            var matchCtx = new MatchContext
            {
                Lookups = lookups,
                Overrides = BuildOverrideMap(overrides),
                Slots = [],
                BuildSlots = true
            };

            var targetSummary = targetProjectId == Guid.Empty
                ? string.Empty
                : await db.Projects.AsNoTracking()
                    .Where(p => p.Id == targetProjectId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync(ct) ?? string.Empty;

            var sourceCalc = package.Calculation.Calculation;
            var info = ApplyImportMatching(matchCtx, package, package.Calculation, targetSummary, userId);

            return new AtacostImportPreviewDTO
            {
                IsValid = true,
                CanImport = targetProjectId != Guid.Empty,
                Kind = package.Kind,
                DisplayName = package.DisplayName,
                TargetSummary = targetSummary,
                Message = package.Message,
                SenderCompany = package.SenderCompany,
                CalculationCount = 1,
                CalcTypeName = sourceCalc.SourceTypeName,
                CalcStatusName = sourceCalc.SourceStatusName,
                CalcRole = sourceCalc.CalculationRole,
                CalcCustomRoleName = sourceCalc.CustomCalculationRoleName,
                MappableSlots = matchCtx.Slots,
                Info = info
            };
        }

        // ─────────────────────────────── Import ───────────────────────────────

        public async Task<AtacostImportResultDTO> ImportProjectPackageAsync(
            byte[] fileBytes,
            Guid targetFolderId,
            int userId,
            int? departmentId,
            bool allowCrossDepartment,
            IReadOnlyList<AtacostManualMappingDTO>? overrides = null,
            CancellationToken ct = default)
        {
            if (targetFolderId == Guid.Empty)
                return AtacostImportResultDTO.Fail(AtacostImportStatus.TargetNotFound, MsgProjectTargetMissing);

            var package = TryUnzip(fileBytes);
            if (package is null || package.Kind != AtacostPackageDTO.KindProject || package.Project is null)
                return AtacostImportResultDTO.Fail(AtacostImportStatus.InvalidFile, MsgProjectInvalidFile);

            await using var db = await dbFactory.CreateDbContextAsync(ct);

            // Tenant-scoped context: a folder in another tenant is not found here (desired isolation).
            var targetDepartmentId = await db.Folders
                .AsNoTracking()
                .Where(f => f.Id == targetFolderId)
                .Select(f => (int?)f.DepartmentId)
                .FirstOrDefaultAsync(ct);

            if (!targetDepartmentId.HasValue)
            {
                logger.LogWarning(
                    "Atacost project import rejected: target folder {FolderId} not found in tenant {TenantId} (user {UserId}).",
                    targetFolderId, db.TenantId, userId);
                return AtacostImportResultDTO.Fail(AtacostImportStatus.TargetNotFound, MsgProjectTargetMissing);
            }

            if (departmentId.HasValue && targetDepartmentId.Value != departmentId.Value && !allowCrossDepartment)
            {
                logger.LogWarning(
                    "Atacost project import rejected: user {UserId} lacks cross-department rights for folder {FolderId} (target dept {TargetDept}, user dept {UserDept}).",
                    userId, targetFolderId, targetDepartmentId, departmentId);
                return AtacostImportResultDTO.Fail(AtacostImportStatus.NotAuthorized, MsgNotAuthorized);
            }

            var existingProjectNames = await db.Projects.AsNoTracking()
                .Where(p => p.FolderId == targetFolderId).Select(p => p.Name).ToListAsync(ct);
            var existingProjectCodes = await db.Projects.AsNoTracking()
                .Select(p => p.Code ?? string.Empty).ToListAsync(ct);

            var projectDto = package.Project.Project;
            projectDto.Name = EnsureUniqueName(projectDto.Name, existingProjectNames);
            projectDto.Code = EnsureUniqueCode(projectDto.Code, existingProjectCodes, FieldLengths.ProjectCode);

            var lookups = await LoadImportLookupsAsync(db, ct);
            var matchCtx = new MatchContext
            {
                Lookups = lookups,
                Overrides = BuildOverrideMap(overrides),
                Slots = [],
                BuildSlots = false
            };

            // Pre-run the per-calc matching (it mutates each calc DTO and builds its import info) so
            // the project's import info can aggregate the calc deviations into one combined view.
            var calcInfos = new List<CalculationImportInfoDTO>();
            foreach (var payload in package.Project.Calculations)
            {
                var info = ApplyImportMatching(matchCtx, package, payload, projectDto.Name, userId);
                payload.Calculation.Metadata.ImportInfo = info;
                calcInfos.Add(info);
            }

            var targetSummary = await BuildTargetSummaryAsync(db, targetFolderId, ct);
            projectDto.Data.ImportInfo = ApplyProjectImportMatching(matchCtx, package, projectDto, userId, targetSummary, calcInfos);
            SanitizeProjectDto(projectDto, targetFolderId);

            var maxOrder = await db.Projects.AsNoTracking()
                .Where(p => p.FolderId == targetFolderId)
                .Select(p => (int?)p.SortOrder).OrderByDescending(x => x).FirstOrDefaultAsync(ct) ?? 0;

            return await RunImportAsync(db, userId, targetFolderId, AtacostPackageDTO.KindProject,
                MsgProjectSaveFailed, async () =>
                {
                    var project = ProjectEntity.Create(projectDto, targetFolderId, userId, maxOrder + 100);
                    project.Id = Guid.NewGuid();
                    db.Projects.Add(project);
                    await db.SaveChangesAsync(ct);

                    var existingCalcNames = new List<string>();
                    var existingCalcCodes = new List<string>();
                    var order = 0;

                    foreach (var payload in package.Project.Calculations)
                    {
                        await CreateCalculationFromPayloadAsync(
                            db, project.Id, targetDepartmentId.Value, userId, payload,
                            existingCalcNames, existingCalcCodes, order += 100, package, project.Name, matchCtx, ct);
                    }

                    logger.LogInformation(
                        "Atacost project import OK. Tenant={TenantId} User={UserId} Folder={FolderId}. " +
                        "Project='{ProjectName}' Calculations={CalcCount}",
                        db.TenantId, userId, targetFolderId, project.Name, package.Project.Calculations.Count);
                    return AtacostImportResultDTO.Ok(project.Id);
                }, ct);
        }

        public async Task<AtacostImportResultDTO> ImportCalculationPackageAsync(
            byte[] fileBytes,
            Guid targetProjectId,
            int userId,
            int? departmentId,
            bool allowCrossDepartment,
            IReadOnlyList<AtacostManualMappingDTO>? overrides = null,
            CancellationToken ct = default)
        {
            if (targetProjectId == Guid.Empty)
                return AtacostImportResultDTO.Fail(AtacostImportStatus.TargetNotFound, MsgCalcTargetMissing);

            var package = TryUnzip(fileBytes);
            if (package is null || package.Kind != AtacostPackageDTO.KindCalculation || package.Calculation is null)
                return AtacostImportResultDTO.Fail(AtacostImportStatus.InvalidFile, MsgCalcInvalidFile);

            await using var db = await dbFactory.CreateDbContextAsync(ct);

            // Validate target. The context is tenant-scoped (global TenantId filter), so a project that
            // belongs to another tenant is simply not found here — which is the desired isolation.
            var target = await db.Projects
                .AsNoTracking()
                .Where(p => p.Id == targetProjectId)
                .Select(p => new { DepartmentId = (int?)p.Folder.DepartmentId, p.Name })
                .FirstOrDefaultAsync(ct);

            if (target is null || !target.DepartmentId.HasValue)
            {
                logger.LogWarning(
                    "Atacost calc import rejected: target project {ProjectId} not found in tenant {TenantId} (user {UserId}).",
                    targetProjectId, db.TenantId, userId);
                return AtacostImportResultDTO.Fail(AtacostImportStatus.TargetNotFound, MsgCalcTargetMissing);
            }

            if (departmentId.HasValue && target.DepartmentId.Value != departmentId.Value && !allowCrossDepartment)
            {
                logger.LogWarning(
                    "Atacost calc import rejected: user {UserId} lacks cross-department rights for project {ProjectId} (target dept {TargetDept}, user dept {UserDept}).",
                    userId, targetProjectId, target.DepartmentId, departmentId);
                return AtacostImportResultDTO.Fail(AtacostImportStatus.NotAuthorized, MsgNotAuthorized);
            }

            var existingCalcNames = await db.Calculations.AsNoTracking()
                .Where(c => c.ProjectId == targetProjectId).Select(c => c.Name).ToListAsync(ct);
            var existingCalcCodes = await db.Calculations.AsNoTracking()
                .Where(c => c.ProjectId == targetProjectId).Select(c => c.Code).ToListAsync(ct);
            var maxOrder = await db.Calculations.AsNoTracking()
                .Where(c => c.ProjectId == targetProjectId)
                .Select(c => (int?)c.SortOrder).OrderByDescending(x => x).FirstOrDefaultAsync(ct) ?? 0;

            var lookups = await LoadImportLookupsAsync(db, ct);
            var matchCtx = new MatchContext
            {
                Lookups = lookups,
                Overrides = BuildOverrideMap(overrides),
                Slots = [],
                BuildSlots = false
            };

            return await RunImportAsync(db, userId, targetProjectId, AtacostPackageDTO.KindCalculation,
                MsgCalcSaveFailed, async () =>
                {
                    var newId = await CreateCalculationFromPayloadAsync(
                        db, targetProjectId, target.DepartmentId.Value, userId, package.Calculation,
                        existingCalcNames, existingCalcCodes, maxOrder + 100, package, target.Name, matchCtx, ct);

                    var info = package.Calculation.Calculation.Metadata.ImportInfo;
                    LogImportInfo("calculation", db.TenantId, userId, targetProjectId, target.Name, info);
                    return AtacostImportResultDTO.Ok(newId);
                }, ct);
        }

        // Wraps the actual write in a single transaction (rolled back on any failure so no half
        // calculation/version/rows survive) and maps internal exceptions to a structured result.
        private async Task<AtacostImportResultDTO> RunImportAsync(
            Persistence.Context.ShardingSingleDbContext db,
            int userId, Guid targetId, string kind, string saveFailedMessage,
            Func<Task<AtacostImportResultDTO>> body, CancellationToken ct)
        {
            try
            {
                // The in-memory provider used by unit tests does not support transactions; only wrap a
                // real (relational) database. EnableRetryOnFailure requires the execution strategy to own
                // the transaction, so the whole write is retried atomically and rolled back on failure.
                if (!db.Database.IsRelational())
                    return await body();

                AtacostImportResultDTO result = AtacostImportResultDTO.Fail(AtacostImportStatus.SaveFailed, saveFailedMessage);
                var strategy = db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await db.Database.BeginTransactionAsync(ct);
                    result = await body();
                    await tx.CommitAsync(ct);
                });
                return result;
            }
            catch (AtacostImportException ex)
            {
                logger.LogWarning("Atacost {Kind} import blocked ({Status}) for target {TargetId}: {Message}",
                    kind, ex.Status, targetId, ex.Message);
                return AtacostImportResultDTO.Fail(ex.Status, ex.Message);
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex,
                    "Atacost {Kind} import DB save failed. Tenant={TenantId} User={UserId} Target={TargetId}. Inner={Inner}",
                    kind, db.TenantId, userId, targetId, ex.InnerException?.Message);
                return AtacostImportResultDTO.Fail(AtacostImportStatus.SaveFailed, saveFailedMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Atacost {Kind} import failed unexpectedly. Tenant={TenantId} User={UserId} Target={TargetId}.",
                    kind, db.TenantId, userId, targetId);
                return AtacostImportResultDTO.Fail(AtacostImportStatus.SaveFailed, saveFailedMessage);
            }
        }

        private void LogImportInfo(string kind, int? tenantId, int userId, Guid targetId, string targetName, CalculationImportInfoDTO? info)
        {
            logger.LogInformation(
                "Atacost {Kind} import OK. Tenant={TenantId} User={UserId} Target={TargetId} ({TargetName}). " +
                "Name='{CalcName}' ImportedRows={Imported} WithIssues={Issues} NotImported={NotImported} " +
                "AutoMapped={Auto} ManualMapped={Manual}",
                kind, tenantId, userId, targetId, targetName,
                info?.SourceFileName, info?.ImportedRows ?? 0, info?.ImportedRowsWithIssues ?? 0,
                info?.NotImportedRows ?? 0, info?.AutomaticallyMappedValues ?? 0, info?.ManuallyMappedValues ?? 0);
        }

        // ─────────────────────────── Building blocks ──────────────────────────

        private static AtacostCalculationPayload BuildCalculationPayload(CalculationEntity calc, SourceLookups lookups)
        {
            var calcDto = calc.ToPostDto();
            PopulateCalcSourceNames(calcDto, lookups);
            return new()
            {
                Calculation = calcDto,
                Tasks = BuildTaskTree(calc.Tasks, lookups)
            };
        }

        private static List<TaskPostDTO> BuildTaskTree(IEnumerable<TaskEntity> allTasks, SourceLookups lookups)
        {
            var byParent = allTasks
                .GroupBy(t => t.ParentTaskId ?? 0)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.SortOrder).ToList());

            List<TaskPostDTO> BuildLevel(int parentKey)
            {
                if (!byParent.TryGetValue(parentKey, out var children))
                    return [];

                return [.. children.Select(t => ToTaskPostDto(t, BuildLevel(t.Id), lookups))];
            }

            return BuildLevel(0);
        }

        private static TaskPostDTO ToTaskPostDto(TaskEntity t, List<TaskPostDTO> children, SourceLookups lookups)
            => new()
            {
                Id = t.Id,
                ParentTaskId = t.ParentTaskId,
                Name = t.Name,
                Metadata = t.GetMetadataSnapshot(),
                Quantity = t.Quantity,
                Unit = t.Unit ?? string.Empty,
                StatusId = t.StatusId,
                OpportunityId = t.OpportunityId,
                SortOrder = t.SortOrder,
                Resources = [.. t.Resources.OrderBy(r => r.SortOrder).Select(r => ToResourcePostDto(r, lookups))],
                Tasks = children
            };

        private static ResourcePostDTO ToResourcePostDto(ResourceEntity r, SourceLookups lookups)
        {
            string? accountCode = null, accountName = null;
            if (r.AccountId.HasValue && lookups.Accounts.TryGetValue(r.AccountId.Value, out var account))
            {
                accountCode = account.Code;
                accountName = account.Name;
            }

            return new()
            {
                Id = r.Id,
                Name = r.Name,
                ResType = r.ResType,
                IsActive = r.IsActive,
                Unit = r.Unit,
                Quantity = r.Quantity ?? 0,
                SortOrder = r.SortOrder,
                Data = r.GetMetadataSnapshot(),
                AccountId = r.AccountId,
                StatusId = r.StatusId,
                ResourceSortId = r.ResourceSortId,
                ResourceTypeId = r.ResourceTypeId,
                OpportunityId = r.OpportunityId,
                OfferId = r.PrimaryOfferId,
                SourceAccountCode = accountCode,
                SourceAccountName = accountName,
                SourceResourceTypeName = LookupName(lookups.ResourceTypes, r.ResourceTypeId),
                SourceResourceSortName = LookupName(lookups.ResourceSorts, r.ResourceSortId)
            };
        }

        private static void PopulateCalcSourceNames(CalculationPostDTO calc, SourceLookups lookups)
        {
            calc.SourceStatusName = LookupName(lookups.Statuses, calc.StatusId);
            calc.SourceTypeName = LookupName(lookups.Types, calc.TypeId);
            calc.SourceCompensationName = LookupName(lookups.Compensations, calc.CompensationId);
            calc.SourceContractName = LookupName(lookups.Contracts, calc.ContractId);
            if (calc.OrganisationId.HasValue && lookups.Organisations.TryGetValue(calc.OrganisationId.Value, out var org))
            {
                calc.SourceOrganisationName = org.Name;
                calc.SourceOrganisationNumber = org.Number;
            }
        }

        private static void PopulateProjectSourceNames(PostProjectDTO project, SourceLookups lookups)
        {
            project.SourceTypeName = LookupName(lookups.Types, project.TypeId);
            project.SourceStatusName = LookupName(lookups.ProjectStatuses, project.StatusId);
            project.SourceProcurementMethodName = LookupName(lookups.ProcurementMethods, project.ProcurementMethodsId);
            project.SourceProcurementProcedureName = LookupName(lookups.ProcurementProcedures, project.ProcurementProcedureId);
            project.SourceCompensationName = LookupName(lookups.Compensations, project.CompensationId);
            project.SourceContractName = LookupName(lookups.Contracts, project.ContractId);
            if (project.OrganisationId.HasValue && lookups.Organisations.TryGetValue(project.OrganisationId.Value, out var org))
            {
                project.SourceOrganisationName = org.Name;
                project.SourceOrganisationNumber = org.Number;
            }
        }

        private static string? LookupName(IReadOnlyDictionary<int, string> map, int? id)
            => id.HasValue && map.TryGetValue(id.Value, out var name) ? name : null;

        private static async Task<int> CreateCalculationFromPayloadAsync(
            Persistence.Context.ShardingSingleDbContext db,
            Guid projectId,
            int departmentId,
            int userId,
            AtacostCalculationPayload payload,
            List<string> existingNames,
            List<string> existingCodes,
            int order,
            AtacostPackageDTO package,
            string targetProjectName,
            MatchContext matchCtx,
            CancellationToken ct)
        {
            var calcDto = payload.Calculation;
            // Match source dropdowns/references by name (plus manual overrides) against the receiving
            // tenant, re-link what matched, and capture the deviations on the DTOs before sanitizing the
            // rest. Project import pre-runs this to aggregate per-calc info, so only build when not set.
            calcDto.Metadata.ImportInfo ??= ApplyImportMatching(matchCtx, package, payload, targetProjectName, userId);
            SanitizeCalculationDto(calcDto);
            calcDto.Name = EnsureUniqueName(calcDto.Name, existingNames);
            calcDto.Code = EnsureUniqueCode(calcDto.Code, existingCodes, FieldLengths.Code);
            calcDto.Order = order;

            var calc = new CalculationEntity();
            calc.AssignToProject(projectId);
            calc.AssignDepartment(departmentId);
            calc.Update(calcDto);
            calc.InitializeVersionGroup();
            calc.CreatedBy = userId;
            calc.CreatedAt = DateTime.UtcNow;

            db.Calculations.Add(calc);
            await db.SaveChangesAsync(ct);

            foreach (var taskDto in payload.Tasks)
            {
                SanitizeTaskDto(taskDto);
                // Drop rows the user chose to exclude (unmatched account/resource refs).
                PruneExcludedResources(taskDto);
                var taskEntity = TaskMapper.MapToTaskEntity(taskDto, calc.Id);
                db.Tasks.Add(taskEntity);
            }

            await db.SaveChangesAsync(ct);

            existingNames.Add(calcDto.Name);
            existingCodes.Add(calcDto.Code);
            return calc.Id;
        }

        // ──────────────────── Manual mapping infrastructure ───────────────────

        // Semantic type keys for a mappable value. Keyed by the underlying lookup table so the same
        // source value is mapped consistently wherever it appears (project + every calculation).
        private static class MapType
        {
            public const string CalcStatus = "calcstatus";
            public const string ProjectStatus = "projstatus";
            public const string Type = "type";
            public const string Compensation = "compensation";
            public const string Contract = "contract";
            public const string Org = "org";
            public const string Account = "account";
            public const string ResType = "restype";
            public const string ResSort = "ressort";
            public const string ProcMethod = "procmethod";
            public const string ProcProc = "procproc";
        }

        private static string LabelFor(string type) => type switch
        {
            MapType.CalcStatus => "Kalkylstatus",
            MapType.ProjectStatus => "Projektstatus",
            MapType.Type => "Projekt-/kalkyltyp",
            MapType.Compensation => "Ersättningsform",
            MapType.Contract => "Entreprenad-/kontraktsform",
            MapType.Org => "Företag/organisation",
            MapType.Account => "Konto",
            MapType.ResType => "Resurstyp",
            MapType.ResSort => "Resurssortering",
            MapType.ProcMethod => "Upphandlingsform",
            MapType.ProcProc => "Upphandlingsförfarande",
            _ => type
        };

        private static string OverrideKey(string type, string? original)
            => $"{type}|{(original ?? string.Empty).Trim().ToLowerInvariant()}";

        private static Dictionary<string, AtacostManualMappingDTO> BuildOverrideMap(IReadOnlyList<AtacostManualMappingDTO>? overrides)
        {
            var map = new Dictionary<string, AtacostManualMappingDTO>(StringComparer.Ordinal);
            if (overrides is null)
                return map;
            foreach (var o in overrides)
            {
                if (!string.IsNullOrWhiteSpace(o.Type))
                    map[OverrideKey(o.Type, o.OriginalValue)] = o;
            }
            return map;
        }

        // Carries the receiving-tenant lookups, the user's manual decisions and (in preview) the
        // collected unmatched slots through the whole matching pass.
        private sealed class MatchContext
        {
            public required ImportLookups Lookups { get; init; }
            public required Dictionary<string, AtacostManualMappingDTO> Overrides { get; init; }
            public required List<AtacostMappableSlotDTO> Slots { get; init; }
            public bool BuildSlots { get; init; }
        }

        private enum RefOutcome { Resolved, Deviation, Excluded }
        private readonly record struct RefResult(RefOutcome Outcome, bool Auto, bool Manual, string? Display, string? Line);

        // Records an unmatched value the user can resolve in the preview. Deduplicates by (type, key)
        // and accumulates the affected-row count, so 100 rows with the same missing account share one slot.
        private static void AddSlot(
            MatchContext ctx, string type, string display, string? key,
            IEnumerable<AtacostLocalOptionDTO> options, int affectedRows, bool rowLevel)
        {
            if (!ctx.BuildSlots)
                return;

            var normKey = (key ?? string.Empty).Trim();
            var existing = ctx.Slots.FirstOrDefault(s => s.Type == type
                && string.Equals(s.OriginalKey, normKey, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                existing.AffectedRows += affectedRows;
                return;
            }

            ctx.Overrides.TryGetValue(OverrideKey(type, key), out var ov);
            ctx.Slots.Add(new AtacostMappableSlotDTO
            {
                Type = type,
                Label = LabelFor(type),
                OriginalValue = display,
                OriginalKey = normKey,
                AffectedRows = affectedRows,
                IsRowLevel = rowLevel,
                Options = [.. options],
                SelectedLocalId = ov?.LocalId,
                SelectedAction = ov?.Action
            });
        }

        // Returns true (and the chosen local id/name) when the user mapped this value to a still-valid local id.
        private static bool TryOverrideMap(
            MatchContext ctx, string type, string? sourceValue,
            IEnumerable<(int Id, string Name)> candidates, out int? id, out string name)
        {
            id = null;
            name = string.Empty;
            if (!ctx.Overrides.TryGetValue(OverrideKey(type, sourceValue), out var ov)
                || ov.Action != "map" || !ov.LocalId.HasValue)
                return false;

            var hit = candidates.Where(c => c.Id == ov.LocalId.Value).Select(c => (int?)c.Id).FirstOrDefault();
            if (hit is null)
                return false;

            id = ov.LocalId;
            name = candidates.First(c => c.Id == ov.LocalId.Value).Name;
            return true;
        }

        private static string? OverrideAction(MatchContext ctx, string type, string? sourceValue)
            => ctx.Overrides.TryGetValue(OverrideKey(type, sourceValue), out var ov) ? ov.Action : null;

        // Matches the imported copy's source dropdowns/references by name against the receiving tenant,
        // then applies any manual override. Matched/overridden values are re-linked; unmatched values
        // are cleared (deviation) or — when the user chose so — their rows are excluded. Surfaces grouped
        // deviations, a per-row Importinfo note and (in preview) the resolvable slots.
        private static CalculationImportInfoDTO ApplyImportMatching(
            MatchContext ctx,
            AtacostPackageDTO package,
            AtacostCalculationPayload payload,
            string targetProjectName,
            int userId)
        {
            var calc = payload.Calculation;
            var tasks = FlattenTasks(payload.Tasks).ToList();
            var resources = tasks.SelectMany(x => x.Resources).ToList();

            var mappings = new List<CalculationImportMappingDTO>();
            var issues = new List<CalculationImportIssueDTO>();
            var notImported = new List<CalculationImportIssueDTO>();
            var auto = 0;
            var manual = 0;

            void Tally((int Auto, int Manual) r) { auto += r.Auto; manual += r.Manual; }

            // ── Calculation-level dropdowns ──
            Tally(MapField(ctx, mappings, MapType.CalcStatus, "Kalkylstatus", calc.SourceStatusName, calc.StatusId,
                ctx.Lookups.Statuses, id => calc.StatusId = id));
            Tally(MapOrganisation(ctx, mappings, calc.SourceOrganisationName, calc.SourceOrganisationNumber,
                calc.OrganisationId, id => calc.OrganisationId = id));
            Tally(MapField(ctx, mappings, MapType.Type, "Kalkyltyp", calc.SourceTypeName, calc.TypeId,
                ctx.Lookups.Types, id => calc.TypeId = id));
            Tally(MapField(ctx, mappings, MapType.Compensation, "Ersättningsform", calc.SourceCompensationName, calc.CompensationId,
                ctx.Lookups.Compensations, id => calc.CompensationId = id));
            Tally(MapField(ctx, mappings, MapType.Contract, "Kontraktsform", calc.SourceContractName, calc.ContractId,
                ctx.Lookups.Contracts, id => calc.ContractId = id));

            // ── Per-resource references ──
            var accountDeviation = new List<(ResourcePostDTO Res, string Display)>();
            var typeDeviation = new List<(ResourcePostDTO Res, string Display)>();
            var sortDeviation = new List<(ResourcePostDTO Res, string Display)>();
            var accountExcluded = new List<(ResourcePostDTO Res, string Display)>();
            var typeExcluded = new List<(ResourcePostDTO Res, string Display)>();
            var sortExcluded = new List<(ResourcePostDTO Res, string Display)>();
            var offerRows = new List<string>();
            var offerCount = 0;
            var excludedCount = 0;

            foreach (var r in resources)
            {
                var acc = MatchResourceAccount(ctx, r);
                var type = MatchResourceRef(ctx, MapType.ResType, r.ResourceTypeId, r.SourceResourceTypeName,
                    ctx.Lookups.ResourceTypes, id => r.ResourceTypeId = id, "Original resurstyp", "Resurstyp");
                var sort = MatchResourceRef(ctx, MapType.ResSort, r.ResourceSortId, r.SourceResourceSortName,
                    ctx.Lookups.ResourceSorts, id => r.ResourceSortId = id, "Original resurssortering", "Resurssortering");

                auto += (acc.Auto ? 1 : 0) + (type.Auto ? 1 : 0) + (sort.Auto ? 1 : 0);
                manual += (acc.Manual ? 1 : 0) + (type.Manual ? 1 : 0) + (sort.Manual ? 1 : 0);

                // Offer links can never be carried across tenants — remember the link, then clear it.
                var hadOffer = r.OfferId.HasValue;
                r.OfferId = null;

                if (acc.Outcome == RefOutcome.Excluded || type.Outcome == RefOutcome.Excluded || sort.Outcome == RefOutcome.Excluded)
                {
                    r.ImportExcluded = true;
                    excludedCount++;
                    if (acc.Outcome == RefOutcome.Excluded) accountExcluded.Add((r, acc.Display!));
                    if (type.Outcome == RefOutcome.Excluded) typeExcluded.Add((r, type.Display!));
                    if (sort.Outcome == RefOutcome.Excluded) sortExcluded.Add((r, sort.Display!));
                    r.Data.ImportInfo = string.Empty;
                    continue;
                }

                var lines = new List<string>();
                if (acc.Outcome == RefOutcome.Deviation) { lines.Add(acc.Line!); accountDeviation.Add((r, acc.Display!)); }
                if (type.Outcome == RefOutcome.Deviation) { lines.Add(type.Line!); typeDeviation.Add((r, type.Display!)); }
                if (sort.Outcome == RefOutcome.Deviation) { lines.Add(sort.Line!); sortDeviation.Add((r, sort.Display!)); }
                if (hadOffer)
                {
                    lines.Add("Original offert · Lokal koppling: Ej kopplad");
                    offerCount++;
                    if (!string.IsNullOrWhiteSpace(r.Name)) offerRows.Add(r.Name);
                }

                r.Data.ImportInfo = string.Join("\n", lines);
            }

            AddGroupedIssues(issues, accountDeviation, "Konto saknade lokal matchning", "Importerades utan konto");
            AddGroupedIssues(issues, typeDeviation, "Resurstyp saknade lokal matchning", "Importerades utan lokal resurstyp");
            AddGroupedIssues(issues, sortDeviation, "Resurssortering saknade lokal matchning", "Importerades utan lokal resurssortering");
            AddGroupedIssues(notImported, accountExcluded, "Konto saknade lokal matchning", "Importerades inte (rader exkluderade)");
            AddGroupedIssues(notImported, typeExcluded, "Resurstyp saknade lokal matchning", "Importerades inte (rader exkluderade)");
            AddGroupedIssues(notImported, sortExcluded, "Resurssortering saknade lokal matchning", "Importerades inte (rader exkluderade)");
            if (offerCount > 0)
            {
                issues.Add(new CalculationImportIssueDTO
                {
                    ProblemType = "Offertkoppling kunde inte överföras",
                    OriginalValue = string.Empty,
                    AffectedRows = offerCount,
                    Action = "Importerades utan offertkoppling",
                    RowNames = [.. offerRows.Distinct().Take(200)]
                });
            }

            var importedRows = tasks.Count + resources.Count(x => !x.ImportExcluded);

            return new CalculationImportInfoDTO
            {
                IsImportedCopy = true,
                ImportedFrom = string.IsNullOrWhiteSpace(package.SenderCompany) ? "Extern ATACOST-app" : package.SenderCompany,
                SourceFileName = $"{(string.IsNullOrWhiteSpace(package.DisplayName) ? "kalkylkopia" : package.DisplayName)}.atacost",
                ImportedBy = $"Användare #{userId}",
                ImportedAtUtc = DateTime.UtcNow,
                TargetProject = targetProjectName,
                ImportedRows = importedRows,
                ImportedRowsWithIssues = resources.Count(x => !x.ImportExcluded && !string.IsNullOrWhiteSpace(x.Data.ImportInfo)),
                NotImportedRows = excludedCount,
                AutomaticallyMappedValues = auto,
                ManuallyMappedValues = manual,
                MainMappings = mappings,
                Issues = issues,
                NotImported = notImported
            };
        }

        private static IEnumerable<TaskPostDTO> FlattenTasks(IEnumerable<TaskPostDTO> tasks)
        {
            foreach (var task in tasks)
            {
                yield return task;
                foreach (var child in FlattenTasks(task.Tasks))
                    yield return child;
            }
        }

        // Matches the project's own dropdowns by name (re-link or clear, same rules as the calc
        // fields) and aggregates the already-computed per-calc deviations so the project's import
        // info gives one combined view. Calc-level row deviations stay grouped per problem type.
        private static CalculationImportInfoDTO ApplyProjectImportMatching(
            MatchContext ctx,
            AtacostPackageDTO package,
            PostProjectDTO project,
            int userId,
            string targetSummary,
            List<CalculationImportInfoDTO> calcInfos)
        {
            var mappings = new List<CalculationImportMappingDTO>();
            var auto = 0;
            var manual = 0;

            void Tally((int Auto, int Manual) r) { auto += r.Auto; manual += r.Manual; }

            Tally(MapField(ctx, mappings, MapType.Type, "Projekttyp", project.SourceTypeName, project.TypeId,
                ctx.Lookups.Types, id => project.TypeId = id));
            Tally(MapField(ctx, mappings, MapType.ProjectStatus, "Projektstatus", project.SourceStatusName, project.StatusId,
                ctx.Lookups.ProjectStatuses, id => project.StatusId = id));
            Tally(MapField(ctx, mappings, MapType.ProcMethod, "Upphandlingsform", project.SourceProcurementMethodName, project.ProcurementMethodsId,
                ctx.Lookups.ProcurementMethods, id => project.ProcurementMethodsId = id));
            Tally(MapField(ctx, mappings, MapType.ProcProc, "Upphandlingsförfarande", project.SourceProcurementProcedureName, project.ProcurementProcedureId,
                ctx.Lookups.ProcurementProcedures, id => project.ProcurementProcedureId = id));
            Tally(MapField(ctx, mappings, MapType.Compensation, "Ersättningsform", project.SourceCompensationName, project.CompensationId,
                ctx.Lookups.Compensations, id => project.CompensationId = id));
            Tally(MapField(ctx, mappings, MapType.Contract, "Entreprenadform", project.SourceContractName, project.ContractId,
                ctx.Lookups.Contracts, id => project.ContractId = id));
            Tally(MapOrganisation(ctx, mappings, project.SourceOrganisationName, project.SourceOrganisationNumber,
                project.OrganisationId, id => project.OrganisationId = id));

            return new CalculationImportInfoDTO
            {
                IsImportedCopy = true,
                ImportedFrom = string.IsNullOrWhiteSpace(package.SenderCompany) ? "Extern ATACOST-app" : package.SenderCompany,
                SourceFileName = $"{(string.IsNullOrWhiteSpace(package.DisplayName) ? "projektkopia" : package.DisplayName)}.atacost",
                ImportedBy = $"Användare #{userId}",
                ImportedAtUtc = DateTime.UtcNow,
                TargetProject = targetSummary,
                ImportedRows = calcInfos.Sum(x => x.ImportedRows),
                ImportedRowsWithIssues = calcInfos.Sum(x => x.ImportedRowsWithIssues),
                NotImportedRows = calcInfos.Sum(x => x.NotImportedRows),
                AutomaticallyMappedValues = auto + calcInfos.Sum(x => x.AutomaticallyMappedValues),
                ManuallyMappedValues = manual + calcInfos.Sum(x => x.ManuallyMappedValues),
                MainMappings = mappings,
                Issues = MergeIssues(calcInfos.SelectMany(x => x.Issues)),
                NotImported = MergeIssues(calcInfos.SelectMany(x => x.NotImported))
            };
        }

        // Combines the same deviation across several calculations into one grouped row.
        private static List<CalculationImportIssueDTO> MergeIssues(IEnumerable<CalculationImportIssueDTO> all)
            => [.. all
                .GroupBy(x => (x.ProblemType, x.OriginalValue))
                .Select(g => new CalculationImportIssueDTO
                {
                    ProblemType = g.Key.ProblemType,
                    OriginalValue = g.Key.OriginalValue,
                    AffectedRows = g.Sum(x => x.AffectedRows),
                    Action = g.First().Action,
                    RowNames = [.. g.SelectMany(x => x.RowNames).Distinct().Take(200)]
                })];

        private static async Task<string> BuildTargetSummaryAsync(
            Persistence.Context.ShardingSingleDbContext db, Guid folderId, CancellationToken ct)
        {
            var row = await db.Folders.AsNoTracking()
                .Where(f => f.Id == folderId)
                .Select(f => new { f.Name, Department = f.Department.Name })
                .FirstOrDefaultAsync(ct);

            if (row is null)
                return string.Empty;

            return string.IsNullOrWhiteSpace(row.Department) ? row.Name : $"{row.Department} / {row.Name}";
        }

        // Matches one main dropdown by name, then by manual override; otherwise clears it.
        // Returns (auto, manual) counts. Records a mapping row and (when unresolved) a preview slot.
        private static (int Auto, int Manual) MapField(
            MatchContext ctx,
            List<CalculationImportMappingDTO> mappings,
            string type,
            string field,
            string? sourceName,
            int? currentId,
            IReadOnlyList<LookupRow> candidates,
            Action<int?> setId)
        {
            if (string.IsNullOrWhiteSpace(sourceName) && !currentId.HasValue)
            {
                setId(null);
                return (0, 0);
            }

            var original = !string.IsNullOrWhiteSpace(sourceName) ? sourceName! : $"ID {currentId}";

            var result = AtacostMatcher.MatchByName(sourceName, candidates);
            if (result.IsMatched)
            {
                var localName = candidates.First(c => c.Id == result.Id!.Value).Name;
                setId(result.Id);
                mappings.Add(new CalculationImportMappingDTO { Field = field, OriginalValue = original, MappedValue = localName });
                return (1, 0);
            }

            // Unresolved by name → offer the slot, then honour any manual choice the user made.
            AddSlot(ctx, type, original, sourceName,
                candidates.Select(c => new AtacostLocalOptionDTO { Id = c.Id, Name = c.Name }), 0, false);

            if (TryOverrideMap(ctx, type, sourceName, candidates.Select(c => (c.Id, c.Name)), out var ovId, out var ovName))
            {
                setId(ovId);
                mappings.Add(new CalculationImportMappingDTO { Field = field, OriginalValue = original, MappedValue = $"{ovName} (manuellt vald)" });
                return (0, 1);
            }

            setId(null);
            mappings.Add(new CalculationImportMappingDTO { Field = field, OriginalValue = original, MappedValue = "Ej mappat" });
            return (0, 0);
        }

        // Matches a company/organisation: by org-number first, then name (see AtacostMatcher), then
        // by manual override. Per spec a local company link is never auto-created when nothing matches.
        private static (int Auto, int Manual) MapOrganisation(
            MatchContext ctx,
            List<CalculationImportMappingDTO> mappings,
            string? sourceName,
            string? sourceNumber,
            int? currentId,
            Action<int?> setId)
        {
            var candidates = ctx.Lookups.Organisations;
            if (string.IsNullOrWhiteSpace(sourceName) && !currentId.HasValue)
            {
                setId(null);
                return (0, 0);
            }

            var original = !string.IsNullOrWhiteSpace(sourceName)
                ? (string.IsNullOrWhiteSpace(sourceNumber) ? sourceName! : $"{sourceName} ({sourceNumber})")
                : $"ID {currentId}";

            var result = AtacostMatcher.MatchOrganisation(sourceNumber, sourceName, candidates);
            if (result.IsMatched)
            {
                var localName = candidates.First(c => c.Id == result.Id!.Value).Name;
                setId(result.Id);
                mappings.Add(new CalculationImportMappingDTO { Field = "Företag/organisation", OriginalValue = original, MappedValue = localName });
                return (1, 0);
            }

            // The slot is keyed by name so the user's pick applies wherever the same company appears.
            AddSlot(ctx, MapType.Org, original, sourceName,
                candidates.Select(c => new AtacostLocalOptionDTO { Id = c.Id, Name = OrgOptionName(c) }), 0, false);

            if (TryOverrideMap(ctx, MapType.Org, sourceName, candidates.Select(c => (c.Id, c.Name)), out var ovId, out var ovName))
            {
                setId(ovId);
                mappings.Add(new CalculationImportMappingDTO { Field = "Företag/organisation", OriginalValue = original, MappedValue = $"{ovName} (manuellt vald)" });
                return (0, 1);
            }

            setId(null);
            mappings.Add(new CalculationImportMappingDTO { Field = "Företag/organisation", OriginalValue = original, MappedValue = "Ej kopplat" });
            return (0, 0);
        }

        private static string OrgOptionName(OrgRow o)
            => string.IsNullOrWhiteSpace(o.Number) ? o.Name : $"{o.Name} ({o.Number})";

        // Matches a resource account by code+name, then by manual override; otherwise records the
        // deviation or — when the user chose so — flags the row for exclusion.
        private static RefResult MatchResourceAccount(MatchContext ctx, ResourcePostDTO r)
        {
            var candidates = ctx.Lookups.Accounts;
            var hasValue = r.AccountId.HasValue
                || !string.IsNullOrWhiteSpace(r.SourceAccountCode)
                || !string.IsNullOrWhiteSpace(r.SourceAccountName);
            if (!hasValue)
            {
                r.AccountId = null;
                return new RefResult(RefOutcome.Resolved, false, false, null, null);
            }

            var display = AccountDisplay(r);
            var result = AtacostMatcher.MatchAccount(r.SourceAccountCode, r.SourceAccountName, candidates);
            if (result.IsMatched)
            {
                r.AccountId = result.Id;
                return new RefResult(RefOutcome.Resolved, true, false, null, null);
            }

            AddSlot(ctx, MapType.Account, display, display,
                candidates.Select(c => new AtacostLocalOptionDTO { Id = c.Id, Name = AccountOptionName(c) }), 1, true);

            if (TryOverrideMap(ctx, MapType.Account, display, candidates.Select(c => (c.Id, AccountOptionName(c))), out var ovId, out _))
            {
                r.AccountId = ovId;
                return new RefResult(RefOutcome.Resolved, false, true, display, null);
            }

            r.AccountId = null;
            if (OverrideAction(ctx, MapType.Account, display) == "exclude")
                return new RefResult(RefOutcome.Excluded, false, false, display, null);

            return new RefResult(RefOutcome.Deviation, false, false, display, $"Originalkonto: {display} · Lokal koppling: Ej mappad");
        }

        private static string AccountDisplay(ResourcePostDTO r)
        {
            var parts = new[] { (r.SourceAccountCode ?? string.Empty).Trim(), (r.SourceAccountName ?? string.Empty).Trim() }
                .Where(s => s.Length > 0);
            var combined = string.Join(" ", parts);
            return combined.Length > 0 ? combined : $"Konto #{r.AccountId}";
        }

        private static string AccountOptionName(AccountRow a)
        {
            var parts = new[] { (a.Code ?? string.Empty).Trim(), (a.Name ?? string.Empty).Trim() }
                .Where(s => s.Length > 0);
            return string.Join(" ", parts);
        }

        // Matches a resource resource-type / resource-sort by name, then by manual override.
        private static RefResult MatchResourceRef(
            MatchContext ctx,
            string type,
            int? currentId,
            string? sourceName,
            IReadOnlyList<LookupRow> candidates,
            Action<int?> setId,
            string originalLabel,
            string genericName)
        {
            if (!currentId.HasValue && string.IsNullOrWhiteSpace(sourceName))
            {
                setId(null);
                return new RefResult(RefOutcome.Resolved, false, false, null, null);
            }

            var display = !string.IsNullOrWhiteSpace(sourceName) ? sourceName! : $"{genericName} #{currentId}";
            var result = AtacostMatcher.MatchByName(sourceName, candidates);
            if (result.IsMatched)
            {
                setId(result.Id);
                return new RefResult(RefOutcome.Resolved, true, false, null, null);
            }

            AddSlot(ctx, type, display, sourceName ?? display,
                candidates.Select(c => new AtacostLocalOptionDTO { Id = c.Id, Name = c.Name }), 1, true);

            if (TryOverrideMap(ctx, type, sourceName ?? display, candidates.Select(c => (c.Id, c.Name)), out var ovId, out _))
            {
                setId(ovId);
                return new RefResult(RefOutcome.Resolved, false, true, display, null);
            }

            setId(null);
            if (OverrideAction(ctx, type, sourceName ?? display) == "exclude")
                return new RefResult(RefOutcome.Excluded, false, false, display, null);

            return new RefResult(RefOutcome.Deviation, false, false, display, $"{originalLabel}: {display} · Lokal koppling: Ej mappad");
        }

        private static void AddGroupedIssues(
            List<CalculationImportIssueDTO> target,
            List<(ResourcePostDTO Res, string Display)> bucket,
            string problemType,
            string action)
        {
            foreach (var group in bucket.GroupBy(x => x.Display))
            {
                target.Add(new CalculationImportIssueDTO
                {
                    ProblemType = problemType,
                    OriginalValue = group.Key,
                    AffectedRows = group.Count(),
                    Action = action,
                    RowNames = [.. group.Select(x => x.Res.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Take(200)]
                });
            }
        }

        // ───────────────────────────── Lookups ────────────────────────────────

        // Source-tenant display names, captured at export so a cross-tenant import can match by name.
        private sealed record SourceLookups(
            Dictionary<int, AccountRow> Accounts,
            Dictionary<int, string> ResourceTypes,
            Dictionary<int, string> ResourceSorts,
            Dictionary<int, string> Statuses,
            Dictionary<int, string> Types,
            Dictionary<int, string> Compensations,
            Dictionary<int, string> Contracts,
            Dictionary<int, OrgRow> Organisations,
            Dictionary<int, string> ProjectStatuses,
            Dictionary<int, string> ProcurementMethods,
            Dictionary<int, string> ProcurementProcedures);

        // Receiving-tenant values, loaded once per import for name-based matching.
        private sealed record ImportLookups(
            List<AccountRow> Accounts,
            List<LookupRow> ResourceTypes,
            List<LookupRow> ResourceSorts,
            List<LookupRow> Statuses,
            List<LookupRow> Types,
            List<LookupRow> Compensations,
            List<LookupRow> Contracts,
            List<OrgRow> Organisations,
            List<LookupRow> ProjectStatuses,
            List<LookupRow> ProcurementMethods,
            List<LookupRow> ProcurementProcedures);

        private static async Task<SourceLookups> LoadSourceLookupsAsync(
            Persistence.Context.ShardingSingleDbContext db, CancellationToken ct)
        {
            var accounts = await db.Accounts.AsNoTracking()
                .Select(x => new { x.Id, x.Code, x.Name }).ToListAsync(ct);
            var resourceTypes = await db.ResourceTypes.AsNoTracking()
                .Select(x => new { x.Id, x.Name }).ToListAsync(ct);
            var resourceSorts = await db.ResourceSorts.AsNoTracking()
                .Select(x => new { x.Id, x.Name }).ToListAsync(ct);
            var statuses = await db.CalculationStatus.AsNoTracking()
                .Select(x => new { x.Id, x.Name }).ToListAsync(ct);
            var types = await db.CalcProjectType.AsNoTracking()
                .Select(x => new { x.Id, x.Name }).ToListAsync(ct);
            var compensations = await db.Compensations.AsNoTracking()
                .Select(x => new { x.Id, x.Name }).ToListAsync(ct);
            var contracts = await db.Contracts.AsNoTracking()
                .Select(x => new { x.Id, x.Name }).ToListAsync(ct);
            // Organisation number lives in the JSON metadata (value-converted), so it cannot be
            // projected in SQL — materialize the rows and read IDNumber in memory.
            var organisations = await db.Organisation.AsNoTracking().ToListAsync(ct);
            var projectStatuses = await db.ProjectStatus.AsNoTracking()
                .Select(x => new { x.Id, x.Name }).ToListAsync(ct);
            var procurementMethods = await db.ProcurementMethod.AsNoTracking()
                .Select(x => new { x.Id, x.Name }).ToListAsync(ct);
            var procurementProcedures = await db.ProcurementProcedure.AsNoTracking()
                .Select(x => new { x.Id, x.Name }).ToListAsync(ct);

            return new SourceLookups(
                accounts.ToDictionary(x => x.Id, x => new AccountRow(x.Id, x.Code, x.Name)),
                resourceTypes.ToDictionary(x => x.Id, x => x.Name),
                resourceSorts.ToDictionary(x => x.Id, x => x.Name),
                statuses.ToDictionary(x => x.Id, x => x.Name),
                types.ToDictionary(x => x.Id, x => x.Name),
                compensations.ToDictionary(x => x.Id, x => x.Name),
                contracts.ToDictionary(x => x.Id, x => x.Name),
                organisations.ToDictionary(x => x.Id, x => new OrgRow(x.Id, x.Name, x.Metadata.IDNumber)),
                projectStatuses.ToDictionary(x => x.Id, x => x.Name),
                procurementMethods.ToDictionary(x => x.Id, x => x.Name),
                procurementProcedures.ToDictionary(x => x.Id, x => x.Name));
        }

        private static async Task<ImportLookups> LoadImportLookupsAsync(
            Persistence.Context.ShardingSingleDbContext db, CancellationToken ct)
        {
            var accounts = await db.Accounts.AsNoTracking()
                .Select(x => new AccountRow(x.Id, x.Code, x.Name)).ToListAsync(ct);
            var resourceTypes = await db.ResourceTypes.AsNoTracking()
                .Select(x => new LookupRow(x.Id, x.Name)).ToListAsync(ct);
            var resourceSorts = await db.ResourceSorts.AsNoTracking()
                .Select(x => new LookupRow(x.Id, x.Name)).ToListAsync(ct);
            var statuses = await db.CalculationStatus.AsNoTracking()
                .Select(x => new LookupRow(x.Id, x.Name)).ToListAsync(ct);
            var types = await db.CalcProjectType.AsNoTracking()
                .Select(x => new LookupRow(x.Id, x.Name)).ToListAsync(ct);
            var compensations = await db.Compensations.AsNoTracking()
                .Select(x => new LookupRow(x.Id, x.Name)).ToListAsync(ct);
            var contracts = await db.Contracts.AsNoTracking()
                .Select(x => new LookupRow(x.Id, x.Name)).ToListAsync(ct);
            // Organisation number lives in the JSON metadata (value-converted), so it cannot be
            // projected in SQL — materialize the rows and read IDNumber in memory.
            var organisationEntities = await db.Organisation.AsNoTracking().ToListAsync(ct);
            var organisations = organisationEntities
                .Select(x => new OrgRow(x.Id, x.Name, x.Metadata.IDNumber)).ToList();
            var projectStatuses = await db.ProjectStatus.AsNoTracking()
                .Select(x => new LookupRow(x.Id, x.Name)).ToListAsync(ct);
            var procurementMethods = await db.ProcurementMethod.AsNoTracking()
                .Select(x => new LookupRow(x.Id, x.Name)).ToListAsync(ct);
            var procurementProcedures = await db.ProcurementProcedure.AsNoTracking()
                .Select(x => new LookupRow(x.Id, x.Name)).ToListAsync(ct);

            return new ImportLookups(
                accounts, resourceTypes, resourceSorts, statuses, types, compensations, contracts, organisations,
                projectStatuses, procurementMethods, procurementProcedures);
        }

        // ──────────────────────── Sanitize (standalone copy) ──────────────────

        private static void SanitizeProjectDto(ProjectManagement.Shared.DTO.Project.PostProjectDTO dto, Guid targetFolderId)
        {
            dto.FolderId = targetFolderId;
            dto.IsArchived = false;
            // Type/status/procurement form & procedure/compensation/contract/organisation are resolved
            // by name in ApplyProjectImportMatching (re-linked when matched, cleared when not), so they
            // are intentionally not touched here.
        }

        private static void SanitizeCalculationDto(CalculationPostDTO dto)
        {
            dto.IsArchived = false;
            dto.IsPrivate = false;
            dto.IsLocked = false;
            // Status/organisation/type/compensation/contract are resolved by name in ApplyImportMatching
            // (re-linked when matched, cleared when not), so they are intentionally not touched here.
            // The remaining tenant-specific references have no name-matching and are always cleared.
            dto.ProcurementMethodsId = null;
            dto.TemplateId = null;
            dto.TemplateColumnId = null;
        }

        // Removes resources the user excluded during manual mapping (recursively through child tasks).
        private static void PruneExcludedResources(TaskPostDTO dto)
        {
            dto.Resources.RemoveAll(r => r.ImportExcluded);
            foreach (var child in dto.Tasks)
                PruneExcludedResources(child);
        }

        private static void SanitizeTaskDto(TaskPostDTO dto)
        {
            dto.Id = 0;
            dto.ParentTaskId = null;
            dto.StatusId = null;
            dto.OpportunityId = null;

            foreach (var res in dto.Resources)
                SanitizeResourceDto(res);

            foreach (var child in dto.Tasks)
                SanitizeTaskDto(child);
        }

        private static void SanitizeResourceDto(ResourcePostDTO dto)
        {
            // Account / resource-type / resource-sort / offer are handled by ApplyImportMatching
            // (re-linked when matched, cleared when not). Only the unmatched-by-design refs remain.
            dto.Id = 0;
            dto.StatusId = null;
            dto.OpportunityId = null;
        }

        // ─────────────────────────────── ZIP I/O ──────────────────────────────

        private static byte[] Zip(AtacostPackageDTO package)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(package, JsonOpts);

            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry(EntryName, CompressionLevel.Optimal);
                using var es = entry.Open();
                es.Write(json, 0, json.Length);
            }

            return ms.ToArray();
        }

        private static AtacostPackageDTO? TryUnzip(byte[] bytes)
        {
            if (bytes is null || bytes.Length == 0)
                return null;

            try
            {
                using var ms = new MemoryStream(bytes);
                using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
                var entry = zip.GetEntry(EntryName);
                if (entry is null)
                    return null;

                using var es = entry.Open();
                return JsonSerializer.Deserialize<AtacostPackageDTO>(es, JsonOpts);
            }
            catch
            {
                return null;
            }
        }

        // ───────────────────────────── Uniqueness ─────────────────────────────

        private static string EnsureUniqueName(string name, IEnumerable<string> existingNames)
        {
            var existing = existingNames.ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            if (!existing.Contains(name))
                return name;

            var baseName = $"{name} - importerad kopia";
            if (!existing.Contains(baseName))
                return baseName;

            var index = 2;
            while (existing.Contains($"{baseName} {index}"))
                index++;

            return $"{baseName} {index}";
        }

        // Codes have a tight max length (calc = 20, project = 80), so the long " - importerad kopia"
        // name suffix would overflow the column. Make the code unique with a short numeric suffix and
        // truncate the base so the result always fits within maxLength.
        private static string EnsureUniqueCode(string? code, IEnumerable<string> existingCodes, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            var existing = existingCodes.ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            var trimmed = code.Trim();
            if (trimmed.Length > maxLength)
                trimmed = trimmed[..maxLength];
            if (!existing.Contains(trimmed))
                return trimmed;

            for (var i = 2; i < 100000; i++)
            {
                var suffix = $"-{i}";
                var baseLen = Math.Min(trimmed.Length, Math.Max(0, maxLength - suffix.Length));
                var candidate = trimmed[..baseLen] + suffix;
                if (candidate.Length > maxLength)
                    candidate = candidate[..maxLength];
                if (!existing.Contains(candidate))
                    return candidate;
            }

            return trimmed[..Math.Min(trimmed.Length, maxLength)];
        }
    }
}
