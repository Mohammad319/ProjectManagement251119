using Application.Feature.ChangeLog;
using Application.Feature.Project.Project;
using Application.Mapping.Calculation;
using Application.Helper;
using Application.Mapping.Project;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Exceptions;

namespace Persistence.Service.Project
{
    public sealed class ProjectService(IDbContextFactoryTenant dbFactory, IChangeLogService changeLog) : IProjectService
    {
        private const string CreateProjectInFolderForbiddenMessage = "Du saknar behörighet att skapa projekt i denna mapp.";

        public async Task<Guid> CreateAsync(PostProjectDTO dto, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var targetDepartmentId = await context.Folders
                .AsNoTracking()
                .Where(f => f.Id == dto.FolderId)
                .Select(f => (int?)f.DepartmentId)
                .FirstOrDefaultAsync(ct);

            if (!targetDepartmentId.HasValue)
                return Guid.Empty;

            if (departmentId.HasValue && targetDepartmentId.Value != departmentId.Value)
                throw new ForbiddenActionException(CreateProjectInFolderForbiddenMessage);

            if (!await ValidateProjectReferencesAsync(context, dto, departmentId, currentProjectId: null, ct: ct))
                return Guid.Empty;

            var normalizedCode = NormalizeCode(dto.Code);
            if (!string.IsNullOrWhiteSpace(normalizedCode) &&
                await context.Projects.AsNoTracking().AnyAsync(x => x.Code == normalizedCode, ct))
            {
                return Guid.Empty;
            }

            var maxOrder = await context.Projects
                .AsNoTracking()
                .Where(x => departmentId == null || x.Folder.DepartmentId == departmentId || x.CreatedBy == userId)
                .OrderByDescending(x => x.SortOrder)
                .Select(x => (int?)x.SortOrder)
                .FirstOrDefaultAsync(ct) ?? 0;

            var project = ProjectEntity.Create(dto, dto.FolderId, userId, maxOrder + 100);
            context.Projects.Add(project);
            await context.SaveChangesAsync(ct);
            await changeLog.AppendProjectAsync(project.Id, ChangeAction.Created, userId, ct);
            return project.Id;
        }

        public async Task<bool> UpdateAsync(Guid id, PostProjectDTO dto, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Use the shared effective-edit rule so a user the project is shared with as
            // "Användare" can save it (previously only own-department/admin passed, which is
            // why such users hit the generic "Ett oväntat fel uppstod" on save).
            var project = await context.Projects
                .Include(x => x.Folder)
                .Where(Access.ProjectAccessRules.CanEdit(userId, departmentId))
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (project == null)
                return false;

            // Archiving and re-homing the project (folder/department) are administrative lifecycle
            // actions, not content edits. A user who only reaches the project via an extra share
            // (CanEdit can be true for "Kan ändra") must not do these — only Admin/own-department/
            // creator may. Other field edits stay allowed for shared editors.
            bool canManageLifecycle = departmentId == null
                || (project.Folder != null && project.Folder.DepartmentId == departmentId)
                || project.CreatedBy == userId;

            if (!canManageLifecycle)
            {
                if (dto.IsArchived != project.IsArchived ||
                    (dto.FolderId != Guid.Empty && dto.FolderId != project.FolderId))
                {
                    throw new ForbiddenActionException(Access.ProjectAccessRules.LifecycleForbiddenMessage);
                }
            }

            if (!await ValidateProjectReferencesAsync(context, dto, departmentId, id, ct, project.FolderId))
                return false;

            var normalizedCode = NormalizeCode(dto.Code);
            if (!string.IsNullOrWhiteSpace(normalizedCode) &&
                await context.Projects.AsNoTracking().AnyAsync(x => x.Id != id && x.Code == normalizedCode, ct))
            {
                return false;
            }

            if (dto.FolderId == Guid.Empty)
                return false;

            // Capture before applying so the change log can record the most specific action.
            var wasArchived = project.IsArchived;
            var oldStatusId = project.ProjectStatusId;

            if (project.FolderId != dto.FolderId)
                project.MoveToFolder(dto.FolderId);

            project.Update(dto);
            project.UpdatedBy = userId;
            project.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            var action = project.IsArchived != wasArchived
                ? (project.IsArchived ? ChangeAction.Archived : ChangeAction.Restored)
                : project.ProjectStatusId != oldStatusId
                    ? ChangeAction.StatusChanged
                    : ChangeAction.Updated;
            await changeLog.AppendProjectAsync(id, action, userId, ct);
            return true;
        }

