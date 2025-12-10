using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Queries;

public sealed record GetCalculationDetailsQuery(int Id) : IRequest<CalculationDetailsDTO>;

public sealed class GetCalculationDetailsQueryHandler(IShardingSingleDbContext context)
    : IRequestHandler<GetCalculationDetailsQuery, CalculationDetailsDTO>
{
    public async Task<CalculationDetailsDTO> Handle(GetCalculationDetailsQuery request, CancellationToken cancellationToken)
    {
        return await context.Calculations
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new CalculationDetailsDTO
            {
                TenderQA = x.TenderQA,
                TenderDeadline = x.TenderDeadline,
                Compensation = x.Compensation.Name,
                Contract = x.Contract.Name,
                Priority = x.Metadata.Priority,
                Procurement = x.Procurement,
                ProcurementMethods = x.ProcurementMethods.Name,
                Name = x.Name,
                Tax = x.Tax,
                TimeMonth = x.Metadata.TimeMonth,
                Type = x.Type.Name,
                Order = x.SortOrder,
                ClientsManager = x.Metadata.ClientsManager,
                Code = x.Code,
                ContactPerson = x.Metadata.ContactPerson,
                Contacts = x.Metadata.Contacts,
                Address = x.Metadata.Address,
                DecisionDate = x.DecisionDate,
                Designer = x.Metadata.Designer,
                Developer = x.Metadata.Developer,
                EndDate = x.EndDate,
                Income = x.Metadata.Income,
                StartDate = x.StartDate,
                Supervisor = x.Metadata.Supervisor,
                PublicationDate = x.PublicationDate,
                HourlyPrice = x.HourlyPriceFactorData.HourlyPrice,
                Maps = x.Metadata.Maps,
                Notes = x.Metadata.Notes,
                Inspector = x.Metadata.Inspector,
                OverviewInfo = x.Metadata.OverviewInfo,
                Responsibles = x.Metadata.Responsibles
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
