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

            var name = (dto.Name ?? string.Empty).Trim();
            if (await NameExistsAsync(context, name, null, ct))
                return 0;

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

            var name = (dto.Name ?? string.Empty).Trim();
            if (await NameExistsAsync(context, name, id, ct))
                return false;

            entity.Update(dto);
            await context.SaveChangesAsync(ct);
            return true;
        }

        private static Task<bool> NameExistsAsync(
            Persistence.Context.ShardingSingleDbContext context, string name, int? excludeId, CancellationToken ct)
            => context.Department
                .AsNoTracking()
                .AnyAsync(d => d.Name == name && (!excludeId.HasValue || d.Id != excludeId.Value), ct);

        public async Task<int> GetTotalUsersCountAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.User.AsNoTracking().CountAsync(ct);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var hasUsers = await context.User
                .AsNoTracking()
                .AnyAsync(u => u.DepartmentId == id, ct);

            var hasFolders = await context.Folders
                .AsNoTracking()
                .AnyAsync(f => f.DepartmentId == id, ct);

            var hasApplications = await context.Applications
                .AsNoTracking()
                .AnyAsync(a => a.DepartmentId == id, ct);

            var hasTemplates = await context.Templates
                .AsNoTracking()
                .AnyAsync(t => t.DepartmentId == id, ct);

            var hasStorages = await context.Storages
                .AsNoTracking()
                .AnyAsync(s => s.DepartmentId == id, ct);

            var hasShares = await context.ShareCalc
                .AsNoTracking()
                .AnyAsync(s => s.DepartmentId == id, ct);

            if (hasUsers || hasFolders || hasApplications || hasTemplates || hasStorages || hasShares)
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
                    Color = x.Color,
                    HeadUserId = x.HeadUserId,
                    HeadUserName = x.Users
                        .Where(u => u.Id == x.HeadUserId)
                        .Select(u => ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim() != ""
                            ? ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim()
                            : u.Email)
                        .FirstOrDefault(),
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
