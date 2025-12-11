using Application.Feature.Calculation.Task;
using Application.Interfaces.Context;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems
{
    public sealed class TaskQueryService(IShardingSingleDbContext dataAccess) : ITaskQueryService
    {
        public async Task<List<TaskListDTO>> GetByFilterAsync(
            FilterCalculationItemsDto filter,
            CancellationToken cancellationToken = default)
        {
            IQueryable<TaskEntity> query;

            // ⚠ نفترض أن عندك عمود Data في الجدول يمثل Task.Metadata كـ JSON
            // ونستخدم FromSqlInterpolated لتفادي الحقن
            if (!string.IsNullOrWhiteSpace(filter.Code) &&
                !string.IsNullOrWhiteSpace(filter.Unit))
            {
                query = dataAccess.Tasks
                    .FromSqlInterpolated($"""
                        SELECT * FROM Tasks 
                        WHERE JSON_VALUE(Data, '$.Code') LIKE '%' + {filter.Code} + '%'
                          AND JSON_VALUE(Data, '$.Unit') LIKE '%' + {filter.Unit} + '%'
                    """);
            }
            else if (!string.IsNullOrWhiteSpace(filter.Code))
            {
                query = dataAccess.Tasks
                    .FromSqlInterpolated($"""
                        SELECT * FROM Tasks 
                        WHERE JSON_VALUE(Data, '$.Code') LIKE '%' + {filter.Code} + '%'
                    """);
            }
            else if (!string.IsNullOrWhiteSpace(filter.Unit))
            {
                query = dataAccess.Tasks
                    .FromSqlInterpolated($"""
                        SELECT * FROM Tasks 
                        WHERE JSON_VALUE(Data, '$.Unit') LIKE '%' + {filter.Unit} + '%'
                    """);
            }
            else
            {
                query = dataAccess.Tasks.AsQueryable();
            }

            query = ApplyFilter(query, filter);

            // تحتاج Include(Status) لأنك تستخدم x.Status.Name/Color في الـ Select
            query = query.Include(x => x.Status);

            var tasks = await query
                // تختار الترتيب اللي يناسبك: Id أو SortOrder
                .OrderByDescending(x => x.SortOrder)
                .Select(x => new TaskListDTO
                {
                    Id = x.Id,
                    Data = x.Metadata,
                    Name = x.Name,
                    StatusColor = x.Status != null ? x.Status.Color : string.Empty,
                    Status = x.Status != null ? x.Status.Name : string.Empty,
                    TaskId = x.ParentTaskId,
                    StatusId = x.StatusId,
                })
                .ToListAsync(cancellationToken);

            return tasks;
        }

        private static IQueryable<TaskEntity> ApplyFilter(
            IQueryable<TaskEntity> query,
            FilterCalculationItemsDto filter)
        {
            if (filter.CalculationID > 0)
                query = query.Where(x => x.CalculationId == filter.CalculationID);
            else if (filter.ProjectID.HasValue)
                query = query.Where(x => x.Calculation.ProjectId == filter.ProjectID);
            else if (filter.FolderID.HasValue)
                query = query.Where(x => x.Calculation.Project.FolderId == filter.FolderID);

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                // EF لا يترجم Contains مع StringComparison
                // نتركها بسيطة، والـ collation في DB يتكفل بحساسية الأحرف.
                query = query.Where(x => x.Name.Contains(filter.Name));
            }

            if (filter.Status > 0)
                query = query.Where(x => x.StatusId == filter.Status);

            return query;
        }
    }
}
