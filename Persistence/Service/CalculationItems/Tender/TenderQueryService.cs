using Application.Services.CalculationItems.Tender;
using Microsoft.EntityFrameworkCore;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Persistence.Service.CalculationItems.Tender
{
    public sealed class TenderQueryService(IDbContextFactoryTenant dbFactory) : ITenderQueryService
    {
        public async Task<TenderAttributeValuesListDTO> GetTenderListAsync(int calculationId,CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);
            TenderAttributeValuesListDTO result = new();
            result.Tenders = await context.Tenders.Where(x => x.CalculationId == calculationId).Select(x => new TenderListDTO()
            {
                Id = x.Id,
                CompanyId = x.OrganisationId,
                Company = x.Organisation.Name,
                Category = x.Organisation.OrganisationCategory.Name,
                SubCategory = x.Organisation.OrganisationCategory.ParentCategory.Name,
                Values = x.TendersAttributes.Select((a) => new ValuesList()
                {
                    AttributeID = a.TenderAttributeId,
                    Values = a.Value
                }).ToList(),
            }).AsNoTracking().ToListAsync();

            result.Attributes = await context.AttributeNameTender
                .Where(x => x.CalculationId == calculationId)
                .Select(x => new TenderAttributeListDTO()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Note = x.Note,
                }).AsNoTracking().ToListAsync();

            return result;
        }

        public async Task<TenderDetailsDTO?> GetTenderDetailsAsync(
            int tenderId, int CalculationId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Tenders.Where(x => x.Id == tenderId && x.CalculationId == CalculationId).Select(
                x => new TenderDetailsDTO()
                {
                    Id = x.Id,
                    Company = x.Organisation.Name,
                    Category = x.Organisation.OrganisationCategory.Name,
                    SubCategory = x.Organisation.OrganisationCategory.ParentCategory.Name,
                    Note = x.Note,
                }
                ).AsNoTracking().FirstOrDefaultAsync();
        }
    }

}
