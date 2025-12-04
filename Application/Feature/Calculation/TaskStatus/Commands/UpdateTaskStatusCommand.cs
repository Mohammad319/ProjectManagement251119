using Application.Interfaces;
using ProjectManagement.Shared.DTO.Calculation;

namespace Application.Feature.Calculation.TaskStatus.Commands
{
    public sealed record UpdateTaskStatusCommand(int Id, PostTaskStatusDTO dto) : IRequest<bool>;
    public class EditTaskStatusCommandHandler(IShardingSingleDbContext postRepository) : IRequestHandler<UpdateTaskStatusCommand, bool>
    {
        public async Task<bool> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
        {
            var _TaskStatus = await postRepository.TaskStatus.FindAsync(request.Id);
            if (_TaskStatus != null)
            {
                _TaskStatus.Name = request.dto.Name;
                _TaskStatus.IsVisible = request.dto.IsVisible;
                _TaskStatus.Color = request.dto.Color;

                postRepository.TaskStatus.Update(_TaskStatus);
                await postRepository.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}
