using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.TemplateTable.Commands
{
    public sealed record CreateTemplateCommands(TemplateListPostDTO dto, int? DepartmentId) : IRequest<TemplateModelDTO>;
    public class CreateTemplateCommandsHandler(IShardingSingleDbContext context) : IRequestHandler<CreateTemplateCommands, TemplateModelDTO>
    {
        public async Task<TemplateModelDTO> Handle(CreateTemplateCommands command, CancellationToken cancellationToken)
        {
            TemplateEntity entity = new()
            {
                Name = command.dto.Name,
                IsVisible = command.dto.Active,
                DepartmentId = command.DepartmentId,
            };
            command.dto.CopyPropertiesTo(entity.Data);
            context.Template.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            TemplateModelDTO r = new() { Name = entity.Name };
            entity.Data.CopyPropertiesTo(r);

            return r;
        }
    }
}
