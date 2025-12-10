using Application.Interfaces;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Users;
using System;

namespace Application.Feature.Identity.Department.Commands
{
    public sealed record CreateDepartmentCommand(DepartmentBase dto) : IRequest<int>;
    public class CreateUsersGroupsCommandHandler(IShardingSingleDbContext dataAccess) : IRequestHandler<CreateDepartmentCommand, int>
    {
        public async Task<int> Handle(CreateDepartmentCommand command, CancellationToken cancellationToken)
        {
            var group = new DepartmentEntity() { Name = command.dto.Name, Description = command.dto.Description, CreatedAt = DateTime.Now };
            dataAccess.Department.Add(group);
            await dataAccess.SaveChangesAsync(cancellationToken);
            return group.Id;
        }
    }
}
