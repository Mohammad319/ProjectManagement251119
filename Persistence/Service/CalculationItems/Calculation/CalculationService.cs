//using Application.Feature.Calculation.Calculation;
//using Application.Interfaces;
//using Domain.Entities.Calculation;
//using ProjectManagement.Shared.Base.Calculation;
//using ProjectManagement.Shared.DTO.Calculation;

//namespace Persistence.Service.CalculationItems
//{
//    public sealed class CalculationService(ShardingSingleDbContext dataAccess, INotificationHub notification) : ICalculationService
//    {

//        // -------------------------------------------------
//        // Create
//        // -------------------------------------------------
//        public async Task<int> CreateAsync(
//            CalculationPostDTO dto,
//            Guid projectId,
//            int userId,
//            int? departmentId,
//            CancellationToken cancellationToken = default)
//        {
//            // ممكن لاحقاً تضيف تحقق على departmentId من المشروع

//            double? maxOrder = await dataAccess.Calculations
//                .Where(x => x.ProjectId == projectId)
//                .MaxAsync(x => (double?)x.SortOrder, cancellationToken);

//            var sortOrder = maxOrder.HasValue ? maxOrder.Value + 100 : 100;

//            // إنشاء كيان جديد باستخدام منطق الدومين
//            var calculation = new CalculationEntity();
//            calculation.AssignToProject(projectId);
//            calculation.Update(dto); // يملأ Code/Name/Tax/Dates/Metadata/Ids...

//            calculation.SortOrder = sortOrder;
//            calculation.CreatedBy = userId;
//            calculation.CreatedAt = DateTime.UtcNow;

//            dataAccess.Calculations.Add(calculation);
//            await dataAccess.SaveChangesAsync(cancellationToken);

//            return calculation.Id;
//        }

//        // -------------------------------------------------
//        // Update
//        // -------------------------------------------------
//        public async Task<bool> UpdateAsync(
//            int id,
//            CalculationPostDTO dto,
//            int userId,
//            int? departmentId,
//            CancellationToken cancellationToken = default)
//        {
//            var calculation = await dataAccess.Calculations
//                .FirstOrDefaultAsync(
//                    x => x.Id == id &&
//                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
//                    cancellationToken);

//            if (calculation == null)
//                return false;

//            calculation.Update(dto);
//            calculation.UpdatedAt = DateTime.UtcNow;
//            calculation.UpdatedBy = userId;

//            await dataAccess.SaveChangesAsync(cancellationToken);

//            // تجهيز DTO للـ Hub (هنا استخدم CalculationPageDTO كـ payload)
//            var pageDto = new CalculationPageDTO
//            {
//                Tax = calculation.Tax,
//                Name = calculation.Name,
//                OrganisationId = calculation.OrganisationId,
//                Code = calculation.Code,
//                TemplateId = calculation.TemplateId,
//                Factors = calculation.HourlyPriceFactorData.Factors,
//                QuanityList = calculation.Metadata.QuanityList,
//                Compensation = calculation.Compensation?.Name ?? string.Empty,
//                Customer = calculation.Organisation?.Name ?? string.Empty,
//                Contract = calculation.Contract?.Name ?? string.Empty
//            };

//            await notification.SendNotificationAsync(
//                calculation.Id.ToString(),
//                ObjectTypHub.calculation,
//                OperationType.Update,
//                pageDto);

//            return true;
//        }

//        // -------------------------------------------------
//        // Soft Delete
//        // -------------------------------------------------
//        public async Task<bool> DeleteAsync(
//            int id,
//            int userId,
//            int? departmentId,
//            CancellationToken cancellationToken = default)
//        {
//            var calculation = await dataAccess.Calculations
//                .FirstOrDefaultAsync(
//                    x => x.Id == id &&
//                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
//                    cancellationToken);

//            if (calculation == null)
//                return false;

//            calculation.IsDeleted = true;
//            calculation.DeletedAt = DateTime.UtcNow;
//            calculation.DeletedBy = userId;

//            await dataAccess.SaveChangesAsync(cancellationToken);

//            // لو حابب تبعث إشعار بالحذف أضفه هنا

//            return true;
//        }

//        // -------------------------------------------------
//        // Copy
//        // -------------------------------------------------
//        public async Task<int> CopyAsync(
//            int id,
//            Guid projectId,
//            int? departmentId,
//            int userId,
//            CancellationToken cancellationToken = default)
//        {
//            var original = await dataAccess.Calculations
//                .AsNoTracking()
//                .Include(c => c.Tasks)
//                    .ThenInclude(t => t.Resources)
//                .FirstOrDefaultAsync(
//                    x => x.Id == id &&
//                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
//                    cancellationToken);

