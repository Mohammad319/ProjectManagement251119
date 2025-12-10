using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Project.Project.Commands
{
    public sealed record DeleteProjectCommand(Guid Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public class DeleteProjectCommandHandler(IShardingSingleDbContext postRepository) : IRequestHandler<DeleteProjectCommand, bool>
    {
        public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await postRepository.Projects.Include(x=>x.Folder)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (project == null || (project.Folder.DepartmentId != request.DepartmentId && request.DepartmentId != null))
                return false;

            postRepository.Projects.Remove(project);
            await postRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
