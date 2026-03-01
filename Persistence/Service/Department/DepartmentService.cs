using Application.Feature.Identity.Department;
using Domain.DTO.User;
using Domain.Entities.Users;
using Persistence.Factory;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Identity;

namespace Persistence.Service.Department
{
    public sealed class DepartmentService(IDbContextFactoryTenant dbFactory) : IDepartmentService
    {
        // ---------------- Commands ----------------
        public async Task<int> CreateAsync(DepartmentBase dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entity = DepartmentEntity.Create(dto);
            context.Department.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, DepartmentBase dto, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.Department.FindAsync(id, ct);
            if (entity == null) return false;

            entity.Update(dto);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.Department
                .Include(x => x.Projects)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity == null) return false;

            // ممنوع الحذف إذا مرتبط بمشاريع
            if (entity.Projects.Any())
                return false;

            context.Department.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        // ---------------- Queries ----------------

        public async Task<List<ListDTO>> GetAsListAsync(CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Department
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ListDTO
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync(ct);
        }

        public async Task<List<DepartmentDetailsDTO>> GetDetailsAsync(CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Department
                .AsNoTracking()
                .Select(x => new DepartmentDetailsDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description ?? string.Empty,
                    Created = x.CreatedAt,
                    LastModified = x.UpdatedAt,
                    UsersCount = x.Users.Count,
                    ProjectsCount = x.Projects.Count,
                    FoldersCount = x.Folders.Count
                })
                .ToListAsync(ct);
        }

        public async Task<List<TenantUserDto>> GetUsersByDepartmentIdAsync(int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.User.Where(x=>x.DepartmentId == departmentId)
                .AsNoTracking()
                .Select(x => new TenantUserDto
                {
                    Id = x.Id,
                    Username = x.UserName,
                    Email = x.Email,
                    DepartmentId = departmentId,
                    Firstname = x.FirstName,
                    Lastname = x.LastName,
                    IdAuth = x.ExternalAuthId,
                })
                .ToListAsync(ct);
        }
    }
}
