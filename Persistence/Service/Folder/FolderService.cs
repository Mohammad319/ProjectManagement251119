using Application.Feature.Project.Folder;
using Domain.Entities.Folder;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Folder;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.Exceptions;

namespace Persistence.Service.Folder
{
    public sealed class FolderService(IDbContextFactoryTenant dbFactory) : IFolderService
    {
        private const string FolderForbiddenMessage = "Du saknar behörighet att hantera mappar i denna avdelning.";

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
            if (folder == null)
                return false;

            if (departmentId.HasValue && folder.DepartmentId != departmentId)
                throw new ForbiddenActionException(FolderForbiddenMessage);

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
                throw new ForbiddenActionException(FolderForbiddenMessage);

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
                throw new ForbiddenActionException(FolderForbiddenMessage);

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
                        DepartmentId = x.DepartmentId,
                        DepartmentName = x.Department.Name,
                        IsSharedGroup = false,
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
                    DepartmentId = x.DepartmentId,
                    DepartmentName = x.Department.Name,
                    IsSharedGroup = false,
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

        // ---------------- Shared / "Alla tillgängliga" discovery ----------------
        // All discovery below keys off PROJECT-LEVEL shares (ProjectAccessRules.CanSee): a project is
        // visible to the user when they created it OR it is shared with them directly / via their
        // department (honoring ValidUntil). Extra project sharing never grants folder management — the
        // resulting folders are returned as read-only visual groups (IsSharedGroup = true).

        public async Task<List<DepartmentAccessDTO>> GetAccessibleDepartmentsAsync(int userId, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var today = DateTime.UtcNow.Date;

            // Normal-access departments: all departments for Admin (departmentId == null), else the own department.
            var normalDeptIds = departmentId == null
                ? await context.Department.AsNoTracking().Select(d => d.Id).ToListAsync(ct)
                : new List<int> { departmentId.Value };

            // Departments where a project is shared/assigned to the user (project-level shares).
            var sharedDeptIdsRaw = await context.Projects
                .AsNoTracking()
                .Where(p => p.Shares.Any(s => (s.ValidUntil == null || s.ValidUntil >= today) &&
                    (s.SharedWithUserId == userId || (departmentId != null && s.DepartmentId == departmentId))))
                .Select(p => p.Folder.DepartmentId)
                .Distinct()
                .ToListAsync(ct);

            var sharedDeptIds = sharedDeptIdsRaw.Where(id => !normalDeptIds.Contains(id)).ToList();
            var allIds = normalDeptIds.Concat(sharedDeptIds).Distinct().ToList();

            var departments = await context.Department
                .AsNoTracking()
                .Where(d => allIds.Contains(d.Id))
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .ToListAsync(ct);

            return departments
                .Select(d => new DepartmentAccessDTO
                {
                    Id = d.Id,
                    Name = d.Name,
                    SharedOnly = !normalDeptIds.Contains(d.Id)
                })
                .ToList();
        }

        public async Task<bool> HasSharedProjectsInDepartmentAsync(int targetDepartmentId, int userId, int? callerDepartmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var today = DateTime.UtcNow.Date;

            return await context.Projects
                .AsNoTracking()
                .AnyAsync(p => p.Folder.DepartmentId == targetDepartmentId &&
                    (p.CreatedBy == userId ||
                     p.Shares.Any(s => (s.ValidUntil == null || s.ValidUntil >= today) &&
                        (s.SharedWithUserId == userId || (callerDepartmentId != null && s.DepartmentId == callerDepartmentId)))), ct);
        }

        public async Task<List<ListFolderDTO>> GetSharedDepartmentFoldersAsync(int targetDepartmentId, int userId, int? callerDepartmentId, bool includeArchived, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var today = DateTime.UtcNow.Date;

            return await context.Folders
                .AsNoTracking()
                .Where(x => (includeArchived || x.IsVisible) && x.DepartmentId == targetDepartmentId &&
                    x.FolderProjects.Any(p => (includeArchived || !p.IsArchived) &&
                        (p.CreatedBy == userId ||
                         p.Shares.Any(s => (s.ValidUntil == null || s.ValidUntil >= today) &&
                            (s.SharedWithUserId == userId || (callerDepartmentId != null && s.DepartmentId == callerDepartmentId))))))
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
                    DepartmentId = x.DepartmentId,
                    DepartmentName = x.Department.Name,
                    IsSharedGroup = true,
                    // Count only the projects the user may actually see (mirrors GetByFolderAsync/CanSee).
                    ProjectCount = x.FolderProjects.Count(p => (includeArchived || !p.IsArchived) &&
                        (p.CreatedBy == userId ||
                         p.Shares.Any(s => (s.ValidUntil == null || s.ValidUntil >= today) &&
                            (s.SharedWithUserId == userId || (callerDepartmentId != null && s.DepartmentId == callerDepartmentId)))))
                })
                .ToListAsync(ct);
        }

        public async Task<List<ListFolderDTO>> GetAccessibleFoldersAsync(int userId, int? callerDepartmentId, bool isAdmin, bool includeArchived, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var today = DateTime.UtcNow.Date;

            // A folder is in scope when the user has NORMAL access to it (own department, or any for
            // Admin) OR it holds at least one project shared/assigned to the user.
            return await context.Folders
                .AsNoTracking()
                .Where(x => (includeArchived || x.IsVisible) &&
                    (isAdmin ||
                     (callerDepartmentId != null && x.DepartmentId == callerDepartmentId) ||
                     x.FolderProjects.Any(p => (includeArchived || !p.IsArchived) &&
                        (p.CreatedBy == userId ||
                         p.Shares.Any(s => (s.ValidUntil == null || s.ValidUntil >= today) &&
                            (s.SharedWithUserId == userId || (callerDepartmentId != null && s.DepartmentId == callerDepartmentId)))))))
                .OrderBy(x => x.Department.Name)
                .ThenBy(x => x.SortOrder)
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
                    DepartmentId = x.DepartmentId,
                    DepartmentName = x.Department.Name,
                    // Normal-access folder (own dept / Admin) → editable; otherwise a read-only shared group.
                    IsSharedGroup = !(isAdmin || (callerDepartmentId != null && x.DepartmentId == callerDepartmentId)),
                    // Editable folders count all matching projects; shared groups count only visible projects.
                    ProjectCount = (isAdmin || (callerDepartmentId != null && x.DepartmentId == callerDepartmentId))
                        ? x.FolderProjects.Count(p => includeArchived || !p.IsArchived)
                        : x.FolderProjects.Count(p => (includeArchived || !p.IsArchived) &&
                            (p.CreatedBy == userId ||
                             p.Shares.Any(s => (s.ValidUntil == null || s.ValidUntil >= today) &&
                                (s.SharedWithUserId == userId || (callerDepartmentId != null && s.DepartmentId == callerDepartmentId)))))
                })
                .ToListAsync(ct);
        }

        public async Task<DetailsFolderDTO?> GetDetailsAsync(Guid id, int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var folderDepartmentId = await context.Folders
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => (int?)x.DepartmentId)
                .FirstOrDefaultAsync(ct);

            if (!folderDepartmentId.HasValue)
                return null;

            if (departmentId.HasValue && folderDepartmentId.Value != departmentId.Value)
                throw new ForbiddenActionException(FolderForbiddenMessage);

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
