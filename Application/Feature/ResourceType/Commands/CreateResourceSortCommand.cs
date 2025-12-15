using Application.Feature.ResourceType;
using Application.Interfaces;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Commands
{
    // ---------- TYPE ----------
    public sealed record CreateResourceTypeCommand(PostResourceTypeDTO Dto) : IRequest<int>;
    public sealed record UpdateResourceTypeCommand(int Id, PostResourceTypeDTO Dto) : IRequest<bool>;
    public sealed record DeleteResourceTypeCommand(int Id) : IRequest<bool>;

    // ---------- SORT ----------
    public sealed record CreateResourceSortCommand(int ResourceTypeId, PostResourceSortDTO Dto) : IRequest<int>;
    public sealed record UpdateResourceSortCommand(int Id, PostResourceSortDTO Dto) : IRequest<bool>;
    public sealed record DeleteResourceSortCommand(int Id) : IRequest<bool>;

    // ---------------------------
    // Handlers (Thin)
    // ---------------------------

    public sealed class CreateResourceTypeCommandHandler(IResourceTypeService service)
        : IRequestHandler<CreateResourceTypeCommand, int>
    {
        public Task<int> Handle(CreateResourceTypeCommand request, CancellationToken ct)
            => service.CreateTypeAsync(request.Dto, ct);
    }

    public sealed class UpdateResourceTypeCommandHandler(IResourceTypeService service)
        : IRequestHandler<UpdateResourceTypeCommand, bool>
    {
        public Task<bool> Handle(UpdateResourceTypeCommand request, CancellationToken ct)
            => service.UpdateTypeAsync(request.Id, request.Dto, ct);
    }

    public sealed class DeleteResourceTypeCommandHandler(IResourceTypeService service)
        : IRequestHandler<DeleteResourceTypeCommand, bool>
    {
        public Task<bool> Handle(DeleteResourceTypeCommand request, CancellationToken ct)
            => service.DeleteTypeAsync(request.Id, ct);
    }

    public sealed class CreateResourceSortCommandHandler(IResourceTypeService service)
        : IRequestHandler<CreateResourceSortCommand, int>
    {
        public Task<int> Handle(CreateResourceSortCommand request, CancellationToken ct)
            => service.CreateSortAsync(request.ResourceTypeId, request.Dto, ct);
    }

    public sealed class UpdateResourceSortCommandHandler(IResourceTypeService service)
        : IRequestHandler<UpdateResourceSortCommand, bool>
    {
        public Task<bool> Handle(UpdateResourceSortCommand request, CancellationToken ct)
            => service.UpdateSortAsync(request.Id, request.Dto, ct);
    }

    public sealed class DeleteResourceSortCommandHandler(IResourceTypeService service)
        : IRequestHandler<DeleteResourceSortCommand, bool>
    {
        public Task<bool> Handle(DeleteResourceSortCommand request, CancellationToken ct)
            => service.DeleteSortAsync(request.Id, ct);
    }
}
