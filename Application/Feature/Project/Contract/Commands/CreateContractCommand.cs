using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Contract.Commands
{
    public sealed record CreateContractCommand(PostContractDTO Dto) : IRequest<int>;

    public class CreateContractCommandHandler(IShardingSingleDbContext postRepository, IMapper mapper) : IRequestHandler<CreateContractCommand, int>
    {
        public async Task<int> Handle(CreateContractCommand request, CancellationToken cancellationToken)
        {
            ContractEntity entity = mapper.Map<ContractEntity>(request.Dto);
            postRepository.Contracts.Add(entity);
            await postRepository.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
