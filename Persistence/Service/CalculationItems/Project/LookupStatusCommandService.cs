using Application.Feature.General;
using Domain.Entities.Calculation;
using Domain.Entities.Project;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.General;

namespace Persistence.Service.CalculationItems.Project
{
    public sealed class LookupStatusCommandService<TS>(IDbContextFactoryTenant dbFactory) : ILookupStatusCommandService<TS>
        where TS : class, IListOrderDTO, new()
    {
        public async Task<int> CreateAsync(PostTaskStatusDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            // هنا نستخدم TS بدل TaskStatusEntity
            var entity = new TS();
            entity.Update(dto.Name, dto.Color, dto.Order, dto.IsVisible);
            ApplyStatusSettings(entity, dto);
            await ApplyControlStatusSettingsAsync(context, entity, dto, ct);

            context.Set<TS>().Add(entity);
            await context.SaveChangesAsync(ct);
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(int id, PostTaskStatusDTO dto, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var set =context.Set<TS>();

            var entity = await set.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null)
                return false;

            entity.Update(dto.Name, dto.Color, dto.Order, dto.IsVisible);
            ApplyStatusSettings(entity, dto);
            await ApplyControlStatusSettingsAsync(context, entity, dto, ct);

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var set =context.Set<TS>();

            var entity = await set.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity == null)
                return false;

            if (await IsUsedAsync(context, entity, id, ct))
                entity.Update(entity.Name, entity.Color, entity.SortOrder, false);
            else
                set.Remove(entity);

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IReadOnlyList<LookupAdminListItemDto>> GetAllListAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var items = await context.Set<TS>()
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            return items.Select(x => new LookupAdminListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Color = x.Color,
                    SortOrder = x.SortOrder,
                    IsVisible = x.IsVisible,
                    Code = x is IControlStatusEntity control ? control.Code : string.Empty,
                    IsDefault = (x is IControlStatusEntity controlDefault && controlDefault.IsDefault)
                              || (x is ProjectStatusEntity psDefault && psDefault.IsDefault)
                              || (x is StatusEntity seDefault && seDefault.IsDefault)
                              || (x is TypeEntity teDefault && teDefault.IsDefault)
                              || (x is ContractEntity ceDefault && ceDefault.IsDefault)
                              || (x is CompensationEntity compDefault && compDefault.IsDefault)
                              || (x is ProcurementMethodEntity pmDefault && pmDefault.IsDefault)
                              || (x is ProcurementProcedureEntity ppDefault && ppDefault.IsDefault),
                    IsSystemDefault = x is IControlStatusEntity controlSystem && controlSystem.IsSystemDefault
                })
                .ToList();
        }

        // -------------------------------------------------
        // GetAllAsync: يرجع كل الكيانات كما هي
        // -------------------------------------------------
        public async Task<List<TS>> GetAllAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Set<TS>()
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);
        }

        // -------------------------------------------------
        // GetVisualAsync: للـ dropdowns و الـ UI
        // -------------------------------------------------
        public async Task<IEnumerable<ListOrderDTO>> GetVisualAsync(int? id, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var query = context.Set<TS>().AsNoTracking();

            if (id.HasValue)
            {
                int targetId = id.Value;
                query = query.Where(x => x.IsVisible || x.Id == targetId);
            }
            else
            {
                query = query.Where(x => x.IsVisible);
            }

            var items = await query
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            return items.Select(x => new ListOrderDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortOrder = x.SortOrder,
                    Color = x.Color,
                    Code = x is IControlStatusEntity control ? control.Code : string.Empty,
                    IsDefault = (x is IControlStatusEntity controlDefault && controlDefault.IsDefault)
                              || (x is ProjectStatusEntity ps && ps.IsDefault)
                              || (x is StatusEntity se && se.IsDefault)
                              || (x is TypeEntity te && te.IsDefault)
                              || (x is ContractEntity ce && ce.IsDefault)
                              || (x is CompensationEntity comp && comp.IsDefault)
                              || (x is ProcurementMethodEntity pm && pm.IsDefault)
                              || (x is ProcurementProcedureEntity pp && pp.IsDefault),
                    IsSystemDefault = x is IControlStatusEntity controlSystem && controlSystem.IsSystemDefault
                })
                .ToList();
        }

        public async Task<bool> MoveAsync(int id, bool moveUp, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            var all = await context.Set<TS>().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
            var item = all.FirstOrDefault(x => x.Id == id);
            if (item is null) return false;

            var sameGroup = all.Where(x => x.IsVisible == item.IsVisible).ToList();
            var groupIdx = sameGroup.FindIndex(x => x.Id == id);
            var swapIdx = moveUp ? groupIdx - 1 : groupIdx + 1;
            if (swapIdx < 0 || swapIdx >= sameGroup.Count) return false;

            // Swap by position in list, then normalize SortOrder.
            // Direct SortOrder-value swap fails when two adjacent items share the same value.
            (sameGroup[groupIdx], sameGroup[swapIdx]) = (sameGroup[swapIdx], sameGroup[groupIdx]);

            for (var i = 0; i < sameGroup.Count; i++)
                sameGroup[i].Update(sameGroup[i].Name, sameGroup[i].Color, i, sameGroup[i].IsVisible);

            await context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<int> CountCalculationsByStatusAsync(int id, CancellationToken ct = default)
        {
            if (typeof(TS) != typeof(StatusEntity)) return 0;
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Set<CalculationEntity>()
                .Where(x => x.StatusId == id)
                .CountAsync(ct);
        }

        public async Task<int> CountProjectsByStatusAsync(int id, CancellationToken ct = default)
        {
            if (typeof(TS) != typeof(ProjectStatusEntity)) return 0;
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            return await context.Set<ProjectEntity>()
                .Where(x => x.ProjectStatusId == id)
                .CountAsync(ct);
        }

        private static void ApplyStatusSettings(TS entity, PostTaskStatusDTO dto)
        {
            if (entity is StatusEntity status)
            {
                status.SetApprovalSettings(
                    dto.IsApprovalStatus,
                    dto.LocksCalculation,
                    dto.AllowsProductionCalculation);
                status.SetHitRateSettings(
                    dto.CountsAsSubmittedBid,
                    dto.CountsAsWonBid,
                    dto.CountsAsLostBid);
            }
            else if (entity is ProjectStatusEntity projectStatus)
            {
                projectStatus.SetHitRateSettings(
                    dto.CountsAsSubmittedBid,
                    dto.CountsAsWonBid,
                    dto.CountsAsLostBid);
            }
        }

        private static async System.Threading.Tasks.Task ApplyControlStatusSettingsAsync(
            Microsoft.EntityFrameworkCore.DbContext context,
            TS entity,
            PostTaskStatusDTO dto,
            CancellationToken ct)
        {
            if (entity is IControlStatusEntity controlStatus)
            {
                var code = controlStatus.IsSystemDefault && !string.IsNullOrWhiteSpace(controlStatus.Code)
                    ? controlStatus.Code
                    : dto.Code;

                controlStatus.SetControlStatusSettings(code, dto.IsDefault, dto.IsSystemDefault || controlStatus.IsSystemDefault);

                if (!controlStatus.IsDefault)
                    return;

                var set = context.Set<TS>();
                var statuses = await set.ToListAsync(ct);
                foreach (var other in statuses)
                {
                    if (other.Id == entity.Id || other is not IControlStatusEntity otherControl)
                        continue;

                    otherControl.SetControlStatusSettings(otherControl.Code, isDefault: false, otherControl.IsSystemDefault);
                }
            }
            else if (entity is ProjectStatusEntity projectStatus)
            {
                projectStatus.SetIsDefault(dto.IsDefault);

                if (!dto.IsDefault)
                    return;

                var statuses = await context.Set<TS>().ToListAsync(ct);
                foreach (var other in statuses)
                {
                    if (other.Id == entity.Id || other is not ProjectStatusEntity otherPs)
                        continue;

                    otherPs.SetIsDefault(false);
                }
            }
            else if (entity is StatusEntity calcStatus)
            {
                calcStatus.SetIsDefault(dto.IsDefault);

                if (!dto.IsDefault)
                    return;

                var statuses = await context.Set<TS>().ToListAsync(ct);
                foreach (var other in statuses)
                {
                    if (other.Id == entity.Id || other is not StatusEntity otherCs)
                        continue;

                    otherCs.SetIsDefault(false);
                }
            }
            else if (entity is TypeEntity typeEntity)
            {
                typeEntity.SetIsDefault(dto.IsDefault);

                if (!dto.IsDefault)
                    return;

                var types = await context.Set<TS>().ToListAsync(ct);
                foreach (var other in types)
                {
                    if (other.Id == entity.Id || other is not TypeEntity otherType)
                        continue;

                    otherType.SetIsDefault(false);
                }
            }
            else if (entity is ContractEntity contractEntity)
            {
                contractEntity.SetIsDefault(dto.IsDefault);

                if (!dto.IsDefault)
                    return;

                var contracts = await context.Set<TS>().ToListAsync(ct);
                foreach (var other in contracts)
                {
                    if (other.Id == entity.Id || other is not ContractEntity otherContract)
                        continue;

                    otherContract.SetIsDefault(false);
                }
            }
            else if (entity is CompensationEntity compensationEntity)
            {
                compensationEntity.SetIsDefault(dto.IsDefault);

                if (!dto.IsDefault)
                    return;

                var compensations = await context.Set<TS>().ToListAsync(ct);
                foreach (var other in compensations)
                {
                    if (other.Id == entity.Id || other is not CompensationEntity otherComp)
                        continue;

                    otherComp.SetIsDefault(false);
                }
            }
            else if (entity is ProcurementMethodEntity procurementEntity)
            {
                procurementEntity.SetIsDefault(dto.IsDefault);

                if (!dto.IsDefault)
                    return;

                var procurements = await context.Set<TS>().ToListAsync(ct);
                foreach (var other in procurements)
                {
                    if (other.Id == entity.Id || other is not ProcurementMethodEntity otherPm)
                        continue;

                    otherPm.SetIsDefault(false);
                }
            }
            else if (entity is ProcurementProcedureEntity procedureEntity)
            {
                procedureEntity.SetIsDefault(dto.IsDefault);

                if (!dto.IsDefault)
                    return;

                var procedures = await context.Set<TS>().ToListAsync(ct);
                foreach (var other in procedures)
                {
                    if (other.Id == entity.Id || other is not ProcurementProcedureEntity otherProc)
                        continue;

                    otherProc.SetIsDefault(false);
                }
            }
        }

        private static async System.Threading.Tasks.Task<bool> IsUsedAsync(
            Microsoft.EntityFrameworkCore.DbContext context,
            TS entity,
            int id,
            CancellationToken ct)
        {
            return entity switch
            {
                TaskStatusEntity => await context.Set<TaskEntity>().AnyAsync(x => x.StatusId == id, ct),
                StatusResourcesEntity => await context.Set<ResourceEntity>().AnyAsync(x => x.StatusId == id, ct),
                _ => false
            };
        }

    }


}
