using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Project.Contract.Commands
{
    // CREATE
    public sealed record CreateContractCommand(PostTaskStatusDTO Dto) : IRequest<int>;

    public sealed class CreateContractCommandHandler(
        ILookupStatusCommandService<ContractEntity> service)
        : IRequestHandler<CreateContractCommand, int>
    {
        public Task<int> Handle(CreateContractCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    // UPDATE
    public sealed record UpdateContractCommand(int Id, PostTaskStatusDTO Dto) : IRequest<bool>;

    public sealed class UpdateContractCommandHandler(
        ILookupStatusCommandService<ContractEntity> service)
        : IRequestHandler<UpdateContractCommand, bool>
    {
        public Task<bool> Handle(UpdateContractCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    // DELETE
    public sealed record DeleteContractCommand(int Id) : IRequest<bool>;

    public sealed class DeleteContractCommandHandler(
        ILookupStatusCommandService<ContractEntity> service)
        : IRequestHandler<DeleteContractCommand, bool>
    {
        public Task<bool> Handle(DeleteContractCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }

    // MOVE
    public sealed record MoveContractCommand(int Id, bool MoveUp) : IRequest<bool>;

    public sealed class MoveContractCommandHandler(
        ILookupStatusCommandService<ContractEntity> service)
        : IRequestHandler<MoveContractCommand, bool>
    {
        public Task<bool> Handle(MoveContractCommand request, CancellationToken ct)
            => service.MoveAsync(request.Id, request.MoveUp, ct);
    }
}
