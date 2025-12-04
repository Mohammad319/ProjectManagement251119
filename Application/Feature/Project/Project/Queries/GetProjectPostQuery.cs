using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectPostQuery(Guid Id) : IRequest<PostProjectDTO>;

    public class GetProjectPostQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetProjectPostQuery, PostProjectDTO>
    {
        public async Task<PostProjectDTO> Handle(GetProjectPostQuery query, CancellationToken cancellationToken)
        {
            var pro = await context.Project
                .Where(x => x.Id == query.Id).AsNoTracking().Select(x => new PostProjectDTO()
                {
                    IsVisible = x.IsVisible,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    TenderDeadline = x.TenderDeadline,
                    Name = x.Name,
                    TenderQA = x.TenderQA,
                    CompensationId = x.CompensationId,
                    ContractId = x.ContractId,
                    OrganisationId = x.OrganisationId.Value,
                    FolderId = x.FolderId,
                    ProcurementMethodsId = x.ProcurementMethodsId,
                    TypeId = x.TypeId,
                    Procurement = x.Data.Procurement,
                    ProjectManager = x.Data.ProjectManager,
                    Notes = x.Data.Notes,
                    ClientsContactPersonTender = x.Data.ClientsContactPersonTender,
                    Address = x.Data.Address,
                    ClientsManager = x.Data.ClientsManager,
                    Code = x.Code,
                    Contacts = x.Data.Contacts,
                    Designer = x.Data.Designer,
                    Developer = x.Data.Developer,
                    Inspector = x.Data.Inspector,
                    OverviewInfoProject = x.Data.OverviewInfoProject,
                    Order = x.Order,
                    Responsibles = x.Data.Responsibles,
                    Supervisor = x.Data.Supervisor,

                })
                .FirstOrDefaultAsync(cancellationToken);
            return pro;
        }
    }
}
