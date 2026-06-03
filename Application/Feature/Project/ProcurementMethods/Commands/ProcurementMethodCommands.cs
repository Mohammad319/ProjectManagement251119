using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Project.ProcurementMethods.Commands
{
    // CREATE
    public sealed record CreateProcurementMethodCommand(PostTaskStatusDTO Dto) : IRequest<int>;

    public sealed class CreateProcurementMethodCommandHandler(
        ILookupStatusCommandService<ProcurementMethodEntity> service)
        : IRequestHandler<CreateProcurementMethodCommand, int>
    {
        public Task<int> Handle(CreateProcurementMethodCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    // UPDATE
    public sealed record UpdateProcurementMethodCommand(int Id, PostTaskStatusDTO Dto) : IRequest<bool>;

    public sealed class UpdateProcurementMethodCommandHandler(
        ILookupStatusCommandService<ProcurementMethodEntity> service)
        : IRequestHandler<UpdateProcurementMethodCommand, bool>
    {
        public Task<bool> Handle(UpdateProcurementMethodCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    // DELETE
    public sealed record DeleteProcurementMethodCommand(int Id) : IRequest<bool>;

    public sealed class DeleteProcurementMethodCommandHandler(
        ILookupStatusCommandService<ProcurementMethodEntity> service)
        : IRequestHandler<DeleteProcurementMethodCommand, bool>
    {
        public Task<bool> Handle(DeleteProcurementMethodCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }

    // MOVE
    public sealed record MoveProcurementMethodCommand(int Id, bool MoveUp) : IRequest<bool>;

    public sealed class MoveProcurementMethodCommandHandler(
        ILookupStatusCommandService<ProcurementMethodEntity> service)
        : IRequestHandler<MoveProcurementMethodCommand, bool>
    {
        public Task<bool> Handle(MoveProcurementMethodCommand request, CancellationToken ct)
            => service.MoveAsync(request.Id, request.MoveUp, ct);
    }
}
