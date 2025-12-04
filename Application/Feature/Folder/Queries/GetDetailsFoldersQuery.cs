using Application.Interfaces;
using ProjectManagement.Shared.DTO.Folder;
using System;

namespace Application.Feature.Project.Folder.Queries
{
    public sealed record GetDetailsFoldersQuery(Guid Id) : IRequest<DetailsFolderDTO>;
    public class GetDetailsFoldersQueryHandler(IShardingSingleDbContext context) : IRequestHandler<GetDetailsFoldersQuery, DetailsFolderDTO>
    {
        public async Task<DetailsFolderDTO> Handle(GetDetailsFoldersQuery query, CancellationToken cancellationToken)
        {
            return await context.Folder.OrderByDescending(x => x)
                .AsNoTracking().Where(x => x.Id == query.Id)
                .Select(x => new DetailsFolderDTO
                {
                    Color = x.Color,
                    Name = x.Name,
                    Department = x.Department.Name,
                    IsVisible = x.IsVisible,
                }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        }
    }
}
