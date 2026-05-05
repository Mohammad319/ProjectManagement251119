using Application.Feature.Calculation.Task;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Task
{
    public sealed class TaskQueryService(IDbContextFactoryTenant dbFactory) : ITaskQueryService
    {
        public async Task<List<TaskListDTO>> GetByFilterAsync(
            FilterCalculationItemsDto filter,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var query = context.Tasks
                .AsNoTracking()
                .Include(x => x.Status)
                .AsQueryable();

            query = ApplyFilter(query, filter);

            return await query
                .OrderByDescending(x => x.SortOrder)
                .Select(x => new TaskListDTO
                {
                    Id = x.Id,
                    Quantity = x.Quantity,
                    Unit = x.Unit ?? string.Empty,
                    Metadata = CalculationItemMetadataMapper.BuildTaskMetadata(
                        x.Metadata,
                        x.Note,
                        null,
                        x.Code,
                        x.IsActive,
                        x.Type,
                        x.IsOH),
                    Name = x.Name,
                    SortOrder = x.SortOrder,
                    StatusColor = x.Status != null ? x.Status.Color : string.Empty,
                    Status = x.Status != null ? x.Status.Name : string.Empty,
                    TaskId = x.ParentTaskId,
                    StatusId = x.StatusId,
                })
                .ToListAsync(ct);
        }

        private static IQueryable<TaskEntity> ApplyFilter(
            IQueryable<TaskEntity> query,
            FilterCalculationItemsDto filter)
        {
            if (filter.CalculationID > 0)
                query = query.Where(x => x.CalculationId == filter.CalculationID);
            else if (filter.ProjectID.HasValue)
                query = query.Where(x => x.Calculation.ProjectId == filter.ProjectID.Value);
            else if (filter.FolderID.HasValue)
                query = query.Where(x => x.Calculation.Project.FolderId == filter.FolderID.Value);

            if (!string.IsNullOrWhiteSpace(filter.Code))
            {
                var q = $"%{filter.Code.Trim()}%";
                query = query.Where(x => x.Code != null && EF.Functions.Like(x.Code, q));
            }

            if (!string.IsNullOrWhiteSpace(filter.Unit))
            {
                var q = $"%{filter.Unit.Trim()}%";
                query = query.Where(x => x.Unit != null && EF.Functions.Like(x.Unit, q));
            }

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var q = $"%{filter.Name.Trim()}%";
                query = query.Where(x => EF.Functions.Like(x.Name, q));
            }

            if (filter.Status > 0)
                query = query.Where(x => x.StatusId == filter.Status);

            return query;
        }
    }
}