//            if (original == null)
//                return 0;

//            // استعمل منطق الدومين لعمل نسخة
//            var copy = CalculationEntity.CreateCopy(original, projectId, userId);

//            double? maxOrder = await dataAccess.Calculations
//                .Where(x => x.ProjectId == projectId)
//                .MaxAsync(x => (double?)x.SortOrder, cancellationToken);

//            copy.SortOrder = maxOrder.HasValue ? maxOrder.Value + 100 : 100;

//            dataAccess.Calculations.Add(copy);
//            await dataAccess.SaveChangesAsync(cancellationToken);

//            return copy.Id;
//        }

//        // -------------------------------------------------
//        // Change SortOrder
//        // -------------------------------------------------
//        public async Task<bool> NewOrderAsync(
//            int id,
//            double newOrder,
//            CancellationToken cancellationToken = default)
//        {
//            var calculation = await dataAccess.Calculations.FindAsync(id);

//            if (calculation == null)
//                return false;

//            calculation.SortOrder = newOrder;
//            await dataAccess.SaveChangesAsync(cancellationToken);

//            return true;
//        }

//        // -------------------------------------------------
//        // Update Hourly Price List
//        // -------------------------------------------------
//        public async Task<bool> UpdateHourlyPriceListAsync(
//            int id,
//            List<HourlyPriceListGroupDTO> hourlyPriceList,
//            int userId,
//            int? departmentId,
//            CancellationToken cancellationToken = default)
//        {
//            var calculation = await dataAccess.Calculations
//                .FirstOrDefaultAsync(
//                    x => x.Id == id &&
//                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
//                    cancellationToken);

//            if (calculation == null)
//                return false;

//            calculation.UpdateHourlyPriceList(hourlyPriceList); // يستعمل Update(List<HourlyPriceListGroupDTO>)
//            calculation.UpdatedBy = userId;
//            calculation.UpdatedAt = DateTime.UtcNow;

//            await dataAccess.SaveChangesAsync(cancellationToken);

//            await notification.SendNotificationAsync(
//                calculation.Id.ToString(),
//                ObjectTypHub.HourlyPrice,
//                OperationType.Update,
//                hourlyPriceList);

//            return true;
//        }

//        // -------------------------------------------------
//        // Update Factors
//        // -------------------------------------------------
//        public async Task<bool> UpdateFactorsAsync(
//            int id,
//            List<OHFactors> factors,
//            int? departmentId,
//            CancellationToken cancellationToken = default)
//        {
//            var calculation = await dataAccess.Calculations
//                .FirstOrDefaultAsync(
//                    x => x.Id == id &&
//                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
//                    cancellationToken);

//            if (calculation == null)
//                return false;

//            calculation.UpdateFactors(factors);

//            await dataAccess.SaveChangesAsync(cancellationToken);

//            var pageDto = new CalculationPageDTO
//            {
//                Tax = calculation.Tax,
//                Name = calculation.Name,
//                OrganisationId = calculation.OrganisationId,
//                Code = calculation.Code,
//                TemplateId = calculation.TemplateId,
//                Factors = calculation.HourlyPriceFactorData.Factors,
//                QuanityList = calculation.Metadata.QuanityList,
//                Compensation = calculation.Compensation?.Name ?? string.Empty,
//                Customer = calculation.Organisation?.Name ?? string.Empty,
//                Contract = calculation.Contract?.Name ?? string.Empty
//            };

//            await notification.SendNotificationAsync(
//                calculation.Id.ToString(),
//                ObjectTypHub.calculation,
//                OperationType.Update,
//                pageDto);

//            return true;
//        }

//        // -------------------------------------------------
//        // Update Quantity List
//        // -------------------------------------------------
//        public async Task<bool> UpdateQuantityListAsync(
//            int id,
//            List<QuanityListDTO> model,
//            int? departmentId,
//            CancellationToken cancellationToken = default)
//        {
//            var calculation = await dataAccess.Calculations
//                .FirstOrDefaultAsync(
//                    x => x.Id == id &&
//                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
//                    cancellationToken);

//            if (calculation == null)
//                return false;

//            calculation.Metadata.QuanityList = model;

//            await dataAccess.SaveChangesAsync(cancellationToken);

