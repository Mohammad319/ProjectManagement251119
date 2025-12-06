using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.App.List;
using System.Linq.Expressions;
using TaskResourceBlueprints.Entities;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Resource
{
    public static class ResourceSelectors
    {
        public static Expression<Func<ResourceDefinition, ResourceEXDto>> Selector => x => new ResourceEXDto
        {
            Id = x.Id,
            Name = x.Name,
            Data = x.Data,
            Group = x.Folder != null ? x.Folder.DisplayName : string.Empty,
            ResType = x.ResType,
            SortOrder = x.SortOrder,
        };
    }

    public interface IResourceService
    {
        Task<List<ResourceDefinition>> GetAllAsync();
        Task<ResourceEXDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(ResourceDefinition resource);
        Task<bool> UpdateAsync(ResourceDefinition resource);
        Task<bool> DeleteAsync(int id);
        Task<List<TabItem>> GetTabItems();
    }

    public class ResourceService(IDbContextFactory<TaskResourceBlueprintsContext> _contextFactory) : IResourceService
    {
        public async Task<List<ResourceDefinition>> GetAllAsync()
        {
            await using var context = _contextFactory.CreateDbContext();

            return await context.Resources
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ResourceEXDto?> GetByIdAsync(int id)
        {
            await using var context = _contextFactory.CreateDbContext();

            return await context.Resources
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(ResourceSelectors.Selector)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CreateAsync(ResourceDefinition resource)
        {
            await using var context = _contextFactory.CreateDbContext();

            context.Resources.Add(resource);
            await context.SaveChangesAsync();

            return resource.Id;
        }

        public async Task<bool> UpdateAsync(ResourceDefinition resource)
        {
            await using var context = _contextFactory.CreateDbContext();

            var existing = await context.Resources.FindAsync(resource.Id);
            if (existing == null)
                return false;

            context.Entry(existing).CurrentValues.SetValues(resource);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var context = _contextFactory.CreateDbContext();

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

            var resource = await context.Resources.FindAsync(id);
            if (resource == null)
                return false;

            context.Resources.Remove(resource);
            await context.SaveChangesAsync();

            return true;
        }

        public async Task<List<TabItem>> GetTabItems()
        {
            await using var context = _contextFactory.CreateDbContext();

            return await context.Resources
                .AsNoTracking()
                .Select(x => new TabItem(x.Id, x.Name))
                .ToListAsync();
        }
    }
}
