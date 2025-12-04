using Application.Interfaces;

namespace Application.Feature.Project.Type.Commands
{
    public sealed record DeleteTypeCommand(int Id) : IRequest<bool>;

    public class DeleteProjectTypeCommandHandler(IShardingSingleDbContext context) : IRequestHandler<DeleteTypeCommand, bool>
    {
        public async Task<bool> Handle(DeleteTypeCommand request, CancellationToken cancellationToken)
        {
            var _ProjectTyp = await context.CalcProjectType.FindAsync(request.Id, cancellationToken);
            if (_ProjectTyp != null)
            {
                context.CalcProjectType.Remove(_ProjectTyp);
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
    }
}
