using ExcelDataLoader.Services;
using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Resource
{
    public interface ITaskResourceService
    {
        Task<bool> CreateAsync(TaskResourceAssignment conditions);
        Task<bool> DeletAsync(int taskid, int resId);
    }
    public class TaskResourceService(IDbContextFactory<TaskResourceBlueprintsContext> contextFactory) : BaseService(contextFactory), ITaskResourceService
    {
        public async Task<bool> CreateAsync(TaskResourceAssignment conditions)
        {
            await using var context = _contextFactory.CreateDbContext();
            context.TaskResourceAssignments.Add(conditions);
            await context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeletAsync(int taskid, int resId)
        {
            await using var context = _contextFactory.CreateDbContext();
            var existing = await context.TaskResourceAssignments
.FirstOrDefaultAsync(tr => tr.TaskId == taskid && tr.ResourceId == resId);
            if (existing == null) return false;
            context.TaskResourceAssignments.Remove(existing);
            await context.SaveChangesAsync();
            return true;
        }

        public class TaskResourceLinkDto
        {
            public int TaskId { get; set; }
            public int ResourceId { get; set; }
        }
    }

}
