using Application.Feature.Calculation.CalcShare;
using Application.Mapping.Calculation;
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
                .Select(ShareCalcDtoMapper.ProjectListDto())
                .ToListAsync(ct);
        }

        public async Task<int> CreateAsync(
            PostShareCalcDTO dto,
            int fromUser,
            int fromDepartment,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            if (!await CalculationAndDepartmentExistAsync(context, dto.CalculationId, dto.DepartmentId, fromDepartment, ct))
                return 0;

            var data = dto.ToMetadata();

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

            entity.Update(entity.DepartmentId, dto.ToMetadata());

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

            if (!departmentId.HasValue ||
                !await CalculationAndDepartmentExistAsync(context, dto.CalculationId, dto.DepartmentId, departmentId.Value, ct))
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
                .FirstOrDefaultAsync(x => x.Id == dto.Id.Value &&
                    x.Calculation.DepartmentId == departmentId.Value, ct);

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
                .FirstOrDefaultAsync(x => x.Id == id &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value), ct);

            if (entity is null)
                return false;

            context.ShareCalc.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }

        private static async Task<bool> CalculationAndDepartmentExistAsync(
            ShardingSingleDbContext context,
            int calculationId,
            int departmentId,
            int sourceDepartmentId,
            CancellationToken ct)
        {
            if (calculationId <= 0 || departmentId <= 0)
                return false;

            var calculationExists = await context.Calculations
                .AsNoTracking()
                .AnyAsync(x => x.Id == calculationId && x.DepartmentId == sourceDepartmentId, ct);

            if (!calculationExists)
                return false;

            return await context.Department
                .AsNoTracking()
                .AnyAsync(x => x.Id == departmentId, ct);
        }
    }
}
