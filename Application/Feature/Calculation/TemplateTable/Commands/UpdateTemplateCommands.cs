using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.TemplateTable.Commands
{
    public sealed record UpdateTemplateCommands(TemplateListPostDTO dto, int Id, int? DepartmentId) : IRequest<bool>;

    public class UpdateTemplateCommandsHandler(IShardingSingleDbContext context) : IRequestHandler<UpdateTemplateCommands, bool>
    {
        public async Task<bool> Handle(UpdateTemplateCommands command, CancellationToken cancellationToken)
        {
            TemplateEntity temp = await context.Templates.FirstOrDefaultAsync(x => x.Id == command.Id);
            if (temp == null) return false;
            temp.DepartmentId = command.DepartmentId;
            temp.Name = command.dto.Name;
            temp.IsVisible = command.dto.Active;
            temp.Metadata = new();
            command.dto.CopyPropertiesTo(temp.Metadata);
            context.Templates.Update(temp);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
