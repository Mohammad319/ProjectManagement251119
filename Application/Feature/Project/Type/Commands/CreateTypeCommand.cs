using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Type.Commands
{
    public sealed record CreateTypeCommand(PostTypeDTO Dto) : IRequest<int>;

    public class CreateTypeCommandHandler(IShardingSingleDbContext postRepository, IMapper mapper) : IRequestHandler<CreateTypeCommand, int>
    {
        public async Task<int> Handle(CreateTypeCommand request, CancellationToken cancellationToken)
        {
            TypeEntity entity = mapper.Map<TypeEntity>(request.Dto);
            postRepository.CalcProjectType.Add(entity);
            await postRepository.SaveChangesAsync(cancellationToken);
            return entity.Id;
        }
    }
}
