using Application.Interfaces;
using System;

namespace Application.Feature.Project.Folder.Commands
{
    public sealed record DeleteFolderCommand(Guid Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public class DeleteFolderCommandHandler(IShardingSingleDbContext postRepository) : IRequestHandler<DeleteFolderCommand, bool>
    {
        public async Task<bool> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
        {
            var _folder = await postRepository.Folder.FindAsync(request.Id, cancellationToken);
            if (_folder != null && (!request.DepartmentId.HasValue || _folder.DepartmentId == request.DepartmentId))
            {
                bool hasAnyProject = await postRepository.Project.AnyAsync(x => x.FolderId == request.Id, cancellationToken);

                if (hasAnyProject || _folder.UserId != request.UserId)
                    return false;

                postRepository.Folder.Remove(_folder);
                await postRepository.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}
