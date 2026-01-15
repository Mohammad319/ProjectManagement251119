using Application.Feature.Calculation.Calculation;
using Application.Interfaces;
using Domain.Entities.Calculation;
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
            // TODO: إن احتجت لاحقاً تحقق departmentId مرتبط بالمشروع (Authorization/Validation)
            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (double?)x.SortOrder, cancellationToken);

            var sortOrder = (maxOrder ?? 0) + 100;

            CalculationEntity calculation = new();
            calculation.AssignToProject(projectId);
            calculation.AssignDepartment(departmentId.Value);
            calculation.Update(dto);

            calculation.SortOrder = sortOrder;
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

            var calculation = await db.Calculations
                .FilterByDepartment(departmentId)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (calculation is null)
                return false;

            calculation.Update(dto);
            if(departmentId.HasValue)
            calculation.AssignDepartment(departmentId.Value);

            calculation.UpdatedAt = DateTime.UtcNow;
            calculation.UpdatedBy = userId;

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

            var calculation = await db.Calculations
                .FilterByDepartment(departmentId)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

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
                .FilterByDepartment(departmentId)
                .Include(c => c.Tasks)
                    .ThenInclude(t => t.Resources)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (original is null)
                return 0;

            var copy = CalculationEntity.CreateCopy(original, projectId, userId);
            copy.AssignDepartment(original.DepartmentId);

            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (double?)x.SortOrder, cancellationToken);

            copy.SortOrder = (maxOrder ?? 0) + 100;

            db.Calculations.Add(copy);
            await db.SaveChangesAsync(cancellationToken);

            return copy.Id;
        }

        public async Task<bool> NewOrderAsync(
            int id,
            double newOrder,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await db.Calculations.FindAsync(new object[] { id }, cancellationToken);
            if (calculation is null)
                return false;

            calculation.SortOrder = newOrder;
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

            var calculation = await db.Calculations
                .FilterByDepartment(departmentId)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (calculation is null)
                return false;

            calculation.UpdateHourlyPriceList(hourlyPriceList);
            calculation.UpdatedBy = userId;
            calculation.UpdatedAt = DateTime.UtcNow;

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

            var calculation = await db.Calculations
                .FilterByDepartment(departmentId)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (calculation is null)
                return false;

            calculation.UpdateFactors(factors);
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

            var calculation = await db.Calculations
                .FilterByDepartment(departmentId)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (calculation is null)
                return false;

            calculation.Metadata.QuanityList = model;
            await db.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                calculation.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                BuildPageDto(calculation));

            return true;
        }

        private static CalculationPageDTO BuildPageDto(CalculationEntity calculation)
            => new()
            {
                Tax = calculation.Tax,
                Name = calculation.Name,
                OrganisationId = calculation.OrganisationId,
                Code = calculation.Code,
                TemplateId = calculation.TemplateId,
                Factors = calculation.Factors,
                QuanityList = calculation.Metadata.QuanityList,
                Compensation = calculation.Compensation?.Name ?? string.Empty,
                Customer = calculation.Organisation?.Name ?? string.Empty,
                Contract = calculation.Contract?.Name ?? string.Empty
            };
    }

    internal static class CalculationQueryExtensions
    {
        public static IQueryable<CalculationEntity> FilterByDepartment(
            this IQueryable<CalculationEntity> query,
            int? departmentId)
        {
            if (!departmentId.HasValue) return query;
            return query.Where(x => x.Project.Folder.DepartmentId == departmentId.Value);
        }
    }
}
