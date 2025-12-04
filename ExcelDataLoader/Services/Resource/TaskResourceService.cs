using ExcelDataLoader.Services;
using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Entities.Questions.Assignments;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.Resource
{
    public interface ITaskResourceService
    {
        Task<bool> CreateAsync(TaskResourceAssignmentEntity conditions);
        Task<bool> DeletAsync(int taskid, int resId);
    }
    public class TaskResourceService(IDbContextFactory<ProjectImportHubContext> contextFactory) : BaseService(contextFactory), ITaskResourceService
    {
        public async Task<bool> CreateAsync(TaskResourceAssignmentEntity conditions)
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
