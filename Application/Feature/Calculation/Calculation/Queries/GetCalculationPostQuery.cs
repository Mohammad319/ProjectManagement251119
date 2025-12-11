using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Queries;

public sealed record GetCalculationPostQuery(int Id) : IRequest<CalculationPostDTO>;

public sealed class GetCalculationPostQueryHandler(IShardingSingleDbContext _context)
    : IRequestHandler<GetCalculationPostQuery, CalculationPostDTO>
{
    public async Task<CalculationPostDTO> Handle(GetCalculationPostQuery request, CancellationToken cancellationToken)
    {
        return await _context.Calculations
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new CalculationPostDTO
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
                TimeMonth = x.Metadata.TimeMonth,
                Order = x.SortOrder,
                ClientsManager = x.Metadata.ClientsManager,
                ContactPerson = x.Metadata.ContactPerson,
                Contacts = x.Metadata.Contacts,
                Address = x.Metadata.Address,
                DecisionDate = x.DecisionDate,
                Designer = x.Metadata.Designer,
                Developer = x.Metadata.Developer,
                Income = x.Metadata.Income,
                Supervisor = x.Metadata.Supervisor,
                PublicationDate = x.PublicationDate,
                HourlyPrice = x.HourlyPriceFactorData.HourlyPrice,
                Maps = x.Metadata.Maps,
                Notes = x.Metadata.Notes,
                Inspector = x.Metadata.Inspector,
                OverviewInfo = x.Metadata.OverviewInfo,
                Responsibles = x.Metadata.Responsibles,
                Priority = x.Metadata.Priority
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