        public async Task<bool> MoveAsync(Guid id, Guid targetFolderId, int userId, int? departmentId, bool allowCrossDepartment, CancellationToken ct)
        {
            if (targetFolderId == Guid.Empty)
                return false;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var project = await context.Projects
                .Include(x => x.Folder)
                .Include(x => x.Calculations)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (project is null)
                return false;

            if (!await context.Projects
                    .Where(Access.ProjectAccessRules.CanManageLifecycle(userId, departmentId))
                    .AnyAsync(x => x.Id == id, ct))
            {
                throw new ForbiddenActionException(Access.ProjectAccessRules.LifecycleForbiddenMessage);
            }

            var targetDepartmentId = await context.Folders
                .AsNoTracking()
                .Where(x => x.Id == targetFolderId)
                .Select(x => (int?)x.DepartmentId)
                .FirstOrDefaultAsync(ct);

            if (!targetDepartmentId.HasValue)
                return false;

            if (departmentId.HasValue && targetDepartmentId.Value != departmentId.Value && !allowCrossDepartment)
                throw new ForbiddenActionException(CreateProjectInFolderForbiddenMessage);

            project.MoveToFolder(targetFolderId);
            project.UpdatedBy = userId;
            project.UpdatedAt = DateTime.UtcNow;

            foreach (var calculation in project.Calculations)
                calculation.AssignDepartment(targetDepartmentId.Value);

            await context.SaveChangesAsync(ct);
            await changeLog.AppendProjectAsync(id, ChangeAction.Moved, userId, ct);
            return true;
        }

        public async Task<Guid> CopyAsync(Guid id, Guid targetFolderId, bool includeCalculations, int userId, int? departmentId, bool allowCrossDepartment, CancellationToken ct)
        {
            if (targetFolderId == Guid.Empty)
                return Guid.Empty;

            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var original = await context.Projects
                .AsNoTracking()
                .Include(x => x.Folder)
                .Include(x => x.Calculations)
                    .ThenInclude(x => x.Tasks)
                        .ThenInclude(x => x.Resources)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (original is null)
                return Guid.Empty;

            if (!await context.Projects
                    .Where(Access.ProjectAccessRules.CanManageLifecycle(userId, departmentId))
                    .AnyAsync(x => x.Id == id, ct))
            {
                throw new ForbiddenActionException(Access.ProjectAccessRules.LifecycleForbiddenMessage);
            }

            var targetDepartmentId = await context.Folders
                .AsNoTracking()
                .Where(x => x.Id == targetFolderId)
                .Select(x => (int?)x.DepartmentId)
                .FirstOrDefaultAsync(ct);

            if (!targetDepartmentId.HasValue)
                return Guid.Empty;

            if (departmentId.HasValue && targetDepartmentId.Value != departmentId.Value && !allowCrossDepartment)
                throw new ForbiddenActionException(CreateProjectInFolderForbiddenMessage);

            var existingProjectNames = await context.Projects
                .AsNoTracking()
                .Where(x => x.FolderId == targetFolderId)
                .Select(x => x.Name)
                .ToListAsync(ct);

            var existingProjectCodes = await context.Projects
                .AsNoTracking()
                .Select(x => x.Code ?? string.Empty)
                .ToListAsync(ct);

            var copyDto = original.ToPostDto();
            copyDto.FolderId = targetFolderId;
            copyDto.Name = EnsureUniqueName(copyDto.Name, existingProjectNames);
            copyDto.Code = EnsureUniqueCode(copyDto.Code, existingProjectCodes);

            var maxOrder = await context.Projects
                .AsNoTracking()
                .Where(x => x.FolderId == targetFolderId)
                .OrderByDescending(x => x.SortOrder)
                .Select(x => (int?)x.SortOrder)
                .FirstOrDefaultAsync(ct) ?? 0;

            var copy = ProjectEntity.Create(copyDto, targetFolderId, userId, maxOrder + 100);
            copy.Id = Guid.NewGuid();
            context.Projects.Add(copy);

            if (includeCalculations)
            {
                var existingCalculationNames = new List<string>();
                var existingCalculationCodes = new List<string>();

                var calcOrder = 0;
                foreach (var calculation in original.Calculations.OrderBy(x => x.SortOrder))
                {
                    var calculationCopy = CalculationEntity.CreateCopy(calculation, copy.Id, userId);
                    calculationCopy.AssignDepartment(targetDepartmentId.Value);

                    var calculationDto = calculation.ToPostDto();
                    calculationDto.Name = EnsureUniqueName(calculationDto.Name, existingCalculationNames);
                    calculationDto.Code = EnsureUniqueCode(calculationDto.Code, existingCalculationCodes);
                    calculationDto.Order = calcOrder += 100;
                    calculationCopy.Update(calculationDto);

                    existingCalculationNames.Add(calculationDto.Name);
                    existingCalculationCodes.Add(calculationDto.Code);
                    copy.Calculations.Add(calculationCopy);
                }
            }

            await context.SaveChangesAsync(ct);
            await changeLog.AppendProjectAsync(copy.Id, ChangeAction.Copied, userId, ct);
            return copy.Id;
        }

