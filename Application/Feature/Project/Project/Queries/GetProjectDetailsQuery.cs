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
                    Type = x.ProjectType.Name,
                    Created = x.CreatedAt,
                    Organisation = x.Organisation.Name,
                    Folder = x.Folder.Name,
                    TenderDeadline = x.TenderDeadline,
                    LastModified = x.UpdatedAt,
                    Name = x.Name,
                    TenderQA = x.TenderQA,
                    ProcurementMethods = x.ProcurementMethod.Name,
                    Procurement = x.Metadata.Procurement,
                    ProjectManager = x.Metadata.ProjectManager,
                    Notes = x.Metadata.Notes,
                    ClientsContactPersonTender = x.Metadata.ClientsContactPersonTender,
                    Address = x.Metadata.Address,
                    ClientsManager = x.Metadata.ClientsManager,
                    Contacts = x.Metadata.Contacts,
                    Designer = x.Metadata.Designer,
                    Developer = x.Metadata.Developer,
                    Inspector = x.Metadata.Inspector,
                    OverviewInfoProject = x.Metadata.OverviewInfoProject,
                    Order = x.SortOrder,
                    Responsibles = x.Metadata.Responsibles,
                    Supervisor = x.Metadata.Supervisor,
                }).FirstOrDefaultAsync(cancellationToken);
        }
    }
}
