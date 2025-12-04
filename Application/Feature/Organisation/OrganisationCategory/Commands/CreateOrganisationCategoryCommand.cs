using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Organisation;
using ProjectManagement.Shared.DTO.Organisation;

namespace Application.Feature.Organisation.OrganisationCategory.Commands
{
    public sealed record CreateOrganisationCategoryCommand(PostOrganisationCategoryDTO Dto) : IRequest<int>;

    public class CreateOrganisationCategoryCommandHandler(IShardingSingleDbContext dataAccess, IMapper mapper) : IRequestHandler<CreateOrganisationCategoryCommand, int>
    {
        public async Task<int> Handle(CreateOrganisationCategoryCommand request, CancellationToken cancellationToken)
        {
            OrganisationCategoryEntity entity = mapper.Map<OrganisationCategoryEntity>(request.Dto);
            if (entity.CategoryId.HasValue && entity.CategoryId > 0)
            {
                var parent = dataAccess.OrganisationCategory.Find(entity.CategoryId);
                if (parent.CategoryId > 0)
                    return 0;
            }
            dataAccess.OrganisationCategory.Add(entity);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
