using Application.Interfaces;
using Domain.Entities.Application;
using System;

namespace Application.Feature.Application.Commands
{
    public sealed record CreateApplicationCommand(ApplicationEntity Dto) : IRequest<int>;

    public class CreateApplicationCommandHandler(IShardingSingleDbContext context) : IRequestHandler<CreateApplicationCommand, int>
    {
        public async Task<int> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
        {
            ApplicationEntity template = new()
            {
                DepartmentId = request.Dto.DepartmentId,
                IsVisible = request.Dto.IsVisible,
                //Description = request.Dto.Description,
                LastUpdate = DateTime.Now,
                Name = request.Dto.Name,
                UserId = request.Dto.UserId,
                Data = request.Dto.Data,
                //DataStr = JsonSerializer.Serialize(request.Dto.Rows),
            };

            context.Applications.Add(template);
            await context.SaveChangesAsync(cancellationToken);
            return template.Id;
        }
    }
}
