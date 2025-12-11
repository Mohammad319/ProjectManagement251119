using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Queries;

public sealed record GetCalculationDetailsQuery(int Id) : IRequest<CalculationDetailsDTO>;

public sealed class GetCalculationDetailsQueryHandler(ICalculationQueryService service)
        : IRequestHandler<GetCalculationDetailsQuery, CalculationDetailsDTO>
{
    public Task<CalculationDetailsDTO?> Handle(GetCalculationDetailsQuery request, CancellationToken ct)
        => service.GetDetailsAsync(request.Id, ct);
}
