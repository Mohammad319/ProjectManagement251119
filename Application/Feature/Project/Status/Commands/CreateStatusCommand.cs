using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Status.Commands
{
    public sealed record CreateStatusCommand(PostStatusDTO Dto) : IRequest<int>;
    public class CreateStatusCommandHandler(IShardingSingleDbContext postRepository, IMapper mapper) : IRequestHandler<CreateStatusCommand, int>
    {
        public async Task<int> Handle(CreateStatusCommand request, CancellationToken cancellationToken)
        {
            StatusEntity entity = mapper.Map<StatusEntity>(request.Dto);
            postRepository.CalculationStatus.Add(entity);
            await postRepository.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
