using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.CalcShare.Commands
{
    public sealed record UpsertCalcShareCommand(ShareCalcUpsertDTO Dto, int UserId, int? DepartmentId) : IRequest<int>;

    public sealed class UpsertCalcShareCommandHandler(IShareCalcService service)
        : IRequestHandler<UpsertCalcShareCommand, int>
    {
        public Task<int> Handle(UpsertCalcShareCommand request, CancellationToken ct)
            => service.UpsertAsync(request.Dto, request.UserId, request.DepartmentId, ct);
    }

    public sealed record DeleteCalcShareCommand(int Id) : IRequest<bool>;

    public sealed class DeleteCalcShareCommandHandler(IShareCalcService service)
        : IRequestHandler<DeleteCalcShareCommand, bool>
    {
        public Task<bool> Handle(DeleteCalcShareCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }
}
