using Application.Feature.Calculation.Task;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Task
{
    public sealed class TaskQueryService(IDbContextFactoryTenant dbFactory) : ITaskQueryService
    {
        public async Task<List<TaskListDTO>> GetByFilterAsync(
            FilterCalculationItemsDto filter,
            CancellationToken ct = default)
        {
            IQueryable<TaskEntity> query;
            await using var context = await dbFactory.CreateDbContextAsync(ct);
                query = context.Tasks.AsQueryable();
            

            query = ApplyFilter(query, filter);

            // تحتاج Include(Status) لأنك تستخدم x.Status.Name/Color في الـ Select
            query = query.Include(x => x.Status);

            var tasks = await query
                .OrderByDescending(x => x.SortOrder)
                .Select(x => new TaskListDTO
                {
                    Id = x.Id,
                    Metadata = x.Metadata,
                    Name = x.Name,
                    StatusColor = x.Status != null ? x.Status.Color : string.Empty,
                    Status = x.Status != null ? x.Status.Name : string.Empty,
                    TaskId = x.ParentTaskId,
                    StatusId = x.StatusId,
                })
                .ToListAsync(ct);

            return tasks;
        }

        private static IQueryable<TaskEntity> ApplyFilter(
            IQueryable<TaskEntity> query,
            FilterCalculationItemsDto filter)
        {
            // هذا الجزء منطقي كـ "نطاق واحد فقط" (Calculation أو Project أو Folder)
            if (filter.CalculationID > 0)
                query = query.Where(x => x.CalculationId == filter.CalculationID);
            else if (filter.ProjectID.HasValue)
                query = query.Where(x => x.Calculation.ProjectId == filter.ProjectID.Value);
            else if (filter.FolderID.HasValue)
                query = query.Where(x => x.Calculation.Project.FolderId == filter.FolderID.Value);


            if (!string.IsNullOrWhiteSpace(filter.Code))
            {
                var q = filter.Code.Trim();
                query = query.Where(x => x.Code != null && EF.Functions.Contains(x.Code, q));
            }

            if (!string.IsNullOrWhiteSpace(filter.Unit))
            {
                var q = filter.Unit.Trim();
                query = query.Where(x => x.Unit != null && EF.Functions.Contains(x.Unit, q));
            }

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var q = filter.Name.Trim();
                query = query.Where(x => EF.Functions.Contains(x.Name, q));
            }

            if (filter.Status > 0)
                query = query.Where(x => x.StatusId == filter.Status);

            return query;
        }
    }
}
