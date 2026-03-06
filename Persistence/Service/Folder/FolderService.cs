using Application.Feature.Project.Folder;
using Domain.Entities.Folder;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Folder;

namespace Persistence.Service.Folder
{
    public sealed class FolderService(IDbContextFactoryTenant dbFactory) : IFolderService
    {

        // ---------------- Commands ----------------

        public async Task<Guid> CreateAsync(PostFolderDTO dto, int userId, int departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var nextOrder = await context.Folders
                .AsNoTracking()
                .Where(x => x.DepartmentId == departmentId || x.CreatedBy == userId)
                .OrderByDescending(x => x.SortOrder)
                .Select(x => (int?)x.SortOrder)
                .FirstOrDefaultAsync(ct) ?? 0;

            var entity = new FolderEntity(
                name: dto.Name,
                color: dto.Color,
                departmentId: departmentId,
                createdBy: userId,
                sortOrder: nextOrder + 100
            );

            context.Folders.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }



        public async Task<bool> UpdateAsync(Guid id, PostFolderDTO dto, int userId, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var folder = await context.Folders.FindAsync(id, ct);
            if (folder == null || (departmentId.HasValue && folder.DepartmentId != departmentId))
                return false;

            folder.Update(dto.Name, dto.Color, dto.IsVisible);
            folder.CreatedBy = userId;

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, int userId, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var folder = await context.Folders.FindAsync(id, ct);
            if (folder == null)
                return false;

            if (departmentId.HasValue && folder.DepartmentId != departmentId)
                return false;

            bool hasProjects = await context.Projects.AnyAsync(x => x.FolderId == id, ct);
            if (hasProjects || folder.CreatedBy != userId)
                return false;

            context.Folders.Remove(folder);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateOrderAsync(Guid id, int newOrder, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var folder = await context.Folders.FindAsync(id, ct);
            if (folder == null) return false;

            folder.UpdateOrder(newOrder);
            await context.SaveChangesAsync(ct);
            return true;
        }

        // ---------------- Queries ----------------

        public async  Task<List<ListFolderDTO>> GetAllVisibleAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Folders
                .AsNoTracking()
                .Where(x => x.IsVisible)
                .Select(x => new ListFolderDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Color = x.Color,
                    Order = x.SortOrder
                })
                .ToListAsync(ct);
        }

        public async Task<List<ListFolderDTO>> GetByDepartmentAsync(bool isVisible, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Folders
                .AsNoTracking()
                .Where(x => x.IsVisible == isVisible && (!departmentId.HasValue || x.DepartmentId == departmentId))
                .OrderBy(x => x.SortOrder)
                .Select(x => new ListFolderDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Color = x.Color,
                    Order = x.SortOrder
                })
                .ToListAsync(ct);
        }
        public async Task<List<ListFolderDTO>> GetFromOtherDepartmentAsync(int departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Folders
                .AsNoTracking()
                .Where(x => x.IsVisible && x.DepartmentId == departmentId)
                .Select(x => new ListFolderDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Color = x.Color,
                    Order = x.SortOrder
                })
                .ToListAsync(ct);
        }

        public async Task<DetailsFolderDTO?> GetDetailsAsync(Guid id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Folders
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new DetailsFolderDTO
                {
                    Name = x.Name,
                    Color = x.Color,
                    IsVisible = x.IsVisible,
                    Department = x.Department.Name
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
