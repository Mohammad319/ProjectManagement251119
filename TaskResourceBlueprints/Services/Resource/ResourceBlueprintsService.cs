using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.App.Dataloader;
using TaskResourceBlueprints.Dto.Resource;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Resource
{
    public interface IResourceBlueprintsService
    {
        Task<List<ResourceLookupDto>> SearchAsync(string term, int maxResults, CancellationToken ct = default);

        Task<int> CreateAsync(ResourceDefinition resource);
        Task<bool> UpdateAsync(ResourceDefinition resource);
        Task<bool> DeleteAsync(int id);
    }

    public class ResourceBlueprintsService(IDbContextFactory<TaskResourceBlueprintsContext> dbContextFactory) : IResourceBlueprintsService
    {
        public async Task<List<ResourceLookupDto>> SearchAsync(string term, int maxResults, CancellationToken ct = default)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(ct);

            term = (term ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(term))
                return [];

            maxResults = Math.Clamp(maxResults, 1, 100);

            var pattern = $"%{term}%";

            return await context.Resources
                .AsNoTracking()
                .Where(r => r.IsActive && EF.Functions.Like(r.Name, pattern))
                .OrderBy(r => r.Name)
                .Take(maxResults)
                .Select(r => new ResourceLookupDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Group = r.Folder != null ? r.Folder.DisplayName : null
                })
                .ToListAsync(ct);
        }

        public async Task<int> CreateAsync(ResourceDefinition resource)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await context.Resources.AddAsync(resource);
            await context.SaveChangesAsync();

            return resource.Id;
        }

        public async Task<bool> UpdateAsync(ResourceDefinition resource)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var existing = await context.Resources.FindAsync(resource.Id);
            if (existing is null)
                return false;

            context.Entry(existing).CurrentValues.SetValues(resource);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            var resource = await context.Resources
                .Include(r => r.TenantLinks)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resource is null)
                return false;

            if (resource.TenantLinks.Count > 0)
                context.RemoveRange(resource.TenantLinks);

            context.Resources.Remove(resource);

            await context.SaveChangesAsync();

            return true;
        }
    }
}
