using Application.Interfaces;
using AutoMapper;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.Calculation.Commands
{
    public sealed record UpdateCalculationCommand(CalculationPostDTO dto, int Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public class UpdateCalculationCommandHandler(ICalculationService service) : IRequestHandler<UpdateCalculationCommand, bool>
    {
        public Task<bool> Handle(UpdateCalculationCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.dto, request.UserId, request.DepartmentId, ct);
    }
}
