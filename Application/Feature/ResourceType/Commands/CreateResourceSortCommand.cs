using Application.Interfaces;
using AutoMapper;
using Domain.Entities.ResourceType;
using ProjectManagement.Shared.DTO.ResourceType;

namespace Application.Feature.Calculation.ResourceType.Commands
{
    public sealed record CreateResourceSortCommand(PostResourceSortDTO Dto) : IRequest<int>;

    public class CreateResourceSortCommandHandler(IShardingSingleDbContext dataAccess, IMapper mapper) : IRequestHandler<CreateResourceSortCommand, int>
    {
        public async Task<int> Handle(CreateResourceSortCommand request, CancellationToken cancellationToken)
        {
            ResourceSortEntity resourceSort = mapper.Map<ResourceSortEntity>(request.Dto);
            request.Dto.CopyPropertiesTo(resourceSort.Data);

            dataAccess.ResourceSort.Add(resourceSort);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return resourceSort.Id;
        }
    }
}
