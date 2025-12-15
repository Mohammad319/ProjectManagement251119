using Application.Interfaces;
using ProjectManagement.Shared.Base.Users;

namespace Application.Feature.Identity.Department.Commands
{
    public sealed record CreateDepartmentCommand(DepartmentBase Dto) : IRequest<int>;
    public sealed record UpdateDepartmentCommand(int Id, DepartmentBase Dto) : IRequest<bool>;
    public sealed record DeleteDepartmentCommand(int Id) : IRequest<bool>;

    public sealed class CreateDepartmentCommandHandler(IDepartmentService service)
        : IRequestHandler<CreateDepartmentCommand, int>
    {
        public Task<int> Handle(CreateDepartmentCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    public sealed class UpdateDepartmentCommandHandler(IDepartmentService service)
        : IRequestHandler<UpdateDepartmentCommand, bool>
    {
        public Task<bool> Handle(UpdateDepartmentCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    public sealed class DeleteDepartmentCommandHandler(IDepartmentService service)
        : IRequestHandler<DeleteDepartmentCommand, bool>
    {
        public Task<bool> Handle(DeleteDepartmentCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }
}
