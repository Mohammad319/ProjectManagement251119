using Application.Interfaces;
using System;

namespace Application.Feature.Project.Project.Commands
{
    public sealed record NewOrderProjectCommand(Guid Id, double NewOrder) : IRequest<bool>;
    public class NewOrderProjectCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<NewOrderProjectCommand, bool>
    {
        public async Task<bool> Handle(NewOrderProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await dataAccess.Projects.FindAsync(request.Id, cancellationToken);
            if (project == null)
                return false;

            project.SortOrder = request.NewOrder;
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }

    }
}
