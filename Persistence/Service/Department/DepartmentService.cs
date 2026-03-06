using Application.Feature.Identity.Department;
using Domain.DTO.User;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.Base.Users;
using ProjectManagement.Shared.DTO.General;
using ProjectManagement.Shared.DTO.Identity;

namespace Persistence.Service.Department
{
    public sealed class DepartmentService(IDbContextFactoryTenant dbFactory) : IDepartmentService
    {
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

            var entity = await context.Department.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
                return false;

            entity.Update(dto);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var hasProjects = await context.Projects
                .AsNoTracking()
                .AnyAsync(p => p.Folder.DepartmentId == id, ct);

            if (hasProjects)
                return false;

            var entity = await context.Department.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
                return false;

            context.Department.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

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
                .OrderBy(x => x.Name)
                .Select(x => new DepartmentDetailsDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description ?? string.Empty,
                    Created = x.CreatedAt,
                    LastModified = x.UpdatedAt,
                    UsersCount = x.Users.Count,
                    FoldersCount = x.Folders.Count,
                    ProjectsCount = x.Folders.SelectMany(f => f.FolderProjects).Count()
                })
                .ToListAsync(ct);
        }

        public async Task<List<TenantUserDto>> GetUsersByDepartmentIdAsync(int? departmentId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.User
                .AsNoTracking()
                .Where(x => x.DepartmentId == departmentId)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ThenBy(x => x.UserName)
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
