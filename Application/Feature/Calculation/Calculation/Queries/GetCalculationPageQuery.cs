using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;

// يُفترض وجود واجهات MediatR هنا (IRequest, IRequestHandler)

namespace Application.Feature.Calculation.Calculation.Queries
{
    public sealed record GetCalculationPageQuery(int Id, int UserId, int? DepartmentId)
        : IRequest<CalculationPageDTO>;

    public sealed class GetCalculationPageQueryHandler
        : IRequestHandler<GetCalculationPageQuery, CalculationPageDTO>
    {
        private readonly IShardingSingleDbContext _context;

        public GetCalculationPageQueryHandler(IShardingSingleDbContext context)
            => _context = context;

        public async Task<CalculationPageDTO> Handle(GetCalculationPageQuery query, CancellationToken cancellationToken)
        {
            // 1. جلب بيانات الحساب الأساسية
            var calculationDto = await _context.Calculation
                .AsNoTracking()
                .Where(x => x.Id == query.Id &&
                            (!query.DepartmentId.HasValue || x.Project.Folder.DepartmentId == query.DepartmentId) &&
                            (!x.IsPrivate || x.CreatedBy == query.UserId))
                .Select(x => new CalculationPageDTO
                {
                    Tax = x.Tax,
                    Name = x.Name,
                    OrganisationId = x.OrganisationId,
                    Code = x.Code,
                    TemplateId = x.TemplateId,
                    Factors = x.HourlyPriceFactorData.Factors,
                    QuanityList = x.Metadata.QuanityList,

                    Compensation = x.Compensation != null ? x.Compensation.Name : string.Empty,
                    Customer = x.Organisation != null ? x.Organisation.Name : string.Empty,
                    Contract = x.Contract != null ? x.Contract.Name : string.Empty,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (calculationDto is null)
                return null; // أو throw NotFoundException

            // 2. جلب العروض
            var offers = await _context.Offer
                .AsNoTracking()
                .Where(o => o.Resource.Task.CalculationId == query.Id)
                .Select(o => new
                {
                    ResourceId = o.ResourceId,
                    Offer = new ListOfferDTO
                    {
                        Id = o.Id,
                        BaseCost = o.Data.BaseCost,
                        Cost = o.Data.Cost,
                        Comment = o.Data.Comment,
                        Date = o.Date,
                        OrganisationId = o.OrganisationId,
                        Organisation = o.Organisation != null ? o.Organisation.Name : string.Empty,
                        SubCategory = (o.Organisation != null && o.Organisation.OrganisationCategory != null)
                            ? o.Organisation.OrganisationCategory.Name
                            : string.Empty,
                        Category = (o.Organisation != null && o.Organisation.OrganisationCategory != null && o.Organisation.OrganisationCategory.ParentCategory != null)
                            ? o.Organisation.OrganisationCategory.ParentCategory.Name
                            : string.Empty
                    }
                })
                .ToListAsync(cancellationToken);

            var offersByResource = offers
                .GroupBy(o => o.ResourceId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Offer).ToList()
                );

            // 3. جلب المهام والموارد
            var tasks = await _context.Tasks
                .AsNoTracking()
                .Where(t => t.CalculationId == query.Id)
                .Select(t => new TaskListDTO
                {
                    TaskId = t.ParentTaskId,
                    Id = t.Id,
                    Name = t.Name,
                    OpportunityId = t.OpportunityId,
                    Order = t.SortOrder,
                    StatusId = t.StatusId,
                    Data = t.Metadata,
                    Status = t.Status != null ? t.Status.Name : string.Empty,
                    StatusColor = t.Status != null ? t.Status.Color : string.Empty,
                    Opportunity = t.Opportunity != null ? t.Opportunity.OpportunityType : string.Empty,

                    Resources = t.Resources.Select(r => new ResourceListDTO
                    {
                        Name = r.Name,
                        Active = r.IsActive,
                        Id = r.Id,
                        ResType = r.ResType,
                        ResourceSortId = r.ResourceSortId,
                        ResourceTypeId = r.ResourceTypeId,
                        AccountId = r.AccountId,
                        StatusId = r.StatusId,
                        OfferId = r.PrimaryOfferId,
                        Order = r.SortOrder,
                        OpportunityId = r.OpportunityId,
                        Data = r.Metadata,

                        Opportunity = r.Opportunity != null ? r.Opportunity.OpportunityType : string.Empty,
                        StatusColor = r.Status != null ? r.Status.Color : string.Empty,
                        Status = r.Status != null ? r.Status.Name : string.Empty,
                        Sort = r.ResourceSort != null ? r.ResourceSort.Name : string.Empty,
                        ResName = r.ResourceType != null ? r.ResourceType.Name : string.Empty,
                        Account = r.Account != null ? r.Account.Name : string.Empty,
                        AccountCode = r.Account != null ? r.Account.Code : string.Empty,

                        Offers = new List<ListOfferDTO>()
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            // 4. ربط العروض بالموارد
            foreach (var task in tasks)
            {
                foreach (var resource in task.Resources)
                {
                    if (offersByResource.TryGetValue(resource.Id, out var resourceOffers))
                    {
                        resource.Offers = resourceOffers;
                    }
                }
            }

            calculationDto.Tasks = tasks;

            return calculationDto;
        }
    }

}