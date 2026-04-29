using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Dto.Resource;
using TaskResourceBlueprints.Entities.Tasks;
using TaskResourceBlueprints.Infrastructure;
using TaskResourceBlueprints.Services.ProjectTask;

namespace TaskResourceBlueprints.Services.Resource
{
    public interface ITaskResourceService
    {
        Task<(IReadOnlyList<ResourceRowDto> rows, string folderName)> GetFolderResourcesAsync(
            int projectTaskId, int folderId, CancellationToken ct = default);
        Task AddAssignmentAsync(int taskId, int resourceId, CancellationToken ct = default);
        Task RemoveResourceFromTaskAsync(int taskId, int resourceId, CancellationToken ct = default);
        Task<bool> RemoveAssignmentAsync(int assignmentId, CancellationToken ct = default);
        Task<TaskResourceDto?> GetTaskResourceAsync(int taskId, int resourceId, CancellationToken ct);
        Task<bool> UpdateAssignmentAsync(int taskId, int resourceId, TaskResourceDto dto, CancellationToken ct);
    }

    public sealed class TaskResourceService(IDbContextFactory<TaskResourceBlueprintsContext> factory)
        : ITaskResourceService
    {
        public async Task<(IReadOnlyList<ResourceRowDto> rows, string folderName)> GetFolderResourcesAsync(
            int projectTaskId, int folderId, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var folderName = await db.ResourceCategories.AsNoTracking()
                .Where(f => f.Id == folderId)
                .Select(f => f.DisplayName)
                .FirstOrDefaultAsync(ct) ?? string.Empty;

            var assignedIds = await db.TaskDefinitionResourceLinks.AsNoTracking()
                .Where(l => l.TaskDefinitionId == projectTaskId)
                .Select(l => l.ResourceDefinitionId)
                .ToHashSetAsync(ct);

            var rows = await db.Resources.AsNoTracking()
                .Where(r => r.FolderId == folderId)
                .OrderBy(r => r.SortOrder)
                .ThenBy(r => r.Name)
                .Select(r => new ResourceRowDto(
                    r.Id,
                    r.Name,
                    r.ResType.ToString(),
                    r.Data.Unit,
                    r.Data.Quantity,
                    r.Data.ChangeFactor1,
                    r.Data.ChangeFactor2,
                    r.Data.CapWaste,
                    r.Data.Cost,
                    r.Data.BaseCost,
                    assignedIds.Contains(r.Id)
                ))
                .ToListAsync(ct);

            return (rows, folderName);
        }

        public async Task AddAssignmentAsync(int taskId, int resourceId, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var exists = await db.TaskDefinitionResourceLinks.AnyAsync(
                l => l.TaskDefinitionId == taskId && l.ResourceDefinitionId == resourceId, ct);
            if (!exists)
            {
                db.TaskDefinitionResourceLinks.Add(new TaskDefinitionResourceLink
                {
                    TaskDefinitionId = taskId,
                    ResourceDefinitionId = resourceId
                });
                await db.SaveChangesAsync(ct);
            }
        }

        public async Task RemoveResourceFromTaskAsync(int taskId, int resourceId, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var link = await db.TaskDefinitionResourceLinks
                .FirstOrDefaultAsync(l => l.TaskDefinitionId == taskId && l.ResourceDefinitionId == resourceId, ct);
            if (link is not null)
            {
                db.TaskDefinitionResourceLinks.Remove(link);
                await db.SaveChangesAsync(ct);
            }
        }

        public async Task<bool> RemoveAssignmentAsync(int assignmentId, CancellationToken ct = default)
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            var link = await db.TaskDefinitionResourceLinks.FindAsync([assignmentId], ct);
            if (link is null) return false;
            db.TaskDefinitionResourceLinks.Remove(link);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<TaskResourceDto?> GetTaskResourceAsync(int taskId, int resourceId, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var link = await db.TaskDefinitionResourceLinks.AsNoTracking()
                .FirstOrDefaultAsync(l => l.TaskDefinitionId == taskId && l.ResourceDefinitionId == resourceId, ct);

            var resource = await db.Resources.AsNoTracking()
                .Where(r => r.Id == resourceId)
                .Select(r => new { r.ResType, r.Data.Unit })
                .FirstOrDefaultAsync(ct);

            if (resource is null)
                return null;

            if (link is null)
                return new TaskResourceDto
                {
                    ResType = resource.ResType,
                    Unit = resource.Unit,
                };

            return new TaskResourceDto
            {
                Quantity = link.Quantity,
                IsFixed = link.IsFixed,
                ResType = resource.ResType,
                CapRole = [],
                Parameters = link.Parameters,
                AddOns = link.AddOns,
                Times = link.Times,
                Unit = resource.Unit,
            };
        }

        public async Task<bool> UpdateAssignmentAsync(int taskId, int resourceId, TaskResourceDto dto, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var link = await db.TaskDefinitionResourceLinks
                .FirstOrDefaultAsync(l => l.TaskDefinitionId == taskId && l.ResourceDefinitionId == resourceId, ct);

            if (link is null) return false;

            link.Quantity = dto.Quantity;
            link.IsFixed = dto.IsFixed;
            link.Parameters = dto.Parameters;
            link.AddOns = dto.AddOns;
            link.Times = dto.Times;
            await db.SaveChangesAsync(ct);
            return true;
        }
    }
}
