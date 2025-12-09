using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Group.Queries;

public sealed record GetTasksByFilterQuery(FilterCalculationItemsDto Filter) : IRequest<object>;

public sealed class GetTasksByFilterQueryHandler(IShardingSingleDbContext _dataAccess)
    : IRequestHandler<GetTasksByFilterQuery, object>
{
    public async Task<object> Handle(GetTasksByFilterQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        IQueryable<TaskEntity> query;

        if (!string.IsNullOrWhiteSpace(filter.Code) && !string.IsNullOrWhiteSpace(filter.Unit))
        {
            query = _dataAccess.Tasks.FromSql(
                $"SELECT * FROM Tasks WHERE JSON_VALUE(Data, '$.Code') LIKE '%{filter.Code}%' AND JSON_VALUE(Data, '$.Unit') LIKE '%{filter.Unit}%'");
        }
        else if (!string.IsNullOrWhiteSpace(filter.Code))
        {
            query = _dataAccess.Tasks.FromSql(
                $"SELECT * FROM Tasks WHERE JSON_VALUE(Data, '$.Code') LIKE '%{filter.Code}%'");
        }
        else if (!string.IsNullOrWhiteSpace(filter.Unit))
        {
            query = _dataAccess.Tasks.FromSql(
                $"SELECT * FROM Tasks WHERE JSON_VALUE(Data, '$.Unit') LIKE '%{filter.Unit}%'");
        }
        else
        {
            query = _dataAccess.Tasks.AsQueryable().Distinct();
        }

        query = ApplyFilter(query, filter);

        var tasks = await query
            .OrderByDescending(x => x)
            .Select(x => new TaskListDTO
            {
                Id = x.Id,
                Data = x.Metadata,
                Name = x.Name,
                StatusColor = x.Status.Color,
                Status = x.Status.Name,
                TaskId = x.ParentTaskId,
                StatusId = x.StatusId,
            })
            .ToListAsync(cancellationToken);

        return tasks;
    }

    private static IQueryable<TaskEntity> ApplyFilter(IQueryable<TaskEntity> query, FilterCalculationItemsDto filter)
    {
        if (filter.CalculationID > 0)
            query = query.Where(x => x.CalculationId == filter.CalculationID);
        else if (filter.ProjectID.HasValue)
            query = query.Where(x => x.Calculation.ProjectId == filter.ProjectID);
        else if (filter.FolderID.HasValue)
            query = query.Where(x => x.Calculation.Project.FolderId == filter.FolderID);

        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(x => x.Name.Contains(filter.Name, StringComparison.CurrentCultureIgnoreCase));

        if (filter.Status > 0)
            query = query.Where(x => x.StatusId == filter.Status);

        return query;
    }
}
