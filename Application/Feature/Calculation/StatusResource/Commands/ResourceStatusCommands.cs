using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.StatusResource.Commands
{
    // CREATE
    public sealed record CreateResourceStatusCommand(PostTaskStatusDTO Dto) : IRequest<int>;

    public sealed class CreateTaskStatusCommandHandler(ILookupStatusCommandService<StatusResourcesEntity> service)
                : IRequestHandler<CreateResourceStatusCommand, int>
    {
        public Task<int> Handle(CreateResourceStatusCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    // UPDATE
    public sealed record UpdateResourceStatusCommand(int Id, PostTaskStatusDTO Dto) : IRequest<bool>;

    public sealed class UpdateTaskStatusCommandHandler(ILookupStatusCommandService<StatusResourcesEntity> service)
                : IRequestHandler<UpdateResourceStatusCommand, bool>
    {
        public Task<bool> Handle(UpdateResourceStatusCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    // DELETE
    public sealed record DeleteResourceStatusCommand(int Id) : IRequest<bool>;

    public sealed class DeleteResourceStatusCommandHandler(ILookupStatusCommandService<StatusResourcesEntity> service)
                : IRequestHandler<DeleteResourceStatusCommand, bool>
    {
        public Task<bool> Handle(DeleteResourceStatusCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }
}
