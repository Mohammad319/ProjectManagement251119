#nullable enable

using Application.Feature.Project.ProjectBid;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Project;

namespace Persistence.Service.Project
{
    public sealed class ProjectBidService(IDbContextFactoryTenant dbFactory) : IProjectBidService
    {
        public async Task<List<ProjectBidListDTO>> GetByProjectAsync(
            Guid projectId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.ProjectBids
                .Where(x => x.ProjectId == projectId &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value))
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .Select(x => new ProjectBidListDTO
                {
                    Id = x.Id,
                    BidderName = x.BidderName,
                    Amount = x.Amount,
                    Note = x.Note,
                    IsWinner = x.IsWinner,
                    SortOrder = x.SortOrder
                })
                .ToListAsync(ct);
        }

        public async Task<int> CreateAsync(
            Guid projectId,
            ProjectBidPostDTO dto,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var projectExists = await context.Projects
                .AnyAsync(x => x.Id == projectId &&
                    (!departmentId.HasValue || x.Folder.DepartmentId == departmentId.Value), ct);

            if (!projectExists)
                return 0;

            var sortOrder = await context.ProjectBids
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (int?)x.SortOrder, ct) ?? 0;

            var bid = new ProjectBidEntity(
                projectId: projectId,
                bidderName: dto.BidderName,
                amount: dto.Amount,
                note: dto.Note,
                isWinner: dto.IsWinner,
                sortOrder: sortOrder + 100);

            context.ProjectBids.Add(bid);
            await context.SaveChangesAsync(ct);
            return bid.Id;
        }

        public async Task<bool> UpdateAsync(
            int id,
            Guid projectId,
            ProjectBidPostDTO dto,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var bid = await context.ProjectBids
                .FirstOrDefaultAsync(x => x.Id == id &&
                    x.ProjectId == projectId &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value), ct);

            if (bid is null)
                return false;

            bid.Update(dto.BidderName, dto.Amount, dto.Note, dto.IsWinner);
            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(
            int id,
            Guid projectId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var bid = await context.ProjectBids
                .FirstOrDefaultAsync(x => x.Id == id &&
                    x.ProjectId == projectId &&
                    (!departmentId.HasValue || x.Project.Folder.DepartmentId == departmentId.Value), ct);

            if (bid is null)
                return false;

            context.ProjectBids.Remove(bid);
            await context.SaveChangesAsync(ct);
            return true;
        }
    }
}
