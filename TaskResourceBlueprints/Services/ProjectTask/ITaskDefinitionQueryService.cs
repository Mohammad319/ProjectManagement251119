using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskResourceBlueprints.Dto.ProjectTask;
using TaskResourceBlueprints.Infrastructure;

namespace TaskResourceBlueprints.Services.ProjectTask
{
    public interface ITaskDefinitionQueryService
    {
        Task<IReadOnlyList<ProjectTaskListItemDto>> GetListAsync(CancellationToken ct);
        Task<TaskDefinitionEditDto> GetForEditAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<ResourceTaskIndexDto>> GetResourcesForTaskAsync(int id, CancellationToken ct);
    }

    public sealed class ProjectTaskQueryService(IDbContextFactory<TaskResourceBlueprintsContext> dbContextFactory) : ITaskDefinitionQueryService
    {
        public async Task<IReadOnlyList<ProjectTaskListItemDto>> GetListAsync(CancellationToken ct)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            return await db.Tasks.AsNoTracking()
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .Select(t => new ProjectTaskListItemDto(
                    t.Id,
                    t.Code,
                    t.Name,
                    t.UnitCode,
                    t.Quantity,
                    t.IsActive
                ))
                .ToListAsync(ct);
        }

        public async Task<TaskDefinitionEditDto> GetForEditAsync(int id, CancellationToken ct)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            var e = await db.Tasks
                .AsNoTracking()
                .Include(t => t.StateLinks)
                .FirstAsync(x => x.Id == id, ct);

            var dto = new TaskDefinitionEditDto
            {
                Id = e.Id,
                Code = e.Code,
                DisplayName = e.Name,
                UnitGroupId = e.TaskUnitGroupId,
                UnitCode = e.UnitCode,
                Quantity = e.Quantity,
                PriceProduction = e.PriceProduction,
                ChangeFactor1 = e.ChangeFactor1,
                ChangeFactor2 = e.ChangeFactor2,
                IsActive = e.IsActive,
                Note = e.FieldNotes,
                VisibleFolderIds = e.VisibleFolderIds?.ToList() ?? new List<int>(),
                Responsible = e.Responsible,
                Status = e.Status,
                AdminNote = e.AdminNote,
                Uncontrollable = e.Uncontrollable,
                SelectedStateIds = e.StateLinks.Select(l => l.TaskStateId).ToList()
            };

            if (e.WorkloadThresholds?.Count >= 3)
            {
                dto.Thickness = e.WorkloadThresholds[0];
                dto.Width = e.WorkloadThresholds[1];
                dto.Length = e.WorkloadThresholds[2];
            }

            return dto;
        }

        public async Task<IReadOnlyList<ResourceTaskIndexDto>> GetResourcesForTaskAsync(int id, CancellationToken ct)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            return await db.TaskDefinitionResourceLinks.AsNoTracking()
                .Where(l => l.TaskDefinitionId == id)
                .OrderBy(l => l.Resource!.SortOrder)
                .ThenBy(l => l.Resource!.Name)
                .Select(l => new ResourceTaskIndexDto(
                    l.Id,
                    l.ResourceDefinitionId,
                    l.Resource!.Name,
                    l.Resource.Data.ChangeFactor1,
                    l.Resource.Data.ChangeFactor2,
                    l.Resource.Data.CapWaste,
                    l.Resource.Data.BaseCost,
                    l.Resource.Data.Unit ?? string.Empty,
                    l.Resource.IsActive
                ))
                .ToListAsync(ct);
        }
    }
}
