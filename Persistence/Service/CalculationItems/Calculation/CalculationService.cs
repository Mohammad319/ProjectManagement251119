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

            var projectDepartmentId = await db.Projects
                .AsNoTracking()
                .Where(p => p.Id == projectId)
                .Select(p => p.Folder.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (projectDepartmentId != departmentId.Value)
                return 0;

            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (double?)x.SortOrder, cancellationToken);

            var sortOrder = (maxOrder ?? 0) + 100;

            CalculationEntity calculation = new();
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

            var calculation = await db.Calculations
                .FilterByDepartment(departmentId)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (calculation is null)
                return false;

            calculation.Update(dto);
            if (departmentId.HasValue)
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

            copy.UpdateOrder((maxOrder ?? 0) + 100);

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
            CalculationData value,
            int? departmentId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await db.Calculations
                .FilterByDepartment(departmentId)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (calculation is null)
                return false;

            var dto = new CalculationPostDTO();
            dto.Metadata.Address = value.Address;
            dto.Metadata.Contacts = value.Contacts;
            dto.Metadata.Notes = value.Notes;
            dto.Metadata.Responsibles = value.Responsibles;
            dto.Metadata.Income = value.Income;
            dto.Metadata.Maps = value.Maps;
            dto.Metadata.Developer = value.Developer;
            dto.Metadata.ClientsManager = value.ClientsManager;
            dto.Metadata.Designer = value.Designer;
            dto.Metadata.OverviewInfo = value.OverviewInfo;
            dto.Metadata.ContactPerson = value.ContactPerson;
            dto.Metadata.Supervisor = value.Supervisor;
            dto.Metadata.Inspector = value.Inspector;

            dto.Code = calculation.Code;
            dto.Name = calculation.Name;
            dto.Tax = calculation.Tax;
            dto.Procurement = calculation.Procurement;
            dto.StartDate = calculation.StartDate;
            dto.EndDate = calculation.EndDate;
            dto.TenderDeadline = calculation.TenderDeadline;
            dto.TenderQA = calculation.TenderQA;
            dto.Order = calculation.SortOrder;
            dto.PublicationDate = calculation.PublicationDate;
            dto.DecisionDate = calculation.DecisionDate;
            dto.IsPrivate = calculation.IsPrivate;
            dto.StatusId = calculation.StatusId;
            dto.ContractId = calculation.ContractId;
            dto.TypeId = calculation.TypeId;
            dto.OrganisationId = calculation.OrganisationId;
            dto.ProcurementMethodsId = calculation.ProcurementMethodsId;
            dto.CompensationId = calculation.CompensationId;
            dto.IsVisible = calculation.IsVisible;

            calculation.Update(dto);
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

        private static CalculationPageDTO BuildPageDto(CalculationEntity x)
        {
            return new CalculationPageDTO
            {
                Factors = x.Factors,
                Tax = x.Tax,
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
