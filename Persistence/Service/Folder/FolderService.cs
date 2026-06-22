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

            var departmentExists = await context.Department
                .AsNoTracking()
                .AnyAsync(x => x.Id == departmentId, ct);

            if (!departmentExists)
                return Guid.Empty;

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
            var folder = await context.Folders
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (folder == null || (departmentId.HasValue && folder.DepartmentId != departmentId))
                return false;

            folder.Update(dto.Name, dto.Color, dto.IsVisible);
            folder.UpdatedBy = userId;

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> MoveAsync(Guid id, int targetDepartmentId, int userId, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var folder = await context.Folders
                .Include(x => x.FolderProjects)
                    .ThenInclude(x => x.Calculations)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (folder is null)
                return false;

            if (departmentId.HasValue && folder.DepartmentId != departmentId.Value)
                return false;

            var targetDepartmentExists = await context.Department
                .AsNoTracking()
                .AnyAsync(x => x.Id == targetDepartmentId, ct);

            if (!targetDepartmentExists)
                return false;

            folder.MoveToDepartment(targetDepartmentId);
            folder.UpdatedBy = userId;
            folder.UpdatedAt = DateTime.UtcNow;

            foreach (var calculation in folder.FolderProjects.SelectMany(project => project.Calculations))
                calculation.AssignDepartment(targetDepartmentId);

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, int userId, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var folder = await context.Folders
                .FirstOrDefaultAsync(x => x.Id == id, ct);
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

        public async Task<bool> UpdateOrderAsync(Guid id, int newOrder, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var rows = await context.Folders
                .Where(x => x.Id == id &&
                    (!departmentId.HasValue || x.DepartmentId == departmentId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SortOrder, newOrder), ct);
            return rows > 0;
        }

        // ---------------- Queries ----------------

        public async Task<List<ListFolderDTO>> GetAllVisibleAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Folders
                .AsNoTracking()
                .Where(x => x.IsVisible)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new ListFolderDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Color = x.Color,
                    Order = (int)x.SortOrder,
                    IsVisible = x.IsVisible,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    ProjectCount = x.FolderProjects.Count(p => !p.IsArchived)
                })
                .ToListAsync(ct);
        }

        public async Task<List<ListFolderDTO>> GetByDepartmentAsync(bool includeArchived, int? departmentId, CancellationToken ct = default, int userId = 0, bool isViewer = false)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Visare: visa bara mappar som innehåller minst ett projekt som delats med
            // användaren/avdelningen via intern projektdelning (tomma/odelade mappar döljs),
            // och räkna bara delade projekt. Övriga: befintlig avdelningsvy.
            if (isViewer)
            {
                var today = DateTime.UtcNow.Date;
                return await context.Folders
                    .AsNoTracking()
                    .Where(x => (includeArchived || x.IsVisible) &&
                        x.FolderProjects.Any(p => (includeArchived || !p.IsArchived) &&
                            p.Shares.Any(s => (s.ValidUntil == null || s.ValidUntil >= today) &&
                                (s.SharedWithUserId == userId || (departmentId != null && s.DepartmentId == departmentId)))))
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .Select(x => new ListFolderDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Color = x.Color,
                        Order = (int)x.SortOrder,
                        IsVisible = x.IsVisible,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                        ProjectCount = x.FolderProjects.Count(p => (includeArchived || !p.IsArchived) &&
                            p.Shares.Any(s => (s.ValidUntil == null || s.ValidUntil >= today) &&
                                (s.SharedWithUserId == userId || (departmentId != null && s.DepartmentId == departmentId))))
                    })
                    .ToListAsync(ct);
            }

            return await context.Folders
                .AsNoTracking()
                .Where(x => (includeArchived || x.IsVisible) && (!departmentId.HasValue || x.DepartmentId == departmentId))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new ListFolderDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Color = x.Color,
                    Order = (int)x.SortOrder,
                    IsVisible = x.IsVisible,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    // Mirror the project list filter (GetByFolderAsync) so the
                    // chevron only shows for folders with visible projects.
                    ProjectCount = x.FolderProjects.Count(p => includeArchived || !p.IsArchived)
                })
                .ToListAsync(ct);
        }

        public async Task<List<ListFolderDTO>> GetFromOtherDepartmentAsync(int departmentId, bool includeArchived, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Folders
                .AsNoTracking()
                .Where(x => (includeArchived || x.IsVisible) && x.DepartmentId == departmentId)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new ListFolderDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Color = x.Color,
                    Order = x.SortOrder,
                    IsVisible = x.IsVisible,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    // Mirror the shared-project filter (GetOtherGroupByFolderAsync):
                    // only count projects that have a calculation shared with this department.
                    ProjectCount = x.FolderProjects.Count(p =>
                        (includeArchived || !p.IsArchived) &&
                        p.Calculations.SelectMany(c => c.SharesCalc).Any(s => s.DepartmentId == departmentId))
                })
                .ToListAsync(ct);
        }

        public async Task<DetailsFolderDTO?> GetDetailsAsync(Guid id, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Folders
                .AsNoTracking()
                .Where(x => x.Id == id &&
                    (!departmentId.HasValue || x.DepartmentId == departmentId.Value))
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
