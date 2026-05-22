using Application.Feature.Project.Project;
using Application.Helper;
using Application.Mapping.Project;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Project;

namespace Persistence.Service.Project
{
    public sealed class ProjectService(IDbContextFactoryTenant dbFactory) : IProjectService
    {
        public async Task<Guid> CreateAsync(PostProjectDTO dto, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var folderOk = await context.Folders
                .AsNoTracking()
                .Where(f => f.Id == dto.FolderId)
                .AnyAsync(f => departmentId == null || f.DepartmentId == departmentId, ct);

            if (!folderOk)
                return Guid.Empty;

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
            return project.Id;
        }

        public async Task<bool> UpdateAsync(Guid id, PostProjectDTO dto, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var project = await context.Projects
                .FirstOrDefaultAsync(x => x.Id == id &&
                    (departmentId == null || x.Folder.DepartmentId == departmentId), ct);

            if (project == null)
                return false;

            if (!await ValidateProjectReferencesAsync(context, dto, departmentId, id, ct))
                return false;

            var normalizedCode = NormalizeCode(dto.Code);
            if (!string.IsNullOrWhiteSpace(normalizedCode) &&
                await context.Projects.AsNoTracking().AnyAsync(x => x.Id != id && x.Code == normalizedCode, ct))
            {
                return false;
            }

            if (dto.FolderId == Guid.Empty)
                return false;

            if (project.FolderId != dto.FolderId)
                project.MoveToFolder(dto.FolderId);

            project.Update(dto);
            project.UpdatedBy = userId;
            project.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var project = await context.Projects
                .FirstOrDefaultAsync(x => x.Id == id &&
                    (departmentId == null || x.Folder.DepartmentId == departmentId), ct);

            if (project == null)
                return false;

            project.MarkDeleted(userId);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateOrderAsync(Guid id, int newOrder, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var project = await context.Projects
                .FirstOrDefaultAsync(x => x.Id == id &&
                    (departmentId == null || x.Folder.DepartmentId == departmentId), ct);
            if (project == null) return false;

            project.UpdateOrder(newOrder);
            await context.SaveChangesAsync(ct);
            return true;
        }


        public async Task<GetProjectCalcConfigDTO> GetProjectCalcConfigAsync(
            int typeObj,
            int methods,
            int contracts,
            int compensations,
            int types,
            int statuses,
            int orgId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            static IQueryable<ListDTO> OrderAndSelect<T>(IQueryable<T> query) where T : class, IListOrderDTO
                => query
                    .Where(x => x.IsVisible)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new ListDTO { Id = x.Id, Name = x.Name });

            var result = new GetProjectCalcConfigDTO
            {
                Methods = await OrderAndSelect(context.ProcurementMethod).ToListAsync(ct),
                Contracts = await OrderAndSelect(context.Contracts).ToListAsync(ct),
                Compensations = await OrderAndSelect(context.Compensations).ToListAsync(ct),
                Types = await OrderAndSelect(context.CalcProjectType).ToListAsync(ct),
                Statuses = await OrderAndSelect(context.CalculationStatus).ToListAsync(ct),
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
                .Where(x => x.Id == id &&
                    (departmentId == null || x.Folder.DepartmentId == departmentId || x.CreatedBy == userId))
                .FirstOrDefaultAsync(ct);

            return project?.ToDetailsDto();
        }

        public async Task<PostProjectDTO?> GetPostAsync(Guid id, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var project = await context.Projects
                .AsNoTracking()
                .Where(x => x.Id == id &&
                    (departmentId == null || x.Folder.DepartmentId == departmentId || x.CreatedBy == userId))
                .FirstOrDefaultAsync(ct);

            return project?.ToPostDto();
        }

        public async Task<IEnumerable<ListProjectDTO>> GetByFolderAsync(Guid folderId, bool isVisible, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Projects.AsNoTracking()
                .Where(x => x.FolderId == folderId && x.IsVisible == isVisible &&
                       (departmentId == null || x.Folder.DepartmentId == departmentId || x.CreatedBy == userId))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(ProjectSelectors.List)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<ListProjectDTO>> GetOtherGroupByFolderAsync(Guid folderId, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Projects.AsNoTracking()
                .Where(x => x.FolderId == folderId && x.IsVisible &&
                    x.Calculations.SelectMany(c => c.SharesCalc)
                        .Any(s => s.CreatedBy == userId || s.DepartmentId == departmentId))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(ProjectSelectors.List)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<SearchProjectDTO>> SearchAsync(ProjectFilter filter, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            filter ??= new ProjectFilter();

            IQueryable<ProjectEntity> query = context.Projects
                .AsNoTracking()
                .Where(x => x.IsVisible == filter.IsVisible)
                .Where(x => departmentId == null || x.Folder.DepartmentId == departmentId || x.CreatedBy == userId);

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

        private static async Task<bool> ValidateProjectReferencesAsync(
            ShardingSingleDbContext context,
            PostProjectDTO dto,
            int? departmentId,
            Guid? currentProjectId,
            CancellationToken ct)
        {
            if (dto.FolderId == Guid.Empty)
                return false;

            var folderOk = await context.Folders
                .AsNoTracking()
                .AnyAsync(f => f.Id == dto.FolderId && (!departmentId.HasValue || f.DepartmentId == departmentId.Value), ct);

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
