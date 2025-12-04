using Microsoft.EntityFrameworkCore;
using ProjectImportHub.Dto.ProjectTask;
using ProjectImportHub.Infrastructure;

namespace ProjectImportHub.Services.ProjectTask
{
    public interface IProjectTaskQueryService
    {
        Task<IReadOnlyList<ProjectTaskListItemDto>> GetListAsync(CancellationToken ct);
        Task<ProjectTaskEditDto> GetForEditAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<ResourceTaskIndexDto>> GetTaskResourcesAsync(int id, CancellationToken ct);

    }

    public sealed class ProjectTaskQueryService(IDbContextFactory<ProjectImportHubContext> factory)
        : IProjectTaskQueryService
    {
        // Deutsch: Liste für Index-Grid (leichtgewichtig, ohne Includes)
        public async Task<IReadOnlyList<ProjectTaskListItemDto>> GetListAsync(CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            return await db.Tasks
                .AsNoTracking()
                .OrderBy(t => t.SortOrder)
                .Select(t => new ProjectTaskListItemDto(
                    t.Id,
                    t.Code,
                    t.DisplayName,
                    t.UnitCode,
                    t.Quantity,
                    t.IsActive
                ))
                .ToListAsync(ct);
        }

        // Deutsch: Detail für Bearbeiten (nur benötigte Felder)
        public async Task<ProjectTaskEditDto> GetForEditAsync(int id, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            var e = await db.Tasks
                .AsNoTracking()
                .FirstAsync(x => x.Id == id, ct);

            var dto = new ProjectTaskEditDto
            {
                Id = e.Id,
                ActionId = e.ActionId,
                ActionTypeId = e.ActionTypeId,
                FallId = e.FallId,
                LocationId = e.LocationId,
                Code = e.Code,
                DisplayName = e.DisplayName,
                UnitGroupId = e.UnitGroupId,
                UnitCode = e.UnitCode,
                Quantity = e.Quantity,
                ChangeFactor1 = e.ChangeFactor1,
                ChangeFactor2 = e.ChangeFactor2,
                IsActive = e.IsActive,
                Note = e.Note,
                VisibleFolderIds = e.VisibleFolderIds?.ToList() ?? [],
                Responsible = e.Responsible,
                Status = e.Status,
                AdminNote = e.AdminNote,
                Uncontrollable = e.Uncontrollable
            };

            if (e.WorkloadThresholds?.Count >= 3)
            {
                dto.Thickness = e.WorkloadThresholds[0];
                dto.Width = e.WorkloadThresholds[1];
                dto.Length = e.WorkloadThresholds[2];
            }

            return dto;
        }

        public async Task<IReadOnlyList<ResourceTaskIndexDto>> GetTaskResourcesAsync(int id, CancellationToken ct)
        {
            await using var db = await factory.CreateDbContextAsync(ct);

            return await db.TaskResourceAssignments
                .AsNoTracking().Where(t => t.TaskId == id)
                .Select(t => new ResourceTaskIndexDto(
                    t.ResourceId,
                    t.Resource.Name,
                    t.Resource.Data.ChangeFactor1,
                    t.Resource.Data.ChangeFactor2,
                    t.Resource.Data.Unit,
                    t.Resource.Active
                ))
                .ToListAsync(ct);
        }
    }
}