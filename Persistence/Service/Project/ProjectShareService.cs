using Application.Feature.Project.ProjectShare;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Project;

namespace Persistence.Service.Project
{
    public sealed class ProjectShareService(IDbContextFactoryTenant dbFactory) : IProjectShareService
    {
        public async Task<IReadOnlyList<ProjectShareListItemDTO>> GetByProjectAsync(
            Guid projectId, int? departmentId, int userId, CancellationToken ct = default)
        {
            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            return await ctx.ProjectShare
                .AsNoTracking()
                .Where(s => s.ProjectId == projectId)
                .Select(s => new ProjectShareListItemDTO
                {
                    Id = s.Id,
                    RecipientType = s.SharedWithUserId != null
                        ? ProjectShareRecipientType.User
                        : ProjectShareRecipientType.Department,
                    UserId = s.SharedWithUserId,
                    DepartmentId = s.DepartmentId,
                    RecipientName = s.SharedWithUserId != null
                        ? (((s.SharedWithUser!.FirstName ?? "") + " " + (s.SharedWithUser!.LastName ?? "")).Trim() == ""
                            ? s.SharedWithUser!.UserName
                            : ((s.SharedWithUser!.FirstName ?? "") + " " + (s.SharedWithUser!.LastName ?? "")).Trim())
                        : s.Department!.Name,
                    Role = s.Role,
                    CalculationIds = s.Calculations.Select(c => c.CalculationId).ToList()
                })
                .ToListAsync(ct);
        }

        public async Task<int> UpsertAsync(
            Guid projectId, ProjectShareUpsertDTO dto, int userId, int? departmentId, CancellationToken ct = default)
        {
            if (dto is null)
                return 0;

            // Exakt en mottagare måste anges enligt vald typ.
            bool validUser = dto.RecipientType == ProjectShareRecipientType.User && dto.UserId is > 0 && dto.DepartmentId is null;
            bool validDept = dto.RecipientType == ProjectShareRecipientType.Department && dto.DepartmentId is > 0 && dto.UserId is null;
            if (!validUser && !validDept)
                return 0;

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            // Projektet måste finnas inom aktuell tenant (global query filter).
            if (!await ctx.Projects.AnyAsync(p => p.Id == projectId, ct))
                return 0;

            ProjectShareEntity? entity = dto.Id is > 0
                ? await ctx.ProjectShare
                    .Include(s => s.Calculations)
                    .FirstOrDefaultAsync(s => s.Id == dto.Id && s.ProjectId == projectId, ct)
                : null;

            if (entity is null)
            {
                entity = validUser
                    ? ProjectShareEntity.ForUser(projectId, dto.UserId!.Value, dto.Role)
                    : ProjectShareEntity.ForDepartment(projectId, dto.DepartmentId!.Value, dto.Role);
                entity.ReplaceCalculations(dto.CalculationIds);
                ctx.ProjectShare.Add(entity);
            }
            else
            {
                entity.SetRole(dto.Role);
                entity.ReplaceCalculations(dto.CalculationIds);
            }

            await ctx.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> DeleteAsync(int id, int userId, int? departmentId, CancellationToken ct = default)
        {
            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var entity = await ctx.ProjectShare.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity is null)
                return false;

            ctx.ProjectShare.Remove(entity);
            await ctx.SaveChangesAsync(ct);
            return true;
        }
    }
}
