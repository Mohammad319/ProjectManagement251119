using Application.Feature.Calculation.CalcShare;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.ShareCalc
{
    public sealed class ShareCalcCommandService(IDbContextFactoryTenant dbFactory) : IShareCalcService
    {
        public async Task<IReadOnlyList<ListShareCalcDTO>> GetAsync(
            int calculationId,
            int? departmentId,
            int userId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var query = context.ShareCalc
                .AsNoTracking()
                .Where(x => x.CalculationId == calculationId);

            if (departmentId.HasValue)
            {
                var depId = departmentId.Value;
                query = query.Where(x => x.Calculation.Project.Folder.DepartmentId == depId);
            }

            return await query
                .Select(x => new ListShareCalcDTO
                {
                    Id = x.Id,
                    DepartmentId = x.DepartmentId,
                    UserId = x.CreatedBy,
                    User = ((x.CreatedAtUser == null ? string.Empty : (x.CreatedAtUser.FirstName ?? string.Empty)) + " " +
                            (x.CreatedAtUser == null ? string.Empty : (x.CreatedAtUser.LastName ?? string.Empty))).Trim(),
                    Tap1 = x.Metadata.Tap1,
                    Tap2 = x.Metadata.Tap2,
                    Tap3 = x.Metadata.Tap3,
                    Tap4 = x.Metadata.Tap4,
                    Tap5 = x.Metadata.Tap5,
                    Tap6 = x.Metadata.Tap6,
                    Department = x.Department == null ? string.Empty : (x.Department.Name ?? string.Empty)
                })
                .ToListAsync(ct);
        }

        public async Task<int> CreateAsync(
            PostShareCalcDTO dto,
            int fromUser,
            int fromDepartment,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (!await CalculationAndDepartmentExistAsync(context, dto.CalculationId, dto.DepartmentId, ct))
                return 0;

            var data = BuildMetadata(dto.Tap1, dto.Tap2, dto.Tap3, dto.Tap4, dto.Tap5, dto.Tap6);

            var existing = await context.ShareCalc
                .FirstOrDefaultAsync(x => x.CalculationId == dto.CalculationId && x.DepartmentId == dto.DepartmentId, ct);

            if (existing is not null)
            {
                existing.Update(existing.DepartmentId, data);
                await context.SaveChangesAsync(ct);
                return existing.Id;
            }

            var entity = new ShareCalcEntity(
                calculationId: dto.CalculationId,
                departmentId: dto.DepartmentId,
                createdBy: fromUser,
                metadata: data
            );

            context.ShareCalc.Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(
            UpdateShareCalcDTO dto,
            int fromUser,
            int fromDepartment,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ShareCalc
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                return false;

            var data = BuildMetadata(dto.Tap1, dto.Tap2, dto.Tap3, dto.Tap4, dto.Tap5, dto.Tap6);

            entity.Update(entity.DepartmentId, data);

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<int> UpsertAsync(
            ShareCalcUpsertDTO dto,
            int userId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (!await CalculationAndDepartmentExistAsync(context, dto.CalculationId, dto.DepartmentId, ct))
                return 0;

            var data = new ShareCalcData { Tabs = dto.ResolveTabs() };

            if (dto.Id is null or 0)
            {
                var existingForTarget = await context.ShareCalc
                    .FirstOrDefaultAsync(x => x.CalculationId == dto.CalculationId && x.DepartmentId == dto.DepartmentId, ct);

                if (existingForTarget is not null)
                {
                    existingForTarget.Update(existingForTarget.DepartmentId, data);
                    await context.SaveChangesAsync(ct);
                    return existingForTarget.Id;
                }

                var entity = new ShareCalcEntity(dto.CalculationId, dto.DepartmentId, userId, data);
                context.ShareCalc.Add(entity);
                await context.SaveChangesAsync(ct);
                return entity.Id;
            }

            var existing = await context.ShareCalc
                .FirstOrDefaultAsync(x => x.Id == dto.Id.Value, ct);

            if (existing is null)
                return 0;

            var duplicateTargetExists = await context.ShareCalc
                .AnyAsync(x => x.Id != existing.Id &&
                               x.CalculationId == existing.CalculationId &&
                               x.DepartmentId == dto.DepartmentId, ct);

            if (duplicateTargetExists)
                return 0;

            existing.Update(dto.DepartmentId, data);
            await context.SaveChangesAsync(ct);
            return existing.Id;
        }

        public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
            => DeleteInternalAsync(id, departmentId: null, userId: null, ct);

        public Task<bool> DeleteAsync(int id, int departmentId, int userId, CancellationToken ct = default)
            => DeleteInternalAsync(id, departmentId, userId, ct);

        private async Task<bool> DeleteInternalAsync(int id, int? departmentId, int? userId, CancellationToken ct)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var entity = await context.ShareCalc
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null)
                return false;

            if (departmentId.HasValue && entity.DepartmentId != departmentId.Value)
                return false;

            if (userId.HasValue && entity.CreatedBy != userId.Value)
                return false;

            context.ShareCalc.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        private static ShareCalcData BuildMetadata(bool tap1, bool tap2, bool tap3, bool tap4, bool tap5, bool tap6)
            => new()
            {
                Tap1 = tap1,
                Tap2 = tap2,
                Tap3 = tap3,
                Tap4 = tap4,
                Tap5 = tap5,
                Tap6 = tap6
            };

        private static async Task<bool> CalculationAndDepartmentExistAsync(
            ShardingSingleDbContext context,
            int calculationId,
            int departmentId,
            CancellationToken ct)
        {
            if (calculationId <= 0 || departmentId <= 0)
                return false;

            var calculationExists = await context.Calculations
                .AsNoTracking()
                .AnyAsync(x => x.Id == calculationId, ct);

            if (!calculationExists)
                return false;

            return await context.Department
                .AsNoTracking()
                .AnyAsync(x => x.Id == departmentId, ct);
        }
    }
}
