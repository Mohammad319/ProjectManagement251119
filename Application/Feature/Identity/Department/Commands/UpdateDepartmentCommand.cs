using Application.Interfaces;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Users;
using System;

namespace Application.Feature.Identity.Department.Commands
{
    public sealed record UpdateDepartmentCommand(int Id, DepartmentBase dto) : IRequest<bool>;
    public class UpdateUsersGroupsCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<UpdateDepartmentCommand, bool>
    {
        public async Task<bool> Handle(UpdateDepartmentCommand command, CancellationToken cancellationToken)
        {
            var group = new DepartmentEntity() { Id = command.Id, Name = command.dto.Name, Description = command.dto.Description, CreatedAt = DateTime.Now };
            dataAccess.Department.Update(group);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
