using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Folder;
using ProjectManagement.Shared.DTO.Folder;
using System;

namespace Application.Feature.Project.Folder.Commands
{
    public sealed record CreateFolderCommand(PostFolderDTO dto, int UserId, int? DepartmentId) : IRequest<Guid>;

    public class CreateFolderCommandHandler(IShardingSingleDbContext context, IMapper mapper) : IRequestHandler<CreateFolderCommand, Guid>
    {
        public async Task<Guid> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
        {
            if (!request.DepartmentId.HasValue)
                return Guid.Empty;
            FolderEntity folder = mapper.Map<FolderEntity>(request.dto);

            double? max = context.Folder.Where(x => x.DepartmentId == request.DepartmentId.Value || x.UserId == request.UserId).Max(x => (double?)x.SortOrder);
            if (max.HasValue) folder.SortOrder = max.Value + 100;
            else folder.SortOrder = 100;

            folder.DepartmentId = request.DepartmentId.Value;
            folder.UserId = request.UserId;
            context.Folder.Add(folder);
            await context.SaveChangesAsync(cancellationToken);
            return folder.Id;
        }
    }
}