        public async Task<bool> DeleteAsync(Guid id, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var project = await context.Projects
                .Include(x => x.Folder)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (project == null)
                return false;

            if (!await context.Projects
                    .Where(Access.ProjectAccessRules.CanManageLifecycle(userId, departmentId))
                    .AnyAsync(x => x.Id == id, ct))
            {
                throw new ForbiddenActionException(Access.ProjectAccessRules.LifecycleForbiddenMessage);
            }

            project.MarkDeleted(userId);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateOrderAsync(Guid id, int newOrder, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var rows = await context.Projects
                .Where(x => x.Id == id &&
                    (departmentId == null || x.Folder.DepartmentId == departmentId))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SortOrder, newOrder), ct);
            return rows > 0;
        }


        public async Task<GetProjectCalcConfigDTO> GetProjectCalcConfigAsync(
            int typeObj,
            int methods,
            int contracts,
            int compensations,
            int types,
            int statuses,
            int projectStatuses,
            int orgId,
            int procedures = 0,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var result = new GetProjectCalcConfigDTO
            {
                Methods = await context.ProcurementMethod
                    .AsNoTracking()
                    .Where(x => x.IsVisible || x.Id == methods)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new ListOrderDTO { Id = x.Id, Name = x.Name, IsDefault = x.IsDefault })
                    .ToListAsync(ct),
                Contracts = await context.Contracts
                    .AsNoTracking()
                    .Where(x => x.IsVisible || x.Id == contracts)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new ListOrderDTO { Id = x.Id, Name = x.Name, IsDefault = x.IsDefault })
                    .ToListAsync(ct),
                Compensations = await context.Compensations
                    .AsNoTracking()
                    .Where(x => x.IsVisible || x.Id == compensations)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new ListOrderDTO { Id = x.Id, Name = x.Name, IsDefault = x.IsDefault })
                    .ToListAsync(ct),
                Types = await context.CalcProjectType
                    .AsNoTracking()
                    .Where(x => x.IsVisible || x.Id == types)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new ListOrderDTO { Id = x.Id, Name = x.Name, IsDefault = x.IsDefault })
                    .ToListAsync(ct),
                Procedures = await context.ProcurementProcedure
                    .AsNoTracking()
                    .Where(x => x.IsVisible || x.Id == procedures)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new ListOrderDTO { Id = x.Id, Name = x.Name, IsDefault = x.IsDefault })
                    .ToListAsync(ct),
                ProjectStatuses = await context.ProjectStatus
                    .AsNoTracking()
                    .Where(x => x.IsVisible || x.Id == projectStatuses)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new StatusListDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Color = x.Color,
                        CountsAsSubmittedBid = x.CountsAsSubmittedBid,
                        CountsAsWonBid = x.CountsAsWonBid,
                        CountsAsLostBid = x.CountsAsLostBid,
                        IsDefault = x.IsDefault
                    })
                    .ToListAsync(ct),
                Statuses = await context.CalculationStatus
                    .AsNoTracking()
                    .Where(x => x.IsVisible || x.Id == statuses)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new StatusListDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Color = x.Color,
                        IsApprovalStatus = x.IsApprovalStatus,
                        LocksCalculation = x.LocksCalculation,
                        AllowsProductionCalculation = x.AllowsProductionCalculation,
                        CountsAsSubmittedBid = x.CountsAsSubmittedBid,
                        CountsAsWonBid = x.CountsAsWonBid,
                        CountsAsLostBid = x.CountsAsLostBid,
                        IsDefault = x.IsDefault
                    })
                    .ToListAsync(ct),
                Organisation = await context.Organisation
                    .AsNoTracking()
                    .Where(x => x.IsVisible)
                    .OrderBy(x => x.Name)
                    .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                    .ToListAsync(ct)
            };

            return result;
        }

        // -------- Queries --------

        public async Task<ProjectDetailsDTO?> GetDetailsAsync(Guid id, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var project = await context.Projects
                .AsNoTracking()
                .Include(x => x.Folder)
                .Include(x => x.Organisation)
                .Include(x => x.ProcurementMethod)
                .Include(x => x.Compensation)
                .Include(x => x.Contract)
                .Include(x => x.ProjectType)
                .Include(x => x.ProjectStatus)
                .Where(x => x.Id == id &&
                    (departmentId == null || x.Folder.DepartmentId == departmentId || x.CreatedBy == userId))
                .FirstOrDefaultAsync(ct);

            return project?.ToDetailsDto();
        }

        public async Task<PostProjectDTO?> GetPostAsync(Guid id, int userId, int? departmentId, CancellationToken ct, bool isViewer = false)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var project = await context.Projects
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Where(Access.ProjectAccessRules.CanSee(userId, departmentId, isViewer))
                .FirstOrDefaultAsync(ct);

            var dto = project?.ToPostDto();
            if (dto is not null)
            {
                // Effective edit permission: a Visare (system role or share-level) gets read-only.
                dto.CanEdit = !isViewer && await context.Projects
                    .Where(Access.ProjectAccessRules.CanEdit(userId, departmentId))
                    .AnyAsync(p => p.Id == id, ct);
            }
            return dto;
        }

        public async Task<IEnumerable<ListProjectDTO>> GetByFolderAsync(Guid folderId, bool includeArchived, int userId, int? departmentId, CancellationToken ct, bool isViewer = false)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var projects = await context.Projects.AsNoTracking()
                .Include(x => x.Folder)
                .Include(x => x.Organisation)
                .Include(x => x.Contract)
                .Include(x => x.Compensation)
                .Include(x => x.ProcurementMethod)
                .Include(x => x.ProcurementProcedure)
                .Include(x => x.ProjectType)
                .Include(x => x.ProjectStatus)
                .Include(x => x.UpdatedByUser)
                .Where(x => x.FolderId == folderId && (includeArchived || !x.IsArchived))
                .Where(Access.ProjectAccessRules.CanSee(userId, departmentId, isViewer))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(ct);

            var statusSortOrders = await GetProjectStatusSortOrdersAsync(context, ct);
            var calculationCounts = await GetCalculationCountsAsync(context, projects.Select(x => x.Id), userId, departmentId, isViewer, ct);
            var accessSummaries = await GetAccessSummariesAsync(context, projects, userId, departmentId, ct);
            return projects.Select(x =>
            {
                var metadata = x.GetMetadataSnapshot();
                statusSortOrders.TryGetValue(x.ProjectStatusId ?? metadata.StatusId ?? 0, out var statusSortOrder);
                calculationCounts.TryGetValue(x.Id, out var calculationCount);
                accessSummaries.TryGetValue(x.Id, out var access);
                return x.ToListDto(statusSortOrder, calculationCount, access: access);
            }).ToList();
        }

        public async Task<IEnumerable<ListProjectDTO>> GetOtherGroupByFolderAsync(Guid folderId, int userId, int? departmentId, bool includeArchived, CancellationToken ct, bool isViewer = false)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            IQueryable<ProjectEntity> baseQuery = context.Projects.AsNoTracking()
                .Include(x => x.Folder)
                .Include(x => x.Organisation)
                .Include(x => x.Contract)
                .Include(x => x.Compensation)
                .Include(x => x.ProcurementMethod)
                .Include(x => x.ProcurementProcedure)
                .Include(x => x.ProjectType)
                .Include(x => x.ProjectStatus)
                .Include(x => x.UpdatedByUser)
                .Where(x => x.FolderId == folderId && (includeArchived || !x.IsArchived));

            // Visare: bara projekt som delats med dem via intern projektdelning.
            // Övriga: befintlig "annan avdelning"-vy via kalkyldelning (ShareCalc).
            baseQuery = isViewer
                ? baseQuery.Where(Access.ProjectAccessRules.CanSee(userId, departmentId, isViewerOnly: true))
                : baseQuery.Where(x => x.Calculations.SelectMany(c => c.SharesCalc)
                        .Any(s => s.CreatedBy == userId || s.DepartmentId == departmentId));

            var projects = await baseQuery
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(ct);

            var statusSortOrders = await GetProjectStatusSortOrdersAsync(context, ct);
            var calculationCounts = await GetCalculationCountsAsync(context, projects.Select(x => x.Id), userId, departmentId, isViewer, ct);
            var accessSummaries = await GetAccessSummariesAsync(context, projects, userId, departmentId, ct);
            return projects.Select(x =>
            {
                var metadata = x.GetMetadataSnapshot();
                statusSortOrders.TryGetValue(x.ProjectStatusId ?? metadata.StatusId ?? 0, out var statusSortOrder);
                calculationCounts.TryGetValue(x.Id, out var calculationCount);
                accessSummaries.TryGetValue(x.Id, out var access);
                return x.ToListDto(statusSortOrder, calculationCount, access: access);
            }).ToList();
        }

        public async Task<IEnumerable<SearchProjectDTO>> SearchAsync(ProjectFilter filter, int userId, int? departmentId, CancellationToken ct, bool isViewer = false)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            filter ??= new ProjectFilter();

            IQueryable<ProjectEntity> query = context.Projects
                .AsNoTracking()
                .Where(x => filter.IsArchived == null || x.IsArchived == filter.IsArchived.Value)
                .Where(Access.ProjectAccessRules.CanSee(userId, departmentId, isViewer));

            if (filter.FolderId.HasValue && filter.FolderId.Value != Guid.Empty)
                query = query.Where(x => x.FolderId == filter.FolderId.Value);

            if (filter.CustomerId.HasValue)
                query = query.Where(x => x.OrganisationId == filter.CustomerId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                string name = filter.Name.Trim();
                string op = (filter.NameOperator ?? string.Empty).Trim().ToLowerInvariant();

                query = op switch
                {
                    "=" or "eq" or "equals" => query.Where(x => x.Name == name),
                    "start" or "startswith" or "beginswith" => query.Where(x => x.Name.StartsWith(name)),
                    "end" or "endswith" => query.Where(x => x.Name.EndsWith(name)),
                    _ => query.Where(x => x.Name.Contains(name))
                };
            }

            if (!string.IsNullOrWhiteSpace(filter.Code))
            {
                string code = filter.Code.Trim();
                query = query.Where(x => x.Code != null && x.Code.Contains(code));
            }

            if (filter.StartDate1.HasValue)
                query = query.Where(x => x.StartDate >= filter.StartDate1.Value);

            if (filter.StartDate2.HasValue)
                query = query.Where(x => x.StartDate <= filter.StartDate2.Value);

            if (filter.EndDate1.HasValue)
                query = query.Where(x => x.EndDate >= filter.EndDate1.Value);

            if (filter.EndDate2.HasValue)
                query = query.Where(x => x.EndDate <= filter.EndDate2.Value);

            query = filter.SortValue switch
            {
                ProjectFilter.Sort.Code => query.OrderBy(x => x.Code).ThenBy(x => x.Name),
                ProjectFilter.Sort.StPro => query.OrderBy(x => x.StartDate).ThenBy(x => x.Name),
                ProjectFilter.Sort.StProDes => query.OrderByDescending(x => x.StartDate).ThenBy(x => x.Name),
                ProjectFilter.Sort.EnPro => query.OrderBy(x => x.EndDate).ThenBy(x => x.Name),
                ProjectFilter.Sort.EnProDes => query.OrderByDescending(x => x.EndDate).ThenBy(x => x.Name),
                _ => query.OrderByDescending(x => x.SortOrder).ThenBy(x => x.Name)
            };

            if (filter.Skip > 0)
                query = query.Skip(filter.Skip);

            return await query
                .Select(ProjectSelectors.Search)
                .ToListAsync(ct);
        }

        private static string? NormalizeCode(string? code)
            => string.IsNullOrWhiteSpace(code) ? null : code.Trim();

        private static Task<Dictionary<int, int>> GetProjectStatusSortOrdersAsync(
            Persistence.Context.ShardingSingleDbContext context,
            CancellationToken ct)
        {
            return context.ProjectStatus
                .AsNoTracking()
                .Select(status => new { status.Id, status.SortOrder })
                .ToDictionaryAsync(status => status.Id, status => status.SortOrder, ct);
        }

        private static async Task<Dictionary<Guid, int>> GetCalculationCountsAsync(
            ShardingSingleDbContext context,
            IEnumerable<Guid> projectIds,
            int userId,
            int? departmentId,
            bool isViewer,
            CancellationToken ct)
        {
            var ids = projectIds.Distinct().ToList();
            if (ids.Count == 0)
                return [];

            var calculations = await context.Calculations
                .AsNoTracking()
                .Where(calculation => !calculation.IsDeleted && ids.Contains(calculation.ProjectId))
                .Where(Access.CalculationAccessRules.CanSee(userId, departmentId, isViewer))
                .Select(calculation => new
                {
                    calculation.Id,
                    calculation.ProjectId,
                    calculation.VersionGroupId,
                    calculation.VersionNumber,
                    calculation.IsCurrentVersion,
                    calculation.IsArchived,
                    calculation.CreatedAt,
                    calculation.UpdatedAt
                })
                .ToListAsync(ct);

            return calculations
                .GroupBy(calculation => calculation.ProjectId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .GroupBy(calculation => calculation.VersionGroupId == Guid.Empty
                            ? $"legacy:{calculation.Id}"
                            : calculation.VersionGroupId.ToString())
                        .Select(family => family
                            .OrderByDescending(calculation => calculation.IsCurrentVersion)
                            .ThenByDescending(calculation => calculation.VersionNumber > 0
                                ? calculation.VersionNumber
                                : int.MinValue)
                            .ThenByDescending(calculation => calculation.UpdatedAt ?? calculation.CreatedAt)
                            .ThenByDescending(calculation => calculation.CreatedAt)
                            .ThenByDescending(calculation => calculation.Id)
                            .First())
                        .Where(calculation => !calculation.IsArchived)
                        .Count());
        }

        // Kompakt åtkomstsammanfattning per projekt för projektlistans "Åtkomst"-kolumn.
        // Skiljer normal avdelningsåtkomst (ViaDepartment) från extra delning (Recipients).
        private static async Task<Dictionary<Guid, ProjectAccessSummaryDTO>> GetAccessSummariesAsync(
            ShardingSingleDbContext context,
            List<ProjectEntity> projects,
            int userId,
            int? departmentId,
            CancellationToken ct)
        {
            var result = new Dictionary<Guid, ProjectAccessSummaryDTO>();
            var ids = projects.Select(p => p.Id).Distinct().ToList();
            if (ids.Count == 0)
                return result;

            // Extra delningar (användare/avdelning) med mottagarnamn, behörighet och antal kalkyler.
            var today = DateTime.UtcNow.Date;
            var shares = await context.ProjectShare
                .AsNoTracking()
                .Where(s => ids.Contains(s.ProjectId) && (s.ValidUntil == null || s.ValidUntil >= today))
                .Select(s => new
                {
                    s.ProjectId,
                    Type = s.SharedWithUserId != null
                        ? ProjectShareRecipientType.User
                        : ProjectShareRecipientType.Department,
                    s.SharedWithUserId,
                    s.DepartmentId,
                    Name = s.SharedWithUserId != null
                        ? (((s.SharedWithUser!.FirstName ?? "") + " " + (s.SharedWithUser!.LastName ?? "")).Trim() == ""
                            ? s.SharedWithUser!.UserName
                            : ((s.SharedWithUser!.FirstName ?? "") + " " + (s.SharedWithUser!.LastName ?? "")).Trim())
                        : s.Department!.Name,
                    s.Role,
                    s.AllCalculations,
                    CalcCount = s.Calculations.Count
                })
                .ToListAsync(ct);

            var sharesByProject = shares.ToLookup(s => s.ProjectId);
            var shareableCounts = await GetShareableCalcCountsAsync(context, ids, ct);

            foreach (var p in projects)
            {
                var shareable = shareableCounts.TryGetValue(p.Id, out var sc) ? sc : 0;
                result[p.Id] = new ProjectAccessSummaryDTO
                {
                    // Normal avdelningsåtkomst: admin (tenant-wide), egen avdelning eller skapare.
                    ViaDepartment = departmentId == null
                        || (p.Folder != null && p.Folder.DepartmentId == departmentId)
                        || p.CreatedBy == userId,
                    ShareableCalcCount = shareable,
                    Recipients = sharesByProject[p.Id]
                        .Select(s => new ProjectAccessRecipientDTO
                        {
                            Type = s.Type,
                            UserId = s.SharedWithUserId,
                            DepartmentId = s.DepartmentId,
                            Name = s.Name ?? string.Empty,
                            Role = s.Role ?? string.Empty,
                            // "Alla kalkyler i projektet" → räkna som alla delbara kalkyler (även nya),
                            // så listans "Åtkomst"-kolumn visar "Alla tillgängliga kalkyler".
                            CalcCount = s.AllCalculations ? shareable : s.CalcCount
                        })
                        .ToList()
                };
            }

            return result;
        }

        // Antal delbara kalkyler per projekt: aktuell version, ej arkiverad och ej privat.
        // Speglar GetCalculationCountsAsync men exkluderar privata kalkyler – det är nämnaren
        // i "3/5 kalkyler" och samma mängd som delningsdialogen erbjuder.
        private static async Task<Dictionary<Guid, int>> GetShareableCalcCountsAsync(
            ShardingSingleDbContext context,
            IEnumerable<Guid> projectIds,
            CancellationToken ct)
        {
            var ids = projectIds.Distinct().ToList();
            if (ids.Count == 0)
                return [];

            var calculations = await context.Calculations
                .AsNoTracking()
                .Where(calculation => !calculation.IsDeleted && ids.Contains(calculation.ProjectId))
                .Select(calculation => new
                {
                    calculation.Id,
                    calculation.ProjectId,
                    calculation.VersionGroupId,
                    calculation.VersionNumber,
                    calculation.IsCurrentVersion,
                    calculation.IsArchived,
                    calculation.IsPrivate,
                    calculation.CreatedAt,
                    calculation.UpdatedAt
                })
                .ToListAsync(ct);

            return calculations
                .GroupBy(calculation => calculation.ProjectId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .GroupBy(calculation => calculation.VersionGroupId == Guid.Empty
                            ? $"legacy:{calculation.Id}"
                            : calculation.VersionGroupId.ToString())
                        .Select(family => family
                            .OrderByDescending(calculation => calculation.IsCurrentVersion)
                            .ThenByDescending(calculation => calculation.VersionNumber > 0
                                ? calculation.VersionNumber
                                : int.MinValue)
                            .ThenByDescending(calculation => calculation.UpdatedAt ?? calculation.CreatedAt)
                            .ThenByDescending(calculation => calculation.CreatedAt)
                            .ThenByDescending(calculation => calculation.Id)
                            .First())
                        .Where(calculation => !calculation.IsArchived && !calculation.IsPrivate)
                        .Count());
        }

        private static string EnsureUniqueName(string name, IEnumerable<string> existingNames)
        {
            var existing = existingNames.ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            if (!existing.Contains(name))
                return name;

            var baseName = $"{name} - kopia";
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

        private static async Task<bool> ValidateProjectReferencesAsync(
            ShardingSingleDbContext context,
            PostProjectDTO dto,
            int? departmentId,
            Guid? currentProjectId,
            CancellationToken ct,
            Guid? currentFolderId = null)
        {
            if (dto.FolderId == Guid.Empty)
                return false;

            // The target folder must be in the user's own department, OR be the project's
            // current folder. The latter lets a cross-department editor (a user the project is
            // shared with as "Användare") save without being forced to move the project out of
            // its owning department's folder.
            var folderOk = await context.Folders
                .AsNoTracking()
                .AnyAsync(f => f.Id == dto.FolderId &&
                    (!departmentId.HasValue
                     || f.DepartmentId == departmentId.Value
                     || (currentFolderId.HasValue && f.Id == currentFolderId.Value)), ct);

            if (!folderOk)
                return false;

            if (dto.OrganisationId.HasValue &&
                !await context.Organisation.AsNoTracking().AnyAsync(x => x.Id == dto.OrganisationId.Value, ct))
                return false;

            if (dto.ProcurementMethodsId.HasValue &&
                !await context.ProcurementMethod.AsNoTracking().AnyAsync(x => x.Id == dto.ProcurementMethodsId.Value, ct))
                return false;

            if (dto.CompensationId.HasValue &&
                !await context.Compensations.AsNoTracking().AnyAsync(x => x.Id == dto.CompensationId.Value, ct))
                return false;

            if (dto.ContractId.HasValue &&
                !await context.Contracts.AsNoTracking().AnyAsync(x => x.Id == dto.ContractId.Value, ct))
                return false;

            if (dto.TypeId.HasValue &&
                !await context.CalcProjectType.AsNoTracking().AnyAsync(x => x.Id == dto.TypeId.Value, ct))
                return false;

            if (dto.StatusId.HasValue &&
                !await context.ProjectStatus.AsNoTracking().AnyAsync(x => x.Id == dto.StatusId.Value, ct))
                return false;

            var normalizedCode = NormalizeCode(dto.Code);
            if (!string.IsNullOrWhiteSpace(normalizedCode))
            {
                var duplicateCodeExists = await context.Projects
                    .AsNoTracking()
                    .AnyAsync(x => x.Code == normalizedCode && (!currentProjectId.HasValue || x.Id != currentProjectId.Value), ct);

                if (duplicateCodeExists)
                    return false;
            }

            return true;
        }
    }
}
