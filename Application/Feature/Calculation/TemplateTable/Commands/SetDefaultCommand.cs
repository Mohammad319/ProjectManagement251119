using Application.Interfaces;
using Domain.Entities.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;

namespace Application.Feature.Calculation.TemplateTable.Commands
{
    public class SetDefaultCommand : IRequest<TemplateModelDTO>
    {
        public int CalculationId { get; set; }
        public int? TemplateId { get; set; }
        //public int TemplatePrintId { get; set; }
        public int? DepartmentId { get; set; }

        public class SetDefaultCommandHandler(IShardingSingleDbContext context) : IRequestHandler<SetDefaultCommand, TemplateModelDTO>
        {
            public async Task<TemplateModelDTO> Handle(SetDefaultCommand command, CancellationToken cancellationToken)
            {
                var calc = context.Calculations.FirstOrDefault(x => x.Id == command.CalculationId);
                if (calc == null) return null;
                if (command.TemplateId == 0)
                    command.TemplateId = null;
                TemplateEntity tempTable = new();

                if (command.TemplateId.HasValue && command.TemplateId > 0)
                {
                    if (command.TemplateId.HasValue && command.TemplateId > 0)
                    {
                        tempTable = await context.Templates.FirstOrDefaultAsync(x => x.Id == command.TemplateId);
                        if (tempTable == null)
                            return null;
                    }

                }
                calc.TemplateId = command.TemplateId;
                context.Calculations.Update(calc);
                await context.SaveChangesAsync(cancellationToken);
                TemplateModelDTO r = new()
                {
                    Name = calc.Name,
                    Currency = tempTable.Metadata.Currency,
                    DateFormat = tempTable.Metadata.DateFormat,
                    NetColor = tempTable.Metadata.NetColor,
                    SSColor = tempTable.Metadata.SSColor,
                    FreezList = tempTable.Metadata.FreezList,
                    MathRound = tempTable.Metadata.MathRound,
                    NetOrder = tempTable.Metadata.NetOrder,
                    NetWidth = tempTable.Metadata.NetWidth,
                    SSOrder = tempTable.Metadata.SSOrder,
                    SSWidth = tempTable.Metadata.SSWidth
                };
                return r;
            }
        }
    }
}
