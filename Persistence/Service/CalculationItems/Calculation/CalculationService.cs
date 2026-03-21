using Application.Feature.Calculation.Calculation;
using Application.Interfaces;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Factory;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Calculation
{
    public sealed class CalculationService(
        IDbContextFactoryTenant dbFactory,
        INotificationHub notification)
        : ICalculationService
    {
        public async Task<int> CreateAsync(
            CalculationPostDTO dto,
            Guid projectId,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            if (departmentId is null) return 0;

            var projectDepartmentId = await db.Projects
                .AsNoTracking()
                .Where(p => p.Id == projectId)
                .Select(p => (int?)p.Folder.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (projectDepartmentId != departmentId.Value)
                return 0;

            if (!await ValidateCalculationReferencesAsync(db, dto, projectId, departmentId.Value, null, cancellationToken))
                return 0;

            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (int?)x.SortOrder, cancellationToken);

            var sortOrder = (maxOrder ?? 0) + 100;

            var calculation = new CalculationEntity();
            calculation.AssignToProject(projectId);
            calculation.AssignDepartment(departmentId.Value);
            calculation.Update(dto);
            calculation.UpdateOrder(sortOrder);
            calculation.CreatedBy = userId;
            calculation.CreatedAt = DateTime.UtcNow;

            db.Calculations.Add(calculation);
            await db.SaveChangesAsync(cancellationToken);

            return calculation.Id;
        }

        public async Task<bool> UpdateAsync(
            int id,
            CalculationPostDTO dto,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await GetEditableCalculationAsync(db, id, departmentId, cancellationToken);
            if (calculation is null)
                return false;

            var effectiveDepartmentId = departmentId ?? calculation.DepartmentId;
            if (!await ValidateCalculationReferencesAsync(db, dto, calculation.ProjectId, effectiveDepartmentId, id, cancellationToken))
                return false;

            calculation.Update(dto);
            calculation.AssignDepartment(effectiveDepartmentId);

            Touch(calculation, userId);
            await db.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                calculation.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                BuildPageDto(calculation));

            return true;
        }

        public async Task<bool> DeleteAsync(
            int id,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await GetEditableCalculationAsync(db, id, departmentId, cancellationToken);
            if (calculation is null)
                return false;

            calculation.IsDeleted = true;
            calculation.DeletedAt = DateTime.UtcNow;
            calculation.DeletedBy = userId;

            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<int> CopyAsync(
            int id,
            Guid projectId,
            int? departmentId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var original = await db.Calculations
                .AsNoTracking()
                .Include(c => c.Tasks)
                    .ThenInclude(t => t.Resources)
                .FirstOrDefaultAsync(x => x.Id == id && (!departmentId.HasValue || x.DepartmentId == departmentId.Value), cancellationToken);

            if (original is null)
                return 0;

            var targetProjectDepartmentId = await db.Projects
                .AsNoTracking()
                .Where(p => p.Id == projectId)
                .Select(p => (int?)p.Folder.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!targetProjectDepartmentId.HasValue)
                return 0;

            if (departmentId.HasValue && targetProjectDepartmentId.Value != departmentId.Value)
                return 0;

            var copy = CalculationEntity.CreateCopy(original, projectId, userId);
            copy.AssignDepartment(targetProjectDepartmentId.Value);

            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (int?)x.SortOrder, cancellationToken);

            copy.UpdateOrder((maxOrder ?? 0) + 100);

            db.Calculations.Add(copy);
            await db.SaveChangesAsync(cancellationToken);

            return copy.Id;
        }

        public async Task<bool> NewOrderAsync(
            int id,
            int newOrder,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await db.Calculations
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (calculation is null)
                return false;

            calculation.UpdateOrder(newOrder);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> UpdateHourlyPriceListAsync(
            int id,
            List<HourlyPriceListGroupDTO> hourlyPriceList,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await GetEditableCalculationAsync(db, id, departmentId, cancellationToken);
            if (calculation is null)
                return false;

            calculation.UpdateHourlyPriceList(hourlyPriceList);
            Touch(calculation, userId);

            await db.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                calculation.Id.ToString(),
                ObjectTypHub.HourlyPrice,
                OperationType.Update,
                hourlyPriceList);

            return true;
        }

        public async Task<bool> UpdateFactorsAsync(
            int id,
            List<OHFactors> factors,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await GetEditableCalculationAsync(db, id, departmentId, cancellationToken);
            if (calculation is null)
                return false;

            calculation.UpdateFactors(factors);
            Touch(calculation, calculation.UpdatedBy ?? calculation.CreatedBy ?? 0);
            await db.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                calculation.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                BuildPageDto(calculation));

            return true;
        }

        public async Task<bool> UpdateQuantityListAsync(
            int id,
            List<QuanityListDTO> model,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await GetEditableCalculationAsync(db, id, departmentId, cancellationToken);
            if (calculation is null)
                return false;

            calculation.UpdateMetadata(m => m.QuanityList = model ?? []);
            Touch(calculation, calculation.UpdatedBy ?? calculation.CreatedBy ?? 0);
            await db.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                calculation.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                BuildPageDto(calculation));

            return true;
        }

        private static async Task<CalculationEntity?> GetEditableCalculationAsync(
            ShardingSingleDbContext db,
            int id,
            int? departmentId,
            CancellationToken cancellationToken)
        {
            return await db.Calculations
                .FirstOrDefaultAsync(x => x.Id == id && (!departmentId.HasValue || x.DepartmentId == departmentId.Value), cancellationToken);
        }

        private static async Task<bool> ValidateCalculationReferencesAsync(
            ShardingSingleDbContext db,
            CalculationPostDTO dto,
            Guid projectId,
            int departmentId,
            int? currentCalculationId,
            CancellationToken cancellationToken)
        {
            var projectDepartmentId = await db.Projects
                .AsNoTracking()
                .Where(p => p.Id == projectId)
                .Select(p => (int?)p.Folder.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (projectDepartmentId != departmentId)
                return false;

            if (dto.OrganisationId.HasValue &&
                !await db.Organisation.AsNoTracking().AnyAsync(x => x.Id == dto.OrganisationId.Value, cancellationToken))
                return false;

            if (dto.TypeId.HasValue &&
                !await db.CalcProjectType.AsNoTracking().AnyAsync(x => x.Id == dto.TypeId.Value, cancellationToken))
                return false;

            if (dto.StatusId.HasValue &&
                !await db.CalculationStatus.AsNoTracking().AnyAsync(x => x.Id == dto.StatusId.Value, cancellationToken))
                return false;

            if (dto.ProcurementMethodsId.HasValue &&
                !await db.ProcurementMethod.AsNoTracking().AnyAsync(x => x.Id == dto.ProcurementMethodsId.Value, cancellationToken))
                return false;

            if (dto.CompensationId.HasValue &&
                !await db.Compensations.AsNoTracking().AnyAsync(x => x.Id == dto.CompensationId.Value, cancellationToken))
                return false;

            if (dto.ContractId.HasValue &&
                !await db.Contracts.AsNoTracking().AnyAsync(x => x.Id == dto.ContractId.Value, cancellationToken))
                return false;

            if (dto.TemplateId.HasValue)
            {
                var templateAllowed = await db.Templates
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == dto.TemplateId.Value && (x.DepartmentId == null || x.DepartmentId == departmentId), cancellationToken);

                if (!templateAllowed)
                    return false;
            }

            var normalizedCode = NormalizeCode(dto.Code);
            if (!string.IsNullOrWhiteSpace(normalizedCode))
            {
                var duplicateCodeExists = await db.Calculations
                    .AsNoTracking()
                    .AnyAsync(x => x.ProjectId == projectId && x.Code == normalizedCode && (!currentCalculationId.HasValue || x.Id != currentCalculationId.Value), cancellationToken);

                if (duplicateCodeExists)
                    return false;
            }

            return true;
        }

        private static string? NormalizeCode(string? code)
            => string.IsNullOrWhiteSpace(code) ? null : code.Trim();

        private static void Touch(CalculationEntity calculation, int userId)
        {
            if (userId > 0)
                calculation.UpdatedBy = userId;

            calculation.UpdatedAt = DateTime.UtcNow;
        }

        private static CalculationPageDTO BuildPageDto(CalculationEntity x)
        {
            return new CalculationPageDTO
            {
                Factors = x.Factors,
                QuanityList = x.Metadata.QuanityList,
                Tax = x.Tax,
                TimeMonth = x.Metadata.TimeMonth,
                Name = x.Name,
                Code = x.Code,
                OrganisationId = x.OrganisationId,
                Supervisor = x.Metadata.Supervisor,
                Inspector = x.Metadata.Inspector,
                TemplateId = x.TemplateId,
                Tasks = []
            };
        }
    }
}
