using Application.Interfaces;
using Application.Interfaces.Context;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Shared.DTO.General;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Feature.Project.Type.Queries
{
    public class GetProjectCalcConfig : IRequest<GetProjectCalcConfigDTO>
    {
        public int TypeObj;

        public int Methods;
        public int Contracts;
        public int Compensations;
        public int Types;
        public int Statuses;
        public int OrgId;

        public class GetProjectCalcConfigHandler(IShardingSingleDbContext context) : IRequestHandler<GetProjectCalcConfig, GetProjectCalcConfigDTO>
        {
            public async Task<GetProjectCalcConfigDTO> Handle(GetProjectCalcConfig query, CancellationToken cancellationToken)
            {
                GetProjectCalcConfigDTO config = new()
                {
                    Types = await context.CalcProjectType.Where(x=> x.IsVisible || x.Id == query.Types).AsNoTracking().Select(x => new ListDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                    }).ToListAsync(cancellationToken: cancellationToken),
                    Methods = await context.ProcurementMethod.AsNoTracking().Where(x => x.IsVisible || x.Id == query.Methods).Select(x => new ListDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                    }).ToListAsync(cancellationToken: cancellationToken),
                    Contracts = await context.Contracts.AsNoTracking().Where(x => x.IsVisible || x.Id == query.Contracts).Select(x => new ListDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                    }).ToListAsync(cancellationToken: cancellationToken),
                    Compensations = await context.Compensations.AsNoTracking().Where(x => x.IsVisible || x.Id == query.Compensations).Select(x => new ListDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                    }).ToListAsync(cancellationToken: cancellationToken),
                    Organisation = await context.Organisation.AsNoTracking().Where(x => x.IsVisible || x.Id == query.OrgId).Select(x => new ListDTO
                    {
                        Id = x.Id,
                        Name = x.Name,
                    }).ToListAsync(cancellationToken: cancellationToken),

                };
                if (query.TypeObj == 1) config.Statuses = await context.CalculationStatus.AsNoTracking().Where(x => x.IsVisible || x.Id == query.Statuses).Select(x => new ListDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                }).ToListAsync(cancellationToken: cancellationToken);
                return config;
            }
        }
    }
}
