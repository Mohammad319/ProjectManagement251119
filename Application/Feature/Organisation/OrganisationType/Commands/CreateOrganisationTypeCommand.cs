using Application.Interfaces;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.OrganisationType.Commands
{
    public sealed record CreateOrganisationTypeCommand(PostOrganisationTypeDTO Dto) : IRequest<int>;
    public sealed record UpdateOrganisationTypeCommand(PostOrganisationTypeDTO Dto, int Id) : IRequest<bool>;
    public sealed record DeleteOrganisationTypeCommand(int Id) : IRequest<bool>;

    public sealed class CreateOrganisationTypeCommandHandler(IOrganisationTypeService service)
        : IRequestHandler<CreateOrganisationTypeCommand, int>
    {
        public Task<int> Handle(CreateOrganisationTypeCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    public sealed class UpdateOrganisationTypeCommandHandler(IOrganisationTypeService service)
        : IRequestHandler<UpdateOrganisationTypeCommand, bool>
    {
        public Task<bool> Handle(UpdateOrganisationTypeCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    public sealed class DeleteOrganisationTypeCommandHandler(IOrganisationTypeService service)
        : IRequestHandler<DeleteOrganisationTypeCommand, bool>
    {
        public Task<bool> Handle(DeleteOrganisationTypeCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }
}
