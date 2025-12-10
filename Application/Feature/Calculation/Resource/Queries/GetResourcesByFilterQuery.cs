using Application.Extention;
using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Calculation;
using System;

namespace Application.Feature.Calculation.Resource.Queries;

public sealed record GetResourcesByFilterQuery(FilterCalculationItemsDto Filter) : IRequest<object>;

public sealed class GetResourcesByFilterQueryHandler(IShardingSingleDbContext _dataAccess)
    : IRequestHandler<GetResourcesByFilterQuery, object>
{
    public async Task<object> Handle(GetResourcesByFilterQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var query = _dataAccess.Resources.AsNoTracking().AsQueryable();

        query = ApplyBaseFilter(query, filter);
        query = ApplyOptionalFilter(query, filter);

        var resultList = await query.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(filter.Unit))
        {
            resultList = resultList
                .Where(x => x.Metadata.Unit.Contains(filter.Unit, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }

        return resultList.Select(x => x.MapToResourceListDTO()).ToList();
    }

    private static IQueryable<Domain.Entities.Calculation.ResourceEntity> ApplyBaseFilter(
        IQueryable<Domain.Entities.Calculation.ResourceEntity> query,
        FilterCalculationItemsDto filter)
    {
        if (filter.CalculationID > 0)
            return query.Where(x => x.Task.CalculationId == filter.CalculationID);

        if (filter.ProjectID.HasValue)
            return query.Where(x => x.Task.Calculation.ProjectId == filter.ProjectID);

        if (filter.FolderID.HasValue)
            return query.Where(x => x.Task.Calculation.Project.FolderId == filter.FolderID);

        return query;
    }

    private static IQueryable<Domain.Entities.Calculation.ResourceEntity> ApplyOptionalFilter(
        IQueryable<Domain.Entities.Calculation.ResourceEntity> query,
        FilterCalculationItemsDto filter)
    {
        if (filter.ResType.HasValue)
            query = query.Where(x => x.ResType == filter.ResType);

        if (filter.ResourceTypeId > 0)
            query = query.Where(x => x.ResourceTypeId == filter.ResourceTypeId);

        if (filter.ResourceSortId > 0)
            query = query.Where(x => x.ResourceSortId == filter.ResourceSortId);

        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(x => x.Name == filter.Name);

        if (filter.Account > 0)
            query = query.Where(x => x.AccountId == filter.Account);

        if (filter.AccountGroup > 0 && filter.Account == 0)
            query = query.Where(x => x.Account.AccountGroupId == filter.AccountGroup);

        if (filter.Status > 0)
            query = query.Where(x => x.StatusId == filter.Status);

        return query;
    }
}
