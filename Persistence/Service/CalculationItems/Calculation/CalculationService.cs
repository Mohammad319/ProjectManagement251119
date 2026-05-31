using Application.Feature.Calculation.Calculation;
using Application.Interfaces;
using Application.Mapping.Calculation;
using Domain.Entities.Calculation;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Factory;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.Enums;

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

            var projectDepartmentId = await db.Projects
                .AsNoTracking()
                .Where(p => p.Id == projectId)
                .Select(p => (int?)p.Folder.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (projectDepartmentId is null)
                return 0;

            if (departmentId.HasValue && projectDepartmentId != departmentId.Value)
                return 0;

            var effectiveDepartmentId = departmentId ?? projectDepartmentId.Value;

            if (!await ValidateCalculationReferencesAsync(db, dto, projectId, effectiveDepartmentId, null, cancellationToken))
                return 0;

            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (int?)x.SortOrder, cancellationToken);

            var sortOrder = (maxOrder ?? 0) + 100;
            dto.CalculationType = CalculationVersionType.Tender;

            var calculation = new CalculationEntity();
            calculation.AssignToProject(projectId);
            calculation.AssignDepartment(effectiveDepartmentId);
            calculation.Update(dto);
            calculation.UpdateOrder(sortOrder);
            calculation.InitializeVersionGroup();
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

            var calculation = await GetUnlockedCalculationAsync(db, id, departmentId, cancellationToken);
            if (calculation is null)
                return false;

            if (!calculation.IsCurrentVersion)
            {
                if (dto.IsVisible == calculation.IsVisible)
                    return false;

                if (!dto.IsVisible &&
                    await HasDerivedCalculationsAsync(db, calculation.Id, cancellationToken))
                {
                    return false;
                }
            }

            var effectiveDepartmentId = departmentId ?? calculation.DepartmentId;
            if (!await ValidateCalculationReferencesAsync(db, dto, calculation.ProjectId, effectiveDepartmentId, id, cancellationToken))
                return false;

            var oldStatusId = calculation.StatusId;
            var requestedStatus = dto.StatusId.HasValue
                ? await db.CalculationStatus
                    .AsNoTracking()
                    .Where(x => x.Id == dto.StatusId.Value)
                    .Select(x => new { x.IsApprovalStatus, x.LocksCalculation })
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            dto.CalculationType = calculation.CalculationType;
            calculation.Update(dto);
            calculation.AssignDepartment(effectiveDepartmentId);

            if (!calculation.IsLocked &&
                oldStatusId != calculation.StatusId &&
                requestedStatus is { IsApprovalStatus: true, LocksCalculation: true })
            {
                calculation.ApproveAndLock(userId, await GetUserDisplayNameAsync(db, userId, cancellationToken));
            }

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

            var calculation = await GetUnlockedCalculationAsync(db, id, departmentId, cancellationToken);
            if (calculation is null ||
                calculation.IsCurrentVersion ||
                await HasDerivedCalculationsAsync(db, calculation.Id, cancellationToken))
            {
                return false;
            }

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
            bool allowCrossDepartment,
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

            if (departmentId.HasValue && targetProjectDepartmentId.Value != departmentId.Value && !allowCrossDepartment)
                return 0;

            var copy = CalculationEntity.CreateCopy(original, projectId, userId);

            var existingNames = await db.Calculations
                .AsNoTracking()
                .Where(x => x.ProjectId == projectId)
                .Select(x => x.Name)
                .ToListAsync(cancellationToken);

            var existingCodes = await db.Calculations
                .AsNoTracking()
                .Where(x => x.ProjectId == projectId)
                .Select(x => x.Code)
                .ToListAsync(cancellationToken);

            var copyDto = original.ToPostDto();
            copyDto.Name = EnsureUniqueName(copyDto.Name, existingNames);
            copyDto.Code = EnsureUniqueCode(copyDto.Code, existingCodes);
            copy.Update(copyDto);
            copy.AssignDepartment(targetProjectDepartmentId.Value);

            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (int?)x.SortOrder, cancellationToken);

            copy.UpdateOrder((maxOrder ?? 0) + 100);

            db.Calculations.Add(copy);
            await db.SaveChangesAsync(cancellationToken);

            return copy.Id;
        }

        public async Task<int> CreateProductionCopyAsync(
            int id,
            int? departmentId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var original = await db.Calculations
                .Include(c => c.Status)
                .Include(c => c.Tasks)
                    .ThenInclude(t => t.Resources)
                .FirstOrDefaultAsync(x => x.Id == id &&
                    (!departmentId.HasValue || x.DepartmentId == departmentId.Value), cancellationToken);

            if (original is null ||
                !original.IsCurrentVersion ||
                !original.IsLocked ||
                (original.CalculationType != CalculationVersionType.Tender &&
                 original.CalculationType != CalculationVersionType.Contract) ||
                original.Status is not { AllowsProductionCalculation: true })
            {
                return 0;
            }

            var copy = CalculationEntity.CreateCopy(original, original.ProjectId, userId);

            var existingNames = await db.Calculations
                .AsNoTracking()
                .Where(x => x.ProjectId == original.ProjectId)
                .Select(x => x.Name)
                .ToListAsync(cancellationToken);

            var existingCodes = await db.Calculations
                .AsNoTracking()
                .Where(x => x.ProjectId == original.ProjectId)
                .Select(x => x.Code)
                .ToListAsync(cancellationToken);

            var copyDto = original.ToPostDto();
            copyDto.Name = EnsureUniqueName(copyDto.Name, existingNames);
            copyDto.Code = EnsureUniqueCode(copyDto.Code, existingCodes);
            copyDto.CalculationType = CalculationVersionType.Production;

            copy.Update(copyDto);
            copy.AssignDepartment(original.DepartmentId);
            copy.MarkAsProductionCopy(original.Id);

            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == original.ProjectId)
                .MaxAsync(x => (int?)x.SortOrder, cancellationToken);

            copy.UpdateOrder((maxOrder ?? 0) + 100);

            db.Calculations.Add(copy);
            await db.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                original.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                BuildPageDto(original));

            return copy.Id;
        }

        public async Task<int> CreateContractCopyAsync(
            int id,
            int? departmentId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var original = await db.Calculations
                .Include(c => c.Status)
                .Include(c => c.Tasks)
                    .ThenInclude(t => t.Resources)
                .FirstOrDefaultAsync(x => x.Id == id &&
                    (!departmentId.HasValue || x.DepartmentId == departmentId.Value), cancellationToken);

            if (original is null ||
                !original.IsCurrentVersion ||
                !original.IsLocked ||
                original.CalculationType != CalculationVersionType.Tender ||
                original.Status is not { AllowsProductionCalculation: true })
            {
                return 0;
            }

            var copy = CalculationEntity.CreateCopy(original, original.ProjectId, userId);

            var existingNames = await db.Calculations
                .AsNoTracking()
                .Where(x => x.ProjectId == original.ProjectId)
                .Select(x => x.Name)
                .ToListAsync(cancellationToken);

            var existingCodes = await db.Calculations
                .AsNoTracking()
                .Where(x => x.ProjectId == original.ProjectId)
                .Select(x => x.Code)
                .ToListAsync(cancellationToken);

            var copyDto = original.ToPostDto();
            copyDto.Name = EnsureUniqueName(copyDto.Name, existingNames);
            copyDto.Code = EnsureUniqueCode(copyDto.Code, existingCodes);
            copyDto.CalculationType = CalculationVersionType.Contract;

            copy.Update(copyDto);
            copy.AssignDepartment(original.DepartmentId);
            copy.MarkAsContractCopy(original.Id);

            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == original.ProjectId)
                .MaxAsync(x => (int?)x.SortOrder, cancellationToken);

            copy.UpdateOrder((maxOrder ?? 0) + 100);

            db.Calculations.Add(copy);
            await db.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                original.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                BuildPageDto(original));

            return copy.Id;
        }

        public async Task<int> CreateVersionAsync(
            int id,
            int? departmentId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var original = await db.Calculations
                .Include(c => c.Tasks)
                    .ThenInclude(t => t.Resources)
                .FirstOrDefaultAsync(x => x.Id == id &&
                    !x.IsDeleted &&
                    (!departmentId.HasValue || x.DepartmentId == departmentId.Value), cancellationToken);

            if (original is null)
                return 0;

            original.InitializeVersionGroup();

            var versionGroupId = original.VersionGroupId;
            var nextVersionNumber = await db.Calculations
                .Where(x => x.ProjectId == original.ProjectId && x.VersionGroupId == versionGroupId)
                .MaxAsync(x => (int?)x.VersionNumber, cancellationToken) ?? 0;
            nextVersionNumber++;

            var existingCodes = await db.Calculations
                .AsNoTracking()
                .Where(x => x.ProjectId == original.ProjectId)
                .Select(x => x.Code)
                .ToListAsync(cancellationToken);

            var copy = CalculationEntity.CreateCopy(original, original.ProjectId, userId);
            var copyDto = original.ToPostDto();
            copyDto.Code = EnsureVersionCode(copyDto.Code, nextVersionNumber, existingCodes);
            copyDto.IsVisible = true;

            copy.Update(copyDto);
            copy.AssignDepartment(original.DepartmentId);
            copy.MarkAsNewVersion(versionGroupId, nextVersionNumber, original.Id);

            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == original.ProjectId)
                .MaxAsync(x => (int?)x.SortOrder, cancellationToken);

            copy.UpdateOrder((maxOrder ?? 0) + 100);

            var previousVersions = await db.Calculations
                .Where(x => x.ProjectId == original.ProjectId && x.VersionGroupId == versionGroupId)
                .ToListAsync(cancellationToken);

            foreach (var previousVersion in previousVersions)
                previousVersion.MarkAsNotCurrentVersion();

            db.Calculations.Add(copy);
            await db.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                original.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                BuildPageDto(original));

            return copy.Id;
        }

        public async Task<bool> MoveAsync(
            int id,
            Guid projectId,
            int? departmentId,
            int userId,
            bool allowCrossDepartment,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await GetEditableCalculationAsync(db, id, departmentId, cancellationToken);
            if (calculation is null)
                return false;

            var targetProjectDepartmentId = await db.Projects
                .AsNoTracking()
                .Where(p => p.Id == projectId)
                .Select(p => (int?)p.Folder.DepartmentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!targetProjectDepartmentId.HasValue)
                return false;

            if (departmentId.HasValue && targetProjectDepartmentId.Value != departmentId.Value && !allowCrossDepartment)
                return false;

            var maxOrder = await db.Calculations
                .Where(x => x.ProjectId == projectId)
                .MaxAsync(x => (int?)x.SortOrder, cancellationToken);

            calculation.MoveToProject(projectId, targetProjectDepartmentId.Value);
            calculation.UpdateOrder((maxOrder ?? 0) + 100);
            Touch(calculation, userId);

            await db.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                calculation.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                BuildPageDto(calculation));

            return true;
        }

        private static async Task<string> GetUserDisplayNameAsync(
            ShardingSingleDbContext db,
            int userId,
            CancellationToken cancellationToken)
        {
            if (userId <= 0)
                return string.Empty;

            var user = await db.User
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new { x.FirstName, x.LastName, x.UserName, x.Email })
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null)
                return string.Empty;

            var fullName = string.Join(" ", new[] { user.FirstName, user.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            return !string.IsNullOrWhiteSpace(user.UserName) ? user.UserName : user.Email ?? string.Empty;
        }

        public async Task<bool> NewOrderAsync(
            int id,
            int newOrder,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var rows = await db.Calculations
                .Where(x => x.Id == id &&
                    !x.IsLocked &&
                    (!departmentId.HasValue || x.DepartmentId == departmentId.Value))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SortOrder, newOrder), cancellationToken);
            return rows > 0;
        }

        public async Task<bool> UpdateSortAsync(
            int id,
            SortConfig sort,
            int userId,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await GetEditableCalculationAsync(db, id, departmentId, cancellationToken);
            if (calculation is null)
                return false;

            calculation.UpdateSort(sort ?? new SortConfig());
            Touch(calculation, userId);

            await db.SaveChangesAsync(cancellationToken);

            await notification.SendNotificationAsync(
                calculation.Id.ToString(),
                ObjectTypHub.calculation,
                OperationType.Update,
                BuildPageDto(calculation));

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

        public async Task<bool> UpdateDisplayPresetsAsync(
            int id,
            DisplayOptionsPresetStore store,
            int? departmentId,
            CancellationToken cancellationToken = default)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var calculation = await GetEditableCalculationAsync(db, id, departmentId, cancellationToken);
            if (calculation is null)
                return false;

            calculation.UpdateDisplayPresets(store);
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static async Task<CalculationEntity?> GetEditableCalculationAsync(
            ShardingSingleDbContext db,
            int id,
            int? departmentId,
            CancellationToken cancellationToken)
        {
            return await db.Calculations
                .FirstOrDefaultAsync(x => x.Id == id &&
                    x.IsCurrentVersion &&
                    !x.IsLocked &&
                    (!departmentId.HasValue || x.DepartmentId == departmentId.Value), cancellationToken);
        }

        private static async Task<CalculationEntity?> GetUnlockedCalculationAsync(
            ShardingSingleDbContext db,
            int id,
            int? departmentId,
            CancellationToken cancellationToken)
        {
            return await db.Calculations
                .FirstOrDefaultAsync(x => x.Id == id &&
                    !x.IsLocked &&
                    (!departmentId.HasValue || x.DepartmentId == departmentId.Value), cancellationToken);
        }

        private static Task<bool> HasDerivedCalculationsAsync(
            ShardingSingleDbContext db,
            int sourceCalculationId,
            CancellationToken cancellationToken)
        {
            return db.Calculations
                .AsNoTracking()
                .AnyAsync(
                    calculation => !calculation.IsDeleted &&
                        calculation.SourceCalculationId == sourceCalculationId,
                    cancellationToken);
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

            if (dto.TemplateColumnId.HasValue)
            {
                var templateColumnAllowed = await db.TemplateColumns
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == dto.TemplateColumnId.Value && (x.DepartmentId == null || x.DepartmentId == departmentId), cancellationToken);

                if (!templateColumnAllowed)
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

        private static string EnsureUniqueName(string name, IEnumerable<string> existingNames)
        {
            var existing = existingNames.ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            if (!existing.Contains(name))
                return name;

            var baseName = $"{name} - kopia";
            if (!existing.Contains(baseName))
                return baseName;

            var index = 2;
            while (existing.Contains($"{baseName} {index}"))
                index++;

            return $"{baseName} {index}";
        }

        private static string EnsureUniqueCode(string code, IEnumerable<string> existingCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            return EnsureUniqueName(code, existingCodes);
        }

        private static string EnsureVersionCode(string code, int versionNumber, IEnumerable<string> existingCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
                return string.Empty;

            var suffix = $"-v{versionNumber}";
            var maxBaseLength = Math.Max(1, 80 - suffix.Length);
            var baseCode = code.Trim();

            if (baseCode.Length > maxBaseLength)
                baseCode = baseCode[..maxBaseLength];

            var existing = existingCodes.ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            var candidate = $"{baseCode}{suffix}";
            if (!existing.Contains(candidate))
                return candidate;

            var index = 2;
            while (true)
            {
                var extraSuffix = $"{suffix}-{index}";
                maxBaseLength = Math.Max(1, 80 - extraSuffix.Length);
                var indexedBase = code.Trim();
                if (indexedBase.Length > maxBaseLength)
                    indexedBase = indexedBase[..maxBaseLength];

                candidate = $"{indexedBase}{extraSuffix}";
                if (!existing.Contains(candidate))
                    return candidate;

                index++;
            }
        }

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
                TemplateColumnId = x.TemplateColumnId,
                CalculationType = x.CalculationType,
                BidRole = x.BidRole,
                IsLocked = x.IsLocked,
                LockedAtUtc = x.LockedAtUtc,
                LockedByUserId = x.LockedByUserId,
                ApprovedByUserId = x.ApprovedByUserId,
                ApprovedByName = x.ApprovedByName,
                ApprovedAtUtc = x.ApprovedAtUtc,
                SourceCalculationId = x.SourceCalculationId,
                VersionGroupId = x.VersionGroupId,
                VersionNumber = x.VersionNumber,
                CreatedFromCalculationId = x.CreatedFromCalculationId,
                IsCurrentVersion = x.IsCurrentVersion,
                Sort = x.Sort,
                Tasks = []
            };
        }
    }
}
