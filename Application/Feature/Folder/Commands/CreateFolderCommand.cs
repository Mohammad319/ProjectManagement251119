using Application.Interfaces;
using ProjectManagement.Shared.DTO.Folder;

namespace Application.Feature.Project.Folder.Commands
{
    public sealed record CreateFolderCommand(PostFolderDTO Dto, int UserId, int DepartmentId) : IRequest<Guid>;

    public sealed class CreateFolderCommandHandler(IFolderService service)
        : IRequestHandler<CreateFolderCommand, Guid>
    {
        public Task<Guid> Handle(CreateFolderCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, request.UserId, request.DepartmentId, ct);
    }
    public sealed record UpdateFolderCommand(Guid Id, PostFolderDTO Dto, int UserId, int? DepartmentId) : IRequest<bool>;

    public sealed class UpdateFolderCommandHandler(IFolderService service)
        : IRequestHandler<UpdateFolderCommand, bool>
    {
        public Task<bool> Handle(UpdateFolderCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, request.UserId, request.DepartmentId, ct);
    }
    public sealed record NewOrderFolderCommand(Guid Id, int NewOrder) : IRequest<bool>;

    public sealed class NewOrderFolderCommandHandler(IFolderService service)
        : IRequestHandler<NewOrderFolderCommand, bool>
    {
        public Task<bool> Handle(NewOrderFolderCommand request, CancellationToken ct)
            => service.UpdateOrderAsync(request.Id, request.NewOrder, ct);
    }

    public sealed record DeleteFolderCommand(Guid Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public sealed class DeleteFolderCommandHandler(IFolderService service)
        : IRequestHandler<DeleteFolderCommand, bool>
    {
        public Task<bool> Handle(DeleteFolderCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, request.UserId, request.DepartmentId, ct);
    }

}
