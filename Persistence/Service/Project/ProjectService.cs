using Application.Feature.Project.Project;
using Application.Helper;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.Project;

namespace Persistence.Service.Project
{
    public sealed class ProjectService(ShardingSingleDbContext db) : IProjectService
    {
        public async Task<Guid> CreateAsync(PostProjectDTO dto, int userId, int? departmentId, CancellationToken ct)
        {
            var folder = await db.Folders.FindAsync(dto.FolderId, ct);
            if (folder == null || (departmentId != null && folder.DepartmentId != departmentId))
                return Guid.Empty;

            double order =
                await db.Projects
                    .Where(x => departmentId == null || x.Folder.DepartmentId == departmentId || x.CreatedBy == userId)
                    .MaxAsync(x => (double?)x.SortOrder, ct)
                ?? 0;

            var project = ProjectEntity.Create(dto, dto.FolderId, userId, order + 100);
            db.Projects.Add(project);
            await db.SaveChangesAsync(ct);
            return project.Id;
        }

        public async Task<bool> UpdateAsync(Guid id, PostProjectDTO dto, int userId, int? departmentId, CancellationToken ct)
        {
            var project = await db.Projects
                .Include(x => x.Folder)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (project == null || (departmentId != null && project.Folder.DepartmentId != departmentId))
                return false;

            project.Update(dto);
            project.UpdatedBy = userId;

            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, int userId, int? departmentId, CancellationToken ct)
        {
            var project = await db.Projects
                .Include(x => x.Folder)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (project == null || (departmentId != null && project.Folder.DepartmentId != departmentId))
                return false;

            db.Projects.Remove(project);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateOrderAsync(Guid id, double newOrder, CancellationToken ct)
        {
            var project = await db.Projects.FindAsync(id, ct);
            if (project == null) return false;

            project.UpdateOrder(newOrder);
            await db.SaveChangesAsync(ct);
            return true;
        }

        // -------- Queries --------

        public Task<ProjectDetailsDTO?> GetDetailsAsync(Guid id, CancellationToken ct)
            => db.Projects.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new ProjectDetailsDTO())
                .FirstOrDefaultAsync(ct);

        public Task<PostProjectDTO?> GetPostAsync(Guid id, CancellationToken ct)
            => db.Projects.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(ProjectSelectors.Post)
                .FirstOrDefaultAsync(ct);

        public Task<IEnumerable<ListProjectDTO>> GetByFolderAsync(Guid folderId, bool isVisible, int userId, int? departmentId, CancellationToken ct)
            => db.Projects.AsNoTracking()
                .Where(x => x.FolderId == folderId && x.IsVisible == isVisible &&
                       (departmentId == null || x.Folder.DepartmentId == departmentId || x.CreatedBy == userId))
                .Select(ProjectSelectors.List)
                .ToListAsync(ct)
                .ContinueWith(t => (IEnumerable<ListProjectDTO>)t.Result, ct);

        public Task<IEnumerable<ListProjectDTO>> GetOtherGroupByFolderAsync(Guid folderId, int userId, int? departmentId, CancellationToken ct)
            => db.Projects.AsNoTracking()
                .Where(x => x.FolderId == folderId && x.IsVisible &&
                    x.Calculations.SelectMany(c => c.SharesCalc)
                        .Any(s => s.CreatedBy == userId || s.DepartmentId == departmentId))
                .Select(ProjectSelectors.List)
                .ToListAsync(ct)
                .ContinueWith(t => (IEnumerable<ListProjectDTO>)t.Result, ct);

        public Task<IEnumerable<SearchProjectDTO>> SearchAsync(ProjectFilter filter, int userId, int? departmentId, CancellationToken ct)
        {
            // نفس منطقك السابق لكن داخل Service
            // (اختصرته هنا، ويمكن نقله حرفيًا من Query القديم)
            throw new NotImplementedException();
        }
    }

}
