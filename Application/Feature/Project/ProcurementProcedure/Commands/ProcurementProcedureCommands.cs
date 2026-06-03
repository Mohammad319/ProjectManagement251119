using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Project.ProcurementProcedure.Commands
{
    public sealed record CreateProcurementProcedureCommand(PostTaskStatusDTO Dto) : IRequest<int>;

    public sealed class CreateProcurementProcedureCommandHandler(
        ILookupStatusCommandService<ProcurementProcedureEntity> service)
        : IRequestHandler<CreateProcurementProcedureCommand, int>
    {
        public Task<int> Handle(CreateProcurementProcedureCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    public sealed record UpdateProcurementProcedureCommand(int Id, PostTaskStatusDTO Dto) : IRequest<bool>;

    public sealed class UpdateProcurementProcedureCommandHandler(
        ILookupStatusCommandService<ProcurementProcedureEntity> service)
        : IRequestHandler<UpdateProcurementProcedureCommand, bool>
    {
        public Task<bool> Handle(UpdateProcurementProcedureCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    public sealed record DeleteProcurementProcedureCommand(int Id) : IRequest<bool>;

    public sealed class DeleteProcurementProcedureCommandHandler(
        ILookupStatusCommandService<ProcurementProcedureEntity> service)
        : IRequestHandler<DeleteProcurementProcedureCommand, bool>
    {
        public Task<bool> Handle(DeleteProcurementProcedureCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }

    public sealed record MoveProcurementProcedureCommand(int Id, bool MoveUp) : IRequest<bool>;

    public sealed class MoveProcurementProcedureCommandHandler(
        ILookupStatusCommandService<ProcurementProcedureEntity> service)
        : IRequestHandler<MoveProcurementProcedureCommand, bool>
    {
        public Task<bool> Handle(MoveProcurementProcedureCommand request, CancellationToken ct)
            => service.MoveAsync(request.Id, request.MoveUp, ct);
    }
}
