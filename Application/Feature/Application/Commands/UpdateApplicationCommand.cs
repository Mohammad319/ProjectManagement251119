using Application.Interfaces;
using Domain.Entities.Application;
using System;

namespace Application.Feature.Application.Commands
{
    public sealed record UpdateApplicationCommand(ApplicationEntity dto) : IRequest<bool>;

    public class UpdateApplicationCommandHandler(IShardingSingleDbContext _dataAccess) : IRequestHandler<UpdateApplicationCommand, bool>
    {
        public async Task<bool> Handle(UpdateApplicationCommand request, CancellationToken cancellationToken)
        {
            var app = await _dataAccess.Application.FindAsync(request.dto.Id);
            if (app == null)
                return false;
            app.LastUpdate = DateTime.Now;
            app.IsVisible = request.dto.IsVisible;
            app.UserId = request.dto.UserId;
            app.Name = request.dto.Name;
            app.Data = request.dto.Data;

            await _dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
