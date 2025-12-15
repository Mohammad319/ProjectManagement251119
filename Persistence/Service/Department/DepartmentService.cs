using Application.Feature.Identity.Department;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Identity;

namespace Persistence.Service.Department
{
    public sealed class DepartmentService(ShardingSingleDbContext db) : IDepartmentService
    {

        // ---------------- Commands ----------------

        public async Task<int> CreateAsync(DepartmentBase dto, CancellationToken ct)
        {
            var entity = DepartmentEntity.Create(dto);
            db.Department.Add(entity);
            await db.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, DepartmentBase dto, CancellationToken ct)
        {
            var entity = await db.Department.FindAsync(id, ct);
            if (entity == null) return false;

            entity.Update(dto);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            var entity = await db.Department
                .Include(x => x.Projects)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity == null) return false;

            // ممنوع الحذف إذا مرتبط بمشاريع
            if (entity.Projects.Any())
                return false;

            db.Department.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }

        // ---------------- Queries ----------------

        public Task<List<ListDTO>> GetAsListAsync(CancellationToken ct)
        {
            return db.Department
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new ListDTO
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync(ct);
        }

        public Task<List<DepartmentDetailsDTO>> GetDetailsAsync(CancellationToken ct)
        {
            return db.Department
                .AsNoTracking()
                .Select(x => new DepartmentDetailsDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Created = x.CreatedAt,
                    LastModified = x.UpdatedAt,
                    UsersCount = x.Users.Count,
                    ProjectsCount = x.Projects.Count,
                    FoldersCount = x.Folders.Count
                })
                .ToListAsync(ct);
        }
    }
}
