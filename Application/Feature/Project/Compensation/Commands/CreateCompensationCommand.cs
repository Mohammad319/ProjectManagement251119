using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProcurementMethods.Commands
{
    public sealed record CreateCompensationCommand(PostCompensationDTO Dto) : IRequest<int>;
    public class CreateCompensationCommandHandler(IShardingSingleDbContext dataAccess, IMapper mapper) : IRequestHandler<CreateCompensationCommand, int>
    {
        public async Task<int> Handle(CreateCompensationCommand request, CancellationToken cancellationToken)
        {
            CompensationEntity entity = mapper.Map<CompensationEntity>(request.Dto);
            dataAccess.Compensations.Add(entity);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