//            var pageDto = new CalculationPageDTO
//            {
//                Tax = calculation.Tax,
//                Name = calculation.Name,
//                OrganisationId = calculation.OrganisationId,
//                Code = calculation.Code,
//                TemplateId = calculation.TemplateId,
//                Factors = calculation.HourlyPriceFactorData.Factors,
//                QuanityList = calculation.Metadata.QuanityList,
//                Compensation = calculation.Compensation?.Name ?? string.Empty,
//                Customer = calculation.Organisation?.Name ?? string.Empty,
//                Contract = calculation.Contract?.Name ?? string.Empty
//            };

//            await notification.SendNotificationAsync(
//                calculation.Id.ToString(),
//                ObjectTypHub.calculation,
//                OperationType.Update,
//                pageDto);

//            return true;
//        }
//    }
//}

using Application.Feature.Calculation.Calculation;
using Application.Interfaces;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using Persistence.Context;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems
{
    public sealed class CalculationService(IDbContextFactory dbFactory, INotificationHub notification)
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

            double? maxOrder = await db.Calculations
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (double?)x.SortOrder, cancellationToken);

            var sortOrder = maxOrder.HasValue ? maxOrder.Value + 100 : 100;

            var calculation = new CalculationEntity();
            calculation.AssignToProject(projectId);
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
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
                    cancellationToken);

            if (calculation == null)
                return false;

            calculation.Update(dto);
            calculation.UpdatedAt = DateTime.UtcNow;
            calculation.UpdatedBy = userId;

            await db.SaveChangesAsync(cancellationToken);

            var pageDto = new CalculationPageDTO
            {
                Tax = calculation.Tax,
                Name = calculation.Name,
                OrganisationId = calculation.OrganisationId,
                Code = calculation.Code,
                TemplateId = calculation.TemplateId,
                Factors = calculation.HourlyPriceFactorData.Factors,
                QuanityList = calculation.Metadata.QuanityList,
                Compensation = calculation.Compensation?.Name ?? string.Empty,
                Customer = calculation.Organisation?.Name ?? string.Empty,
                Contract = calculation.Contract?.Name ?? string.Empty
            };

            await notification.SendNotificationAsync(
                calculation.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                pageDto);

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
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
                    cancellationToken);

            if (calculation == null)
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
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
                    cancellationToken);

            if (original == null)
                return 0;

            var copy = CalculationEntity.CreateCopy(original, projectId, userId);

            double? maxOrder = await db.Calculations
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (double?)x.SortOrder, cancellationToken);

            copy.SortOrder = maxOrder.HasValue ? maxOrder.Value + 100 : 100;

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

            var calculation = await db.Calculations.FindAsync([id], cancellationToken);
            if (calculation == null)
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
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
                    cancellationToken);

            if (calculation == null)
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
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
                    cancellationToken);

            if (calculation == null)
                return false;

            calculation.UpdateFactors(factors);
            await db.SaveChangesAsync(cancellationToken);

            var pageDto = new CalculationPageDTO
            {
                Tax = calculation.Tax,
                Name = calculation.Name,
                OrganisationId = calculation.OrganisationId,
                Code = calculation.Code,
                TemplateId = calculation.TemplateId,
                Factors = calculation.HourlyPriceFactorData.Factors,
                QuanityList = calculation.Metadata.QuanityList,
                Compensation = calculation.Compensation?.Name ?? string.Empty,
                Customer = calculation.Organisation?.Name ?? string.Empty,
                Contract = calculation.Contract?.Name ?? string.Empty
            };

            await notification.SendNotificationAsync(
                calculation.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                pageDto);

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
                .FirstOrDefaultAsync(
                    x => x.Id == id &&
                         (departmentId == null || x.Project.Folder.DepartmentId == departmentId),
                    cancellationToken);

            if (calculation == null)
                return false;

            calculation.Metadata.QuanityList = model;
            await db.SaveChangesAsync(cancellationToken);

            var pageDto = new CalculationPageDTO
            {
                Tax = calculation.Tax,
                Name = calculation.Name,
                OrganisationId = calculation.OrganisationId,
                Code = calculation.Code,
                TemplateId = calculation.TemplateId,
                Factors = calculation.HourlyPriceFactorData.Factors,
                QuanityList = calculation.Metadata.QuanityList,
                Compensation = calculation.Compensation?.Name ?? string.Empty,
                Customer = calculation.Organisation?.Name ?? string.Empty,
                Contract = calculation.Contract?.Name ?? string.Empty
            };

            await notification.SendNotificationAsync(
                calculation.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                pageDto);

            return true;
        }
    }
}
