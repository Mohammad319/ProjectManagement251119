using Application.Feature.General.DropdownSettings;
using Domain.Entities.Project;
using Persistence.Factory;
using ProjectManagement.Shared.Constant;

namespace Persistence.Service.UserSettings
{
    public sealed class DropdownSettingService(IDbContextFactoryTenant dbFactory) : IDropdownSettingService
    {
        public async Task<Dictionary<string, bool>> GetRequirementsAsync(CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            // Tenant scoping is applied automatically by the global query filter.
            var stored = await context.DropdownSettings
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Category, x => x.IsRequired, ct);

            return DropdownCategoryConst.MergeWithDefaults(stored);
        }

        public async Task SetRequiredAsync(string category, bool isRequired, CancellationToken ct = default)
        {
            await using var context = await dbFactory.CreateDbContextAsync(ct);

            var existing = await context.DropdownSettings
                .FirstOrDefaultAsync(x => x.Category == category, ct);

            if (existing is null)
            {
                context.DropdownSettings.Add(DropdownSettingEntity.Create(category, isRequired));
            }
            else
            {
                existing.SetIsRequired(isRequired);
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
