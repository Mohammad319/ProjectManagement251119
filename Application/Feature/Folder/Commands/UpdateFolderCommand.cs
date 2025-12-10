using Application.Interfaces;
using ProjectManagement.Shared.DTO.Folder;
using System;

namespace Application.Feature.Project.Folder.Commands
{
    public sealed record UpdateFolderCommand(PostFolderDTO Dto, Guid Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public class UpdateFolderCommandHandler(IShardingSingleDbContext context) : IRequestHandler<UpdateFolderCommand, bool>
    {
        public async Task<bool> Handle(UpdateFolderCommand request, CancellationToken cancellationToken)
        {
            var _folder = await context.Folders.FindAsync(request.Id, cancellationToken);
            if (_folder != null && (!request.DepartmentId.HasValue || _folder.DepartmentId == request.DepartmentId.Value))
            {
                _folder.CreatedBy = request.UserId;
                _folder.Name = request.Dto.Name;
                _folder.Color = request.Dto.Color;
                _folder.IsVisible = request.Dto.IsVisible;
                context.Folders.Update(_folder);
                await context.SaveChangesAsync(cancellationToken);
                return true;
            }
            return false;
        }
    }
}
