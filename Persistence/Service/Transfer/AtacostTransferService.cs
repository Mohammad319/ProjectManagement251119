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

            var package = new AtacostPackageDTO
            {
                Kind = AtacostPackageDTO.KindProject,
                ExportedAtUtc = DateTime.UtcNow,
                DisplayName = project.Name ?? string.Empty,
                Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
                Project = new AtacostProjectPayload
                {
                    Project = projectDto,
                    Calculations = [.. calcs.OrderBy(c => c.SortOrder).Select(BuildCalculationPayload)]
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

            var package = new AtacostPackageDTO
            {
                Kind = AtacostPackageDTO.KindCalculation,
                ExportedAtUtc = DateTime.UtcNow,
                DisplayName = calc.Name ?? string.Empty,
                Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
                Calculation = BuildCalculationPayload(calc)
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
            SanitizeProjectDto(projectDto, targetFolderId);
            projectDto.Name = EnsureUniqueName(projectDto.Name, existingProjectNames);
            projectDto.Code = EnsureUniqueCode(projectDto.Code, existingProjectCodes);

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
                    existingCalcNames, existingCalcCodes, order += 100, ct);
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

            return await CreateCalculationFromPayloadAsync(
                db, targetProjectId, targetProjectDepartmentId.Value, userId, package.Calculation,
                existingCalcNames, existingCalcCodes, maxOrder + 100, ct);
        }

        // ─────────────────────────── Building blocks ──────────────────────────

        private static AtacostCalculationPayload BuildCalculationPayload(CalculationEntity calc)
            => new()
            {
                Calculation = calc.ToPostDto(),
                Tasks = BuildTaskTree(calc.Tasks)
            };

        private static List<TaskPostDTO> BuildTaskTree(IEnumerable<TaskEntity> allTasks)
        {
            var byParent = allTasks
                .GroupBy(t => t.ParentTaskId ?? 0)
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.SortOrder).ToList());

            List<TaskPostDTO> BuildLevel(int parentKey)
            {
                if (!byParent.TryGetValue(parentKey, out var children))
                    return [];

                return [.. children.Select(t => ToTaskPostDto(t, BuildLevel(t.Id)))];
            }

            return BuildLevel(0);
        }

        private static TaskPostDTO ToTaskPostDto(TaskEntity t, List<TaskPostDTO> children)
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
                Resources = [.. t.Resources.OrderBy(r => r.SortOrder).Select(ToResourcePostDto)],
                Tasks = children
            };

        private static ResourcePostDTO ToResourcePostDto(ResourceEntity r)
            => new()
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
                OfferId = r.PrimaryOfferId
            };

        private static async Task<int> CreateCalculationFromPayloadAsync(
            Persistence.Context.ShardingSingleDbContext db,
            Guid projectId,
            int departmentId,
            int userId,
            AtacostCalculationPayload payload,
            List<string> existingNames,
            List<string> existingCodes,
            int order,
            CancellationToken ct)
        {
            var calcDto = payload.Calculation;
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

        // ──────────────────────── Sanitize (standalone copy) ──────────────────

        private static void SanitizeProjectDto(ProjectManagement.Shared.DTO.Project.PostProjectDTO dto, Guid targetFolderId)
        {
            dto.FolderId = targetFolderId;
            dto.IsArchived = false;
            // Clear tenant-specific references - the copy may be imported into another tenant.
            dto.OrganisationId = null;
            dto.ProcurementMethodsId = null;
            dto.ProcurementProcedureId = null;
            dto.CompensationId = null;
            dto.ContractId = null;
            dto.TypeId = null;
            dto.StatusId = null;
        }

        private static void SanitizeCalculationDto(CalculationPostDTO dto)
        {
            dto.IsArchived = false;
            dto.IsPrivate = false;
            dto.IsLocked = false;
            // Clear tenant-specific references (status/account/organisation/template belong to the source tenant).
            dto.OrganisationId = null;
            dto.CompensationId = null;
            dto.ContractId = null;
            dto.ProcurementMethodsId = null;
            dto.TypeId = null;
            dto.StatusId = null;
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
            dto.Id = 0;
            dto.AccountId = null;
            dto.StatusId = null;
            dto.ResourceSortId = null;
            dto.ResourceTypeId = null;
            dto.OpportunityId = null;
            dto.OfferId = null;
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
