using Application.Interfaces;
using Application.Interfaces.Context;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.DTO.ResourceType;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Project.Status.Commands
{
    public sealed record UpdateStatusCommand(PostStatusDTO Dto, int Id) : IRequest<bool>;
        public class UpdateStatusCommandHandler(IShardingSingleDbContext _context) : IRequestHandler<UpdateStatusCommand, bool>
        {
            public async Task<bool> Handle(UpdateStatusCommand request, CancellationToken cancellationToken)
            {
                var _status = await _context.CalculationStatus.FindAsync(request.Id, cancellationToken);
                if (_status != null)
                {
                    _status.Name = request.Dto.Name;
                    _status.Color = request.Dto.Color;
                    _status.IsVisible = request.Dto.IsVisible;

                    //_context.Status.Update(_status);
                    await _context.SaveChangesAsync(cancellationToken);
                    return true;
                }
                return false;
            }
        }
}
