using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Services.Common;

namespace TaskResourceBlueprints.Services.Resource
{
    public interface ITaskResourceService
    {
        Task<bool> CreateAsync(TaskResourceAssignment assignment, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int taskId, int resourceId, CancellationToken cancellationToken = default);
    }

    public sealed class TaskResourceService : DbContextServiceBase, ITaskResourceService
    {
        public TaskResourceService(IDbContextFactory<TaskResourceBlueprintsContext> contextFactory)
            : base(contextFactory)
        {
        }

        public async Task<bool> CreateAsync(
            TaskResourceAssignment assignment,
            CancellationToken cancellationToken = default)
        {
            await using var context = await CreateDbContextAsync(cancellationToken);

            // يمكن التحقق من عدم التكرار إن احتجت:
            var exists = await context.TaskResourceAssignments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.TaskId == assignment.TaskId &&
                    x.ResourceId == assignment.ResourceId,
                    cancellationToken);

            if (exists)
                return false;

            await context.TaskResourceAssignments.AddAsync(assignment, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAsync(
            int taskId,
            int resourceId,
            CancellationToken cancellationToken = default)
        {
            await using var context = await CreateDbContextAsync(cancellationToken);

            var existing = await context.TaskResourceAssignments
                .FirstOrDefaultAsync(
                    tr => tr.TaskId == taskId && tr.ResourceId == resourceId,
                    cancellationToken);

            if (existing is null)
                return false;

            context.TaskResourceAssignments.Remove(existing);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public sealed class TaskResourceLinkDto
        {
            public int TaskId { get; init; }
            public int ResourceId { get; init; }
        }
    }
}
