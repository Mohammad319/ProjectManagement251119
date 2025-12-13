using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Project.Status.Commands
{
    // CREATE
    public sealed record CreateStatusCommand(PostTaskStatusDTO Dto) : IRequest<int>;

    public sealed class CreateStatusCommandHandler(
        ILookupStatusCommandService<StatusEntity> service)
        : IRequestHandler<CreateStatusCommand, int>
    {
        public Task<int> Handle(CreateStatusCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    // UPDATE
    public sealed record UpdateStatusCommand(int Id, PostTaskStatusDTO Dto) : IRequest<bool>;

    public sealed class UpdateStatusCommandHandler(
        ILookupStatusCommandService<StatusEntity> service)
        : IRequestHandler<UpdateStatusCommand, bool>
    {
        public Task<bool> Handle(UpdateStatusCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    // DELETE
    public sealed record DeleteStatusCommand(int Id) : IRequest<bool>;

    public sealed class DeleteStatusCommandHandler(
        ILookupStatusCommandService<StatusEntity> service)
        : IRequestHandler<DeleteStatusCommand, bool>
    {
        public Task<bool> Handle(DeleteStatusCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }
}
