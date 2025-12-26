using Application.Services.CalculationItems.CalcShare;
using Domain.Entities.Calculation;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.ShareCalc
{
    public sealed class ShareCalcCommandService(IDbContextFactoryTenant dbFactory) : IShareCalcService
    {

        public async Task<IEnumerable<ListShareCalcDTO>> GetAsync(int calculationId, int? departmentId, int userId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.ShareCalc.AsNoTracking().Where(x => x.CalculationId == calculationId &&
                            x.Calculation.Project.Folder.DepartmentId == departmentId)
                .Select(x => new ListShareCalcDTO
                {
                    Id = x.Id,
                    DepartmentId = x.DepartmentId,
                    UserId = x.CreatedBy,
                    User = x.CreatedAtUser!.FirstName + " " + x.CreatedAtUser!.LastName,
                    Tap1 = x.Metadata.Tap1,
                    Tap2 = x.Metadata.Tap2,
                    Tap3 = x.Metadata.Tap3,
                    Tap4 = x.Metadata.Tap4,
                    Tap5 = x.Metadata.Tap5,
                    Tap6 = x.Metadata.Tap6,
                    Department = x.Department.Name
                })
                .ToListAsync(ct)
                .ContinueWith(t => (IEnumerable<ListShareCalcDTO>)t.Result, ct);
        }

        public async Task<int> CreateAsync(PostShareCalcDTO dto, int fromUser, int fromDepartment, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            // ملاحظة: dto غالبًا يحتوي CalculationId + DepartmentId + taps
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

        public async Task<bool> UpdateAsync(UpdateShareCalcDTO dto, int fromUser, int fromDepartment, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entity = await context.ShareCalc.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);
            if (entity == null) return false;

            // إن كنت تريد منع تعديل غير المالك/قسم مختلف، ضع قواعدك هنا
            //entity.UpdateDepartment(dto.de);

            //entity.UpdateTabs(new ShareCalcData
            //{
            //    Tap1 = dto.Tap1,
            //    Tap2 = dto.Tap2,
            //    Tap3 = dto.Tap3,
            //    Tap4 = dto.Tap4,
            //    Tap5 = dto.Tap5,
            //    Tap6 = dto.Tap6
            //});

            await context.SaveChangesAsync(ct);
            return true;
        }
        public async Task<int> UpsertAsync(ShareCalcUpsertDTO dto, int userId, int? departmentId, CancellationToken ct = default)
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

            var existing = await context.ShareCalc.FirstOrDefaultAsync(x => x.Id == dto.Id.Value, ct);
            if (existing == null) return 0;

            existing.Update(dto.DepartmentId, data);
            await context.SaveChangesAsync(ct);
            return existing.Id;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entity = await context.ShareCalc.FindAsync(id);
            if (entity == null) return false;

            context.ShareCalc.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }
        public async Task<bool> DeleteAsync(int id, int departmentId, int userId, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var entity = await context.ShareCalc.FindAsync(id);
            if (entity == null) return false;

            context.ShareCalc.Remove(entity);
            await context.SaveChangesAsync(ct);
            return true;
        }
    }

}
