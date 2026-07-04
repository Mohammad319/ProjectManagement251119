using Application.Interfaces;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.Organisation.Commands
{
    // -------------------------
    // CREATE
    // -------------------------
    public sealed record CreateOrganisationCommand(PostOrganisationDTO Dto) : IRequest<int>;

    public sealed class CreateOrganisationCommandHandler(IOrganisationService service)
        : IRequestHandler<CreateOrganisationCommand, int>
    {
        public Task<int> Handle(CreateOrganisationCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    // -------------------------
    // UPDATE
    // -------------------------
    public sealed record UpdateOrganisationCommand(PostOrganisationDTO Dto, int Id) : IRequest<bool>;

    public sealed class UpdateOrganisationCommandHandler(IOrganisationService service)
        : IRequestHandler<UpdateOrganisationCommand, bool>
    {
        public Task<bool> Handle(UpdateOrganisationCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Id, request.Dto, ct);
    }

    // -------------------------
    // DELETE
    // -------------------------
    public sealed record DeleteOrganisationCommand(int Id) : IRequest<bool>;

    public sealed class DeleteOrganisationCommandHandler(IOrganisationService service)
        : IRequestHandler<DeleteOrganisationCommand, bool>
    {
        public Task<bool> Handle(DeleteOrganisationCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }

    // -------------------------
    // ARCHIVE / RESTORE (arkivera istället för ta bort)
    // -------------------------
    public sealed record ArchiveOrganisationCommand(int Id, bool Archive) : IRequest<bool>;

    public sealed class ArchiveOrganisationCommandHandler(IOrganisationService service)
        : IRequestHandler<ArchiveOrganisationCommand, bool>
    {
        public Task<bool> Handle(ArchiveOrganisationCommand request, CancellationToken ct)
            => service.SetVisibilityAsync(request.Id, !request.Archive, ct);
    }
}
