using Application.Feature.General;
using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Project.Type.Commands
{
    // CREATE
    public sealed record CreateTypeCommand(PostTaskStatusDTO Dto) : IRequest<int>;

    public sealed class CreateTypeCommandHandler(
        ILookupStatusCommandService<TypeEntity> service)
        : IRequestHandler<CreateTypeCommand, int>
    {
        public Task<int> Handle(CreateTypeCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    // UPDATE
    public sealed record UpdateTypeCommand(int Id, PostTaskStatusDTO Dto) : IRequest<bool>;

    public sealed class UpdateTypeCommandHandler(
        ILookupStatusCommandService<TypeEntity> service)
        : IRequestHandler<UpdateTypeCommand, bool>
    {
        public Task<bool> Handle(UpdateTypeCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    // DELETE
    public sealed record DeleteTypeCommand(int Id) : IRequest<bool>;

    public sealed class DeleteTypeCommandHandler(
        ILookupStatusCommandService<TypeEntity> service)
        : IRequestHandler<DeleteTypeCommand, bool>
    {
        public Task<bool> Handle(DeleteTypeCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }
}
