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

            term = term.Trim();

            return await context.Resources
                .AsNoTracking()
                .Where(r => r.IsActive &&
                            (r.Name.Contains(term))) // عدّل حسب الحاجة
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
            resource.CalcResCost ??= new CalcResCost();
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

            // نتأكد أولاً أن الـ Resource موجود
            var resource = await context.Resources.FindAsync(id);
            if (resource is null)
                return false;

            // حذف العلاقات المرتبطة
            var conditionAssignments = await context.ConditionResourceAssignments
                .Where(x => x.ResourceId == id)
                .ToListAsync();

            var resourceChoices = await context.ResourceChoiceOptions
                .Where(x => x.ResourceId == id)
                .ToListAsync();

            var taskAssignments = await context.TaskResourceAssignments
                .Where(x => x.ResourceId == id)
                .ToListAsync();

            context.RemoveRange(conditionAssignments);
            context.RemoveRange(resourceChoices);
            context.RemoveRange(taskAssignments);

            context.Resources.Remove(resource);

            await context.SaveChangesAsync();

            return true;
        }
    }
}
