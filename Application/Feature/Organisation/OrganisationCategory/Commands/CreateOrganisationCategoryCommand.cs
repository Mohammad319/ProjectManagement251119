using Application.Interfaces;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.OrganisationCategory.Commands
{
    public sealed record CreateOrganisationCategoryCommand(PostOrganisationCategoryDTO Dto) : IRequest<int>;
    public sealed record UpdateOrganisationCategoryCommand(PutOrganisationCategoryDTO Dto) : IRequest<bool>;
    public sealed record DeleteOrganisationCategoryCommand(int Id) : IRequest<bool>;

    public sealed class CreateOrganisationCategoryCommandHandler(IOrganisationCategoryService service)
        : IRequestHandler<CreateOrganisationCategoryCommand, int>
    {
        public Task<int> Handle(CreateOrganisationCategoryCommand request, CancellationToken ct)
            => service.CreateAsync(request.Dto, ct);
    }

    public sealed class UpdateOrganisationCategoryCommandHandler(IOrganisationCategoryService service)
        : IRequestHandler<UpdateOrganisationCategoryCommand, bool>
    {
        public Task<bool> Handle(UpdateOrganisationCategoryCommand request, CancellationToken ct)
            => service.UpdateAsync(request.Dto, ct);
    }

    public sealed class DeleteOrganisationCategoryCommandHandler(IOrganisationCategoryService service)
        : IRequestHandler<DeleteOrganisationCategoryCommand, bool>
    {
        public Task<bool> Handle(DeleteOrganisationCategoryCommand request, CancellationToken ct)
            => service.DeleteAsync(request.Id, ct);
    }
}
