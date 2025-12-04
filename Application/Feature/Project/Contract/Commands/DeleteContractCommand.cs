using Application.Interfaces;

namespace Application.Feature.Project.Contract.Commands
{
    public sealed record DeleteContractCommand(int Id) : IRequest<bool>;
    public class DeleteContractCommandHandler(IShardingSingleDbContext context) : IRequestHandler<DeleteContractCommand, bool>
    {
        public async Task<bool> Handle(DeleteContractCommand request, CancellationToken cancellationToken)
        {
            var _ProjectTyp = await context.Contract.FindAsync(request.Id, cancellationToken);
            if (_ProjectTyp != null)
            {
                context.Contract.Remove(_ProjectTyp);
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}
