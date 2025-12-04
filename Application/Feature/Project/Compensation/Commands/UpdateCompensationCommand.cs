using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Compensation.Commands
{
    public sealed record UpdateCompensationCommand(PostCompensationDTO Dto, int Id) : IRequest<bool>;
    public class UpdateCompensationCommandHandler(IShardingSingleDbContext postRepository) : IRequestHandler<UpdateCompensationCommand, bool>
    {
        public async Task<bool> Handle(UpdateCompensationCommand request, CancellationToken cancellationToken)
        {
            var _ProjectCompensation = await postRepository.Compensation.FindAsync(request.Id, cancellationToken);
            if (_ProjectCompensation != null)
            {
                _ProjectCompensation.Name = request.Dto.Name;
                _ProjectCompensation.IsVisible = request.Dto.IsVisible;
                await postRepository.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}
