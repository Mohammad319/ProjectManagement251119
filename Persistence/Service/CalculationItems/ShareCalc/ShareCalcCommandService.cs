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

            // لو departmentId null: لا نفلتر بالقسم
            var query = context.ShareCalc
                .AsNoTracking()
                .Where(x => x.CalculationId == calculationId);

            if (departmentId.HasValue)
            {
                // فلترة واضحة بالقسم (مقارنة int مع int)
                var depId = departmentId.Value;
                query = query.Where(x => x.Calculation.Project.Folder.DepartmentId == depId);
            }

            return await query
                .Select(x => new ListShareCalcDTO
                {
                    Id = x.Id,
                    DepartmentId = x.DepartmentId,
                    UserId = x.CreatedBy,

                    // Null-safe (في حال CreatedAtUser لم يتم تحميله/كان null)
                    User = ((x.CreatedAtUser == null ? string.Empty : (x.CreatedAtUser.FirstName ?? string.Empty)) + " " +
                            (x.CreatedAtUser == null ? string.Empty : (x.CreatedAtUser.LastName ?? string.Empty))).Trim(),

                    Tap1 = x.Metadata.Tap1,
                    Tap2 = x.Metadata.Tap2,
                    Tap3 = x.Metadata.Tap3,
                    Tap4 = x.Metadata.Tap4,
                    Tap5 = x.Metadata.Tap5,
                    Tap6 = x.Metadata.Tap6,

                    // Null-safe
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

            var data = new ShareCalcData
            {
                Tap1 = dto.Tap1,
                Tap2 = dto.Tap2,
                Tap3 = dto.Tap3,
                Tap4 = dto.Tap4,
                Tap5 = dto.Tap5,
                Tap6 = dto.Tap6
            };

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

            if (entity is null) return false;

            // إن أردت تقييد التعديل على نفس القسم/المستخدم:
            // if (entity.DepartmentId != fromDepartment || entity.CreatedBy != fromUser) return false;

            var data = new ShareCalcData
            {
                Tap1 = dto.Tap1,
                Tap2 = dto.Tap2,
                Tap3 = dto.Tap3,
                Tap4 = dto.Tap4,
                Tap5 = dto.Tap5,
                Tap6 = dto.Tap6
            };

            // استخدم Update الموجودة على الكيان (مثل Upsert)
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

            var data = new ShareCalcData { Tabs = dto.ResolveTabs() };

            if (dto.Id is null or 0)
            {
                var entity = new ShareCalcEntity(dto.CalculationId, dto.DepartmentId, userId, data);
                context.ShareCalc.Add(entity);
                await context.SaveChangesAsync(ct);
                return entity.Id;
            }

            var existing = await context.ShareCalc
                .FirstOrDefaultAsync(x => x.Id == dto.Id.Value, ct);

            if (existing is null) return 0;

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

            if (entity is null) return false;

            // إن أردت تقييد الحذف حسب القسم/المستخدم فعّل هذا:
            if (departmentId.HasValue && entity.DepartmentId != departmentId.Value) return false;
            if (userId.HasValue && entity.CreatedBy != userId.Value) return false;

            context.ShareCalc.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }
    }
}
