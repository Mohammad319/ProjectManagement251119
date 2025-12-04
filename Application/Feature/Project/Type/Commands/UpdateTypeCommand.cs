using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Type.Commands
{
    public sealed record UpdateTypeCommand(PostTypeDTO Dto, int Id) : IRequest<bool>;
    public class UpdateTypeCommandHandler(IShardingSingleDbContext _context) : IRequestHandler<UpdateTypeCommand, bool>
    {
        public async Task<bool> Handle(UpdateTypeCommand request, CancellationToken cancellationToken)
        {
            var _ProjectType = await _context.CalcProjectType.FindAsync(request.Id, cancellationToken);
            if (_ProjectType != null)
            {
                _ProjectType.Name = request.Dto.Name;
                _ProjectType.IsVisible = request.Dto.IsVisible;

                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}
