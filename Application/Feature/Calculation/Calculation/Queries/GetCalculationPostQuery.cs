using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Queries;

public sealed record GetCalculationPostQuery(int Id) : IRequest<PostCalculationDTO>;

public sealed class GetCalculationPostQueryHandler(IShardingSingleDbContext _context)
    : IRequestHandler<GetCalculationPostQuery, PostCalculationDTO>
{
    public async Task<PostCalculationDTO> Handle(GetCalculationPostQuery request, CancellationToken cancellationToken)
    {
        return await _context.Calculation
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new PostCalculationDTO
            {
                TenderQA = x.TenderQA,
                TenderDeadline = x.TenderDeadline,
                CompensationId = x.CompensationId,
                ContractId = x.ContractId,
                Procurement = x.Procurement,
                ProcurementMethodsId = x.ProcurementMethodsId,
                Name = x.Name,
                Tax = x.Tax,
                TypeId = x.TypeId,
                IsPrivate = x.IsPrivate,
                Code = x.Code,
                OrganisationId = x.OrganisationId,
                EndDate = x.EndDate,
                StatusId = x.StatusId,
                IsVisible = x.IsVisible,
                StartDate = x.StartDate,
                TimeMonth = x.Data.TimeMonth,
                Order = x.Order,
                ClientsManager = x.Data.ClientsManager,
                ContactPerson = x.Data.ContactPerson,
                Contacts = x.Data.Contacts,
                Address = x.Data.Address,
                DecisionDate = x.DecisionDate,
                Designer = x.Data.Designer,
                Developer = x.Data.Developer,
                Income = x.Data.Income,
                Supervisor = x.Data.Supervisor,
                PublicationDate = x.PublicationDate,
                HourlyPrice = x.HourlyPriceFactorData.HourlyPrice,
                Maps = x.Data.Maps,
                Notes = x.Data.Notes,
                Inspector = x.Data.Inspector,
                OverviewInfo = x.Data.OverviewInfo,
                Responsibles = x.Data.Responsibles,
                Priority = x.Data.Priority
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
