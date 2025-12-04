using Application.Interfaces;

namespace Application.Feature.Project.ProcurementMethods.Commands
{
    public sealed record DeleteProcurementMethodsCommand(int Id) : IRequest<bool>;

    public class DeleteProcurementMethodsCommandHandler(IShardingSingleDbContext context) : IRequestHandler<DeleteProcurementMethodsCommand, bool>
    {
        public async Task<bool> Handle(DeleteProcurementMethodsCommand request, CancellationToken cancellationToken)
        {
            var _ProjectTyp = await context.ProcurementMethod.FindAsync(request.Id, cancellationToken);
            if (_ProjectTyp != null)
            {
                context.ProcurementMethod.Remove(_ProjectTyp);
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }
    }
}
