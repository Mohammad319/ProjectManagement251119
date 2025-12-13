using Application.Feature.Project.Folder;
using Domain.Entities.Folder;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using ProjectManagement.Shared.DTO.Folder;

namespace Persistence.Service.Folder
{
    public sealed class FolderService(ShardingSingleDbContext db) : IFolderService
    {

        // ---------------- Commands ----------------

        public async Task<Guid> CreateAsync(PostFolderDTO dto, int userId, int departmentId, CancellationToken ct = default)
        {
            double nextOrder =
                await db.Folders
                    .Where(x => x.DepartmentId == departmentId || x.CreatedBy == userId)
                    .MaxAsync(x => (double?)x.SortOrder, ct)
                ?? 0;

            var entity = new FolderEntity(
                name: dto.Name,
                color: dto.Color,
                departmentId: departmentId,
                createdBy: userId,
                sortOrder: nextOrder + 100
            );

            db.Folders.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }


        public async Task<bool> UpdateAsync(Guid id, PostFolderDTO dto, int userId, int? departmentId, CancellationToken ct = default)
        {
            var folder = await db.Folders.FindAsync(id, ct);
            if (folder == null || (departmentId.HasValue && folder.DepartmentId != departmentId))
                return false;

            folder.Update(dto.Name, dto.Color, dto.IsVisible);
            folder.CreatedBy = userId;

            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, int userId, int? departmentId, CancellationToken ct = default)
        {
            var folder = await db.Folders.FindAsync(id, ct);
            if (folder == null)
                return false;

            if (departmentId.HasValue && folder.DepartmentId != departmentId)
                return false;

            bool hasProjects = await db.Projects.AnyAsync(x => x.FolderId == id, ct);
            if (hasProjects || folder.CreatedBy != userId)
                return false;

            db.Folders.Remove(folder);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> UpdateOrderAsync(Guid id, double newOrder, CancellationToken ct = default)
        {
            var folder = await db.Folders.FindAsync(id, ct);
            if (folder == null) return false;

            folder.UpdateOrder(newOrder);
            await db.SaveChangesAsync(ct);
            return true;
        }

        // ---------------- Queries ----------------

        public Task<List<ListFolderDTO>> GetAllVisibleAsync(CancellationToken ct = default)
        {
            return db.Folders
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

        public Task<List<ListFolderDTO>> GetByDepartmentAsync(bool isVisible, int? departmentId, CancellationToken ct = default)
        {
            var query = db.Folders.AsNoTracking().Where(x => x.IsVisible == isVisible);

            if (departmentId.HasValue)
                query = query.Where(x => x.DepartmentId == departmentId);

            return query.Select(x => new ListFolderDTO
            {
                Id = x.Id,
                Name = x.Name,
                Color = x.Color,
                Order = x.SortOrder
            }).ToListAsync(ct);
        }

        public Task<List<ListFolderDTO>> GetFromOtherDepartmentAsync(int departmentId, CancellationToken ct = default)
        {
            return db.Folders
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

        public Task<DetailsFolderDTO?> GetDetailsAsync(Guid id, CancellationToken ct = default)
        {
            return db.Folders
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
