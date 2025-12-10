using Application.Interfaces;
using System;

namespace Application.Feature.Project.Folder.Commands
{
    public sealed record DeleteFolderCommand(Guid Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public class DeleteFolderCommandHandler(IShardingSingleDbContext postRepository) : IRequestHandler<DeleteFolderCommand, bool>
    {
        public async Task<bool> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
        {
            var _folder = await postRepository.Folders.FindAsync(request.Id, cancellationToken);
            if (_folder != null && (!request.DepartmentId.HasValue || _folder.DepartmentId == request.DepartmentId))
            {
                bool hasAnyProject = await postRepository.Projects.AnyAsync(x => x.FolderId == request.Id, cancellationToken);

                if (hasAnyProject || _folder.CreatedBy != request.UserId)
                    return false;

                postRepository.Folders.Remove(_folder);
                await postRepository.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}
