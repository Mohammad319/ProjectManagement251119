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
                    ProcurementMethodsId = x.ProcurementMethodId,
                    TypeId = x.ProjectTypeId,
                    Procurement = x.Metadata.Procurement,
                    ProjectManager = x.Metadata.ProjectManager,
                    Notes = x.Metadata.Notes,
                    ClientsContactPersonTender = x.Metadata.ClientsContactPersonTender,
                    Address = x.Metadata.Address,
                    ClientsManager = x.Metadata.ClientsManager,
                    Code = x.Code,
                    Contacts = x.Metadata.Contacts,
                    Designer = x.Metadata.Designer,
                    Developer = x.Metadata.Developer,
                    Inspector = x.Metadata.Inspector,
                    OverviewInfoProject = x.Metadata.OverviewInfoProject,
                    Order = x.SortOrder,
                    Responsibles = x.Metadata.Responsibles,
                    Supervisor = x.Metadata.Supervisor,

                })
                .FirstOrDefaultAsync(cancellationToken);
            return pro;
        }
    }
}
