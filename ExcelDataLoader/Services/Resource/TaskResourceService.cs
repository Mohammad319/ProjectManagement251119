using ExcelDataLoader.Services;
using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.Resource
{
    public interface ITaskResourceService
    {
        Task<bool> CreateAsync(TaskResourceAssignment assignment, CancellationToken ct = default);
        Task<bool> DeleteAsync(int taskId, int resourceId, CancellationToken ct = default);
    }
    public class TaskResourceService(IDbContextFactory<TaskResourceBlueprintsContext> contextFactory) : BaseService(contextFactory), ITaskResourceService
    {
        public async Task<bool> CreateAsync(TaskResourceAssignment assignment, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var exists = await context.TaskResourceAssignments
                .AnyAsync(tr => tr.TaskId == assignment.TaskId && tr.ResourceId == assignment.ResourceId, ct);

            if (exists)
                return false;

            context.TaskResourceAssignments.Add(assignment);
            await context.SaveChangesAsync(ct);
            return true;
        }
        public async Task<bool> DeleteAsync(int taskId, int resourceId, CancellationToken ct = default)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var existing = await context.TaskResourceAssignments
                .FirstOrDefaultAsync(tr => tr.TaskId == taskId && tr.ResourceId == resourceId, ct);
            if (existing == null) return false;
            context.TaskResourceAssignments.Remove(existing);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public class TaskResourceLinkDto
        {
            public int TaskId { get; set; }
            public int ResourceId { get; set; }
        }
    }

}
