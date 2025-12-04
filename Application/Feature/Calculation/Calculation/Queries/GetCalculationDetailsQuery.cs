using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Queries;

public sealed record GetCalculationDetailsQuery(int Id) : IRequest<CalculationDetailsDTO>;

public sealed class GetCalculationDetailsQueryHandler(IShardingSingleDbContext context)
    : IRequestHandler<GetCalculationDetailsQuery, CalculationDetailsDTO>
{
    public async Task<CalculationDetailsDTO> Handle(GetCalculationDetailsQuery request, CancellationToken cancellationToken)
    {
        return await context.Calculation
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new CalculationDetailsDTO
            {
                TenderQA = x.TenderQA,
                TenderDeadline = x.TenderDeadline,
                Compensation = x.Compensation.Name,
                Contract = x.Contract.Name,
                Priority = x.Data.Priority,
                Procurement = x.Procurement,
                ProcurementMethods = x.ProcurementMethods.Name,
                Name = x.Name,
                Tax = x.Tax,
                TimeMonth = x.Data.TimeMonth,
                Type = x.Type.Name,
                Order = x.Order,
                ClientsManager = x.Data.ClientsManager,
                Code = x.Code,
                ContactPerson = x.Data.ContactPerson,
                Contacts = x.Data.Contacts,
                Address = x.Data.Address,
                DecisionDate = x.DecisionDate,
                Designer = x.Data.Designer,
                Developer = x.Data.Developer,
                EndDate = x.EndDate,
                Income = x.Data.Income,
                StartDate = x.StartDate,
                Supervisor = x.Data.Supervisor,
                PublicationDate = x.PublicationDate,
                HourlyPrice = x.HourlyPriceFactorData.HourlyPrice,
                Maps = x.Data.Maps,
                Notes = x.Data.Notes,
                Inspector = x.Data.Inspector,
                OverviewInfo = x.Data.OverviewInfo,
                Responsibles = x.Data.Responsibles
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
