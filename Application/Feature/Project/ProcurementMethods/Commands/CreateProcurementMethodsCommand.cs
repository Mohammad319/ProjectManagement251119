using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.ProcurementMethods.Commands
{
    public sealed record CreateProcurementMethodsCommand(PostProcurementMethodsDTO Dto) : IRequest<int>;
    public class CreateProcurementMethodsCommandHandler(IShardingSingleDbContext dataAccess, IMapper mapper) : IRequestHandler<CreateProcurementMethodsCommand, int>
    {
        public async Task<int> Handle(CreateProcurementMethodsCommand request, CancellationToken cancellationToken)
        {
            ProcurementMethodEntity entity = mapper.Map<ProcurementMethodEntity>(request.Dto);
            dataAccess.ProcurementMethod.Add(entity);
            await dataAccess.SaveChangesAsync();
            return entity.Id;
        }
    }
}
