using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;
using System;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectDetailsQuery(Guid Id) : IRequest<ProjectDetailsDTO>;
    public class GetProjectDetailsQueryHandler(IShardingSingleDbContext _context) : IRequestHandler<GetProjectDetailsQuery, ProjectDetailsDTO>
    {
        public async Task<ProjectDetailsDTO> Handle(GetProjectDetailsQuery query, CancellationToken cancellationToken)
        {
            return await _context.Project.Where(x => x.Id == query.Id)
                .AsNoTracking().Select(x => new ProjectDetailsDTO()
                {
                    IsVisible = x.IsVisible,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Code = x.Code,
                    Compensation = x.Compensation.Name,
                    Contract = x.Contract.Name,
                    Type = x.Type.Name,
                    Created = x.Created,
                    Organisation = x.Organisation.Name,
                    Folder = x.Folder.Name,
                    TenderDeadline = x.TenderDeadline,
                    LastModified = x.LastModified,
                    Name = x.Name,
                    TenderQA = x.TenderQA,
                    ProcurementMethods = x.ProcurementMethods.Name,
                    Procurement = x.Data.Procurement,
                    ProjectManager = x.Data.ProjectManager,
                    Notes = x.Data.Notes,
                    ClientsContactPersonTender = x.Data.ClientsContactPersonTender,
                    Address = x.Data.Address,
                    ClientsManager = x.Data.ClientsManager,
                    Contacts = x.Data.Contacts,
                    Designer = x.Data.Designer,
                    Developer = x.Data.Developer,
                    Inspector = x.Data.Inspector,
                    OverviewInfoProject = x.Data.OverviewInfoProject,
                    Order = x.Order,
                    Responsibles = x.Data.Responsibles,
                    Supervisor = x.Data.Supervisor,
                }).FirstOrDefaultAsync(cancellationToken);
        }
    }
}
