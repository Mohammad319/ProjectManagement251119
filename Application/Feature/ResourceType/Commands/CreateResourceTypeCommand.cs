using Application.Interfaces;
using AutoMapper;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Commands
{
    public sealed record CreateResourceTypeCommand(PostResourceTypeDTO Dto) : IRequest<int>;
    public class CreateResourceTypeCommandHandler(IShardingSingleDbContext dataAccess, IMapper mapper) : IRequestHandler<CreateResourceTypeCommand, int>
    {
        public async Task<int> Handle(CreateResourceTypeCommand request, CancellationToken cancellationToken)
        {
            ResourceTypeEntity resourceType = mapper.Map<ResourceTypeEntity>(request.Dto);
            request.Dto.CopyPropertiesTo(resourceType.Metadata);

            dataAccess.ResourceTypes.Add(resourceType);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return resourceType.Id;
        }
    }
}
