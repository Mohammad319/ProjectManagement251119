using Application.Interfaces;
using Application.Interfaces.Context;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.Offer;

namespace Application.Feature.Offer.Queries;

public sealed record GetOffersByFilterQuery(OfferFilterDTO Filter) : IRequest<List<ListOfferCalcInfo>>;

public sealed class GetOffersByFilterQueryHandler(IShardingSingleDbContext _dataAccess)
    : IRequestHandler<GetOffersByFilterQuery, List<ListOfferCalcInfo>>
{
    public async Task<List<ListOfferCalcInfo>> Handle(GetOffersByFilterQuery request, CancellationToken cancellationToken)
    {
        var f = request.Filter;

        IQueryable<OfferEntity> result = _dataAccess.Offers.AsQueryable();

        if (f.CalculationID > 0)
            result = result.Where(x => x.Resource.Task.CalculationId == f.CalculationID);
        else if (f.ProjectID.HasValue)
            result = result.Where(x => x.Resource.Task.Calculation.ProjectId == f.ProjectID);
        else if (f.FolderID.HasValue)
            result = result.Where(x => x.Resource.Task.Calculation.Project.FolderId == f.FolderID);

        if (f.ResType.HasValue)
            result = result.Where(x => x.Resource.ResType == f.ResType);
        if (f.ResourceTypeId.HasValue)
            result = result.Where(x => x.Resource.ResourceTypeId == f.ResourceTypeId);
        if (f.ResourceSortId.HasValue)
            result = result.Where(x => x.Resource.ResourceSortId == f.ResourceSortId);

        if (f.OrganisationId.HasValue)
            result = result.Where(x => x.OrganisationId == f.OrganisationId);

        return await result
            .OrderByDescending(x => x)
            .Select(x => new ListOfferCalcInfo
            {
                Id = x.Id,
                BaseCost = x.Metadata.BaseCost,
                SubCategory = x.Organisation.OrganisationCategory.Name,
                Category = x.Organisation.OrganisationCategory.ParentCategory.Name,
                Comment = x.Comment,
                Cost = x.Metadata.Cost,
                Date = x.Date,
                Organisation = x.Organisation.Name,
                ResName = x.Resource.Name,
                TaskCode = x.Resource.Task.Metadata.Code,
                TaskName = x.Resource.Task.Name,
                CalcCode = x.Resource.Task.Calculation.Code,
                CalcName = x.Resource.Task.Calculation.Name,
            })
            .ToListAsync(cancellationToken);
    }
}
