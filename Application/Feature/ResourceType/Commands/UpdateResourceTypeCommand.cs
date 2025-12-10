using Application.Interfaces;
using AutoMapper;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Commands
{
    public sealed record UpdateResourceTypeCommand(PostResourceTypeDTO Dto, int Id) : IRequest<bool>;
    public class UpdateResourceTypeCommandHandler(IShardingSingleDbContext dataAccess, IMapper mapper) : IRequestHandler<UpdateResourceTypeCommand, bool>
    {
        public async Task<bool> Handle(UpdateResourceTypeCommand request, CancellationToken cancellationToken)
        {
            var resourceType = await dataAccess.ResourceTypes.FindAsync(request.Id, cancellationToken);
            if (resourceType == null)
                return false;
            resourceType = mapper.Map(request.Dto, resourceType);
            request.Dto.CopyPropertiesTo(resourceType.Metadata);
            dataAccess.ResourceTypes.Update(resourceType);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
