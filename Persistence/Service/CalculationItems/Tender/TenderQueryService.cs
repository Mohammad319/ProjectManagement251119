#nullable enable

using Application.Services.CalculationItems.Tender;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Tender
{
    public sealed class TenderQueryService(IDbContextFactoryTenant dbFactory) : ITenderQueryService
    {
        public async Task<TenderAttributeValuesListDTO> GetTenderListAsync(
            int calculationId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var tenders = await context.Tenders
                .Where(x => x.CalculationId == calculationId &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value))
                .AsNoTracking()
                .Select(x => new TenderListDTO
                {
                    Id = x.Id,
                    CompanyId = x.OrganisationId,
                    Company = x.Organisation != null ? x.Organisation.Name ?? string.Empty : string.Empty,
                    Category = x.Organisation != null && x.Organisation.OrganisationCategory != null ? x.Organisation.OrganisationCategory.Name ?? string.Empty : string.Empty,
                    SubCategory = x.Organisation != null && x.Organisation.OrganisationCategory != null && x.Organisation.OrganisationCategory.ParentCategory != null ? x.Organisation.OrganisationCategory.ParentCategory.Name ?? string.Empty : string.Empty,
                    Values = x.TendersAttributes.Select(a => new ValuesList
                    {
                        AttributeID = a.TenderAttributeId,
                        Values = a.Value
                    }).ToList()
                })
                .ToListAsync(ct);

            var attributes = await context.AttributeNameTender
                .Where(x => x.CalculationId == calculationId &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value))
                .AsNoTracking()
                .Select(x => new TenderAttributeListDTO
                {
                    Id = x.Id,
                    Name = x.Name ?? string.Empty,
                    Note = x.Note ?? string.Empty
                })
                .ToListAsync(ct);

            return new TenderAttributeValuesListDTO
            {
                Tenders = tenders,
                Attributes = attributes
            };
        }

        public async Task<TenderDetailsDTO?> GetTenderDetailsAsync(
            int tenderId,
            int calculationId,
            int? departmentId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Tenders
                .Where(x => x.Id == tenderId &&
                    x.CalculationId == calculationId &&
                    (!departmentId.HasValue || x.Calculation.DepartmentId == departmentId.Value))
                .AsNoTracking()
                .Select(x => new TenderDetailsDTO
                {
                    Id = x.Id,
                    Company = x.Organisation != null ? x.Organisation.Name ?? string.Empty : string.Empty,
                    Category = x.Organisation != null && x.Organisation.OrganisationCategory != null ? x.Organisation.OrganisationCategory.Name ?? string.Empty : string.Empty,
                    SubCategory = x.Organisation != null && x.Organisation.OrganisationCategory != null && x.Organisation.OrganisationCategory.ParentCategory != null ? x.Organisation.OrganisationCategory.ParentCategory.Name ?? string.Empty : string.Empty,
                    Note = x.Note ?? string.Empty
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
