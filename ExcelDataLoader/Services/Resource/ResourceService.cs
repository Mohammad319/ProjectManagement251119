
using ExcelDataLoader.Services;
using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Infrastructure;
using ProjectManagement.Shared.DTO.App.Dataloader;
using ProjectManagement.Shared.DTO.App.List;
using System.Linq.Expressions;

namespace ProjectImportHub.Services.Resource
{
    public static class ResourceSelectors
    {
        public static Expression<Func<Entities.ResourceEntity, ResourceEXDto>> Selector => x => new ResourceEXDto()
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
        Task<List<Entities.ResourceEntity>> GetAllAsync();
        Task<ResourceEXDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(Entities.ResourceEntity resource);
        Task<bool> UpdateAsync(Entities.ResourceEntity resource);
        Task<bool> DeleteAsync(int id);
        Task<List<TabItem>> GetTabItems();
    }
    public class ResourceService(IDbContextFactory<ProjectImportHubContext> contextFactory) : 
        BaseService(contextFactory) , IResourceService
    {
        public async Task<List<Entities.ResourceEntity>> GetAllAsync()
        {
            await using var _context = _contextFactory.CreateDbContext();
            return await _context.Resources.ToListAsync();
        }

        public async Task<ResourceEXDto?> GetByIdAsync(int id)
        {
            await using var _context = _contextFactory.CreateDbContext();
            return await _context.Resources.Where(x => x.Id == id).Select(ResourceSelectors.Selector).FirstOrDefaultAsync();
        }

        public async Task<int> CreateAsync(Entities.ResourceEntity resource)
        {
            await using var _context = _contextFactory.CreateDbContext();
            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();
            return resource.Id;
        }

        public async Task<bool> UpdateAsync(Entities.ResourceEntity resource)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var existing = await _context.Resources.FindAsync(resource.Id);
            if (existing == null)
                return false;

            _context.Entry(existing).CurrentValues.SetValues(resource);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            await using var _context = _contextFactory.CreateDbContext();
            var ra = await _context.ConditionResourceAssignments.Where(x => x.ResourceId == id).ToListAsync();
            var rc = await _context.ResourceOptionItems.Where(x => x.ResourceId == id).ToListAsync();
            var tsre = await _context.TaskResourceAssignments.Where(x => x.ResourceId == id).ToListAsync();

            _context.RemoveRange(ra);
            _context.RemoveRange(rc);
            _context.RemoveRange(tsre);
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
                return false;

            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<List<TabItem>> GetTabItems()
        {
            await using var _context = _contextFactory.CreateDbContext();
            return await _context.Resources
                .Select(x => new TabItem(x.Id, x.Name))
                .ToListAsync();
        }
    }

}
