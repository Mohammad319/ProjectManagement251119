using Application.Interfaces;
using Domain.Entities.Project;
using ProjectManagement.Shared.DTO.General;
using System;
using System.Linq.Expressions;

namespace Application.Feature.Project.Type.Queries
{
    public sealed record CalcProjectFormControllerQuery() : IRequest<ResourceFormDTO>;
    public class CalcProjectFormControllerQueryHandler(IShardingSingleDbContext context) : IRequestHandler<CalcProjectFormControllerQuery, ResourceFormDTO>
    {
        public async Task<ResourceFormDTO> Handle(CalcProjectFormControllerQuery query, CancellationToken cancellationToken)
        {
            ResourceFormDTO projectCalcFormDTO = new();
    //        projectCalcFormDTO.Status = await context.CalculationStatus.Where(x => x.IsVisible == true).AsNoTracking()
    //            .Select(x => new ListDTO{Id = x.Id,Name = x.Name,}).ToListAsync(cancellationToken);
    //        projectCalcFormDTO.Types = await context.CalcProjectType.Where(x => x.IsVisible == true).AsNoTracking()
    //.Select(x => new ListDTO { Id = x.Id, Name = x.Name, }).ToListAsync(cancellationToken);

    //        projectCalcFormDTO.Methods = await context.ProcurementMethod.Where(x => x.IsVisible == true).AsNoTracking()
    //            .Select(x => new ListDTO{Id = x.Id,Name = x.Name,}).ToListAsync(cancellationToken);

    //        projectCalcFormDTO.Compensations = await context.Compensations.Where(x => x.IsVisible == true).AsNoTracking()
    //            .Select(x => new ListDTO { Id = x.Id, Name = x.Name, }).ToListAsync(cancellationToken);

    //        projectCalcFormDTO.Contracts = await context.Contracts.Where(x => x.IsVisible == true).AsNoTracking()
    //            .Select(x => new ListDTO { Id = x.Id, Name = x.Name, }).ToListAsync(cancellationToken);

    //        projectCalcFormDTO.OrganisationCategories = await context.OrganisationCategory.AsNoTracking()
    //            .Select(x => new ListDTO { Id = x.Id, Name = x.Name, }).ToListAsync(cancellationToken);
    //        projectCalcFormDTO.Organisations = await context.Organisation.Where(x => x.IsVisible == true).AsNoTracking()
    //            .Select(x => new ListDTO { Id = x.Id, Name = x.Name, }).ToListAsync(cancellationToken);

            return projectCalcFormDTO;
        }
    }
}
