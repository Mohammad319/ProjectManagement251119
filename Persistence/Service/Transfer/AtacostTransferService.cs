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
using Persistence.Factory;
using Persistence.Service.Access;
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
    public sealed class AtacostTransferService(IDbContextFactoryTenant dbFactory) : IAtacostTransferService
    {
        private const string EntryName = "package.json";

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
            byte[] fileBytes, Guid targetFolderId, int userId, CancellationToken ct = default)
        {
            var package = TryUnzip(fileBytes);
            if (package is null || package.Kind != AtacostPackageDTO.KindProject || package.Project is null)
                return new AtacostImportPreviewDTO { IsValid = false };

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var lookups = await LoadImportLookupsAsync(db, ct);

            // Matching mutates the deserialized copy; that is fine because nothing is persisted here.
            var projectDto = package.Project.Project;
            var calcInfos = new List<CalculationImportInfoDTO>();
            foreach (var payload in package.Project.Calculations)
                calcInfos.Add(ApplyImportMatching(package, payload, projectDto.Name, userId, lookups));

            var targetSummary = targetFolderId == Guid.Empty
                ? string.Empty
                : await BuildTargetSummaryAsync(db, targetFolderId, ct);

            var info = ApplyProjectImportMatching(package, projectDto, userId, targetSummary, lookups, calcInfos);

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
                Info = info
            };
        }

        public async Task<AtacostImportPreviewDTO> PreviewCalculationPackageAsync(
            byte[] fileBytes, Guid targetProjectId, int userId, CancellationToken ct = default)
        {
            var package = TryUnzip(fileBytes);
            if (package is null || package.Kind != AtacostPackageDTO.KindCalculation || package.Calculation is null)
                return new AtacostImportPreviewDTO { IsValid = false };

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var lookups = await LoadImportLookupsAsync(db, ct);

            var targetSummary = targetProjectId == Guid.Empty
                ? string.Empty
                : await db.Projects.AsNoTracking()
                    .Where(p => p.Id == targetProjectId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync(ct) ?? string.Empty;

            var info = ApplyImportMatching(package, package.Calculation, targetSummary, userId, lookups);

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
                Info = info
            };
        }

        // ─────────────────────────────── Import ───────────────────────────────

        public async Task<Guid> ImportProjectPackageAsync(
            byte[] fileBytes,
            Guid targetFolderId,
            int userId,
            int? departmentId,
            bool allowCrossDepartment,
            CancellationToken ct = default)
        {
            if (targetFolderId == Guid.Empty)
                return Guid.Empty;

            var package = TryUnzip(fileBytes);
            if (package is null || package.Kind != AtacostPackageDTO.KindProject || package.Project is null)
                return Guid.Empty;

            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var targetDepartmentId = await db.Folders
                .AsNoTracking()
                .Where(f => f.Id == targetFolderId)
                .Select(f => (int?)f.DepartmentId)
                .FirstOrDefaultAsync(ct);

            if (!targetDepartmentId.HasValue)
                return Guid.Empty;

            if (departmentId.HasValue && targetDepartmentId.Value != departmentId.Value && !allowCrossDepartment)
                return Guid.Empty;

            var existingProjectNames = await db.Projects
                .AsNoTracking()
                .Where(p => p.FolderId == targetFolderId)
                .Select(p => p.Name)
                .ToListAsync(ct);

            var existingProjectCodes = await db.Projects
                .AsNoTracking()
                .Select(p => p.Code ?? string.Empty)
                .ToListAsync(ct);

            var projectDto = package.Project.Project;
            projectDto.Name = EnsureUniqueName(projectDto.Name, existingProjectNames);
            projectDto.Code = EnsureUniqueCode(projectDto.Code, existingProjectCodes);

            var lookups = await LoadImportLookupsAsync(db, ct);

            // Pre-run the per-calc matching (it mutates each calc DTO and builds its import info) so
            // the project's import info can aggregate the calc deviations into one combined view.
            var calcInfos = new List<CalculationImportInfoDTO>();
            foreach (var payload in package.Project.Calculations)
            {
                var info = ApplyImportMatching(package, payload, projectDto.Name, userId, lookups);
                payload.Calculation.Metadata.ImportInfo = info;
                calcInfos.Add(info);
            }

            var targetSummary = await BuildTargetSummaryAsync(db, targetFolderId, ct);
            projectDto.Data.ImportInfo = ApplyProjectImportMatching(package, projectDto, userId, targetSummary, lookups, calcInfos);
            SanitizeProjectDto(projectDto, targetFolderId);

            var maxOrder = await db.Projects
                .AsNoTracking()
                .Where(p => p.FolderId == targetFolderId)
                .Select(p => (int?)p.SortOrder)
                .OrderByDescending(x => x)
                .FirstOrDefaultAsync(ct) ?? 0;

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
                    existingCalcNames, existingCalcCodes, order += 100, package, project.Name, lookups, ct);
            }

            return project.Id;
        }

        public async Task<int> ImportCalculationPackageAsync(
            byte[] fileBytes,
            Guid targetProjectId,
            int userId,
            int? departmentId,
            bool allowCrossDepartment,
            CancellationToken ct = default)
        {
            if (targetProjectId == Guid.Empty)
                return 0;

            var package = TryUnzip(fileBytes);
            if (package is null || package.Kind != AtacostPackageDTO.KindCalculation || package.Calculation is null)
                return 0;

            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var targetProjectDepartmentId = await db.Projects
                .AsNoTracking()
                .Where(p => p.Id == targetProjectId)
                .Select(p => (int?)p.Folder.DepartmentId)
                .FirstOrDefaultAsync(ct);

            if (!targetProjectDepartmentId.HasValue)
                return 0;

            if (departmentId.HasValue && targetProjectDepartmentId.Value != departmentId.Value && !allowCrossDepartment)
                return 0;

            var existingCalcNames = await db.Calculations
                .AsNoTracking()
                .Where(c => c.ProjectId == targetProjectId)
                .Select(c => c.Name)
                .ToListAsync(ct);

            var existingCalcCodes = await db.Calculations
                .AsNoTracking()
                .Where(c => c.ProjectId == targetProjectId)
                .Select(c => c.Code)
                .ToListAsync(ct);

            var maxOrder = await db.Calculations
                .AsNoTracking()
                .Where(c => c.ProjectId == targetProjectId)
                .Select(c => (int?)c.SortOrder)
                .OrderByDescending(x => x)
                .FirstOrDefaultAsync(ct) ?? 0;

            var targetProjectName = await db.Projects
                .AsNoTracking()
                .Where(p => p.Id == targetProjectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct) ?? string.Empty;

            var lookups = await LoadImportLookupsAsync(db, ct);

            return await CreateCalculationFromPayloadAsync(
                db, targetProjectId, targetProjectDepartmentId.Value, userId, package.Calculation,
                existingCalcNames, existingCalcCodes, maxOrder + 100, package, targetProjectName, lookups, ct);
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
            ImportLookups lookups,
            CancellationToken ct)
        {
            var calcDto = payload.Calculation;
            // Match source dropdowns/references by name against the receiving tenant, re-link what
            // matched, and capture the deviations on the DTOs before sanitizing the rest. Project
            // import pre-runs this to aggregate per-calc info, so only build it when not already set.
            calcDto.Metadata.ImportInfo ??= ApplyImportMatching(package, payload, targetProjectName, userId, lookups);
            SanitizeCalculationDto(calcDto);
            calcDto.Name = EnsureUniqueName(calcDto.Name, existingNames);
            calcDto.Code = EnsureUniqueCode(calcDto.Code, existingCodes);
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
                var taskEntity = TaskMapper.MapToTaskEntity(taskDto, calc.Id);
                db.Tasks.Add(taskEntity);
            }

            await db.SaveChangesAsync(ct);

            existingNames.Add(calcDto.Name);
            existingCodes.Add(calcDto.Code);
            return calc.Id;
        }

        // Matches the imported copy's source dropdowns/references by name against the receiving
        // tenant. Matched values are re-linked (the source id is replaced by the local id);
        // unmatched values are left for the sanitizer to clear and surfaced as grouped deviations
        // plus a per-row Importinfo note. Returns the import-info summary stored on the calculation.
        private static CalculationImportInfoDTO ApplyImportMatching(
            AtacostPackageDTO package,
            AtacostCalculationPayload payload,
            string targetProjectName,
            int userId,
            ImportLookups lookups)
        {
            var calc = payload.Calculation;
            var tasks = FlattenTasks(payload.Tasks).ToList();
            var resources = tasks.SelectMany(x => x.Resources).ToList();

            var mappings = new List<CalculationImportMappingDTO>();
            var issues = new List<CalculationImportIssueDTO>();
            var autoMapped = 0;

            // ── Calculation-level dropdowns ──
            autoMapped += MapCalcField(mappings, "Kalkylstatus", calc.SourceStatusName, calc.StatusId,
                lookups.Statuses, id => calc.StatusId = id, "Ej mappat");
            autoMapped += MapOrganisation(mappings, calc.SourceOrganisationName, calc.SourceOrganisationNumber,
                calc.OrganisationId, lookups.Organisations, id => calc.OrganisationId = id);
            autoMapped += MapCalcField(mappings, "Kalkyltyp", calc.SourceTypeName, calc.TypeId,
                lookups.Types, id => calc.TypeId = id, "Ej mappat");
            autoMapped += MapCalcField(mappings, "Ersättningsform", calc.SourceCompensationName, calc.CompensationId,
                lookups.Compensations, id => calc.CompensationId = id, "Ej mappat");
            autoMapped += MapCalcField(mappings, "Kontraktsform", calc.SourceContractName, calc.ContractId,
                lookups.Contracts, id => calc.ContractId = id, "Ej mappat");

            // ── Per-resource references ──
            var accountBucket = new List<(ResourcePostDTO Res, string Display)>();
            var typeBucket = new List<(ResourcePostDTO Res, string Display)>();
            var sortBucket = new List<(ResourcePostDTO Res, string Display)>();
            var offerRows = new List<string>();
            var offerCount = 0;

            foreach (var r in resources)
            {
                var lines = new List<string>();

                var acc = MatchResourceAccount(r, lookups.Accounts);
                if (acc.Auto) autoMapped++;
                if (acc.Line is not null) lines.Add(acc.Line);
                if (acc.Display is not null) accountBucket.Add((r, acc.Display));

                var type = MatchResourceRef(r.ResourceTypeId, r.SourceResourceTypeName, lookups.ResourceTypes,
                    id => r.ResourceTypeId = id, "Original resurstyp", "Resurstyp");
                if (type.Auto) autoMapped++;
                if (type.Line is not null) lines.Add(type.Line);
                if (type.Display is not null) typeBucket.Add((r, type.Display));

                var sort = MatchResourceRef(r.ResourceSortId, r.SourceResourceSortName, lookups.ResourceSorts,
                    id => r.ResourceSortId = id, "Original resurssortering", "Resurssortering");
                if (sort.Auto) autoMapped++;
                if (sort.Line is not null) lines.Add(sort.Line);
                if (sort.Display is not null) sortBucket.Add((r, sort.Display));

                if (r.OfferId.HasValue)
                {
                    lines.Add($"Original offert: Offert #{r.OfferId.Value} · Lokal koppling: Ej kopplad");
                    offerCount++;
                    if (!string.IsNullOrWhiteSpace(r.Name)) offerRows.Add(r.Name);
                    r.OfferId = null;
                }

                r.Data.ImportInfo = string.Join("\n", lines);
            }

            AddGroupedIssues(issues, accountBucket, "Konto saknade lokal matchning", "Importerades utan konto");
            AddGroupedIssues(issues, typeBucket, "Resurstyp saknade lokal matchning", "Importerades utan lokal resurstyp");
            AddGroupedIssues(issues, sortBucket, "Resurssortering saknade lokal matchning", "Importerades utan lokal resurssortering");
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

            return new CalculationImportInfoDTO
            {
                IsImportedCopy = true,
                ImportedFrom = string.IsNullOrWhiteSpace(package.SenderCompany) ? "Extern ATACOST-app" : package.SenderCompany,
                SourceFileName = $"{(string.IsNullOrWhiteSpace(package.DisplayName) ? "kalkylkopia" : package.DisplayName)}.atacost",
                ImportedBy = $"Användare #{userId}",
                ImportedAtUtc = DateTime.UtcNow,
                TargetProject = targetProjectName,
                ImportedRows = tasks.Count + resources.Count,
                ImportedRowsWithIssues = resources.Count(x => !string.IsNullOrWhiteSpace(x.Data.ImportInfo)),
                NotImportedRows = 0,
                AutomaticallyMappedValues = autoMapped,
                ManuallyMappedValues = 0,
                MainMappings = mappings,
                Issues = issues
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
            AtacostPackageDTO package,
            PostProjectDTO project,
            int userId,
            string targetSummary,
            ImportLookups lookups,
            List<CalculationImportInfoDTO> calcInfos)
        {
            var mappings = new List<CalculationImportMappingDTO>();
            var autoMapped = 0;

            autoMapped += MapCalcField(mappings, "Projekttyp", project.SourceTypeName, project.TypeId,
                lookups.Types, id => project.TypeId = id, "Ej mappat");
            autoMapped += MapCalcField(mappings, "Projektstatus", project.SourceStatusName, project.StatusId,
                lookups.ProjectStatuses, id => project.StatusId = id, "Ej mappat");
            autoMapped += MapCalcField(mappings, "Upphandlingsform", project.SourceProcurementMethodName, project.ProcurementMethodsId,
                lookups.ProcurementMethods, id => project.ProcurementMethodsId = id, "Ej mappat");
            autoMapped += MapCalcField(mappings, "Upphandlingsförfarande", project.SourceProcurementProcedureName, project.ProcurementProcedureId,
                lookups.ProcurementProcedures, id => project.ProcurementProcedureId = id, "Ej mappat");
            autoMapped += MapCalcField(mappings, "Ersättningsform", project.SourceCompensationName, project.CompensationId,
                lookups.Compensations, id => project.CompensationId = id, "Ej mappat");
            autoMapped += MapCalcField(mappings, "Entreprenadform", project.SourceContractName, project.ContractId,
                lookups.Contracts, id => project.ContractId = id, "Ej mappat");
            autoMapped += MapOrganisation(mappings, project.SourceOrganisationName, project.SourceOrganisationNumber,
                project.OrganisationId, lookups.Organisations, id => project.OrganisationId = id);

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
                AutomaticallyMappedValues = autoMapped + calcInfos.Sum(x => x.AutomaticallyMappedValues),
                ManuallyMappedValues = 0,
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

        // Matches one calc-level dropdown by name. Sets the local id on match, clears it otherwise.
        // Returns 1 when the value was auto-mapped, 0 otherwise. Only records a mapping row when the
        // source actually had a value (a name to display or an original id).
        private static int MapCalcField(
            List<CalculationImportMappingDTO> mappings,
            string field,
            string? sourceName,
            int? currentId,
            IReadOnlyList<LookupRow> candidates,
            Action<int?> setId,
            string notMappedLabel)
        {
            if (string.IsNullOrWhiteSpace(sourceName) && !currentId.HasValue)
            {
                setId(null);
                return 0;
            }

            var original = !string.IsNullOrWhiteSpace(sourceName) ? sourceName! : $"ID {currentId}";
            var result = AtacostMatcher.MatchByName(sourceName, candidates);
            if (result.IsMatched)
            {
                var localName = candidates.First(c => c.Id == result.Id!.Value).Name;
                setId(result.Id);
                mappings.Add(new CalculationImportMappingDTO { Field = field, OriginalValue = original, MappedValue = localName });
                return 1;
            }

            setId(null);
            mappings.Add(new CalculationImportMappingDTO { Field = field, OriginalValue = original, MappedValue = notMappedLabel });
            return 0;
        }

        // Matches a company/organisation: by org-number first, then name (see AtacostMatcher).
        // Per spec a local company link is never auto-created when nothing matches.
        private static int MapOrganisation(
            List<CalculationImportMappingDTO> mappings,
            string? sourceName,
            string? sourceNumber,
            int? currentId,
            IReadOnlyList<OrgRow> candidates,
            Action<int?> setId)
        {
            if (string.IsNullOrWhiteSpace(sourceName) && !currentId.HasValue)
            {
                setId(null);
                return 0;
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
                return 1;
            }

            setId(null);
            mappings.Add(new CalculationImportMappingDTO { Field = "Företag/organisation", OriginalValue = original, MappedValue = "Ej kopplat" });
            return 0;
        }

        // Matches a resource account by code+name. Returns the auto-mapped flag, an optional per-row
        // Importinfo note, and an optional grouping display for the unmatched-account deviation list.
        private static (bool Auto, string? Line, string? Display) MatchResourceAccount(
            ResourcePostDTO r, IReadOnlyList<AccountRow> candidates)
        {
            var hasValue = r.AccountId.HasValue
                || !string.IsNullOrWhiteSpace(r.SourceAccountCode)
                || !string.IsNullOrWhiteSpace(r.SourceAccountName);
            if (!hasValue)
            {
                r.AccountId = null;
                return (false, null, null);
            }

            var display = AccountDisplay(r);
            var result = AtacostMatcher.MatchAccount(r.SourceAccountCode, r.SourceAccountName, candidates);
            if (result.IsMatched)
            {
                r.AccountId = result.Id;
                return (true, null, null);
            }

            r.AccountId = null;
            return (false, $"Originalkonto: {display} · Lokal koppling: Ej mappad", display);
        }

        private static string AccountDisplay(ResourcePostDTO r)
        {
            var parts = new[] { (r.SourceAccountCode ?? string.Empty).Trim(), (r.SourceAccountName ?? string.Empty).Trim() }
                .Where(s => s.Length > 0);
            var combined = string.Join(" ", parts);
            return combined.Length > 0 ? combined : $"Konto #{r.AccountId}";
        }

        // Matches a resource resource-type / resource-sort by name.
        private static (bool Auto, string? Line, string? Display) MatchResourceRef(
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
                return (false, null, null);
            }

            var display = !string.IsNullOrWhiteSpace(sourceName) ? sourceName! : $"{genericName} #{currentId}";
            var result = AtacostMatcher.MatchByName(sourceName, candidates);
            if (result.IsMatched)
            {
                setId(result.Id);
                return (true, null, null);
            }

            setId(null);
            return (false, $"{originalLabel}: {display} · Lokal koppling: Ej mappad", display);
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

        private static string EnsureUniqueCode(string? code, IEnumerable<string> existingCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            return EnsureUniqueName(code, existingCodes);
        }
    }
}
