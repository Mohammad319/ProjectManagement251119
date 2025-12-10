using Application.Interfaces;
using AutoMapper;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Project.Project.Commands
{
    public sealed record EditProjectCommand(PostProjectDTO Dto,Guid Id, int UserId, int? DepartmentId) : IRequest<bool>;

    public class EditProjectCommandHandler(IShardingSingleDbContext _dataAccess, IMapper _mapper) : IRequestHandler<EditProjectCommand, bool>
    {
        public async Task<bool> Handle(EditProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _dataAccess.Projects.FirstOrDefaultAsync(x =>
            x.Id == request.Id && (request.DepartmentId == null || x.Folder.DepartmentId == request.DepartmentId), cancellationToken);
            if (project == null)
                return false;
            double order = project.SortOrder;
            _mapper.Map(request.Dto, project);
            project.CreatedBy = request.UserId;
            request.Dto.CopyPropertiesTo(project.Metadata);
            _dataAccess.Projects.Update(project);
            await _dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}