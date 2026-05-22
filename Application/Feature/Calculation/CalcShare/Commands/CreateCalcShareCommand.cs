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

    public sealed record DeleteCalcShareCommand(int Id, int? DepartmentId, int UserId) : IRequest<bool>;

    public sealed class DeleteCalcShareCommandHandler(IShareCalcService service)
        : IRequestHandler<DeleteCalcShareCommand, bool>
    {
        public Task<bool> Handle(DeleteCalcShareCommand request, CancellationToken ct)
            => request.DepartmentId.HasValue
                ? service.DeleteAsync(request.Id, request.DepartmentId.Value, request.UserId, ct)
                : System.Threading.Tasks.Task.FromResult(false);
    }
}
