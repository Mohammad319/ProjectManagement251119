using Application.Feature.Project.Project;
using Application.Helper;
using Domain.Entities.Project;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Project;

namespace Persistence.Service.Project
{
    public sealed class ProjectService(IDbContextFactoryTenant dbFactory) : IProjectService
    {
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

            var typesTask = context.CalcProjectType.AsNoTracking()
                .Where(x => x.IsVisible || x.Id == types)
                .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);

            var methodsTask = context.ProcurementMethod.AsNoTracking()
                .Where(x => x.IsVisible || x.Id == methods)
                .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);

            var contractsTask = context.Contracts.AsNoTracking()
                .Where(x => x.IsVisible || x.Id == contracts)
                .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);

            var compensationsTask = context.Compensations.AsNoTracking()
                .Where(x => x.IsVisible || x.Id == compensations)
                .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);

            var orgTask = context.Organisation.AsNoTracking()
                .Where(x => x.IsVisible || x.Id == orgId)
                .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                .ToListAsync(ct);

            Task<List<ListDTO>>? statusesTask = null;
            if (typeObj == 1)
            {
                statusesTask = context.CalculationStatus.AsNoTracking()
                    .Where(x => x.IsVisible || x.Id == statuses)
                    .Select(x => new ListDTO { Id = x.Id, Name = x.Name })
                    .ToListAsync(ct);
            }

            if (statusesTask is null)
                await Task.WhenAll(typesTask, methodsTask, contractsTask, compensationsTask, orgTask);
            else
                await Task.WhenAll(typesTask, methodsTask, contractsTask, compensationsTask, orgTask, statusesTask);

            return new GetProjectCalcConfigDTO
            {
                Types = typesTask.Result,
                Methods = methodsTask.Result,
                Contracts = contractsTask.Result,
                Compensations = compensationsTask.Result,
                Organisation = orgTask.Result,
                Statuses = statusesTask?.Result
            };
        }

        public async Task<Guid> CreateAsync(PostProjectDTO dto, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var folderOk = await context.Folders
                .AsNoTracking()
                .Where(f => f.Id == dto.FolderId)
                .AnyAsync(f => departmentId == null || f.DepartmentId == departmentId, ct);

            if (!folderOk) return Guid.Empty;

            var maxOrder = await context.Projects
                .AsNoTracking()
                .Where(x => departmentId == null || x.Folder.DepartmentId == departmentId || x.CreatedBy == userId)
                .OrderByDescending(x => x.SortOrder)
                .Select(x => (double?)x.SortOrder)
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

            project.Update(dto);
            project.UpdatedBy = userId;

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

            context.Projects.Remove(project);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateOrderAsync(Guid id, double newOrder, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var project = await context.Projects.FindAsync(id, ct);
            if (project == null) return false;

            project.UpdateOrder(newOrder);
            await context.SaveChangesAsync(ct);
            return true;
        }

        // -------- Queries --------

        public async Task<ProjectDetailsDTO?> GetDetailsAsync(Guid id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Projects.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new ProjectDetailsDTO())
                .FirstOrDefaultAsync(ct);
        }
        public async Task<PostProjectDTO?> GetPostAsync(Guid id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Projects.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(ProjectSelectors.Post)
                .FirstOrDefaultAsync(ct);
        }
        public async Task<IEnumerable<ListProjectDTO>> GetByFolderAsync(Guid folderId, bool isVisible, int userId, int? departmentId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Projects.AsNoTracking()
                .Where(x => x.FolderId == folderId && x.IsVisible == isVisible &&
                       (departmentId == null || x.Folder.DepartmentId == departmentId || x.CreatedBy == userId))
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
                .Select(ProjectSelectors.List)
                .ToListAsync(ct);
        }

        public Task<IEnumerable<SearchProjectDTO>> SearchAsync(ProjectFilter filter, int userId, int? departmentId, CancellationToken ct)
        {
            // نفس منطقك السابق لكن داخل Service
            // (اختصرته هنا، ويمكن نقله حرفيًا من Query القديم)
            throw new NotImplementedException();
        }
    }

}
