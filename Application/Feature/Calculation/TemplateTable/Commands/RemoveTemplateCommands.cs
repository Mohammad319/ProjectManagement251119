using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Calculation.TemplateTable.Commands
{
    public sealed record RemoveTemplateCommands(int Id, int? DepartmentId) : IRequest<bool>;
        public class RemoveTemplateCommandsHandler(IShardingSingleDbContext context) : IRequestHandler<RemoveTemplateCommands, bool>
        {
            public async Task<bool> Handle(RemoveTemplateCommands command, CancellationToken cancellationToken)
            {
                var result = await context.Template.FirstOrDefaultAsync(x => x.Id == command.Id);
                var Calcs = await context.Calculation.Where(x => x.TemplateId == command.Id).ToListAsync();
                foreach (var cal in Calcs) cal.TemplateId = null;
                context.Calculation.UpdateRange(Calcs);

                if (result != null || (!command.DepartmentId.HasValue || result.DepartmentId == command.DepartmentId))
                {
                    context.Template.Remove(result);
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
        }
    }
