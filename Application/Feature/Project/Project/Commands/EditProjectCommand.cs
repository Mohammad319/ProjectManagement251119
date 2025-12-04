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
            var project = await _dataAccess.Project.FirstOrDefaultAsync(x =>
            x.Id == request.Id && (request.DepartmentId == null || x.Folder.DepartmentId == request.DepartmentId), cancellationToken);
            if (project == null)
                return false;
            double order = project.Order;
            _mapper.Map(request.Dto, project);
            project.UserId = request.UserId;
            request.Dto.CopyPropertiesTo(project.Data);
            _dataAccess.Project.Update(project);
            await _dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}