using Application.Interfaces;
using Domain.Entities.Application;
using System;

namespace Application.Feature.Application.Commands
{
    public sealed record UpdateCalcAppCommand(ApplicationValuesEntity dto) : IRequest<bool>;
    public class UpdateCalcAppCommandHandler(IShardingSingleDbContext _dataAccess) : IRequestHandler<UpdateCalcAppCommand, bool>
    {
        public async Task<bool> Handle(UpdateCalcAppCommand request, CancellationToken cancellationToken)
        {
            var app = await _dataAccess.ApplicationValues.FindAsync(request.dto.Id);
            if (app == null)
                return false;
            app.LastUpdate = DateTime.Now;
            app.Data = request.dto.Data;
            app.UserId = request.dto.UserId;
            app.Responsible = request.dto.Responsible;
            app.Name = request.dto.Name;
            await _dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
