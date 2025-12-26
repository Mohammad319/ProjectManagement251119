using Application.Services.CalculationItems.Tender;
using Persistence.Factory;
using ProjectManagement.Shared.DTO.Calculation;

namespace Persistence.Service.CalculationItems.Tender
{
    public sealed class TenderQueryService(IDbContextFactoryTenant dbFactory) : ITenderQueryService
    {
        public async Task<List<TenderListDTO>> GetTenderListAsync(
            int calculationId,
            CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            return await context.Tenders
                .Where(x => x.CalculationId == calculationId)
                .Select(x => new TenderListDTO
                {
                    Id = x.Id,
                    Company = x.Organisation.Name,
                    CompanyId = x.OrganisationId,
                    Note = x.Note
                })
                .ToListAsync(ct);
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
