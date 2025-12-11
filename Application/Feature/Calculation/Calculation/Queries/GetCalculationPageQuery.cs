using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Offer;

// يُفترض وجود واجهات MediatR هنا (IRequest, IRequestHandler)

namespace Application.Feature.Calculation.Calculation.Queries
{
    public sealed record GetCalculationPageQuery(int Id, int UserId, int? DepartmentId)
        : IRequest<CalculationPageDTO>;

    public sealed class GetCalculationPageQueryHandler(ICalculationQueryService service)
                : IRequestHandler<GetCalculationPageQuery, CalculationPageDTO>
    {
        public Task<CalculationPageDTO?> Handle(GetCalculationPageQuery request, CancellationToken ct)
            => service.GetPageAsync(request.Id, request.UserId, request.DepartmentId, ct);
    }

}