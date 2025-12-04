using Application.Interfaces;

namespace Application.Feature.Identity.Department.Commands
{
    public sealed record DeleteDepartmentCommand(int Id) : IRequest<bool>;
    public class DeleteDepartmentCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<DeleteDepartmentCommand, bool>
    {
        public async Task<bool> Handle(DeleteDepartmentCommand command, CancellationToken cancellationToken)
        {
            var group = await dataAccess.Department.FindAsync(command.Id);
            if (group == null || await dataAccess.Project.AnyAsync(x => x.Folder.DepartmentId == command.Id, cancellationToken: cancellationToken))
                return false;
            dataAccess.Department.Remove(group);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
