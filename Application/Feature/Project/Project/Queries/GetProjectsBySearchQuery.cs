using Application.Interfaces;
using ProjectManagement.Shared.DTO.Project;

namespace Application.Feature.Project.Project.Queries
{
    public sealed record GetProjectsBySearchQuery(ProjectFilter dto, int UserId, int DepartmentId) : IRequest<IEnumerable<SearchProjectDTO>>;
    public class GetProjectsBySearchQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetProjectsBySearchQuery, IEnumerable<SearchProjectDTO>>
    {
        public async Task<IEnumerable<SearchProjectDTO>> Handle(GetProjectsBySearchQuery query, CancellationToken cancellationToken)
        {
            var projects = context.Project.Where(x => x.Folder.DepartmentId == query.DepartmentId || x.UserId == query.UserId
            || (x.Calculations.SelectMany(c => c.SharesCalc)
            .Any(s => s.UserId == query.UserId || s.DepartmentId == query.DepartmentId))).AsQueryable();
            projects = projects.Where(x => x.IsVisible == query.dto.IsVisible);
            if (query.dto.CustomerId.HasValue)
                projects = projects.Where(x => x.OrganisationId == query.dto.CustomerId.Value);
            if (query.dto.FolderId.HasValue)
            {
                projects = projects.Where(x => x.FolderId == query.dto.FolderId.Value);
            }
            if (query.dto.StartDate1.HasValue)
                projects = projects.Where(x => x.StartDate >= query.dto.StartDate1);
            if (query.dto.StartDate2.HasValue)
                projects = projects.Where(x => x.StartDate <= query.dto.StartDate2.Value.AddDays(1));
            if (query.dto.EndDate1.HasValue)
                projects = projects.Where(x => x.EndDate >= query.dto.EndDate1);
            if (query.dto.EndDate2.HasValue)
                projects = projects.Where(x => x.EndDate <= query.dto.EndDate2.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(query.dto.Code))
                projects = projects.Where(x => x.Code == query.dto.Code);
            if (!string.IsNullOrWhiteSpace(query.dto.Name))
            {
                projects = query.dto.NameOperator switch
                {
                    "==" => projects = projects.Where(x => x.Name == query.dto.Name),
                    "!=" => projects = projects.Where(x => x.Name != query.dto.Name),
                    "%var" => projects = projects.Where(x => x.Name.StartsWith(query.dto.Name)),
                    "var%" => projects = projects.Where(x => x.Name.EndsWith(query.dto.Name)),

                    _ => projects = projects.Where(x => x.Name.Contains(query.dto.Name))
                };
            }

            //projects = query.dto.SortValue switch
            //{
            //    Sort.Code => projects.OrderBy(x => x.Code),
            //    Sort.StPro => projects.OrderBy(x => x.StartDate),
            //    Sort.StProDes => projects.OrderByDescending(x => x.StartDate),
            //    Sort.EnPro => projects.OrderBy(x => x.EndDate),
            //    Sort.EnProDes => projects.OrderByDescending(x => x.EndDate),
            //    _ => projects.OrderByDescending(x => x),
            //};
            return await projects.Skip(query.dto.Skip).Take(15)
                    .AsNoTracking().Select(x => new SearchProjectDTO()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Code = x.Code,
                        EndDate = x.EndDate,
                        StartDate = x.StartDate,
                        Order = x.Order,
                        FolderId = x.FolderId,
                    }).ToListAsync(cancellationToken);
        }
    }
}
