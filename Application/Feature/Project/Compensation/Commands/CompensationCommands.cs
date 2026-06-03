using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProcurementMethods.Commands
{
    // CREATE
    public sealed record CreateCompensationCommand(PostTaskStatusDTO Dto) : IRequest<int>;

    public sealed class CreateCompensationCommandHandler(
        ILookupStatusCommandService<CompensationEntity> service)
        : IRequestHandler<CreateCompensationCommand, int>
    {
        public Task<int> Handle(CreateCompensationCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    // UPDATE
    public sealed record UpdateCompensationCommand(int Id, PostTaskStatusDTO Dto) : IRequest<bool>;

    public sealed class UpdateCompensationCommandHandler(
        ILookupStatusCommandService<CompensationEntity> service)
        : IRequestHandler<UpdateCompensationCommand, bool>
    {
        public Task<bool> Handle(UpdateCompensationCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    // DELETE
    public sealed record DeleteCompensationCommand(int Id) : IRequest<bool>;

    public sealed class DeleteCompensationCommandHandler(
        ILookupStatusCommandService<CompensationEntity> service)
        : IRequestHandler<DeleteCompensationCommand, bool>
    {
        public Task<bool> Handle(DeleteCompensationCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }

    // MOVE
    public sealed record MoveCompensationCommand(int Id, bool MoveUp) : IRequest<bool>;

    public sealed class MoveCompensationCommandHandler(
        ILookupStatusCommandService<CompensationEntity> service)
        : IRequestHandler<MoveCompensationCommand, bool>
    {
        public Task<bool> Handle(MoveCompensationCommand request, CancellationToken ct)
            => service.MoveAsync(request.Id, request.MoveUp, ct);
    }
}
