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
                var calc = context.Calculation.FirstOrDefault(x => x.Id == command.CalculationId);
                if (calc == null) return null;
                if (command.TemplateId == 0)
                    command.TemplateId = null;
                TemplateEntity tempTable = new();

                if (command.TemplateId.HasValue && command.TemplateId > 0)
                {
                    if (command.TemplateId.HasValue && command.TemplateId > 0)
                    {
                        tempTable = await context.Template.FirstOrDefaultAsync(x => x.Id == command.TemplateId);
                        if (tempTable == null)
                            return null;
                    }

                }
                calc.TemplateId = command.TemplateId;
                context.Calculation.Update(calc);
                await context.SaveChangesAsync(cancellationToken);
                TemplateModelDTO r = new()
                {
                    Name = calc.Name,
                    Currency = tempTable.Data.Currency,
                    DateFormat = tempTable.Data.DateFormat,
                    NetColor = tempTable.Data.NetColor,
                    SSColor = tempTable.Data.SSColor,
                    FreezList = tempTable.Data.FreezList,
                    MathRound = tempTable.Data.MathRound,
                    NetOrder = tempTable.Data.NetOrder,
                    NetWidth = tempTable.Data.NetWidth,
                    SSOrder = tempTable.Data.SSOrder,
                    SSWidth = tempTable.Data.SSWidth
                };
                return r;
            }
        }
    }
}
