using Application.Interfaces;
using Application.Interfaces.Context;
using AutoMapper;
using Domain.Entities.Folder;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.Project;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Project.Project.Commands
{
    public sealed record CreateProjectCommand(PostProjectDTO Dto, int UserId, int? DepartmentId) : IRequest<Guid>;

    public class CreateProjectCommandHandler(IShardingSingleDbContext _dataAccess, IMapper _mapper) : IRequestHandler<CreateProjectCommand, Guid>
    {
        public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            FolderEntity folder = await _dataAccess.Folder.FindAsync(request.Dto.FolderId, cancellationToken);
            if (folder == null)
                return Guid.Empty;

            if (folder.DepartmentId != request.DepartmentId && request.DepartmentId!= null)
                return Guid.Empty;
            ProjectEntity post = _mapper.Map<ProjectEntity>(request.Dto);
            request.Dto.CopyPropertiesTo(post.Metadata);
            double? max = _dataAccess.Project.Where(x => (request.DepartmentId == null || x.Folder.DepartmentId == request.DepartmentId) || x.UserId == request.UserId)
                .Max(x => (double?)x.SortOrder);
            if (max.HasValue) folder.Order = max.Value + 100;
            else folder.Order = 100;
            post.UserId = request.UserId;

            _dataAccess.Project.Add(post);
            await _dataAccess.SaveChangesAsync(cancellationToken);

            return post.Id;
        }
    }
}
